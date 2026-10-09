namespace EditorTexto.Structure;

/// <summary>
/// Patrón Composite — rol: Composite (clase base de todos los compuestos).
/// Responsabilidad: guardar y exponer los hijos de un nodo compuesto.
/// Ítem de cambio que esconde: cómo se almacenan los hijos.
/// El parámetro genérico TChild fija qué tipo de hijo acepta cada nivel
/// (una Line solo acepta Word, una Page solo acepta Paragraph, etc.),
/// así el compilador impide armar un árbol inválido.
/// </summary>
public abstract class CompositeElement<TChild> : DocumentElement
    where TChild : DocumentElement
{
    private readonly List<TChild> _children = new();

    /// <summary>Hijos con su tipo concreto.</summary>
    protected IReadOnlyList<TChild> Elements => _children;

    public override IReadOnlyList<DocumentElement> Children => _children;

    public void Add(TChild child)
    {
        ArgumentNullException.ThrowIfNull(child);
        _children.Add(child);
    }
}