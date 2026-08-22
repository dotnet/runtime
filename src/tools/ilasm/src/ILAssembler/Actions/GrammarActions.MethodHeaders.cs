// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    internal void BeginMethod(CILParser.MethodHeadContext context, MethodHeaderValue value)
    {
        ResetMethodBodyState();
        if (!value.IsValid)
        {
            return;
        }

        MethodHeaderValue header = value;

        EntityRegistry.TypeDefinitionEntity containingType =
            _currentTypeDefinition.PeekOrDefault() ?? _entityRegistry.ModuleType;
        EntityRegistry.MethodDefinitionEntity methodDefinition =
            EntityRegistry.CreateUnrecordedMethodDefinition(containingType, header.Name);

        _currentMethod = new(methodDefinition);
        try
        {
            RegisterGenericParameterNames(
                methodDefinition,
                methodDefinition.GenericParameters,
                header.GenericParameters);

            methodDefinition.MethodAttributes = header.Attributes;
            ApplyImplicitMethodAttributes(methodDefinition);

            if (methodDefinition.MethodAttributes.HasFlag(MethodAttributes.Abstract) &&
                !methodDefinition.ContainingType.Attributes.HasFlag(TypeAttributes.Abstract))
            {
                ReportWarning(
                    DiagnosticIds.AbstractMethodNotInAbstractType,
                    string.Format(
                        DiagnosticMessageTemplates.AbstractMethodNotInAbstractType,
                        methodDefinition.Name),
                    context);
            }

            ApplyPInvokeInformation(methodDefinition, header, context);

            byte signatureHeader = header.CallingConvention;
            if (header.GenericParameters.Length != 0)
            {
                signatureHeader |= (byte)SignatureAttributes.Generic;
            }

            SignatureHeader parsedHeader = new(signatureHeader);
            if (!methodDefinition.MethodAttributes.HasFlag(MethodAttributes.Static) &&
                !parsedHeader.IsInstance &&
                _currentTypeDefinition.Count > 0)
            {
                signatureHeader |= (byte)SignatureAttributes.Instance;
                parsedHeader = new(signatureHeader);
            }
            if (parsedHeader.HasExplicitThis && !parsedHeader.IsInstance)
            {
                signatureHeader |= (byte)SignatureAttributes.Instance;
                parsedHeader = new(signatureHeader);
            }

            BlobBuilder methodSignature = new();
            methodSignature.WriteByte(signatureHeader);
            if (header.GenericParameters.Length != 0)
            {
                methodSignature.WriteCompressedInteger(header.GenericParameters.Length);
            }

            MaterializeGenericParameterConstraints(
                methodDefinition.GenericParameters,
                methodDefinition.GenericParameterConstraints,
                header.GenericParameters);

            ImmutableArray<SignatureArg> arguments = MaterializeSignatureArguments(header.Arguments);
            methodSignature.WriteCompressedInteger(arguments.Length);

            SignatureArg returnValue = new(
                (ParameterAttributes)header.ReturnAttributes,
                MaterializeType(header.ReturnType),
                MaterializeMarshallingDescriptor(header.ReturnMarshalling),
                null);
            returnValue.SignatureBlob.WriteContentTo(methodSignature);
            methodDefinition.Parameters.Add(
                EntityRegistry.CreateParameter(
                    returnValue.Attributes,
                    returnValue.Name,
                    returnValue.MarshallingDescriptor,
                    0));

            for (int i = 0; i < arguments.Length; i++)
            {
                SignatureArg argument = arguments[i];
                argument.SignatureBlob.WriteContentTo(methodSignature);
                string? parameterName = argument.Name ?? $"A_{i}";
                methodDefinition.Parameters.Add(
                    EntityRegistry.CreateParameter(
                        argument.Attributes,
                        parameterName,
                        argument.MarshallingDescriptor,
                        i + 1));
            }

            methodDefinition.SignatureHeader = parsedHeader;
            methodDefinition.MethodSignature = methodSignature;
            methodDefinition.ImplementationAttributes = header.ImplementationAttributes;

            if (!EntityRegistry.TryAddMethodDefinitionToContainingType(methodDefinition))
            {
                ReportError(
                    DiagnosticIds.DuplicateMethod,
                    DiagnosticMessageTemplates.DuplicateMethod,
                    context);
            }

            _currentMethod = new(methodDefinition);
            _methodOwner = context.Parent;
        }
        catch
        {
            _currentMethod = null;
            _methodOwner = null;
            ResetMethodBodyState();
            throw;
        }
    }

    private static void ApplyImplicitMethodAttributes(EntityRegistry.MethodDefinitionEntity method)
    {
        if (method.Name is ".ctor" or ".cctor")
        {
            method.MethodAttributes |= MethodAttributes.RTSpecialName | MethodAttributes.SpecialName;
        }
        else if (method.MethodAttributes.HasFlag(MethodAttributes.RTSpecialName))
        {
            method.MethodAttributes |= MethodAttributes.SpecialName;
        }
    }

    private void ApplyPInvokeInformation(
        EntityRegistry.MethodDefinitionEntity method,
        MethodHeaderValue header,
        CILParser.MethodHeadContext context)
    {
        (EntityRegistry.ModuleReferenceEntity Module, string? EntryPoint, MethodImportAttributes Attributes)?
            pInvokeInformation = null;

        foreach (PInvokeValue pInvoke in header.PInvokes)
        {
            if (pInvoke.ModuleName is null)
            {
                ReportError(
                    DiagnosticIds.InvalidPInvokeSignature,
                    DiagnosticMessageTemplates.InvalidPInvokeSignature,
                    context);
                continue;
            }

            pInvokeInformation = (
                _entityRegistry.GetOrCreateModuleReference(pInvoke.ModuleName, _ => { }),
                pInvoke.EntryPointName ?? header.Name,
                pInvoke.Attributes);
        }

        method.MethodImportInformation = pInvokeInformation;
    }

}
