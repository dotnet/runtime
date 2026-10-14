// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Interop;
using Microsoft.Interop.UnitTests;
using Xunit;
using InteropScopedKind = Microsoft.Interop.ScopedKind;
using RoslynScopedKind = Microsoft.CodeAnalysis.ScopedKind;

namespace LibraryImportGenerator.UnitTests;

public class SourceGenerationModelTests
{
    [Theory]
    [InlineData("int", typeof(WellKnownTypeInfo))]
    [InlineData("string", typeof(WellKnownTypeInfo))]
    [InlineData("global::System.Enum", typeof(WellKnownTypeInfo))]
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
    [InlineData(SpecialType.System_Object)]
    [InlineData(SpecialType.System_Decimal)]
    [InlineData(SpecialType.System_DateTime)]
    [InlineData(SpecialType.System_Array)]
    [InlineData(SpecialType.System_Nullable_T)]
    public void CompilerOnlySpecialTypesAreNotAddedToCore(SpecialType type)
    {
        Assert.Equal(WellKnownType.None, type.ToWellKnownType());
    }

    [Fact]
    public void EveryCoreWellKnownTypeHasAnExplicitCompilerMapping()
    {
        foreach (WellKnownType type in Enum.GetValues<WellKnownType>())
        {
            SpecialType compilerType = Enum.Parse<SpecialType>(type.ToString());
            Assert.Equal(type, compilerType.ToWellKnownType());
        }
    }

    [Theory]
    [InlineData(RefKind.None, ByRefMarshalKind.None)]
    [InlineData(RefKind.In, ByRefMarshalKind.In)]
    [InlineData(RefKind.Out, ByRefMarshalKind.Out)]
    [InlineData(RefKind.Ref, ByRefMarshalKind.Ref)]
    [InlineData(RefKind.RefReadOnlyParameter, ByRefMarshalKind.RefReadOnlyParameter)]
    public void RefKindsAreConvertedExplicitly(RefKind source, ByRefMarshalKind expected)
    {
        Assert.Equal(expected, source.ToByRefMarshalKind());
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

        Assert.Equal(parameter.RefKind.ToByRefMarshalKind(), model.RefKind);
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
