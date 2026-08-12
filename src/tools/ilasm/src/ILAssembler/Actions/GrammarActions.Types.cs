// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    private readonly Stack<RuleContext> _namespaceOwners = new();
    private readonly Stack<RuleContext> _typeOwners = new();

    internal void BeginNamespace(CILParser.NameSpaceHeadContext context)
    {
        ClearPendingCustomAttributeOwners();
        string namespaceName = VisitNameSpaceHead(context).Value;
        string? outerNamespace = _currentNamespace.PeekOrDefault();
        _currentNamespace.Push(string.IsNullOrEmpty(outerNamespace) ? namespaceName : $"{outerNamespace}.{namespaceName}");
        _namespaceOwners.Push(context.Parent);
    }

    internal void BeginType(CILParser.ClassHeadContext context)
    {
        ClearPendingCustomAttributeOwners();
        _currentTypeDefinition.Push(VisitClassHead(context).Value);
        _typeOwners.Push(context.Parent);
    }

    /// <summary>
    /// Releases the namespace, type and method state that a top-level declaration introduced.
    /// </summary>
    /// <remarks>
    /// This runs from the <c>decl</c> rule's <c>finally</c> block so that a syntax error inside the
    /// declaration body cannot leak a namespace, type or method scope into the following declarations.
    /// </remarks>
    internal void EndDeclaration(CILParser.DeclContext context)
    {
        EndScopesOwnedBy(context);
        if (context.nameSpaceHead() is not null ||
            context.classHead() is not null ||
            context.methodHead() is not null)
        {
            ClearPendingCustomAttributeOwners();
        }
    }

    /// <summary>
    /// Releases the type and method state that a class member declaration introduced.
    /// </summary>
    internal void EndClassDeclaration(CILParser.ClassDeclContext context)
    {
        EndScopesOwnedBy(context);
        if (context.classHead() is not null || context.methodHead() is not null)
        {
            ClearPendingCustomAttributeOwners();
        }
    }

    private void EndScopesOwnedBy(RuleContext owner)
    {
        if (ReferenceEquals(_methodOwner, owner))
        {
            EndMethod();
        }

        if (_typeOwners.Count > 0 && ReferenceEquals(_typeOwners.Peek(), owner))
        {
            EndType();
        }

        if (_namespaceOwners.Count > 0 && ReferenceEquals(_namespaceOwners.Peek(), owner))
        {
            EndNamespace();
        }
    }

    private void EndType()
    {
        _typeOwners.Pop();
        _currentTypeDefinition.Pop();
        ClearPendingCustomAttributeOwners();
    }

    private void EndNamespace()
    {
        _namespaceOwners.Pop();
        _currentNamespace.Pop();
        ClearPendingCustomAttributeOwners();
    }

    private void ResetTypeScopes()
    {
        _typeOwners.Clear();
        _currentTypeDefinition.Clear();
        _namespaceOwners.Clear();
        _currentNamespace.Clear();
    }

    /// <summary>
    /// Drops the owners that a trailing <c>.custom</c> directive would bind to.
    /// </summary>
    /// <remarks>
    /// A trailing custom attribute only binds to the preceding field, generic parameter or generic
    /// constraint within the same type body, so the pending owners must be dropped whenever a
    /// namespace, type or method boundary is crossed.
    /// </remarks>
    private void ClearPendingCustomAttributeOwners()
    {
        _lastFieldDefinition = null;
        _pendingClassCustomAttributeOwner = null;
    }

