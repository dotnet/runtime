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

#pragma warning disable CA1822 // Mark members as static
internal sealed partial class GrammarActions
{
        GrammarResult ICILVisitor<GrammarResult>.VisitCatchClause(CILParser.CatchClauseContext context) => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        GrammarResult ICILVisitor<GrammarResult>.VisitExportDecl(CILParser.ExportDeclContext context) => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        public GrammarResult VisitFaultClause(CILParser.FaultClauseContext context) => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);

        GrammarResult ICILVisitor<GrammarResult>.VisitFilterClause(CILParser.FilterClauseContext context) => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        public GrammarResult VisitFinallyClause(CILParser.FinallyClauseContext context) => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);

        GrammarResult ICILVisitor<GrammarResult>.VisitHandlerBlock(CILParser.HandlerBlockContext context) => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        GrammarResult ICILVisitor<GrammarResult>.VisitImplAttr(ILAssembler.CILParser.ImplAttrContext context) => VisitImplAttr(context);
        public GrammarResult.Flag<MethodImplAttributes> VisitImplAttr(CILParser.ImplAttrContext context)
        {
            if (context.int32() is CILParser.Int32Context int32)
            {
                return new((MethodImplAttributes)VisitInt32(int32).Value, ShouldAppend: false);
            }
            string attribute = context.GetText();
            return attribute switch
            {
                "native" => new(MethodImplAttributes.Native, MethodImplAttributes.CodeTypeMask),
                "cil" or "il" => new(MethodImplAttributes.IL, MethodImplAttributes.CodeTypeMask),
                "optil" => new(MethodImplAttributes.OPTIL, MethodImplAttributes.CodeTypeMask),
                "managed" => new(MethodImplAttributes.Managed, MethodImplAttributes.ManagedMask),
                "unmanaged" => new(MethodImplAttributes.Unmanaged, MethodImplAttributes.ManagedMask),
                "forwardref" => new(MethodImplAttributes.ForwardRef),
                "preservesig" => new(MethodImplAttributes.PreserveSig),
                "runtime" => new(MethodImplAttributes.Runtime, MethodImplAttributes.CodeTypeMask),
                "internalcall" => new(MethodImplAttributes.InternalCall),
                "synchronized" => new(MethodImplAttributes.Synchronized),
                "noinlining" => new(MethodImplAttributes.NoInlining),
                "aggressiveinlining" => new(MethodImplAttributes.AggressiveInlining),
                "nooptimization" => new(MethodImplAttributes.NoOptimization),
                "aggressiveoptimization" => new(MethodImplAttributes.AggressiveOptimization),
                "async" => new(MethodImplAttributes.Async),
                _ => throw new UnreachableException(),
            };
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitImplClause(CILParser.ImplClauseContext context) => VisitImplClause(context);
        public GrammarResult.Sequence<EntityRegistry.InterfaceImplementationEntity> VisitImplClause(CILParser.ImplClauseContext context) => context.implList() is {} implList ? VisitImplList(implList) : new(ImmutableArray<EntityRegistry.InterfaceImplementationEntity>.Empty);

        GrammarResult ICILVisitor<GrammarResult>.VisitImplList(CILParser.ImplListContext context) => VisitImplList(context);
        public GrammarResult.Sequence<EntityRegistry.InterfaceImplementationEntity> VisitImplList(CILParser.ImplListContext context)
        {
            var builder = ImmutableArray.CreateBuilder<EntityRegistry.InterfaceImplementationEntity>();
            foreach (var impl in context.typeSpec())
            {
                builder.Add(EntityRegistry.CreateUnrecordedInterfaceImplementation(_currentTypeDefinition.PeekOrDefault()!, VisitTypeSpec(impl).Value));
            }
            return new(builder.ToImmutable());
        }

        private void ValidateLabelReferences()
        {
            if (_currentMethod is null)
            {
                return;
            }

            // Report errors for any labels that were referenced but never declared
            foreach (var undefinedLabel in _currentMethod.UndefinedLabelReferences)
            {
                ReportError(
                    DiagnosticIds.LabelNotFound,
                    string.Format(DiagnosticMessageTemplates.LabelNotFound, undefinedLabel.Key),
                    undefinedLabel.Value);
            }
        }

        public GrammarResult VisitLabels(CILParser.LabelsContext context) => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);

        GrammarResult ICILVisitor<GrammarResult>.VisitLabelDecl(CILParser.LabelDeclContext context) => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        GrammarResult ICILVisitor<GrammarResult>.VisitLocalsDecl(CILParser.LocalsDeclContext context) => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        GrammarResult ICILVisitor<GrammarResult>.VisitMethAttr(CILParser.MethAttrContext context) => VisitMethAttr(context);
        public GrammarResult.Flag<MethodAttributes> VisitMethAttr(CILParser.MethAttrContext context)
        {
            if (context.int32() is CILParser.Int32Context int32)
            {
                return new((MethodAttributes)VisitInt32(int32).Value, ShouldAppend: false);
            }
            string attribute = context.GetText();
            return attribute switch
            {
                "static" => new(MethodAttributes.Static),
                "public" => new(MethodAttributes.Public, MethodAttributes.MemberAccessMask),
                "private" => new(MethodAttributes.Private, MethodAttributes.MemberAccessMask),
                "family" => new(MethodAttributes.Family, MethodAttributes.MemberAccessMask),
                "final" => new(MethodAttributes.Final),
                "specialname" => new(MethodAttributes.SpecialName),
                "virtual" => new(MethodAttributes.Virtual),
                "strict" => new(MethodAttributes.CheckAccessOnOverride),
                "abstract" => new(MethodAttributes.Abstract),
                "assembly" => new(MethodAttributes.Assembly, MethodAttributes.MemberAccessMask),
                "famandassem" => new(MethodAttributes.FamANDAssem, MethodAttributes.MemberAccessMask),
                "famorassem" => new(MethodAttributes.FamORAssem, MethodAttributes.MemberAccessMask),
                "privatescope" => new(MethodAttributes.PrivateScope, MethodAttributes.MemberAccessMask),
                "hidebysig" => new(MethodAttributes.HideBySig),
                "newslot" => new(MethodAttributes.NewSlot),
                "rtspecialname" => new(MethodAttributes.RTSpecialName),
                "unmanagedexp" => new(MethodAttributes.UnmanagedExport),
                "reqsecobj" => new(MethodAttributes.RequireSecObject),
                _ => throw new UnreachableException(),
            };
        }

        public GrammarResult VisitMethodDecls(CILParser.MethodDeclsContext context) => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        GrammarResult ICILVisitor<GrammarResult>.VisitMethodDecl(CILParser.MethodDeclContext context) => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        GrammarResult ICILVisitor<GrammarResult>.VisitMethodHead(CILParser.MethodHeadContext context) => VisitMethodHead(context);
        public GrammarResult.Literal<EntityRegistry.MethodDefinitionEntity> VisitMethodHead(CILParser.MethodHeadContext context)
        {
            string name = VisitMethodName(context.methodName()).Value;
            var containingType = _currentTypeDefinition.PeekOrDefault() ?? _entityRegistry.ModuleType;
            var methodDefinition = EntityRegistry.CreateUnrecordedMethodDefinition(containingType, name);

            BlobBuilder methodSignature = new();
            byte sigHeader = VisitCallConv(context.callConv()).Value;

            // Two-pass generic parameter processing for method params:
            // Pass 1: Register all parameter names (without resolving constraints)
            _currentMethod = new(methodDefinition);
            var typarContexts = context.typarsClause()?.typars()?.typar() ?? Array.Empty<CILParser.TyparContext>();
            for (int i = 0; i < typarContexts.Length; i++)
            {
                var attributes = VisitTyparAttribs(typarContexts[i].typarAttribs()).Value;
                var param = EntityRegistry.CreateGenericParameter(attributes, VisitDottedName(typarContexts[i].dottedName()).Value);
                param.Owner = methodDefinition;
                param.Index = i;
                methodDefinition.GenericParameters.Add(param);
            }
            if (typarContexts.Length != 0)
            {
                sigHeader |= (byte)SignatureAttributes.Generic;
            }
            methodDefinition.MethodAttributes = context.methAttr().Aggregate((MethodAttributes)0, (acc, attr) => acc | VisitMethAttr(attr));

            // COMPAT: Native ilasm implicitly adds RTSpecialName + SpecialName for .ctor/.cctor methods
            if (name is ".ctor" or ".cctor")
            {
                methodDefinition.MethodAttributes |= MethodAttributes.RTSpecialName | MethodAttributes.SpecialName;
            }
            // COMPAT: Native ilasm implicitly adds SpecialName when RTSpecialName is set
            else if (methodDefinition.MethodAttributes.HasFlag(MethodAttributes.RTSpecialName))
            {
                methodDefinition.MethodAttributes |= MethodAttributes.SpecialName;
            }

            if (methodDefinition.MethodAttributes.HasFlag(MethodAttributes.Abstract) && !methodDefinition.ContainingType.Attributes.HasFlag(TypeAttributes.Abstract))
            {
                ReportWarning(DiagnosticIds.AbstractMethodNotInAbstractType,
                    string.Format(DiagnosticMessageTemplates.AbstractMethodNotInAbstractType, methodDefinition.Name),
                    context);
            }

            (EntityRegistry.ModuleReferenceEntity Module, string? EntryPoint, MethodImportAttributes Attributes)? pInvokeInformation = null;
            foreach (var pInvokeInfo in context.pinvImpl())
            {
                var (moduleName, entryPoint, attributes) = VisitPinvImpl(pInvokeInfo).Value;
                if (moduleName is null)
                {
                    ReportError(DiagnosticIds.InvalidPInvokeSignature,
                        DiagnosticMessageTemplates.InvalidPInvokeSignature,
                        pInvokeInfo);
                    continue;
                }
                pInvokeInformation = (_entityRegistry.GetOrCreateModuleReference(moduleName, _ => { }), entryPoint ?? name, attributes);
            }
            methodDefinition.MethodImportInformation = pInvokeInformation;

            SignatureHeader parsedHeader = new(sigHeader);
            if (methodDefinition.MethodAttributes.HasFlag(MethodAttributes.Static) && (parsedHeader.IsInstance || parsedHeader.HasExplicitThis))
            {
                // Error on static + instance.
            }
            // COMPAT: Native ilasm auto-adds instance calling convention for non-static methods in class context
            if (!methodDefinition.MethodAttributes.HasFlag(MethodAttributes.Static)
                && !parsedHeader.IsInstance
                && _currentTypeDefinition.Count > 0)
            {
                sigHeader |= (byte)SignatureAttributes.Instance;
                parsedHeader = new(sigHeader);
            }
            if (parsedHeader.HasExplicitThis && !parsedHeader.IsInstance)
            {
                // Warn on explicit-this + non-instance
                parsedHeader = new(sigHeader |= (byte)SignatureAttributes.Instance);
            }
            methodSignature.WriteByte(sigHeader);
            if (typarContexts.Length != 0)
            {
                methodSignature.WriteCompressedInteger(typarContexts.Length);
            }
            // Pass 2: Resolve constraints (now all params are registered)
            for (int i = 0; i < typarContexts.Length; i++)
            {
                var param = methodDefinition.GenericParameters[i];
                foreach (var constraint in VisitTyBound(typarContexts[i].tyBound()).Value)
                {
                    constraint.Owner = param;
                    param.Constraints.Add(constraint);
                    methodDefinition.GenericParameterConstraints.Add(constraint);
                }
            }

            var args = VisitSigArgs(context.sigArgs()).Value;
            methodSignature.WriteCompressedInteger(args.Length);

            SignatureArg returnValue = new(VisitParamAttr(context.paramAttr()).Value, VisitType(context.type()).Value, VisitMarshalClause(context.marshalClause()).Value, null);

            returnValue.SignatureBlob.WriteContentTo(methodSignature);
            methodDefinition.Parameters.Add(EntityRegistry.CreateParameter(returnValue.Attributes, returnValue.Name, returnValue.MarshallingDescriptor, 0));
            for (int i = 0; i < args.Length; i++)
            {
                SignatureArg? arg = args[i];
                arg.SignatureBlob.WriteContentTo(methodSignature);
                // COMPAT: Native ilasm auto-generates A_N names for unnamed parameters
                string? paramName = arg.Name ?? $"A_{i}";
                methodDefinition.Parameters.Add(EntityRegistry.CreateParameter(arg.Attributes, paramName, arg.MarshallingDescriptor, i + 1));
            }
            // We've parsed all signature information. We can reset the current method now (the caller will handle setting/unsetting it for the method body).
            _currentMethod = null;
            methodDefinition.SignatureHeader = parsedHeader;
            methodDefinition.MethodSignature = methodSignature;

            methodDefinition.ImplementationAttributes = context.implAttr().Aggregate((MethodImplAttributes)0, (acc, attr) => acc | VisitImplAttr(attr));
            if (!EntityRegistry.TryAddMethodDefinitionToContainingType(methodDefinition))
            {
                ReportError(DiagnosticIds.DuplicateMethod,
                    DiagnosticMessageTemplates.DuplicateMethod,
                    context);
            }

            return new(methodDefinition);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitMethodName(CILParser.MethodNameContext context) => VisitMethodName(context);

        public static GrammarResult.String VisitMethodName(CILParser.MethodNameContext context)
            => new(context.Value ?? string.Empty);

        internal string GetMethodName(IToken token) => token.Text;

        GrammarResult ICILVisitor<GrammarResult>.VisitOverrideDecl(CILParser.OverrideDeclContext context) => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        GrammarResult ICILVisitor<GrammarResult>.VisitParameterDecl(CILParser.ParameterDeclContext context) => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        public GrammarResult VisitScopeBlock(CILParser.ScopeBlockContext context) => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        public GrammarResult VisitSehBlock(CILParser.SehBlockContext context) => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        GrammarResult ICILVisitor<GrammarResult>.VisitSehClause(CILParser.SehClauseContext context) => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        GrammarResult ICILVisitor<GrammarResult>.VisitSehClauses(CILParser.SehClausesContext context) => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        GrammarResult ICILVisitor<GrammarResult>.VisitTryBlock(CILParser.TryBlockContext context) => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        GrammarResult ICILVisitor<GrammarResult>.VisitVtentryDecl(CILParser.VtentryDeclContext context) => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

#pragma warning restore CA1822 // Mark members as static
}
