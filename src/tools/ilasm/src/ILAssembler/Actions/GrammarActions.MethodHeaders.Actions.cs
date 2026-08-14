// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Antlr4.Runtime;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    internal CILParser.MethodHeaderBuilder PrepareMethodHeader()
    {
        ClearPendingCustomAttributeOwners();
        return new CILParser.MethodHeaderBuilder();
    }

    internal void AddMethodAttribute(
        CILParser.MethodHeaderBuilder builder,
        CILParser.AttributeValue<MethodAttributes> value)
        => builder.Attributes = ApplyAttribute(builder.Attributes, value);

    internal void AddPInvoke(CILParser.MethodHeaderBuilder builder, PInvokeValue value)
        => builder.PInvokes.Add(value);

    internal void AddMethodImplementationAttribute(
        CILParser.MethodHeaderBuilder builder,
        CILParser.AttributeValue<MethodImplAttributes> value)
        => builder.ImplementationAttributes = ApplyAttribute(
            builder.ImplementationAttributes,
            value);

    internal MethodHeaderValue CreateMethodHeader(
        CILParser.MethodHeadContext context,
        CILParser.MethodHeaderBuilder builder,
        int initialSyntaxErrorCount,
        byte callingConvention,
        int returnAttributes,
        TypeValue returnType,
        MarshallingDescriptorValue returnMarshalling,
        string name,
        ImmutableArray<GenericParameterDeclarationValue> genericParameters,
        ImmutableArray<SignatureArgumentValue> arguments)
    {
        if (HasSyntaxErrorsSince(initialSyntaxErrorCount) ||
            context.exception is not null)
        {
            return MethodHeaderValue.Error;
        }

        return new MethodHeaderValue(
            true,
            builder.Attributes,
            builder.PInvokes.ToImmutable(),
            callingConvention,
            returnAttributes,
            returnType,
            returnMarshalling,
            name,
            genericParameters,
            arguments,
            builder.ImplementationAttributes);
    }

    internal CILParser.AttributeValue<MethodAttributes> CreateMethodAttribute(IToken token)
        => token.Text switch
        {
            "static" => new CILParser.AttributeValue<MethodAttributes>(MethodAttributes.Static, 0, true),
            "public" => new CILParser.AttributeValue<MethodAttributes>(
                MethodAttributes.Public,
                MethodAttributes.MemberAccessMask,
                true),
            "private" => new CILParser.AttributeValue<MethodAttributes>(
                MethodAttributes.Private,
                MethodAttributes.MemberAccessMask,
                true),
            "family" => new CILParser.AttributeValue<MethodAttributes>(
                MethodAttributes.Family,
                MethodAttributes.MemberAccessMask,
                true),
            "final" => new CILParser.AttributeValue<MethodAttributes>(MethodAttributes.Final, 0, true),
            "specialname" => new CILParser.AttributeValue<MethodAttributes>(MethodAttributes.SpecialName, 0, true),
            "virtual" => new CILParser.AttributeValue<MethodAttributes>(MethodAttributes.Virtual, 0, true),
            "strict" => new CILParser.AttributeValue<MethodAttributes>(
                MethodAttributes.CheckAccessOnOverride,
                0,
                true),
            "abstract" => new CILParser.AttributeValue<MethodAttributes>(MethodAttributes.Abstract, 0, true),
            "assembly" => new CILParser.AttributeValue<MethodAttributes>(
                MethodAttributes.Assembly,
                MethodAttributes.MemberAccessMask,
                true),
            "famandassem" => new CILParser.AttributeValue<MethodAttributes>(
                MethodAttributes.FamANDAssem,
                MethodAttributes.MemberAccessMask,
                true),
            "famorassem" => new CILParser.AttributeValue<MethodAttributes>(
                MethodAttributes.FamORAssem,
                MethodAttributes.MemberAccessMask,
                true),
            "privatescope" => new CILParser.AttributeValue<MethodAttributes>(
                MethodAttributes.PrivateScope,
                MethodAttributes.MemberAccessMask,
                true),
            "hidebysig" => new CILParser.AttributeValue<MethodAttributes>(MethodAttributes.HideBySig, 0, true),
            "newslot" => new CILParser.AttributeValue<MethodAttributes>(MethodAttributes.NewSlot, 0, true),
            "rtspecialname" => new CILParser.AttributeValue<MethodAttributes>(MethodAttributes.RTSpecialName, 0, true),
            "unmanagedexp" => new CILParser.AttributeValue<MethodAttributes>(MethodAttributes.UnmanagedExport, 0, true),
            "reqsecobj" => new CILParser.AttributeValue<MethodAttributes>(MethodAttributes.RequireSecObject, 0, true),
            _ => throw new UnreachableException(),
        };

    internal CILParser.AttributeValue<MethodAttributes> CreateRawMethodAttribute(IToken token)
        => new((MethodAttributes)ParseInt32(token), 0, false);

    internal CILParser.AttributeValue<MethodImplAttributes>
        CreateMethodImplementationAttribute(IToken token)
        => token.Text switch
        {
            "native" => new CILParser.AttributeValue<MethodImplAttributes>(
                MethodImplAttributes.Native,
                MethodImplAttributes.CodeTypeMask,
                true),
            "cil" or "il" => new CILParser.AttributeValue<MethodImplAttributes>(
                MethodImplAttributes.IL,
                MethodImplAttributes.CodeTypeMask,
                true),
            "optil" => new CILParser.AttributeValue<MethodImplAttributes>(
                MethodImplAttributes.OPTIL,
                MethodImplAttributes.CodeTypeMask,
                true),
            "managed" => new CILParser.AttributeValue<MethodImplAttributes>(
                MethodImplAttributes.Managed,
                MethodImplAttributes.ManagedMask,
                true),
            "unmanaged" => new CILParser.AttributeValue<MethodImplAttributes>(
                MethodImplAttributes.Unmanaged,
                MethodImplAttributes.ManagedMask,
                true),
            "forwardref" => new CILParser.AttributeValue<MethodImplAttributes>(MethodImplAttributes.ForwardRef, 0, true),
            "preservesig" => new CILParser.AttributeValue<MethodImplAttributes>(MethodImplAttributes.PreserveSig, 0, true),
            "runtime" => new CILParser.AttributeValue<MethodImplAttributes>(
                MethodImplAttributes.Runtime,
                MethodImplAttributes.CodeTypeMask,
                true),
            "internalcall" => new CILParser.AttributeValue<MethodImplAttributes>(MethodImplAttributes.InternalCall, 0, true),
            "synchronized" => new CILParser.AttributeValue<MethodImplAttributes>(MethodImplAttributes.Synchronized, 0, true),
            "noinlining" => new CILParser.AttributeValue<MethodImplAttributes>(MethodImplAttributes.NoInlining, 0, true),
            "aggressiveinlining" => new CILParser.AttributeValue<MethodImplAttributes>(
                MethodImplAttributes.AggressiveInlining,
                0,
                true),
            "nooptimization" => new CILParser.AttributeValue<MethodImplAttributes>(MethodImplAttributes.NoOptimization, 0, true),
            "aggressiveoptimization" => new CILParser.AttributeValue<MethodImplAttributes>(
                MethodImplAttributes.AggressiveOptimization,
                0,
                true),
            "async" => new CILParser.AttributeValue<MethodImplAttributes>(MethodImplAttributes.Async, 0, true),
            _ => throw new UnreachableException(),
        };

    internal CILParser.AttributeValue<MethodImplAttributes>
        CreateRawMethodImplementationAttribute(IToken token)
        => new((MethodImplAttributes)ParseInt32(token), 0, false);

    internal void SetPInvokeModule(CILParser.PInvokeBuilder builder, string moduleName)
        => builder.ModuleName = moduleName;

    internal void SetPInvokeEntryPoint(
        CILParser.PInvokeBuilder builder,
        string entryPointName)
        => builder.EntryPointName = entryPointName;

    internal void AddPInvokeAttribute(
        CILParser.PInvokeBuilder builder,
        CILParser.AttributeValue<MethodImportAttributes> value)
        => builder.Attributes = ApplyAttribute(builder.Attributes, value);

    internal PInvokeValue CreatePInvoke(CILParser.PInvokeBuilder builder)
        => new(builder.ModuleName, builder.EntryPointName, builder.Attributes);

    internal CILParser.AttributeValue<MethodImportAttributes>
        CreatePInvokeAttribute(IToken token)
        => token.Text switch
        {
            "nomangle" => new CILParser.AttributeValue<MethodImportAttributes>(
                MethodImportAttributes.ExactSpelling,
                0,
                true),
            "ansi" => new CILParser.AttributeValue<MethodImportAttributes>(
                MethodImportAttributes.CharSetAnsi,
                0,
                true),
            "unicode" => new CILParser.AttributeValue<MethodImportAttributes>(
                MethodImportAttributes.CharSetUnicode,
                0,
                true),
            "autochar" => new CILParser.AttributeValue<MethodImportAttributes>(
                MethodImportAttributes.CharSetAuto,
                0,
                true),
            "lasterr" => new CILParser.AttributeValue<MethodImportAttributes>(
                MethodImportAttributes.SetLastError,
                0,
                true),
            "winapi" => new CILParser.AttributeValue<MethodImportAttributes>(
                MethodImportAttributes.CallingConventionWinApi,
                0,
                true),
            "cdecl" => new CILParser.AttributeValue<MethodImportAttributes>(
                MethodImportAttributes.CallingConventionCDecl,
                0,
                true),
            "stdcall" => new CILParser.AttributeValue<MethodImportAttributes>(
                MethodImportAttributes.CallingConventionStdCall,
                0,
                true),
            "thiscall" => new CILParser.AttributeValue<MethodImportAttributes>(
                MethodImportAttributes.CallingConventionThisCall,
                0,
                true),
            "fastcall" => new CILParser.AttributeValue<MethodImportAttributes>(
                MethodImportAttributes.CallingConventionFastCall,
                0,
                true),
            _ => throw new UnreachableException(),
        };

    internal CILParser.AttributeValue<MethodImportAttributes>
        CreateBestFitPInvokeAttribute(IToken setting)
        => new(
            setting.Text == "on"
                ? MethodImportAttributes.BestFitMappingEnable
                : MethodImportAttributes.BestFitMappingDisable,
            0,
            true);

    internal CILParser.AttributeValue<MethodImportAttributes>
        CreateCharMapErrorPInvokeAttribute(IToken setting)
        => new(
            setting.Text == "on"
                ? MethodImportAttributes.ThrowOnUnmappableCharEnable
                : MethodImportAttributes.ThrowOnUnmappableCharDisable,
            0,
            true);

    internal CILParser.AttributeValue<MethodImportAttributes>
        CreateRawPInvokeAttribute(IToken token)
        => new(
            (MethodImportAttributes)ParseInt32(token),
            0,
            false);

    internal CILParser.AttributeValue<GenericParameterAttributes>
        CreateGenericParameterAttribute(IToken token)
        => token.Text switch
        {
            "+" => new CILParser.AttributeValue<GenericParameterAttributes>(
                GenericParameterAttributes.Covariant,
                0,
                true),
            "-" => new CILParser.AttributeValue<GenericParameterAttributes>(
                GenericParameterAttributes.Contravariant,
                0,
                true),
            "class" => new CILParser.AttributeValue<GenericParameterAttributes>(
                GenericParameterAttributes.ReferenceTypeConstraint,
                0,
                true),
            "valuetype" => new CILParser.AttributeValue<GenericParameterAttributes>(
                GenericParameterAttributes.NotNullableValueTypeConstraint,
                0,
                true),
            "byreflike" => new CILParser.AttributeValue<GenericParameterAttributes>(
                GenericParameterAttributes.AllowByRefLike,
                0,
                true),
            ".ctor" => new CILParser.AttributeValue<GenericParameterAttributes>(
                GenericParameterAttributes.DefaultConstructorConstraint,
                0,
                true),
            _ => throw new UnreachableException(),
        };

    internal CILParser.AttributeValue<GenericParameterAttributes>
        CreateRawGenericParameterAttribute(IToken token)
        => new(
            (GenericParameterAttributes)ParseInt32(token),
            0,
            true);

    internal GenericParameterAttributes AddGenericParameterAttribute(
        GenericParameterAttributes attributes,
        CILParser.AttributeValue<GenericParameterAttributes> value)
        => ApplyAttribute(attributes, value);

    internal GenericParameterDeclarationValue CreateGenericParameterDeclaration(
        GenericParameterAttributes attributes,
        CILParser.TyBoundContext? constraints,
        string name)
        => new GenericParameterDeclarationValue(
            attributes,
            name,
            constraints?.Value ?? []);
}
