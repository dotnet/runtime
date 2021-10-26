// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DllImportGenerator.UnitTests
{
    public class AttributeForwarding
    {
        [ConditionalTheory]
        [InlineData("SuppressGCTransition", "System.Runtime.InteropServices.SuppressGCTransitionAttribute")]
        [InlineData("UnmanagedCallConv", "System.Runtime.InteropServices.UnmanagedCallConvAttribute")]
        public async Task KnownParameterlessAttribute(string attributeSourceName, string attributeMetadataName)
        {
            string source = @$"
using System.Runtime.InteropServices;
partial class C
{{
    [{attributeSourceName}]
    [GeneratedDllImportAttribute(""DoesNotExist"")]
    public static partial S Method1();
}}

[NativeMarshalling(typeof(Native))]
struct S
{{
}}

struct Native
{{
    public Native(S s) {{ }}
    public S ToManaged() {{ return default; }}
}}
";
            Compilation origComp = await TestUtils.CreateCompilation(source);
            Compilation newComp = TestUtils.RunGenerators(origComp, out _, new Microsoft.Interop.DllImportGenerator());
            Assert.Empty(newComp.GetDiagnostics());

            ITypeSymbol attributeType = newComp.GetTypeByMetadataName(attributeMetadataName)!;

            Assert.NotNull(attributeType);

            IMethodSymbol targetMethod = GetGeneratedPInvokeTargetFromCompilation(newComp);

            Assert.Contains(
                targetMethod.GetAttributes(),
                attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeType));
        }

        [ConditionalFact]
        public async Task UnmanagedCallConvAttribute_EmptyCallConvArray()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;
partial class C
{
    [UnmanagedCallConv(CallConvs = new Type[0])]
    [GeneratedDllImportAttribute(""DoesNotExist"")]
    public static partial S Method1();
}

[NativeMarshalling(typeof(Native))]
struct S
{
}

struct Native
{
    public Native(S s) { }
    public S ToManaged() { return default; }
}
";
            Compilation origComp = await TestUtils.CreateCompilation(source);
            Compilation newComp = TestUtils.RunGenerators(origComp, out _, new Microsoft.Interop.DllImportGenerator());
            Assert.Empty(newComp.GetDiagnostics());

            ITypeSymbol attributeType = newComp.GetTypeByMetadataName("System.Runtime.InteropServices.UnmanagedCallConvAttribute")!;

            Assert.NotNull(attributeType);

            IMethodSymbol targetMethod = GetGeneratedPInvokeTargetFromCompilation(newComp);

            Assert.Contains(
                targetMethod.GetAttributes(),
                attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeType)
                    && attr.NamedArguments.Length == 1
                    && attr.NamedArguments[0].Key == "CallConvs"
                    && attr.NamedArguments[0].Value.Values.Length == 0);
        }

        [ConditionalFact]
        public async Task UnmanagedCallConvAttribute_SingleCallConvType()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
partial class C
{
    [UnmanagedCallConv(CallConvs = new[]{typeof(CallConvStdcall)})]
    [GeneratedDllImportAttribute(""DoesNotExist"")]
    public static partial S Method1();
}

[NativeMarshalling(typeof(Native))]
struct S
{
}

struct Native
{
    public Native(S s) { }
    public S ToManaged() { return default; }
}
";
            Compilation origComp = await TestUtils.CreateCompilation(source);
            Compilation newComp = TestUtils.RunGenerators(origComp, out _, new Microsoft.Interop.DllImportGenerator());
            Assert.Empty(newComp.GetDiagnostics());

            ITypeSymbol attributeType = newComp.GetTypeByMetadataName("System.Runtime.InteropServices.UnmanagedCallConvAttribute")!;
            ITypeSymbol callConvType = newComp.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvStdcall")!;

            Assert.NotNull(attributeType);

            IMethodSymbol targetMethod = GetGeneratedPInvokeTargetFromCompilation(newComp);

            Assert.Contains(
                targetMethod.GetAttributes(),
                attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeType)
                    && attr.NamedArguments.Length == 1
                    && attr.NamedArguments[0].Key == "CallConvs"
                    && attr.NamedArguments[0].Value.Values.Length == 1
                    && SymbolEqualityComparer.Default.Equals(
                        (INamedTypeSymbol?)attr.NamedArguments[0].Value.Values[0].Value!,
                        callConvType));
        }

        [ConditionalFact]
        public async Task UnmanagedCallConvAttribute_MultipleCallConvTypes()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
