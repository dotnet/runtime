using System;

public class Program
{
    static void Main()
    {
        try
        {
            new Foo<object>(0, 0);
        }
        catch (Exception ex)
        {
          var t = ex.StackTrace;
        }
    }
}

public class Foo<T> : FooBase
{
    int i;
    int ii;

    public Foo(int i, int ii)
    {
        this.i = i;
        this.ii = ii;
        this.iii = 0;
        throw new Exception();
    }
}

public abstract class FooBase
{
    protected int iii;
}
