// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Interop;
using Microsoft.Interop.UnitTests;
using Xunit;
using InteropRefKind = Microsoft.Interop.RefKind;
using InteropScopedKind = Microsoft.Interop.ScopedKind;
using InteropSpecialType = Microsoft.Interop.SpecialType;
using RoslynRefKind = Microsoft.CodeAnalysis.RefKind;
using RoslynScopedKind = Microsoft.CodeAnalysis.ScopedKind;
using RoslynSpecialType = Microsoft.CodeAnalysis.SpecialType;

namespace LibraryImportGenerator.UnitTests;

public class SourceGenerationModelTests
{
    [Theory]
    [InlineData("int", typeof(SpecialTypeInfo))]
    [InlineData("string", typeof(SpecialTypeInfo))]
    [InlineData("global::System.Enum", typeof(SpecialTypeInfo))]
    [InlineData("object", typeof(ReferenceTypeInfo))]
    [InlineData("decimal", typeof(ValueTypeInfo))]
    [InlineData("global::System.DateTime", typeof(ValueTypeInfo))]
    [InlineData("int?", typeof(ValueTypeInfo))]
    [InlineData("global::System.Array", typeof(ReferenceTypeInfo))]
    [InlineData("global::System.Delegate", typeof(ReferenceTypeInfo))]
    [InlineData("global::System.Collections.Generic.IEnumerable<int>", typeof(ReferenceTypeInfo))]
    [InlineData("int[]", typeof(SzArrayType))]
    [InlineData("int*", typeof(PointerTypeInfo))]
    [InlineData("delegate*<void>", typeof(PointerTypeInfo))]
    [InlineData("global::System.Action", typeof(DelegateTypeInfo))]
    [InlineData("global::System.Span<int>", typeof(ValueTypeInfo))]
    public void SymbolFactoriesPreserveTypeNamesWithoutExpandingSpecialTypes(string typeName, Type modelType)
    {
        IParameterSymbol parameter = GetParameter(typeName);
        ManagedTypeInfo model = ManagedTypeInfo.CreateTypeInfoForTypeSymbol(parameter.Type);

        Assert.Equal(modelType, model.GetType());
        Assert.Equal(parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), model.FullTypeName);
        Assert.Equal(parameter.Type.ToDisplayString(), model.DiagnosticFormattedName);
        Assert.Equal(model, ManagedTypeInfo.CreateTypeInfoForTypeSymbol(parameter.Type));
    }

    [Theory]
    [InlineData(RoslynSpecialType.System_Object)]
    [InlineData(RoslynSpecialType.System_Decimal)]
    [InlineData(RoslynSpecialType.System_DateTime)]
    [InlineData(RoslynSpecialType.System_Array)]
    [InlineData(RoslynSpecialType.System_Nullable_T)]
    public void CompilerOnlySpecialTypesAreNotAddedToCore(RoslynSpecialType type)
    {
        Assert.Equal(InteropSpecialType.None, type.ToInteropSpecialType());
    }

    [Fact]
    public void EveryCoreSpecialTypeHasAnExplicitCompilerMapping()
    {
        foreach (InteropSpecialType type in Enum.GetValues<InteropSpecialType>())
        {
            RoslynSpecialType compilerType = Enum.Parse<RoslynSpecialType>(type.ToString());
            Assert.Equal(type, compilerType.ToInteropSpecialType());
        }
    }

    [Theory]
    [InlineData(RoslynRefKind.None, InteropRefKind.None)]
    [InlineData(RoslynRefKind.In, InteropRefKind.In)]
    [InlineData(RoslynRefKind.Out, InteropRefKind.Out)]
    [InlineData(RoslynRefKind.Ref, InteropRefKind.Ref)]
    [InlineData(RoslynRefKind.RefReadOnlyParameter, InteropRefKind.RefReadOnlyParameter)]
    public void RefKindsAreConvertedExplicitly(RoslynRefKind source, InteropRefKind expected)
    {
        Assert.Equal(expected, source.ToInteropRefKind());
    }

    [Theory]
    [InlineData(RoslynScopedKind.None, InteropScopedKind.None)]
    [InlineData(RoslynScopedKind.ScopedRef, InteropScopedKind.ScopedRef)]
    [InlineData(RoslynScopedKind.ScopedValue, InteropScopedKind.ScopedValue)]
    public void ScopedKindsAreConvertedExplicitly(RoslynScopedKind source, InteropScopedKind expected)
    {
        Assert.Equal(expected, source.ToInteropScopedKind());
    }

    [Theory]
    [InlineData("")]
    [InlineData("in")]
    [InlineData("out")]
    [InlineData("ref")]
    [InlineData("ref readonly")]
    [InlineData("scoped ref")]
    public void ParameterFactoriesPreserveCompilerClassifications(string modifiers)
    {
        Compilation compilation = TestUtils.CreateCompilation($"unsafe class C {{ void M({modifiers} int @event) {{ }} }}");
        IParameterSymbol parameter = ((IMethodSymbol)compilation.GetTypeByMetadataName("C")!.GetMembers("M").Single()).Parameters[0];
        TypePositionInfo model = TypePositionInfo.CreateForParameter(parameter, NoMarshallingInfo.Instance, compilation);

        Assert.Equal(parameter.RefKind.ToInteropRefKind(), model.RefKind);
        Assert.Equal(parameter.ScopedKind.ToInteropScopedKind(), model.ScopedKind);
        Assert.Equal("@event", model.InstanceIdentifier);
        Assert.Equal(TypePositionInfo.UnsetIndex, model.ManagedIndex);
        Assert.Equal(TypePositionInfo.UnsetIndex, model.NativeIndex);
    }

    private static IParameterSymbol GetParameter(string type)
    {
        Compilation compilation = TestUtils.CreateCompilation($"unsafe class C {{ void M({type} value) {{ }} }}");
        return ((IMethodSymbol)compilation.GetTypeByMetadataName("C")!.GetMembers("M").Single()).Parameters[0];
    }
}
