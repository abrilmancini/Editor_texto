// Program (punto de entrada)
// Responsabilidad: crear los objetos (configuración, editor, vista, historial, manejador de teclas)
// y correr el loop principal: dibujar, leer una tecla, procesarla.
// Ítem de cambio que esconde: cómo se ensambla la aplicación (qué implementaciones se usan y con qué medidas).

using System.Text;
using EditorTexto;
using EditorTexto.Commands;
using EditorTexto.UI;

Console.OutputEncoding = Encoding.UTF8;   // para que se vean ñ y tildes
Console.TreatControlCAsInput = true;      // CTRL+C no cierra el programa

EditorConfig config = new EditorConfig(lineWidth: 50, linesPerPage: 20, visibleLines: 15);
Editor editor = new Editor(config);
editor.LoadText(
    "Estamos haciendo un editor de texto. Queremos representar un documento de manera tal " +
    "que pueda estar separado en páginas, en párrafos, en líneas y en palabras.\n" +
    "\n" +
    "Probá escribir, borrar, mover el cursor con las flechas y cambiar la alineación con F1 a F4.");

ConsoleView view = new ConsoleView(editor);
CommandHistory history = new CommandHistory();
InputHandler input = new InputHandler(editor, history);

Console.Clear();
while (true)
{
    view.Draw();
    ConsoleKeyInfo key = Console.ReadKey(intercept: true);
    if (!input.Handle(key))
        break;
}

Console.Clear();
Console.WriteLine("¡Chau!");