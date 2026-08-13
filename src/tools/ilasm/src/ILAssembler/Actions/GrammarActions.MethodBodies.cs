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

        public GrammarResult VisitMethodDecls(CILParser.MethodDeclsContext context) => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        GrammarResult ICILVisitor<GrammarResult>.VisitMethodDecl(CILParser.MethodDeclContext context) => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

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
