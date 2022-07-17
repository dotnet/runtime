// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace LibraryImportGenerator.UnitTests
{
    public class AdditionalAttributesOnStub
    {
        [Fact]
        public async Task SkipLocalsInitAdded()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
[assembly:DisableRuntimeMarshalling]
partial class C
{
    [LibraryImportAttribute(""DoesNotExist"")]
    public static partial S Method();
}

[NativeMarshalling(typeof(Marshaller))]
struct S
{
}

struct Native
{
}

[CustomMarshaller(typeof(S), MarshalMode.Default, typeof(Marshaller))]
static class Marshaller
{
    public static Native ConvertToUnmanaged(S s) => default;

    public static S ConvertToManaged(Native n) => default;
}";
            Compilation comp = await TestUtils.CreateCompilation(source);

            Compilation newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.LibraryImportGenerator());

            ITypeSymbol c = newComp.GetTypeByMetadataName("C")!;
            IMethodSymbol stubMethod = c.GetMembers().OfType<IMethodSymbol>().Single(m => m.Name == "Method");
            Assert.Contains(stubMethod.GetAttributes(), attr => attr.AttributeClass!.ToDisplayString() == typeof(SkipLocalsInitAttribute).FullName);
        }

        [Fact]
        public async Task SkipLocalsInitNotAddedOnForwardingStub()
        {
            string source = @"
using System.Runtime.InteropServices;
partial class C
{
    [LibraryImportAttribute(""DoesNotExist"")]
    public static partial void Method();
}";
            Compilation comp = await TestUtils.CreateCompilation(source);

            Compilation newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.LibraryImportGenerator());

            ITypeSymbol c = newComp.GetTypeByMetadataName("C")!;
            IMethodSymbol stubMethod = c.GetMembers().OfType<IMethodSymbol>().Single(m => m.Name == "Method");
            Assert.DoesNotContain(stubMethod.GetAttributes(), attr => attr.AttributeClass!.ToDisplayString() == typeof(SkipLocalsInitAttribute).FullName);
        }

        [Fact]
        public async Task GeneratedCodeAdded()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
[assembly:DisableRuntimeMarshalling]
partial class C
{
    [LibraryImportAttribute(""DoesNotExist"")]
    public static partial S Method();
}

[NativeMarshalling(typeof(Marshaller))]
struct S
{
}

struct Native
{
}

[CustomMarshaller(typeof(S), MarshalMode.Default, typeof(Marshaller))]
static class Marshaller
{
    public static Native ConvertToUnmanaged(S s) => default;

