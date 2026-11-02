// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.CodeFix.UnsafeContext
{
    /// <summary>
    /// Related and unrelated leaves covered by a candidate block. Leaves are simple statements and compound-statement
    /// headers; a leaf is related when it contains an operation that needs the context.
    /// </summary>
    internal readonly record struct Coverage(int Related, int Unrelated)
    {
        /// <summary>The coverage rule: unrelated leaves do not outnumber related ones.</summary>
        public bool IsBalanced => Unrelated <= Related;

        public static Coverage Leaf(bool related) => related ? new(1, 0) : new(0, 1);

        public static Coverage operator +(Coverage left, Coverage right) =>
            new(left.Related + right.Related, left.Unrelated + right.Unrelated);
    }

    /// <summary>
    /// The header expressions a statement evaluates itself and the statement containers it runs. <c>else if</c> chains
    /// and stacked <c>fixed</c>/<c>using</c> statements are flattened, since no block may be inserted between their parts.
    /// </summary>
    /// <param name="Bodies">Blocks, switch sections or embedded statements.</param>
    internal readonly record struct StatementShape(ImmutableArray<SyntaxNode> Headers, ImmutableArray<SyntaxNode> Bodies)
    {
        public bool IsCompound => !Bodies.IsEmpty;

        public bool AnyBodyContains(TextSpan span) => Bodies.Any(b => BodyContains(b, span));

        /// <summary>Whether <paramref name="span"/> is in a body's statements; switch labels and guards are headers.</summary>
        public static bool BodyContains(SyntaxNode body, TextSpan span) =>
            body is SwitchSectionSyntax section
                ? section.Statements.Any(s => s.FullSpan.Contains(span))
                : body.Span.Contains(span);

        public static StatementSyntax Unlabel(StatementSyntax statement)
        {
            while (statement is LabeledStatementSyntax labeled)
                statement = labeled.Statement;

            return statement;
        }

        public static StatementShape Of(StatementSyntax statement) =>
            Unlabel(statement) switch
            {
                BlockSyntax block => new([], [block]),
                CheckedStatementSyntax checkedStatement => new([], [checkedStatement.Block]),
                UnsafeStatementSyntax unsafeStatement => new([], [unsafeStatement.Block]),
                IfStatementSyntax ifStatement => OfIfChain(ifStatement),
                WhileStatementSyntax whileStatement => new([whileStatement.Condition], [whileStatement.Statement]),
                DoStatementSyntax doStatement => new([doStatement.Condition], [doStatement.Statement]),
                ForStatementSyntax forStatement => new(GetForHeaders(forStatement), [forStatement.Statement]),
                CommonForEachStatementSyntax forEachStatement => new([forEachStatement.Expression], [forEachStatement.Statement]),
                LockStatementSyntax lockStatement => new([lockStatement.Expression], [lockStatement.Statement]),
                UsingStatementSyntax usingStatement => OfChain(usingStatement, static s => s is UsingStatementSyntax u ? ((SyntaxNode?)u.Declaration ?? u.Expression!, u.Statement) : null),
                FixedStatementSyntax fixedStatement => OfChain(fixedStatement, static s => s is FixedStatementSyntax f ? (f.Declaration, f.Statement) : null),
                SwitchStatementSyntax switchStatement => new(
                    [switchStatement.Expression, .. switchStatement.Sections.SelectMany(static s => s.Labels.OfType<CasePatternSwitchLabelSyntax>()).Select(static l => l.WhenClause).OfType<SyntaxNode>()],
                    [.. switchStatement.Sections]),
                TryStatementSyntax tryStatement => new(
                    [.. tryStatement.Catches.Select(static c => c.Filter).OfType<SyntaxNode>()],
                    [tryStatement.Block, .. tryStatement.Catches.Select(static c => c.Block), .. tryStatement.Finally is { } f ? [f.Block] : ImmutableArray<SyntaxNode>.Empty]),
                _ => new([], []),
            };

        /// <summary>Counts the leaves of <paramref name="statement"/> for the coverage rule.</summary>
        public static Coverage CountLeaves(StatementSyntax statement, TargetSet targets)
        {
            statement = Unlabel(statement);
            if (statement is LocalFunctionStatementSyntax)
                return default;

            StatementShape shape = Of(statement);
            if (!shape.IsCompound)
                return Coverage.Leaf(targets.AnyWithin(statement.Span));

            // A `for` header counts once; an operation covering the whole statement (e.g. enumeration) relates it all.
            bool coveredWhole = targets.AnyCovering(statement.Span);
            Coverage coverage = statement is ForStatementSyntax
                ? Coverage.Leaf(coveredWhole || shape.Headers.Any(h => targets.AnyWithin(h.Span)))
                : shape.Headers.Aggregate(default(Coverage), (sum, h) => sum + Coverage.Leaf(coveredWhole || targets.AnyWithin(h.Span)));

            if (coveredWhole && shape.Headers.IsEmpty)
                coverage += Coverage.Leaf(related: true);

            return shape.Bodies.Aggregate(coverage, (sum, body) => sum + CountBody(body, targets));
        }

        /// <summary>Counts the leaves of a body: a block's or section's statements, or an embedded statement.</summary>
        public static Coverage CountBody(SyntaxNode body, TargetSet targets) =>
            body switch
            {
                BlockSyntax block => CountLeaves(block.Statements, targets),
                SwitchSectionSyntax section => CountLeaves(section.Statements, targets),
                StatementSyntax statement => CountLeaves(statement, targets),
                _ => default,
            };

        // Takes the original list: a collection expression would re-create it, losing the statements' positions.
        private static Coverage CountLeaves(SyntaxList<StatementSyntax> statements, TargetSet targets) =>
            statements.Aggregate(default(Coverage), (sum, statement) => sum + CountLeaves(statement, targets));

        /// <summary>
        /// Whether the statement owns its operations directly: a simple statement, or one whose operations are in its
        /// headers or cover it whole (like an implicit enumeration), rather than only in its bodies.
        /// </summary>
        public static bool HasOwnOperations(StatementSyntax statement, TargetSet targets)
        {
            statement = Unlabel(statement);
            StatementShape shape = Of(statement);
            return !shape.IsCompound || targets.AnyCovering(statement.Span) || shape.Headers.Any(h => targets.AnyWithin(h.Span));
        }

        /// <summary>
        /// Whether covering the statement would cover its bodies only because a header needs a context. A consumer of
        /// the whole statement, such as an implicit enumeration, is exempt.
        /// </summary>
        public static bool CoversBodyOnlyForHeader(StatementSyntax statement, TargetSet targets)
        {
            statement = Unlabel(statement);
            StatementShape shape = Of(statement);
            if (!shape.IsCompound || targets.AnyCovering(statement.Span) || !shape.Headers.Any(h => targets.AnyWithin(h.Span)))
                return false;

            Coverage bodies = shape.Bodies.Aggregate(default(Coverage), (sum, body) => sum + CountBody(body, targets));
            return bodies.Related == 0 && bodies.Unrelated > 0;
        }

        private static StatementShape OfIfChain(IfStatementSyntax ifStatement)
        {
            List<SyntaxNode> headers = [], bodies = [];
            for (IfStatementSyntax? current = ifStatement; current is not null; current = current.Else?.Statement as IfStatementSyntax)
            {
                headers.Add(current.Condition);
                bodies.Add(current.Statement);
                if (current.Else is { Statement: not IfStatementSyntax } elseClause)
                    bodies.Add(elseClause.Statement);
            }

            return new([.. headers], [.. bodies]);
        }

        // Flattens stacked `using`/`fixed` statements into their headers and the innermost body.
        private static StatementShape OfChain(StatementSyntax statement, Func<StatementSyntax, (SyntaxNode Header, StatementSyntax Body)?> unfold)
        {
            List<SyntaxNode> headers = [];
            while (unfold(statement) is (var header, var body))
            {
                headers.Add(header);
                statement = body;
            }

            return new([.. headers], [statement]);
        }

        private static ImmutableArray<SyntaxNode> GetForHeaders(ForStatementSyntax forStatement) =>
            [
                .. forStatement.Declaration is { } declaration ? [declaration] : ImmutableArray<SyntaxNode>.Empty,
                .. forStatement.Initializers,
                .. forStatement.Condition is { } condition ? [condition] : ImmutableArray<SyntaxNode>.Empty,
                .. forStatement.Incrementors,
            ];
    }
}
