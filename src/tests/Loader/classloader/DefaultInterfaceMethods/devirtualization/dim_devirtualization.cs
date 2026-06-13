using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

public class Program
{
    [Fact]
    public static void TestEntryPoint()
    {
        GitHubIssue39419RegressionCase.Run();
        ConstrainedGenericInlineCase.Run();
        MultiInterfaceDefaultMethodCases.Run();
        NonGenericDefaultMethodCase.Run();
        OverriddenDefaultMethodCases.Run();
        ValueTypeArgumentDefaultMethodCase.Run();
        StructNonDefaultImplementationCase.Run();
        ContravariantDefaultMethodCase.Run();
        DirectStructMethodCase.Run();
        StaticGenericMethodCase.Run();
    }
}

static class GitHubIssue39419RegressionCase
{
    interface IM<T>
    {
        bool UseDefaultM
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => true;
        }

        ValueTask M(T instance) => throw new NotImplementedException("M must be implemented if UseDefaultM is false");

        static ValueTask DefaultM(T instance)
        {
            return default;
        }
    }

    struct M : IM<int>
    {
    }

    public static void Run()
    {
        var m = new M();
        if (((IM<int>)m).UseDefaultM)
        {
            IM<int>.DefaultM(42);
            return;
        }

        ((IM<int>)m).M(42);
        throw new UnreachableException();
    }
}

static class ConstrainedGenericInlineCase
{
    interface I<T> where T : IComparable<T>
    {
        T GetAt(int i, T[] tx) => tx[i];
    }

    class C : I<string>
    {
    }

    private static readonly string[] s_values = new string[] { "test" };

    public static void Run()
    {
        I<string> c = new C();
        var dcs = c.GetAt(0, s_values);
        Assert.Equal("test", dcs);
    }
}

static class MultiInterfaceDefaultMethodCases
{
    interface I<T>
    {
        string DefaultTypeOf() => typeof(T).Name;
    }

    class Dummy
    {
    }

    class ClassImplementation : I<string>, I<object>, I<Dummy>
    {
        string I<Dummy>.DefaultTypeOf() => "C.Dummy";
    }

    struct StructImplementation : I<string>, I<object>, I<Dummy>
    {
        string I<Dummy>.DefaultTypeOf() => "C.Dummy";
    }

    public static void Run()
    {
        RunClassImplementation();
        RunStructImplementation();
    }

    static void RunClassImplementation()
    {
        var c = new ClassImplementation();
        var dcs = ((I<string>)c).DefaultTypeOf();
        Assert.Equal("String", dcs);
        var dos = ((I<object>)c).DefaultTypeOf();
        Assert.Equal("Object", dos);
        var dds = ((I<Dummy>)c).DefaultTypeOf();
        Assert.Equal("C.Dummy", dds);
    }

    static void RunStructImplementation()
    {
        var c = new StructImplementation();
        var dcs = ((I<string>)c).DefaultTypeOf();
        Assert.Equal("String", dcs);
        var dos = ((I<object>)c).DefaultTypeOf();
        Assert.Equal("Object", dos);
        var dds = ((I<Dummy>)c).DefaultTypeOf();
        Assert.Equal("C.Dummy", dds);
    }
}

static class NonGenericDefaultMethodCase
{
    interface I
    {
        string DefaultTypeOf() => typeof(string).Name;
    }

    class C : I
    {
    }

    public static void Run()
    {
        var c = new C();
        var dcs = ((I)c).DefaultTypeOf();
        Assert.Equal("String", dcs);
    }
}

static class OverriddenDefaultMethodCases
{
    interface I<T>
    {
        string DefaultTypeOf() => typeof(T).Name;
    }
    class C : I<string>
    {
        public string DefaultTypeOf() => "C.String";
    }

    class GenericC<T> : I<T>
    {
        public string DefaultTypeOf() => "C." + typeof(T).Name;
    }

    public static void Run()
    {
        RunNonGenericOverride();
        RunGenericOverride();
    }

    static void RunNonGenericOverride()
    {
        var c = new C();
        var dcs = ((I<string>)c).DefaultTypeOf();
        Assert.Equal("C.String", dcs);
    }

    static void RunGenericOverride()
    {
        var c = new GenericC<string>();
        var dcs = ((I<string>)c).DefaultTypeOf();
        Assert.Equal("C.String", dcs);
    }
}

static class ValueTypeArgumentDefaultMethodCase
{
    interface I<T>
    {
        string DefaultTypeOf() => typeof(T).Name;
    }

    class C : I<int>
    {
    }

    public static void Run()
    {
        var c = new C();
        var dcs = ((I<int>)c).DefaultTypeOf();
        Assert.Equal("Int32", dcs);
    }
}

static class StructNonDefaultImplementationCase
{
    interface I<T>
    {
        string DefaultTypeOf();
    }

    struct C<T> : I<T>
    {
        public string DefaultTypeOf() => "C." + typeof(T).Name;
    }

    public static void Run()
    {
        var c = new C<string>();
        var dcs = ((I<string>)c).DefaultTypeOf();
        Assert.Equal("C.String", dcs);
    }
}

static class ContravariantDefaultMethodCase
{
    interface I<in T>
    {
        string DefaultTypeOf() => typeof(T).Name;
    }

    class C : I<object>
    {
    }

    public static void Run()
    {
        var c = new C();
        var dcs = ((I<string>)c).DefaultTypeOf();
        Assert.Equal("Object", dcs);
    }
}

static class DirectStructMethodCase
{
    struct C<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public string DefaultTypeOf() => typeof(T).Name;
    }

    public static void Run()
    {
        var c = new C<string>();
        var dcs = c.DefaultTypeOf();
        Assert.Equal("String", dcs);
    }
}

static class StaticGenericMethodCase
{
    class C<T>
    {
        public static string DefaultTypeOf() => typeof(T).Name;
    }

    public static void Run()
    {
        var dcs = C<string>.DefaultTypeOf();
        Assert.Equal("String", dcs);
    }
}

