using System.Diagnostics;
using EditorTexto.Layout;
using EditorTexto.Model;
using EditorTexto.Structure;

namespace EditorTexto.Pruebas;

/// <summary>
/// Responsabilidad: probar el LayoutEngine (paso 3): que no corte palabras,
/// que respete el ancho, que arme bien las páginas y que sea rápido.
/// </summary>
public static class LayoutChecks
{
    private static int _failures;

    public static void Run()
    {
        _failures = 0;
        EditorConfig config = new EditorConfig(lineWidth: 20, linesPerPage: 4);
        LayoutEngine engine = new LayoutEngine(config);
        TextBuffer buffer = new TextBuffer();

        buffer.Load(
            "Estamos haciendo un editor de texto que separa el documento en paginas, parrafos, lineas y palabras.\n" +
            "\n" +
            "Las palabras nunca deben cortarse, solo cambian de linea.");

        Document document = engine.Layout(buffer);
        PrintLeftAligned(document, config.LineWidth);

        List<Line> lines = document.Lines.ToList();

        Check("ninguna línea supera el ancho",
            lines.All(line => LeftText(line).Length <= config.LineWidth));

        Check("no se pierde ni se corta ninguna palabra",
            string.Join(" ", lines.SelectMany(line => line.Words).Select(word => word.Text)) ==
            string.Join(" ", Enumerable.Range(0, buffer.ParagraphCount)
                .SelectMany(p => buffer.GetParagraph(p).Split(' ', StringSplitOptions.RemoveEmptyEntries))));

        Check("las líneas de cada párrafo cubren todo el párrafo sin huecos",
            Enumerable.Range(0, buffer.ParagraphCount).All(p =>
            {
                List<Line> ofParagraph = lines.Where(line => line.ParagraphIndex == p).ToList();
                bool contiguous = ofParagraph.Zip(ofParagraph.Skip(1)).All(pair => pair.First.EndOffset == pair.Second.StartOffset);
                return ofParagraph[0].StartOffset == 0
                    && ofParagraph[^1].EndOffset == buffer.GetParagraphLength(p)
                    && ofParagraph[^1].IsLastOfParagraph
                    && contiguous;
            }));

        Check("ninguna página tiene más líneas que el máximo",
            document.Pages.All(page => page.Lines.Count() <= config.LinesPerPage));

        Check("un párrafo partido entre páginas se marca como continuación",
            document.Pages.Skip(1).Any(page => page.Paragraphs[0].IsContinuation));

        Check("el párrafo vacío tiene una línea sin palabras",
            lines.Count(line => line.ParagraphIndex == 1) == 1 && lines.First(line => line.ParagraphIndex == 1).Words.Count == 0);

        buffer.Load("12345678901234567890 abc");
        List<Line> exact = engine.Layout(buffer).Lines.ToList();
        Check("una palabra de exactamente el ancho entra sola en su línea",
            exact.Count == 2 && LeftText(exact[0]) == "12345678901234567890");

        buffer.Load("");
        Document empty = engine.Layout(buffer);
        Check("un documento vacío tiene 1 página y 1 línea", empty.Pages.Count == 1 && empty.Lines.Count() == 1);

        // Documento grande: 100.000 palabras en 1.000 párrafos.
        string bigParagraph = string.Join(" ", Enumerable.Repeat("palabra", 100));
        buffer.Load(string.Join("\n", Enumerable.Repeat(bigParagraph, 1000)));
        LayoutEngine realEngine = new LayoutEngine(new EditorConfig());
        Stopwatch stopwatch = Stopwatch.StartNew();
        Document big = realEngine.Layout(buffer);
        stopwatch.Stop();
        Console.WriteLine($"  (100.000 palabras → {big.Pages.Count} páginas en {stopwatch.ElapsedMilliseconds} ms)");
        Check("un documento de 100.000 palabras se acomoda en menos de 500 ms", stopwatch.ElapsedMilliseconds < 500);

        Console.WriteLine(_failures == 0 ? "\nTodo OK" : $"\n{_failures} prueba(s) fallaron");
    }

    /// <summary>Texto de la línea alineado a izquierda (provisorio hasta el paso 4).</summary>
    private static string LeftText(Line line) => string.Join(" ", line.Words.Select(word => word.Text));

    private static void PrintLeftAligned(Document document, int width)
    {
        foreach (Page page in document.Pages)
        {
            Console.WriteLine($"--- Página {page.Number} " + new string('-', Math.Max(0, width - 11)));
            foreach (Line line in page.Lines)
                Console.WriteLine($"|{LeftText(line).PadRight(width)}|");
        }
        Console.WriteLine();
    }

    private static void Check(string name, bool condition)
    {
        if (!condition) _failures++;
        Console.WriteLine($"{(condition ? "OK  " : "FAIL")}  {name}");
    }
}