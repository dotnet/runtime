using System;
using System.Reflection;


public abstract class ABase
{
    public abstract object this[int index] { get; }
}

public sealed class Concrete<T> : ABase
    where T : class
{
    public override T this[int index]
    {
        get
        {
            throw null;
        }
    }
}

class Parent
{
    public virtual object Value { get; }
}

class Child<T> : Parent where T : class
{
    public override T Value { get => (T)base.Value; }
}

class Foo { }

class Program
{
    static int Main()
    {
        Type[] t = Assembly.GetExecutingAssembly().GetTypes();
        new Child<Foo>();

        return 100;
    }
}
