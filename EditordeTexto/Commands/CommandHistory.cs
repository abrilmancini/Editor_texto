namespace EditorTexto.Commands;

/// <summary>
/// Patrón Command — rol: Invoker.
/// Responsabilidad: ejecutar comandos y guardar los que cambiaron algo, para deshacerlos
/// en orden inverso (el último que se hizo es el primero que se deshace).
/// Ítem de cambio que esconde: la política de deshacer (hoy una pila sin límite;
/// se podría limitar la cantidad, agrupar por palabra o agregar rehacer).
/// </summary>
public sealed class CommandHistory
{
    private readonly Stack<IEditorCommand> _done = new();

    public int Count => _done.Count;

    public bool CanUndo => _done.Count > 0;

    /// <summary>Ejecuta el comando y, si cambió algo, lo guarda.</summary>
    public void Execute(IEditorCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Execute())
            _done.Push(command);
    }

    /// <summary>Deshace el último comando. Devuelve false si no había nada para deshacer.</summary>
    public bool Undo()
    {
        if (_done.Count == 0)
            return false;

        _done.Pop().Undo();
        return true;
    }

    public void Clear() => _done.Clear();
}