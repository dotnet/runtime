using System;
using System.Runtime.CompilerServices;
using Xunit;

public class InternalMethodImplTest
{
    [Fact]
    public static int TypeLoadExceptionMessageContainsMethodNameWhenInternalCallOnlyMethodIsCalled()
    {
        try
        {
            new F1();
            return -1;
        }
        catch (TypeLoadException ex)
        {
            return ex.Message.Contains("Internal call method 'F2.Foo' with non-zero RVA.") ? 100 : -1;
        }
        catch (Exception ex)
        {
            return -1;
        }
    }
}

class F1
{
    public F1()
    {
        var f2 = new F2();
    }
}

class F2
{
    [MethodImpl(MethodImplOptions.InternalCall)]
    public void Foo()
    {

    }
}