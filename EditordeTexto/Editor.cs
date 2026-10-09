using EditorTexto.Layout;
using EditorTexto.Model;
using EditorTexto.Structure;

namespace EditorTexto;

/// <summary>
/// Responsabilidad: ser la fachada del modelo. Junta el TextBuffer, la alineación actual
/// y el documento formateado; después de cada cambio vuelve a armar el documento.
/// También sabe en qué línea visual está el cursor y cómo moverlo entre líneas.
/// Ítem de cambio que esconde: qué hay que hacer después de cada cambio (re-layout)
/// y las reglas de edición (espacios dobles, palabras más largas que el ancho).
/// </summary>
public sealed class Editor
{
    private readonly LayoutEngine _layoutEngine;
    private List<Line> _lines = new();
    private int[] _firstLineOfParagraph = Array.Empty<int>();

    public EditorConfig Config { get; }
    public TextBuffer Buffer { get; } = new TextBuffer();
    public LineRenderer Renderer { get; }
    public IAlignmentStrategy Alignment { get; private set; } = new LeftAlignment();
    public Document Document { get; private set; } = new Document();

    /// <summary>Mensaje para la barra de estado (por ejemplo, por qué no se pudo escribir).</summary>
    public string StatusMessage { get; set; } = "";

    /// <summary>Todas las líneas del documento, en orden.</summary>
    public IReadOnlyList<Line> Lines => _lines;

    public Editor(EditorConfig config)
    {
        Config = config ?? throw new ArgumentNullException(nameof(config));
        _layoutEngine = new LayoutEngine(config);
        Renderer = new LineRenderer(config.LineWidth);
        Relayout();
    }

    // ---------- Documento ----------

    public void LoadText(string text)
    {
        Buffer.Load(text);
        Relayout();
    }

    public void SetAlignment(IAlignmentStrategy alignment)
    {
        Alignment = alignment ?? throw new ArgumentNullException(nameof(alignment));
        // No hace falta re-layout: la alineación no cambia los cortes de línea.
    }

    /// <summary>Vuelve a armar el árbol Document a partir del buffer.</summary>
    private void Relayout()
    {
        Document = _layoutEngine.Layout(Buffer);
        _lines = Document.Lines.ToList();

        _firstLineOfParagraph = new int[Buffer.ParagraphCount];
        for (int i = _lines.Count - 1; i >= 0; i--)
            _firstLineOfParagraph[_lines[i].ParagraphIndex] = i;
    }

    // ---------- Edición (cada una hace re-layout) ----------

    public void InsertChar(char character) { Buffer.Insert(character); Relayout(); }
    public void SplitParagraph() { Buffer.SplitParagraph(); Relayout(); }
    public Deletion DeleteBefore() { Deletion deletion = Buffer.DeleteBefore(); Relayout(); return deletion; }
    public Deletion DeleteAfter() { Deletion deletion = Buffer.DeleteAfter(); Relayout(); return deletion; }

    // ---------- Reglas de edición ----------

    /// <summary>
    /// Dice si se puede insertar el carácter en el cursor. Si no se puede, <paramref name="reason"/>
    /// explica por qué (vacío si simplemente se ignora, como un espacio doble).
    /// </summary>
    public bool CanInsert(char character, out string reason)
    {
        reason = "";
        string paragraph = Buffer.GetParagraph(Buffer.Cursor.Paragraph);
        int offset = Buffer.Cursor.Offset;

        if (character == ' ')
        {
            // Sin espacios al inicio del párrafo ni dos espacios seguidos.
            return offset > 0 && Buffer.CharBeforeCursor() != ' ' && Buffer.CharAfterCursor() != ' ';
        }

        int wordLength = RunLeft(paragraph, offset) + 1 + RunRight(paragraph, offset);
        return CheckWordLength(wordLength, out reason);
    }

    /// <summary>Dice si Backspace se puede aplicar sin crear una palabra más larga que el ancho.</summary>
    public bool CanDeleteBefore(out string reason)
    {
        reason = "";
        CursorPosition cursor = Buffer.Cursor;
        string paragraph = Buffer.GetParagraph(cursor.Paragraph);

        if (cursor.Offset > 0 && paragraph[cursor.Offset - 1] == ' ')
            return CheckWordLength(RunLeft(paragraph, cursor.Offset - 1) + RunRight(paragraph, cursor.Offset), out reason);

        if (cursor.Offset == 0 && cursor.Paragraph > 0)
        {
            string previous = Buffer.GetParagraph(cursor.Paragraph - 1);
            return CheckWordLength(RunLeft(previous, previous.Length) + RunRight(paragraph, 0), out reason);
        }

        return true;
    }

