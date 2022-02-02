// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Interop;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
[assembly:DisableRuntimeMarshalling]
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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
[assembly:DisableRuntimeMarshalling]
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
[assembly:DisableRuntimeMarshalling]
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
[assembly:DisableRuntimeMarshalling]
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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
[assembly:DisableRuntimeMarshalling]

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

        [ConditionalFact]
        public async Task InOutAttributes_Forwarded_To_ForwardedParameter()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
partial class C
{
    [GeneratedDllImportAttribute(""DoesNotExist"")]
    public static partial bool Method1([In, Out] int[] a);
}
" + CodeSnippets.GeneratedDllImportAttributeDeclaration;
            Compilation origComp = await TestUtils.CreateCompilation(source, TestTargetFramework.Standard);
            Compilation newComp = TestUtils.RunGenerators(origComp, out _, new Microsoft.Interop.DllImportGenerator());

            IMethodSymbol targetMethod = GetGeneratedPInvokeTargetFromCompilation(newComp);

            INamedTypeSymbol marshalAsAttribute = newComp.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_MarshalAsAttribute)!;
            INamedTypeSymbol inAttribute = newComp.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_InAttribute)!;
            INamedTypeSymbol outAttribute = newComp.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_OutAttribute)!;
            Assert.Collection(targetMethod.Parameters,
                param => Assert.Collection(param.GetAttributes(),
                    attr =>
                    {
                        Assert.Equal(marshalAsAttribute, attr.AttributeClass, SymbolEqualityComparer.Default);
                        Assert.Equal(UnmanagedType.LPArray, (UnmanagedType)attr.ConstructorArguments[0].Value!);
                        Assert.Empty(attr.NamedArguments);
                    },
                    attr =>
                    {
                        Assert.Equal(inAttribute, attr.AttributeClass, SymbolEqualityComparer.Default);
                        Assert.Empty(attr.ConstructorArguments);
                        Assert.Empty(attr.NamedArguments);
                    },
                    attr =>
                    {
                        Assert.Equal(outAttribute, attr.AttributeClass, SymbolEqualityComparer.Default);
                        Assert.Empty(attr.ConstructorArguments);
                        Assert.Empty(attr.NamedArguments);
                    }));
        }

        [ConditionalFact]
        public async Task MarshalAsAttribute_Forwarded_To_ForwardedParameter()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
partial class C
{
    [GeneratedDllImportAttribute(""DoesNotExist"")]
    public static partial bool Method1([MarshalAs(UnmanagedType.I2)] int a);
}
" + CodeSnippets.GeneratedDllImportAttributeDeclaration;
            Compilation origComp = await TestUtils.CreateCompilation(source, TestTargetFramework.Standard);
            Compilation newComp = TestUtils.RunGenerators(origComp, out _, new Microsoft.Interop.DllImportGenerator());

            IMethodSymbol targetMethod = GetGeneratedPInvokeTargetFromCompilation(newComp);

            INamedTypeSymbol marshalAsAttribute = newComp.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_MarshalAsAttribute)!;
            Assert.Collection(targetMethod.Parameters,
                param => Assert.Collection(param.GetAttributes(),
                    attr =>
                    {
                        Assert.Equal(marshalAsAttribute, attr.AttributeClass, SymbolEqualityComparer.Default);
                        Assert.Equal(UnmanagedType.I2, (UnmanagedType)attr.ConstructorArguments[0].Value!);
                        Assert.Empty(attr.NamedArguments);
                    }));
        }

        [ConditionalFact]
        public async Task MarshalAsAttribute_Forwarded_To_ForwardedParameter_Array()
        {
            string source = @"
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
partial class C
{
    [GeneratedDllImportAttribute(""DoesNotExist"")]
    public static partial bool Method1([MarshalAs(UnmanagedType.LPArray, SizeConst = 10, SizeParamIndex = 1, ArraySubType = UnmanagedType.I4)] int[] a, int b);
}
" + CodeSnippets.GeneratedDllImportAttributeDeclaration;
            Compilation origComp = await TestUtils.CreateCompilation(source, TestTargetFramework.Standard);
            Compilation newComp = TestUtils.RunGenerators(origComp, out _, new Microsoft.Interop.DllImportGenerator());

            IMethodSymbol targetMethod = GetGeneratedPInvokeTargetFromCompilation(newComp);

            INamedTypeSymbol marshalAsAttribute = newComp.GetTypeByMetadataName(TypeNames.System_Runtime_InteropServices_MarshalAsAttribute)!;
            Assert.Collection(targetMethod.Parameters,
                param => Assert.Collection(param.GetAttributes(),
                    attr =>
                    {
                        Assert.Equal(marshalAsAttribute, attr.AttributeClass, SymbolEqualityComparer.Default);
                        Assert.Equal(UnmanagedType.LPArray, (UnmanagedType)attr.ConstructorArguments[0].Value!);
                        var namedArgs = attr.NamedArguments.ToImmutableDictionary();
                        Assert.Equal(10, namedArgs["SizeConst"].Value);
                        Assert.Equal((short)1, namedArgs["SizeParamIndex"].Value);
                        Assert.Equal(UnmanagedType.I4, (UnmanagedType)namedArgs["ArraySubType"].Value!);
                    }),
                param => Assert.Equal(SpecialType.System_Int32, param.Type.SpecialType));
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
