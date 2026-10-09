using EditorTexto.Model;
using EditorTexto.Structure;

namespace EditorTexto.Layout;

/// <summary>
/// Responsabilidad: construir el árbol Document (Composite) a partir del TextBuffer:
/// parte cada párrafo en palabras, acomoda las palabras en líneas de ancho fijo
/// sin cortarlas nunca y agrupa las líneas en páginas.
/// Ítem de cambio que esconde: el algoritmo de corte de líneas y de páginas.
/// La alineación NO se decide acá: en el paso 4 la aplica una estrategia (Strategy).
/// </summary>
public sealed class LayoutEngine
{
    private readonly EditorConfig _config;

    public LayoutEngine(EditorConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    /// <summary>Arma el documento completo. Se llama después de cada cambio.</summary>
    public Document Layout(TextBuffer buffer)
    {
        Document document = new Document();
        Page page = new Page(1);
        document.Add(page);
        int linesInPage = 0;

        for (int paragraphIndex = 0; paragraphIndex < buffer.ParagraphCount; paragraphIndex++)
        {
            List<Line> lines = BreakIntoLines(paragraphIndex, buffer.GetParagraph(paragraphIndex));
            Paragraph? currentParagraph = null;

            for (int i = 0; i < lines.Count; i++)
            {
                if (linesInPage == _config.LinesPerPage)
                {
                    page = new Page(page.Number + 1);
                    document.Add(page);
                    linesInPage = 0;
                    currentParagraph = null; // el párrafo sigue en un nodo nuevo en la página nueva
                }

                if (currentParagraph is null)
                {
                    currentParagraph = new Paragraph(paragraphIndex, isContinuation: i > 0);
                    page.Add(currentParagraph);
                }

                currentParagraph.Add(lines[i]);
                linesInPage++;
            }
        }

        return document;
    }

    /// <summary>
    /// Separa un párrafo en palabras. Cualquier grupo de espacios cuenta como un separador.
    /// Cada palabra recuerda en qué offset del párrafo empieza.
    /// </summary>
    public static List<Word> SplitIntoWords(string text)
    {
        List<Word> words = new List<Word>();
        int i = 0;

        while (i < text.Length)
        {
            while (i < text.Length && text[i] == ' ') i++;   // saltear espacios
            int start = i;
            while (i < text.Length && text[i] != ' ') i++;   // avanzar sobre la palabra
            if (i > start)
                words.Add(new Word(text.Substring(start, i - start), start));
        }

        return words;
    }

    /// <summary>
    /// Corte de líneas "greedy": se agregan palabras mientras
    /// largo de las palabras + 1 espacio entre cada una no supere el ancho.
    /// Una palabra nunca se corta. (Si una sola palabra fuera más larga que el ancho,
    /// queda sola en su línea; los comandos del paso 6 van a impedir que eso pase.)
    /// </summary>
    private List<Line> BreakIntoLines(int paragraphIndex, string text)
    {
        List<Word> words = SplitIntoWords(text);

        // Párrafo vacío (o solo espacios): una línea sin palabras donde puede estar el cursor.
        if (words.Count == 0)
            return new List<Line> { new Line(paragraphIndex, 0, text.Length, isLastOfParagraph: true) };

        List<List<Word>> groups = new List<List<Word>>();
        List<Word> current = new List<Word>();
        int currentLength = 0;

        foreach (Word word in words)
        {
            bool fits = current.Count == 0 || currentLength + 1 + word.Length <= _config.LineWidth;
            if (!fits)
            {
                groups.Add(current);
                current = new List<Word>();
                currentLength = 0;
            }

            currentLength = current.Count == 0 ? word.Length : currentLength + 1 + word.Length;
            current.Add(word);
        }
        groups.Add(current);

        // Cada línea cubre desde el inicio de su primera palabra hasta el inicio de la
        // primera palabra de la siguiente (así los espacios del corte quedan en la línea de arriba).
        // La primera empieza en 0 y la última termina al final del párrafo: no quedan huecos.
        List<Line> lines = new List<Line>();
        for (int i = 0; i < groups.Count; i++)
        {
            bool isLast = i == groups.Count - 1;
            int start = i == 0 ? 0 : groups[i][0].StartOffset;
            int end = isLast ? text.Length : groups[i + 1][0].StartOffset;

            Line line = new Line(paragraphIndex, start, end, isLast);
            foreach (Word word in groups[i]) line.Add(word);
            lines.Add(line);
        }

        return lines;
    }
}