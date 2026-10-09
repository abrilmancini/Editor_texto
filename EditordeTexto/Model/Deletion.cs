namespace EditorTexto.Model;

/// <summary>
/// Qué se borró en una operación de borrado: nada, un carácter o un salto de párrafo.
/// </summary>
public enum DeletionKind
{
    None,
    Character,
    ParagraphBreak
}

/// <summary>
/// Responsabilidad: informar qué borró el <see cref="TextBuffer"/>, para que
/// el comando que hizo el borrado pueda deshacerlo después (paso 6).
/// Ítem de cambio que esconde: qué información hace falta para revertir un borrado.
/// </summary>
/// <param name="Kind">Tipo de borrado.</param>
/// <param name="Character">El carácter borrado (solo tiene sentido si Kind es Character).</param>
public readonly record struct Deletion(DeletionKind Kind, char Character = '\0')
{
    public static Deletion Nothing => new(DeletionKind.None);
    public static Deletion ParagraphBreak => new(DeletionKind.ParagraphBreak);
    public static Deletion Of(char character) => new(DeletionKind.Character, character);
}