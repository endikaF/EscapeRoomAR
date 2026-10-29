# Halloween AR (Unity)

Monstruos, decoración y sonidos que aparecen en puntos que tú colocas en la casa. Funciona en el editor, en móvil/tablet (AR Foundation + ARCore) y en Meta Quest 3 (AR Foundation + OpenXR: Meta).

---

## Escenas y acceso

El proyecto tiene dos escenas: **Inicio** (menú) y **Gafas** (juego).

| Quién | Cómo entra | Qué hace |
|---|---|---|
| **Jugador adulto** | Botón **JUGAR** (clic con el gatillo derecho) | Modo JUGAR |
| **Jugador niño** | Botón **JUGAR NIÑOS** | Modo JUGAR con `KidsMode` (solo cambia la voz del esqueleto) |
| **Administrador** | Con el mando derecho en el menú: **A, A, B, Grip** (máx. 3 s entre pulsaciones) | Modo COLOCAR con permisos de administrador |

- El gatillo no forma parte de la combinación. Si te equivocas de botón, la secuencia se reinicia.
- Solo en el editor, en el menú: **1** = A, **2** = B, **3** = Grip.

---

## Tipos de punto

| Categoría | Tipos (en el orden en que se cicla) | Cuándo aparece |
|---|---|---|
| **Monster** | Zombie, Spider, Bat, Skeleton, Payaso_perseguir, Payaso_emboscar | Al acercarte (distancia horizontal, 3 m por defecto) |
| **Decoration** | WebCorner, WebWall, WebCircle, Payaso_secuencia | Nada más entrar en JUGAR |
| **Audio** | Audio_reloj, Audio_cancion, Audio_demo | Al acercarte |

- La distancia de activación se puede cambiar por tipo en el `MonsterRegistry`.
- Cada punto aparece una sola vez por partida.
- **Zombis y payasos**: colócalos en el **suelo** (usan la altura del punto como suelo).
- **Spider y telarañas** (WebCorner, WebWall, WebCircle): se pegan a la superficie donde las pongas (pared, techo o suelo).

---

## Controles para colocar los puntos (Quest 3, solo administrador)

| Botón | Acción |
|---|---|
| **Gatillo derecho** | Poner un punto donde apunta el rayo (cian) |
| **A** (derecho) | Cambiar de tipo dentro de la categoría activa |
| **X** (izquierdo) | Cambiar de categoría (Monster → Decoration → Audio) |
| **Grip** (derecho) | Toque corto: deshacer el último punto. Mantener 1,5 s: borrar todos |
| **B** (derecho) | Pasar a JUGAR |

El texto de estado delante de ti muestra modo, categoría, tipo, nº de puntos y el último mensaje. Con el `AR Anchor Manager` asignado, los puntos se guardan solos y se recargan al abrir la app.

## Controles para jugar (Quest 3)

En JUGAR **solo funcionan los gatillos** (izquierdo y derecho, cada uno con su rayo rojo y su cadencia). A, B, X, Y y Grip no hacen nada.

| Botón | Acción |
|---|---|
| **Gatillo izquierdo / derecho** | Disparar (1 de daño; los monstruos tienen 3 de vida por defecto) |

**Salir al menú**: mantener 3 s a la vez **Grip izquierdo + Grip derecho + A + X** (solo en JUGAR). Vuelve a Inicio con todo reiniciado a adulto y sin permisos de administrador; los puntos se conservan. Para volver a colocar hay que entrar de nuevo como administrador.

Los botones del sistema (Meta y menú) los gestiona el casco y no se pueden bloquear desde Unity.

## Editor y móvil

| Acción | Editor (sin mando) | Móvil/tablet |
|---|---|---|
| Poner punto / disparar | Clic izquierdo | Tocar un plano / tocar el monstruo |
| Cambiar tipo | **1** | Botones de los tipos de la categoría |
| Cambiar categoría | **2** | Botón **Cat** |
| COLOCAR / JUGAR | **Espacio** | Botones **JUGAR** / **VOLVER A COLOCAR** |
| Deshacer / Borrar todos | **Backspace** / **Supr** | Botones **Deshacer** / **Borrar todo** |
| Salir al menú (en JUGAR) | Mantener **F9** 3 s | — |

Con mando simulado (XR Interaction Toolkit) valen los mismos botones que en el Quest. Quita `ClickToShoot` de la cámara para no disparar dos veces.

---

## Configuración en un escenario nuevo

### Proyecto
- **Active Input Handling = Both** (se mezclan Input System e Input antiguo).
- Paquetes: AR Foundation, Input System, XR Core Utils y OpenXR + Meta OpenXR (Quest).
- Build Profiles: `Inicio` primera y `Gafas` segunda, plataforma Android. Un solo proveedor XR por build: **OpenXR** para Quest, **ARCore** para móvil.
- Features de OpenXR activadas: **Session, Planes, Raycasts, Camera (Passthrough) y Anchors**.
- Cámara con fondo transparente (passthrough).
- Quita `ClickToShoot` y `EditorPlayerController` de la cámara en el build del Quest (son solo para el editor).

