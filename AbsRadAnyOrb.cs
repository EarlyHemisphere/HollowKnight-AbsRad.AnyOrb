using System.Collections.Generic;
using Modding;
using UnityEngine;
using HutongGames.PlayMaker.Actions;
using ModCommon.Util;

namespace AbsRadAnyOrb {
    public class AbsRadAnyOrb : Mod, ITogglableMod {
        public AbsRadAnyOrb() : base("AbsRad AnyOrb") { }
        public override void Initialize(Dictionary<string, Dictionary<string, GameObject>> preloadedObjects) {
            Log("Initializing");

            ModHooks.AfterSavegameLoadHook += AfterSaveGameLoad;
            ModHooks.NewGameHook += AddComponent;
            ModHooks.LanguageGetHook += LangGet;
            Object.DontDestroyOnLoad(preloadedObjects["GG_Radiance"]["Boss Control/Absolute Radiance"]);
            AnyOrb.InstantiateOrbs(preloadedObjects["GG_Radiance"]["Boss Control/Absolute Radiance"].LocateMyFSM("Attack Commands").GetAction<SpawnObjectFromGlobalPool>("Spawn Fireball", 1).gameObject.Value);

            Log("Initialized");
        }

        public override List<(string, string)> GetPreloadNames() => new List<(string, string)> {("GG_Radiance", "Boss Control/Absolute Radiance")};

        public override string GetVersion() => GetType().Assembly.GetName().Version.ToString();

        private static void AfterSaveGameLoad(SaveGameData _) {
            AddComponent();
        }

        private static void AddComponent() {
            GameManager.instance.gameObject.AddComponent<RadianceFinder>();
        }

        private static string LangGet(string key, string sheettitle, string orig) {
            if (key != null) {
                switch (key) {
                    case "ABSOLUTE_RADIANCE_SUPER": return "Orbsolute";
                    case "GG_S_RADIANCE": return "God of orbs.";
                    case "GODSEEKER_RADIANCE_STATUE": return "Ok.";
                    default: return Language.Language.GetInternal(key, sheettitle);
                }
            }
            return orig;
        }

        public override int LoadPriority() => 2; // Initialize after other radiance mods to prevent changes from being overwritten

        public void Unload() {
            ModHooks.AfterSavegameLoadHook -= AfterSaveGameLoad;
            ModHooks.NewGameHook -= AddComponent;
            ModHooks.LanguageGetHook -= LangGet;
            RadianceFinder radianceFinder = GameManager.instance?.gameObject.GetComponent<RadianceFinder>();
            if (radianceFinder != null) {
                radianceFinder.Unload();
                UnityEngine.Object.Destroy(radianceFinder);
            }
        }
    }
}
