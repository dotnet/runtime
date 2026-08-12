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
    internal void OnClassDeclaration(CILParser.ClassDeclContext context)
    {
        if (context.classHead() is not null || context.methodHead() is not null)
        {
            return;
        }

        VisitClassDecl(context);
    }

    public GrammarResult VisitClassDecl(CILParser.ClassDeclContext context)
    {
        bool isStandaloneCustomAttribute =
            context.customAttrDecl().Length == 1 &&
            context.PARAM() is null;
        if (!isStandaloneCustomAttribute)
        {
            _pendingClassCustomAttributeOwner = null;
        }

        if (context.classHead() is not null || context.methodHead() is not null)
        {
            throw new UnreachableException(StructuralNodeIsDrivenByParserActions);
        }

        if (context.secDecl() is {} secDecl)
        {
            var declarativeSecurity = VisitSecDecl(secDecl).Value;
            declarativeSecurity?.Parent = _currentTypeDefinition.PeekOrDefault();
        }
        else if (context.TYPE() is not null &&
                 context.typeSpec().Length == 1 &&
                 context.customDescr() is { } interfaceAttribute)
        {
            var currentType = _currentTypeDefinition.PeekOrDefault();
            if (currentType is not null)
            {
                EntityRegistry.TypeEntity interfaceType = VisitTypeSpec(context.typeSpec()[0]).Value;
                EntityRegistry.InterfaceImplementationEntity? implementation =
                    currentType.InterfaceImplementations.FirstOrDefault(
                        candidate => candidate.InterfaceType == interfaceType);
                if (implementation is null)
                {
                    implementation =
                        EntityRegistry.CreateUnrecordedInterfaceImplementation(currentType, interfaceType);
                    currentType.InterfaceImplementations.Add(implementation);
                }

                VisitCustomDescr(interfaceAttribute).Value.Owner = implementation;
            }
        }
        else if (context.fieldDecl() is {} fieldDecl)
        {
            _ = VisitFieldDecl(fieldDecl);
        }
        else if (context.dataDecl() is { } dataDecl)
        {
            _ = VisitDataDecl(dataDecl);
        }
        else if (context.extSourceSpec() is { } extSourceSpec)
        {
            _ = VisitExtSourceSpec(extSourceSpec);
        }
        else if (context.languageDecl() is { } languageDecl)
        {
            _ = VisitLanguageDecl(languageDecl);
        }
        else if (context.OVERRIDE() is not null)
        {
            var currentType = _currentTypeDefinition.PeekOrDefault();
            if (currentType is not null)
            {
                var typeSpecs = context.typeSpec();
                var methodNames = context.methodName();
                var callConventions = context.callConv();
                var returnTypes = context.type();
                var signatureArguments = context.sigArgs();
                var genericArities = context.genArity();

                int bodySignatureIndex = context.METHOD().Length == 0 ? 0 : 1;
                BlobBuilder declarationSignature = BuildMethodReferenceSignature(
                    callConventions[0],
                    returnTypes[0],
                    signatureArguments[0],
                    genericArities.Length > 0 ? VisitGenArity(genericArities[0]).Value : 0);
                BlobBuilder bodySignature = context.METHOD().Length == 0
                    ? declarationSignature
                    : BuildMethodReferenceSignature(
                        callConventions[bodySignatureIndex],
                        returnTypes[bodySignatureIndex],
                        signatureArguments[bodySignatureIndex],
                        genericArities.Length > bodySignatureIndex
                            ? VisitGenArity(genericArities[bodySignatureIndex]).Value
                            : 0);

                string bodyName = VisitMethodName(methodNames[1]).Value;
                EntityRegistry.MethodDefinitionEntity[] bodyMethods = currentType.Methods
                    .Where(method =>
                        method.Name == bodyName &&
                        method.MethodSignature is not null &&
                        method.MethodSignature.ContentEquals(bodySignature))
                    .Take(2)
                    .ToArray();
                if (bodyMethods.Length != 1)
                {
                    ReportError(
                        DiagnosticIds.InvalidMetadataToken,
                        $"Override body method '{bodyName}' could not be resolved uniquely",
                        context);
                    return GrammarResult.SentinelValue.Result;
                }

                EntityRegistry.MethodDefinitionEntity bodyMethod = bodyMethods[0];
                EntityRegistry.MemberReferenceEntity declaration =
                    _entityRegistry.CreateLazilyRecordedMemberReference(
                        VisitTypeSpec(typeSpecs[0]).Value,
                        VisitMethodName(methodNames[0]).Value,
                        declarationSignature);
                currentType.MethodImplementations.Add(
                    EntityRegistry.CreateUnrecordedMethodImplementation(bodyMethod, declaration));
            }
        }
        else if (context.int32() is {} int32)
        {
            // .pack or .size
            string keyword = context.GetChild(0).GetText();
            int value = VisitInt32(int32).Value;
            var currentType = _currentTypeDefinition.PeekOrDefault();
            if (currentType is not null)
            {
                if (keyword == ".pack")
                {
                    currentType.PackingSize = value;
                }
                else if (keyword == ".size")
                {
                    currentType.ClassSize = value;
                }
            }
        }
        else if (context.propHead() is CILParser.PropHeadContext propHead)
        {
            var property = VisitPropHead(propHead).Value;
            var currentType = _currentTypeDefinition.PeekOrDefault();
            if (currentType is not null)
            {
                currentType.Properties.Add(property);
                foreach (var propDecl in context.propDecls().propDecl())
                {
                    if (propDecl.customAttrDecl() is { } customAttrDecl)
                    {
                        var customAttr = VisitCustomAttrDecl(customAttrDecl).Value;
                        if (customAttr is not null)
                        {
                            customAttr.Owner = property;
                        }
                    }
                    else if (VisitPropDecl(propDecl).Value is { } accessor)
                    {
                        property.Accessors.Add(accessor);
                    }
                }
            }
        }
        else if (context.eventHead() is CILParser.EventHeadContext eventHead)
        {
            var evt = VisitEventHead(eventHead).Value;
            var currentType = _currentTypeDefinition.PeekOrDefault();
            if (currentType is not null)
            {
                currentType.Events.Add(evt);
                foreach (var eventDecl in context.eventDecls().eventDecl())
                {
                    if (eventDecl.customAttrDecl() is { } customAttrDecl)
                    {
                        var customAttr = VisitCustomAttrDecl(customAttrDecl).Value;
                        if (customAttr is not null)
                        {
                            customAttr.Owner = evt;
                        }
                    }
                    else if (VisitEventDecl(eventDecl).Value is { } accessor)
                    {
                        evt.Accessors.Add(accessor);
                    }
                }
            }
        }
        else if (context.OVERRIDE() is not null)
        {
            var currentType = _currentTypeDefinition.PeekOrDefault();
            if (currentType is null)
            {
                throw new UnreachableException();
            }

            CILParser.CallConvContext[] callConvs = context.callConv();
            CILParser.TypeContext[] returnTypes = context.type();
            CILParser.TypeSpecContext[] owners = context.typeSpec();
            CILParser.MethodNameContext[] methodNames = context.methodName();
            CILParser.GenArityContext[] genericArities = context.genArity();
            CILParser.SigArgsContext[] parameterLists = context.sigArgs();

            EntityRegistry.MemberReferenceEntity declaration;
            EntityRegistry.MemberReferenceEntity body;
            if (callConvs.Length == 2)
            {
                declaration = CreateExplicitMethodReference(
                    callConvs[0], returnTypes[0], owners[0], methodNames[0], genericArities[0], parameterLists[0]);
                body = CreateExplicitMethodReference(
                    callConvs[1], returnTypes[1], owners[1], methodNames[1], genericArities[1], parameterLists[1]);
            }
            else
            {
                EntityRegistry.TypeEntity declarationOwner = VisitTypeSpec(owners[0]).Value;
                string declarationName = VisitMethodName(methodNames[0]).Value;
                EntityRegistry.TypeEntity bodyOwner = VisitTypeSpec(owners[1]).Value;
                string bodyName = VisitMethodName(methodNames[1]).Value;
                BlobBuilder bodySignature = CreateExplicitMethodSignature(
                    callConvs[0], returnTypes[0], genericArity: null, parameterLists[0]);
                BlobBuilder declarationSignature = new();
                bodySignature.WriteContentTo(declarationSignature);
                declaration = _entityRegistry.CreateLazilyRecordedMemberReference(
                    declarationOwner, declarationName, declarationSignature);
                body = _entityRegistry.CreateLazilyRecordedMemberReference(
                    bodyOwner, bodyName, bodySignature);
            }

            currentType.MethodImplementations.Add(EntityRegistry.CreateUnrecordedMethodImplementation(currentType, body, declaration));
        }
        else if (isStandaloneCustomAttribute)
        {
            if (VisitCustomAttrDecl(context.customAttrDecl()[0]).Value is { } customAttr)
            {
                customAttr.Owner =
                    _pendingClassCustomAttributeOwner ??
                    _currentTypeDefinition.PeekOrDefault();
            }
        }
        else if (context.PARAM() is not null)
        {
            var customAttrDeclarations = context.customAttrDecl();
            var currentType = _currentTypeDefinition.PeekOrDefault();
            if (currentType is not null && context.TYPE() is not null)
            {
                EntityRegistry.GenericParameterEntity? param = null;
                if (context.int32() is { } int32ctx)
                {
                    int index = VisitInt32(int32ctx).Value;
                    if (index >= 0 && index < currentType.GenericParameters.Count)
                    {
                        param = currentType.GenericParameters[index];
                    }
                    else
                    {
                        ReportError(
                            DiagnosticIds.GenericParameterIndexOutOfRange,
                            string.Format(DiagnosticMessageTemplates.GenericParameterIndexOutOfRange, index),
                            context);
                    }
                }
                else if (context.dottedName() is { } dn)
                {
                    string name = VisitDottedName(dn).Value;
                    foreach (var genericParam in currentType.GenericParameters)
                    {
                        if (genericParam.Name == name)
                        {
                            param = genericParam;
                            break;
                        }
                    }
                }
                if (param is not null)
                {
                    foreach (var attr in customAttrDeclarations ?? Array.Empty<CILParser.CustomAttrDeclContext>())
                    {
                        var customAttrDecl = VisitCustomAttrDecl(attr).Value;
                        customAttrDecl?.Owner = param;
                    }
                    _pendingClassCustomAttributeOwner = param;
                }
            }
            else if (currentType is not null && context.CONSTRAINT() is not null)
            {
                EntityRegistry.GenericParameterEntity? param = null;
                if (context.int32() is { } int32ctx)
                {
                    int index = VisitInt32(int32ctx).Value;
                    if (index >= 0 && index < currentType.GenericParameters.Count)
                    {
                        param = currentType.GenericParameters[index];
                    }
                    else
                    {
                        ReportError(
                            DiagnosticIds.GenericParameterIndexOutOfRange,
                            string.Format(DiagnosticMessageTemplates.GenericParameterIndexOutOfRange, index),
                            context);
                    }
                }
                else if (context.dottedName() is { } dn)
                {
                    string name = VisitDottedName(dn).Value;
                    foreach (var genericParam in currentType.GenericParameters)
                    {
                        if (genericParam.Name == name)
                        {
                            param = genericParam;
                            break;
                        }
                    }
                }
                if (param is not null)
                {
                    var baseType = VisitTypeSpec(context.typeSpec()[0]).Value;
                    EntityRegistry.GenericParameterConstraintEntity? constraint =
                        param.Constraints.FirstOrDefault(entity => entity.BaseType == baseType);
                    if (constraint is null)
                    {
                        constraint = EntityRegistry.CreateGenericConstraint(baseType);
                        constraint.Owner = param;
                        param.Constraints.Add(constraint);
                        currentType.GenericParameterConstraints.Add(constraint);
                    }
                    foreach (var attr in customAttrDeclarations ?? Array.Empty<CILParser.CustomAttrDeclContext>())
                    {
                        var customAttrDecl = VisitCustomAttrDecl(attr).Value;
                        customAttrDecl?.Owner = constraint;
                    }
                    _pendingClassCustomAttributeOwner = constraint;
                }
            }
        }

        return GrammarResult.SentinelValue.Result;
    }

