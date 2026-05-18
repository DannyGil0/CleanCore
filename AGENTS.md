# AGENTS.md — VR House Cleaning Simulator

## Project Overview

Unity VR project: a house cleaning simulator where the player cleans dirt off surfaces using VR controllers (or mouse). Built on **URP** (Universal Render Pipeline) with **XR Interaction Toolkit**.

There are two main scenes:
- **`Assets/Scenes/SampleScene.unity`** — Original working demo with a SawMill model (~22 paintable surfaces). Reference implementation.
- **`Assets/StylArts/StylizedHouseInterior/Scene/URP_Stylized_House_Interior.unity`** — The target scene. A full stylized house interior (StylArts asset pack) with ~2524 MeshRenderers, adapted to use the cleaning system.

## Core Cleaning System Architecture

### How Dirt Works (Shader Level)

The `Custom/Uber Shader` (`Assets/Shaders/Uber Shader.shader`) is a modified URP Lit shader with two extra properties:

```hlsl
_DirtTex("Dirt Texture", 2D)   // What dirt looks like (brownish procedural texture)
_DirtMask("Dirt Mask", 2D)     // Per-surface RenderTexture: white=dirty, black=clean
```

In the fragment shader:
```hlsl
half4 dirt = SampleTexture3D(triplanarUV, _DirtTex, sampler_DirtTex);  // triplanar projected
half dirtMask = SAMPLE_TEXTURE2D(_DirtMask, sampler_DirtMask, lightmapUV).r;  // uses UV1
albedoAlpha = lerp(albedoAlpha, dirt, dirtMask);  // blend
```

Key points:
- `_DirtMask` defaults to **white** → surfaces start dirty
- `_DirtMask` is sampled using **UV1** (lightmap UVs / `staticLightmapUV`), NOT UV0
- `_DirtTex` uses **triplanar projection** (world space) so it doesn't need UV mapping
- When player cleans: `_DirtMask` gets painted **black** at the hit point → reveals albedo underneath

### Paint.shader (`Assets/Shaders/Paint.shader`)

Internal shader used by `PaintableSurface` to render paint strokes onto the `_DirtMask` RenderTexture. Maps paint position using UV1.

### ParallelReduce.compute (`Assets/Shaders/ParallelReduce.compute`)

Compute shader that calculates the overall dirtiness percentage of a surface by reducing the `_DirtMask` texture.

### PaintableSurface.cs (`Assets/Scripts/Powerwash Simulator/PaintableSurface.cs`)

Core component attached to each cleanable GameObject. On `Start()`:
1. Creates a `RenderTexture` of size `TextureSize` (enum: XS=64..XXL=2048)
2. Initializes RT to **white** (all dirty): `GL.Clear(true, true, new Color(1,1,1,0))`
3. Clones the renderer's materials and sets `_DirtMask = RT` on each clone
4. Sets up a `CommandBuffer` for painting strokes
5. Uses compute shader to measure coverage (dirtiness %)

Required serialized fields:
- `_transform` — the object's Transform
- `_mainRenderer` — the MeshRenderer
- `PaintShader` — reference to `Paint.shader`
- `CoverageShader` — reference to `ParallelReduce.compute`
- `TextureSize` — RT resolution (use `M`=256 for large scenes, `L`=512 for small ones)

Validated by `KBCore.Refs` (`[Self]`, `[Child]` attributes) — will throw errors in console if references are null.

### Painter.cs (`Assets/Scripts/Powerwash Simulator/Painter.cs`)

Handles user input. Raycasts from VR controllers (or camera for desktop):
- **fire1** (left click / trigger) → paints **black** (cleans)
- **fire2** (right click / grip) → paints **white** (dirties)

Requires target objects on **Layer 6** ("Environment") and with a **Collider** (MeshCollider).

### PaintableSurfaceUI.cs (`Assets/Scripts/Powerwash Simulator/PaintableSurfaceUI.cs`)

Displays cleaning progress. Has `_surfaces` array (serialized) that must contain ALL `PaintableSurface` instances in the scene. Connected via editor script (Fase 4).

## Editor Setup Script

### `Assets/Editor/HouseCleaningSetup.cs`

