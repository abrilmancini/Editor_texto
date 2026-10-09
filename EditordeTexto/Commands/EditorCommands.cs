using EditorTexto.Layout;
using EditorTexto.Model;

namespace EditorTexto.Commands;

/// <summary>
/// Patrón Command — rol: ConcreteCommand. Receiver: Editor (y su TextBuffer).
/// Responsabilidad: insertar un carácter en el cursor y poder sacarlo.
/// Ítem de cambio que esconde: cómo se inserta y se deshace un carácter.
/// </summary>
public sealed class InsertCharCommand : IEditorCommand
{
    private readonly Editor _editor;
    private readonly char _character;
    private CursorPosition _position;

    public InsertCharCommand(Editor editor, char character)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        _character = character;
    }

    public bool Execute()
    {
        if (!_editor.CanInsert(_character, out string reason))
        {
            _editor.StatusMessage = reason;
            return false;
        }

        _position = _editor.Buffer.Cursor;
        _editor.InsertChar(_character);
        return true;
    }

    public void Undo()
    {
        _editor.Buffer.SetCursor(_position);
        _editor.DeleteAfter(); // borra el carácter insertado; el cursor queda donde estaba
    }
}

/// <summary>
/// Patrón Command — rol: ConcreteCommand. Receiver: Editor.
/// Responsabilidad: Backspace (borrar antes del cursor o unir con el párrafo anterior) y deshacerlo.
/// Ítem de cambio que esconde: qué se guarda para poder restaurar lo borrado.
/// </summary>
public sealed class DeleteBeforeCommand : IEditorCommand
{
    private readonly Editor _editor;
    private Deletion _deletion;
    private CursorPosition _afterDelete;

    public DeleteBeforeCommand(Editor editor)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
    }

    public bool Execute()
    {
        if (!_editor.CanDeleteBefore(out string reason))
        {
            _editor.StatusMessage = reason;
            return false;
        }

        _deletion = _editor.DeleteBefore();
        _afterDelete = _editor.Buffer.Cursor;
        return _deletion.Kind != DeletionKind.None;
    }

    public void Undo()
    {
        _editor.Buffer.SetCursor(_afterDelete);
        if (_deletion.Kind == DeletionKind.Character)
            _editor.InsertChar(_deletion.Character);   // el cursor queda después del carácter, como antes
        else if (_deletion.Kind == DeletionKind.ParagraphBreak)
            _editor.SplitParagraph();                  // el cursor queda al inicio del párrafo, como antes
    }
}

/// <summary>
/// Patrón Command — rol: ConcreteCommand. Receiver: Editor.
/// Responsabilidad: Supr (borrar después del cursor o unir con el párrafo siguiente) y deshacerlo.
/// Ítem de cambio que esconde: qué se guarda para poder restaurar lo borrado.
/// </summary>
public sealed class DeleteAfterCommand : IEditorCommand
{
    private readonly Editor _editor;
    private Deletion _deletion;
    private CursorPosition _position;

    public DeleteAfterCommand(Editor editor)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
    }

    public bool Execute()
    {
        if (!_editor.CanDeleteAfter(out string reason))
        {
            _editor.StatusMessage = reason;
            return false;
        }

        _position = _editor.Buffer.Cursor;
        _deletion = _editor.DeleteAfter();
        return _deletion.Kind != DeletionKind.None;
    }

    public void Undo()
    {
        _editor.Buffer.SetCursor(_position);
        if (_deletion.Kind == DeletionKind.Character)
            _editor.InsertChar(_deletion.Character);
        else if (_deletion.Kind == DeletionKind.ParagraphBreak)
            _editor.SplitParagraph();
        _editor.Buffer.SetCursor(_position); // Supr no mueve el cursor
    }
}

/// <summary>
/// Patrón Command — rol: ConcreteCommand. Receiver: Editor.
/// Responsabilidad: Enter (partir el párrafo en el cursor) y deshacerlo (volver a unirlo).
/// Ítem de cambio que esconde: cómo se parte y se vuelve a unir un párrafo.
/// </summary>
public sealed class SplitParagraphCommand : IEditorCommand
{
    private readonly Editor _editor;
    private CursorPosition _position;

    public SplitParagraphCommand(Editor editor)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
    }

    public bool Execute()
    {
        _position = _editor.Buffer.Cursor;
        _editor.SplitParagraph();
        return true;
    }

    public void Undo()
    {
        _editor.Buffer.SetCursor(_position); // queda al final del primer pedazo
        _editor.DeleteAfter();               // une con el párrafo siguiente
    }
}

/// <summary>
/// Patrón Command — rol: ConcreteCommand. Receiver: Editor.
/// Responsabilidad: cambiar la alineación y poder volver a la anterior.
/// Ítem de cambio que esconde: cómo se recuerda la alineación anterior.
/// </summary>
public sealed class ChangeAlignmentCommand : IEditorCommand
{
    private readonly Editor _editor;
    private readonly IAlignmentStrategy _newAlignment;
    private IAlignmentStrategy? _previousAlignment;

    public ChangeAlignmentCommand(Editor editor, IAlignmentStrategy newAlignment)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        _newAlignment = newAlignment ?? throw new ArgumentNullException(nameof(newAlignment));
    }

    public bool Execute()
    {
        if (_editor.Alignment.GetType() == _newAlignment.GetType())
            return false; // ya estaba en esa alineación: no se guarda en el historial

        _previousAlignment = _editor.Alignment;
        _editor.SetAlignment(_newAlignment);
        return true;
    }

    public void Undo()
    {
        if (_previousAlignment is not null)
            _editor.SetAlignment(_previousAlignment);
    }
}