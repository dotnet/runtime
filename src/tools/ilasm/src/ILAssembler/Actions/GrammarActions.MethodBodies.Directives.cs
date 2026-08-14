// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    internal void EndLocalsDirective(CILParser.LocalsDeclContext context)
    {
        bool hasSyntaxError = EndSemanticRoot(context);
        if (hasSyntaxError || _currentMethod is null || context.arguments is null)
        {
            return;
        }

        if (context.initialize is not null)
        {
            _currentMethod.Definition.BodyAttributes = MethodBodyAttributes.InitLocals;
        }

        Dictionary<string, int> localsScope;
        if (_currentMethod.LocalsScopes.Count > 0)
        {
            localsScope = _currentMethod.LocalsScopes[^1];
        }
        else
        {
            localsScope = new();
            _currentMethod.LocalsScopes.Add(localsScope);
        }

        ImmutableArray<SignatureArg> locals =
            MaterializeSignatureArguments(GetSignatureArgumentsValue(context.arguments.Value));
        foreach (SignatureArg local in locals)
        {
            if (local.Name is not null)
            {
                localsScope.TryAdd(local.Name, _currentMethod.AllLocals.Count);
            }

            _currentMethod.AllLocals.Add(local);
        }
    }

    internal void EndExportDirective(CILParser.ExportDeclContext context)
    {
        bool hasSyntaxError = EndSemanticRoot(context);
        if (hasSyntaxError || _currentMethod is null || context.ordinal is null)
        {
            return;
        }

        _currentMethod.Definition.ExportOrdinal = ParseInt32(context.ordinal.Start);
        _currentMethod.Definition.ExportAlias =
            context.alias is null ? null : ParseIdentifier(context.alias.Start);
    }

    internal void EndVTableEntryDirective(CILParser.VtentryDeclContext context)
    {
        bool hasSyntaxError = EndSemanticRoot(context);
        if (hasSyntaxError || _currentMethod is null || context.table is null || context.slot is null)
        {
            return;
        }

        _currentMethod.Definition.VTableEntry = ParseInt32(context.table.Start);
        _currentMethod.Definition.VTableSlot = ParseInt32(context.slot.Start);
    }

    internal void EndOverrideDirective(CILParser.OverrideDeclContext context)
    {
        bool hasSyntaxError = EndSemanticRoot(context);
        if (hasSyntaxError ||
            _currentMethod is null ||
            context.owner is null ||
            context.name is null ||
            _currentTypeDefinition.PeekOrDefault() is not { } currentType)
        {
            return;
        }

        BlobBuilder signature = _currentMethod.Definition.MethodSignature!;
        if (context.convention is not null)
        {
            if (context.returnType is null || context.arity is null || context.arguments is null)
            {
                return;
            }

            signature = BuildOverrideSignature(
                context.convention.Value,
                context.returnType.Value,
                context.arity.Value,
                context.arguments.Value);
        }

        EntityRegistry.TypeEntity owner =
            ResolveTypeSpecification(GetTypeSpecificationValue(context.owner.Value));
        EntityRegistry.MemberReferenceEntity declaration =
            _entityRegistry.CreateLazilyRecordedMemberReference(owner, context.name.Value, signature);
        currentType.MethodImplementations.Add(
            EntityRegistry.CreateUnrecordedMethodImplementation(_currentMethod.Definition, declaration));
    }

    private BlobBuilder BuildOverrideSignature(
        byte callingConvention,
        object? returnType,
        int genericArity,
        object? signatureArguments)
    {
        BlobBuilder signature = new();
        byte header = callingConvention;
        if (genericArity > 0)
        {
            header |= (byte)SignatureAttributes.Generic;
        }

        signature.WriteByte(header);
        if (genericArity > 0)
        {
            signature.WriteCompressedInteger(genericArity);
        }

        ImmutableArray<SignatureArg> arguments =
            MaterializeSignatureArguments(GetSignatureArgumentsValue(signatureArguments));
        signature.WriteCompressedInteger(arguments.Length);
        MaterializeType(GetTypeValue(returnType)).WriteContentTo(signature);
        foreach (SignatureArg argument in arguments)
        {
            argument.SignatureBlob.WriteContentTo(signature);
        }

        return signature;
    }

    internal void BeginParameterDirective(CILParser.ParameterDeclContext context)
    {
        BeginSemanticRoot(context);
        _parameterDirectiveFrames.Push(new(context));
    }

    internal void AddParameterCustomAttribute(
        CILParser.ParameterDeclContext context,
        CILParser.CustomAttrDeclContext attribute)
    {
        Debug.Assert(_parameterDirectiveFrames.Count > 0);
        if (_parameterDirectiveFrames.Count == 0)
        {
            return;
        }

        ParameterDirectiveFrame frame = _parameterDirectiveFrames.Peek();
        Debug.Assert(ReferenceEquals(frame.Owner, context));
        if (ReferenceEquals(frame.Owner, context))
        {
            frame.CustomAttributes.Add(attribute);
        }
    }

    internal void EndParameterDirective(CILParser.ParameterDeclContext context)
    {
        ParameterDirectiveFrame? frame = null;
        Debug.Assert(_parameterDirectiveFrames.Count > 0);
        if (_parameterDirectiveFrames.Count > 0 &&
            ReferenceEquals(_parameterDirectiveFrames.Peek().Owner, context))
        {
            frame = _parameterDirectiveFrames.Pop();
        }

        bool hasSyntaxError = EndSemanticRoot(context);
        if (hasSyntaxError || frame is null || _currentMethod is null)
        {
            return;
        }

        if (context.genericIndex is not null || context.genericName is not null)
        {
            EntityRegistry.GenericParameterEntity? parameter = ResolveMethodGenericParameter(
                context.genericIndex,
                context.genericName?.Value,
                context);
            if (parameter is not null)
            {
                ApplyCustomAttributes(frame.CustomAttributes, parameter);
            }

            return;
        }

        if (context.constraintIndex is not null || context.constraintName is not null)
        {
            EntityRegistry.GenericParameterEntity? parameter = ResolveMethodGenericParameter(
                context.constraintIndex,
                context.constraintName?.Value,
                context);
            if (parameter is null || context.constraintType is null)
            {
                return;
            }

            EntityRegistry.TypeEntity baseType =
                ResolveTypeSpecification(GetTypeSpecificationValue(context.constraintType.Value));
            EntityRegistry.GenericParameterConstraintEntity? constraint =
                parameter.Constraints.FirstOrDefault(candidate => candidate.BaseType == baseType);
            if (constraint is null)
            {
                constraint = EntityRegistry.CreateGenericConstraint(baseType);
                constraint.Owner = parameter;
                parameter.Constraints.Add(constraint);
                _currentMethod.Definition.GenericParameterConstraints.Add(constraint);
            }

            ApplyCustomAttributes(frame.CustomAttributes, constraint);
            return;
        }

        if (context.parameterIndex is null || context.initializer is null)
        {
            return;
        }

        int index = ParseInt32(context.parameterIndex.Start);
        if ((uint)index >= (uint)_currentMethod.Definition.Parameters.Count)
        {
            ReportError(
                DiagnosticIds.ParameterIndexOutOfRange,
                string.Format(DiagnosticMessageTemplates.ParameterIndexOutOfRange, index),
                context);
            return;
        }

        EntityRegistry.ParameterEntity parameterEntity = _currentMethod.Definition.Parameters[index];
        object? constantValue = VisitInitOpt(context.initializer).Value;
        if (constantValue is not NoConstantSentinel)
        {
            parameterEntity.ConstantValue = constantValue;
            parameterEntity.HasConstant = true;
        }

        foreach (CILParser.CustomAttrDeclContext attributeContext in frame.CustomAttributes)
        {
            EntityRegistry.CustomAttributeEntity? attribute = VisitCustomAttrDecl(attributeContext).Value;
            if (attribute is not null)
            {
                attribute.Owner = parameterEntity;
                parameterEntity.HasCustomAttributes = true;
            }
        }
    }

    private EntityRegistry.GenericParameterEntity? ResolveMethodGenericParameter(
        CILParser.Int32Context? indexContext,
        string? name,
        CILParser.ParameterDeclContext diagnosticContext)
    {
        Debug.Assert(_currentMethod is not null);
        if (indexContext is not null)
        {
            int index = ParseInt32(indexContext.Start);
            if ((uint)index >= (uint)_currentMethod.Definition.GenericParameters.Count)
            {
                ReportError(
                    DiagnosticIds.GenericParameterIndexOutOfRange,
                    string.Format(DiagnosticMessageTemplates.GenericParameterIndexOutOfRange, index),
                    diagnosticContext);
                return null;
            }

            return _currentMethod.Definition.GenericParameters[index];
        }

        EntityRegistry.GenericParameterEntity? parameter =
            _currentMethod.Definition.GenericParameters.FirstOrDefault(candidate => candidate.Name == name);
        if (parameter is null)
        {
            ReportError(
                DiagnosticIds.UnknownGenericParameter,
                string.Format(DiagnosticMessageTemplates.UnknownGenericParameter, name),
                diagnosticContext);
        }

        return parameter;
    }

    private void ApplyCustomAttributes(
        List<CILParser.CustomAttrDeclContext> attributeContexts,
        EntityRegistry.EntityBase owner)
    {
        foreach (CILParser.CustomAttrDeclContext attributeContext in attributeContexts)
        {
            EntityRegistry.CustomAttributeEntity? attribute = VisitCustomAttrDecl(attributeContext).Value;
            if (attribute is not null)
            {
                attribute.Owner = owner;
            }
        }
    }

