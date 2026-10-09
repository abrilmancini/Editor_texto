using EditorTexto.Commands;
using EditorTexto.Model;
using EditorTexto.UI;

namespace EditorTexto.Pruebas;

/// <summary>
/// Responsabilidad: probar los comandos y el CTRL+Z (paso 6).
/// </summary>
public static class CommandChecks
{
    private static int _failures;

    private sealed record Snapshot(string Text, CursorPosition Cursor, string Alignment);

    public static void Run()
    {
        _failures = 0;
        Editor editor = new Editor(new EditorConfig(lineWidth: 20, linesPerPage: 3));
        CommandHistory history = new CommandHistory();
        InputHandler input = new InputHandler(editor, history);

        // --- Casos puntuales ---
        Type(input, "hola mundo");
        Press(input, ConsoleKey.LeftArrow);
        Press(input, ConsoleKey.LeftArrow);
        Press(input, ConsoleKey.LeftArrow);
        Press(input, ConsoleKey.Enter);             // "hola mu" | "ndo"
        Press(input, ConsoleKey.F4);
        Press(input, ConsoleKey.Backspace);         // vuelve a unir
        Press(input, ConsoleKey.Delete);            // borra la "n"
        Check("el texto quedó como se esperaba", editor.Buffer.GetText() == "hola mudo");

        CtrlZ(input);
        Check("deshacer Supr devuelve la letra y el cursor", editor.Buffer.GetText() == "hola mundo" && editor.Buffer.Cursor == new CursorPosition(0, 7));
        CtrlZ(input);
        Check("deshacer Backspace vuelve a partir el párrafo", editor.Buffer.GetText() == "hola mu\nndo" && editor.Buffer.Cursor == new CursorPosition(1, 0));
        CtrlZ(input);
        Check("deshacer F4 vuelve a la alineación anterior", editor.Alignment.Name == "Izquierda");
        CtrlZ(input);
        Check("deshacer Enter vuelve a unir el párrafo", editor.Buffer.GetText() == "hola mundo" && editor.Buffer.Cursor == new CursorPosition(0, 7));
        for (int i = 0; i < 10; i++) CtrlZ(input);
        Check("deshaciendo todo se llega al documento vacío", editor.Buffer.GetText() == "" && !history.CanUndo);
        CtrlZ(input);
        Check("CTRL+Z sin nada para deshacer avisa y no rompe", editor.StatusMessage.Length > 0);

        Press(input, ConsoleKey.Backspace);
        Check("una operación sin efecto no se guarda en el historial", history.Count == 0);

        // --- Prueba con 2000 teclas al azar: después de deshacer todo, cada estado tiene que coincidir ---
        editor.LoadText("");
        history.Clear();
        Random random = new Random(1234);
        ConsoleKey[] keys =
        {
            ConsoleKey.A, ConsoleKey.A, ConsoleKey.A, ConsoleKey.A, ConsoleKey.Spacebar, ConsoleKey.Spacebar,
            ConsoleKey.Enter, ConsoleKey.Backspace, ConsoleKey.Delete,
            ConsoleKey.LeftArrow, ConsoleKey.RightArrow, ConsoleKey.UpArrow, ConsoleKey.DownArrow,
            ConsoleKey.Home, ConsoleKey.End, ConsoleKey.F1, ConsoleKey.F2, ConsoleKey.F3, ConsoleKey.F4
        };

        Stack<(Snapshot Before, bool AlignmentOnly)> expected = new();
        for (int i = 0; i < 2000; i++)
        {
            ConsoleKey key = keys[random.Next(keys.Length)];
            char character = key switch
            {
                ConsoleKey.A => "abcdeñá"[random.Next(7)],
                ConsoleKey.Spacebar => ' ',
                _ => '\0'
            };

            Snapshot before = Take(editor);
            int countBefore = history.Count;
            Press(input, key, character);
            if (history.Count > countBefore)
                expected.Push((before, key is >= ConsoleKey.F1 and <= ConsoleKey.F4));
        }

        int commandsRecorded = expected.Count;
        bool allMatch = true;
        while (expected.Count > 0)
        {
            (Snapshot before, bool alignmentOnly) = expected.Pop();
            CtrlZ(input);
            Snapshot now = Take(editor);
            bool same = now.Text == before.Text && now.Alignment == before.Alignment
                        && (alignmentOnly || now.Cursor == before.Cursor);
            if (!same)
            {
                allMatch = false;
                break;
            }
        }
        Console.WriteLine($"  ({commandsRecorded} comandos guardados en el historial)");
        Check("2000 teclas al azar: deshacer restaura cada estado (texto, cursor y alineación)", allMatch && commandsRecorded > 500);
        Check("después de deshacer todo, el documento queda vacío", editor.Buffer.GetText() == "");

        Console.WriteLine(_failures == 0 ? "\nTodo OK" : $"\n{_failures} prueba(s) fallaron");
    }

    private static Snapshot Take(Editor editor) =>
        new Snapshot(editor.Buffer.GetText(), editor.Buffer.Cursor, editor.Alignment.Name);

    private static void Type(InputHandler input, string text)
    {
        foreach (char c in text)
            Press(input, c == ' ' ? ConsoleKey.Spacebar : ConsoleKey.A, c);
    }

    private static void CtrlZ(InputHandler input) =>
        input.Handle(new ConsoleKeyInfo('\u001A', ConsoleKey.Z, false, false, true));

    private static void Press(InputHandler input, ConsoleKey key, char character = '\0') =>
        input.Handle(new ConsoleKeyInfo(character, key, false, false, false));

    private static void Check(string name, bool condition)
    {
        if (!condition) _failures++;
        Console.WriteLine($"{(condition ? "OK  " : "FAIL")}  {name}");
    }
}