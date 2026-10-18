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
| **Grip** | Deshacer el último punto | – |

- El rayo es **cian** en modo COLOCAR y **rojo** en modo JUGAR.
- El texto de estado delante de ti muestra: modo, tipo seleccionado, nº de puntos y el último mensaje.
- Coloca los **zombis en el suelo**: usan la altura del punto como suelo. Las arañas pueden ir en pared, techo o suelo.
- Los puntos se pierden al cerrar la app (la persistencia con anclas es la siguiente fase).

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