#pragma warning disable CA1822 // Parser actions own these directive side effects.
    internal void ProcessMethodDataDeclaration(CILParser.DataDeclContext context)
        => _ = context;
#pragma warning restore CA1822

    internal void ProcessMethodSecurityDeclaration(CILParser.SecDeclContext context)
    {
        if (_currentMethod is null || context.HasSyntaxError)
        {
            return;
        }

        EntityRegistry.DeclarativeSecurityAttributeEntity? security = VisitSecDecl(context).Value;
        security?.Parent = _currentMethod.Definition;
    }

#pragma warning disable CA1822 // Parser actions own these directive side effects.
    internal void ProcessMethodSourceDirective(CILParser.ExtSourceSpecContext context)
        => _ = context;

    internal void ProcessMethodLanguageDirective(CILParser.LanguageDeclContext context)
        => _ = context;
#pragma warning restore CA1822

    internal void ProcessMethodCustomAttribute(CILParser.CustomDescrInMethodBodyContext context)
    {
        if (_currentMethod is null || context.HasSyntaxError)
        {
            return;
        }

        EntityRegistry.CustomAttributeEntity? attribute = VisitCustomDescrInMethodBody(context).Value;
        if (attribute is not null)
        {
            attribute.Owner = _currentMethod.Definition;
        }
    }
}
