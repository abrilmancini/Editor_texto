namespace EditorTexto.Commands;

public sealed class CommandHistory
{
    private readonly Stack<IEditorCommand> _done = new();

    public int Count => _done.Count;

    public bool CanUndo => _done.Count > 0;

    public void Execute(IEditorCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.Execute())
            _done.Push(command);
    }

    public bool Undo()
    {
        if (_done.Count == 0)
            return false;

        _done.Pop().Undo();
        return true;
    }

    public void Clear() => _done.Clear();
}