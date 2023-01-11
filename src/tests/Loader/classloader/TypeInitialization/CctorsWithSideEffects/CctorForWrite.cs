using System;
using System.Runtime.CompilerServices;

public class CorrectException : Exception
{
}

public class CCC
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Call() => throw new CorrectException();

    public static int Main()
    {
        try
        {
            ClassWithCctor.StaticField = Call();
        }
        catch (CorrectException)
        {
            return 100;
        }
        catch
        {
        }
        return 1;
    }
}

class ClassWithCctor
{
    public static int StaticField;
    static ClassWithCctor() => throw new Exception();
}
