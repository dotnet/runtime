using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
    #pragma warning disable 169

    [Kept]
    public class First
    {
        static int Field;

        [Kept]
        static int FirstMethod() => 300;

        [Kept]
        static int Main()
        {
            return FirstMethod();
        }

        static void LastMethod(int someParameter) { }
    }

    class FirstType
    {
        static void Method() { }
    }

    class AnotherType
    {
        static void Method() { }
    }
}