Menu: **Tools > House Cleaning Setup**

Automates the entire process of converting the StylArts house scene to use the cleaning system. Has these phases:

| Menu Item | What It Does |
|---|---|
| **0 - Diagnostico** | Reports current state: UV1 status, shader counts, PaintableSurface count, materials with null _BaseMap |
| **1 - Enable UV1** | Enables `generateSecondaryUV` on all FBX models in `Art/Meshes/`. Required for `_DirtMask` sampling via UV1 |
| **2-3 - Switch Shaders + Setup** | Clones materials to Uber Shader, adds PaintableSurface/MeshCollider/Layer 6. Creates materials in `Assets/HouseUberMaterials/` |
| **4 - Connect UI** | Populates `PaintableSurfaceUI._surfaces` array with all PaintableSurface instances |
| **5 - Fix Selected** | Processes selected hierarchy objects bypassing size/shader filters. For walls or objects missed by auto-filter |
| **RESET** | Reverts cloned materials to originals, removes PaintableSurface, deletes `HouseUberMaterials/` |

Execution order: **1 → 2-3 → 4** (run 5 only for specific missed objects, then re-run 4).

## Material Architecture in the House Scene

### Two Material Folders (Important!)

The StylArts pack has **two separate material folders**:

1. **`Assets/StylArts/StylizedHouseInterior/Art/Materials/`** (105 files, prefix `M_`)
   - **These are the ones the scene renderers actually use**
   - Original ShaderGraph shaders: `S_PBR_OPAQUE_ORM_LeartesMasterMaterial`, `S_Kitchen_Master`
   - Have correct texture references

2. **`Assets/StylArts/StylizedHouseInterior/Art/Meshes/Materials/`** (193 files, prefix `MI_`/`MM_`)
   - Extracted from FBX imports, mostly **NOT used** by scene renderers
   - Were accidentally converted to Uber Shader in an early attempt, losing their textures
   - Can be safely ignored

### ShaderGraph Property Names

The original ShaderGraph shaders use **auto-generated property names**, not standard ones:

**`S_PBR_OPAQUE_ORM_LeartesMasterMaterial`:**
| Display Name | Reference Name |
|---|---|
| Material 01 Base Colour | `Texture2D_2A1A8040` |
| Material 01 Normal | `Texture2D_7EA392D1` |
| Material 01 ORM | `Texture2D_B955310A` |

**`S_Kitchen_Master`:**
| Display Name | Reference Name |
|---|---|
| BaseColor | `Texture2D_296162aee2a745d3b57152085d1d03fd` |
| Normal | `Texture2D_eb55fdf554d54ac0bf51809f94be4d07` |

The setup script handles this via `Shader.GetPropertyCount()`/`GetPropertyName()`/`GetPropertyDescription()` dynamic enumeration, with hardcoded fallbacks for these specific names.

### Cloned Materials

Created by Fase 2-3 in `Assets/HouseUberMaterials/`. Named `OriginalName_Uber.mat`. These are standalone `.mat` files using `Custom/Uber Shader` with:
- `_BaseMap` / `_MainTex` — copied from original albedo
- `_BumpMap` — copied from original normal
- `_DirtTex` — procedurally generated dirt texture (`Assets/Textures/GeneratedDirtTex.png`)
- `_DirtMask` — defaults to white (set at runtime by PaintableSurface)

### Materials with No Texture (Solid Color)

Some materials (e.g., `M_Base_Mat_2`) are solid-color with no albedo texture. They originally used an HDRP shader (missing in URP), falling back to `URP/Lit`. The color is stored in `_Color` (HDRP convention), not `_BaseColor` (URP convention). The script handles this via fallback color reading.

## Filtering Logic

Fase 2-3 skips renderers that:
- Have no `MeshFilter` or no mesh
- Use only skip-listed shaders: `S_Glass`, `S_Sky`, `S_PBR_TRANSPARENT_ORM`, `S_VertexPaintGround`, `URP/Unlit`, skybox, UI
- Have bounds smaller than `0.25m` (filters tiny props to save VRAM)

