# VR In-World Menu — Setup Guide

Menú de configuración **World Space** para el simulador de limpieza VR. Usa **XR Interaction Toolkit 3.1.3** (compatible con flujos de XRI 2.x: `XRUIInputModule`, `TrackedDeviceGraphicRaycaster`).

**Para agentes de IA:** contexto técnico completo en [`VR_MENU_AGENT_CONTEXT.md`](VR_MENU_AGENT_CONTEXT.md).

## Requisitos

- Unity con URP y OpenXR
- Paquete `com.unity.xr.interaction.toolkit` 3.1.3+
- TextMeshPro importado
- Escena con `XR Origin (XR Rig)` y `PaintableSurfaceUI` configurado (Fase 4 de House Cleaning Setup)

## Archivos del sistema

| Ruta | Rol |
|------|-----|
| `Assets/Scripts/VRMenu/VRMenuRuntimeBootstrap.cs` | Crea menú al Play y tras cada carga de escena |
| `Assets/Scripts/VRMenu/VRMenuFactory.cs` | Construye UI en runtime |
| `Assets/Scripts/VRMenu/InWorldMenuVR.cs` | Lógica del menú, modales, visibilidad |
| `Assets/Scripts/VRMenu/InWorldMenuPlacement.cs` | Posición `(0, 1.4, 2)` vs XR Origin |
| `Assets/Scripts/VRMenu/VRMenuToggleInput.cs` | Tecla M / botón Menu del mando |
| `Assets/Scripts/VRMenu/VRMenuWorldCanvasDriver.cs` | Cámara del canvas World Space |
| `Assets/Scripts/VRMenu/AudioManager.cs` | Volúmenes vía AudioMixer |
| `Assets/Scripts/VRMenu/CleaningStatsAggregator.cs` | % global de limpieza |
| `Assets/Scripts/VRMenu/VRMenuButtonFeedback.cs` | Hover/click SFX + haptics |
| `Assets/Scripts/VRMenu/HelpPanelController.cs` | Panel ayuda + VOLVER AL MENÚ |
| `Assets/Scripts/VRMenu/VRMenuUIBinder.cs` | Wiring de botones en runtime |
| `Assets/Editor/InWorldMenuVRBuilder.cs` | Generación de prefab y escena |
| `Assets/Prefabs/UI/InWorldMenuVR.prefab` | Prefab (se crea desde Tools) |
| `Assets/Audio/MainMixer.mixer` | AudioMixer (se crea desde Tools) |

## Setup rápido (Unity Editor)

**Al dar Play**, si no hay menú en la escena, se crea automáticamente (`VRMenuRuntimeBootstrap` + `VRMenuFactory`). En consola: `[VRMenu] Menu generado en runtime...` y `Menu visible frente al jugador...`

Para un prefab permanente en la escena (opcional):

1. **Tools → VR Menu → Create Audio Mixer Asset**
2. **Tools → VR Menu → Create InWorld Menu Prefab**
3. Abre `URP_Stylized_House_Interior.unity`
4. **Tools → VR Menu → Add To House Interior Scene**

Esto configura `EventSystem` + `XRUIInputModule`, instancia el prefab y conecta `PaintableSurfaceUI`, `XROrigin` y haptics.

**Si no ves el menú al Play:** mira delante del jugador (~2 m, altura ~1.4 m). Pulsa **M** para abrir/cerrar. En Hierarchy busca `InWorldMenuVR`.

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
- **`VRMenuWorldCanvasDriver`** asigna la cámara XR automáticamente

### Ray Interactor

En el XR Rig, el **Ray Interactor** debe tener **Enable UI Interaction** activado.

## Controles del menú

| Elemento | Acción |
|----------|--------|
| **M** (teclado) | Abrir/cerrar menú (clic en ventana Game) |
| Botón **Menu** (Quest) / **Start** (Oculus) | Abrir/cerrar menú |
| **CERRAR** | Ocultar menú |
| Slider Ambiente | Volumen 0–1, default 0.7, PlayerPrefs |
| Slider Música | Volumen 0–1, default 0.4 |
| STATS | % medio de limpieza global |
| Recenter | `XRInputSubsystem.TryRecenter()` |
| Reiniciar | Modal → recarga escena (el menú se recrea solo) |
| Ayuda | Panel fullscreen de controles |
| **VOLVER AL MENÚ** | Cierra ayuda y vuelve al panel principal |
| Salir | Modal → `Application.Quit()` |

La lista por superficie (`PaintableSurfaceUI`) **sigue activa** en paralelo.

## Logs de depuración

Filtra la consola por `[VRMenu]`.

## Checklist en dispositivo (Quest / PCVR)

- [ ] Canvas visible ~2 m delante, altura ~1.4 m
- [ ] Ray resalta botones
- [ ] M / Menu abre y cierra menú
- [ ] Ayuda → **VOLVER AL MENÚ** regresa al panel
- [ ] Tras **REINICIAR ESCENA**, menú y **M** siguen funcionando
- [ ] Lista `PaintableSurfaceUI` sigue funcionando

## Troubleshooting

| Problema | Solución |
|----------|----------|
| UI no responde al ray | `XRUIInputModule`, UI Interaction en Ray Interactor |
| Menú no aparece al Play | Hierarchy: ¿`InWorldMenuVR`? Consola `[VRMenu]`; salir y entrar de Play |
| **M** no funciona tras REINICIAR ESCENA | Bug corregido con `sceneLoaded`; recompilar; si persiste ver `VR_MENU_AGENT_CONTEXT.md` |
| Ayuda sin volver | Usar **VOLVER AL MENÚ**; regenerar prefab si falta `BtnHelpBack` |
| Menú al revés | Ver orientación en `VR_MENU_AGENT_CONTEXT.md` |
| Volumen sin efecto | Exponer `VolAmbiente` / `VolMusica` en mixer |
| Error KBCore PaintableSurfaceUI | House Cleaning Setup Fase 4 |

## XRI 2.x vs 3.x

Proyecto en **XRI 3.1.3**. `XROrigin` en `Unity.XR.CoreUtils`. Recenter: `TryRecenter()`, no APIs obsoletas.
