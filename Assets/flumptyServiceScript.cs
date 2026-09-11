using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class flumptySettings
{
    public string SiteUrl = "https://ktane.timwi.de/json/raw";
}

public class flumptyModuleInfo
{
    public string id;
    public string name;
    public int date;
    public int tpScore;
    
    public flumptyModuleInfo(string id, string name, int date, int tpScore, int timeModeScore)
    {
        this.id = id;
        this.name = name;
        this.date = date;
        this.tpScore = tpScore;
        this.timeModeScore = timeModeScore;
    }
}

public class flumptyServiceScript : MonoBehaviour { // успешно спиздил у Mystery Module

    private static List<flumptyModuleInfo> modules = new List<flumptyModuleInfo>();

    public static List<flumptyModuleInfo> getModuleInfo() => SettingsLoaded ? modules : null;
    
    static bool SettingsLoaded = false;

    private string _settingsFile;
    private flumptySettings _settings;

    void Start()
    {
        name = "flumptyService";

        _settingsFile = Path.Combine(Path.Combine(Application.persistentDataPath, "Modsettings"), "flumptyService.json");

        if (!File.Exists(_settingsFile))
            _settings = new flumptySettings();
        else
        {
            try
            {
                _settings = JsonConvert.DeserializeObject<flumptySettings>(File.ReadAllText(_settingsFile), new StringEnumConverter());
                if (_settings == null)
                    throw new Exception("Settings could not be read. Creating new Settings...");
                SettingsLoaded = true;
                Debug.LogFormat(@"[flumptyService] Settings successfully loaded");
            }
            catch (Exception e)
            {
                Debug.LogFormat(@"[flumptyService] Error loading settings file:");
                Debug.LogException(e);
                _settings = new flumptySettings();
            }
        }

        Debug.LogFormat(@"[flumptyService] Service is active");
        StartCoroutine(GetData());
    }

    IEnumerator GetData()
    {
        using (var http = UnityWebRequest.Get(_settings.SiteUrl))
        {
            
            yield return http.SendWebRequest();

            if (http.isNetworkError)
            {
                Debug.LogFormat(@"[flumptyService] Website {0} responded with error: {1}", _settings.SiteUrl,
                    http.error);
                yield break;
            }

            if (http.responseCode != 200)
            {
                Debug.LogFormat(@"[flumptyService] Website {0} responded with code: {1}", _settings.SiteUrl,
                    http.responseCode);
                yield break;
            }

            var allModules = JObject.Parse(http.downloadHandler.text)["KtaneModules"] as JArray;
            if (allModules == null)
            {
                Debug.LogFormat(
                    @"[flumptyService] Website {0} did not respond with a JSON array at “KtaneModules” key.",
                    _settings.SiteUrl);
                yield break;
            }
            
            

            foreach (JObject module in allModules.Where(x=> x["Type"].Value<string>() == "Regular"))
            {
                modules.Add(new flumptyModuleInfo(
                    module["ModuleID"].Value<string>(), 
                    module["Name"].Value<string>(), 
                    int.Parse(module["Published"].Value<string>().Replace("-","")),
                    module["TwitchPlays"] == null? 0: module["TwitchPlays"]["Score"].Value<int>(),
                    module["TimeMode"] == null? 0: module["TimeMode"]["Score"].Value<int>()));
                print("[flumptyService] Found module: " + module["Name"].Value<string>() + " ("+ module["ModuleID"].Value<string>()+")");
            }

            Debug.Log("[flumptyService] List successfully loaded.");
            SettingsLoaded = true;

            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(_settingsFile)))
                    Directory.CreateDirectory(Path.GetDirectoryName(_settingsFile));
                File.WriteAllText(_settingsFile,
                    JsonConvert.SerializeObject(_settings, Formatting.Indented, new StringEnumConverter()));
            }
            catch (Exception e)
            {
                Debug.LogFormat("[flumptyService] Failed to save settings file:");
                Debug.LogException(e);
            }
        }
    }
}
