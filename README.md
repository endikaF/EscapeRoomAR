# Halloween AR (Unity)

Zombis y arañas que aparecen en puntos que tú colocas en la casa y te persiguen. Funciona en el editor, en móvil/tablet (AR Foundation + ARCore) y en Meta Quest 3 (AR Foundation + OpenXR: Meta).

La lógica de juego es la misma en todas las plataformas. Solo cambian la entrada (toque, ratón, mando) y el proveedor XR.

---

## Escenas y flujo de uso

El proyecto tiene dos escenas: **Inicio** (menú) y **Gafas** (juego).

| Quién | Cómo entra | Qué hace |
|---|---|---|
| **Jugador (cliente)** | Botón **JUGAR** del menú (clic con el gatillo derecho) | Entra directamente en modo JUGAR |
| **Administrador** | Combinación en el menú, con el mando derecho: **A, A, B, Grip** (en ese orden, máx. 3 s entre pulsaciones) | Entra en modo COLOCAR con permisos de administrador |

- El gatillo **no** forma parte de la combinación: solo sirve para hacer clic en el botón.
- Si te equivocas de botón, la secuencia se reinicia.
- Solo en el editor, atajos equivalentes en el menú: **1** = A, **2** = B, **3** = Grip (o sea 1, 1, 2, 3).

---

## Controles en Quest 3

### Modo COLOCAR (solo administrador, mando derecho)

| Botón | Acción |
|---|---|
| **Gatillo** | Poner un punto donde apunta el rayo |
| **A** | Cambiar tipo (Zombie ↔ Spider) |
| **B** | Pasar a JUGAR |
| **Grip** | Toque corto: deshacer el último punto. Mantener 1,5 s: borrar todos |

### Modo JUGAR (protección para niños)

En modo JUGAR **solo funcionan los gatillos**. Se dispara con los **dos mandos a la vez** (cada uno con su rayo y su propia cadencia mínima entre disparos). El resto de botones están deshabilitados: **A, B y Grip no hacen nada**, ni borran puntos ni cambian de modo. El mando izquierdo no tiene ninguna otra función.

| Botón | Acción |
|---|---|
| **Gatillo izquierdo / derecho** | Disparar |
| Cualquier otro botón | Nada |

Los botones del sistema (**Meta** de cada mando y **menú** del izquierdo) los gestiona el casco y no se pueden bloquear desde Unity: pueden pausar la app. Si esto no es aceptable para el uso previsto, hay que valorar el modo quiosco de Meta (consultar su documentación actual).

### Salir al menú inicial (gesto de salida)

Mantener **a la vez durante 3 segundos**:

> **Grip izquierdo + Grip derecho + A + X**

- Solo funciona en modo JUGAR.
- Si sueltas algún botón antes de los 3 s, el contador vuelve a cero.
- Al salir se carga la escena **Inicio** y todo empieza de cero (cliente, modo Jugar). Los puntos **no se pierden**: están en las anclas guardadas y en el JSON, y se recargan al volver a entrar en **Gafas**.
- En el editor, mantener **F9** (en modo Jugar) equivale al gesto completo.
- Para volver a colocar puntos hay que salir al menú y entrar con la combinación de administrador.
- El script es `ExitToMenuGesture` (objeto vacío en la escena Gafas, con el `Provider` asignado y `Menu Scene Name` = `Inicio`).

### Fin de partida automático (opcional)

El componente `EndOfGame` devuelve al menú de forma automática cuando:
- todos los puntos han hecho aparecer su monstruo y no queda ninguno vivo (tras `Delay After Last Kill`, 5 s por defecto), o
- se agota el tiempo (`Use Timer` activado; `Time Limit Seconds`, 300 s por defecto, contado desde que se entra en JUGAR).

Para usarlo: objeto vacío en Gafas con `EndOfGame`, asignando `Provider`, `Spawn Manager` y `Exit` (el objeto con `ExitToMenuGesture`). Requiere la propiedad `AllPointsSpawned` en `SpawnManager`. Si no se quiere, basta con no añadirlo o desactivarlo.

### Información en pantalla

- El rayo es **cian** en modo COLOCAR y **rojo** en modo JUGAR (en JUGAR, uno por cada mando).
- El texto de estado delante de ti muestra: modo, tipo seleccionado, nº de puntos y el último mensaje.
- Coloca los **zombis en el suelo**: usan la altura del punto como suelo. Las arañas pueden ir en pared, techo o suelo.
- Con el **AR Anchor Manager** asignado, los puntos se guardan solos y se vuelven a cargar al abrir la app.

## Controles en el editor

### Con mando simulado (simulador de XR Interaction Toolkit)
Mismos botones que en el Quest (gatillos, A, B, Grip, y el gesto de salida con grips + A + X). Las teclas para manejar cada mando dependen de la versión del simulador: están en el panel de ayuda que aparece en la ventana Game al dar a Play. Quita `ClickToShoot` de la cámara para no disparar dos veces.

### Sin mando (XR Simulation de AR Foundation o ratón)
| Acción | Tecla |
|---|---|
| Poner punto / disparar | Clic izquierdo |
| Cambiar tipo | **1** |
| Alternar COLOCAR / JUGAR | **Espacio** |
| Deshacer | **Backspace** |
| Borrar todos | **Supr** |
| Salir al menú (en JUGAR) | Mantener **F9** durante 3 s |