partial class C
{
    [UnmanagedCallConv(CallConvs = new[]{typeof(CallConvStdcall), typeof(CallConvSuppressGCTransition)})]
    [GeneratedDllImportAttribute(""DoesNotExist"")]
    public static partial S Method1();
}

[NativeMarshalling(typeof(Native))]
struct S
{
}

struct Native
{
    public Native(S s) { }
    public S ToManaged() { return default; }
}
";
            Compilation origComp = await TestUtils.CreateCompilation(source);
            Compilation newComp = TestUtils.RunGenerators(origComp, out _, new Microsoft.Interop.DllImportGenerator());
            Assert.Empty(newComp.GetDiagnostics());

            ITypeSymbol attributeType = newComp.GetTypeByMetadataName("System.Runtime.InteropServices.UnmanagedCallConvAttribute")!;
            ITypeSymbol callConvType = newComp.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvStdcall")!;
            ITypeSymbol callConvType2 = newComp.GetTypeByMetadataName("System.Runtime.CompilerServices.CallConvSuppressGCTransition")!;

            Assert.NotNull(attributeType);

            IMethodSymbol targetMethod = GetGeneratedPInvokeTargetFromCompilation(newComp);

            Assert.Contains(
                targetMethod.GetAttributes(),
                attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeType)
                    && attr.NamedArguments.Length == 1
                    && attr.NamedArguments[0].Key == "CallConvs"
                    && attr.NamedArguments[0].Value.Values.Length == 2
                    && SymbolEqualityComparer.Default.Equals(
                        (INamedTypeSymbol?)attr.NamedArguments[0].Value.Values[0].Value!,
                        callConvType)
                    && SymbolEqualityComparer.Default.Equals(
                        (INamedTypeSymbol?)attr.NamedArguments[0].Value.Values[1].Value!,
                        callConvType2));
        }

        [ConditionalFact]
        public async Task OtherAttributeType()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

class OtherAttribute : Attribute {}

partial class C
{
    [Other]
    [GeneratedDllImportAttribute(""DoesNotExist"")]
    public static partial S Method1();
}

[NativeMarshalling(typeof(Native))]
struct S
{
}

struct Native
{
    public Native(S s) { }
    public S ToManaged() { return default; }
}
";
            Compilation origComp = await TestUtils.CreateCompilation(source);
            Compilation newComp = TestUtils.RunGenerators(origComp, out _, new Microsoft.Interop.DllImportGenerator());

            Assert.Empty(newComp.GetDiagnostics());

            ITypeSymbol attributeType = newComp.GetTypeByMetadataName("OtherAttribute")!;

            Assert.NotNull(attributeType);

            IMethodSymbol targetMethod = GetGeneratedPInvokeTargetFromCompilation(newComp);

            Assert.DoesNotContain(
                targetMethod.GetAttributes(),
                attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeType));
        }

        private static IMethodSymbol GetGeneratedPInvokeTargetFromCompilation(Compilation newComp)
        {
            // The last syntax tree is the generated code
            SyntaxTree generatedCode = newComp.SyntaxTrees.Last();
            SemanticModel model = newComp.GetSemanticModel(generatedCode);

            var localFunctions = generatedCode.GetRoot()
                .DescendantNodes().OfType<LocalFunctionStatementSyntax>()
                .ToList();
            LocalFunctionStatementSyntax innerDllImport = Assert.Single(localFunctions);
            IMethodSymbol targetMethod = (IMethodSymbol)model.GetDeclaredSymbol(innerDllImport)!;
            return targetMethod;
        }
    }
}
