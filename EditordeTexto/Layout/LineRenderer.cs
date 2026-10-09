using System.Text;
using EditorTexto.Structure;

namespace EditorTexto.Layout;

/// <summary>
/// Patrón Strategy — rol: Context.
/// Responsabilidad: convertir una Line en un string de ancho fijo usando la estrategia
/// de alineación que le pasen, y traducir un offset del párrafo a la columna donde
/// se dibuja (para ubicar el cursor en el paso 5).
/// Ítem de cambio que esconde: cómo se dibuja una línea y dónde cae el cursor.
/// </summary>
public sealed class LineRenderer
{
    private readonly int _width;

    public LineRenderer(int width)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        _width = width;
    }

    /// <summary>Texto de la línea, siempre con exactamente <c>width</c> caracteres (rellena con espacios).</summary>
    public string Render(Line line, IAlignmentStrategy alignment)
    {
        LineSpacing spacing = alignment.ComputeSpacing(line, _width);
        StringBuilder text = new StringBuilder();
        text.Append(' ', spacing.LeftPadding);

        for (int i = 0; i < line.Words.Count; i++)
        {
            if (i > 0) text.Append(' ', spacing.Gaps[i - 1]);
            text.Append(line.Words[i].Text);
        }

        return text.ToString().PadRight(_width);
    }

    /// <summary>
    /// Columna (0 = borde izquierdo) donde se dibuja el cursor si está en <paramref name="offset"/>.
    /// El offset tiene que pertenecer a la línea (entre StartOffset y EndOffset).
    /// </summary>
    public int ColumnOf(Line line, IAlignmentStrategy alignment, int offset)
    {
        LineSpacing spacing = alignment.ComputeSpacing(line, _width);
        int column = spacing.LeftPadding;

        if (line.Words.Count == 0 || offset <= line.Words[0].StartOffset)
            return Math.Min(column, _width);

        for (int i = 0; i < line.Words.Count; i++)
        {
            Word word = line.Words[i];

            // Cursor dentro de la palabra (o justo al final de ella).
            if (offset <= word.EndOffset)
                return Math.Min(column + (offset - word.StartOffset), _width);

            column += word.Length;

            bool isLastWord = i == line.Words.Count - 1;
            if (isLastWord)
                break;

            // Cursor en los espacios entre esta palabra y la siguiente.
            int gap = spacing.Gaps[i];
            Word next = line.Words[i + 1];
            if (offset < next.StartOffset)
                return Math.Min(column + Math.Min(offset - word.EndOffset, gap), _width);

            column += gap;
        }

        // Cursor en los espacios después de la última palabra.
        int afterLast = offset - line.Words[^1].EndOffset;
        return Math.Min(column + afterLast, _width);
    }
}