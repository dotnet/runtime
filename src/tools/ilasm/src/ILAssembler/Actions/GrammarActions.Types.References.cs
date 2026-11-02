// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Antlr4.Runtime;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    internal TypeName AddSlashedNamePart(TypeName? containingTypeName, string name)
        => new(containingTypeName, name);

    internal ClassNameValue CreateUnqualifiedClassName(TypeName name)
        => new UnqualifiedClassNameValue(name);

    internal ClassNameValue CreateAssemblyQualifiedClassName(string assemblyName, TypeName name)
        => new AssemblyQualifiedClassNameValue(assemblyName, name);

    internal ClassNameValue CreateModuleQualifiedClassName(
        IToken token,
        string moduleName,
        TypeName name)
        => new ModuleQualifiedClassNameValue(token, moduleName, name);

    internal ClassNameValue CreateTokenQualifiedClassName(int scopeToken, TypeName name)
        => new TokenQualifiedClassNameValue(scopeToken, name);

    internal ClassNameValue CreatePointerQualifiedClassName(TypeName name)
        => new PointerQualifiedClassNameValue(name);

    internal ClassNameValue CreateTokenClassName(int typeToken)
        => new TokenClassNameValue(typeToken);

    internal ClassNameValue CreateThisClassName(IToken token)
        => new SpecialClassNameValue(token, SpecialClassNameKind.This);

    internal ClassNameValue CreateBaseClassName(IToken token)
        => new SpecialClassNameValue(token, SpecialClassNameKind.Base);

    internal ClassNameValue CreateNesterClassName(IToken token)
        => new SpecialClassNameValue(token, SpecialClassNameKind.Nester);

    private EntityRegistry.TypeEntity ResolveClassName(ClassNameValue className)
    {
        switch (className)
        {
            case SpecialClassNameValue { Kind: SpecialClassNameKind.This } special:
                if (_currentTypeDefinition.Count == 0)
                {
                    ReportError(DiagnosticIds.ThisOutsideClass, DiagnosticMessageTemplates.ThisOutsideClass, special.Token);
                    return new EntityRegistry.FakeTypeEntity(default(TypeDefinitionHandle));
                }
                return _currentTypeDefinition.Peek();
            case SpecialClassNameValue { Kind: SpecialClassNameKind.Base } special:
                if (_currentTypeDefinition.Count == 0)
                {
                    ReportError(DiagnosticIds.BaseOutsideClass, DiagnosticMessageTemplates.BaseOutsideClass, special.Token);
                    return new EntityRegistry.FakeTypeEntity(default(TypeDefinitionHandle));
                }
                if (_currentTypeDefinition.Peek().BaseType is not { } baseType)
                {
                    ReportError(DiagnosticIds.NoBaseType, DiagnosticMessageTemplates.NoBaseType, special.Token);
                    return new EntityRegistry.FakeTypeEntity(default(TypeDefinitionHandle));
                }
                return baseType;
            case SpecialClassNameValue { Kind: SpecialClassNameKind.Nester } special:
                if (_currentTypeDefinition.Count < 2)
                {
                    ReportError(DiagnosticIds.NesterOutsideNestedClass, DiagnosticMessageTemplates.NesterOutsideNestedClass, special.Token);
                    return new EntityRegistry.FakeTypeEntity(default(TypeDefinitionHandle));
                }
                return _currentTypeDefinition.Peek().ContainingType!;
            case AssemblyQualifiedClassNameValue assembly:
                return _entityRegistry.GetOrCreateTypeReference(
                    _entityRegistry.GetOrCreateAssemblyReference(assembly.AssemblyName, _ => { }),
                    assembly.Name);
            case ModuleQualifiedClassNameValue module:
                if (_entityRegistry.FindModuleReference(module.ModuleName) is not { } moduleReference)
                {
                    ReportError(
                        DiagnosticIds.ModuleNotFound,
                        string.Format(DiagnosticMessageTemplates.ModuleNotFound, module.ModuleName),
                        module.Token);
                    return new EntityRegistry.FakeTypeEntity(default(TypeDefinitionHandle));
                }
                return _entityRegistry.GetOrCreateTypeReference(moduleReference, module.Name);
            case TokenQualifiedClassNameValue tokenScope:
                return _entityRegistry.GetOrCreateTypeReference(
                    ResolveMetadataToken(tokenScope.Token),
                    tokenScope.Name);
            case PointerQualifiedClassNameValue pointer:
                return _entityRegistry.GetOrCreateTypeReference(
                    new EntityRegistry.FakeTypeEntity(default(ModuleDefinitionHandle)),
                    pointer.Name);
            case UnqualifiedClassNameValue unqualified:
                return ResolveUnqualifiedClassName(unqualified.Name);
            case TokenClassNameValue typeToken:
                EntityRegistry.EntityBase resolvedToken = ResolveMetadataToken(typeToken.Token);
                return resolvedToken is EntityRegistry.TypeEntity type
                    ? type
                    : new EntityRegistry.FakeTypeEntity(resolvedToken.Handle);
            default:
                return new EntityRegistry.FakeTypeEntity(default(TypeDefinitionHandle));
        }
    }

    private EntityRegistry.TypeEntity ResolveUnqualifiedClassName(TypeName typeName)
    {
        if (typeName.ContainingTypeName is null && TryResolveTypedefAsType(typeName.DottedName) is { } typedef)
        {
            return typedef;
        }

        if (typeName.ContainingTypeName is null)
        {
            (string ns, string name) = NameHelpers.SplitDottedNameToNamespaceAndName(typeName.DottedName);
            if (ns == "System" && name is "String" or "Object" or "ValueType" or "Enum"
                or "Type" or "Array" or "Delegate" or "MulticastDelegate"
                or "Exception" or "Attribute")
            {
                return _entityRegistry.GetOrCreateTypeReference(_entityRegistry.GetCoreLibAssemblyReference(), typeName);
            }
        }

        Stack<TypeName> containingTypes = new();
        for (TypeName? containingType = typeName; containingType is not null; containingType = containingType.ContainingTypeName)
        {
            containingTypes.Push(containingType);
        }

        EntityRegistry.TypeDefinitionEntity? typeDefinition = null;
        while (containingTypes.Count != 0)
        {
            TypeName containingType = containingTypes.Pop();
            (string ns, string name) = NameHelpers.SplitDottedNameToNamespaceAndName(containingType.DottedName);
            typeDefinition = _entityRegistry.GetOrCreateTypeDefinition(typeDefinition, ns, name, _ => { });
        }

        Debug.Assert(typeDefinition is not null);
        return typeDefinition;
    }

    private EntityRegistry.TypeEntity ResolveTypeSpecification(TypeSpecificationValue typeSpecification)
    {
        return typeSpecification switch
        {
            ClassTypeSpecificationValue classType => ResolveClassName(classType.ClassName),
            ModuleTypeSpecificationValue module => ResolveModuleTypeSpecification(module.ModuleName),
            AssemblyTypeSpecificationValue assembly => new EntityRegistry.FakeTypeEntity(
                _entityRegistry.GetOrCreateAssemblyReference(assembly.AssemblyName, _ => { }).Handle),
            SignatureTypeSpecificationValue signature => _entityRegistry.GetOrCreateTypeSpec(MaterializeType(signature.Type)),
            _ => new EntityRegistry.FakeTypeEntity(default(TypeDefinitionHandle))
        };
    }

    private EntityRegistry.FakeTypeEntity ResolveModuleTypeSpecification(string moduleName)
    {
        EntityRegistry.ModuleReferenceEntity? module = _entityRegistry.FindModuleReference(moduleName);
        return module is null
            ? new EntityRegistry.FakeTypeEntity(MetadataTokens.ModuleReferenceHandle(0))
            : new EntityRegistry.FakeTypeEntity(module.Handle);
    }

    private EntityRegistry.EntityBase ResolveMetadataToken(int token)
        => _entityRegistry.ResolveHandleToEntity(MetadataTokens.EntityHandle(token));
}
