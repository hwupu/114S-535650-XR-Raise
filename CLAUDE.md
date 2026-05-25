# XR-Raise Project — CLAUDE.md

## Project Overview

A narrative XR experience for Meta Quest headsets. The game explores themes of academic pressure, body image, and life choices through three scenes. Built with Unity + Meta XR SDK (OVR).

**Platform**: Meta Quest (OVR SDK)
**Render Pipeline**: Universal Render Pipeline (URP)
**Key Package**: GLTFast (for .glb model loading)

---

## Scene Flow

```
[Opening Call]          OpeningCallSequence.cs
      ↓ (X button answers)
[Scene 1 — House]       scene 1_house.unity / Scene1manage.cs
      ↓ (ceiling collapses, escape hole triggers)
[Scene 2 — Forest]      SceneForest2.unity
      ↓ (all 3 events + fragments collected)
[Scene 3 — Future]      (CS scene OR Music scene, not yet built)
```

---

## Implemented Systems

### 1. SwingLocomotion (`Assets/Scripts/SwingLocomotion.cs`)

Hand-swing based locomotion. Both hands must swing in anti-phase (opposite directions) to trigger movement.

**Normal State Parameters (weight at minimum):**
| Parameter | Value |
|-----------|-------|
| `maxSpeed` | 2.0 m/s |
| `deadzone` | 0.02 |
| `speedSmoothing` | 8 |
| `maxSwingVelocity` | 5 |

**Heavy State Parameters (weight at maximum):**
| Parameter | Value |
|-----------|-------|
| `heavyMaxSpeed` | 0.6 m/s |
| `heavyDeadzone` | 0.5 |

Linear interpolation between light/heavy is done via `SetWeightFactor(float t)` where t = 0 (lightest) → 1 (heaviest).

**Controller Buttons:**
| Button | Action |
|--------|--------|
| Right A (Button One) | Toggle locomotion on/off |

**Keyboard Testing (Editor):**
| Key | Action |
|-----|--------|
| — | No keyboard shortcut; test via Play Mode with OVR emulation |

---

### 2. BodyShapeManager (`Assets/Scripts/BodyShapeManager.cs`)

Singleton. Tracks weight (0–30) and snack count. Drives locomotion speed via `ApplyWeightToLocomotion()`.

- `startWeight` = 10
- `maxWeight` = 30
- Each snack eaten → weight +3, `SnackCount++`
- Every 5 dumbbell reps → weight −1
- Weight change fires `OnWeightChanged` event
- Scene 3 uses `Weight` and `SnackCount` to select fat/thin model appearance

**Keyboard Testing (Editor):**
| Key | Action |
|-----|--------|
| Space | Add +3 weight (simulates eating a snack) |

**Controller Testing:**
| Button | Action |
|--------|--------|
| Right B (Button Two) | Add +3 weight (debug trigger) |

---

### 3. EatableSnack (`Assets/Scripts/EatableSnack.cs`)

Attached to snack prefabs (KuaiKuai, Lays in `Assets/Prefabs/KuaiKuai/`, `Assets/Prefabs/Lays/`).

**Eating mechanic:**
1. Player grabs snack via OVRGrabbable (Rigidbody becomes kinematic)
2. Script checks distance from snack to `OVRCameraRig.centerEyeAnchor`
3. If distance < `eatDistance` (0.25 m) → snack consumed
4. Calls `BodyShapeManager.AddWeight(3)`, snack GameObject destroyed

**Controller Testing:** Grab snack with grip button, bring to face.

---

### 4. DumbbellExercise (`Assets/Scripts/DumbbellExercise.cs`)

Attached to dumbbell prefab (`Assets/Prefabs/Dumbbell/`). Detects bicep curl reps via hand Y-velocity.

**State machine:** Neutral → MovingUp → MovingDown → (rep counted on return up)
- `repsRequired` = 5 reps per set
- `upVelThreshold` = 0.4 m/s
- `downVelThreshold` = 0.4 m/s
- After 5 reps → `BodyShapeManager.AddWeight(-1)`

**Keyboard Testing (Editor):**
| Key | Action |
|-----|--------|
| Space | Add +3 weight (tests weight system, not reps) |

