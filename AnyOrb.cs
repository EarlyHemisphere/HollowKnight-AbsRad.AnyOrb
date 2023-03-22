using System.Collections;
using SFCore.Utils;
using UnityEngine;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;

public class AnyOrb : MonoBehaviour {
    private PlayMakerFSM attackCommandsFSM;
    private PlayMakerFSM controlFSM;
    private PlayMakerFSM orbPrefabFinalControlFSM;
    private GameObject[] orbs;
    private static int NUM_ORBS = 500;
    private int spawningIdx;
    private GameObject orbPrefab;
    private float orbZ;
    public void Awake() {
        attackCommandsFSM = base.gameObject.LocateMyFSM("Attack Commands");
        controlFSM = base.gameObject.LocateMyFSM("Control");
        orbs = new GameObject[NUM_ORBS];
        spawningIdx = 0;
    }

    public void Start() {
        Modding.Logger.Log("Changing AbsRad orb spawning behavior...");

        foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>()) {
            if (go.name == "Radiant Orb") {
                orbPrefab = go;
                orbPrefabFinalControlFSM = orbPrefab.LocateMyFSM("Final Control");
            }
        }
        
        for(int i = 0; i < NUM_ORBS; i++) {
            if (!orbs[i]) {
                GameObject orb = GameObject.Instantiate(orbPrefab);
                orb.SetActive(false);
                MeshRenderer orbMeshRenderer = orb.GetComponent<MeshRenderer>();
                orbMeshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                orbMeshRenderer.receiveShadows = false;
                orbMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                orbMeshRenderer.rayTracingMode = UnityEngine.Experimental.Rendering.RayTracingMode.Off;
                PlayMakerFSM orbControl = orb.LocateMyFSM("Orb Control");
                orbControl.RemoveAction("Init", 9);
                orbControl.RemoveAction("Init", 8);
                orbControl.RemoveAction("Init", 7);
                orbControl.RemoveAction("Init", 6);
                orbControl.RemoveAction("Init", 3);
                orbControl.RemoveAction("Init", 0);
                orbControl.RemoveAction("Impact", 11);
                orbControl.RemoveAction("Impact", 10);
                orbControl.RemoveAction("Impact", 8);
                orbControl.RemoveAction("Impact", 6);
                orbControl.RemoveAction("Impact", 5);
                orbControl.RemoveAction("Impact", 4);
                orbControl.RemoveAction("Impact", 1);
                orbControl.RemoveAction("Impact", 0);
                orbControl.RemoveAction("Dissipate", 4);
                orbControl.RemoveAction("Dissipate", 0);
                orbControl.RemoveAction("Stop Particles", 0);
                orbControl.ChangeTransition("Impact", "FINISHED", "Init");
                FsmOwnerDefault ownerDefault = new FsmOwnerDefault();
                ownerDefault.OwnerOption = OwnerDefaultOption.UseOwner;
                orbControl.AddAction("Impact", new ActivateGameObject {
                    gameObject = ownerDefault,
                    activate = false,
                    recursive = false,
                    resetOnExit = false,
                    everyFrame = false
                });
                orbControl.AddAction("Stop Particles", new ActivateGameObject {
                    gameObject = ownerDefault,
                    activate = false,
                    recursive = false,
                    resetOnExit = false,
                    everyFrame = false
                });
                orbControl.ChangeTransition("Stop Particles", "FINISHED", "Init");
                orbControl.RemoveFsmState("Recycle");
                GameObject.DestroyImmediate(orb.GetComponent<AudioSource>());
                for (int j = 0; j < orb.transform.childCount; j++) {
                    GameObject child = orb.transform.GetChild(j).gameObject;
                    if (child.name == "Fader" ||
                        child.name == "Fader Old" ||
                        child.name == "Appear Glow" ||
                        child.name == "Particle System" ||
                        child.name == "Impact" ||
                        child.name == "Impact Particles") {
                        GameObject.Destroy(child);
                    }
                }
                // Component copy = orb.AddComponent<PlayMakerFSM>();
                // foreach (FieldInfo field in orbPrefabFinalControlFSM.GetType().GetFields()) {
                //     field.SetValue(copy, field.GetValue(orbPrefabFinalControlFSM));
                // }
                PlayMakerFSM finalControlFSM = orb.LocateMyFSM("Final Control");
                finalControlFSM.RemoveFsmState("Recycle");
                // finalControlFSM.AddFsmState("Deactivate");
                // finalControlFSM.AddAction("Deactivate", new ActivateGameObject {
                //     gameObject = ownerDefault,
                //     activate = false,
                //     recursive = false,
                //     resetOnExit = false,
                //     everyFrame = false
                // });
                // finalControlFSM.AddTransition("Check", "END", "Deactivate");
                // finalControlFSM.AddTransition("Deactivate", "FINISHED", "Check");
                // GameObjectExtensions.PrintSceneHierarchyTree(orb);
                orbs[i] = orb;
            }
        }
        
