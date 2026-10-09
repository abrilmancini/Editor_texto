using EditorTexto.Structure;

namespace EditorTexto.Visitors;

/// <summary>Resultado del StatisticsVisitor.</summary>
public sealed record DocumentStatistics(int Words, int Characters, int Lines, int Paragraphs, int Pages);

/// <summary>
/// Patrón Visitor — rol: ConcreteVisitor.
/// Responsabilidad: recorrer el documento y contar palabras, caracteres (sin espacios),
/// líneas, párrafos y páginas.
/// Ítem de cambio que esconde: qué métricas se calculan y cómo se cuentan
/// (por ejemplo, un párrafo partido entre dos páginas cuenta una sola vez).
/// </summary>
public sealed class StatisticsVisitor : IDocumentVisitor
{
    private int _words;
    private int _characters;
    private int _lines;
    private int _paragraphs;
    private int _pages;

    /// <summary>Atajo: recorre el documento y devuelve las estadísticas.</summary>
    public static DocumentStatistics Calculate(Document document)
    {
        StatisticsVisitor visitor = new StatisticsVisitor();
        document.Accept(visitor);
        return visitor.Result;
    }

    public DocumentStatistics Result => new DocumentStatistics(_words, _characters, _lines, _paragraphs, _pages);

    public void VisitDocument(Document document) => VisitChildren(document);

    public void VisitPage(Page page)
    {
        _pages++;
        VisitChildren(page);
    }

    public void VisitParagraph(Paragraph paragraph)
    {
        if (!paragraph.IsContinuation) // la continuación en otra página es el mismo párrafo
            _paragraphs++;
        VisitChildren(paragraph);
    }

    public void VisitLine(Line line)
    {
        _lines++;
        VisitChildren(line);
    }

    public void VisitWord(Word word)
    {
        _words++;
        _characters += word.Length;
    }

    private void VisitChildren(DocumentElement element)
    {
        foreach (DocumentElement child in element.Children)
            child.Accept(this);
    }
}