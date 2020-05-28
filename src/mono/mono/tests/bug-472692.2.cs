public class Program
{
    static void Main()
    {
        var t = new Foo<object>();
        var asMethod = t.GetType().GetMethod("Bar");
        var asInterface = asMethod.MakeGenericMethod(typeof(object));
        var asMock = asInterface.Invoke(t, null);
    }
}

public class Foo<T>
{
    public virtual Helper<TInt> Bar<TInt>()
    {
        return new Helper<TInt>();
    }
}

public class Helper<T> { }
