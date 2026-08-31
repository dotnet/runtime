using System;

public abstract class Elem {
    public abstract Type getType<T> ();
}

public sealed class TextElem : Elem {
    public override Type getType<T> () { return typeof (T); }
}

public class OtherTextElem : Elem {
    public sealed override Type getType<T> () { return typeof (T); }
}

public class main {
    public static int Main () {
	TextElem elem = new TextElem ();

	if (elem.getType<string> () != typeof (string))
	    return 1;

	OtherTextElem oelem = new OtherTextElem ();

	if (oelem.getType<string> () != typeof (string))
	    return 1;

	return 0;
    }
}
