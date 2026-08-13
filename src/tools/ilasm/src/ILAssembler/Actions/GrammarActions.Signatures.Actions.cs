// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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
    private readonly Stack<TypeSignatureFrame> _typeSignatureFrames = new();
    private readonly Stack<TypeArgumentsFrame> _typeArgumentsFrames = new();
    private readonly Stack<BoundsFrame> _boundsFrames = new();
    private readonly Stack<SignatureArgumentsFrame> _signatureArgumentsFrames = new();
    private readonly Stack<ParameterAttributesFrame> _parameterAttributesFrames = new();

    private sealed class TypeSignatureFrame
    {
        public TypeSignatureFrame(CILParser.TypeContext owner)
        {
            Owner = owner;
        }

        public CILParser.TypeContext Owner { get; }

        public ElementTypeValue ElementType { get; set; } = ElementTypeValue.Error;

        public ImmutableArray<TypeModifierValue>.Builder Modifiers { get; } = ImmutableArray.CreateBuilder<TypeModifierValue>();
    }

    private sealed class TypeArgumentsFrame
    {
        public TypeArgumentsFrame(CILParser.TypeArgsContext owner)
        {
            Owner = owner;
        }

        public CILParser.TypeArgsContext Owner { get; }

        public ImmutableArray<TypeValue>.Builder Arguments { get; } = ImmutableArray.CreateBuilder<TypeValue>();
    }

    private sealed class BoundsFrame
    {
        public BoundsFrame(CILParser.BoundsContext owner)
        {
            Owner = owner;
        }

        public CILParser.BoundsContext Owner { get; }

        public ImmutableArray<ArrayBoundValue>.Builder Bounds { get; } = ImmutableArray.CreateBuilder<ArrayBoundValue>();
    }

    private sealed class SignatureArgumentsFrame
    {
        public SignatureArgumentsFrame(CILParser.SigArgsContext owner)
        {
            Owner = owner;
        }

        public CILParser.SigArgsContext Owner { get; }

        public ImmutableArray<SignatureArgumentValue>.Builder Arguments { get; } = ImmutableArray.CreateBuilder<SignatureArgumentValue>();
    }

    private sealed class ParameterAttributesFrame
    {
        public ParameterAttributesFrame(CILParser.ParamAttrContext owner)
        {
            Owner = owner;
        }

        public CILParser.ParamAttrContext Owner { get; }

        public int Attributes { get; set; }
    }

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

    internal void BeginTypeSignature(CILParser.TypeContext context)
        => _typeSignatureFrames.Push(new(context));

    internal void SetTypeSignatureElement(CILParser.TypeContext context, object? value)
    {
        Debug.Assert(_typeSignatureFrames.Count > 0);
        TypeSignatureFrame frame = _typeSignatureFrames.Peek();
        Debug.Assert(ReferenceEquals(frame.Owner, context));
        if (ReferenceEquals(frame.Owner, context))
        {
            frame.ElementType = GetElementTypeValue(value);
        }
    }

    internal void AddTypeSignatureModifier(CILParser.TypeContext context, object? value)
    {
        Debug.Assert(_typeSignatureFrames.Count > 0);
        TypeSignatureFrame frame = _typeSignatureFrames.Peek();
        Debug.Assert(ReferenceEquals(frame.Owner, context));
        if (ReferenceEquals(frame.Owner, context))
        {
            frame.Modifiers.Add(GetTypeModifierValue(value));
        }
    }

    internal object EndTypeSignature(CILParser.TypeContext context)
    {
        Debug.Assert(_typeSignatureFrames.Count > 0);
        TypeSignatureFrame frame = _typeSignatureFrames.Pop();
        Debug.Assert(ReferenceEquals(frame.Owner, context));
        return ReferenceEquals(frame.Owner, context)
            ? new TypeValue(frame.ElementType, frame.Modifiers.ToImmutable())
            : TypeValue.Error;
    }

    internal void BeginTypeArguments(CILParser.TypeArgsContext context)
        => _typeArgumentsFrames.Push(new(context));

    internal void AddTypeArgument(CILParser.TypeArgsContext context, object? value)
    {
        Debug.Assert(_typeArgumentsFrames.Count > 0);
        TypeArgumentsFrame frame = _typeArgumentsFrames.Peek();
        Debug.Assert(ReferenceEquals(frame.Owner, context));
        if (ReferenceEquals(frame.Owner, context))
        {
            frame.Arguments.Add(GetTypeValue(value));
        }
    }

    internal object EndTypeArguments(CILParser.TypeArgsContext context)
    {
        Debug.Assert(_typeArgumentsFrames.Count > 0);
        TypeArgumentsFrame frame = _typeArgumentsFrames.Pop();
        Debug.Assert(ReferenceEquals(frame.Owner, context));
        return ReferenceEquals(frame.Owner, context) ? frame.Arguments.ToImmutable() : ImmutableArray<TypeValue>.Empty;
    }

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

    internal void BeginBounds(CILParser.BoundsContext context)
        => _boundsFrames.Push(new(context));

    internal void AddBound(CILParser.BoundsContext context, CILParser.BoundContext bound)
    {
        Debug.Assert(_boundsFrames.Count > 0);
        BoundsFrame frame = _boundsFrames.Peek();
        Debug.Assert(ReferenceEquals(frame.Owner, context));
        if (ReferenceEquals(frame.Owner, context))
        {
            frame.Bounds.Add(new(
                bound.HasLower ? bound.Lower : null,
                bound.HasUpper ? bound.Upper : null));
        }
    }

    internal object EndBounds(CILParser.BoundsContext context)
    {
        Debug.Assert(_boundsFrames.Count > 0);
        BoundsFrame frame = _boundsFrames.Pop();
        Debug.Assert(ReferenceEquals(frame.Owner, context));
        return ReferenceEquals(frame.Owner, context) ? frame.Bounds.ToImmutable() : ImmutableArray<ArrayBoundValue>.Empty;
    }

    internal void BeginSignatureArguments(CILParser.SigArgsContext context)
        => _signatureArgumentsFrames.Push(new(context));

    internal void AddSignatureArgument(CILParser.SigArgsContext context, object? value)
    {
        Debug.Assert(_signatureArgumentsFrames.Count > 0);
        SignatureArgumentsFrame frame = _signatureArgumentsFrames.Peek();
        Debug.Assert(ReferenceEquals(frame.Owner, context));
        if (ReferenceEquals(frame.Owner, context))
        {
            frame.Arguments.Add(GetSignatureArgumentValue(value));
        }
    }

    internal object EndSignatureArguments(CILParser.SigArgsContext context)
    {
        Debug.Assert(_signatureArgumentsFrames.Count > 0);
        SignatureArgumentsFrame frame = _signatureArgumentsFrames.Pop();
        Debug.Assert(ReferenceEquals(frame.Owner, context));
        return ReferenceEquals(frame.Owner, context) ? frame.Arguments.ToImmutable() : ImmutableArray<SignatureArgumentValue>.Empty;
    }

    internal object CreateSentinelSignatureArgument()
        => new SignatureArgumentValue(true, 0, null, null, null);

    internal object CreateSignatureArgument(
        int attributes,
        object? type,
        object? marshalling,
        CILParser.IdContext? name)
        => new SignatureArgumentValue(
            false,
            attributes,
            GetTypeValue(type),
            GetMarshallingDescriptorValue(marshalling),
            name is null ? null : VisitId(name).Value);

    internal void BeginParameterAttributes(CILParser.ParamAttrContext context)
        => _parameterAttributesFrames.Push(new(context));

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

    internal void SetRawParameterAttributeElement(CILParser.ParamAttrElementContext context, IToken token)
    {
        context.Value = ParseInt32(token) + 1;
        context.ShouldAppend = false;
    }

    internal void AddParameterAttribute(CILParser.ParamAttrContext context, CILParser.ParamAttrElementContext element)
    {
        Debug.Assert(_parameterAttributesFrames.Count > 0);
        ParameterAttributesFrame frame = _parameterAttributesFrames.Peek();
        Debug.Assert(ReferenceEquals(frame.Owner, context));
        if (ReferenceEquals(frame.Owner, context))
        {
            frame.Attributes = element.ShouldAppend
                ? frame.Attributes | element.Value
                : element.Value;
        }
    }

    internal int EndParameterAttributes(CILParser.ParamAttrContext context)
    {
        Debug.Assert(_parameterAttributesFrames.Count > 0);
        ParameterAttributesFrame frame = _parameterAttributesFrames.Pop();
        Debug.Assert(ReferenceEquals(frame.Owner, context));
        return ReferenceEquals(frame.Owner, context) ? frame.Attributes : 0;
    }

    internal object CreateClassElementType(object? className, bool isValueType)
        => new ClassElementTypeValue(GetClassNameValue(className), isValueType);

    internal object CreateObjectElementType()
        => new PrimitiveElementTypeValue((byte)SignatureTypeCode.Object);

    internal object CreateTypedReferenceElementType()
        => new PrimitiveElementTypeValue((byte)SignatureTypeCode.TypedReference);

    internal object CreateVoidElementType()
        => new PrimitiveElementTypeValue((byte)SignatureTypeCode.Void);

    internal object CreatePrimitiveElementType(byte typeCode)
        => new PrimitiveElementTypeValue(typeCode);

    internal object CreateFunctionPointerElementType(byte callingConvention, object? returnType, object? arguments)
        => new FunctionPointerElementTypeValue(
            callingConvention,
            GetTypeValue(returnType),
            GetSignatureArgumentsValue(arguments));

    internal object CreateIndexedGenericParameterElementType(bool isMethodParameter, IToken token)
        => new IndexedGenericParameterElementTypeValue(isMethodParameter, ParseInt32(token));

    internal object CreateNamedGenericParameterElementType(IToken token, bool isMethodParameter, string name)
        => new NamedGenericParameterElementTypeValue(token, isMethodParameter, name);

    internal object CreateTypedefElementType(IToken token, string alias)
        => new TypedefElementTypeValue(token, alias);

    internal object CreateSentinelElementType(object? type)
        => new SentinelElementTypeValue(GetTypeValue(type));

    internal object CreateSzArrayTypeModifier()
        => new SimpleTypeModifierValue(SimpleTypeModifierKind.SzArray);

    internal object CreateByReferenceTypeModifier()
        => new SimpleTypeModifierValue(SimpleTypeModifierKind.ByReference);

    internal object CreatePointerTypeModifier()
        => new SimpleTypeModifierValue(SimpleTypeModifierKind.Pointer);

    internal object CreatePinnedTypeModifier()
        => new SimpleTypeModifierValue(SimpleTypeModifierKind.Pinned);

    internal object CreateArrayTypeModifier(object? bounds)
        => new ArrayTypeModifierValue(GetBoundsValue(bounds));

    internal object CreateCustomTypeModifier(object? type, bool isRequired)
        => new CustomTypeModifierValue(GetTypeSpecificationValue(type), isRequired);

    internal object CreateGenericArgumentsModifier(object? arguments)
        => new GenericArgumentsTypeModifierValue(GetTypeArgumentsValue(arguments));

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

    internal object CreateClassTypeSpecification(object? className)
        => new ClassTypeSpecificationValue(GetClassNameValue(className));

    internal object CreateAssemblyTypeSpecification(string assemblyName)
        => new AssemblyTypeSpecificationValue(assemblyName);

    internal object CreateModuleTypeSpecification(string moduleName)
        => new ModuleTypeSpecificationValue(moduleName);

    internal object CreateSignatureTypeSpecification(object? type)
        => new SignatureTypeSpecificationValue(GetTypeValue(type));

    internal int GetGenericArity(CILParser.GenArityNotEmptyContext? context)
        => context?.Value ?? 0;

    internal object CreateCalliSignature(byte callingConvention, object? returnType, object? arguments)
        => new CalliSignatureValue(
            callingConvention,
            GetTypeValue(returnType),
            GetSignatureArgumentsValue(arguments));

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