Estos atajos solo se activan si no hay ningún mando detectado (excepto F9, que funciona siempre en el editor).

## Controles en móvil/tablet

| Acción | Cómo |
|---|---|
| Colocar punto | Tocar un plano (modo COLOCAR) |
| Elegir tipo | Botones **Zombie** / **Spider** |
| Deshacer / Borrar todo | Botones **Deshacer** / **Borrar todo** |
| Pasar a jugar | Botón **JUGAR** |
| Volver a colocar | Botón **VOLVER A COLOCAR** |
| Disparar | Tocar la pantalla apuntando al monstruo |

El gesto de salida y el bloqueo de botones son específicos de los mandos del Quest.

---

## Primera vez en el Quest: pasos a seguir

### Antes de ponerte el casco (en el PC)
1. Activa el **modo desarrollador** del Quest desde la app móvil de Meta y ten el cable USB a mano.
2. Comprueba que en Unity están activadas las features de OpenXR (**Session**, **Planes**, **Raycasts**, **Camera (Passthrough)** y **Anchors**) y que `Anchor Manager` está asignado en el `Provider`.
3. En `Build Profiles`, pon la escena `Inicio` la primera y `Gafas` la segunda, con la plataforma en Android y **OpenXR** (no ARCore) en XR Plug-in Management.
4. Conecta el Quest por USB y pulsa **Build And Run**.

### Con el casco puesto, en el lugar donde se va a usar
5. Acepta la **depuración USB** si el casco te la pide.
6. Haz el **Space Setup** de esa habitación (ajustes del casco, *Espacio*): escanea suelo, paredes, techo y puertas o ventanas. Mira bien alrededor, sobre todo las paredes donde quieras colocar arañas.
7. Abre la app desde la biblioteca (*Orígenes desconocidos*) y acepta el **permiso de escena** cuando aparezca.
8. En el menú, entra como administrador con la combinación **A, A, B, Grip** del mando derecho.
9. Espera unos segundos mirando a tu alrededor hasta que se vean los planos. Si no aparecen, cierra la app y vuelve a abrirla una vez.
10. Comprueba el **texto de estado** delante de ti: debe indicar modo COLOCAR. Si dice que no se pueden guardar anclas, la persistencia no está activa (revisa el paso 2).

### Colocar los puntos
11. Apunta con el mando derecho al suelo y pulsa el **gatillo** para colocar un **zombi**. El estado debe decir "Punto Zombie colocado" y, enseguida, "guardado".
12. Pulsa **A** para cambiar a **Spider**, apunta a una pared o al techo y coloca otro punto.
13. Si te equivocas, toca el **grip** para deshacer el último. Mantenlo pulsado 1,5 s para borrar todos.

### Probar el juego
14. Pulsa **B** para pasar a modo JUGAR, retrocede unos metros y camina hacia los puntos: deben aparecer los monstruos.
15. Dispara con los **gatillos** (prueba también los dos a la vez) apuntando a un monstruo.
16. Comprueba la protección: pulsa A, B, los grips, los joysticks y X/Y. No debe pasar nada.
17. Mantén **grip izquierdo + grip derecho + A + X** durante 3 s: debe volver al menú inicial.

### Comprobar la persistencia
18. Cierra la app del todo.
19. Ábrela desde **otra esquina de la habitación y mirando hacia otro lado**.
20. Entra como administrador (A, A, B, Grip), mira alrededor y espera a que el estado diga que se han cargado los puntos.
21. Pulsa **B** y comprueba que los monstruos aparecen en los sitios que marcaste.
22. Para una prueba más exigente, apaga y enciende el casco y repite los pasos 19 a 21.

### Si algo falla

| Síntoma | Causa probable |
|---|---|
| "No hay plano ahí" | Falta el Space Setup, falta el permiso de escena, o apuntas a algo no detectado |
| Pantalla negra | Falta la feature de passthrough o el fondo de la cámara no es transparente |
| No guarda los puntos | La feature de anclas no está activa, falta el `Anchor Manager`, o hay un error de permiso en el log |
| Carga a medias al reabrir | El casco aún se está relocalizando: mira por la habitación unos segundos; el estado indica cuántos faltan |
| Los puntos salen en otro sitio | Cambió mucho la habitación o es otro espacio: repite el Space Setup y vuelve a colocarlos |
| El rayo del mando izquierdo sale inclinado | Ajusta `Ray Pitch Offset` en `QuestControllerPlacement` (afecta a los dos mandos) |
| El gesto de salida no funciona | Solo vale en modo JUGAR; comprueba que `Provider` está asignado y que `Menu Scene Name` es `Inicio` y está en Build Profiles |

### Notas
- Cada ubicación necesita su propio Space Setup y sus propios puntos. Si cambias de sitio, repite desde el paso 6.
- Si mueves muchos muebles después de escanear, repite el Space Setup para que los planos y el reconocimiento sigan siendo fiables.
- La persistencia solo se puede validar en el casco: en el editor y en los simuladores no se guardan anclas.
- Móvil y Quest son ambos Android: con el proveedor XR hay que elegir uno por build (ARCore para móvil, OpenXR para Quest).