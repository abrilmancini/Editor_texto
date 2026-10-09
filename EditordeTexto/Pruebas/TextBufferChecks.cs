using EditorTexto.Model;

namespace EditorTexto.Pruebas;

/// <summary>
/// Responsabilidad: probar a mano el TextBuffer sin interfaz (paso 1).
/// No es parte del editor; se puede borrar o dejar como prueba.
/// </summary>
public static class TextBufferChecks
{
    private static int _failures;

    public static void Run()
    {
        _failures = 0;
        TextBuffer buffer = new TextBuffer();

        foreach (char c in "hola") buffer.Insert(c);
        Check("escribir 'hola'", buffer.GetText() == "hola" && buffer.Cursor == new CursorPosition(0, 4));

        buffer.MoveLeft();
        buffer.MoveLeft();
        buffer.SplitParagraph();
        Check("Enter en el medio parte el párrafo", buffer.GetText() == "ho\nla" && buffer.Cursor == new CursorPosition(1, 0));

        Deletion merge = buffer.DeleteBefore();
        Check("Backspace al inicio une párrafos",
            merge.Kind == DeletionKind.ParagraphBreak && buffer.GetText() == "hola" && buffer.Cursor == new CursorPosition(0, 2));

        Deletion del = buffer.DeleteAfter();
        Check("Supr borra el carácter siguiente",
            del == Deletion.Of('l') && buffer.GetText() == "hoa" && buffer.Cursor == new CursorPosition(0, 2));

        buffer.MoveToParagraphStart();
        Check("Backspace al inicio del documento no hace nada", buffer.DeleteBefore().Kind == DeletionKind.None);

        buffer.MoveToParagraphEnd();
        Check("Supr al final del documento no hace nada", buffer.DeleteAfter().Kind == DeletionKind.None);

        buffer.Load("primer párrafo\nsegundo");
        buffer.SetCursor(new CursorPosition(0, 14));
        buffer.MoveRight();
        Check("flecha derecha al final pasa al párrafo siguiente", buffer.Cursor == new CursorPosition(1, 0));
        buffer.MoveLeft();
        Check("flecha izquierda al inicio vuelve al párrafo anterior", buffer.Cursor == new CursorPosition(0, 14));

        buffer.SetCursor(new CursorPosition(1, 7));
        buffer.SplitParagraph();
        Check("Enter al final crea un párrafo vacío", buffer.ParagraphCount == 3 && buffer.GetParagraph(2) == "");

        bool threw = false;
        try { buffer.SetCursor(new CursorPosition(0, 99)); }
        catch (ArgumentOutOfRangeException) { threw = true; }
        Check("SetCursor rechaza posiciones inválidas", threw);

        Console.WriteLine(_failures == 0 ? "\nTodo OK" : $"\n{_failures} prueba(s) fallaron");
    }

    private static void Check(string name, bool condition)
    {
        if (!condition) _failures++;
        Console.WriteLine($"{(condition ? "OK  " : "FAIL")}  {name}");
    }
}