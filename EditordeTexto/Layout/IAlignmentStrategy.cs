using EditorTexto.Structure;

namespace EditorTexto.Layout;

/// <summary>
/// Resultado de una estrategia de alineación: cuántos espacios van antes de la
/// primera palabra y cuántos entre cada par de palabras (Gaps tiene palabras − 1 elementos).
/// </summary>
public sealed record LineSpacing(int LeftPadding, IReadOnlyList<int> Gaps);

/// <summary>
/// Patrón Strategy — rol: Strategy.
/// Responsabilidad: definir la interfaz común de todos los algoritmos de alineación.
/// Ítem de cambio que esconde: cómo se reparten los espacios en una línea.
/// El contexto (LineRenderer) no sabe qué alineación está usando: solo llama a ComputeSpacing.
/// </summary>
public interface IAlignmentStrategy
{
    /// <summary>Nombre para mostrar en la barra de estado.</summary>
    string Name { get; }

    /// <summary>Calcula los espacios de una línea para que ocupe como máximo <paramref name="width"/> columnas.</summary>
    LineSpacing ComputeSpacing(Line line, int width);
}