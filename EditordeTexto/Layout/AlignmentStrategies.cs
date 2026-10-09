using EditorTexto.Structure;

namespace EditorTexto.Layout;

/// <summary>
/// Utilidades compartidas por las estrategias (no es parte del patrón).
/// </summary>
internal static class SpacingMath
{
    /// <summary>Lista de huecos de 1 espacio (palabras − 1 elementos).</summary>
    public static int[] SingleGaps(Line line) => Enumerable.Repeat(1, Math.Max(0, line.Words.Count - 1)).ToArray();

    /// <summary>Columnas que ocupa la línea con un espacio entre palabras.</summary>
    public static int NaturalLength(Line line) => line.WordsLength + Math.Max(0, line.Words.Count - 1);

    /// <summary>Columnas que sobran hasta el ancho (nunca negativo).</summary>
    public static int FreeSpace(Line line, int width) => Math.Max(0, width - NaturalLength(line));
}

/// <summary>
/// Patrón Strategy — rol: ConcreteStrategy.
/// Responsabilidad: alinear a izquierda (un espacio entre palabras, lo que sobra queda a la derecha).
/// Ítem de cambio que esconde: el algoritmo de alineación a izquierda.
/// </summary>
public sealed class LeftAlignment : IAlignmentStrategy
{
    public string Name => "Izquierda";

    public LineSpacing ComputeSpacing(Line line, int width) =>
        new LineSpacing(0, SpacingMath.SingleGaps(line));
}

/// <summary>
/// Patrón Strategy — rol: ConcreteStrategy.
/// Responsabilidad: alinear a derecha (todo lo que sobra va antes de la primera palabra).
/// Ítem de cambio que esconde: el algoritmo de alineación a derecha.
/// </summary>
public sealed class RightAlignment : IAlignmentStrategy
{
    public string Name => "Derecha";

    public LineSpacing ComputeSpacing(Line line, int width) =>
        new LineSpacing(SpacingMath.FreeSpace(line, width), SpacingMath.SingleGaps(line));
}

/// <summary>
/// Patrón Strategy — rol: ConcreteStrategy.
/// Responsabilidad: centrar (lo que sobra se reparte a los dos lados; si es impar, el extra va a la derecha).
/// Ítem de cambio que esconde: el algoritmo de centrado.
/// </summary>
public sealed class CenterAlignment : IAlignmentStrategy
{
    public string Name => "Centrado";

    public LineSpacing ComputeSpacing(Line line, int width) =>
        new LineSpacing(SpacingMath.FreeSpace(line, width) / 2, SpacingMath.SingleGaps(line));
}

/// <summary>
/// Patrón Strategy — rol: ConcreteStrategy.
/// Responsabilidad: justificar: estirar los espacios entre palabras para que la línea
/// ocupe exactamente el ancho. La última línea del párrafo y las líneas de una sola
/// palabra se alinean a izquierda (como en cualquier procesador de texto).
/// Ítem de cambio que esconde: cómo se reparten los espacios sobrantes entre los huecos.
/// </summary>
public sealed class JustifiedAlignment : IAlignmentStrategy
{
    public string Name => "Justificado";

    public LineSpacing ComputeSpacing(Line line, int width)
    {
        int gapCount = line.Words.Count - 1;
        if (line.IsLastOfParagraph || gapCount <= 0)
            return new LineSpacing(0, SpacingMath.SingleGaps(line));

        int spaces = Math.Max(gapCount, width - line.WordsLength); // al menos 1 espacio por hueco
        int baseGap = spaces / gapCount;
        int extra = spaces % gapCount;                              // los primeros "extra" huecos llevan uno más

        int[] gaps = new int[gapCount];
        for (int i = 0; i < gapCount; i++)
            gaps[i] = baseGap + (i < extra ? 1 : 0);

        return new LineSpacing(0, gaps);
    }
}