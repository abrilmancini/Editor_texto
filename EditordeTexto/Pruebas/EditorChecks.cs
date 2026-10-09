using EditorTexto.Layout;
using EditorTexto.Model;
using EditorTexto.UI;
using EditorTexto.Commands;

namespace EditorTexto.Pruebas;

/// <summary>
/// Responsabilidad: probar el editor completo (paso 5) simulando teclas, sin abrir la consola.
/// </summary>
public static class EditorChecks
{
    private static int _failures;

    public static void Run()
    {
        _failures = 0;
        Editor editor = new Editor(new EditorConfig(lineWidth: 20, linesPerPage: 3, visibleLines: 6));
        InputHandler input = new InputHandler(editor, new CommandHistory());
        ConsoleView view = new ConsoleView(editor);

        Type(input, "hola mundo");
        Check("escribir con el teclado", editor.Buffer.GetText() == "hola mundo");

        Press(input, ConsoleKey.Spacebar, ' ');
        Press(input, ConsoleKey.Spacebar, ' ');
        Check("un espacio doble se ignora", editor.Buffer.GetText() == "hola mundo ");

        Type(input, "esto es un texto largo que ocupa varias lineas");
        Check("el texto se reparte en varias líneas", editor.Lines.Count > 1);

        Press(input, ConsoleKey.Home);
        Check("Inicio va al inicio de la línea visual",
            editor.Buffer.Cursor.Offset == editor.CursorLine.StartOffset && editor.CursorLineIndex > 0);

        Press(input, ConsoleKey.UpArrow);
        Check("flecha arriba sube una línea", editor.CursorLineIndex == editor.Lines.Count - 2);

        Press(input, ConsoleKey.End);
        Check("Fin deja el cursor en la misma línea", editor.CursorLineIndex == editor.Lines.Count - 2);

        Press(input, ConsoleKey.DownArrow);
        Check("flecha abajo baja una línea", editor.CursorLineIndex == editor.Lines.Count - 1);

        editor.Buffer.SetCursor(new CursorPosition(0, 4)); // justo antes del espacio de "hola mundo"
        Press(input, ConsoleKey.Spacebar, ' ');
        Check("espacio antes de un espacio lo saltea en vez de duplicarlo",
            editor.Buffer.Cursor.Offset == 5 && editor.Buffer.GetText().StartsWith("hola mundo"));

        Press(input, ConsoleKey.F4);
        Check("F4 cambia a justificado", editor.Alignment is JustifiedAlignment);

        // El carácter que se ve debajo del cursor tiene que ser el del buffer en esa posición.
        editor.Buffer.SetCursor(new CursorPosition(0, 5)); // sobre la "m" de "mundo"
        Frame frame = view.BuildFrame();
        Check("el cursor se dibuja sobre el carácter correcto",
            frame.Rows[frame.CursorRow][frame.CursorColumn] == 'm');

        Press(input, ConsoleKey.Enter);
        Check("Enter parte el párrafo", editor.Buffer.ParagraphCount == 2 && editor.Buffer.GetParagraph(1).StartsWith("mundo"));

        Press(input, ConsoleKey.Backspace);
        Check("Backspace al inicio vuelve a unir", editor.Buffer.ParagraphCount == 1);

        editor.LoadText("");
        Type(input, new string('x', 20));
        Press(input, ConsoleKey.X, 'x');
        Check("no deja escribir una palabra más larga que el ancho",
            editor.Buffer.GetText().Length == 20 && editor.StatusMessage.Length > 0);

        editor.LoadText("12345678901 1234567890");
        editor.Buffer.SetCursor(new CursorPosition(0, 12));
        Press(input, ConsoleKey.Backspace);
        Check("no deja borrar un espacio si la palabra unida no entra",
            editor.Buffer.GetText() == "12345678901 1234567890");

        editor.LoadText(string.Join("\n", Enumerable.Range(1, 10).Select(i => $"parrafo {i}")));
        for (int i = 0; i < 9; i++) Press(input, ConsoleKey.DownArrow);
        frame = view.BuildFrame();
        Check("la vista reducida sigue al cursor hasta el final",
            frame.Rows.Any(row => row.Contains("parrafo 10")) && !frame.Rows.Any(row => row.Contains("parrafo 1 ")));
        Check("hay separadores de página", frame.Rows.Any(row => row.Contains("Página 4")));
        Check("el encabezado muestra la página actual", frame.Rows[0].Contains("Página 4/4"));

        Check("Escape sale", input.Handle(new ConsoleKeyInfo('\u001b', ConsoleKey.Escape, false, false, false)) == false);

        Console.WriteLine(_failures == 0 ? "\nTodo OK" : $"\n{_failures} prueba(s) fallaron");
    }

    private static void Type(InputHandler input, string text)
    {
        foreach (char c in text)
            Press(input, c == ' ' ? ConsoleKey.Spacebar : ConsoleKey.A, c);
    }

    private static void Press(InputHandler input, ConsoleKey key, char character = '\0') =>
        input.Handle(new ConsoleKeyInfo(character, key, false, false, false));

    private static void Check(string name, bool condition)
    {
        if (!condition) _failures++;
        Console.WriteLine($"{(condition ? "OK  " : "FAIL")}  {name}");
    }
}