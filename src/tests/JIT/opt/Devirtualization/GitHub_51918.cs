using System;
using Xunit;

public interface IGeneric<T>
{
    int InterfaceMethod();
}

public interface IMakeClassMethodSealedVirtual
{
    int ClassMethod();
}

public class GenericClass<A,B> : IMakeClassMethodSealedVirtual
{
    public static int _sv1;
    public static int _sv2;

    public int _v1;
    public int _v2;

    public GenericClass()
    {
        _v1 = _sv1++;
        _v2 = _sv2++;
    }

    public int ClassMethod()
    {
        return _v1 - _v2;
    }

    public InnerClass GetInnerClass() => new InnerClass(this);
  
    public sealed class InnerClass : IGeneric<A>
    {
        GenericClass<A,B> _localPointer;

        public InnerClass(GenericClass<A,B> pointer)
        {
            _localPointer = pointer;
        }

        public int InterfaceMethod() => _localPointer.ClassMethod();
    }
}

public class GitHub_51918
{
    [Fact]
    public static int TestEntryPoint()
    {
        IGeneric<int> genInterface = new GenericClass<int, string>().GetInnerClass();
        // Validate that two levels of inlining we don't behave incorrectly due to generic
        // canonicalization. (Devirtualize the interface call, and then devirtualize/inline
        // the call to ClassMethod)
        Console.WriteLine(genInterface.InterfaceMethod());
        if (genInterface.InterfaceMethod() == 0)
            return 100;

        return 1;
    }
}
