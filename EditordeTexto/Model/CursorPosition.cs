namespace EditorTexto.Model;

/// <summary>
/// Responsabilidad: representar una posición lógica dentro del texto:
/// en qué párrafo está el cursor y antes de qué carácter de ese párrafo.
/// Ítem de cambio que esconde: cómo se representa una posición en el documento.
/// Es inmutable: para "mover" el cursor se crea una posición nueva.
/// </summary>
/// <param name="Paragraph">Índice del párrafo (empieza en 0).</param>
/// <param name="Offset">Cantidad de caracteres antes del cursor en ese párrafo (0 = inicio).</param>
public readonly record struct CursorPosition(int Paragraph, int Offset)
{
    public override string ToString() => $"(párrafo {Paragraph}, offset {Offset})";
}