#pragma warning disable CA1822 // Mark members as static
        GrammarResult ICILVisitor<GrammarResult>.VisitEventAttr(CILParser.EventAttrContext context) => VisitEventAttr(context);
        public GrammarResult.Flag<EventAttributes> VisitEventAttr(CILParser.EventAttrContext context)
        {
            return context.GetText() switch
            {
                "specialname" => new(EventAttributes.SpecialName),
                "rtspecialname" => new(0), // COMPAT: Ignore
                _ => throw new UnreachableException(),
            };
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitEventDecl(CILParser.EventDeclContext context) => VisitEventDecl(context);
        public GrammarResult.Literal<(MethodSemanticsAttributes, EntityRegistry.EntityBase)?> VisitEventDecl(CILParser.EventDeclContext context)
        {
            if (context.ChildCount != 2)
            {
                return new(null);
            }
            string accessor = context.GetChild(0).GetText();
            EntityRegistry.EntityBase memberReference = VisitMethodRef(context.methodRef()).Value;
            MethodSemanticsAttributes methodSemanticsAttributes = accessor switch
            {
                ".addon" => MethodSemanticsAttributes.Adder,
                ".removeon" => MethodSemanticsAttributes.Remover,
                ".fire" => MethodSemanticsAttributes.Raiser,
                ".other" => MethodSemanticsAttributes.Other,
                _ => throw new UnreachableException(),
            };
            return new((methodSemanticsAttributes, memberReference));
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitEventDecls(CILParser.EventDeclsContext context) => VisitEventDecls(context);
        public GrammarResult.Sequence<(MethodSemanticsAttributes, EntityRegistry.EntityBase)> VisitEventDecls(CILParser.EventDeclsContext context)
            => new(
                context.eventDecl()
                .Select(decl => VisitEventDecl(decl).Value)
                .Where(decl => decl is not null)
                .Select(decl => decl!.Value).ToImmutableArray());

        GrammarResult ICILVisitor<GrammarResult>.VisitEventHead(CILParser.EventHeadContext context) => VisitEventHead(context);
        public GrammarResult.Literal<EntityRegistry.EventEntity> VisitEventHead(CILParser.EventHeadContext context)
        {
            string name = VisitDottedName(context.dottedName()).Value;
            EventAttributes eventAttributes = context.eventAttr().Select(attr => VisitEventAttr(attr).Value).Aggregate((EventAttributes)0, (a, b) => a | b);
            return new(new EntityRegistry.EventEntity(eventAttributes, VisitTypeSpec(context.typeSpec()).Value, name));
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitFieldAttr(CILParser.FieldAttrContext context) => VisitFieldAttr(context);
        public GrammarResult.Flag<FieldAttributes> VisitFieldAttr(CILParser.FieldAttrContext context)
        {
            if (context.int32() is { } int32)
            {
                return new((FieldAttributes)VisitInt32(int32).Value, ShouldAppend: false);
            }

            return context.GetText() switch
            {
                "static" => new(FieldAttributes.Static),
                "public" => new(FieldAttributes.Public, FieldAttributes.FieldAccessMask),
                "private" => new(FieldAttributes.Private, FieldAttributes.FieldAccessMask),
                "family" => new(FieldAttributes.Family, FieldAttributes.FieldAccessMask),
                "initonly" => new(FieldAttributes.InitOnly),
                "rtspecialname" => new(FieldAttributes.RTSpecialName),
                "specialname" => new(FieldAttributes.SpecialName),
                "assembly" => new(FieldAttributes.Assembly, FieldAttributes.FieldAccessMask),
                "famandassem" => new(FieldAttributes.FamANDAssem, FieldAttributes.FieldAccessMask),
                "famorassem" => new(FieldAttributes.FamORAssem, FieldAttributes.FieldAccessMask),
                "privatescope" => new(FieldAttributes.PrivateScope, FieldAttributes.FieldAccessMask),
                "literal" => new(FieldAttributes.Literal),
#pragma warning disable SYSLIB0050 // FieldAttributes.NotSeralized is obsolete
                "notserialized" => new(FieldAttributes.NotSerialized),
#pragma warning restore SYSLIB0050 // FieldAttributes.NotSeralized is obsolete
                "volatile" => new(0), // COMPAT: volatile is not a field attribute; accepted for compatibility
                _ => throw new UnreachableException()
            };
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitFieldDecl(CILParser.FieldDeclContext context) => VisitFieldDecl(context);
        public GrammarResult VisitFieldDecl(CILParser.FieldDeclContext context)
        {
            var fieldAttrs = context.fieldAttr().Select(VisitFieldAttr).Aggregate((FieldAttributes)0, (a, b) => a | b);
            // COMPAT: Native ilasm implicitly adds SpecialName when RTSpecialName is set
            if (fieldAttrs.HasFlag(FieldAttributes.RTSpecialName))
            {
                fieldAttrs |= FieldAttributes.SpecialName;
            }
            var fieldType = VisitType(context.type()).Value;
            var marshalBlobs = context.marshalBlob();
            var marshalBlob = marshalBlobs.Length > 0 ? VisitMarshalBlob(marshalBlobs[marshalBlobs.Length - 1]).Value : null;
            string name = VisitDottedName(context.dottedName()).Value;
            var rvaOffset = VisitAtOpt(context.atOpt()).Value;
            var fieldOffset = VisitRepeatOpt(context.repeatOpt()).Value;
            var constantValue = VisitInitOpt(context.initOpt()).Value;

            var signature = new BlobEncoder(new BlobBuilder());
            _ = signature.Field();
            fieldType.WriteContentTo(signature.Builder);

            var field = EntityRegistry.CreateUnrecordedFieldDefinition(fieldAttrs, _currentTypeDefinition.PeekOrDefault() ?? _entityRegistry.ModuleType, name, signature.Builder);
            _lastFieldDefinition = field;
            _pendingClassCustomAttributeOwner = field;

            if (field is not null)
            {
                field.MarshallingDescriptor = marshalBlob;
                field.DataDeclarationName = rvaOffset;
                field.Offset = fieldOffset;
                if (constantValue is not NoConstantSentinel)
                {
                    field.ConstantValue = constantValue;
                    field.HasConstant = true;
                }
            }

            return GrammarResult.SentinelValue.Result;
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitFieldInit(CILParser.FieldInitContext context) => VisitFieldInit(context);
        public GrammarResult.Literal<object?> VisitFieldInit(CILParser.FieldInitContext context)
        {
            // fieldInit: fieldSerInit | compQstring | NULLREF;
            if (context.NULLREF() is not null)
            {
                return new(null);
            }
            if (context.compQstring() is CILParser.CompQstringContext compQstring)
            {
                return new(VisitCompQstring(compQstring).Value);
            }
            if (context.fieldSerInit() is CILParser.FieldSerInitContext fieldSerInit)
            {
                // fieldSerInit returns a blob with type byte prefix - extract the actual value
                var blob = VisitFieldSerInit(fieldSerInit).Value;
                return new(ExtractConstantFromSerInit(blob));
            }
            return new(null);
        }

        public GrammarResult VisitFieldOrProp(CILParser.FieldOrPropContext context) => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);

        GrammarResult ICILVisitor<GrammarResult>.VisitFieldRef(CILParser.FieldRefContext context) => VisitFieldRef(context);
        public GrammarResult.Literal<EntityRegistry.EntityBase> VisitFieldRef(CILParser.FieldRefContext context)
        {
            if (context.type() is not CILParser.TypeContext type)
            {
                // This is a typedef reference for a field member
                string alias = VisitDottedName(context.dottedName()).Value;
                var resolved = TryResolveTypedefAsMember(alias);
                if (resolved is not null)
                {
                    return new(resolved);
                }
                ReportError(DiagnosticIds.TypedefNotFound, string.Format(DiagnosticMessageTemplates.TypedefNotFound, alias), context);
                return new(_entityRegistry.CreateLazilyRecordedMemberReference(_entityRegistry.ModuleType, alias, new BlobBuilder()));
            }

            var fieldTypeSig = VisitType(type).Value;
            EntityRegistry.TypeEntity definingType = _entityRegistry.ModuleType;
            if (context.typeSpec() is CILParser.TypeSpecContext typeSpec)
            {
                definingType = VisitTypeSpec(typeSpec).Value;
            }

            string name = VisitDottedName(context.dottedName()).Value;

            var fieldSig = new BlobBuilder(fieldTypeSig.Count + 1);
            fieldSig.WriteByte((byte)SignatureKind.Field);
            fieldTypeSig.WriteContentTo(fieldSig);
            return new(_entityRegistry.CreateLazilyRecordedMemberReference(definingType, name, fieldSig));
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitInitOpt(CILParser.InitOptContext context) => VisitInitOpt(context);
        public GrammarResult.Literal<object?> VisitInitOpt(CILParser.InitOptContext context)
        {
            if (context.fieldInit() is CILParser.FieldInitContext fieldInit)
            {
                return VisitFieldInit(fieldInit);
            }
            // No initializer - return a sentinel indicating no constant
            return new(NoConstantSentinel.Instance);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitMemberRef(CILParser.MemberRefContext context) => VisitMemberRef(context);
        public GrammarResult.Literal<EntityRegistry.EntityBase> VisitMemberRef(CILParser.MemberRefContext context)
        {
            if (context.mdtoken() is CILParser.MdtokenContext mdToken)
            {
                return VisitMdtoken(mdToken);
            }

            if (context.methodRef() is CILParser.MethodRefContext methodRef)
            {
                return VisitMethodRef(methodRef);
            }
            if (context.fieldRef() is CILParser.FieldRefContext fieldRef)
            {
                return VisitFieldRef(fieldRef);
            }

            throw new UnreachableException();
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitPropAttr(CILParser.PropAttrContext context) => VisitPropAttr(context);
        public static GrammarResult.Flag<PropertyAttributes> VisitPropAttr(CILParser.PropAttrContext context)
        {
            return context.GetText() switch
            {
                "specialname" => new(PropertyAttributes.SpecialName),
                "rtspecialname" => new(0), // COMPAT: Ignore
                _ => throw new UnreachableException(),
            };
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitPropDecl(CILParser.PropDeclContext context) => VisitPropDecl(context);
        public GrammarResult.Literal<(MethodSemanticsAttributes, EntityRegistry.EntityBase)?> VisitPropDecl(CILParser.PropDeclContext context)
        {
            if (context.ChildCount != 2)
            {
                return new(null);
            }
            string accessor = context.GetChild(0).GetText();
            EntityRegistry.EntityBase memberReference = VisitMethodRef(context.methodRef()).Value;
            MethodSemanticsAttributes methodSemanticsAttributes = accessor switch
            {
                ".set" => MethodSemanticsAttributes.Setter,
                ".get" => MethodSemanticsAttributes.Getter,
                ".other" => MethodSemanticsAttributes.Other,
                _ => throw new UnreachableException(),
            };
            return new((methodSemanticsAttributes, memberReference));
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitPropDecls(CILParser.PropDeclsContext context) => VisitPropDecls(context);
        public GrammarResult.Sequence<(MethodSemanticsAttributes, EntityRegistry.EntityBase)> VisitPropDecls(CILParser.PropDeclsContext context)
            => new(
                context.propDecl()
                .Select(decl => VisitPropDecl(decl).Value)
                .Where(decl => decl is not null)
                .Select(decl => decl!.Value).ToImmutableArray());

        GrammarResult ICILVisitor<GrammarResult>.VisitPropHead(ILAssembler.CILParser.PropHeadContext context) => VisitPropHead(context);
        public GrammarResult.Literal<EntityRegistry.PropertyEntity> VisitPropHead(CILParser.PropHeadContext context)
        {
            var propAttrs = context.propAttr().Select(VisitPropAttr).Aggregate((PropertyAttributes)0, (a, b) => a | b);
            var name = VisitDottedName(context.dottedName()).Value;

            var signature = new BlobBuilder();
            byte callConv = (byte)(VisitCallConv(context.callConv()).Value | (byte)SignatureKind.Property);
            signature.WriteByte(callConv);
            var args = VisitSigArgs(context.sigArgs()).Value;
            signature.WriteCompressedInteger(args.Length);
            VisitType(context.type()).Value.WriteContentTo(signature);
            foreach (var arg in args)
            {
                arg.SignatureBlob.WriteContentTo(signature);
            }

            // Handle initOpt to set the Constant table entry if a constant value is provided.
            var constantValue = VisitInitOpt(context.initOpt()).Value;
            var property = new EntityRegistry.PropertyEntity(propAttrs, signature, name);
            if (constantValue is not NoConstantSentinel)
            {
                property.ConstantValue = constantValue;
                property.HasConstant = true;
                property.Attributes |= PropertyAttributes.HasDefault;
            }
            return new(property);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitRepeatOpt(CILParser.RepeatOptContext context) => VisitRepeatOpt(context);
        public GrammarResult.Literal<int?> VisitRepeatOpt(CILParser.RepeatOptContext context) => context.int32() is {} int32 ? new(VisitInt32(int32).Value) : new(null);

#pragma warning restore CA1822 // Mark members as static
}
