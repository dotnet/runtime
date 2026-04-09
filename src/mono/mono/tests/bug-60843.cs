using System;
using System.Runtime.CompilerServices;

class A : Attribute
{
    public object X;

    public static void Main()
    {
        var x = (C<int>.E)AttributeTest(typeof(C<>.E));
        Assert(C<int>.E.V == x);
        var y = (C<int>.E2[])AttributeTest(typeof(C<>.E2));
        Assert(y.Length == 2);
        Assert(y[0] == C<int>.E2.A);
        Assert(y[1] == C<int>.E2.B);
    }

    public static object AttributeTest (Type t) {
        var cas = t.GetCustomAttributes(false);
        Assert(cas.Length == 1);
        Assert(cas[0] is A);
        var a = (A)cas[0];
        return a.X;
    }

    private static int AssertCount = 0;

    public static void Assert (
        bool b, 
        [CallerFilePath] string sourceFile = null, 
        [CallerLineNumber] int lineNumber = 0
    ) {
        AssertCount++;

        if (!b) {
            Console.Error.WriteLine($"Assert failed at {sourceFile}:{lineNumber}");
            Environment.Exit(AssertCount);
        }
    }
}
 
public class C<T>
{
    [A(X = C<int>.E.V)]
    public enum E { V }

    [A(X = new [] { C<int>.E2.A, C<int>.E2.B })]
    public enum E2 { A, B }
}