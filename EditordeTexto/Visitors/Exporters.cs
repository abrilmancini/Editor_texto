using System.Text;
using EditorTexto.Layout;
using EditorTexto.Structure;

namespace EditorTexto.Visitors;

/// <summary>
/// Patrón Visitor — rol: ConcreteVisitor.
/// Responsabilidad: exportar el documento como texto plano, tal como se ve en el editor:
/// con el ancho fijo, la alineación actual y una marca entre páginas.
/// Ítem de cambio que esconde: el formato de salida .txt.
/// </summary>
public sealed class PlainTextExporter : IDocumentVisitor
{
    private readonly LineRenderer _renderer;
    private readonly IAlignmentStrategy _alignment;
    private readonly StringBuilder _output = new();

    public PlainTextExporter(LineRenderer renderer, IAlignmentStrategy alignment)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _alignment = alignment ?? throw new ArgumentNullException(nameof(alignment));
    }

    public string Result => _output.ToString();

    public void VisitDocument(Document document)
    {
        foreach (DocumentElement page in document.Children)
            page.Accept(this);
    }

    public void VisitPage(Page page)
    {
        if (page.Number > 1)
            _output.AppendLine().AppendLine($"--- Página {page.Number} ---").AppendLine();

        foreach (DocumentElement paragraph in page.Children)
            paragraph.Accept(this);
    }

    public void VisitParagraph(Paragraph paragraph)
    {
        foreach (DocumentElement line in paragraph.Children)
            line.Accept(this);
    }

    public void VisitLine(Line line) =>
        _output.AppendLine(_renderer.Render(line, _alignment).TrimEnd());

    public void VisitWord(Word word)
    {
        // No se usa: la línea se dibuja entera con el LineRenderer, que ya conoce sus palabras.
    }
}

/// <summary>
/// Patrón Visitor — rol: ConcreteVisitor.
/// Responsabilidad: exportar el documento a Markdown: cada párrafo en una sola línea
/// (Markdown hace su propio corte de línea) y una línea en blanco entre párrafos.
/// Ítem de cambio que esconde: el formato de salida .md.
/// Muestra la ventaja del Visitor: un formato nuevo es una clase nueva, sin tocar el documento.
/// </summary>
public sealed class MarkdownExporter : IDocumentVisitor
{
    private readonly List<List<string>> _paragraphs = new();

    public string Result =>
        string.Join(Environment.NewLine + Environment.NewLine,
            _paragraphs.Where(words => words.Count > 0)          // los párrafos vacíos no suman nada en Markdown
                       .Select(words => string.Join(" ", words))) + Environment.NewLine;

    public void VisitDocument(Document document)
    {
        foreach (DocumentElement page in document.Children)
            page.Accept(this);
    }

    public void VisitPage(Page page)
    {
        foreach (DocumentElement paragraph in page.Children)
            paragraph.Accept(this);
    }

    public void VisitParagraph(Paragraph paragraph)
    {
        // Un párrafo partido entre páginas se exporta junto: la continuación no abre uno nuevo.
        if (!paragraph.IsContinuation || _paragraphs.Count == 0)
            _paragraphs.Add(new List<string>());

        foreach (DocumentElement line in paragraph.Children)
            line.Accept(this);
    }

    public void VisitLine(Line line)
    {
        foreach (DocumentElement word in line.Children)
            word.Accept(this);
    }

    public void VisitWord(Word word) => _paragraphs[^1].Add(word.Text);
}