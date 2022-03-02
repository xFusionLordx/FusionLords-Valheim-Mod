using BepInEx;
using HarmonyLib;
using Jotunn.Managers;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Remember_Server
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    internal class Remember_Server : BaseUnityPlugin
    {
        public const string PluginGUID = "com.fuisonlord.serverremember";
        public const string PluginName = "Remember Server";
        public const string PluginVersion = "0.0.1";
        private readonly Harmony harmony = new Harmony(PluginGUID);
        private static readonly string Destination = Paths.ConfigPath + "\\ServerRemember\\Worlds.json";
        private static ObservableCollection<string> ServerWorlds;

        private void Awake()
        {
            harmony.PatchAll();
            GUIManager.OnCustomGUIAvailable += ModifyGUI;
            ServerWorlds = LoadList();
            ServerWorlds.CollectionChanged += SaveList;
        }

        [HarmonyPatch(nameof(FejdStartup.UpdateWorldList))]
        class Patch_UpdateWorldList
        {
            static void Postfix()
            {
                bool flag = false;
                Toggle toggle = Find("OpenServer").GetComponent<Toggle>();
                GameObject selected = FindAll("WorldElement").First(o => o.transform.Find("selected").gameObject.activeSelf);
                String key = getKey(selected);
                if (selected != null && ServerWorlds.Contains(key))
                    flag = true;
                toggle.isOn = flag;
            }
        }

        public static string getKey(GameObject selected)
        {
            return $"Name:{selected.transform.Find("name").GetComponent<Text>().text}:{selected.transform.Find("seed").GetComponent<Text>().text}";
        }

        private ObservableCollection<String> LoadList()
        {
            if (!File.Exists(Destination)) return new ObservableCollection<string>();
            string input = File.ReadAllText(Destination);
            if (input == "") return new ObservableCollection<string>();
            return SimpleJson.SimpleJson.DeserializeObject<ObservableCollection<String>>(input);
        }

        private void SaveList(object sender, NotifyCollectionChangedEventArgs e)
        {
            Logger.LogInfo("Saving server world list.");
            string text = SimpleJson.SimpleJson.SerializeObject(ServerWorlds, SimpleJson.SimpleJson.PocoJsonSerializerStrategy);
            if (!Directory.Exists(Destination.Substring(0, Destination.LastIndexOf("\\"))))
                Directory.CreateDirectory(Destination.Substring(0, Destination.LastIndexOf("\\")));
            if (!File.Exists(Destination))
                File.Create(Destination).Close();
            File.WriteAllText(Destination, text);
            Logger.LogInfo("Save complete.");
        }

        public static GameObject Find(String name) => Resources.FindObjectsOfTypeAll<GameObject>().First(o => o.name == name);

        public static GameObject[] FindAll(string name) => Resources.FindObjectsOfTypeAll<GameObject>().Where(o => o.name.Contains(name)).ToArray();

        public void ModifyGUI()
        {
            var sceneName = SceneManager.GetActiveScene().name;
            if (sceneName != "start") return;
            Toggle toggle = Find("OpenServer").GetComponent<Toggle>();
            toggle.onValueChanged.AddListener((bool isOn) =>
            {
                GameObject selected = FindAll("WorldElement").First(o => o.transform.Find("selected").gameObject.activeSelf);
                string key = getKey(selected);
                if (selected != null && isOn && !ServerWorlds.Contains(key)) ServerWorlds.Add(key);
                else if (selected != null && !isOn && ServerWorlds.Contains(key)) ServerWorlds.Remove(key);
            });
        }
    }
}