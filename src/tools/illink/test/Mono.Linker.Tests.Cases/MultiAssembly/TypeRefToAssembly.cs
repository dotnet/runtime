using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.MultiAssembly
{
    [SetupLinkerAction("link", "lib")]
    [SetupCompileBefore("lib.dll", new[] { "Dependencies/TypeRefToAssembly_Library.cs" })]

    [KeptTypeInAssembly("lib.dll", typeof(TypeRefToAssembly_Library.TestType))]
    public class TypeRefToAssembly
    {
        public static void Main()
        {
            var t = typeof(TypeRefToAssembly_Library.TestType);
        }
    }
}