After Fase 2-3: ~1668 PaintableSurface, ~856 skipped, ~81 unique cloned materials.
Each PaintableSurface uses a 256x256 RenderTexture (~437 MB total VRAM for dirt masks).

## Problems Encountered & Solutions

### 1. MCP Unity Connection Failures
**Problem:** `Unity_RunCommand` via MCP kept returning "Connection revoked" (Unity bug UUM-141533).
**Solution:** Switched from MCP commands to standard Unity Editor scripts (`.cs` in `Assets/Editor/`) with `[MenuItem]` functions the user runs from Unity's menu.

### 2. Materials Not Switching (0 PaintableSurface)
**Problem:** Fase 2 changed 88 `.mat` files in `Art/Meshes/Materials/`, but scene renderers used different materials from `Art/Materials/`. Fase 3 found 0 renderers with Uber Shader.
**Solution:** Combined Fases 2+3. Now iterates scene renderers directly, clones their actual materials (from `Art/Materials/`), and saves clones to `Assets/HouseUberMaterials/`.

### 3. White Textures When Cleaning
**Problem:** Cloned materials had `_BaseMap = null` because `CopyKnownProperties` looked for `_BaseMap`/`_MainTex`, but ShaderGraph materials use auto-generated names like `Texture2D_2A1A8040`.
**Solution:** Rewrote to `CopyTexturesFromSource` using `Shader.GetPropertyCount()` to dynamically enumerate source shader properties and map them by display name. Added hardcoded fallbacks for known ShaderGraph property names.

### 4. Broken Materials from Early Attempts
**Problem:** 88 materials in `Art/Meshes/Materials/` were converted to Uber Shader, destroying their original texture references (Unity strips properties not in the new shader).
**Solution:** Added name-based fallback: when cloning a broken Uber material, strips `MI_`/`MM_` prefix → tries `M_` prefix → finds original in `Art/Materials/`.

### 5. HDRP Solid-Color Materials
**Problem:** Some materials (e.g., `M_Base_Mat_2`) used a missing HDRP shader. Their color was stored as `_Color`, not `_BaseColor`. Dynamic enumeration on the fallback URP/Lit shader returned wrong defaults.
**Solution:** Added explicit `_Color` fallback reading in `CopyTexturesFromSource`.

### 6. Missing Walls
**Problem:** Specific walls (e.g., `House_Walls_SM_14`) were skipped by auto-filters.
**Solution:** Added "5 - Fix Selected Objects" menu item that processes selected hierarchy objects bypassing all filters.

## VR In-World Menu

World Space settings menu for VR (not camera-locked). Setup via **Tools → VR Menu** (see [`Docs/VR_MENU_SETUP.md`](Docs/VR_MENU_SETUP.md)).

| Path | Role |
|------|------|
| `Assets/Scripts/VRMenu/InWorldMenuVR.cs` | Menu logic, modals, PlayerPrefs |
| `Assets/Scripts/VRMenu/CleaningStatsAggregator.cs` | Global cleanliness % from `PaintableSurfaceUI.Surfaces` |
| `Assets/Prefabs/UI/InWorldMenuVR.prefab` | Generated by `InWorldMenuVRBuilder` |
| `Assets/Audio/MainMixer.mixer` | `VolAmbiente` / `VolMusica` exposed parameters |

- Placed once at `(0, 1.4, 2)` relative to `XROrigin` via `InWorldMenuPlacement`
- Uses `XRUIInputModule` + `TrackedDeviceGraphicRaycaster` (XRI 3.1.3)
- Does **not** replace per-surface `PaintableSurfaceUI` list — both UIs coexist
- Scene hook: **Tools → VR Menu → Add To House Interior Scene**

## Key Constraints

- **UV1 is mandatory** — `_DirtMask` samples via `staticLightmapUV` (UV1). Without `generateSecondaryUV = true` on FBX imports, painting won't map correctly.
- **Layer 6 required** — `Painter.cs` raycasts only against this layer.
- **MeshCollider required** — raycasts need colliders to hit surfaces.
- **Static batching must be OFF** — batched meshes can't have individual `_DirtMask` RTs.
- **KBCore.Refs validation** — `PaintableSurfaceUI._surfaces` array must not be empty or the console will log errors (cosmetic but distracting).
