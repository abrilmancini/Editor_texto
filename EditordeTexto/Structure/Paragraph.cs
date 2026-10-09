using System.Xml.Linq;

namespace EditorTexto.Structure;

/// <summary>
/// Patrón Composite — rol: Composite (contiene Line).
/// Responsabilidad: agrupar las líneas de un párrafo del TextBuffer dentro de una página.
/// Ítem de cambio que esconde: cómo se reparte un párrafo entre páginas.
/// Si un párrafo cruza un salto de página, queda en dos nodos Paragraph:
/// el segundo tiene IsContinuation = true y el mismo Index.
/// </summary>
public sealed class Paragraph : CompositeElement<Line>
{
    /// <summary>Índice del párrafo en el TextBuffer.</summary>
    public int Index { get; }

    public bool IsContinuation { get; }

    public IReadOnlyList<Line> Lines => Elements;

    public Paragraph(int index, bool isContinuation)
    {
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
        Index = index;
        IsContinuation = isContinuation;
    }
    public override void Accept(IDocumentVisitor visitor) => visitor.VisitParagraph(this);
    public override string Describe() =>
        $"Paragraph #{Index}{(IsContinuation ? " (continuación)" : "")}";
}