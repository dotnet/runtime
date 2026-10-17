// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Antlr4.Runtime;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    internal byte AddInstanceCallingConvention(byte callingConvention)
        => (byte)(callingConvention | (byte)SignatureAttributes.Instance);

    internal byte AddExplicitCallingConvention(byte callingConvention)
        => (byte)(callingConvention | (byte)(SignatureAttributes.ExplicitThis | SignatureAttributes.Instance));

    internal byte GetRawCallingConvention(IToken token) => (byte)ParseInt32(token);

    internal byte GetDefaultCallingConvention() => (byte)SignatureCallingConvention.Default;

    internal byte GetCallingConvention(IToken token)
        => (byte)(token.Type switch
        {
            CILParser.DEFAULT => SignatureCallingConvention.Default,
            CILParser.VARARG => SignatureCallingConvention.VarArgs,
            CILParser.CDECL => SignatureCallingConvention.CDecl,
            CILParser.STDCALL => SignatureCallingConvention.StdCall,
            CILParser.THISCALL => SignatureCallingConvention.ThisCall,
            CILParser.FASTCALL => SignatureCallingConvention.FastCall,
            CILParser.UNMANAGED => SignatureCallingConvention.Unmanaged,
            _ => throw new UnreachableException()
        });

    internal void InitializeBound(CILParser.BoundContext context)
    {
        context.Lower = 0;
        context.Upper = 0;
        context.HasLower = false;
        context.HasUpper = false;
    }

    internal void SetBoundSize(CILParser.BoundContext context, IToken sizeToken)
    {
        context.Lower = 0;
        context.Upper = ParseInt32(sizeToken);
        context.HasLower = true;
        context.HasUpper = true;
    }

    internal void SetBoundRange(CILParser.BoundContext context, IToken lowerToken, IToken upperToken)
    {
        int lower = ParseInt32(lowerToken);
        context.Lower = lower;
        context.Upper = ParseInt32(upperToken) - lower + 1;
        context.HasLower = true;
        context.HasUpper = true;
    }

    internal void SetBoundLower(CILParser.BoundContext context, IToken lowerToken)
    {
        context.Lower = ParseInt32(lowerToken);
        context.HasLower = true;
    }

    internal ArrayBoundValue CreateArrayBound(CILParser.BoundContext bound)
        => new(
            bound.HasLower ? bound.Lower : null,
            bound.HasUpper ? bound.Upper : null);

    internal SignatureArgumentValue CreateSentinelSignatureArgument()
        => new SignatureArgumentValue(true, 0, null, null, null);

    internal SignatureArgumentValue CreateSignatureArgument(
        int attributes,
        TypeValue type,
        MarshallingDescriptorValue marshalling,
        CILParser.IdContext? name)
        => new SignatureArgumentValue(
            false,
            attributes,
            type,
            marshalling,
            name is null ? null : GetIdentifier(name));

    internal void SetParameterAttributeElement(CILParser.ParamAttrElementContext context, IToken attribute)
    {
        context.Value = attribute.Text switch
        {
            "in" => (int)ParameterAttributes.In,
            "out" => (int)ParameterAttributes.Out,
            "opt" => (int)ParameterAttributes.Optional,
            _ => throw new UnreachableException()
        };
        context.ShouldAppend = true;
    }

    /// <summary>
    /// Applies a <c>[n]</c> attribute element. On a method parameter or any other signature argument, <c>[n]</c>
    /// sets the raw parameter attributes to <c>n + 1</c>, replacing the elements before it. On an argument of a
    /// <c>.locals</c> directive it is the slot index of the local instead: the value is recorded for
    /// <see cref="EndLocalsDirective"/>, which stores it in <see cref="SignatureArgumentValue.Slot"/>, and the
    /// argument's attributes are left unchanged.
    /// </summary>
    internal void SetRawParameterAttributeElement(CILParser.ParamAttrElementContext context, IToken token)
    {
        int value = ParseInt32(token);
        if (context.Parent is CILParser.ParamAttrContext
            {
                Parent: CILParser.SigArgContext
                {
                    Parent: CILParser.SigArgsContext { Parent: CILParser.LocalsDeclContext } arguments
                }
            })
        {
            // The parser builds no parse tree, so the argument is identified by its position: while an argument
            // is being parsed, the list holds the arguments before it.
            _explicitLocalSlots[arguments.Builder.Count] = (value, token);
            context.Value = 0;
            context.ShouldAppend = true;
            return;
        }

        context.Value = value + 1;
        context.ShouldAppend = false;
    }

    internal int AddParameterAttribute(int attributes, int value, bool shouldAppend)
        => shouldAppend ? attributes | value : value;

    internal ElementTypeValue CreateClassElementType(ClassNameValue className, bool isValueType)
        => new ClassElementTypeValue(className, isValueType);

    internal ElementTypeValue CreateObjectElementType()
        => new PrimitiveElementTypeValue((byte)SignatureTypeCode.Object);

    internal ElementTypeValue CreateTypedReferenceElementType()
        => new PrimitiveElementTypeValue((byte)SignatureTypeCode.TypedReference);

    internal ElementTypeValue CreateVoidElementType()
        => new PrimitiveElementTypeValue((byte)SignatureTypeCode.Void);

    internal ElementTypeValue CreatePrimitiveElementType(byte typeCode)
        => new PrimitiveElementTypeValue(typeCode);

    internal ElementTypeValue CreateFunctionPointerElementType(
        byte callingConvention,
        TypeValue returnType,
        ImmutableArray<SignatureArgumentValue> arguments)
        => new FunctionPointerElementTypeValue(
            callingConvention,
            returnType,
            arguments);

    internal ElementTypeValue CreateIndexedGenericParameterElementType(
        bool isMethodParameter,
        IToken token)
        => new IndexedGenericParameterElementTypeValue(isMethodParameter, ParseInt32(token));

    internal ElementTypeValue CreateNamedGenericParameterElementType(
        IToken token,
        bool isMethodParameter,
        string name)
        => new NamedGenericParameterElementTypeValue(token, isMethodParameter, name);

    internal ElementTypeValue CreateTypedefElementType(IToken token, string alias)
        => new TypedefElementTypeValue(token, alias);

    internal ElementTypeValue CreateSentinelElementType(TypeValue type)
        => new SentinelElementTypeValue(type);

    internal TypeModifierValue CreateSzArrayTypeModifier()
        => new SimpleTypeModifierValue(SimpleTypeModifierKind.SzArray);

    internal TypeModifierValue CreateByReferenceTypeModifier()
        => new SimpleTypeModifierValue(SimpleTypeModifierKind.ByReference);

    internal TypeModifierValue CreatePointerTypeModifier()
        => new SimpleTypeModifierValue(SimpleTypeModifierKind.Pointer);

    internal TypeModifierValue CreatePinnedTypeModifier()
        => new SimpleTypeModifierValue(SimpleTypeModifierKind.Pinned);

    internal TypeModifierValue CreateArrayTypeModifier(ImmutableArray<ArrayBoundValue> bounds)
        => new ArrayTypeModifierValue(bounds);

    internal TypeModifierValue CreateCustomTypeModifier(
        TypeSpecificationValue type,
        bool isRequired)
        => new CustomTypeModifierValue(type, isRequired);

    internal TypeModifierValue CreateGenericArgumentsModifier(
        ImmutableArray<TypeValue> arguments)
        => new GenericArgumentsTypeModifierValue(arguments);

    internal byte GetSimpleType(IToken token, bool isUnsigned)
    {
        SignatureTypeCode typeCode = (token.Type, isUnsigned) switch
        {
            (CILParser.CHAR, false) => SignatureTypeCode.Char,
            (CILParser.STRING, false) => SignatureTypeCode.String,
            (CILParser.BOOL, false) => SignatureTypeCode.Boolean,
            (CILParser.INT8, false) => SignatureTypeCode.SByte,
            (CILParser.INT16, false) => SignatureTypeCode.Int16,
            (CILParser.INT32_, false) => SignatureTypeCode.Int32,
            (CILParser.INT64_, false) => SignatureTypeCode.Int64,
            (CILParser.FLOAT32, false) => SignatureTypeCode.Single,
            (CILParser.FLOAT64_, false) => SignatureTypeCode.Double,
            (CILParser.UINT8, false) => SignatureTypeCode.Byte,
            (CILParser.UINT16, false) => SignatureTypeCode.UInt16,
            (CILParser.UINT32, false) => SignatureTypeCode.UInt32,
            (CILParser.UINT64, false) => SignatureTypeCode.UInt64,
            (CILParser.INT8, true) => SignatureTypeCode.Byte,
            (CILParser.INT16, true) => SignatureTypeCode.UInt16,
            (CILParser.INT32_, true) => SignatureTypeCode.UInt32,
            (CILParser.INT64_, true) => SignatureTypeCode.UInt64,
            _ => throw new UnreachableException()
        };

        return (byte)typeCode;
    }

    internal byte GetNativeIntType() => (byte)SignatureTypeCode.IntPtr;

    internal byte GetNativeUIntType() => (byte)SignatureTypeCode.UIntPtr;

    internal TypeSpecificationValue CreateClassTypeSpecification(ClassNameValue className)
        => new ClassTypeSpecificationValue(className);

    internal TypeSpecificationValue CreateAssemblyTypeSpecification(string assemblyName)
        => new AssemblyTypeSpecificationValue(assemblyName);

    internal TypeSpecificationValue CreateModuleTypeSpecification(string moduleName)
        => new ModuleTypeSpecificationValue(moduleName);

    internal TypeSpecificationValue CreateSignatureTypeSpecification(TypeValue type)
        => new SignatureTypeSpecificationValue(type);

    internal int GetGenericArity(CILParser.GenArityNotEmptyContext? context)
        => context?.Value ?? 0;

    internal CalliSignatureValue CreateCalliSignature(
        byte callingConvention,
        TypeValue returnType,
        ImmutableArray<SignatureArgumentValue> arguments)
        => new CalliSignatureValue(
            callingConvention,
            returnType,
            arguments);

    private BlobBuilder MaterializeCalliSignature(CalliSignatureValue calliSignature)
    {
        BlobBuilder signature = new();
        signature.WriteByte(calliSignature.CallingConvention);
        ImmutableArray<SignatureArg> materializedArguments = MaterializeSignatureArguments(calliSignature.Arguments);
        signature.WriteCompressedInteger(materializedArguments.Count(argument => !argument.IsSentinel));
        MaterializeType(calliSignature.ReturnType).WriteContentTo(signature);
        foreach (SignatureArg argument in materializedArguments)
        {
            argument.SignatureBlob.WriteContentTo(signature);
        }

        return signature;
    }
}
