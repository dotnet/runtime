using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Libraries.Dependencies;

#if RootLibraryVisibleForwarders
[assembly: TypeForwardedTo(typeof(ExternalPublic))]
#endif

namespace Mono.Linker.Tests.Cases.Libraries
{
    [IgnoreTestCase("NativeAOT doesn't implement library trimming the same way", IgnoredBy = Tool.NativeAot)]
    [KeptAttributeAttribute(typeof(IgnoreTestCaseAttribute), By = Tool.Trimmer)]

    [SetupCompileBefore("library.dll", new[] { "Dependencies/RootLibraryVisibleForwarders_Lib.cs" })]
    [SetupCompileBefore("target.exe", new[] { "Dependencies/RootLibraryVisibleForwarderTargetProcessedFirst.cs" })]
    [SetupCompileAfter("forwarder.dll", new[] { "Dependencies/RootLibraryVisibleForwarderTargetProcessedFirst_Forwarder.cs" }, references: new[] { "target.exe" })]
    [SetupLinkerLinkPublicAndFamily]
    [SetupLinkerArgument("-a", "target", "entrypoint")]
    [SetupLinkerArgument("-a", "forwarder", "visible")]
    [Define("RootLibraryVisibleForwarders")]

    [Kept]
    [KeptMember(".ctor()")]
    [KeptExportedType(typeof(ExternalPublic))]
    [KeptMemberInAssembly("library.dll", typeof(ExternalPublic), "ProtectedMethod()")]
    [KeptTypeInAssembly("forwarder.dll", typeof(RootLibraryVisibleForwarderTargetProcessedFirst))]
    [KeptMemberInAssembly("target.exe", typeof(RootLibraryVisibleForwarderTargetProcessedFirst), "UnusedPublicMethod()")]
    public class RootLibraryVisibleForwarders
    {
        [Kept]
        public static void Main()
        {
        }
    }
}
