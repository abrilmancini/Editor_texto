using System.Text;

namespace EditorTexto.Structure;

/// <summary>
/// Patrón Composite — rol: Component.
/// Responsabilidad: ser la interfaz común de todos los nodos del documento
/// (Document, Page, Paragraph, Line, Word) para que el resto del programa
/// los recorra de la misma forma, sin preguntar qué tipo de nodo es.
/// Ítem de cambio que esconde: la forma común de los nodos del árbol.
/// </summary>
public abstract class DocumentElement
{
    /// <summary>Hijos del nodo. Una hoja (Word) devuelve una lista vacía.</summary>
    public abstract IReadOnlyList<DocumentElement> Children { get; }

    /// <summary>Descripción corta de este nodo, sin sus hijos (para depurar).</summary>
    public abstract string Describe();

    /// <summary>
    /// Operación uniforme del Composite: dibuja el subárbol recorriendo Children
    /// sin saber si cada hijo es una hoja o un compuesto.
    /// </summary>
    public string ToTreeString()
    {
        StringBuilder output = new StringBuilder();
        AppendTree(output, 0);
        return output.ToString();
    }

    private void AppendTree(StringBuilder output, int depth)
    {
        output.Append(' ', depth * 2).AppendLine(Describe());
        foreach (DocumentElement child in Children)
        {
            child.AppendTree(output, depth + 1);
        }
    }
    public abstract void Accept(IDocumentVisitor visitor);
}