    public static S ConvertToManaged(Native n) => default;
}";
            Compilation comp = await TestUtils.CreateCompilation(source);

            Compilation newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.LibraryImportGenerator());

            ITypeSymbol c = newComp.GetTypeByMetadataName("C")!;
            IMethodSymbol stubMethod = c.GetMembers().OfType<IMethodSymbol>().Single(m => m.Name == "Method");
            Assert.Contains(stubMethod.GetAttributes(), attr => attr.AttributeClass!.ToDisplayString() == typeof(System.CodeDom.Compiler.GeneratedCodeAttribute).FullName);
        }

        [Fact]
        public async Task GeneratedCodeNotAddedOnForwardingStub()
        {
            string source = @"
using System.Runtime.InteropServices;
partial class C
{
    [LibraryImportAttribute(""DoesNotExist"")]
    public static partial void Method();
}";
            Compilation comp = await TestUtils.CreateCompilation(source);

            Compilation newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.LibraryImportGenerator());

            ITypeSymbol c = newComp.GetTypeByMetadataName("C")!;
            IMethodSymbol stubMethod = c.GetMembers().OfType<IMethodSymbol>().Single(m => m.Name == "Method");
            Assert.DoesNotContain(stubMethod.GetAttributes(), attr => attr.AttributeClass!.ToDisplayString() == typeof(System.CodeDom.Compiler.GeneratedCodeAttribute).FullName);
        }

        public static IEnumerable<object[]> GetDownlevelTargetFrameworks()
        {
            yield return new object[] { TestTargetFramework.Net, true };
            yield return new object[] { TestTargetFramework.Net6, true };
            yield return new object[] { TestTargetFramework.Net5, true };
            yield return new object[] { TestTargetFramework.Core, false };
            yield return new object[] { TestTargetFramework.Standard, false };
            yield return new object[] { TestTargetFramework.Framework, false };
        }

        [Theory]
        [MemberData(nameof(GetDownlevelTargetFrameworks))]
        [OuterLoop("Uses the network for downlevel ref packs")]
        public async Task SkipLocalsInitOnDownlevelTargetFrameworks(TestTargetFramework targetFramework, bool expectSkipLocalsInit)
        {
            string source = $@"
using System.Runtime.InteropServices;
{CodeSnippets.LibraryImportAttributeDeclaration}
partial class C
{{
    [LibraryImportAttribute(""DoesNotExist"")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool Method();
}}";
            Compilation comp = await TestUtils.CreateCompilation(source, targetFramework);

            Compilation newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.LibraryImportGenerator());

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
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.CompilerServices;
[module:SkipLocalsInit]
partial class C
{
    [LibraryImportAttribute(""DoesNotExist"")]
    public static partial S Method();
}

[NativeMarshalling(typeof(Marshaller))]
struct S
{
}

struct Native
{
}

[CustomMarshaller(typeof(S), MarshalMode.Default, typeof(Marshaller))]
static class Marshaller
{
    public static Native ConvertToUnmanaged(S s) => default;

    public static S ConvertToManaged(Native n) => default;
}";
            Compilation comp = await TestUtils.CreateCompilation(source);

            Compilation newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.LibraryImportGenerator());

            ITypeSymbol c = newComp.GetTypeByMetadataName("C")!;
            IMethodSymbol stubMethod = c.GetMembers().OfType<IMethodSymbol>().Single(m => m.Name == "Method");
            Assert.DoesNotContain(stubMethod.GetAttributes(), attr => attr.AttributeClass!.ToDisplayString() == typeof(SkipLocalsInitAttribute).FullName);
        }

        [Fact]
        public async Task SkipLocalsInitNotAddedWhenDefinedAtClassLevel()
        {
            string source = @"
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.CompilerServices;
[SkipLocalsInit]
partial class C
{
    [LibraryImportAttribute(""DoesNotExist"")]
    public static partial S Method();
}

[NativeMarshalling(typeof(Marshaller))]
struct S
{
}

struct Native
{
}

[CustomMarshaller(typeof(S), MarshalMode.Default, typeof(Marshaller))]
static class Marshaller
{
    public static Native ConvertToUnmanaged(S s) => default;

    public static S ConvertToManaged(Native n) => default;
}";
            Compilation comp = await TestUtils.CreateCompilation(source);

            Compilation newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.LibraryImportGenerator());

            ITypeSymbol c = newComp.GetTypeByMetadataName("C")!;
            IMethodSymbol stubMethod = c.GetMembers().OfType<IMethodSymbol>().Single(m => m.Name == "Method");
            Assert.DoesNotContain(stubMethod.GetAttributes(), attr => attr.AttributeClass!.ToDisplayString() == typeof(SkipLocalsInitAttribute).FullName);
        }

        [Fact]
        public async Task SkipLocalsInitNotAddedWhenDefinedOnMethodByUser()
        {
            string source = @"
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Runtime.CompilerServices;
partial class C
{
    [SkipLocalsInit]
    [LibraryImportAttribute(""DoesNotExist"")]
    public static partial S Method();
}

[NativeMarshalling(typeof(Marshaller))]
struct S
{
}

struct Native
{
}

[CustomMarshaller(typeof(S), MarshalMode.Default, typeof(Marshaller))]
static class Marshaller
{
    public static Native ConvertToUnmanaged(S s) => default;

    public static S ConvertToManaged(Native n) => default;
}";
            Compilation comp = await TestUtils.CreateCompilation(source);

            Compilation newComp = TestUtils.RunGenerators(comp, out _, new Microsoft.Interop.LibraryImportGenerator());

            ITypeSymbol c = newComp.GetTypeByMetadataName("C")!;
            IMethodSymbol stubMethod = c.GetMembers().OfType<IMethodSymbol>().Single(m => m.Name == "Method");
            Assert.DoesNotContain(newComp.GetDiagnostics(), d => d.Id != "CS0579"); // No duplicate attribute error
        }
    }
}
