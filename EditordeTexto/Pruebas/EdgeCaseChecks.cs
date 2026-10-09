using System.Diagnostics;
using EditorTexto.Commands;
using EditorTexto.Layout;
using EditorTexto.Model;
using EditorTexto.Structure;
using EditorTexto.UI;
using EditorTexto.Visitors;

namespace EditorTexto.Pruebas;

/// <summary>
/// Responsabilidad: probar casos borde (paso 8) y que nada se rompa con miles de teclas al azar.
/// </summary>
public static class EdgeCaseChecks
{
    private static int _failures;

    public static void Run()
    {
        _failures = 0;

        // --- Documento vacío: ninguna tecla tiene que romper nada ---
        (Editor editor, InputHandler input, ConsoleView view) = Create(width: 10, perPage: 3);
        bool emptyOk = true;
        foreach (ConsoleKey key in new[]
                 {
                     ConsoleKey.Backspace, ConsoleKey.Delete, ConsoleKey.LeftArrow, ConsoleKey.RightArrow,
                     ConsoleKey.UpArrow, ConsoleKey.DownArrow, ConsoleKey.Home, ConsoleKey.End,
                     ConsoleKey.F2, ConsoleKey.F3, ConsoleKey.F4, ConsoleKey.F1
                 })
        {
            emptyOk &= TryPress(input, key) && TryFrame(view);
        }
        CtrlZ(input);
        Check("documento vacío: todas las teclas funcionan sin errores", emptyOk && editor.Buffer.GetText() == "");

        // --- Palabra de exactamente el ancho ---
        Type(input, "0123456789");
        Check("palabra de exactamente el ancho: entra", editor.Buffer.GetText() == "0123456789" && editor.Lines.Count == 1);
        Type(input, "x");
        Check("una letra más: se rechaza y avisa", editor.Buffer.GetText() == "0123456789" && editor.StatusMessage.Length > 0);
        Type(input, " ok");
        Check("después de un espacio se puede seguir escribiendo (pasa a la línea siguiente)",
            editor.Buffer.GetText() == "0123456789 ok" && editor.Lines.Count == 2);

        // --- Unir palabras con Supr o con Backspace al inicio de párrafo ---
        editor.LoadText("abcdefgh\nijk");
        editor.Buffer.SetCursor(new CursorPosition(1, 0));
        Press(input, ConsoleKey.Backspace);
        Check("Backspace no une párrafos si la palabra resultante no entra",
            editor.Buffer.ParagraphCount == 2 && editor.StatusMessage.Length > 0);
        editor.Buffer.SetCursor(new CursorPosition(0, 8));
        Press(input, ConsoleKey.Delete);
        Check("Supr tampoco", editor.Buffer.ParagraphCount == 2);

        // --- Borrar al inicio y al final del documento ---
        editor.LoadText("hola");
        Press(input, ConsoleKey.Backspace);
        Check("Backspace al inicio del documento no hace nada", editor.Buffer.GetText() == "hola");
        Press(input, ConsoleKey.End);
        Press(input, ConsoleKey.Delete);
        Check("Supr al final del documento no hace nada", editor.Buffer.GetText() == "hola");

        // --- Enter en medio de una palabra y deshacer ---
        editor.LoadText("palabra");
        editor.Buffer.SetCursor(new CursorPosition(0, 3));
        Press(input, ConsoleKey.Enter);
        Check("Enter en medio de una palabra la parte en dos párrafos", editor.Buffer.GetText() == "pal\nabra");
        CtrlZ(input);
        Check("deshacer la vuelve a unir", editor.Buffer.GetText() == "palabra" && editor.Buffer.Cursor == new CursorPosition(0, 3));

        // --- Deshacer después de unir párrafos con Supr ---
        editor.LoadText("uno\ndos");
        editor.Buffer.SetCursor(new CursorPosition(0, 3));
        Press(input, ConsoleKey.Delete);
        CtrlZ(input);
        Check("deshacer una unión de párrafos los vuelve a separar", editor.Buffer.GetText() == "uno\ndos");

        // --- Cursor al final de una línea llena con cada alineación ---
        editor.LoadText("0123456789 abc");
        editor.Buffer.SetCursor(new CursorPosition(0, 10));
        bool fullLineOk = true;
        foreach (ConsoleKey alignment in new[] { ConsoleKey.F1, ConsoleKey.F2, ConsoleKey.F3, ConsoleKey.F4 })
        {
            Press(input, alignment);
            Frame frame = view.BuildFrame();
            fullLineOk &= frame.CursorColumn <= editor.Config.LineWidth + 1;
        }
        Check("cursor al final de una línea llena: no se sale del recuadro", fullLineOk);

        // --- 20.000 teclas al azar con invariantes ---
        Check("20.000 teclas al azar sin errores y con todas las reglas cumplidas", Fuzz(20_000));

        // --- Documento grande: cada tecla tiene que ser rápida ---
        (Editor big, InputHandler bigInput, ConsoleView bigView) = Create(width: 50, perPage: 20);
        big.LoadText(string.Join("\n", Enumerable.Repeat(string.Join(" ", Enumerable.Repeat("palabra", 100)), 1000)));
        for (int i = 0; i < 500; i++) PressQuiet(bigInput, ConsoleKey.DownArrow); // ir a la mitad
        Stopwatch stopwatch = Stopwatch.StartNew();
        const int keystrokes = 20;
        for (int i = 0; i < keystrokes; i++)
        {
            Type(bigInput, "x");
            bigView.BuildFrame(); // lo mismo que pasa en cada tecla real: editar + dibujar
        }
        stopwatch.Stop();
        long perKey = stopwatch.ElapsedMilliseconds / keystrokes;
        Console.WriteLine($"  (documento de 100.000 palabras: {perKey} ms por tecla)");
        Check("documento de 100.000 palabras: cada tecla tarda menos de 300 ms", perKey < 300);

        Console.WriteLine(_failures == 0 ? "\nTodo OK" : $"\n{_failures} prueba(s) fallaron");
    }

