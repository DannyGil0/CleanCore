# VR In-World Menu — Setup Guide

Menú de configuración **World Space** para el simulador de limpieza VR. Usa **XR Interaction Toolkit 3.1.3** (compatible con flujos de XRI 2.x: `XRUIInputModule`, `TrackedDeviceGraphicRaycaster`).

## Requisitos

- Unity con URP y OpenXR
- Paquete `com.unity.xr.interaction.toolkit` 3.1.3+
- TextMeshPro importado
- Escena con `XR Origin (XR Rig)` y `PaintableSurfaceUI` configurado (Fase 4 de House Cleaning Setup)

## Archivos del sistema

| Ruta | Rol |
|------|-----|
| `Assets/Scripts/VRMenu/InWorldMenuVR.cs` | Lógica del menú, modales, PlayerPrefs |
| `Assets/Scripts/VRMenu/InWorldMenuPlacement.cs` | Posición `(0, 1.4, 2)` vs XR Origin al iniciar |
| `Assets/Scripts/VRMenu/AudioManager.cs` | Volúmenes vía AudioMixer |
| `Assets/Scripts/VRMenu/CleaningStatsAggregator.cs` | % global de limpieza |
| `Assets/Scripts/VRMenu/VRMenuButtonFeedback.cs` | Hover/click SFX + haptics |
| `Assets/Scripts/VRMenu/HelpPanelController.cs` | Panel de ayuda |
| `Assets/Scripts/VRMenu/VRMenuUIBinder.cs` | Wiring de botones en runtime |
| `Assets/Editor/InWorldMenuVRBuilder.cs` | Generación de prefab y escena |
| `Assets/Prefabs/UI/InWorldMenuVR.prefab` | Prefab (se crea desde el menú Tools) |
| `Assets/Audio/MainMixer.mixer` | AudioMixer (se crea desde el menú Tools) |

## Setup rápido (Unity Editor)

**Al dar Play**, si no hay menú en la escena, se crea automáticamente (`VRMenuRuntimeBootstrap`). Deberías ver en consola: `[VRMenu] Menu generado en runtime...`

Para un prefab permanente en la escena (recomendado):

1. **Tools → VR Menu → Create Audio Mixer Asset**
2. **Tools → VR Menu → Create InWorld Menu Prefab**
3. Abre `URP_Stylized_House_Interior.unity`
4. **Tools → VR Menu → Add To House Interior Scene**

Esto configura `EventSystem` + `XRUIInputModule`, instancia el prefab y conecta `PaintableSurfaceUI`, `XROrigin` y haptics.

**Si no ves el menú al Play:** mira delante del jugador (~2 m, altura ~1.4 m). En Hierarchy busca `InWorldMenuVR`. Gira la vista o usa el Scene view para localizarlo.

## AudioMixer — configuración manual (si hace falta)

Si los sliders no cambian el volumen, abre `Assets/Audio/MainMixer.mixer`:

1. Crea grupos hijos bajo **Master**: `Ambiente`, `Musica`
2. En cada grupo, clic derecho en **Attenuation → Volume → Expose**
3. Nombra los parámetros expuestos exactamente:
   - `VolAmbiente` (grupo Ambiente)
   - `VolMusica` (grupo Musica)
4. Asigna `AudioSource` del juego:
   - Sonidos de limpieza / ambiente → salida **Ambiente**
   - Música → salida **Musica**

Conversión en código: `Mathf.Log10(linear01) * 20` (linear 0–1 → dB).

## XR Interaction Toolkit

### EventSystem

- Un solo `EventSystem` activo con **`XRUIInputModule`**
- Desactiva `StandaloneInputModule` / `InputSystemUIInputModule` duplicados si bloquean la UI

### Canvas del menú

- **Render Mode:** World Space
- **Escala:** `0.002` (panel ~2 m × 1.2 m)
- **`TrackedDeviceGraphicRaycaster`** en el canvas (no uses solo `GraphicRaycaster` para VR)

### Ray Interactor

En el XR Rig, el **Ray Interactor** debe tener **Enable UI Interaction** activado (el Starter Asset ya lo trae en `Ray Interactor.prefab`).

### Capas

- Canvas en capa **Default** (o incluida en el Raycast Mask del interactor)

## Controles del menú

| Elemento | Acción |
|----------|--------|
| Slider Ambiente | Volumen 0–1, default 0.7, guardado en PlayerPrefs |
| Slider Música | Volumen 0–1, default 0.4 |
| STATS | % medio de `Cleanliness` de todas las superficies |
| Recenter | `XRInputSubsystem.TryRecenter()` |
| Reiniciar | Modal Sí/No → recarga escena activa |
| Ayuda | Muestra controles de limpieza y menú |
| Salir | Modal → `Application.Quit()` |

La lista por superficie (`PaintableSurfaceUI`) **sigue activa** en paralelo.

## Logs de depuración

Filtra la consola por `[VRMenu]`:

- Hover / Click en botones
- Cambios de volumen
- Recenter OK / fallo
- Apertura/cierre de modal y ayuda

## Checklist en dispositivo (Quest / PCVR)

- [ ] Canvas visible a ~1 m, altura ~1.4 m, sin seguir la cámara
- [ ] Ray del mando resalta botones (hover)
- [ ] Click en botones con sonido + vibración
- [ ] Sliders mueven volumen (o guardan prefs si aún no hay audio en escena)
- [ ] % de limpieza sube al limpiar
- [ ] Recenter muestra log en consola (Quest) o efecto visible
- [ ] Reiniciar / Salir piden confirmación
- [ ] Lista `PaintableSurfaceUI` sigue funcionando

## Troubleshooting

| Problema | Solución |
|----------|----------|
| UI no responde al ray | `XRUIInputModule`, UI Interaction en Ray Interactor, capa del canvas |
| Texto borroso | Aumentar escala del canvas o font size TMP |
| Recenter no hace nada en Editor | Normal con Mock HMD; probar en Quest |
| Volumen sin efecto | Exponer `VolAmbiente` / `VolMusica` en el mixer y asignar grupos a AudioSources |
| Error KBCore en PaintableSurfaceUI | Ejecutar House Cleaning Setup Fase 4 |

## XRI 2.x vs 3.x

Este proyecto usa **XRI 3.1.3**. Los nombres de componentes coinciden con la checklist de XRI 2.x (`XRUIInputModule`, `TrackedDeviceGraphicRaycaster`). No uses APIs obsoletas como `XROrigin.RequestResetView()` (no existe); usa `TryRecenter()` del subsistema XR.
