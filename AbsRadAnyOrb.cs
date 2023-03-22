using Modding;

public class AbsRadAnyOrb : Mod, ITogglableMod {
    public AbsRadAnyOrb() : base("AbsRad AnyOrb") { }
    public override void Initialize() {
        Log("Initializing");

        ModHooks.AfterSavegameLoadHook += AfterSaveGameLoad;
        ModHooks.NewGameHook += AddComponent;
        ModHooks.LanguageGetHook += LangGet;
        Log("Initialized");
    }

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

    public override int LoadPriority() => 2;

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
