using Modding;

public class AbsRadAnyOrb : Mod, ITogglableMod {
    public AbsRadAnyOrb() : base("AbsRad AnyOrb") { }
    public override void Initialize() {
        Log("Initializing");

        ModHooks.AfterSavegameLoadHook += AfterSaveGameLoad;
        ModHooks.NewGameHook += AddComponent;
        Log("Initialized");
    }

    public override string GetVersion() => GetType().Assembly.GetName().Version.ToString();

    public bool ToggleButtonInsideMenu => false;

    private static void AfterSaveGameLoad(SaveGameData _) {
        AddComponent();
    }

    private static void AddComponent() {
        GameManager.instance.gameObject.AddComponent<RadianceFinder>();
    }

    public void Unload() {
        ModHooks.AfterSavegameLoadHook -= AfterSaveGameLoad;
        ModHooks.NewGameHook -= AddComponent;
        RadianceFinder radianceFinder = GameManager.instance?.gameObject.GetComponent<RadianceFinder>();
        if (radianceFinder != null) {
            radianceFinder.Unload();
            UnityEngine.Object.Destroy(radianceFinder);
        }
    }
}