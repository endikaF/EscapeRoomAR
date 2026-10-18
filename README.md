# Halloween AR (Unity)

Zombis y arañas que aparecen en puntos que tú colocas en la casa y te persiguen. Funciona en el editor, en móvil/tablet (AR Foundation + ARCore) y en Meta Quest 3 (AR Foundation + OpenXR: Meta).

La lógica de juego es la misma en todas las plataformas. Solo cambian la entrada (toque, ratón, mando) y el proveedor XR.

---

## Controles en Quest 3 (mando derecho)

| Botón | Modo COLOCAR | Modo JUGAR |
|---|---|---|
| **Gatillo** | Poner un punto donde apunta el rayo | Disparar |
| **A** | Cambiar tipo (Zombie ↔ Spider) | – |
| **B** | Pasar a JUGAR | Volver a COLOCAR (borra los monstruos aparecidos) |
| **Grip** | Toque corto: deshacer el último punto. Mantener 1,5 s: borrar todos | – |

- El rayo es **cian** en modo COLOCAR y **rojo** en modo JUGAR.
- El texto de estado delante de ti muestra: modo, tipo seleccionado, nº de puntos y el último mensaje.
- Coloca los **zombis en el suelo**: usan la altura del punto como suelo. Las arañas pueden ir en pared, techo o suelo.
- Con el **AR Anchor Manager** asignado, los puntos se guardan solos y se vuelven a cargar al abrir la app.

## Controles en el editor

### Con mando simulado (simulador de XR Interaction Toolkit)
Mismos botones que en el Quest (gatillo, A, B, Grip). Las teclas para manejar cada mando dependen de la versión del simulador: están en el panel de ayuda que aparece en la ventana Game al dar a Play. Quita `ClickToShoot` de la cámara para no disparar dos veces.

### Sin mando (XR Simulation de AR Foundation o ratón)
| Acción | Tecla |
|---|---|
| Poner punto / disparar | Clic izquierdo |
| Cambiar tipo | **1** |
| Alternar COLOCAR / JUGAR | **Espacio** |
| Deshacer | **Backspace** |
| Borrar todos | **Supr** |

Estos atajos solo se activan si no hay ningún mando detectado.

## Controles en móvil/tablet

| Acción | Cómo |
|---|---|
| Colocar punto | Tocar un plano (modo COLOCAR) |
| Elegir tipo | Botones **Zombie** / **Spider** |
| Deshacer / Borrar todo | Botones **Deshacer** / **Borrar todo** |
| Pasar a jugar | Botón **JUGAR** |
| Volver a colocar | Botón **VOLVER A COLOCAR** |
| Disparar | Tocar la pantalla apuntando al monstruo |

---

## Primera vez en el Quest: pasos a seguir

### Antes de ponerte el casco (en el PC)
1. Activa el **modo desarrollador** del Quest desde la app móvil de Meta y ten el cable USB a mano.
2. Comprueba que en Unity están activadas las features de OpenXR (**Session**, **Planes**, **Raycasts**, **Camera (Passthrough)** y **Anchors**) y que `Anchor Manager` está asignado en el `Provider`.
3. En `Build Profiles`, pon la escena `Quest` la primera, con la plataforma en Android y **OpenXR** (no ARCore) en XR Plug-in Management.
4. Conecta el Quest por USB y pulsa **Build And Run**.

### Con el casco puesto, en el lugar donde se va a usar
5. Acepta la **depuración USB** si el casco te la pide.
6. Haz el **Space Setup** de esa habitación (ajustes del casco, *Espacio*): escanea suelo, paredes, techo y puertas o ventanas. Mira bien alrededor, sobre todo las paredes donde quieras colocar arañas.
7. Abre la app desde la biblioteca (*Orígenes desconocidos*) y acepta el **permiso de escena** cuando aparezca.
8. Espera unos segundos mirando a tu alrededor hasta que se vean los planos. Si no aparecen, cierra la app y vuelve a abrirla una vez.
9. Comprueba el **texto de estado** delante de ti: debe indicar modo COLOCAR. Si dice que no se pueden guardar anclas, la persistencia no está activa (revisa el paso 2).

### Colocar los puntos
10. Apunta con el mando derecho al suelo y pulsa el **gatillo** para colocar un **zombi**. El estado debe decir "Punto Zombie colocado" y, enseguida, "guardado".
11. Pulsa **A** para cambiar a **Spider**, apunta a una pared o al techo y coloca otro punto.
12. Si te equivocas, toca el **grip** para deshacer el último. Mantenlo pulsado 1,5 s para borrar todos.

### Probar el juego
13. Pulsa **B** para pasar a modo JUGAR, retrocede unos metros y camina hacia los puntos: deben aparecer los monstruos.
14. Dispara con el **gatillo** apuntando a un monstruo. Pulsa **B** de nuevo para volver a colocar (borra los monstruos aparecidos, no los puntos).

### Comprobar la persistencia
15. Cierra la app del todo.
16. Ábrela desde **otra esquina de la habitación y mirando hacia otro lado**.
17. Mira alrededor y espera a que el estado diga que se han cargado los puntos.
18. Pulsa **B** y comprueba que los monstruos aparecen en los sitios que marcaste.
19. Para una prueba más exigente, apaga y enciende el casco y repite los pasos 16 a 18.

### Si algo falla

| Síntoma | Causa probable |
|---|---|
| "No hay plano ahí" | Falta el Space Setup, falta el permiso de escena, o apuntas a algo no detectado |
| Pantalla negra | Falta la feature de passthrough o el fondo de la cámara no es transparente |
| No guarda los puntos | La feature de anclas no está activa, falta el `Anchor Manager`, o hay un error de permiso en el log |
| Carga a medias al reabrir | El casco aún se está relocalizando: mira por la habitación unos segundos; el estado indica cuántos faltan |
| Los puntos salen en otro sitio | Cambió mucho la habitación o es otro espacio: repite el Space Setup y vuelve a colocarlos |

### Notas
- Cada ubicación necesita su propio Space Setup y sus propios puntos. Si cambias de sitio, repite desde el paso 6.
- Si mueves muchos muebles después de escanear, repite el Space Setup para que los planos y el reconocimiento sigan siendo fiables.
- La persistencia solo se puede validar en el casco: en el editor y en los simuladores no se guardan anclas.
- Móvil y Quest son ambos Android: con el proveedor XR hay que elegir uno por build (ARCore para móvil, OpenXR para Quest).