**Controller Testing:**
| Button | Action |
|--------|--------|
| Right B (Button Two) | Add +3 weight (debug) |

To test rep counting: hold both controllers and perform up/down motions in Play Mode.

---

### 5. CatManager (`Assets/Scripts/CatManager.cs`)

Singleton. Manages whether the cat has been saved and controls cat follow behavior + paw prints.

- `CatSaved` (bool) — persists across the scene for Scene 3 reference
- Cat follows player head at offset `(0.3, 0, 0.8)` with `followSpeed` = 2 m/s
- Raycast keeps cat on terrain surface
- Spawns paw print prefab (`Assets/Prefabs/Paw/`) every `pawPrintSpacing` = 0.4 m

**Keyboard Testing (Editor):**
| Key | Action |
|-----|--------|
| C | Force-trigger cat rescue (skips potion interaction) |

---

### 6. InjuredCat (`Assets/Scripts/InjuredCat.cs`)

Attached to injured cat model in Scene 2. Sphere collider set as Trigger.

**Healing mechanic:**
1. Player grabs potion via OVRGrabbable
2. Tilts potion upside-down (`transform.up.y < −0.5`) while held → `PotionPour.IsPoured = true`
3. Tilted potion collider enters InjuredCat trigger zone
4. `Heal()` called → color changes, `CatManager.OnCatSaved()` called

**Keyboard Testing (Editor):**
| Key | Action |
|-----|--------|
| H | Force-heal cat (skips potion) |

---

### 7. PotionPour (`Assets/Scripts/PotionPour.cs`)

Attached to potion bottle. Detects grab + tilt.

- `IsHeld` = `Rigidbody.isKinematic` (set true by OVRGrabbable)
- `IsPoured` = IsHeld AND `transform.up.y < tiltThreshold` (−0.5)

**Keyboard Testing (Editor):**
| Key | Action |
|-----|--------|
| P | Toggle force-pour state |

---

### 8. Scene1manage (`Assets/Scripts/Scene1manage.cs`)

Controls narrative phases in the house scene.

| Phase | Ceiling Sink Speed | Light Flicker Speed | Text Force |
|-------|--------------------|---------------------|------------|
| Phase 1 Oppressive | 0.05 | 0.8 s | 4 |
| Phase 2 Panic | 0.25 | 0.15 s | 10 |
| Phase 3 Escape | — ceiling at y ≤ 1.2 → escape hole | — | — |

**Keyboard Testing (Editor):** Check `Scene1manage.cs` for any keyboard debug triggers (`KeyCode` references). Otherwise trigger via `StartMotherCalling()` in inspector or debug button.

---

### 9. OpeningCallSequence (`Assets/Scripts/OpeningCallSequence.cs`)

Opening cinematic sequence.

| State | Trigger |
|-------|---------|
| Ringing → Answered | Press X button (left controller) |
| Answered → Fading | Auto after voice clip ends |
| Fading → Scene Load | `SceneManager.LoadScene("scene 1_house")` |

**Controller Testing:**
| Button | Action |
|--------|--------|
| Left X | Answer the phone call |

---

## Systems To Build (Scene 2 — SceneForest2)

### GameManager (new script needed: `Assets/Scripts/GameManager.cs`)

Singleton. Records all three event choices in Scene 2 and persists results to Scene 3.

**Data to track:**
```csharp
bool choseCSDepartment;       // true = CS, false = Music
bool catWasSaved;             // from CatManager.CatSaved
bool choseSafe;               // true = 保險箱 more coins, false = 小豬撲滿 more coins
int fragmentsUnlocked;        // 0–3
int safeCoins;
int piggyCoins;
```

**Scene 3 branch logic (wire up in GameManager):**
- CS department chosen → load CS scene
- Music department chosen → load Music scene
- Safe has more coins → spawn stock 模型
- Piggy bank has more coins → spawn Switch/PS5 模型
- `BodyShapeManager.Weight` / `BodyShapeManager.SnackCount` → select fat/thin model appearance

---

### Event Timer System (new script: `Assets/Scripts/EventTimer.cs`)

Each of the 3 events has a 3-minute countdown.

