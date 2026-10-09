using System.Xml.Linq;

namespace EditorTexto.Structure;

/// <summary>
/// Patrón Composite — rol: Composite (contiene Word).
/// Responsabilidad: agrupar las palabras que entran en una línea de ancho fijo
/// y saber qué rango de offsets de su párrafo cubre (para ubicar el cursor).
/// Ítem de cambio que esconde: qué parte del párrafo ocupa cada línea.
/// Puede no tener palabras (párrafo vacío), por eso guarda el rango explícito.
/// </summary>
public sealed class Line : CompositeElement<Word>
{
    public int ParagraphIndex { get; }

    /// <summary>Primer offset del párrafo que pertenece a esta línea.</summary>
    public int StartOffset { get; }

    /// <summary>Offset final (exclusivo); incluye el espacio donde se cortó la línea.</summary>
    public int EndOffset { get; }

    /// <summary>True si es la última línea de su párrafo (el justificado no la estira).</summary>
    public bool IsLastOfParagraph { get; }

    public IReadOnlyList<Word> Words => Elements;

    public Line(int paragraphIndex, int startOffset, int endOffset, bool isLastOfParagraph)
    {
        if (paragraphIndex < 0) throw new ArgumentOutOfRangeException(nameof(paragraphIndex));
        if (startOffset < 0 || endOffset < startOffset) throw new ArgumentOutOfRangeException(nameof(endOffset));

        ParagraphIndex = paragraphIndex;
        StartOffset = startOffset;
        EndOffset = endOffset;
        IsLastOfParagraph = isLastOfParagraph;
    }

    /// <summary>Largo de las palabras sumadas, sin espacios.</summary>
    public int WordsLength => Words.Sum(word => word.Length);

    public override void Accept(IDocumentVisitor visitor) => visitor.VisitLine(this);
    public override string Describe() =>
        $"Line [{StartOffset}..{EndOffset}){(IsLastOfParagraph ? " (última del párrafo)" : "")}";
}