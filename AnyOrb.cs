using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using SFCore.Utils;


namespace AbsRadAnyOrb {
    public class AnyOrb : MonoBehaviour {
        private PlayMakerFSM attackCommandsFSM;
        private PlayMakerFSM controlFSM;
        private PlayMakerFSM attackChoicesFSM;
        private static GameObject[] orbs;
        private static readonly int NUM_ORBS = 1250;
        private int spawningIdx = 0;
        private GameObject currentOrb;
        private static GameObject orbPrefab;
        private GameObject knight;
        private static GameObject eyeBeamGlow = null;
        private HashSet<GameObject> orbRainOrbs = new();
        private HashSet<GameObject> climbOrbs = new();

        public void Awake() {
            attackCommandsFSM = gameObject.LocateMyFSM("Attack Commands");
            controlFSM = gameObject.LocateMyFSM("Control");
            attackChoicesFSM = gameObject.LocateMyFSM("Attack Choices");
            knight = GameObject.Find("Knight");
        }

        public void Start() {
            StartCoroutine(AddExtraOrbSpawns());
            currentOrb = orbs[spawningIdx];

            // Allow only orb attacks
            SendRandomEventV3 a1Choice = attackChoicesFSM.GetAction<SendRandomEventV3>("A1 Choice", 1);
            SendRandomEventV3 a2Choice = attackChoicesFSM.GetAction<SendRandomEventV3>("A2 Choice", 1);
            a1Choice.weights = new FsmFloat[]{0, 0, 0, 0, 0, 0, 0, 1};
            a1Choice.eventMax = new FsmInt[]{10000, 10000, 10000, 10000, 10000, 10000, 10000, 10000};
            a1Choice.missedMax = new FsmInt[]{10000, 10000, 10000, 10000, 10000, 10000, 10000, 10000};
            a2Choice.weights = new FsmFloat[]{0, 0, 1, 0, 0, 0};
            a2Choice.eventMax = new FsmInt[]{10000, 10000, 10000, 10000, 10000, 10000};
            a2Choice.missedMax = new FsmInt[]{10000, 10000, 10000, 10000, 10000, 10000};

            // Configure orb attack
            attackCommandsFSM.RemoveAction("Orb Summon", 5);
            attackCommandsFSM.RemoveAction("Orb Summon", 4);
            attackCommandsFSM.RemoveAction("Spawn Fireball", 1);

            // Orb rain
            controlFSM.GetAction<Wait>("Rage Comb", 0).time = 0.4f;
            controlFSM.RemoveAction("Rage Comb", 2);
            controlFSM.RemoveAction("Rage Comb", 1);
            controlFSM.AddAction("Rage Comb", new CallMethod {
                behaviour = this,
                methodName = "SpawnOrbRainWave",
                parameters = new FsmVar[0],
                everyFrame = false
            });
            controlFSM.AddAction("Rage Comb", attackCommandsFSM.GetAction<AudioPlaySimple>("Spawn Fireball", 3));
            controlFSM.AddAction("Stun1 Start", new CallMethod {
                behaviour = this,
                methodName = "DespawnAllOrbs",
                parameters = new FsmVar[0],
                everyFrame = false
            });

            // Climb orb attack
            attackCommandsFSM.GetAction<RandomFloat>("Aim", 4).min = -3.5f;
            attackCommandsFSM.GetAction<RandomFloat>("Aim", 4).max = 3.5f;
            attackCommandsFSM.GetAction<Wait>("Aim", 11).time = 0.20f;
            attackCommandsFSM.RemoveAction("Aim", 10);
            attackCommandsFSM.RemoveAction("Aim", 9);
            attackCommandsFSM.RemoveAction("Aim", 3);                                                                                                               
            attackCommandsFSM.RemoveAction("Aim", 1);
            attackCommandsFSM.AddAction("Aim Back", attackCommandsFSM.GetAction<AudioPlaySimple>("Spawn Fireball", 3));
            attackCommandsFSM.AddAction("Aim Back", new CallMethod {
                behaviour = this,
                methodName = "FireOrb",
                parameters = new FsmVar[0],
                everyFrame = false
            });
            attackCommandsFSM.AddAction("Aim Back", new Wait {
                time = 0.20f,
                finishEvent = new FsmEvent("FINISHED"),
                realTime = false
            });
            controlFSM.AddAction("Scream", new CallMethod {
                behaviour = this,
                methodName = "DespawnAllOrbs",
                parameters = new FsmVar[0],
                everyFrame = false
            });
        }

