// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Antlr4.Runtime;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    private readonly Stack<MethodHeaderFrame> _methodHeaderFrames = new();
    private readonly Stack<PInvokeFrame> _pInvokeFrames = new();
    private readonly Stack<GenericTypeListFrame> _genericTypeListFrames = new();
    private readonly Stack<GenericParameterAttributesFrame> _genericParameterAttributesFrames = new();
    private readonly Stack<GenericParametersFrame> _genericParametersFrames = new();

    private sealed class MethodHeaderFrame
    {
        public MethodHeaderFrame(CILParser.MethodHeadContext owner, int initialSyntaxErrorCount)
        {
            Owner = owner;
            InitialSyntaxErrorCount = initialSyntaxErrorCount;
        }

        public CILParser.MethodHeadContext Owner { get; }

        public int InitialSyntaxErrorCount { get; }

        public MethodAttributes Attributes { get; set; }

        public ImmutableArray<PInvokeValue>.Builder PInvokes { get; } =
            ImmutableArray.CreateBuilder<PInvokeValue>();

        public MethodImplAttributes ImplementationAttributes { get; set; }
    }

    private sealed class PInvokeFrame
    {
        public PInvokeFrame(CILParser.PinvImplContext owner)
        {
            Owner = owner;
        }

        public CILParser.PinvImplContext Owner { get; }

        public string? ModuleName { get; set; }

        public string? EntryPointName { get; set; }

        public MethodImportAttributes Attributes { get; set; }
    }

    private sealed class GenericTypeListFrame
    {
        public GenericTypeListFrame(CILParser.TypeListContext owner)
        {
            Owner = owner;
        }

        public CILParser.TypeListContext Owner { get; }

        public ImmutableArray<TypeSpecificationValue>.Builder Types { get; } =
            ImmutableArray.CreateBuilder<TypeSpecificationValue>();
    }

    private sealed class GenericParameterAttributesFrame
    {
        public GenericParameterAttributesFrame(CILParser.TyparAttribsContext owner)
        {
            Owner = owner;
        }

        public CILParser.TyparAttribsContext Owner { get; }

        public GenericParameterAttributes Attributes { get; set; }
    }

    private sealed class GenericParametersFrame
    {
        public GenericParametersFrame(CILParser.TyparsContext owner)
        {
            Owner = owner;
        }

        public CILParser.TyparsContext Owner { get; }

        public ImmutableArray<GenericParameterDeclarationValue>.Builder Parameters { get; } =
            ImmutableArray.CreateBuilder<GenericParameterDeclarationValue>();
    }

    internal void BeginMethodHeader(CILParser.MethodHeadContext context)
    {
        ClearPendingCustomAttributeOwners();
        _methodHeaderFrames.Push(new(context, _syntaxErrorCount));
    }

    internal void AddMethodAttribute(CILParser.MethodHeadContext context, object? value)
    {
        if (TryGetMethodHeaderFrame(context) is { } frame)
        {
            frame.Attributes = ApplyAttribute(frame.Attributes, GetAttributeValue<MethodAttributes>(value));
        }
    }
    internal void AddPInvoke(CILParser.MethodHeadContext context, object? value)
    {
        if (TryGetMethodHeaderFrame(context) is { } frame)
        {
            frame.PInvokes.Add(GetPInvokeValue(value));
        }
    }

    internal void AddMethodImplementationAttribute(CILParser.MethodHeadContext context, object? value)
    {
        if (TryGetMethodHeaderFrame(context) is { } frame)
        {
            frame.ImplementationAttributes = ApplyAttribute(
                frame.ImplementationAttributes,
                GetAttributeValue<MethodImplAttributes>(value));
        }
    }

    internal object CreateMethodHeader(
        CILParser.MethodHeadContext context,
        byte callingConvention,
        int returnAttributes,
        object? returnType,
        object? returnMarshalling,
        string name,
        object? genericParameters,
        object? arguments)
    {
        MethodHeaderFrame? frame = TryGetMethodHeaderFrame(context);
        if (frame is null ||
            frame.InitialSyntaxErrorCount != _syntaxErrorCount ||
            context.exception is not null)
        {
            return MethodHeaderValue.Error;
        }

        return new MethodHeaderValue(
            true,
            frame.Attributes,
            frame.PInvokes.ToImmutable(),
            callingConvention,
            returnAttributes,
            GetTypeValue(returnType),
            GetMarshallingDescriptorValue(returnMarshalling),
            name,
            GetGenericParameterDeclarations(genericParameters),
            GetSignatureArgumentsValue(arguments),
            frame.ImplementationAttributes);
    }

    internal void EndMethodHeader(CILParser.MethodHeadContext context)
    {
        if (_methodHeaderFrames.Count == 0)
        {
            return;
        }

        MethodHeaderFrame frame = _methodHeaderFrames.Peek();
        Debug.Assert(ReferenceEquals(frame.Owner, context));
        if (ReferenceEquals(frame.Owner, context))
        {
            _methodHeaderFrames.Pop();
        }
    }

    internal object CreateMethodAttribute(IToken token)
        => token.Text switch
        {
            "static" => new AttributeValue<MethodAttributes>(MethodAttributes.Static, 0, true),
            "public" => new AttributeValue<MethodAttributes>(
                MethodAttributes.Public,
                MethodAttributes.MemberAccessMask,
                true),
            "private" => new AttributeValue<MethodAttributes>(
                MethodAttributes.Private,
                MethodAttributes.MemberAccessMask,
                true),
            "family" => new AttributeValue<MethodAttributes>(
                MethodAttributes.Family,
                MethodAttributes.MemberAccessMask,
                true),
            "final" => new AttributeValue<MethodAttributes>(MethodAttributes.Final, 0, true),
            "specialname" => new AttributeValue<MethodAttributes>(MethodAttributes.SpecialName, 0, true),
            "virtual" => new AttributeValue<MethodAttributes>(MethodAttributes.Virtual, 0, true),
            "strict" => new AttributeValue<MethodAttributes>(
                MethodAttributes.CheckAccessOnOverride,
                0,
                true),
            "abstract" => new AttributeValue<MethodAttributes>(MethodAttributes.Abstract, 0, true),
            "assembly" => new AttributeValue<MethodAttributes>(
                MethodAttributes.Assembly,
                MethodAttributes.MemberAccessMask,
                true),
            "famandassem" => new AttributeValue<MethodAttributes>(
                MethodAttributes.FamANDAssem,
                MethodAttributes.MemberAccessMask,
                true),
            "famorassem" => new AttributeValue<MethodAttributes>(
                MethodAttributes.FamORAssem,
                MethodAttributes.MemberAccessMask,
                true),
            "privatescope" => new AttributeValue<MethodAttributes>(
                MethodAttributes.PrivateScope,
                MethodAttributes.MemberAccessMask,
                true),
            "hidebysig" => new AttributeValue<MethodAttributes>(MethodAttributes.HideBySig, 0, true),
            "newslot" => new AttributeValue<MethodAttributes>(MethodAttributes.NewSlot, 0, true),
            "rtspecialname" => new AttributeValue<MethodAttributes>(MethodAttributes.RTSpecialName, 0, true),
            "unmanagedexp" => new AttributeValue<MethodAttributes>(MethodAttributes.UnmanagedExport, 0, true),
            "reqsecobj" => new AttributeValue<MethodAttributes>(MethodAttributes.RequireSecObject, 0, true),
            _ => throw new UnreachableException(),
        };

    internal object CreateRawMethodAttribute(IToken token)
        => new AttributeValue<MethodAttributes>((MethodAttributes)ParseInt32(token), 0, false);

    internal object CreateMethodImplementationAttribute(IToken token)
        => token.Text switch
        {
            "native" => new AttributeValue<MethodImplAttributes>(
                MethodImplAttributes.Native,
                MethodImplAttributes.CodeTypeMask,
                true),
            "cil" or "il" => new AttributeValue<MethodImplAttributes>(
                MethodImplAttributes.IL,
                MethodImplAttributes.CodeTypeMask,
                true),
            "optil" => new AttributeValue<MethodImplAttributes>(
                MethodImplAttributes.OPTIL,
                MethodImplAttributes.CodeTypeMask,
                true),
            "managed" => new AttributeValue<MethodImplAttributes>(
                MethodImplAttributes.Managed,
                MethodImplAttributes.ManagedMask,
                true),
            "unmanaged" => new AttributeValue<MethodImplAttributes>(
                MethodImplAttributes.Unmanaged,
                MethodImplAttributes.ManagedMask,
                true),
            "forwardref" => new AttributeValue<MethodImplAttributes>(MethodImplAttributes.ForwardRef, 0, true),
            "preservesig" => new AttributeValue<MethodImplAttributes>(MethodImplAttributes.PreserveSig, 0, true),
            "runtime" => new AttributeValue<MethodImplAttributes>(
                MethodImplAttributes.Runtime,
                MethodImplAttributes.CodeTypeMask,
                true),
            "internalcall" => new AttributeValue<MethodImplAttributes>(MethodImplAttributes.InternalCall, 0, true),
            "synchronized" => new AttributeValue<MethodImplAttributes>(MethodImplAttributes.Synchronized, 0, true),
            "noinlining" => new AttributeValue<MethodImplAttributes>(MethodImplAttributes.NoInlining, 0, true),
            "aggressiveinlining" => new AttributeValue<MethodImplAttributes>(
                MethodImplAttributes.AggressiveInlining,
                0,
                true),
            "nooptimization" => new AttributeValue<MethodImplAttributes>(MethodImplAttributes.NoOptimization, 0, true),
            "aggressiveoptimization" => new AttributeValue<MethodImplAttributes>(
                MethodImplAttributes.AggressiveOptimization,
                0,
                true),
            "async" => new AttributeValue<MethodImplAttributes>(MethodImplAttributes.Async, 0, true),
            _ => throw new UnreachableException(),
        };

    internal object CreateRawMethodImplementationAttribute(IToken token)
        => new AttributeValue<MethodImplAttributes>((MethodImplAttributes)ParseInt32(token), 0, false);

    internal void BeginPInvoke(CILParser.PinvImplContext context)
        => _pInvokeFrames.Push(new(context));

    internal void SetPInvokeModule(CILParser.PinvImplContext context, string moduleName)
    {
        if (TryGetPInvokeFrame(context) is { } frame)
        {
            frame.ModuleName = moduleName;
        }
    }

    internal void SetPInvokeEntryPoint(CILParser.PinvImplContext context, string entryPointName)
    {
        if (TryGetPInvokeFrame(context) is { } frame)
        {
            frame.EntryPointName = entryPointName;
        }
    }

    internal void AddPInvokeAttribute(CILParser.PinvImplContext context, object? value)
    {
        if (TryGetPInvokeFrame(context) is { } frame)
        {
            frame.Attributes = ApplyAttribute(
                frame.Attributes,
                GetAttributeValue<MethodImportAttributes>(value));
        }
    }

    internal object EndPInvoke(CILParser.PinvImplContext context)
    {
        if (_pInvokeFrames.Count == 0)
        {
            return new PInvokeValue(null, null, 0);
        }

        PInvokeFrame frame = _pInvokeFrames.Peek();
        Debug.Assert(ReferenceEquals(frame.Owner, context));
        if (!ReferenceEquals(frame.Owner, context))
        {
            return new PInvokeValue(null, null, 0);
        }

        _pInvokeFrames.Pop();
        return new PInvokeValue(frame.ModuleName, frame.EntryPointName, frame.Attributes);
    }

    internal object CreatePInvokeAttribute(IToken token)
        => token.Text switch
        {
            "nomangle" => new AttributeValue<MethodImportAttributes>(
                MethodImportAttributes.ExactSpelling,
                0,
                true),
            "ansi" => new AttributeValue<MethodImportAttributes>(
                MethodImportAttributes.CharSetAnsi,
                0,
                true),
            "unicode" => new AttributeValue<MethodImportAttributes>(
                MethodImportAttributes.CharSetUnicode,
                0,
                true),
            "autochar" => new AttributeValue<MethodImportAttributes>(
                MethodImportAttributes.CharSetAuto,
                0,
                true),
            "lasterr" => new AttributeValue<MethodImportAttributes>(
                MethodImportAttributes.SetLastError,
                0,
                true),
            "winapi" => new AttributeValue<MethodImportAttributes>(
                MethodImportAttributes.CallingConventionWinApi,
                0,
                true),
            "cdecl" => new AttributeValue<MethodImportAttributes>(
                MethodImportAttributes.CallingConventionCDecl,
                0,
                true),
            "stdcall" => new AttributeValue<MethodImportAttributes>(
                MethodImportAttributes.CallingConventionStdCall,
                0,
                true),
            "thiscall" => new AttributeValue<MethodImportAttributes>(
                MethodImportAttributes.CallingConventionThisCall,
                0,
                true),
            "fastcall" => new AttributeValue<MethodImportAttributes>(
                MethodImportAttributes.CallingConventionFastCall,
                0,
                true),
            _ => throw new UnreachableException(),
        };

    internal object CreateBestFitPInvokeAttribute(IToken setting)
        => new AttributeValue<MethodImportAttributes>(
            setting.Text == "on"
                ? MethodImportAttributes.BestFitMappingEnable
                : MethodImportAttributes.BestFitMappingDisable,
            0,
            true);

    internal object CreateCharMapErrorPInvokeAttribute(IToken setting)
        => new AttributeValue<MethodImportAttributes>(
            setting.Text == "on"
                ? MethodImportAttributes.ThrowOnUnmappableCharEnable
                : MethodImportAttributes.ThrowOnUnmappableCharDisable,
            0,
            true);

    internal object CreateRawPInvokeAttribute(IToken token)
        => new AttributeValue<MethodImportAttributes>(
            (MethodImportAttributes)ParseInt32(token),
            0,
            false);

    internal void BeginGenericTypeList(CILParser.TypeListContext context)
        => _genericTypeListFrames.Push(new(context));

    internal void AddGenericType(CILParser.TypeListContext context, object? value)
    {
        if (TryGetGenericTypeListFrame(context) is { } frame)
        {
            frame.Types.Add(GetTypeSpecificationValue(value));
        }
    }

    internal object EndGenericTypeList(CILParser.TypeListContext context)
    {
        if (_genericTypeListFrames.Count == 0)
        {
            return ImmutableArray<TypeSpecificationValue>.Empty;
        }

        GenericTypeListFrame frame = _genericTypeListFrames.Peek();
        Debug.Assert(ReferenceEquals(frame.Owner, context));
        if (!ReferenceEquals(frame.Owner, context))
        {
            return ImmutableArray<TypeSpecificationValue>.Empty;
        }

        _genericTypeListFrames.Pop();
        return frame.Types.ToImmutable();
    }

    internal object CreateEmptyGenericParameterList()
        => ImmutableArray<GenericParameterDeclarationValue>.Empty;

    internal object CreateGenericParameterAttribute(IToken token)
        => token.Text switch
        {
            "+" => new AttributeValue<GenericParameterAttributes>(
                GenericParameterAttributes.Covariant,
                0,
                true),
            "-" => new AttributeValue<GenericParameterAttributes>(
                GenericParameterAttributes.Contravariant,
                0,
                true),
            "class" => new AttributeValue<GenericParameterAttributes>(
                GenericParameterAttributes.ReferenceTypeConstraint,
                0,
                true),
            "valuetype" => new AttributeValue<GenericParameterAttributes>(
                GenericParameterAttributes.NotNullableValueTypeConstraint,
                0,
                true),
            "byreflike" => new AttributeValue<GenericParameterAttributes>(
                GenericParameterAttributes.AllowByRefLike,
                0,
                true),
            ".ctor" => new AttributeValue<GenericParameterAttributes>(
                GenericParameterAttributes.DefaultConstructorConstraint,
                0,
                true),
            _ => throw new UnreachableException(),
        };

    internal object CreateRawGenericParameterAttribute(IToken token)
        => new AttributeValue<GenericParameterAttributes>(
            (GenericParameterAttributes)ParseInt32(token),
            0,
            true);

    internal void BeginGenericParameterAttributes(CILParser.TyparAttribsContext context)
        => _genericParameterAttributesFrames.Push(new(context));

    internal void AddGenericParameterAttribute(CILParser.TyparAttribsContext context, object? value)
    {
        if (TryGetGenericParameterAttributesFrame(context) is { } frame)
        {
            frame.Attributes = ApplyAttribute(
                frame.Attributes,
                GetAttributeValue<GenericParameterAttributes>(value));
        }
    }

    internal object EndGenericParameterAttributes(CILParser.TyparAttribsContext context)
    {
        if (_genericParameterAttributesFrames.Count == 0)
        {
            return new AttributeValue<GenericParameterAttributes>(0, 0, true);
        }

        GenericParameterAttributesFrame frame = _genericParameterAttributesFrames.Peek();
        Debug.Assert(ReferenceEquals(frame.Owner, context));
        if (!ReferenceEquals(frame.Owner, context))
        {
            return new AttributeValue<GenericParameterAttributes>(0, 0, true);
        }

        _genericParameterAttributesFrames.Pop();
        return new AttributeValue<GenericParameterAttributes>(frame.Attributes, 0, true);
    }

    internal object CreateGenericParameterDeclaration(
        object? attributes,
        CILParser.TyBoundContext? constraints,
        string name)
        => new GenericParameterDeclarationValue(
            GetAttributeValue<GenericParameterAttributes>(attributes).Value,
            name,
            constraints?.Value is ImmutableArray<TypeSpecificationValue> types ? types : []);

    internal void BeginGenericParameters(CILParser.TyparsContext context)
        => _genericParametersFrames.Push(new(context));

    internal void AddGenericParameter(CILParser.TyparsContext context, object? value)
    {
        if (TryGetGenericParametersFrame(context) is { } frame)
        {
            frame.Parameters.Add(GetGenericParameterDeclaration(value));
        }
    }

    internal object EndGenericParameters(CILParser.TyparsContext context)
    {
        if (_genericParametersFrames.Count == 0)
        {
            return ImmutableArray<GenericParameterDeclarationValue>.Empty;
        }

        GenericParametersFrame frame = _genericParametersFrames.Peek();
        Debug.Assert(ReferenceEquals(frame.Owner, context));
        if (!ReferenceEquals(frame.Owner, context))
        {
            return ImmutableArray<GenericParameterDeclarationValue>.Empty;
        }

        _genericParametersFrames.Pop();
        return frame.Parameters.ToImmutable();
    }

    private MethodHeaderFrame? TryGetMethodHeaderFrame(CILParser.MethodHeadContext context)
    {
        Debug.Assert(_methodHeaderFrames.Count > 0);
        MethodHeaderFrame? frame = _methodHeaderFrames.Count == 0 ? null : _methodHeaderFrames.Peek();
        Debug.Assert(frame is null || ReferenceEquals(frame.Owner, context));
        return frame is not null && ReferenceEquals(frame.Owner, context) ? frame : null;
    }

    private PInvokeFrame? TryGetPInvokeFrame(CILParser.PinvImplContext context)
    {
        Debug.Assert(_pInvokeFrames.Count > 0);
        PInvokeFrame? frame = _pInvokeFrames.Count == 0 ? null : _pInvokeFrames.Peek();
        Debug.Assert(frame is null || ReferenceEquals(frame.Owner, context));
        return frame is not null && ReferenceEquals(frame.Owner, context) ? frame : null;
    }

    private GenericTypeListFrame? TryGetGenericTypeListFrame(CILParser.TypeListContext context)
    {
        Debug.Assert(_genericTypeListFrames.Count > 0);
        GenericTypeListFrame? frame = _genericTypeListFrames.Count == 0 ? null : _genericTypeListFrames.Peek();
        Debug.Assert(frame is null || ReferenceEquals(frame.Owner, context));
        return frame is not null && ReferenceEquals(frame.Owner, context) ? frame : null;
    }

    private GenericParameterAttributesFrame? TryGetGenericParameterAttributesFrame(
        CILParser.TyparAttribsContext context)
    {
        Debug.Assert(_genericParameterAttributesFrames.Count > 0);
        GenericParameterAttributesFrame? frame =
            _genericParameterAttributesFrames.Count == 0 ? null : _genericParameterAttributesFrames.Peek();
        Debug.Assert(frame is null || ReferenceEquals(frame.Owner, context));
        return frame is not null && ReferenceEquals(frame.Owner, context) ? frame : null;
    }

    private GenericParametersFrame? TryGetGenericParametersFrame(CILParser.TyparsContext context)
    {
        Debug.Assert(_genericParametersFrames.Count > 0);
        GenericParametersFrame? frame =
            _genericParametersFrames.Count == 0 ? null : _genericParametersFrames.Peek();
        Debug.Assert(frame is null || ReferenceEquals(frame.Owner, context));
        return frame is not null && ReferenceEquals(frame.Owner, context) ? frame : null;
    }
}
