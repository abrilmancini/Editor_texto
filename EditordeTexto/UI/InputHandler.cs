using EditorTexto.Commands;
using EditorTexto.Layout;
using EditorTexto.Visitors;

namespace EditorTexto.UI;

/// <summary>
/// Patrón Command — rol: Client (crea el comando que corresponde a cada tecla y se lo pasa al Invoker).
/// Responsabilidad: traducir cada tecla a una acción del editor.
/// Ítem de cambio que esconde: el mapeo de teclas (qué tecla hace qué).
/// Las ediciones van como comandos (se pueden deshacer); mover el cursor no.
/// </summary>
public sealed class InputHandler
{
    private readonly Editor _editor;
    private readonly CommandHistory _history;

    public InputHandler(Editor editor, CommandHistory history)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
        _history = history ?? throw new ArgumentNullException(nameof(history));
    }

    /// <summary>Procesa una tecla. Devuelve false si el usuario quiere salir.</summary>
    public bool Handle(ConsoleKeyInfo key)
    {
        _editor.StatusMessage = "";
        bool ctrl = key.Modifiers.HasFlag(ConsoleModifiers.Control);

        if ((ctrl && key.Key == ConsoleKey.Z) || key.KeyChar == '\u001A')
        {
            if (!_history.Undo())
                _editor.StatusMessage = "No hay nada para deshacer.";
            return true;
        }

        // Atajos alternativos para exportar, por si F5/F6 están ocupadas por el teclado (brillo, volumen...).
        if (ctrl && key.Key == ConsoleKey.S) { ExportPlainText(); return true; }
        if (ctrl && key.Key == ConsoleKey.D) { ExportMarkdown(); return true; }

        switch (key.Key)
        {
            case ConsoleKey.Escape:
                return false;

            // Movimientos: no son comandos, no se deshacen (como en cualquier editor).
            case ConsoleKey.LeftArrow: _editor.MoveLeft(); return true;
            case ConsoleKey.RightArrow: _editor.MoveRight(); return true;
            case ConsoleKey.UpArrow: _editor.MoveUp(); return true;
            case ConsoleKey.DownArrow: _editor.MoveDown(); return true;
            case ConsoleKey.Home: _editor.MoveToLineStart(); return true;
            case ConsoleKey.End: _editor.MoveToLineEnd(); return true;

            case ConsoleKey.F1: _history.Execute(new ChangeAlignmentCommand(_editor, new LeftAlignment())); return true;
            case ConsoleKey.F2: _history.Execute(new ChangeAlignmentCommand(_editor, new RightAlignment())); return true;
            case ConsoleKey.F3: _history.Execute(new ChangeAlignmentCommand(_editor, new CenterAlignment())); return true;
            case ConsoleKey.F4: _history.Execute(new ChangeAlignmentCommand(_editor, new JustifiedAlignment())); return true;

            // Exportar: no modifica el documento, así que no es un comando ni se deshace.
            case ConsoleKey.F5: ExportPlainText(); return true;
            case ConsoleKey.F6: ExportMarkdown(); return true;

            case ConsoleKey.Enter: _history.Execute(new SplitParagraphCommand(_editor)); return true;
            case ConsoleKey.Backspace: _history.Execute(new DeleteBeforeCommand(_editor)); return true;
            case ConsoleKey.Delete: _history.Execute(new DeleteAfterCommand(_editor)); return true;
        }

        if (!char.IsControl(key.KeyChar))
        {
            // Espacio justo antes de otro espacio: en vez de duplicarlo, se lo saltea.
            if (key.KeyChar == ' ' && _editor.Buffer.CharAfterCursor() == ' ')
            {
                _editor.MoveRight();
                return true;
            }

            _history.Execute(new InsertCharCommand(_editor, key.KeyChar));
        }

        return true;
    }

    private void ExportPlainText()
    {
        PlainTextExporter exporter = new PlainTextExporter(_editor.Renderer, _editor.Alignment);
        _editor.Document.Accept(exporter);
        Save("documento.txt", exporter.Result);
    }

    private void ExportMarkdown()
    {
        MarkdownExporter exporter = new MarkdownExporter();
        _editor.Document.Accept(exporter);
        Save("documento.md", exporter.Result);
    }

    /// <summary>Guarda el archivo en la carpeta del programa y avisa en la barra de estado dónde quedó.</summary>
    private void Save(string fileName, string content)
    {
        try
        {
            string path = Path.GetFullPath(fileName);
            File.WriteAllText(path, content);
            _editor.StatusMessage = $"Exportado a {path}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _editor.StatusMessage = $"No se pudo exportar: {ex.Message}";
        }
    }
}