using EditorTexto.Layout;
using EditorTexto.Model;
using EditorTexto.Structure;

namespace EditorTexto.Pruebas;

/// <summary>
/// Responsabilidad: probar las cuatro alineaciones (paso 4) y el cálculo de columnas del cursor.
/// </summary>
public static class AlignmentChecks
{
    private static int _failures;

    public static void Run()
    {
        _failures = 0;
        EditorConfig config = new EditorConfig(lineWidth: 30, linesPerPage: 20);
        TextBuffer buffer = new TextBuffer();
        buffer.Load(
            "Nos gustaria tambien poder alinear el texto a izquierda, derecha, centrado y justificado.\n" +
            "\n" +
            "Las palabras nunca deben cortarse.");

        Document document = new LayoutEngine(config).Layout(buffer);
        List<Line> lines = document.Lines.ToList();
        LineRenderer renderer = new LineRenderer(config.LineWidth);

        IAlignmentStrategy[] strategies =
        {
            new LeftAlignment(),
            new RightAlignment(),
            new CenterAlignment(),
            new JustifiedAlignment()
        };

        // El mismo documento, dibujado con cada estrategia: el código que dibuja no cambia.
        foreach (IAlignmentStrategy strategy in strategies)
        {
            Console.WriteLine($"--- {strategy.Name} ".PadRight(config.LineWidth + 2, '-'));
            foreach (Line line in lines)
                Console.WriteLine($"|{renderer.Render(line, strategy)}|");
            Console.WriteLine();
        }

        foreach (IAlignmentStrategy strategy in strategies)
        {
            Check($"{strategy.Name}: todas las líneas miden exactamente el ancho",
                lines.All(line => renderer.Render(line, strategy).Length == config.LineWidth));

            Check($"{strategy.Name}: las palabras salen en el mismo orden y enteras",
                lines.All(line =>
                    renderer.Render(line, strategy).Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        .SequenceEqual(line.Words.Select(word => word.Text))));

            Check($"{strategy.Name}: el cursor al inicio de cada palabra cae sobre esa palabra",
                lines.All(line => line.Words.All(word =>
                {
                    int column = renderer.ColumnOf(line, strategy, word.StartOffset);
                    return renderer.Render(line, strategy).Substring(column).StartsWith(word.Text);
                })));
        }

        IAlignmentStrategy justified = strategies[3];
        Check("Justificado: las líneas no finales llegan hasta el borde derecho",
            lines.Where(line => !line.IsLastOfParagraph && line.Words.Count > 1)
                 .All(line => renderer.Render(line, justified)[^1] != ' '));
        Check("Justificado: la última línea del párrafo queda a izquierda",
            lines.Where(line => line.IsLastOfParagraph)
                 .All(line => renderer.Render(line, justified) == renderer.Render(line, strategies[0])));

        IAlignmentStrategy right = strategies[1];
        Check("Derecha: las líneas con palabras terminan en el borde",
            lines.Where(line => line.Words.Count > 0).All(line => renderer.Render(line, right)[^1] != ' '));

        IAlignmentStrategy center = strategies[2];
        Check("Centrado: la diferencia entre margen izquierdo y derecho es 0 o 1",
            lines.Where(line => line.Words.Count > 0).All(line =>
            {
                string text = renderer.Render(line, center);
                int leftMargin = text.Length - text.TrimStart().Length;
                int rightMargin = text.Length - text.TrimEnd().Length;
                return rightMargin - leftMargin is 0 or 1;
            }));

        Check("el cursor al final de una línea vacía no se sale del ancho",
            strategies.All(strategy => renderer.ColumnOf(lines.First(line => line.Words.Count == 0), strategy, 0) <= config.LineWidth));

        Console.WriteLine(_failures == 0 ? "\nTodo OK" : $"\n{_failures} prueba(s) fallaron");
    }

    private static void Check(string name, bool condition)
    {
        if (!condition) _failures++;
        Console.WriteLine($"{(condition ? "OK  " : "FAIL")}  {name}");
    }
}