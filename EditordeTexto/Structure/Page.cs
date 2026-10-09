namespace EditorTexto.Structure;

/// <summary>
/// Patrón Composite — rol: Composite (contiene Paragraph).
/// Responsabilidad: agrupar los párrafos (o pedazos de párrafo) que entran en una página.
/// Ítem de cambio que esconde: qué contenido cae en cada página.
/// </summary>
public sealed class Page : CompositeElement<Paragraph>
{
    /// <summary>Número de página, empezando en 1.</summary>
    public int Number { get; }

    public IReadOnlyList<Paragraph> Paragraphs => Elements;

    /// <summary>Todas las líneas de la página, en orden.</summary>
    public IEnumerable<Line> Lines => Paragraphs.SelectMany(paragraph => paragraph.Lines);

    public Page(int number)
    {
        if (number < 1) throw new ArgumentOutOfRangeException(nameof(number));
        Number = number;
    }
    public override void Accept(IDocumentVisitor visitor) => visitor.VisitPage(this);
    public override string Describe() => $"Page {Number}";
}