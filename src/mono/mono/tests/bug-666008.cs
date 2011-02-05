using System;

abstract class Foo<T>
{
    public virtual int OnReloaded () {
        Console.WriteLine ("HIT!");
		return 0;
    }
}

class Bar<T> : Foo<T>
{
    public int DoIt (Func<int> a) {
        return a ();
    }

    public override int OnReloaded () {
        return DoIt (base.OnReloaded);
    }
}

public class Tests
{
    public static int Main (String[] args) {
        var b = new Bar<string> ();
        return b.OnReloaded ();
    }
}