### Escena Inicio
- Canvas en **World Space** con los botones **JUGAR** y **JUGAR NIÑOS**, cada uno con **Collider**.
- `OnClick`: JUGAR → `MenuController.StartPlay`; JUGAR NIÑOS → `MenuController.StartPlayKids`.
- Objeto con `MenuController`.
- Objeto con `MenuControllerPointer`: asignar `Xr Origin` y el mismo `Ray Pitch Offset` que en `QuestControllerPlacement`.
- XR Origin con cámara.

### Escena Gafas

| Objeto | Componente | Qué asignar |
|---|---|---|
| XR Origin | AR Session, `ARRaycastManager`, `ARPlaneManager`, `ARAnchorManager` (Quest) | — |
| Provider | `ARTapSpawnPointProvider` | `Raycast Manager`, `Plane Manager`, `Anchor Manager` (vacío = sin persistencia, móvil), `Points Parent` (solo sin anclas: el XR Origin) |
| Spawn | `SpawnManager` | `Provider Source` = el provider, `Registry`, `Activation Distance` (3) |
| Mandos | `QuestControllerPlacement` | `Provider`, `Spawn Manager`, `Xr Origin`, `Ray Pitch Offset` |
| Salida | `ExitToMenuGesture` | `Provider`, `Menu Scene Name` = `Inicio` |
| Permisos | `QuestPermissions` | — (pide `com.oculus.permission.USE_SCENE`) |
| HUD móvil | `ARPlacementHUD` | `Provider`, `Spawn Manager` |
| Música | `ThemeMusicController` | `AudioSource` 2D con la canción |
| Murmullo | `BatMurmur` | `AudioSource` con el murmullo |
| Fin de partida (opcional) | `EndOfGame` | `Provider`, `Spawn Manager`, `Exit` (el objeto con `ExitToMenuGesture`). Vuelve al menú cuando no quedan monstruos vivos tras todos los spawns, o por tiempo si `Use Timer` está activo (300 s por defecto) |

### MonsterRegistry
Clic derecho en Project > **Create > Halloween > Monster Registry** y asignar el prefab de cada tipo (opcional: prefabs alternativos, que se eligen al azar, y distancia de activación por tipo).

### Requisitos de cada prefab

| Tipo | Scripts / requisitos |
|---|---|
| Zombie | `ChasePlayer` + `MonsterHealth` + Collider. Animator: bool `IsMoving`, triggers `Attack`, `Hit`, `Die` |
| Spider | `SpiderCrawl` + `MonsterHealth` + Collider. Animator: `IsMoving`, `Attack`. Sonido de ataque: Animation Event a `PlayAttackSound` o `AttackSoundState` en el estado de ataque |
| Bat | Prefab vacío con `BatSwarm` y `Bat Prefab` asignado (Animator del murciélago: bool `Fast`, trigger `Attack`) |
| Skeleton | `SkeletonSequence`: `Lid`, `Skeleton Animator`, nombres exactos de los estados (`tumbado_Incorporar`, `Girar cabeza izq`, `Hablar`), `Voice` y clips adulto/niños |
| WebCorner / WebWall / WebCircle | Prefab decorativo |
| Payaso_secuencia | `PayasoSecuencia`: estado de señalar (`Pointing`) y `Voice` |
| Payaso_perseguir | `PayasoPerseguir` + `MonsterHealth` + Collider. Animator: `IsMoving`, `Attack` |
| Payaso_emboscar | `PayasoEmboscar` + `MonsterHealth` + Collider. `Watch Distance` (4) ≥ distancia de activación del tipo |
| Audio_reloj / Audio_demo | `AudioPoint` (Kind = Clock o Demogorgon) + `AudioSource` con clip (Play On Awake y Loop desactivados) |

Para recibir disparos, el prefab necesita **Collider** y `MonsterHealth`.

---

## Puesta en marcha en el Quest (lugar nuevo)

1. Activa el modo desarrollador del Quest y conéctalo por USB. En Unity pulsa **Build And Run** y acepta la depuración USB en el casco.
2. Haz el **Space Setup** de la habitación (ajustes del casco, *Espacio*): escanea suelo, paredes, techo y puertas o ventanas.
3. Abre la app (*Orígenes desconocidos*) y acepta el **permiso de escena**.
4. En el menú, entra como administrador: **A, A, B, Grip** (mando derecho).
5. Mira alrededor unos segundos hasta que se vean los planos. Comprueba que el texto de estado indica modo COLOCAR.
6. Coloca los puntos: **gatillo** para poner, **A** para el tipo, **X** para la categoría, **Grip** para deshacer.
7. Pulsa **B** para probar en modo JUGAR y sal con el gesto de salida cuando termines.

Cada ubicación necesita su propio Space Setup y sus propios puntos.