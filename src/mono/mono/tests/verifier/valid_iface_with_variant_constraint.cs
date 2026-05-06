using System;


interface IFoo<out T> {}

interface IFoo2<T> where T : IFoo<object> {}

public class Foo : IFoo2<IFoo<string>> {}

 
public class Test
{
    static int Main ()
    {
        return 0;
    }
}
