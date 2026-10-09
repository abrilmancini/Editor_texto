namespace EditorTexto.Structure;

/// <summary>
/// Patrón Visitor — rol: Visitor.
/// Responsabilidad: declarar una operación Visit por cada tipo de nodo del documento.
/// Ítem de cambio que esconde: las operaciones que se hacen sobre el documento
/// (contar, exportar...). Cada operación nueva es un visitor nuevo, sin tocar los nodos.
/// Los visitors concretos recorren los hijos (Children) en el orden que necesiten.
/// </summary>
public interface IDocumentVisitor
{
    void VisitDocument(Document document);
    void VisitPage(Page page);
    void VisitParagraph(Paragraph paragraph);
    void VisitLine(Line line);
    void VisitWord(Word word);
}