    /// <summary>
    /// Aprieta teclas al azar y después de cada una verifica que:
    /// no haya excepciones, el cursor sea válido y esté dentro del recuadro,
    /// ninguna línea supere el ancho, ninguna palabra supere el ancho
    /// y las estadísticas coincidan con el texto.
    /// </summary>
    private static bool Fuzz(int count)
    {
        (Editor editor, InputHandler input, ConsoleView view) = Create(width: 12, perPage: 4);
        Random random = new Random(42);
        ConsoleKey[] keys =
        {
            ConsoleKey.A, ConsoleKey.A, ConsoleKey.A, ConsoleKey.A, ConsoleKey.A, ConsoleKey.Spacebar, ConsoleKey.Spacebar,
            ConsoleKey.Enter, ConsoleKey.Backspace, ConsoleKey.Backspace, ConsoleKey.Delete,
            ConsoleKey.LeftArrow, ConsoleKey.RightArrow, ConsoleKey.UpArrow, ConsoleKey.DownArrow,
            ConsoleKey.Home, ConsoleKey.End, ConsoleKey.F1, ConsoleKey.F2, ConsoleKey.F3, ConsoleKey.F4, ConsoleKey.Z
        };

        for (int i = 0; i < count; i++)
        {
            ConsoleKey key = keys[random.Next(keys.Length)];
            try
            {
                if (key == ConsoleKey.Z) CtrlZ(input);
                else if (key == ConsoleKey.A) PressQuiet(input, key, "abcñáé"[random.Next(6)]);
                else if (key == ConsoleKey.Spacebar) PressQuiet(input, key, ' ');
                else PressQuiet(input, key);

                Frame frame = view.BuildFrame();
                int width = editor.Config.LineWidth;
                CursorPosition cursor = editor.Buffer.Cursor;
                DocumentStatistics stats = StatisticsVisitor.Calculate(editor.Document);
                string[] realWords = editor.Buffer.GetText().Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                bool ok = cursor.Paragraph < editor.Buffer.ParagraphCount
                          && cursor.Offset <= editor.Buffer.GetParagraphLength(cursor.Paragraph)
                          && frame.CursorRow >= 0 && frame.CursorRow < frame.Rows.Count
                          && frame.CursorColumn >= 1 && frame.CursorColumn <= width + 1
                          && editor.Lines.All(line => editor.Renderer.Render(line, editor.Alignment).Length == width)
                          && realWords.All(word => word.Length <= width)
                          && stats.Words == realWords.Length
                          && stats.Paragraphs == editor.Buffer.ParagraphCount;
                if (!ok)
                {
                    Console.WriteLine($"  Falla en la tecla {i} ({key}). Texto: \"{editor.Buffer.GetText().Replace("\n", "\\n")}\"");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  Excepción en la tecla {i} ({key}): {ex.Message}");
                return false;
            }
        }
        return true;
    }

    private static (Editor, InputHandler, ConsoleView) Create(int width, int perPage)
    {
        Editor editor = new Editor(new EditorConfig(lineWidth: width, linesPerPage: perPage, visibleLines: 6));
        return (editor, new InputHandler(editor, new CommandHistory()), new ConsoleView(editor));
    }

    private static bool TryPress(InputHandler input, ConsoleKey key)
    {
        try { PressQuiet(input, key); return true; }
        catch (Exception ex) { Console.WriteLine($"  {key}: {ex.Message}"); return false; }
    }

    private static bool TryFrame(ConsoleView view)
    {
        try { view.BuildFrame(); return true; }
        catch (Exception ex) { Console.WriteLine($"  vista: {ex.Message}"); return false; }
    }

    private static void Type(InputHandler input, string text)
    {
        foreach (char c in text)
            PressQuiet(input, c == ' ' ? ConsoleKey.Spacebar : ConsoleKey.A, c);
    }

    private static void Press(InputHandler input, ConsoleKey key) => PressQuiet(input, key);

    private static void PressQuiet(InputHandler input, ConsoleKey key, char character = '\0') =>
        input.Handle(new ConsoleKeyInfo(character, key, false, false, false));

    private static void CtrlZ(InputHandler input) =>
        input.Handle(new ConsoleKeyInfo('\u001A', ConsoleKey.Z, false, false, true));

    private static void Check(string name, bool condition)
    {
        if (!condition) _failures++;
        Console.WriteLine($"{(condition ? "OK  " : "FAIL")}  {name}");
    }
}