#pragma warning disable CA1822 // Mark members as static
        GrammarResult ICILVisitor<GrammarResult>.VisitClassAttr(CILParser.ClassAttrContext context) => VisitClassAttr(context);

        public GrammarResult.Literal<(GrammarResult.Flag<TypeAttributes> Attribute, EntityRegistry.WellKnownBaseType? FallbackBase, bool RequireSealed)> VisitClassAttr(CILParser.ClassAttrContext context)
        {
            if (context.int32() is CILParser.Int32Context int32)
            {
                int value = VisitInt32(int32).Value;
                // COMPAT: The VALUE and ENUM keywords use sentinel values to pass through the fallback base type
                // in ILASM. These sentinel values can be provided through the "pass the value of the flag" feature,
                // so we detect those old flags here and provide the correct fallback type.
                bool requireSealed = false;
                EntityRegistry.WellKnownBaseType? fallbackBase = null;
                if ((value & 0x80000000) != 0)
                {
                    requireSealed = true;
                    fallbackBase = EntityRegistry.WellKnownBaseType.System_ValueType;
                }
                if ((value & 0x40000000) != 0)
                {
                    fallbackBase = EntityRegistry.WellKnownBaseType.System_Enum;
                }
                // Mask off the sentinel bits
                value &= unchecked((int)~0xC0000000);
                // COMPAT: When explicit flags are provided they always supercede previously set flags
                // (other than the sentinel values)
                return new((new((TypeAttributes)value, ShouldAppend: false), fallbackBase, requireSealed));
            }

            if (context.ENUM() is not null)
            {
                // COMPAT: ilasm implies the Sealed flag when using the 'value' keyword in a type declaration
                // even when the 'enum' keyword is used.
                return new((new(context.VALUE() is not null ? TypeAttributes.Sealed : 0), EntityRegistry.WellKnownBaseType.System_Enum, false));
            }
            else if (context.VALUE() is not null)
            {
                // COMPAT: ilasm implies the Sealed flag when using the 'value' keyword in a type declaration
                return new((new(TypeAttributes.Sealed), EntityRegistry.WellKnownBaseType.System_ValueType, true));
            }
            else if (context.EXPLICIT() is not null)
            {
                return new((new(TypeAttributes.ExplicitLayout), null, false));
            }
            else if (context.INTERFACE() is not null)
            {
                // COMPAT: interface implies abstract
                return new((new(TypeAttributes.Interface | TypeAttributes.Abstract), null, false));
            }

            switch (context.GetText())
            {
                case "public":
                    return new((new(TypeAttributes.Public, TypeAttributes.VisibilityMask), null, false));
                case "private":
                    return new((new(TypeAttributes.NotPublic, TypeAttributes.VisibilityMask), null, false));
                case "nestedpublic":
                    return new((new(TypeAttributes.NestedPublic, TypeAttributes.VisibilityMask), null, false));
                case "nestedprivate":
                    return new((new(TypeAttributes.NestedPrivate, TypeAttributes.VisibilityMask), null, false));
                case "nestedfamily":
                    return new((new(TypeAttributes.NestedFamily, TypeAttributes.VisibilityMask), null, false));
                case "nestedassembly":
                    return new((new(TypeAttributes.NestedAssembly, TypeAttributes.VisibilityMask), null, false));
                case "nestedfamandassem":
                    return new((new(TypeAttributes.NestedFamANDAssem, TypeAttributes.VisibilityMask), null, false));
                case "nestedfamorassem":
                    return new((new(TypeAttributes.NestedFamORAssem, TypeAttributes.VisibilityMask), null, false));
                case "ansi":
                    return new((new(TypeAttributes.AnsiClass, TypeAttributes.StringFormatMask), null, false));
                case "autochar":
                    return new((new(TypeAttributes.AutoClass, TypeAttributes.StringFormatMask), null, false));
                case "unicode":
                    return new((new(TypeAttributes.UnicodeClass, TypeAttributes.StringFormatMask), null, false));
                case "auto":
                    return new((new(TypeAttributes.AutoLayout, TypeAttributes.LayoutMask), null, false));
                case "sequential":
                    return new((new(TypeAttributes.SequentialLayout, TypeAttributes.LayoutMask), null, false));
                case "extended":
                    return new((new(TypeAttributes.ExtendedLayout, TypeAttributes.LayoutMask), null, false));
                case "sealed":
                    return new((new(TypeAttributes.Sealed), null, false));
                case "abstract":
                    return new((new(TypeAttributes.Abstract), null, false));
                case "import":
                    return new((new(TypeAttributes.Import), null, false));
                case "serializable":
#pragma warning disable SYSLIB0050
                    return new((new(TypeAttributes.Serializable), null, false));
#pragma warning restore SYSLIB0050
                case "windowsruntime":
                    return new((new(TypeAttributes.WindowsRuntime), null, false));
                case "beforefieldinit":
                    return new((new(TypeAttributes.BeforeFieldInit), null, false));
                case "specialname":
                    return new((new(TypeAttributes.SpecialName), null, false));
                case "rtspecialname":
                    return new((new(TypeAttributes.RTSpecialName), null, false));
                default:
                    return new((new((TypeAttributes)Enum.Parse(typeof(TypeAttributes), context.GetText(), true)), null, false));
            }
        }

        public GrammarResult VisitClassDecls(CILParser.ClassDeclsContext context) => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);


        GrammarResult ICILVisitor<GrammarResult>.VisitClassHead(CILParser.ClassHeadContext context) => VisitClassHead(context);
        public GrammarResult.Literal<EntityRegistry.TypeDefinitionEntity> VisitClassHead(CILParser.ClassHeadContext context)
        {
            string typeFullName = VisitDottedName(context.dottedName()).Value;
            int typeFullNameLastDot = typeFullName.LastIndexOf('.');
            // A dot at position 0 is part of the name (e.g., ".GlobalStruct"), not a namespace separator
            if (typeFullNameLastDot == 0)
            {
                typeFullNameLastDot = -1;
            }
            string typeNS;
            if (_currentTypeDefinition.Count != 0)
            {
                if (typeFullNameLastDot == -1)
                {
                    typeNS = string.Empty;
                }
                else
                {
                    typeNS = typeFullName.Substring(0, typeFullNameLastDot);
                }
            }
            else
            {
                if (typeFullNameLastDot == -1)
                {
                    typeNS = _currentNamespace.PeekOrDefault() ?? string.Empty;
                }
                else
                {
                    typeNS = $"{_currentNamespace.PeekOrDefault()}{typeFullName.Substring(0, typeFullNameLastDot)}";
                }
            }

            bool isNewType = false;

            var typeDefinition = _entityRegistry.GetOrCreateTypeDefinition(
                _currentTypeDefinition.PeekOrDefault(),
                typeNS,
                typeFullNameLastDot != -1
                    ? typeFullName.Substring(typeFullNameLastDot + 1)
                    : typeFullName,
                (newTypeDef) =>
                {
                    isNewType = true;
                    EntityRegistry.WellKnownBaseType? fallbackBase = _options.NoAutoInherit ? null : EntityRegistry.WellKnownBaseType.System_Object;
                    bool requireSealed = false;
                    var classAttrs = context.classAttr();
                    newTypeDef.Attributes = classAttrs.Select(VisitClassAttr).Aggregate(
                        (TypeAttributes)0,
                        (acc, result) =>
                        {
                            var (attribute, implicitBase, attrRequireSealed) = result.Value;
                            if (implicitBase is not null)
                            {
                                fallbackBase = implicitBase;
                            }
                            if (!attribute.ShouldAppend)
                            {
                                requireSealed = attrRequireSealed;
                                return attribute.Value;
                            }
                            requireSealed |= attrRequireSealed;
                            if (attribute.Value == TypeAttributes.RTSpecialName)
                            {
                                // COMPAT: ILASM ignores the rtspecialname directive on a type.
                                return acc;
                            }
                            if ((attribute.Value & TypeAttributes.Interface) != 0)
                            {
                                // COMPAT: interface implies abstract
                                return acc | TypeAttributes.Interface | TypeAttributes.Abstract;
                            }
                            // Use the Flag's | operator which handles group masks
                            // (visibility, layout, string format) correctly.
                            return acc | attribute;
                        });


                    // Two-pass generic parameter processing:
                    // Pass 1: Register all parameter names (without resolving constraints)
                    var typarContexts = context.typarsClause()?.typars()?.typar() ?? Array.Empty<CILParser.TyparContext>();
                    for (int i = 0; i < typarContexts.Length; i++)
                    {
                        var attributes = VisitTyparAttribs(typarContexts[i].typarAttribs()).Value;
                        var param = EntityRegistry.CreateGenericParameter(attributes, VisitDottedName(typarContexts[i].dottedName()).Value);
                        param.Owner = newTypeDef;
                        param.Index = i;
                        newTypeDef.GenericParameters.Add(param);
                    }

                    // Push the type so that !T references in constraints, extends, and implements can resolve
                    _currentTypeDefinition.Push(newTypeDef);

                    // Pass 2: Resolve constraints (now all params are registered and type is on stack)
                    for (int i = 0; i < typarContexts.Length; i++)
                    {
                        var param = newTypeDef.GenericParameters[i];
                        foreach (var constraint in VisitTyBound(typarContexts[i].tyBound()).Value)
                        {
                            constraint.Owner = param;
                            param.Constraints.Add(constraint);
                            newTypeDef.GenericParameterConstraints.Add(constraint);
                        }
                    }

                    if (context.extendsClause() is CILParser.ExtendsClauseContext extends)
                    {
                        newTypeDef.BaseType = VisitExtendsClause(context.extendsClause()).Value;
                    }

                    if (context.implClause() is CILParser.ImplClauseContext impl)
                    {
                        newTypeDef.InterfaceImplementations.AddRange(VisitImplClause(context.implClause()).Value);
                    }

                    _currentTypeDefinition.Pop();

                    // Interfaces should not have an implicit base type
                    if (newTypeDef.Attributes.HasFlag(TypeAttributes.Interface))
                    {
                        fallbackBase = null;
                    }

                    newTypeDef.BaseType ??= _entityRegistry.ResolveImplicitBaseType(fallbackBase);

                    // When the user has provided a type definition for a type that directly inherits
                    // System.ValueType but has not sealed it, emit a warning and add the 'sealed' modifier.
                    if (!newTypeDef.Attributes.HasFlag(TypeAttributes.Sealed) &&
                        (requireSealed // COMPAT: when both the sentinel values for 'value' and 'enum' are explicitly
                                       // specified, the sealed modifier is required even though
                                       // the base type isn't System.ValueType.
                        || _entityRegistry.SystemValueTypeType.Equals(newTypeDef.BaseType)))
                    {
                        _diagnostics.Add(
                            new Diagnostic(
                                DiagnosticIds.UnsealedValueType,
                                DiagnosticSeverity.Error,
                                string.Format(DiagnosticMessageTemplates.UnsealedValueType, newTypeDef.Name),
                                Location.From(context.dottedName().Stop, _documents)));
                        newTypeDef.Attributes |= TypeAttributes.Sealed;
                    }
                });

            if (!isNewType)
            {
                // Type was forward-referenced. Apply attributes, generic params,
                // base type, and interface implementations that were deferred.
                var classAttrs = context.classAttr();
                typeDefinition.Attributes = classAttrs.Select(VisitClassAttr).Aggregate(
                    typeDefinition.Attributes,
                    (acc, result) =>
                    {
                        var (attribute, _, _) = result.Value;
                        if (!attribute.ShouldAppend)
                            return attribute.Value;
                        if ((attribute.Value & TypeAttributes.Interface) != 0)
                            return acc | TypeAttributes.Interface | TypeAttributes.Abstract;
                        return acc | attribute.Value;
                    });

                if (typeDefinition.GenericParameters.Count == 0)
                {
                    // Two-pass generic parameter processing for forward-referenced types
                    var typarContexts = context.typarsClause()?.typars()?.typar() ?? Array.Empty<CILParser.TyparContext>();
                    for (int i = 0; i < typarContexts.Length; i++)
                    {
                        var attributes = VisitTyparAttribs(typarContexts[i].typarAttribs()).Value;
                        var param = EntityRegistry.CreateGenericParameter(attributes, VisitDottedName(typarContexts[i].dottedName()).Value);
                        param.Owner = typeDefinition;
                        param.Index = i;
                        typeDefinition.GenericParameters.Add(param);
                    }

                    _currentTypeDefinition.Push(typeDefinition);

                    // Pass 2: Resolve constraints
                    for (int i = 0; i < typarContexts.Length; i++)
                    {
                        var param = typeDefinition.GenericParameters[i];
                        foreach (var constraint in VisitTyBound(typarContexts[i].tyBound()).Value)
                        {
                            constraint.Owner = param;
                            param.Constraints.Add(constraint);
                            typeDefinition.GenericParameterConstraints.Add(constraint);
                        }
                    }
                }
                else
                {
                    _ = context.typarsClause().Accept(this);
                    _currentTypeDefinition.Push(typeDefinition);
                }

                if (context.extendsClause() is CILParser.ExtendsClauseContext extends && typeDefinition.BaseType is null)
                {
                    typeDefinition.BaseType = VisitExtendsClause(extends).Value;
                }
                else
                {
                    _ = context.extendsClause()?.Accept(this);
                }

                if (context.implClause() is CILParser.ImplClauseContext impl)
                {
                    typeDefinition.InterfaceImplementations.AddRange(VisitImplClause(impl).Value);
                }

                _currentTypeDefinition.Pop();
            }

            return new(typeDefinition);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitClassName(CILParser.ClassNameContext context) => VisitClassName(context);
        public GrammarResult.Literal<EntityRegistry.TypeEntity> VisitClassName(CILParser.ClassNameContext context)
        {
            if (context.THIS() is not null)
            {
                if (_currentTypeDefinition.Count == 0)
                {
                    ReportError(DiagnosticIds.ThisOutsideClass, DiagnosticMessageTemplates.ThisOutsideClass, context);
                    return new(new EntityRegistry.FakeTypeEntity(default(TypeDefinitionHandle)));
                }
                var thisType = _currentTypeDefinition.Peek();
                return new(thisType);
            }
            else if (context.BASE() is not null)
            {
                if (_currentTypeDefinition.Count == 0)
                {
                    ReportError(DiagnosticIds.BaseOutsideClass, DiagnosticMessageTemplates.BaseOutsideClass, context);
                    return new(new EntityRegistry.FakeTypeEntity(default(TypeDefinitionHandle)));
                }
                var baseType = _currentTypeDefinition.Peek().BaseType;
                if (baseType is null)
                {
                    ReportError(DiagnosticIds.NoBaseType, DiagnosticMessageTemplates.NoBaseType, context);
                    return new(new EntityRegistry.FakeTypeEntity(default(TypeDefinitionHandle)));
                }
                return new(baseType);
            }
            else if (context.NESTER() is not null)
            {
                if (_currentTypeDefinition.Count < 2)
                {
                    ReportError(DiagnosticIds.NesterOutsideNestedClass, DiagnosticMessageTemplates.NesterOutsideNestedClass, context);
                    return new(new EntityRegistry.FakeTypeEntity(default(TypeDefinitionHandle)));
                }
                var nesterType = _currentTypeDefinition.Peek().ContainingType!;
                return new(nesterType);
            }
            else if (context.slashedName() is CILParser.SlashedNameContext slashedName)
            {
                EntityRegistry.EntityBase? resolutionContext = null;
                if (context.dottedName() is CILParser.DottedNameContext dottedAssemblyOrModuleName)
                {
                    if (context.MODULE() is not null)
                    {
                        string moduleName = VisitDottedName(dottedAssemblyOrModuleName).Value;
                        resolutionContext = _entityRegistry.FindModuleReference(moduleName);
                        if (resolutionContext is null)
                        {
                            ReportError(DiagnosticIds.ModuleNotFound, string.Format(DiagnosticMessageTemplates.ModuleNotFound, moduleName), context);
                            return new(new EntityRegistry.FakeTypeEntity(default(TypeDefinitionHandle)));
                        }
                    }
                    else
                    {
                        resolutionContext = _entityRegistry.GetOrCreateAssemblyReference(VisitDottedName(dottedAssemblyOrModuleName).Value, newRef => { });
                    }
                }
                else if (context.mdtoken() is CILParser.MdtokenContext typeRefScope)
                {
                    resolutionContext = VisitMdtoken(typeRefScope).Value;
                }
                else if (context.PTR() is not null)
                {
                    resolutionContext = new EntityRegistry.FakeTypeEntity(default(ModuleDefinitionHandle));
                }

                if (resolutionContext is not null)
                {
                    EntityRegistry.TypeReferenceEntity typeRef = _entityRegistry.GetOrCreateTypeReference(resolutionContext, VisitSlashedName(slashedName).Value);
                    return new(typeRef);
                }

                Debug.Assert(resolutionContext is null);

                return new(ResolveTypeDef());

                // Resolve typedef references
                EntityRegistry.TypeEntity ResolveTypeDef()
                {
                    TypeName typeName = VisitSlashedName(slashedName).Value;
                    if (typeName.ContainingTypeName is null)
                    {
                        // Check for typedef.
                        var typedefResult = TryResolveTypedefAsType(typeName.DottedName);
                        if (typedefResult is not null)
                        {
                            return typedefResult;
                        }
                    }

                    // COMPAT: Before creating a forward-reference TypeDef, check if the type
                    // matches a well-known corelib type. Native ilasm resolves unqualified
                    // references to types like System.String as TypeRefs from the corelib.
                    if (typeName.ContainingTypeName is null)
                    {
                        var (ns, nm) = NameHelpers.SplitDottedNameToNamespaceAndName(typeName.DottedName);
                        if (ns == "System" && nm is "String" or "Object" or "ValueType" or "Enum"
                            or "Type" or "Array" or "Delegate" or "MulticastDelegate"
                            or "Exception" or "Attribute")
                        {
                            var coreLib = _entityRegistry.GetCoreLibAssemblyReference();
                            return _entityRegistry.GetOrCreateTypeReference(coreLib, typeName);
                        }
                    }

                    Stack<TypeName> containingTypes = new();
                    for (TypeName? containingType = typeName; containingType is not null; containingType = containingType.ContainingTypeName)
                    {
                        containingTypes.Push(containingType);
                    }
                    EntityRegistry.TypeDefinitionEntity? typeDef = null;
                    while (containingTypes.Count != 0)
                    {
                        TypeName containingType = containingTypes.Pop();

                        (string ns, string name) = NameHelpers.SplitDottedNameToNamespaceAndName(containingType.DottedName);

                        typeDef = _entityRegistry.GetOrCreateTypeDefinition(
                            typeDef,
                            ns,
                            name,
                            _ => { });
                    }

                    return typeDef!;
                }
            }
            else if (context.mdtoken() is CILParser.MdtokenContext typeToken)
            {
                EntityRegistry.EntityBase resolvedToken = VisitMdtoken(typeToken).Value;

                if (resolvedToken is not EntityRegistry.TypeEntity type)
                {
                    return new(new EntityRegistry.FakeTypeEntity(resolvedToken.Handle));
                }
                return new(type);
            }

            throw new UnreachableException();
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitClassSeq(CILParser.ClassSeqContext context) => VisitClassSeq(context);
        public GrammarResult.FormattedBlob VisitClassSeq(CILParser.ClassSeqContext context)
        {
            BlobBuilder objSeqBlob = new(0);
            foreach (var item in context.classSeqElement())
            {
                objSeqBlob.LinkSuffix(VisitClassSeqElement(item).Value);
            }
            return new(objSeqBlob);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitClassSeqElement(CILParser.ClassSeqElementContext context) => VisitClassSeqElement(context);

        public GrammarResult.FormattedBlob VisitClassSeqElement(CILParser.ClassSeqElementContext context)
        {
            BlobBuilder blob = new();
            if (context.className() is CILParser.ClassNameContext className)
            {
                if (VisitClassName(className).Value is EntityRegistry.IHasReflectionNotation notation)
                {
                    blob.WriteSerializedString(notation.ReflectionNotation);
                }
                else
                {
                    blob.WriteSerializedString("");
                }
                return new(blob);
            }

            blob.WriteSerializedString(
                context.SQSTRING() is { } stringNode
                    ? StringHelpers.ParseQuotedString(stringNode.Symbol.Text)
                    : null);
            return new(blob);
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitExtendsClause(CILParser.ExtendsClauseContext context) => VisitExtendsClause(context);

        public GrammarResult.Literal<EntityRegistry.TypeEntity?> VisitExtendsClause(CILParser.ExtendsClauseContext context)
        {
            if (context.typeSpec() is CILParser.TypeSpecContext typeSpec)
            {
                return new(VisitTypeSpec(typeSpec).Value);
            }
            else
            {
                return new(null);
            }
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitNameSpaceHead(CILParser.NameSpaceHeadContext context) => VisitNameSpaceHead(context);

        public static GrammarResult.String VisitNameSpaceHead(CILParser.NameSpaceHeadContext context) => VisitDottedName(context.dottedName());

        GrammarResult ICILVisitor<GrammarResult>.VisitTyBound(CILParser.TyBoundContext context) => VisitTyBound(context);
        public GrammarResult.Sequence<EntityRegistry.GenericParameterConstraintEntity> VisitTyBound(CILParser.TyBoundContext? context)
        {
            // context or typeList can be null when there are no constraints
            if (context?.typeList() is not CILParser.TypeListContext typeList)
            {
                return new(ImmutableArray<EntityRegistry.GenericParameterConstraintEntity>.Empty);
            }
            // Filter out null types (from unresolved type parameters) before creating constraints
            return new(VisitTypeList(typeList).Value
                .Where(t => t is not null)
                .Select(EntityRegistry.CreateGenericConstraint)
                .ToImmutableArray());
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitTypar(CILParser.TyparContext context) => VisitTypar(context);

        public GrammarResult.Literal<EntityRegistry.GenericParameterEntity> VisitTypar(CILParser.TyparContext context)
        {
            GenericParameterAttributes attributes = VisitTyparAttribs(context.typarAttribs()).Value;
            EntityRegistry.GenericParameterEntity genericParameter = EntityRegistry.CreateGenericParameter(attributes, VisitDottedName(context.dottedName()).Value);

            foreach (var constraint in VisitTyBound(context.tyBound()).Value)
            {
                genericParameter.Constraints.Add(constraint);
            }

            return new(genericParameter);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitTyparAttrib(CILParser.TyparAttribContext context) => VisitTyparAttrib(context);
        public GrammarResult.Flag<GenericParameterAttributes> VisitTyparAttrib(CILParser.TyparAttribContext context)
        {
            return context switch
            {
                { covariant: not null } => new(GenericParameterAttributes.Covariant),
                { contravariant: not null } => new(GenericParameterAttributes.Contravariant),
                { @class: not null } => new(GenericParameterAttributes.ReferenceTypeConstraint),
                { valuetype: not null } => new(GenericParameterAttributes.NotNullableValueTypeConstraint),
                { byrefLike: not null } => new(GenericParameterAttributes.AllowByRefLike),
                { ctor: not null } => new(GenericParameterAttributes.DefaultConstructorConstraint),
                { flags: CILParser.Int32Context int32 } => new((GenericParameterAttributes)VisitInt32(int32).Value),
                _ => throw new UnreachableException()
            };
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitTyparAttribs(CILParser.TyparAttribsContext context) => VisitTyparAttribs(context);

        public GrammarResult.Literal<GenericParameterAttributes> VisitTyparAttribs(CILParser.TyparAttribsContext context) =>
            new(context.typarAttrib()
                .Select(VisitTyparAttrib)
                .Aggregate(
                    (GenericParameterAttributes)0, (agg, attr) => agg | attr));

        GrammarResult ICILVisitor<GrammarResult>.VisitTypars(CILParser.TyparsContext context) => VisitTypars(context);
        public GrammarResult.Sequence<EntityRegistry.GenericParameterEntity> VisitTypars(CILParser.TyparsContext context)
        {
            CILParser.TyparContext[] typeParameters = context.typar();
            ImmutableArray<EntityRegistry.GenericParameterEntity>.Builder builder = ImmutableArray.CreateBuilder<EntityRegistry.GenericParameterEntity>(typeParameters.Length);

            foreach (var typeParameter in typeParameters)
            {
                builder.Add(VisitTypar(typeParameter).Value);
            }
            return new(builder.MoveToImmutable());
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitTyparsClause(CILParser.TyparsClauseContext context) => VisitTyparsClause(context);
        public GrammarResult.Sequence<EntityRegistry.GenericParameterEntity> VisitTyparsClause(CILParser.TyparsClauseContext context) => context.typars() is null ? new(ImmutableArray<EntityRegistry.GenericParameterEntity>.Empty) : VisitTypars(context.typars());

#pragma warning restore CA1822 // Mark members as static
}