        private void Update() {
            // Bit of a performance hit here, but I'm too lazy to figure out why the Final Control FSM is not doing its job
            PlayMakerFSM finalControl = orbPrefab.LocateMyFSM("Final Control");
            float minX = finalControl.FsmVariables.GetFsmFloat("Min X").Value;
            float maxX = finalControl.FsmVariables.GetFsmFloat("Max X").Value;
            float minY = finalControl.FsmVariables.GetFsmFloat("Min Y").Value;
            float maxY = finalControl.FsmVariables.GetFsmFloat("Max Y").Value;
            if (attackCommandsFSM.FsmVariables.GetFsmFloat("Orb Min Y").Value > 150f) {
                climbOrbs = new HashSet<GameObject>();
                foreach(GameObject orb in orbs) {
                    if (orb.activeSelf) {
                        if (orb.transform.position.x < minX ||
                            orb.transform.position.x > maxX ||
                            orb.transform.position.y < minY ||
                            orb.transform.position.y > maxY
                        ) {
                            orb.SetActive(false);
                        }
                    }
                }
            }

            foreach(GameObject orb in orbRainOrbs) {
                if (orb.transform.position.y < 0) {
                    orb.SetActive(false);
                } else {
                    orb.transform.Translate(Vector3.down * 15 * Time.deltaTime);
                }
            }
            orbRainOrbs.RemoveWhere(orb => orb.activeSelf == false);

            foreach(GameObject orb in climbOrbs) {
                orb.transform.Translate(orb.transform.right * 60 * Time.deltaTime);
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
            Vector3 radPos = gameObject.transform.position;
            Vector3 innerCircleStartingPos = gameObject.transform.position + new Vector3(0, -minDist, 0);
            Vector3 outerCircleStartingPos = gameObject.transform.position + new Vector3(0, -maxDist, 0);

            // Spawn along top of range
            for (float x = orbMinX; x < orbMaxX; x += 2f) {
                float distance = Vector2.Distance(new Vector2(x, orbMaxY), new Vector2(base.gameObject.transform.position.x, base.gameObject.transform.position.y));
                if (distance >= minDist && distance <= maxDist) {
                    SpawnOrb(x, orbMaxY);
                }
            }

            // Spawn along bottom of range
            for (float x = orbMinX; x < orbMaxX; x += 2f) {
                float distance = Vector2.Distance(new Vector2(x, orbMinY), new Vector2(base.gameObject.transform.position.x, base.gameObject.transform.position.y));
                if (distance >= minDist && distance <= maxDist) {
                    SpawnOrb(x, orbMinY);
                }
            }

            // Spawn along left side of range
            for (float y = orbMinY; y < orbMaxY; y += 2f) {
                float distance = Vector2.Distance(new Vector2(orbMinX, y), new Vector2(base.gameObject.transform.position.x, base.gameObject.transform.position.y));
                if (distance >= minDist && distance <= maxDist) {
                    SpawnOrb(orbMinX, y);
                }
            }

            // Spawn along right side of range
            for (float y = orbMinY; y < orbMaxY; y += 2f) {
                float distance = Vector2.Distance(new Vector2(orbMaxX, y), new Vector2(base.gameObject.transform.position.x, base.gameObject.transform.position.y));
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
            if (orbMinY > 150f) {
                // Final phase
                for (float x = orbMinX + 1; x <= orbMaxX - 1; x += 2f) {
                    float distance = Vector2.Distance(new Vector2(x, 157), new Vector2(gameObject.transform.position.x, gameObject.transform.position.y));
                    if (distance < attackCommandsFSM.GetAction<FloatCompare>("Orb Pos", 6).float2.Value ||
                        distance > attackCommandsFSM.GetAction<FloatCompare>("Orb Pos", 7).float2.Value) {
                        continue;
                    }

                    SpawnOrb(x, 157);
                }
            } else {
                for (float x = orbMinX + 1; x <= orbMaxX - 1; x += 2f) {
                    for (float y = orbMinY + 1; y <= orbMaxY - 1; y += 2f) {
                        float distance = Vector2.Distance(new Vector2(x, y), new Vector2(gameObject.transform.position.x, gameObject.transform.position.y));
                        if (distance < attackCommandsFSM.GetAction<FloatCompare>("Orb Pos", 6).float2.Value ||
                            distance > attackCommandsFSM.GetAction<FloatCompare>("Orb Pos", 7).float2.Value) {
                            continue;
                        }

                        SpawnOrb(x, y);
                    }
                }
            }
        }

        private void SpawnOrb(float x, float y) {
            currentOrb.transform.SetPosition2D(x, y);
            currentOrb.GetComponent<Rigidbody2D>().isKinematic = true;
            currentOrb.SetActive(true);
            currentOrb.LocateMyFSM("Orb Control").SendEvent("FIRE");
            IncrementSpawningOrb();
        }

        public void SpawnOrbRainWave() {
            float x = 40;
            for (int _ = 0; _ < 20; _++) {
                x += UnityEngine.Random.Range(2.5f, 4.0f);
                currentOrb.transform.SetPosition2D(x, 35f);
                currentOrb.transform.GetComponent<Rigidbody2D>().isKinematic = true;
                currentOrb.SetActive(true);
                orbRainOrbs.Add(currentOrb);
                IncrementSpawningOrb();
            }
        }

        public void FireOrb() {
            float rotation = attackCommandsFSM.FsmVariables.GetFsmFloat("Rotation").Value;
            if (eyeBeamGlow == null) {
                eyeBeamGlow = GameObject.Find("Eye Beam Glow");
            }

            currentOrb.GetComponent<Rigidbody2D>().isKinematic = true;
            currentOrb.transform.SetPosition2D(eyeBeamGlow.transform.position.x, eyeBeamGlow.transform.position.y);

            // Rotate orb to face along beam indicator
            currentOrb.transform.eulerAngles = new Vector3(0, 0, rotation / 2); // I have no idea why dividing by 2 is necessary here

            // Move orb closer to player when spawning
            if (eyeBeamGlow.transform.position.y - knight.transform.position.y > 15f) {
                currentOrb.transform.Translate(new Vector3(Mathf.Tan((rotation - 270) * Mathf.Deg2Rad) * (eyeBeamGlow.transform.position.y - knight.transform.position.y - 15), -(eyeBeamGlow.transform.position.y - knight.transform.position.y - 15), 0), Space.World);
            }

            currentOrb.GetComponent<Rigidbody2D>().isKinematic = false;
            currentOrb.SetActive(true);
            climbOrbs.Add(currentOrb);
            IncrementSpawningOrb();

            StartCoroutine(RemoveFiredOrb(currentOrb));
        }

        public IEnumerator RemoveFiredOrb(GameObject orb) {
            yield return DespawnOrb(orb);
            climbOrbs.Remove(orb);
        }

        public IEnumerator DespawnOrb(GameObject orb) {
            yield return new WaitForSeconds(2);
            orb.SetActive(false);
        }

        public void DespawnAllOrbs() {
            foreach(GameObject orb in orbRainOrbs) {
                orb.SetActive(false);
            }
            foreach(GameObject orb in climbOrbs) {
                orb.SetActive(false);
            }
            orbRainOrbs = new HashSet<GameObject>();
            climbOrbs = new HashSet<GameObject>();
        }

        public void IncrementSpawningOrb() {
            spawningIdx = (spawningIdx + 1) % NUM_ORBS;
            currentOrb = orbs[spawningIdx];
        }
        
        public static void InstantiateOrbs(GameObject prefab) {
            orbs = new GameObject[NUM_ORBS];
            orbPrefab = prefab;
            
            for(int i = 0; i < NUM_ORBS; i++) {
                if (!orbs[i]) {
                    GameObject orb = Instantiate(prefab);
                    MeshRenderer orbMeshRenderer = orb.GetComponent<MeshRenderer>();
                    PlayMakerFSM orbControlFSM = orb.LocateMyFSM("Orb Control");

                    orb.SetActive(false);

                    orbMeshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                    orbMeshRenderer.receiveShadows = false;
                    orbMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    orbMeshRenderer.rayTracingMode = UnityEngine.Experimental.Rendering.RayTracingMode.Off;

                    orbControlFSM.RemoveAction("Init", 9);
                    orbControlFSM.RemoveAction("Init", 8);
                    orbControlFSM.RemoveAction("Init", 7);
                    orbControlFSM.RemoveAction("Init", 6);
                    orbControlFSM.RemoveAction("Init", 3);
                    orbControlFSM.RemoveAction("Init", 0);

                    orbControlFSM.RemoveAction("Dissipate", 4);
                    orbControlFSM.RemoveAction("Dissipate", 0);

                    orbControlFSM.RemoveAction("Impact", 11);
                    orbControlFSM.RemoveAction("Impact", 10);
                    orbControlFSM.RemoveAction("Impact", 8);
                    // orbControlFSM.RemoveAction("Impact", 7);
                    orbControlFSM.RemoveAction("Impact", 6);
                    orbControlFSM.RemoveAction("Impact", 5);
                    orbControlFSM.RemoveAction("Impact", 4);
                    orbControlFSM.RemoveAction("Impact", 1);
                    orbControlFSM.RemoveAction("Impact", 0);
                    orbControlFSM.ChangeTransition("Impact", "FINISHED", "Init");

                    // orbControlFSM.RemoveAction("Stop Particles", 1);
                    orbControlFSM.RemoveAction("Stop Particles", 0);
                    orbControlFSM.ChangeTransition("Stop Particles", "FINISHED", "Init");
                    orbControlFSM.RemoveFsmState("Recycle");
                    orb.LocateMyFSM("Final Control").RemoveFsmState("Recycle");

                    for (int j = orb.transform.childCount - 1; j >= 0; j--) {
                        GameObject child = orb.transform.GetChild(j).gameObject;
                        // if (child.name == "Appear Glow" ||
                            // child.name == "Fader Old" ||
                            // child.name == "Fader" ||

                        if (child.name == "Particle System" ||
                            child.name == "Impact" ||
                            child.name == "Impact Particles") {
                            DestroyImmediate(child);
                        }
                    }

                    FsmOwnerDefault ownerDefault = new() {
                        OwnerOption = OwnerDefaultOption.UseOwner
                    };
                    orbControlFSM.AddAction("Impact", new ActivateGameObject {
                        gameObject = ownerDefault,
                        activate = false,
                        recursive = false,
                        resetOnExit = false,
                        everyFrame = false
                    });
                    orbControlFSM.AddAction("Stop Particles", new ActivateGameObject {
                        gameObject = ownerDefault,
                        activate = false,
                        recursive = false,
                        resetOnExit = false,
                        everyFrame = false
                    });

                    DestroyImmediate(orb.GetComponent<AudioSource>());
                    DontDestroyOnLoad(orb);

                    orbs[i] = orb;

                    // Fire orb once so one-time effects don't appear in the Absolute Radiance fight
                    try {
                        orb.SetActive(true);
                        orbControlFSM.SendEvent("FIRE");
                    } catch (NullReferenceException) {
                        // Cannot chase player because player does not exist. Ignore
                    }
                }
            }
        }

        public static void UnloadScene() {
            orbs.ToList().ForEach(orb => orb?.SetActive(false));
        }
    }
}