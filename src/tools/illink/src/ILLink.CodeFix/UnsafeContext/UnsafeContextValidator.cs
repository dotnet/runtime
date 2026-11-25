// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.CodeFix.UnsafeContext
{
    /// <summary>
    /// Applies the current candidate of every region to the document and rebinds the result on one forked
    /// compilation. A member is valid when its diagnostics are unchanged except that the targeted unsafe-context
    /// errors are gone: no new errors or warnings (ref safety, definite assignment, scoping, overloads...) and no
    /// pre-existing diagnostic disappearing or changing severity, as an unsafe context may downgrade ref-safety errors.
    /// </summary>
    internal sealed class UnsafeContextValidator(
        Document document,
        SyntaxNode root,
        Compilation compilation,
        PlanningContext context,
        IReadOnlyDictionary<MemberDeclarationSyntax, ImmutableArray<Diagnostic>> baseline)
    {
        private readonly Dictionary<MemberDeclarationSyntax, SyntaxAnnotation> _memberAnnotations =
            baseline.Keys.ToDictionary(static member => member, static _ => new SyntaxAnnotation("UnsafeContextMember"));

        /// <summary>Applies the current edit of every region and returns the new root.</summary>
        public SyntaxNode Apply(IReadOnlyCollection<UnsafeContextRegion> regions)
        {
            SyntaxEditor editor = new(root, document.Project.Solution.Services);
            EditSession session = new(editor, context.Formatting, (CSharpParseOptions)root.SyntaxTree.Options);
            foreach (UnsafeContextRegion region in regions)
                region.CurrentEdit!.Apply(session, region.Annotation);

            // Registered last, so the callback sees the member with all of its edits.
            foreach (MemberDeclarationSyntax member in regions.Select(GetMember).Distinct())
                editor.ReplaceNode(member, (current, _) => current.WithAdditionalAnnotations(_memberAnnotations[member]));

            return editor.GetChangedRoot();
        }

        /// <summary>Returns the regions whose edits made their member's diagnostics differ unexpectedly.</summary>
        public HashSet<UnsafeContextRegion> Validate(SyntaxNode newRoot, IReadOnlyCollection<UnsafeContextRegion> regions)
        {
            SyntaxTree oldTree = root.SyntaxTree;
            SyntaxTree newTree = oldTree.WithRootAndOptions(newRoot, oldTree.Options);
            newRoot = newTree.GetRoot(context.CancellationToken);
            SemanticModel model = compilation.ReplaceSyntaxTree(oldTree, newTree).GetSemanticModel(newTree);

            HashSet<UnsafeContextRegion> failed = [];
            foreach (var group in regions.GroupBy(GetMember))
            {
                var member = newRoot.GetAnnotatedNodes(_memberAnnotations[group.Key]).OfType<MemberDeclarationSyntax>().FirstOrDefault();
                var spans = group.Select(r => (Region: r, Old: r.CurrentEdit!.Span, New: GetNewSpan(newRoot, r))).ToList();
                if (member is null || spans.Any(static s => s.New is null))
                {
                    failed.UnionWith(group);
                    continue;
                }

                List<RegionSpans> regionSpans = [.. spans.Select(static s => new RegionSpans(s.Region, s.Old, s.New!.Value))];
                ImmutableArray<Diagnostic> after = UnsafeContextSemantics.GetDiagnostics(model, member, context.CancellationToken);
                Compare(baseline[group.Key], after, regionSpans, failed);
            }

            return failed;
        }

        private static MemberDeclarationSyntax GetMember(UnsafeContextRegion region) => region.Targets[0].Scope.Member;

        // The union of the spans of the nodes the region's edit annotated.
        private static TextSpan? GetNewSpan(SyntaxNode root, UnsafeContextRegion region) =>
            root.GetAnnotatedNodes(region.Annotation).Aggregate((TextSpan?)null, static (span, node) =>
                span is { } current ? TextSpan.FromBounds(Math.Min(current.Start, node.SpanStart), Math.Max(current.End, node.Span.End)) : node.Span);

        /// <summary>Adds to <paramref name="failed"/> the regions blamed for leftover or changed diagnostics.</summary>
        private static void Compare(ImmutableArray<Diagnostic> before, ImmutableArray<Diagnostic> after, List<RegionSpans> regions, HashSet<UnsafeContextRegion> failed)
        {
            // An unsafe-context error left inside a region means its context did not cover the operation.
            foreach (Diagnostic diagnostic in after.Where(static d => UnsafeContextFacts.IsFixableDiagnosticId(d.Id)))
            {
                if (Innermost(regions, diagnostic.Location.SourceSpan, static r => r.New) is { } region)
                    failed.Add(region);
            }

            var oldByKey = before.Where(IsCompared).ToLookup(Key);
            var newByKey = after.Where(IsCompared).ToLookup(Key);
            foreach (var key in oldByKey.Select(static g => g.Key).Union(newByKey.Select(static g => g.Key)))
            {
                var oldDiagnostics = oldByKey[key].ToList();
                var newDiagnostics = newByKey[key].ToList();
                if (newDiagnostics.Count > oldDiagnostics.Count)
                    Blame(regions, newDiagnostics.Select(static d => d.Location.SourceSpan), static r => r.New, failed);
                else if (oldDiagnostics.Count > newDiagnostics.Count)
                    Blame(regions, oldDiagnostics.Select(static d => d.Location.SourceSpan), static r => r.Old, failed);
            }
        }

        /// <summary>
        /// Attributes changed diagnostics to the regions containing them; failing that, to the nearest preceding
        /// region (a narrowed scope affects later code), or to every region of the member.
        /// </summary>
        private static void Blame(List<RegionSpans> regions, IEnumerable<TextSpan> locations, Func<RegionSpans, TextSpan> span, HashSet<UnsafeContextRegion> failed)
        {
            List<TextSpan> spans = [.. locations];
            List<UnsafeContextRegion> containing = [.. spans.Select(l => Innermost(regions, l, span)).OfType<UnsafeContextRegion>()];
            if (containing.Count > 0)
            {
                failed.UnionWith(containing);
                return;
            }

            foreach (TextSpan location in spans)
            {
                // Only a block narrows scopes or splits declarations, so it is the likely cause of a later change.
                var preceding = regions
                    .Where(r => span(r).End <= location.Start)
                    .OrderByDescending(r => r.Region.CurrentEdit!.Form == UnsafeContextForm.Block)
                    .ThenByDescending(r => span(r).End)
                    .FirstOrDefault();
                if (preceding.Region is not null)
                    failed.Add(preceding.Region);
                else
                    failed.UnionWith(regions.Select(static r => r.Region));
            }
        }

        private static UnsafeContextRegion? Innermost(List<RegionSpans> regions, TextSpan location, Func<RegionSpans, TextSpan> span) =>
            regions.Where(r => span(r).Contains(location)).OrderBy(r => span(r).Length).FirstOrDefault().Region;

        // Only errors and warnings are compared; the fixable diagnostics are checked separately.
        private static bool IsCompared(Diagnostic diagnostic) =>
            diagnostic.Severity >= DiagnosticSeverity.Warning
            && !diagnostic.IsSuppressed
            && !UnsafeContextFacts.IsFixableDiagnosticId(diagnostic.Id);

        private static (string Id, DiagnosticSeverity Severity, string Message) Key(Diagnostic diagnostic) =>
            (diagnostic.Id, diagnostic.Severity, diagnostic.GetMessage(CultureInfo.InvariantCulture));

        /// <summary>A region's span before and after the edits.</summary>
        private readonly record struct RegionSpans(UnsafeContextRegion Region, TextSpan Old, TextSpan New);
    }
}
