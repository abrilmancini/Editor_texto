namespace EditorTexto;

/// <summary>
/// Responsabilidad: guardar las medidas fijas de la "hoja" del editor.
/// Ítem de cambio que esconde: el ancho de línea, las líneas por página
/// y cuántas líneas entran en la vista reducida.
/// </summary>
public sealed class EditorConfig
{
    public int LineWidth { get; }
    public int LinesPerPage { get; }
    public int VisibleLines { get; }

    public EditorConfig(int lineWidth = 40, int linesPerPage = 10, int visibleLines = 15)
    {
        if (lineWidth <= 0) throw new ArgumentOutOfRangeException(nameof(lineWidth));
        if (linesPerPage <= 0) throw new ArgumentOutOfRangeException(nameof(linesPerPage));
        if (visibleLines <= 0) throw new ArgumentOutOfRangeException(nameof(visibleLines));

        LineWidth = lineWidth;
        LinesPerPage = linesPerPage;
        VisibleLines = visibleLines;
    }
}