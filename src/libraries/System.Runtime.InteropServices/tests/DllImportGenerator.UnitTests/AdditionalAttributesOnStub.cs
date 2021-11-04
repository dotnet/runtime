// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace DllImportGenerator.UnitTests
{
    public class AdditionalAttributesOnStub
    {
        [ConditionalFact]
        public async Task SkipLocalsInitAdded()
        {
            string source = @"
using System.Runtime.InteropServices;
partial class C
{
    [GeneratedDllImportAttribute(""DoesNotExist"")]
    public static partial S Method();
}

[NativeMarshalling(typeof(Native))]
struct S
{
}

struct Native
{
    public Native(S s) { }
    public S ToManaged() { return default; }
}";
            Compilation comp = await TestUtils.CreateCompilation(source);

            Compilation newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.DllImportGenerator());

            ITypeSymbol c = newComp.GetTypeByMetadataName("C")!;
            IMethodSymbol stubMethod = c.GetMembers().OfType<IMethodSymbol>().Single(m => m.Name == "Method");
            Assert.Contains(stubMethod.GetAttributes(), attr => attr.AttributeClass!.ToDisplayString() == typeof(SkipLocalsInitAttribute).FullName);
        }

        [ConditionalFact]
        public async Task SkipLocalsInitNotAddedOnForwardingStub()
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
            Assert.DoesNotContain(stubMethod.GetAttributes(), attr => attr.AttributeClass!.ToDisplayString() == typeof(SkipLocalsInitAttribute).FullName);
        }

        public static IEnumerable<object[]> GetDownlevelTargetFrameworks()
        {
            yield return new object[] { TestTargetFramework.Net, true };
            yield return new object[] { TestTargetFramework.Net6, true };
            yield return new object[] { TestTargetFramework.Net5, false };
            yield return new object[] { TestTargetFramework.Core, false };
            yield return new object[] { TestTargetFramework.Standard, false };
            yield return new object[] { TestTargetFramework.Framework, false };
        }

        [ConditionalTheory]
        [MemberData(nameof(GetDownlevelTargetFrameworks))]
        public async Task SkipLocalsInitOnDownlevelTargetFrameworks(TestTargetFramework targetFramework, bool expectSkipLocalsInit)
        {
            string source = $@"
using System.Runtime.InteropServices;
{CodeSnippets.GeneratedDllImportAttributeDeclaration}
namespace System.Runtime.InteropServices
{{
    sealed class NativeMarshallingAttribute : System.Attribute
    {{
        public NativeMarshallingAttribute(System.Type nativeType) {{ }}
    }}
}}
partial class C
{{
    [GeneratedDllImportAttribute(""DoesNotExist"")]
    public static partial S Method();
}}

[NativeMarshalling(typeof(Native))]
struct S
{{
}}

struct Native
{{
    public Native(S s) {{ }}
    public S ToManaged() {{ return default; }}
}}";
            Compilation comp = await TestUtils.CreateCompilation(source, targetFramework);

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

        [ConditionalFact]
        public async Task SkipLocalsInitNotAddedWhenDefinedAtModuleLevel()
        {
            string source = @"
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
[module:SkipLocalsInit]
partial class C
{
    [GeneratedDllImportAttribute(""DoesNotExist"")]
    public static partial S Method();
}

[NativeMarshalling(typeof(Native))]
struct S
{
}

struct Native
{
    public Native(S s) { }
    public S ToManaged() { return default; }
}";
            Compilation comp = await TestUtils.CreateCompilation(source);

            Compilation newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.DllImportGenerator());

            ITypeSymbol c = newComp.GetTypeByMetadataName("C")!;
            IMethodSymbol stubMethod = c.GetMembers().OfType<IMethodSymbol>().Single(m => m.Name == "Method");
            Assert.DoesNotContain(stubMethod.GetAttributes(), attr => attr.AttributeClass!.ToDisplayString() == typeof(SkipLocalsInitAttribute).FullName);
        }

        [ConditionalFact]
        public async Task SkipLocalsInitNotAddedWhenDefinedAtClassLevel()
        {
            string source = @"
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
[SkipLocalsInit]
partial class C
{
    [GeneratedDllImportAttribute(""DoesNotExist"")]
    public static partial S Method();
}

[NativeMarshalling(typeof(Native))]
struct S
{
}

struct Native
{
    public Native(S s) { }
    public S ToManaged() { return default; }
}";
            Compilation comp = await TestUtils.CreateCompilation(source);

            Compilation newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.DllImportGenerator());

            ITypeSymbol c = newComp.GetTypeByMetadataName("C")!;
            IMethodSymbol stubMethod = c.GetMembers().OfType<IMethodSymbol>().Single(m => m.Name == "Method");
            Assert.DoesNotContain(stubMethod.GetAttributes(), attr => attr.AttributeClass!.ToDisplayString() == typeof(SkipLocalsInitAttribute).FullName);
        }

        [ConditionalFact]
        public async Task SkipLocalsInitNotAddedWhenDefinedOnMethodByUser()
        {
            string source = @"
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
partial class C
{
    [SkipLocalsInit]
    [GeneratedDllImportAttribute(""DoesNotExist"")]
    public static partial S Method();
}

[NativeMarshalling(typeof(Native))]
struct S
{
}

struct Native
{
    public Native(S s) { }
    public S ToManaged() { return default; }
}";
            Compilation comp = await TestUtils.CreateCompilation(source);

            Compilation newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.DllImportGenerator());

            ITypeSymbol c = newComp.GetTypeByMetadataName("C")!;
            IMethodSymbol stubMethod = c.GetMembers().OfType<IMethodSymbol>().Single(m => m.Name == "Method");
            Assert.DoesNotContain(newComp.GetDiagnostics(), d => d.Id != "CS0579"); // No duplicate attribute error
        }
    }
}
