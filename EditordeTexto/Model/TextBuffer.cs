using System.Text;

namespace EditorTexto.Model;

/// <summary>
/// Responsabilidad: guardar el texto del documento como una lista de párrafos
/// y la posición del cursor; insertar, borrar, partir y unir párrafos en el cursor.
/// Ítem de cambio que esconde: cómo se almacena el texto en memoria
/// (hoy una lista de StringBuilder; mañana podría ser un gap buffer o una rope
/// sin que el resto del programa se entere).
/// No sabe nada de líneas, páginas ni alineación: eso lo calcula el LayoutEngine.
/// </summary>
public sealed class TextBuffer
{
    private readonly List<StringBuilder> _paragraphs = new() { new StringBuilder() };

    public CursorPosition Cursor { get; private set; } = new(0, 0);

    public int ParagraphCount => _paragraphs.Count;

    public string GetParagraph(int index) => _paragraphs[index].ToString();

    public int GetParagraphLength(int index) => _paragraphs[index].Length;

    /// <summary>Todo el texto, con '\n' entre párrafos (útil para probar y exportar).</summary>
    public string GetText() => string.Join("\n", _paragraphs);

    /// <summary>Reemplaza todo el contenido. Cada '\n' separa un párrafo.</summary>
    public void Load(string text)
    {
        _paragraphs.Clear();
        foreach (string paragraph in text.Replace("\r\n", "\n").Split('\n'))
        {
            _paragraphs.Add(new StringBuilder(paragraph));
        }
        Cursor = new CursorPosition(0, 0);
    }

    // ---------- Cursor ----------

    public void SetCursor(CursorPosition position)
    {
        if (position.Paragraph < 0 || position.Paragraph >= _paragraphs.Count)
            throw new ArgumentOutOfRangeException(nameof(position), $"Párrafo inválido: {position}");
        if (position.Offset < 0 || position.Offset > _paragraphs[position.Paragraph].Length)
            throw new ArgumentOutOfRangeException(nameof(position), $"Offset inválido: {position}");

        Cursor = position;
    }

    public void MoveLeft()
    {
        if (Cursor.Offset > 0)
            Cursor = Cursor with { Offset = Cursor.Offset - 1 };
        else if (Cursor.Paragraph > 0)
            Cursor = new CursorPosition(Cursor.Paragraph - 1, _paragraphs[Cursor.Paragraph - 1].Length);
    }

    public void MoveRight()
    {
        if (Cursor.Offset < CurrentParagraph.Length)
            Cursor = Cursor with { Offset = Cursor.Offset + 1 };
        else if (Cursor.Paragraph < _paragraphs.Count - 1)
            Cursor = new CursorPosition(Cursor.Paragraph + 1, 0);
    }

    /// <summary>Al inicio del párrafo. En el paso 5, Inicio va a ir al inicio de la línea visual.</summary>
    public void MoveToParagraphStart() => Cursor = Cursor with { Offset = 0 };

    /// <summary>Al final del párrafo. En el paso 5, Fin va a ir al final de la línea visual.</summary>
    public void MoveToParagraphEnd() => Cursor = Cursor with { Offset = CurrentParagraph.Length };

    /// <summary>Carácter justo antes del cursor en el mismo párrafo, o null si está al inicio.</summary>
    public char? CharBeforeCursor() =>
        Cursor.Offset > 0 ? CurrentParagraph[Cursor.Offset - 1] : null;

    /// <summary>Carácter justo después del cursor en el mismo párrafo, o null si está al final.</summary>
    public char? CharAfterCursor() =>
        Cursor.Offset < CurrentParagraph.Length ? CurrentParagraph[Cursor.Offset] : null;

    // ---------- Edición ----------

    /// <summary>Inserta un carácter en el cursor y lo avanza una posición.</summary>
    public void Insert(char character)
    {
        if (char.IsControl(character))
            throw new ArgumentException("Para un párrafo nuevo usar SplitParagraph().", nameof(character));

        CurrentParagraph.Insert(Cursor.Offset, character);
        Cursor = Cursor with { Offset = Cursor.Offset + 1 };
    }

    /// <summary>Enter: lo que está después del cursor pasa a un párrafo nuevo.</summary>
    public void SplitParagraph()
    {
        StringBuilder current = CurrentParagraph;
        int restLength = current.Length - Cursor.Offset;
        string rest = current.ToString(Cursor.Offset, restLength);
        current.Remove(Cursor.Offset, restLength);

        _paragraphs.Insert(Cursor.Paragraph + 1, new StringBuilder(rest));
        Cursor = new CursorPosition(Cursor.Paragraph + 1, 0);
    }

    /// <summary>
    /// Backspace: borra el carácter antes del cursor.
    /// Si el cursor está al inicio de un párrafo, lo une con el anterior.
    /// </summary>
    public Deletion DeleteBefore()
    {
        if (Cursor.Offset > 0)
        {
            int offset = Cursor.Offset - 1;
            char deleted = CurrentParagraph[offset];
            CurrentParagraph.Remove(offset, 1);
            Cursor = Cursor with { Offset = offset };
            return Deletion.Of(deleted);
        }

        if (Cursor.Paragraph == 0)
            return Deletion.Nothing;

        int previous = Cursor.Paragraph - 1;
        int joinOffset = _paragraphs[previous].Length;
        MergeWithNext(previous);
        Cursor = new CursorPosition(previous, joinOffset);
        return Deletion.ParagraphBreak;
    }

    /// <summary>
    /// Supr: borra el carácter después del cursor.
    /// Si el cursor está al final de un párrafo, le une el siguiente. El cursor no se mueve.
    /// </summary>
    public Deletion DeleteAfter()
    {
        if (Cursor.Offset < CurrentParagraph.Length)
        {
            char deleted = CurrentParagraph[Cursor.Offset];
            CurrentParagraph.Remove(Cursor.Offset, 1);
            return Deletion.Of(deleted);
        }

        if (Cursor.Paragraph == _paragraphs.Count - 1)
            return Deletion.Nothing;

        MergeWithNext(Cursor.Paragraph);
        return Deletion.ParagraphBreak;
    }

    // ---------- Privados ----------

    private StringBuilder CurrentParagraph => _paragraphs[Cursor.Paragraph];

    private void MergeWithNext(int index)
    {
        _paragraphs[index].Append(_paragraphs[index + 1]);
        _paragraphs.RemoveAt(index + 1);
    }
}