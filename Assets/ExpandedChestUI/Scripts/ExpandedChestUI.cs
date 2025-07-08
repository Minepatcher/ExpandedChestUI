using System.Linq;
using PugMod;
using UnityEngine;

namespace ExpandedChestUI.Scripts
{
    public class ExpandedChestUI : IMod
    {
        private const string Version = "0.0.1";
        public const string ModID = "ExpandedChestUIMod";
        public const string FriendlyName = "Expanded ChestUI Mod";
        public static readonly Logger Log = new(FriendlyName);
        public static GameObject ChestUIObject;
        
        public void EarlyInit()
        {
            Log.LogInfo($"{FriendlyName} version: {Version}");
            LoadedMod modInfo = API.ModLoader.LoadedMods.FirstOrDefault(modInfo => modInfo.Handlers.Contains(this));
            if (modInfo != null) return;
            Log.LogError($"Failed to load {FriendlyName}: metadata not found!");
        }

        public void Init()
        {
        }

        public void Shutdown()
        {
        }

        public void ModObjectLoaded(Object obj)
        {
            if (obj is null) return;
            switch (obj)
            {
                case GameObject gameObject:
                {
                    if (gameObject.name == "ExpandedChestUI")
                        ChestUIObject = gameObject;
                    break;
                }
            }
        }

        public void Update()
        {
        }
    }
    
    public class Logger
    {
        private readonly string _tag;

        public Logger(string tag)
        {
            _tag = $"[{tag}]";
        }

        public void LogDebug(string text)
        {
            Debug.unityLogger.Log(LogType.Log, _tag, text);
        }
        
        public void LogInfo(string text)
        {
            Debug.unityLogger.Log(LogType.Log, _tag, text);
        }
        
        public void LogWarning(string text)
        {
            Debug.unityLogger.Log(LogType.Warning, _tag, text);
        }
        
        public void LogError(string text)
        {
            Debug.unityLogger.Log(LogType.Error, _tag, text);
        }
    }
}
