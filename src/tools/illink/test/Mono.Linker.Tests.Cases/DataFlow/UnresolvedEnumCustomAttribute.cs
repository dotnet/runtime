using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
    [IgnoreTestCase("Ignore in NativeAOT, see https://github.com/dotnet/runtime/issues/82447", IgnoredBy = Tool.NativeAot)]
    [KeptAttributeAttribute(typeof(IgnoreTestCaseAttribute), By = Tool.Trimmer)]
    [SkipILVerify]
    [SkipUnresolved(true)]
    [SetupCompileBefore("UnresolvedEnumLibrary.dll", new[] { "Dependencies/UnresolvedEnumLibrary.cs" }, removeFromLinkerInput: true)]
    [SetupCompileBefore("UnresolvedAttributeLibrary.dll", new[] { "Dependencies/UnresolvedAttributeLibrary.cs" }, references: new[] { "UnresolvedEnumLibrary.dll" })]
    [KeptMemberInAssembly("UnresolvedAttributeLibrary.dll", "Mono.Linker.Tests.Cases.DataFlow.Dependencies.UnresolvedEnumAttribute", new[] {
        "set_RequiredType(System.Type)",
        "set_EnumValue(Mono.Linker.Tests.Cases.DataFlow.Dependencies.UnresolvedEnum)",
        "set_Other(System.Int32)"
    }, Tool = Tool.NativeAot)]
    [ExpectedNoWarnings]
    class UnresolvedEnumCustomAttribute
    {
        public static void Main()
        {
            Dependencies.UnresolvedAttributeLibrary.Use();
        }
    }
}
