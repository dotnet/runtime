// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using Antlr4.Runtime;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    private readonly Stack<ClassGenericDirectiveFrame> _classGenericDirectiveFrames = new();
    private readonly Dictionary<
        EntityRegistry.TypeDefinitionEntity,
        List<PendingClassMethodOverride>> _pendingClassMethodOverrides = new();

    private sealed record ClassGenericDirectiveFrame(
        CILParser.ClassDeclContext Owner,
        EntityRegistry.EntityBase? AttributeOwner);

    private sealed record PendingClassMethodOverride(
        EntityRegistry.MemberReferenceEntity Declaration,
        EntityRegistry.MemberReferenceEntity? ReferencedBody,
        string BodyName,
        BlobBuilder BodySignature,
        IToken Location);

    private void PrepareClassMember()
    {
        _pendingClassCustomAttributeOwner = null;
    }

    internal void ProcessClassDataDeclaration(CILParser.DataDeclContext context)
    {
        PrepareClassMember();
        if (!context.HasSyntaxError)
        {
            _ = VisitDataDecl(context);
        }
    }

    internal void ProcessClassSecurityDeclaration(CILParser.SecDeclContext context)
    {
        PrepareClassMember();
        if (context.HasSyntaxError)
        {
            return;
        }

        EntityRegistry.DeclarativeSecurityAttributeEntity? security = VisitSecDecl(context).Value;
        security?.Parent = _currentTypeDefinition.PeekOrDefault();
    }

    internal void ProcessClassSourceDirective(CILParser.ExtSourceSpecContext context)
    {
        PrepareClassMember();
        if (!context.HasSyntaxError)
        {
            _ = VisitExtSourceSpec(context);
        }
    }

    internal void ProcessClassLanguageDirective(CILParser.LanguageDeclContext context)
    {
        PrepareClassMember();
        if (!context.HasSyntaxError)
        {
            _ = VisitLanguageDecl(context);
        }
    }

    internal void ProcessClassCustomAttribute(CILParser.CustomAttrDeclContext context)
    {
        if (context.HasSyntaxError)
        {
            return;
        }

        if (VisitCustomAttrDecl(context).Value is { } customAttribute)
        {
            customAttribute.Owner =
                _pendingClassCustomAttributeOwner ??
                _currentTypeDefinition.PeekOrDefault();
        }
    }

    internal void SetClassSize(IToken token)
    {
        PrepareClassMember();
        if (_currentTypeDefinition.PeekOrDefault() is { } currentType)
        {
            currentType.ClassSize = ParseInt32(token);
        }
    }

    internal void SetClassPackingSize(IToken token)
    {
        PrepareClassMember();
        if (_currentTypeDefinition.PeekOrDefault() is { } currentType)
        {
            currentType.PackingSize = ParseInt32(token);
        }
    }

    internal void ProcessClassExport(
        CILParser.ExportHeadContext export,
        CILParser.ExptypeDeclsContext declarations)
    {
        PrepareClassMember();
        _ = export;
        _ = declarations;
    }

    internal void ProcessClassCompilerControl()
    {
        PrepareClassMember();
    }

    internal void AddClassMethodOverride(
        CILParser.ClassDeclContext context,
        object? declarationOwner,
        string declarationName,
        byte bodyCallingConvention,
        object? bodyReturnType,
        object? bodyOwner,
        string bodyName,
        object? bodyArguments)
    {
        PrepareClassMember();
        AddClassMethodOverrideCore(
            context,
            bodyCallingConvention,
            bodyReturnType,
            declarationOwner,
            declarationName,
            0,
            bodyArguments,
            bodyCallingConvention,
            bodyReturnType,
            bodyOwner,
            bodyName,
            0,
            bodyArguments);
    }

    internal void AddClassMethodOverride(
        CILParser.ClassDeclContext context,
        byte declarationCallingConvention,
        object? declarationReturnType,
        object? declarationOwner,
        string declarationName,
        int declarationArity,
        object? declarationArguments,
        byte bodyCallingConvention,
        object? bodyReturnType,
        object? bodyOwner,
        string bodyName,
        int bodyArity,
        object? bodyArguments)
    {
        PrepareClassMember();
        AddClassMethodOverrideCore(
            context,
            declarationCallingConvention,
            declarationReturnType,
            declarationOwner,
            declarationName,
            declarationArity,
            declarationArguments,
            bodyCallingConvention,
            bodyReturnType,
            bodyOwner,
            bodyName,
            bodyArity,
            bodyArguments);
    }

    private void AddClassMethodOverrideCore(
        CILParser.ClassDeclContext context,
        byte declarationCallingConvention,
        object? declarationReturnType,
        object? declarationOwner,
        string declarationName,
        int declarationArity,
        object? declarationArguments,
        byte bodyCallingConvention,
        object? bodyReturnType,
        object? bodyOwner,
        string bodyName,
        int bodyArity,
        object? bodyArguments)
    {
        if (_currentTypeDefinition.PeekOrDefault() is not { } currentType)
        {
            return;
        }

        BlobBuilder declarationSignature = BuildClassMethodOverrideSignature(
            declarationCallingConvention,
            declarationReturnType,
            declarationArguments,
            declarationArity);
        BlobBuilder bodySignature = BuildClassMethodOverrideSignature(
            bodyCallingConvention,
            bodyReturnType,
            bodyArguments,
            bodyArity);

        EntityRegistry.MemberReferenceEntity declaration =
            _entityRegistry.CreateLazilyRecordedMemberReference(
                ResolveTypeSpecification(GetTypeSpecificationValue(declarationOwner)),
                declarationName,
                declarationSignature);
        EntityRegistry.TypeEntity resolvedBodyOwner =
            ResolveTypeSpecification(GetTypeSpecificationValue(bodyOwner));
        EntityRegistry.MemberReferenceEntity? referencedBody =
            ReferenceEquals(resolvedBodyOwner, currentType)
                ? null
                : _entityRegistry.CreateLazilyRecordedMemberReference(
                    resolvedBodyOwner,
                    bodyName,
                    bodySignature);

        if (!_pendingClassMethodOverrides.TryGetValue(
                currentType,
                out List<PendingClassMethodOverride>? pendingOverrides))
        {
            pendingOverrides = new();
            _pendingClassMethodOverrides.Add(currentType, pendingOverrides);
        }

        pendingOverrides.Add(
            new(
                declaration,
                referencedBody,
                bodyName,
                bodySignature,
                context.Start));
    }

    private void CompleteClassMethodOverrides(EntityRegistry.TypeDefinitionEntity type)
    {
        if (!_pendingClassMethodOverrides.Remove(
                type,
                out List<PendingClassMethodOverride>? pendingOverrides))
        {
            return;
        }

        foreach (PendingClassMethodOverride pending in pendingOverrides)
        {
            if (pending.ReferencedBody is { } referencedBody)
            {
                type.MethodImplementations.Add(
                    EntityRegistry.CreateUnrecordedMethodImplementation(
                        type,
                        referencedBody,
                        pending.Declaration));
                continue;
            }

            EntityRegistry.MethodDefinitionEntity? bodyMethod = null;
            bool isAmbiguous = false;
            foreach (EntityRegistry.MethodDefinitionEntity candidate in type.Methods)
            {
                if (candidate.Name != pending.BodyName ||
                    candidate.MethodSignature is null ||
                    !candidate.MethodSignature.ContentEquals(pending.BodySignature))
                {
                    continue;
                }

                if (bodyMethod is not null)
                {
                    isAmbiguous = true;
                    break;
                }

                bodyMethod = candidate;
            }

            if (bodyMethod is null || isAmbiguous)
            {
                ReportError(
                    DiagnosticIds.InvalidMetadataToken,
                    $"Override body method '{pending.BodyName}' could not be resolved uniquely",
                    pending.Location);
                continue;
            }

            type.MethodImplementations.Add(
                EntityRegistry.CreateUnrecordedMethodImplementation(
                    bodyMethod,
                    pending.Declaration));
        }
    }

    private BlobBuilder BuildClassMethodOverrideSignature(
        byte callingConvention,
        object? returnType,
        object? arguments,
        int genericArity)
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

        ImmutableArray<SignatureArgumentValue> argumentValues =
            GetSignatureArgumentsValue(arguments);
        ImmutableArray<SignatureArg> materializedArguments =
            MaterializeSignatureArguments(argumentValues);
        int parameterCount = 0;
        foreach (SignatureArg argument in materializedArguments)
        {
            if (!argument.IsSentinel)
            {
                parameterCount++;
            }
        }
        signature.WriteCompressedInteger(parameterCount);
        MaterializeType(GetTypeValue(returnType)).WriteContentTo(signature);
        foreach (SignatureArg argument in materializedArguments)
        {
            argument.SignatureBlob.WriteContentTo(signature);
        }

        return signature;
    }

    internal void BeginClassGenericParameterDirective(
        CILParser.ClassDeclContext context,
        IToken index)
    {
        PrepareClassMember();
        BeginClassGenericDirective(context, FindClassGenericParameter(context, ParseInt32(index)));
    }

    internal void BeginClassGenericParameterDirective(
        CILParser.ClassDeclContext context,
        string name)
    {
        PrepareClassMember();
        BeginClassGenericDirective(context, FindClassGenericParameter(name));
    }

    internal void BeginClassGenericConstraintDirective(
        CILParser.ClassDeclContext context,
        IToken index,
        object? constraintType)
    {
        PrepareClassMember();
        BeginClassGenericDirective(
            context,
            FindOrCreateClassGenericConstraint(
                FindClassGenericParameter(context, ParseInt32(index)),
                constraintType));
    }

    internal void BeginClassGenericConstraintDirective(
        CILParser.ClassDeclContext context,
        string name,
        object? constraintType)
    {
        PrepareClassMember();
        BeginClassGenericDirective(
            context,
            FindOrCreateClassGenericConstraint(
                FindClassGenericParameter(name),
                constraintType));
    }

    private void BeginClassGenericDirective(
        CILParser.ClassDeclContext context,
        EntityRegistry.EntityBase? owner)
    {
        _pendingClassCustomAttributeOwner = owner;
        _classGenericDirectiveFrames.Push(new(context, owner));
    }

    internal void AddClassGenericDirectiveAttribute(
        CILParser.ClassDeclContext context,
        CILParser.CustomAttrDeclContext attribute)
    {
        if (attribute.HasSyntaxError ||
            _classGenericDirectiveFrames.Count == 0 ||
            !ReferenceEquals(_classGenericDirectiveFrames.Peek().Owner, context) ||
            _classGenericDirectiveFrames.Peek().AttributeOwner is not { } owner)
        {
            return;
        }

        if (VisitCustomAttrDecl(attribute).Value is { } customAttribute)
        {
            customAttribute.Owner = owner;
        }
    }

    private EntityRegistry.GenericParameterEntity? FindClassGenericParameter(
        CILParser.ClassDeclContext context,
        int index)
    {
        EntityRegistry.TypeDefinitionEntity? currentType = _currentTypeDefinition.PeekOrDefault();
        if (currentType is not null &&
            index >= 0 &&
            index < currentType.GenericParameters.Count)
        {
            return currentType.GenericParameters[index];
        }

        ReportError(
            DiagnosticIds.GenericParameterIndexOutOfRange,
            string.Format(DiagnosticMessageTemplates.GenericParameterIndexOutOfRange, index),
            context);
        return null;
    }

    private EntityRegistry.GenericParameterEntity? FindClassGenericParameter(string name)
    {
        EntityRegistry.TypeDefinitionEntity? currentType = _currentTypeDefinition.PeekOrDefault();
        if (currentType is null)
        {
            return null;
        }

        foreach (EntityRegistry.GenericParameterEntity parameter in currentType.GenericParameters)
        {
            if (parameter.Name == name)
            {
                return parameter;
            }
        }

        return null;
    }

    private EntityRegistry.GenericParameterConstraintEntity? FindOrCreateClassGenericConstraint(
        EntityRegistry.GenericParameterEntity? parameter,
        object? constraintType)
    {
        if (parameter is null ||
            _currentTypeDefinition.PeekOrDefault() is not { } currentType)
        {
            return null;
        }

        EntityRegistry.TypeEntity baseType =
            ResolveTypeSpecification(GetTypeSpecificationValue(constraintType));
        foreach (EntityRegistry.GenericParameterConstraintEntity constraint in parameter.Constraints)
        {
            if (constraint.BaseType == baseType)
            {
                return constraint;
            }
        }

        EntityRegistry.GenericParameterConstraintEntity newConstraint =
            EntityRegistry.CreateGenericConstraint(baseType);
        newConstraint.Owner = parameter;
        parameter.Constraints.Add(newConstraint);
        currentType.GenericParameterConstraints.Add(newConstraint);
        return newConstraint;
    }

    internal void AddInterfaceImplementationAttribute(
        CILParser.ClassDeclContext context,
        object? interfaceType,
        CILParser.CustomDescrContext attribute)
    {
        PrepareClassMember();
        if (attribute.HasSyntaxError ||
            _currentTypeDefinition.PeekOrDefault() is not { } currentType)
        {
            return;
        }

        EntityRegistry.TypeEntity resolvedInterface =
            ResolveTypeSpecification(GetTypeSpecificationValue(interfaceType));
        EntityRegistry.InterfaceImplementationEntity? implementation = null;
        foreach (EntityRegistry.InterfaceImplementationEntity candidate in currentType.InterfaceImplementations)
        {
            if (candidate.InterfaceType == resolvedInterface)
            {
                implementation = candidate;
                break;
            }
        }

        if (implementation is null)
        {
            implementation =
                EntityRegistry.CreateUnrecordedInterfaceImplementation(currentType, resolvedInterface);
            currentType.InterfaceImplementations.Add(implementation);
        }

        VisitCustomDescr(attribute).Value.Owner = implementation;
        _ = context;
    }

    private void EndClassGenericDirective(CILParser.ClassDeclContext context)
    {
        if (_classGenericDirectiveFrames.Count > 0 &&
            ReferenceEquals(_classGenericDirectiveFrames.Peek().Owner, context))
        {
            _classGenericDirectiveFrames.Pop();
        }
    }
}
