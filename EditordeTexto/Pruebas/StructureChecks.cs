using EditorTexto.Structure;

namespace EditorTexto.Pruebas;

/// <summary>
/// Responsabilidad: armar a mano un árbol chico y mostrarlo (paso 2).
/// Simula lo que en el paso 3 va a hacer el LayoutEngine.
/// </summary>
public static class StructureChecks
{
    public static void Run()
    {
        // Párrafo 0: "Hola mundo cruel" partido en dos líneas.
        // Párrafo 1: vacío.
        Line line1 = new Line(paragraphIndex: 0, startOffset: 0, endOffset: 11, isLastOfParagraph: false);
        line1.Add(new Word("Hola", 0));
        line1.Add(new Word("mundo", 5));

        Line line2 = new Line(0, 11, 16, isLastOfParagraph: true);
        line2.Add(new Word("cruel", 11));

        Paragraph paragraph0 = new Paragraph(index: 0, isContinuation: false);
        paragraph0.Add(line1);
        paragraph0.Add(line2);

        Paragraph paragraph1 = new Paragraph(1, false);
        paragraph1.Add(new Line(1, 0, 0, isLastOfParagraph: true));

        Page page = new Page(1);
        page.Add(paragraph0);
        page.Add(paragraph1);

        Document document = new Document();
        document.Add(page);

        Console.WriteLine(document.ToTreeString());

        // Recorrido uniforme: la misma función sirve para cualquier nodo.
        Console.WriteLine($"Palabras (recorrido genérico): {CountWords(document)}");
        Console.WriteLine($"Líneas: {document.Lines.Count()}");

        // Esto NO compila, y está bien: una Line solo acepta Word.
        // line1.Add(new Page(2));
    }

    private static int CountWords(DocumentElement element)
    {
        if (element is Word) return 1;
        return element.Children.Sum(CountWords);
    }
}