**Requirements:**
- Shared countdown timer (3 min) starts when Scene 2 begins
- Countdown display in UI (TextMeshPro canvas)
- At 30 seconds remaining → play a voice reminder audio clip
- Timer ends → events are locked, player must return to Scene 1 room

**Keyboard Testing (planned):**
| Key | Action |
|-----|--------|
| T | Skip to 35 seconds remaining (test 30s warning) |
| Y | Force timer expiry |

---

### Application Fragments System (new script: `Assets/Scripts/FragmentManager.cs`)

Three fragments form 備審資料 (application portfolio) inside the Scene 1 room.

**Behavior:**
- All 3 fragments hidden (invisible/disabled) at start
- Completing any one of the 3 events → one fragment appears with sound effect
- Total 3 fragments; order tied to event order (Event 1 → Fragment 1, etc.)
- Fragment prefabs live in `Assets/Prefabs/Admission/`

**Keyboard Testing (planned):**
| Key | Action |
|-----|--------|
| 1 | Reveal fragment 1 |
| 2 | Reveal fragment 2 |
| 3 | Reveal fragment 3 |

---

### Event 1: Department Selection — CS vs Music

**Models:** Two admission letter (錄取通知書) models in Scene 2.

**Interaction flow:**
1. Player grabs either letter via OVRGrabbable
2. Grabbed letter glows (emission material or outline effect)
3. Corresponding sound effect plays
4. Other letter dims / floats away
5. `GameManager.choseCSDepartment` set accordingly
6. Fragment 1 revealed

**New script needed:** `AdmissionLetterChoice.cs`

**Keyboard Testing (planned):**
| Key | Action |
|-----|--------|
| C | Force-choose CS department |
| M | Force-choose Music department |

---

### Event 2: Cat Rescue (already partially implemented)

Uses existing `CatManager`, `InjuredCat`, `PotionPour`.

**GameManager hook:**
- After `CatManager.OnCatSaved()` fires → `GameManager.catWasSaved = true`, reveal Fragment 2
- If timer expires without saving cat → `GameManager.catWasSaved = false`, Fragment 2 still revealed (event completed by timeout/skip)

**Keyboard Testing:**
| Key | Action |
|-----|--------|
| H | Force-heal cat (InjuredCat.cs debug trigger) |
| C | Force cat rescue (CatManager.cs debug trigger) |

---

### Event 3: Money Tree — Safe vs Piggy Bank

**Scene:** A money tree with gold coins. Player grabs coins and places them into either 保險箱 (safe) or 小豬撲滿 (piggy bank).

**Models/Prefabs needed:**
- `Assets/Prefabs/Coin/` — coin prefabs (already exist)
- `Assets/Prefabs/Vault/` — safe model (already exists)
- Piggy bank model (needs to be added)
- Money tree model

**Interaction flow:**
1. Grab coin from tree via OVRGrabbable
2. Drop coin into safe or piggy bank trigger zone
3. Counter increments (`safeCoins++` or `piggyCoins++`)
4. When player leaves interaction area (or event timer ends):
   - Compare `safeCoins` vs `piggyCoins` → set `GameManager.choseSafe`
   - Reveal Fragment 3

**New scripts needed:** `CoinDeposit.cs` (or add logic to GameManager)

**Keyboard Testing (planned):**
| Key | Action |
|-----|--------|
| S | Add 1 coin to safe (debug) |
| P | Add 1 coin to piggy bank (debug) |
| F | Force finalize money event |

---

## Scene 3 — Future (not yet built)

Scene 3 reads from `GameManager` (singleton or static) and `BodyShapeManager`:

| Condition | Result |
|-----------|--------|
| `choseCSDepartment == true` | Load CS-themed scene |
| `choseCSDepartment == false` | Load Music-themed scene |
| `safeCoins > piggyCoins` | Spawn stock (股票) model |
| `piggyCoins >= safeCoins` | Spawn Switch + PS5 models |
| High `Weight` / High `SnackCount` | Show fat body model |
| Low `Weight` / Low `SnackCount` | Show thin body model |
| `CatManager.CatSaved == true` | Cat appears in Scene 3 with player |

