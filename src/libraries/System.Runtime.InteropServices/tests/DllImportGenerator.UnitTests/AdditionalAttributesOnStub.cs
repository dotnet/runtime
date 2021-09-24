using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DllImportGenerator.UnitTests
{
    public class AdditionalAttributesOnStub
    {
        [Fact]
        public async Task SkipLocalsInitAdded()
        {
            string source = @"
using System.Runtime.InteropServices;
partial class C
{
    [GeneratedDllImportAttribute(""DoesNotExist"")]
    public static partial void Method();
}";
            Compilation comp = await TestUtils.CreateCompilation(source);

            Compilation newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.DllImportGenerator());

            ITypeSymbol c = newComp.GetTypeByMetadataName("C")!;
            IMethodSymbol stubMethod = c.GetMembers().OfType<IMethodSymbol>().Single(m => m.Name == "Method");
            Assert.Contains(stubMethod.GetAttributes(), attr => attr.AttributeClass!.ToDisplayString() == typeof(SkipLocalsInitAttribute).FullName);
        }

        public static IEnumerable<object[]> GetDownlevelTargetFrameworks()
        {
            yield return new object[] { ReferenceAssemblies.Net.Net50, true };
            yield return new object[] { ReferenceAssemblies.NetCore.NetCoreApp31, false };
            yield return new object[] { ReferenceAssemblies.NetStandard.NetStandard20, false };
            yield return new object[] { ReferenceAssemblies.NetFramework.Net48.Default, false };
        }

        [Theory]
        [MemberData(nameof(GetDownlevelTargetFrameworks))]
        public async Task SkipLocalsInitOnDownlevelTargetFrameworks(ReferenceAssemblies referenceAssemblies, bool expectSkipLocalsInit)
        {
            string source = @"
using System.Runtime.InteropServices;
namespace System.Runtime.InteropServices
{
    sealed class GeneratedDllImportAttribute : System.Attribute
    {
        public GeneratedDllImportAttribute(string a) { }
    }
}partial class C
{
    [GeneratedDllImportAttribute(""DoesNotExist"")]
    public static partial void Method();
}";
            Compilation comp = await TestUtils.CreateCompilationWithReferenceAssemblies(source, referenceAssemblies);

            Compilation newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.DllImportGenerator());

            ITypeSymbol c = newComp.GetTypeByMetadataName("C")!;
            IMethodSymbol stubMethod = c.GetMembers().OfType<IMethodSymbol>().Single(m => m.Name == "Method");
            if (expectSkipLocalsInit)
            {
                Assert.Contains(stubMethod.GetAttributes(), attr => attr.AttributeClass!.ToDisplayString() == typeof(SkipLocalsInitAttribute).FullName);
            }
            else
            {
                Assert.DoesNotContain(stubMethod.GetAttributes(), attr => attr.AttributeClass!.ToDisplayString() == typeof(SkipLocalsInitAttribute).FullName);
            }
        }

        [Fact]
        public async Task SkipLocalsInitNotAddedWhenDefinedAtModuleLevel()
        {
            string source = @"
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
[module:SkipLocalsInit]
partial class C
{
    [GeneratedDllImportAttribute(""DoesNotExist"")]
    public static partial void Method();
}";
            Compilation comp = await TestUtils.CreateCompilation(source);

            Compilation newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.DllImportGenerator());

            ITypeSymbol c = newComp.GetTypeByMetadataName("C")!;
            IMethodSymbol stubMethod = c.GetMembers().OfType<IMethodSymbol>().Single(m => m.Name == "Method");
            Assert.DoesNotContain(stubMethod.GetAttributes(), attr => attr.AttributeClass!.ToDisplayString() == typeof(SkipLocalsInitAttribute).FullName);
        }

        [Fact]
        public async Task SkipLocalsInitNotAddedWhenDefinedAtClassLevel()
        {
            string source = @"
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
[SkipLocalsInit]
partial class C
{
    [GeneratedDllImportAttribute(""DoesNotExist"")]
    public static partial void Method();
}";
            Compilation comp = await TestUtils.CreateCompilation(source);

            Compilation newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.DllImportGenerator());

            ITypeSymbol c = newComp.GetTypeByMetadataName("C")!;
            IMethodSymbol stubMethod = c.GetMembers().OfType<IMethodSymbol>().Single(m => m.Name == "Method");
            Assert.DoesNotContain(stubMethod.GetAttributes(), attr => attr.AttributeClass!.ToDisplayString() == typeof(SkipLocalsInitAttribute).FullName);
        }

        [Fact]
        public async Task SkipLocalsInitNotAddedWhenDefinedOnMethodByUser()
        {
            string source = @"
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
partial class C
{
    [SkipLocalsInit]
    [GeneratedDllImportAttribute(""DoesNotExist"")]
    public static partial void Method();
}";
            Compilation comp = await TestUtils.CreateCompilation(source);

            Compilation newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.DllImportGenerator());

            ITypeSymbol c = newComp.GetTypeByMetadataName("C")!;
            IMethodSymbol stubMethod = c.GetMembers().OfType<IMethodSymbol>().Single(m => m.Name == "Method");
            Assert.DoesNotContain(newComp.GetDiagnostics(), d => d.Id != "CS0579"); // No duplicate attribute error
        }
    }
}
