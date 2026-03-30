
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.MultiAssembly
{
    [SetupLinkerAction ("link", "Dep")]
    [SetupCompileBefore("Dep.dll", new[] { "Dependencies/Dep.cs" })]

    [KeptMemberInAssembly("Dep.dll", typeof(DepClass), "Kept()")]
    [KeptMemberInAssembly("Dep.dll", typeof(DepClass), nameof(DepClass.KeptField))]
    public class MultiAssembly
    {
        public static void Main()
        {
            DepClass.Kept();
            DepClass.KeptField = 0;
        }
    }
}
