// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.CodeFix.UnsafeContext
{
    /// <summary>The statements of a block or a switch section: the places a new unsafe block can be inserted into.</summary>
    internal sealed class StatementList(SyntaxNode owner, SyntaxList<StatementSyntax> statements)
    {
        public StatementList(BlockSyntax block) : this(block, block.Statements) { }

        public StatementList(SwitchSectionSyntax section) : this(section, section.Statements) { }

        public SyntaxNode Owner { get; } = owner;

        public SyntaxList<StatementSyntax> Statements { get; } = statements;

        public int Count => Statements.Count;

        public StatementSyntax this[int index] => Statements[index];

        /// <summary>The node spanning the declaration space of this list's locals: the whole switch for a section.</summary>
        public SyntaxNode ScopeNode => Owner is SwitchSectionSyntax { Parent: SwitchStatementSyntax switchStatement } ? switchStatement : Owner;

        /// <summary>
        /// The statements that may reference this list's locals, with their index here (-1 for other switch sections,
        /// which share one declaration space).
        /// </summary>
        public IEnumerable<(StatementSyntax Statement, int Index)> StatementsInScope =>
            ScopeNode is SwitchStatementSyntax switchStatement
                ? switchStatement.Sections.SelectMany(section => section.Statements.Select((s, i) => (s, section == Owner ? i : -1)))
                : Statements.Select(static (s, i) => (s, i));

        public int IndexOfStatementContaining(TextSpan span)
        {
            for (int i = 0; i < Statements.Count; i++)
            {
                if (Statements[i].FullSpan.Contains(span))
                    return i;
            }

            return -1;
        }

        public static StatementList? ForContainer(SyntaxNode container) =>
            container switch
            {
                BlockSyntax block => new StatementList(block),
                SwitchSectionSyntax section => new StatementList(section),
                _ => null,
            };
    }

    /// <summary>
    /// The spans in one scope that need an unsafe context: the diagnosed operations and the placeholder
    /// <c>unsafe(...)</c> expressions of earlier passes, ordered for fast containment queries.
    /// </summary>
    internal sealed class TargetSet(IEnumerable<TextSpan> spans)
    {
        private readonly ImmutableArray<TextSpan> _spans = [.. spans.OrderBy(static s => s.Start)];

        /// <summary>Whether an operation lies within <paramref name="span"/>.</summary>
        public bool AnyWithin(TextSpan span)
        {
            for (int i = FirstStartingAtOrAfter(span.Start); i < _spans.Length && _spans[i].Start < span.End; i++)
            {
                if (_spans[i].End <= span.End)
                    return true;
            }

            return false;
        }

        /// <summary>Whether an operation covers all of <paramref name="span"/>, e.g. an implicit enumeration.</summary>
        public bool AnyCovering(TextSpan span) => _spans.Any(s => s.Contains(span));

        private int FirstStartingAtOrAfter(int position)
        {
            int low = 0, high = _spans.Length;
            while (low < high)
            {
                int mid = (low + high) / 2;
                if (_spans[mid].Start < position)
                    low = mid + 1;
                else
                    high = mid;
            }

            return low;
        }
    }
}
