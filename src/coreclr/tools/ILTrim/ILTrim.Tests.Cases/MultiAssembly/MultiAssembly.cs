
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.MultiAssembly
{
    [SetupLinkerAction ("link", "Dep")]
    [SetupCompileBefore("Dep.dll", new[] { "Dependencies/Dep.cs" })]
    public class MultiAssembly
    {
        public static void Main()
        {
            DepClass.Kept();
        }
    }
}