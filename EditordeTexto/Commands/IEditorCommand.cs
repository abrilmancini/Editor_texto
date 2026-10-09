namespace EditorTexto.Commands;

/// <summary>
/// Patrón Command — rol: Command.
/// Responsabilidad: definir la interfaz de toda operación que se puede deshacer.
/// Ítem de cambio que esconde: qué hace cada operación y cómo se revierte;
/// quien la ejecuta (CommandHistory) no sabe de qué operación se trata.
/// </summary>
public interface IEditorCommand
{
    /// <summary>
    /// Ejecuta la operación. Devuelve true si cambió algo (y entonces se guarda para deshacer);
    /// false si no se pudo o no tuvo efecto (por ejemplo, Backspace al inicio del documento).
    /// </summary>
    bool Execute();

    /// <summary>Revierte exactamente lo que hizo Execute, incluida la posición del cursor.</summary>
    void Undo();
}