        orbZ = attackCommandsFSM.GetAction<SetVector3XYZ>("Orb Pos", 2).z.Value;
        StartCoroutine(AddExtraOrbSpawns());
    }

    private void Update() {
        // Bit of a performance hit here, but I could not get the code above to work and I'm lazy
        PlayMakerFSM finalControl = orbPrefab.LocateMyFSM("Final Control");
        float minX = finalControl.FsmVariables.GetFsmFloat("Min X").Value;
        float maxX = finalControl.FsmVariables.GetFsmFloat("Max X").Value;
        float minY = finalControl.FsmVariables.GetFsmFloat("Min Y").Value;
        float maxY = finalControl.FsmVariables.GetFsmFloat("Max Y").Value;
        if (controlFSM.FsmVariables.GetFsmBool("Ascend Ready").Value == true && base.gameObject.transform.GetPositionY() > 150f) {
            foreach(GameObject orb in orbs) {
                if (orb.activeSelf) {
                    if (orb.transform.GetPositionX() < minX ||
                        orb.transform.GetPositionX() > maxX ||
                        orb.transform.GetPositionY() < minY ||
                        orb.transform.GetPositionY() > maxY
                    ) {
                        orb.SetActive(false);
                    }
                }
            }
        }
    }

    private IEnumerator AddExtraOrbSpawns() {
        yield return null;
        attackCommandsFSM.InsertAction("Spawn Fireball", new CallMethod {
            behaviour = this,
            methodName = "SpawnExtraOrbs",
            parameters = new FsmVar[0],
            everyFrame = false
        }, 0);
    }

    public void SpawnExtraOrbs() {
        float orbMinX = attackCommandsFSM.FsmVariables.GetFsmFloat("Orb Min X").Value;
        float orbMaxX = attackCommandsFSM.FsmVariables.GetFsmFloat("Orb Max X").Value;
        float orbMinY = attackCommandsFSM.FsmVariables.GetFsmFloat("Orb Min Y").Value;
        float orbMaxY = attackCommandsFSM.FsmVariables.GetFsmFloat("Orb Max Y").Value;
        float minDist = attackCommandsFSM.GetAction<FloatCompare>("Orb Pos", 6).float2.Value;
        float maxDist = attackCommandsFSM.GetAction<FloatCompare>("Orb Pos", 7).float2.Value;
        Vector3 radPos = base.gameObject.transform.position;
        Vector3 innerCircleStartingPos = base.gameObject.transform.position + new Vector3(0, -minDist, 0);
        Vector3 outerCircleStartingPos = base.gameObject.transform.position + new Vector3(0, -maxDist, 0);

        // Spawn along top of range
        for (float x = orbMinX; x < orbMaxX; x += 2.5f) {
            float distance = Vector2.Distance(new Vector2(x, orbMaxY), new Vector2(base.gameObject.transform.GetPositionX(), base.gameObject.transform.GetPositionY()));
            if (distance >= minDist && distance <= maxDist) {
                SpawnOrb(x, orbMaxY);
            }
        }

        // Spawn along bottom of range
        for (float x = orbMinX; x < orbMaxX; x += 2.5f) {
            float distance = Vector2.Distance(new Vector2(x, orbMinY), new Vector2(base.gameObject.transform.GetPositionX(), base.gameObject.transform.GetPositionY()));
            if (distance >= minDist && distance <= maxDist) {
                SpawnOrb(x, orbMinY);
            }
        }

        // Spawn along left side of range
        for (float y = orbMinY; y < orbMaxY; y += 2.5f) {
            float distance = Vector2.Distance(new Vector2(orbMinX, y), new Vector2(base.gameObject.transform.GetPositionX(), base.gameObject.transform.GetPositionY()));
            if (distance >= minDist && distance <= maxDist) {
                SpawnOrb(orbMinX, y);
            }
        }

        // Spawn along right side of range
        for (float y = orbMinY; y < orbMaxY; y += 2.5f) {
            float distance = Vector2.Distance(new Vector2(orbMaxX, y), new Vector2(base.gameObject.transform.GetPositionX(), base.gameObject.transform.GetPositionY()));
            if (distance >= minDist && distance <= maxDist) {
                SpawnOrb(orbMaxX, y);
            }
        }
        
        // Spawn along inner circle
        for (float degrees = 0; degrees < 360; degrees += 45) {
            Vector3 rotatedPos = Quaternion.Euler(0, 0, degrees) * (innerCircleStartingPos - radPos) + radPos;
            if (rotatedPos.x >= orbMinX && rotatedPos.x <= orbMaxX && rotatedPos.y >= orbMinY && rotatedPos.y <= orbMaxY) {
                SpawnOrb(rotatedPos.x, rotatedPos.y);
            }
        }

        // Spawn along outer circle
        for (float degrees = 0; degrees < 360; degrees += 15) {
            Vector3 rotatedPos = Quaternion.Euler(0, 0, degrees) * (outerCircleStartingPos - radPos) + radPos;
            if (rotatedPos.x >= orbMinX && rotatedPos.x <= orbMaxX && rotatedPos.y >= orbMinY && rotatedPos.y <= orbMaxY) {
                SpawnOrb(rotatedPos.x, rotatedPos.y);
            }
        }

        // Fill out inside boundaries
        for (float x = orbMinX + 2; x < orbMaxX - 2; x += 2.5f) {
            for (float y = orbMinY + 2; y < orbMaxY - 2; y += 2.5f) {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(base.gameObject.transform.GetPositionX(), base.gameObject.transform.GetPositionY()));
                if (distance < attackCommandsFSM.GetAction<FloatCompare>("Orb Pos", 6).float2.Value ||
                    distance > attackCommandsFSM.GetAction<FloatCompare>("Orb Pos", 7).float2.Value) {
                    continue;
                }

                SpawnOrb(x, y);
            }
        }
    }

    private void SpawnOrb(float x, float y) {
        orbs[spawningIdx].transform.SetPosition3D(x, y, orbZ);
        orbs[spawningIdx].SetActive(true);
        orbs[spawningIdx].GetComponent<PlayMakerFSM>().SendEvent("FIRE");
        spawningIdx = (spawningIdx + 1) % NUM_ORBS;
    }

    public void Unload() {
        attackCommandsFSM.RemoveAction("Orb Summon", 0);
    }
}