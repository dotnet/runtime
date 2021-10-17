using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Basic
{
#pragma warning disable 169

    [SetupLinkerArgument("-a", "test.exe", "library")]
    [SetupLinkerArgument("--enable-opt", "ipconstprop")]
    public class LibraryModeTest
    {
        static int Field;

        [Kept]
        public LibraryModeTest() { }

        [Kept]
        public void M1() { }

        [Kept]
        public void M2() { }

        [Kept]
        public void M3() { }

        [Kept]
        public static int Main()
        {
            return 42;
        }
        void P_M1() { }
        void P_M2() { }
        void P_M3() { }
    }

    internal class InternalType
    {
        public void M1() { }
    }

    [Kept]
    [KeptMember(".ctor()")] // Public types are assumed to be constructed
    public class AnotherPublicType
    {
        [Kept]
        public void M1() { }
    }
}
