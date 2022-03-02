using BepInEx;
using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Managers;
using Jotunn.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using static ItemDrop;

namespace LoxMod
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class LoxMod : BaseUnityPlugin
    {
        public const string PluginGUID = "com.FusionLord.LoxMod";
        public const string PluginName = "Lox Mod";
        public const string PluginVersion = "0.0.1";

        public static LoxMod INSTANCE;

        public static ButtonConfig AttackButton = new ButtonConfig
        {
            Name = "Attack",
            Key = KeyCode.Mouse0,
            BlockOtherInputs = true
        },
            SecondAttackButton = new ButtonConfig
            {
                Name = "SecondAttack",
                Key = KeyCode.Mouse2,
                BlockOtherInputs = true
            };
        private static GameObject swipeFab, stompFab;
        private static ItemData swipe, stomp;
        private static float oldSwipe, oldStomp;

        public static ConfigEntry<float> LoxScale;
        public static ConfigEntry<float> LoxPrimaryMultiplier;
        public static ConfigEntry<float> LoxSecondaryMultiplier;

        private void Awake()
        {
            INSTANCE = this;
            InputManager.Instance.AddButton(PluginGUID, AttackButton);
            InputManager.Instance.AddButton(PluginGUID, SecondAttackButton);

            Config.SaveOnConfigSet = true;

            LoxScale = Config.Bind("Creatures", "Lox Scale", 1f,
                new ConfigDescription("Changes Lox size.",
                new AcceptableValueRange<float>(0.05f, 5f)));
            LoxScale.SettingChanged += LoxBehaviour.UpdateScale;

            LoxPrimaryMultiplier = Config.Bind("Creatures", "Lox Damage Primary Modifier", 1f,
                new ConfigDescription("Changes Lox primary attack damage Modifer.",
                new AcceptableValueRange<float>(0f, 2f)));
            LoxPrimaryMultiplier.SettingChanged += UpdateLoxWepsP;

            LoxSecondaryMultiplier = Config.Bind("Creatures", "Lox Damage Secondary Modifier", 1f,
                new ConfigDescription("Changes Lox secondary attack damage Modifer.",
                new AcceptableValueRange<float>(0f, 2f)));
            LoxPrimaryMultiplier.SettingChanged += UpdateLoxWepsS;

            CreatureManager.OnVanillaCreaturesAvailable += ModLox;
            PrefabManager.OnVanillaPrefabsAvailable += GetLoxWeps;

            Log("Lox Mod has landed.");
        }

        public static void UpdateLoxWepsP(object sender = null, EventArgs e = null)
        {
            SettingChangedEventArgs args = (SettingChangedEventArgs)e;
            String key = args.ChangedSetting.Definition.Key;
            if (key == LoxPrimaryMultiplier.Definition.Key)
            {
                swipe.m_shared.m_damages.Modify(1.0f / oldSwipe);
                swipe.m_shared.m_damages.Modify(LoxPrimaryMultiplier.Value);
                oldSwipe = LoxPrimaryMultiplier.Value;
            }
            if (key == LoxSecondaryMultiplier.Definition.Key)
            {
                stomp.m_shared.m_damages.Modify(1.0f / oldStomp);
                stomp.m_shared.m_damages.Modify(LoxSecondaryMultiplier.Value);
                oldStomp = LoxSecondaryMultiplier.Value;
            }
        }

        public static void UpdateLoxWepsS(object sender = null, EventArgs e = null)
        {
            SettingChangedEventArgs args = (SettingChangedEventArgs)e;
        }

        public static void GetLoxWeps()
        {
            swipeFab = PrefabManager.Instance.CreateClonedPrefab("loxmod_bite", "lox_bite");
            ItemData prim = swipeFab.GetComponent<ItemDrop>().m_itemData;
            prim.m_shared.m_attack.m_hitTerrain = true;
            prim.m_shared.m_toolTier = 2;
            prim.m_shared.m_damages.m_chop = 130;
            prim.m_shared.m_damages.m_pickaxe = 130;
            swipe = prim.Clone();
            swipe.m_shared.m_damages.Modify(LoxPrimaryMultiplier.Value);
            oldSwipe = LoxPrimaryMultiplier.Value;
            stompFab = PrefabManager.Instance.CreateClonedPrefab("loxmod_stomp", "lox_stomp");
            ItemData sec = stompFab.GetComponent<ItemDrop>().m_itemData;
            sec.m_shared.m_toolTier = 2;
            sec.m_shared.m_damages.m_chop = 130;
            sec.m_shared.m_damages.m_pickaxe = 130;
            stomp = sec.Clone();
            stomp.m_shared.m_damages.Modify(LoxSecondaryMultiplier.Value);
            oldStomp = LoxSecondaryMultiplier.Value;
        }

        public void Update()
        {
            Player player = Player.m_localPlayer;
            if (player != null)
            {
                if (player.IsRiding() != AttackButton.BlockOtherInputs)
                {
                    AttackButton.BlockOtherInputs = SecondAttackButton.BlockOtherInputs = player.IsRiding();
                }

                IDoodadController doodad = player.GetDoodadController();
                if (doodad is Sadle saddle)
                {
                    if (saddle != null)
                    {
                        Humanoid lox = (Humanoid)saddle.GetCharacter();

                        lox.m_baseAI.StopAllCoroutines();

                        if (ZInput.GetButtonDown(AttackButton.Name) && !lox.InAttack())
                        {
                            lox.m_rightItem = swipe;
                            lox.StartAttack(null, false);
                        }
                        if (ZInput.GetButtonDown(SecondAttackButton.Name) && !lox.InAttack())
                        {
                            lox.m_rightItem = stomp;
                            lox.StartAttack(null, false);
                        }
                    }
                }
            }
        }

        void ModLox()
        {
            CreatureManager.Instance.GetCreaturePrefab("Lox").AddComponent<LoxBehaviour>();
            CreatureManager.OnVanillaCreaturesAvailable -= ModLox;
        }

        void Log(String message) => Logger.LogInfo($"{PluginName}: {message}");

        class LoxBehaviour : MonoBehaviour
        {
            private static readonly List<GameObject> Loxes = new List<GameObject>();

            public static void UpdateScale(object sender, EventArgs e) => Loxes.ForEach(l => l.transform.localScale = Vector3.one * LoxScale.Value);

            void Awake()
            {
                this.gameObject.transform.localScale = Vector3.one * LoxScale.Value;
                Loxes.Add(this.gameObject);
            }

            void OnDestroy() => Loxes.Remove(this.gameObject);
        }
    }
}