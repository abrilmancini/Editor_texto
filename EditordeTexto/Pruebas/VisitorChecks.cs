using EditorTexto.Commands;
using EditorTexto.Layout;
using EditorTexto.Model;
using EditorTexto.Structure;
using EditorTexto.UI;
using EditorTexto.Visitors;

namespace EditorTexto.Pruebas;

/// <summary>
/// Responsabilidad: probar los visitors (paso 7): estadísticas y exportación.
/// </summary>
public static class VisitorChecks
{
    private static int _failures;

    public static void Run()
    {
        _failures = 0;
        EditorConfig config = new EditorConfig(lineWidth: 20, linesPerPage: 3);
        Editor editor = new Editor(config);
        editor.LoadText(
            "Hola mundo cruel, este parrafo es largo y cruza de pagina.\n" +
            "\n" +
            "Segundo parrafo.");

        DocumentStatistics stats = StatisticsVisitor.Calculate(editor.Document);
        Console.WriteLine($"  {stats}");

        Check("cuenta las palabras", stats.Words == 13);
        Check("cuenta los caracteres sin espacios",
            stats.Characters == editor.Buffer.GetText().Count(c => c != ' ' && c != '\n'));
        Check("cuenta las líneas", stats.Lines == editor.Lines.Count);
        Check("cuenta las páginas", stats.Pages == editor.Document.Pages.Count && stats.Pages >= 2);
        Check("un párrafo partido entre páginas cuenta una sola vez", stats.Paragraphs == 3);

        // Texto plano: igual a lo que se ve en el editor.
        PlainTextExporter textExporter = new PlainTextExporter(editor.Renderer, new JustifiedAlignment());
        editor.Document.Accept(textExporter);
        string text = textExporter.Result;
        Console.WriteLine("  --- documento.txt ---");
        foreach (string line in text.Split(Environment.NewLine)) Console.WriteLine("  " + line);

        string[] textLines = text.Split(Environment.NewLine);
        Check(".txt: ninguna línea supera el ancho", textLines.All(line => line.Length <= config.LineWidth));
        Check(".txt: marca cada salto de página", textLines.Count(line => line.StartsWith("--- Página")) == stats.Pages - 1);
        Check(".txt: aparecen todas las palabras en orden",
            text.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(token => token != "---" && token != "Página" && !int.TryParse(token, out _))
                .SequenceEqual(editor.Buffer.GetText().Split(new[] { ' ', '\n' }, StringSplitOptions.RemoveEmptyEntries)));

        // Markdown: cada párrafo en una línea, aunque cruce de página.
        MarkdownExporter markdownExporter = new MarkdownExporter();
        editor.Document.Accept(markdownExporter);
        string markdown = markdownExporter.Result;
        Console.WriteLine("  --- documento.md ---");
        foreach (string line in markdown.Split(Environment.NewLine)) Console.WriteLine("  " + line);
        Check(".md: el párrafo que cruza de página sale entero en una línea",
            markdown.StartsWith("Hola mundo cruel, este parrafo es largo y cruza de pagina." + Environment.NewLine));
        Check(".md: los párrafos se separan con una línea en blanco",
            markdown.Contains(Environment.NewLine + Environment.NewLine + "Segundo parrafo."));

        // Las estadísticas se actualizan al escribir y al deshacer.
        CommandHistory history = new CommandHistory();
        InputHandler input = new InputHandler(editor, history);
        editor.Buffer.SetCursor(new CursorPosition(2, editor.Buffer.GetParagraphLength(2))); // al final del documento
        foreach (char c in " nuevo") input.Handle(new ConsoleKeyInfo(c, c == ' ' ? ConsoleKey.Spacebar : ConsoleKey.A, false, false, false));
        int afterTyping = StatisticsVisitor.Calculate(editor.Document).Words;
        for (int i = 0; i < 6; i++) input.Handle(new ConsoleKeyInfo('\u001A', ConsoleKey.Z, false, false, true));
        int afterUndo = StatisticsVisitor.Calculate(editor.Document).Words;
        Check("las estadísticas cambian al escribir y vuelven al deshacer", afterTyping == 14 && afterUndo == 13);

        // F5 exporta de verdad a un archivo.
        input.Handle(new ConsoleKeyInfo('\0', ConsoleKey.F5, false, false, false));
        string path = Path.GetFullPath("documento.txt");
        Check("F5 escribe documento.txt y avisa dónde", File.Exists(path) && editor.StatusMessage.Contains(path));
        File.Delete(path);

        // Documento grande: contar sigue siendo rápido.
        editor.LoadText(string.Join("\n", Enumerable.Repeat(string.Join(" ", Enumerable.Repeat("palabra", 100)), 1000)));
        System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
        DocumentStatistics big = StatisticsVisitor.Calculate(editor.Document);
        stopwatch.Stop();
        Console.WriteLine($"  (100.000 palabras contadas en {stopwatch.ElapsedMilliseconds} ms)");
        Check("documento grande: 100.000 palabras", big.Words == 100_000);

        Console.WriteLine(_failures == 0 ? "\nTodo OK" : $"\n{_failures} prueba(s) fallaron");
    }

    private static void Check(string name, bool condition)
    {
        if (!condition) _failures++;
        Console.WriteLine($"{(condition ? "OK  " : "FAIL")}  {name}");
    }
}