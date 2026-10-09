namespace EditorTexto.Structure;

/// <summary>
/// Patrón Composite — rol: Leaf.
/// Responsabilidad: representar una palabra (caracteres sin espacios)
/// y recordar dónde empieza dentro de su párrafo en el TextBuffer.
/// Ítem de cambio que esconde: cómo se ubica una palabra en el texto fuente.
/// </summary>
public sealed class Word : DocumentElement
{
    public string Text { get; }

    /// <summary>Offset del primer carácter de la palabra dentro de su párrafo.</summary>
    public int StartOffset { get; }

    public int Length => Text.Length;

    /// <summary>Offset justo después del último carácter (exclusivo).</summary>
    public int EndOffset => StartOffset + Text.Length;

    public Word(string text, int startOffset)
    {
        if (string.IsNullOrEmpty(text))
            throw new ArgumentException("Una palabra no puede estar vacía.", nameof(text));
        if (text.Contains(' '))
            throw new ArgumentException("Una palabra no puede tener espacios.", nameof(text));
        if (startOffset < 0)
            throw new ArgumentOutOfRangeException(nameof(startOffset));

        Text = text;
        StartOffset = startOffset;
    }

    public override IReadOnlyList<DocumentElement> Children => Array.Empty<DocumentElement>();

    public override void Accept(IDocumentVisitor visitor) => visitor.VisitWord(this);
    public override string Describe() => $"Word \"{Text}\" [{StartOffset}..{EndOffset})";
}