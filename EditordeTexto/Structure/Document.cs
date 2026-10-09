using System.Xml.Linq;

namespace EditorTexto.Structure;

/// <summary>
/// Patrón Composite — rol: Composite raíz (contiene Page).
/// Responsabilidad: ser la raíz del documento formateado.
/// Ítem de cambio que esconde: cómo se agrupa el documento completo.
/// Nunca se edita a mano: lo arma el LayoutEngine a partir del TextBuffer.
/// </summary>
public sealed class Document : CompositeElement<Page>
{
    public IReadOnlyList<Page> Pages => Elements;

    /// <summary>Todas las líneas del documento, en orden (útil para la vista y el cursor).</summary>
    public IEnumerable<Line> Lines => Pages.SelectMany(page => page.Lines);
    public override void Accept(IDocumentVisitor visitor) => visitor.VisitDocument(this);
    public override string Describe() => $"Document ({Pages.Count} página/s)";
}