    /// <summary>Dice si Supr se puede aplicar sin crear una palabra más larga que el ancho.</summary>
    public bool CanDeleteAfter(out string reason)
    {
        reason = "";
        CursorPosition cursor = Buffer.Cursor;
        string paragraph = Buffer.GetParagraph(cursor.Paragraph);

        if (cursor.Offset < paragraph.Length && paragraph[cursor.Offset] == ' ')
            return CheckWordLength(RunLeft(paragraph, cursor.Offset) + RunRight(paragraph, cursor.Offset + 1), out reason);

        if (cursor.Offset == paragraph.Length && cursor.Paragraph < Buffer.ParagraphCount - 1)
        {
            string next = Buffer.GetParagraph(cursor.Paragraph + 1);
            return CheckWordLength(RunLeft(paragraph, paragraph.Length) + RunRight(next, 0), out reason);
        }

        return true;
    }

    private bool CheckWordLength(int wordLength, out string reason)
    {
        reason = wordLength > Config.LineWidth
            ? $"Una palabra no puede tener más de {Config.LineWidth} caracteres (no se cortan)."
            : "";
        return wordLength <= Config.LineWidth;
    }

    /// <summary>Cantidad de caracteres no-espacio seguidos justo antes de <paramref name="offset"/>.</summary>
    private static int RunLeft(string text, int offset)
    {
        int count = 0;
        while (offset - count - 1 >= 0 && text[offset - count - 1] != ' ') count++;
        return count;
    }

    /// <summary>Cantidad de caracteres no-espacio seguidos desde <paramref name="offset"/>.</summary>
    private static int RunRight(string text, int offset)
    {
        int count = 0;
        while (offset + count < text.Length && text[offset + count] != ' ') count++;
        return count;
    }

    // ---------- Cursor en la vista ----------

    /// <summary>Índice (en <see cref="Lines"/>) de la línea donde está el cursor.</summary>
    public int CursorLineIndex
    {
        get
        {
            CursorPosition cursor = Buffer.Cursor;
            int index = _firstLineOfParagraph[cursor.Paragraph];
            while (index + 1 < _lines.Count
                   && _lines[index + 1].ParagraphIndex == cursor.Paragraph
                   && _lines[index + 1].StartOffset <= cursor.Offset)
            {
                index++;
            }
            return index;
        }
    }

    public Line CursorLine => _lines[CursorLineIndex];

    /// <summary>Columna (dentro de la línea) donde se dibuja el cursor.</summary>
    public int CursorColumn => Renderer.ColumnOf(CursorLine, Alignment, Buffer.Cursor.Offset);

    /// <summary>Página (empezando en 1) donde está el cursor.</summary>
    public int CursorPage => CursorLineIndex / Config.LinesPerPage + 1;

    public void MoveLeft() => Buffer.MoveLeft();
    public void MoveRight() => Buffer.MoveRight();
    public void MoveUp() => MoveVertically(-1);
    public void MoveDown() => MoveVertically(+1);

    /// <summary>Inicio de la línea visual.</summary>
    public void MoveToLineStart()
    {
        Line line = CursorLine;
        Buffer.SetCursor(new CursorPosition(line.ParagraphIndex, line.StartOffset));
    }

    /// <summary>Fin de la línea visual.</summary>
    public void MoveToLineEnd()
    {
        Line line = CursorLine;
        Buffer.SetCursor(new CursorPosition(line.ParagraphIndex, LastOffsetOf(line)));
    }

    /// <summary>Va a la línea de arriba o de abajo, a la columna más parecida a la actual.</summary>
    private void MoveVertically(int delta)
    {
        int target = CursorLineIndex + delta;
        if (target < 0 || target >= _lines.Count)
            return;

        int desiredColumn = CursorColumn;
        Line line = _lines[target];
        int bestOffset = line.StartOffset;
        int bestDistance = int.MaxValue;

        for (int offset = line.StartOffset; offset <= LastOffsetOf(line); offset++)
        {
            int distance = Math.Abs(Renderer.ColumnOf(line, Alignment, offset) - desiredColumn);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestOffset = offset;
            }
        }

        Buffer.SetCursor(new CursorPosition(line.ParagraphIndex, bestOffset));
    }

    /// <summary>
    /// Último offset que pertenece a la línea. En una línea que no es la última del párrafo,
    /// EndOffset ya es el inicio de la línea siguiente, así que se resta uno.
    /// </summary>
    private static int LastOffsetOf(Line line) =>
        line.IsLastOfParagraph ? line.EndOffset : line.EndOffset - 1;
}