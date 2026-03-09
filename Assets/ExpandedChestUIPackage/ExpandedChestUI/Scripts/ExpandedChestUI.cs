using System.Linq;
using ExpandedChestUI.Scripts.Components;
using PugMod;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace ExpandedChestUI.Scripts
{
    public class ExpandedChestUI : IMod
    {
        public const string Version = "0.3.1";
        public const string ModID = "ExpandedChestUIMod";
        public const string FriendlyName = "Expanded ChestUI Mod";
        internal static readonly Logger Log = new(FriendlyName);
        public static GameObject ChestUIObject;
        
        public void EarlyInit()
        {
            Log.LogInfo($"{FriendlyName} version: {Version}");
            var modInfo = API.ModLoader.LoadedMods.FirstOrDefault(modInfo => modInfo.Handlers.Contains(this));
            if (modInfo == null)
            {
                Log.LogError($"Failed to load {FriendlyName}: metadata not found!");
                return;
            }
            ChestUIObject = modInfo.Assets.OfType<GameObject>().FirstOrDefault(x => x.name == "ExpandedChestUI");
        }

        public void Init() { }

        public void Shutdown() { }

        public void ModObjectLoaded(Object obj) { }

        public void Update() { }
    }
    
    public class Logger
    {
        private readonly string _tag;

        public Logger(string tag) => _tag = $"[{tag}]";

        public void LogDebug(string text) => Debug.unityLogger.Log(LogType.Log, _tag, text);

        public void LogInfo(string text) => Debug.unityLogger.Log(LogType.Log, _tag, text);

        public void LogWarning(string text) => Debug.unityLogger.Log(LogType.Warning, _tag, text);

        public void LogError(string text) => Debug.unityLogger.Log(LogType.Error, _tag, text);
    }
}
