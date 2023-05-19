using System;

/* Regression test for https://github.com/dotnet/runtime/issues/85821
 * ensure that self-referencing generic instances are initialized correctly and don't TLE
 */

public class Program
{
    public static int Main()
    {
        var test = typeof(Node<double>);
        var fit = test.GetField("Children").FieldType;
        if (fit == null)
            return 101;
        
        var test2 = typeof(Node2<int>);
        var fit2 = test2.GetField("Children").FieldType;
        if (fit2 == null)
            return 102;

        return 100;
    }
}

public class Node<T>
{
    public Node<T>[] Children;
}


public class Node2<T>
{
    public Node2<T>[][] Children;
}
