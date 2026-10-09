using EditorTexto.Structure;
using EditorTexto.Visitors;

namespace EditorTexto.UI;

/// <summary>
/// Lo que hay que dibujar: las filas de texto y dónde va el cursor.
/// Separarlo de la consola permite probar la vista sin abrir una ventana.
/// </summary>
public sealed record Frame(IReadOnlyList<string> Rows, int CursorRow, int CursorColumn);

/// <summary>
/// Responsabilidad: dibujar en la consola la vista reducida del documento (solo las líneas
/// alrededor del cursor), el encabezado con página/línea/alineación, la barra de estado y la ayuda.
/// Ítem de cambio que esconde: cómo se muestra todo en la consola.
/// </summary>
public sealed class ConsoleView
{
    private const int HeaderRows = 2; // encabezado + borde superior

    private readonly Editor _editor;
    private int _top;               // primera fila visible de la vista reducida
    private int _previousRowCount;

    public ConsoleView(Editor editor)
    {
        _editor = editor ?? throw new ArgumentNullException(nameof(editor));
    }

    /// <summary>Arma el cuadro sin tocar la consola.</summary>
    public Frame BuildFrame()
    {
        EditorConfig config = _editor.Config;
        int width = config.LineWidth;
        int perPage = config.LinesPerPage;
        int visible = config.VisibleLines;
        IReadOnlyList<Line> lines = _editor.Lines;

        // Filas de la vista: las líneas, con un separador entre página y página.
        // La línea i está en la fila i + i / perPage (hay i / perPage separadores antes).
        int totalRows = lines.Count + (lines.Count - 1) / perPage;
        int cursorLine = _editor.CursorLineIndex;
        int cursorRow = cursorLine + cursorLine / perPage;

        // Mover la ventana visible para que el cursor siempre se vea.
        if (cursorRow < _top) _top = cursorRow;
        if (cursorRow >= _top + visible) _top = cursorRow - visible + 1;
        _top = Math.Clamp(_top, 0, Math.Max(0, totalRows - visible));

        List<string> rows = new List<string>
        {
            $" Página {_editor.CursorPage}/{_editor.Document.Pages.Count} · " +
            $"Línea {cursorLine + 1}/{lines.Count} · Alineación: {_editor.Alignment.Name}",
            "+" + new string('-', width) + "+"
        };

        for (int row = _top; row < _top + visible; row++)
        {
            if (row >= totalRows)
            {
                rows.Add(" " + new string(' ', width) + " ");
                continue;
            }

            int block = row / (perPage + 1);
            int position = row % (perPage + 1);
            if (position < perPage)
            {
                Line line = lines[block * perPage + position];
                rows.Add("|" + _editor.Renderer.Render(line, _editor.Alignment) + "|");
            }
            else
            {
                rows.Add(PageSeparator(block + 2, width));
            }
        }

        rows.Add("+" + new string('-', width) + "+");
        DocumentStatistics stats = StatisticsVisitor.Calculate(_editor.Document);
        rows.Add($" Palabras: {stats.Words} · Caracteres: {stats.Characters} · Líneas: {stats.Lines} · " +
                 $"Párrafos: {stats.Paragraphs} · Páginas: {stats.Pages}");
        rows.Add(" " + _editor.StatusMessage);
        rows.Add(" Mover: flechas, Inicio, Fin · Alinear: F1 Izq, F2 Der, F3 Centro, F4 Justif.");
        rows.Add(" Ctrl+Z deshacer · F5 o Ctrl+S exportar .txt · F6 o Ctrl+D exportar .md · Esc salir");
        int screenRow = HeaderRows + cursorRow - _top;
        int screenColumn = 1 + _editor.CursorColumn; // +1 por el borde "|"
        return new Frame(rows, screenRow, screenColumn);
    }

    /// <summary>Dibuja el cuadro en la consola, sobrescribiendo el anterior (sin Console.Clear, para que no parpadee).</summary>
    public void Draw()
    {
        Frame frame = BuildFrame();
        int consoleWidth = Math.Max(1, Console.WindowWidth - 1);

        Console.CursorVisible = false;
        Console.SetCursorPosition(0, 0);

        foreach (string row in frame.Rows)
            Console.WriteLine(Fit(row, consoleWidth));

        for (int i = frame.Rows.Count; i < _previousRowCount; i++)
            Console.WriteLine(new string(' ', consoleWidth));
        _previousRowCount = frame.Rows.Count;

        Console.SetCursorPosition(Math.Min(frame.CursorColumn, consoleWidth), frame.CursorRow);
        Console.CursorVisible = true;
    }

    private static string PageSeparator(int nextPage, int width)
    {
        string label = $" Página {nextPage} ";
        string inner = ("---" + label).PadRight(width, '-');
        return "+" + inner[..width] + "+";
    }

    /// <summary>Recorta o rellena la fila al ancho de la consola, para que no salte de línea.</summary>
    private static string Fit(string row, int width) =>
        row.Length > width ? row[..width] : row.PadRight(width);
}