---

## File Map

```
Assets/
├── Scripts/
│   ├── SwingLocomotion.cs          ← locomotion (weight-based speed)
│   ├── BodyShapeManager.cs         ← weight/snack counter (singleton)
│   ├── EatableSnack.cs             ← snack grab→eat interaction
│   ├── DumbbellExercise.cs         ← rep counting → weight loss
│   ├── CatManager.cs               ← cat follow + paw prints (singleton)
│   ├── InjuredCat.cs               ← potion trigger → heal
│   ├── PotionPour.cs               ← tilt detection for potion
│   ├── Scene1manage.cs             ← house narrative phases
│   ├── OpeningCallSequence.cs      ← opening phone call cinematic
│   ├── GameManager.cs              ← [TO BUILD] choice tracking singleton
│   ├── EventTimer.cs               ← [TO BUILD] 3-min countdown per event
│   ├── FragmentManager.cs          ← [TO BUILD] 備審碎片 reveal logic
│   └── AdmissionLetterChoice.cs    ← [TO BUILD] CS/Music grab choice
├── Prefabs/
│   ├── KuaiKuai/                   ← snack prefab (has EatableSnack.cs)
│   ├── Lays/                       ← snack prefab (has EatableSnack.cs)
│   ├── Dumbbell/                   ← dumbbell prefab (has DumbbellExercise.cs)
│   ├── Coin/                       ← coin prefabs for money tree event
│   ├── Vault/                      ← safe model for money event
│   ├── Animals/                    ← cat model + animations
│   ├── Paw/                        ← paw print prefab
│   └── Admission/                  ← admission letter models + fragment models
├── Scenes/
│   ├── scene 1_house.unity         ← Scene 1 (house/mother)
│   ├── SceneForest2.unity          ← Scene 2 (forest events)
│   └── [Scene 3 CS/Music]          ← [TO BUILD]
└── scene1_prefab/                  ← GLB models for house scene
```

---

## Full Controller Button Reference

| Button | System | Action |
|--------|--------|--------|
| Right A (Button One) | SwingLocomotion | Toggle locomotion on/off |
| Right B (Button Two) | DumbbellExercise / BodyShape | Debug: add +3 weight |
| Left X | OpeningCallSequence | Answer phone call |
| Grip (either hand) | OVRGrabbable | Grab objects (snacks, potion, letters, coins) |

---

## Full Keyboard Debug Reference (Editor Play Mode)

| Key | System | Action |
|-----|--------|--------|
| Space | BodyShapeManager (via DumbbellExercise) | Add +3 weight |
| C | CatManager | Force cat rescue (skip potion) |
| H | InjuredCat | Force heal cat |
| P | PotionPour | Toggle force-pour state |
| 1 | FragmentManager [TO BUILD] | Reveal fragment 1 |
| 2 | FragmentManager [TO BUILD] | Reveal fragment 2 |
| 3 | FragmentManager [TO BUILD] | Reveal fragment 3 |
| C | AdmissionLetterChoice [TO BUILD] | Force choose CS |
| M | AdmissionLetterChoice [TO BUILD] | Force choose Music |
| S | CoinDeposit [TO BUILD] | Add coin to safe |
| P | CoinDeposit [TO BUILD] | Add coin to piggy bank |
| T | EventTimer [TO BUILD] | Skip to 35 s remaining |
| Y | EventTimer [TO BUILD] | Force timer expiry |

---

## Architecture Principles

- **Singletons**: `BodyShapeManager.Instance`, `CatManager.Instance`, `GameManager.Instance` (to build). Do not use `FindObjectOfType` in Update loops.
- **OVRGrabbable pattern**: Grab state is detected via `Rigidbody.isKinematic` being set by OVRGrabbable. Check this flag in Update.
- **Weight factor**: Always normalize weight to 0–1 before passing to locomotion: `t = (Weight - startWeight) / (maxWeight - startWeight)`.
- **Scene persistence**: Use `DontDestroyOnLoad` on `GameManager` and `BodyShapeManager` so data survives scene loads.
- **Events fire one-time**: Use a `_done` / `_healed` flag guard in interaction scripts to prevent double-triggering.
