# Editor de Texto en C#

Un editor de texto enriquecido por consola desarrollado en C# (.NET), estructurado bajo principios de diseño orientado a objetos y patrones de diseño clásicos (GoF).

---

## Estructura del Proyecto

```text
??? Commands/          # Patrón Command (operaciones de edición y deshacer)
??? Layout/            # Patrón Strategy y motor de renderizado/maquetado
??? Model/             # Estado elemental (buffer de texto, cursor, selecciones)
??? Structure/         # Patrón Composite (jerarquía del documento)
??? UI/                # Manejo de entrada por teclado y vistas por consola
??? Visitors/          # Patrón Visitor (estadísticas y exportadores)
??? Editor.cs          # Fachada / Controlador central del editor
??? Editorconfig.cs    # Configuración global (dimensiones de página, márgenes)
??? Program.cs         # Punto de entrada de la aplicación