// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Aliases = System.Collections.Generic.ValueListBuilder<(string aliasName, string symbolName)>;

namespace Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

internal static partial class SyntaxValueProviderExtensions
{
    // Normal class as Runtime does not seem to support records currently.

    /// <summary>
    /// Information computed about a particular tree.  Cached so we don't repeatedly recompute this important
    /// information each time the incremental pipeline is rerun.
    /// </summary>
    private sealed class SyntaxTreeInfo : IEquatable<SyntaxTreeInfo>
    {
        public readonly SyntaxTree Tree;
        public readonly bool ContainsGlobalAliases;
        public readonly bool ContainsAttributeList;

        public SyntaxTreeInfo(SyntaxTree tree, bool containsGlobalAliases, bool containsAttributeList)
        {
            Tree = tree;
            ContainsGlobalAliases = containsGlobalAliases;
            ContainsAttributeList = containsAttributeList;
        }

        public bool Equals(SyntaxTreeInfo? other)
            => Tree == other?.Tree;

        public override bool Equals(object? obj)
            => this.Equals(obj as SyntaxTreeInfo);

        public override int GetHashCode()
            => Tree.GetHashCode();
    }

    /// <summary>
    /// Caching of syntax-tree to the info we've computed about it.  Used because compilations will have thousands of
    /// trees, and the incremental pipeline will get called back for *all* of them each time a compilation changes.  We
    /// do not want to continually recompute this data over and over again each time that happens given that normally
    /// only one tree will be different.  We also do not want to create an IncrementalValuesProvider that yield this
    /// information as that will mean we have a node in the tree that scales with the number of *all syntax trees*, not
    /// the number of *relevant syntax trees*. This can lead to huge memory churn keeping track of a high number of
    /// trees, most of which are not going to be relevant.
    /// </summary>
    private static readonly ConditionalWeakTable<SyntaxTree, SyntaxTreeInfo> s_treeToInfo = new ConditionalWeakTable<SyntaxTree, SyntaxTreeInfo>();

#if false
    // Not used in runtime.  Pooling is not a pattern here, and we use ValueListBuilder instead.

    private static readonly ObjectPool<Stack<string>> s_stackPool = new(static () => new());
#endif

    /// <summary>
    /// Returns all syntax nodes of that match <paramref name="predicate"/> if that node has an attribute on it that
    /// could possibly bind to the provided <paramref name="simpleName"/>. <paramref name="simpleName"/> should be the
    /// simple, non-qualified, name of the attribute, including the <c>Attribute</c> suffix, and not containing any
    /// generics, containing types, or namespaces.  For example <c>CLSCompliantAttribute</c> for <see
    /// cref="System.CLSCompliantAttribute"/>.
    /// <para/> This provider understands <see langword="using"/> (<c>Import</c> in Visual Basic) aliases and will find
    /// matches even when the attribute references an alias name.  For example, given:
    /// <code>
    /// using XAttribute = System.CLSCompliantAttribute;
    /// [X]
    /// class C { }
    /// </code>
    /// Then
    /// <c>context.SyntaxProvider.CreateSyntaxProviderForAttribute(nameof(CLSCompliantAttribute), (node, c) => node is ClassDeclarationSyntax)</c>
    /// will find the <c>C</c> class.
    /// </summary>
    /// <remarks>
    /// Note: a 'Values'-provider of arrays are returned.  Each array provides all the matching nodes from a single <see
    /// cref="SyntaxTree"/>.
    /// </remarks>
    public static IncrementalValuesProvider<(SyntaxTree tree, ImmutableArray<SyntaxNode> matches)> ForAttributeWithSimpleName(
        this SyntaxValueProvider @this,
        IncrementalGeneratorInitializationContext context,
        string simpleName,
        Func<SyntaxNode, CancellationToken, bool> predicate)
    {
        var syntaxHelper = CSharpSyntaxHelper.Instance;

        // Create a provider over all the syntax trees in the compilation.  This is better than CreateSyntaxProvider as
        // using SyntaxTrees is purely syntax and will not update the incremental node for a tree when another tree is
        // changed. CreateSyntaxProvider will have to rerun all incremental nodes since it passes along the
        // SemanticModel, and that model is updated whenever any tree changes (since it is tied to the compilation).
        var syntaxTreesProvider = context.CompilationProvider
            .SelectMany((compilation, cancellationToken) => compilation.SyntaxTrees
                .Select(tree => GetTreeInfo(tree, syntaxHelper, cancellationToken))
                .Where(info => info.ContainsGlobalAliases || info.ContainsAttributeList))
            /*.WithTrackingName("compilationUnit_ForAttribute")*/;

        // Create a provider that provides (and updates) the global aliases for any particular file when it is edited.
        var individualFileGlobalAliasesProvider = syntaxTreesProvider
            .Where(info => info.ContainsGlobalAliases)
            .Select((info, cancellationToken) => getGlobalAliasesInCompilationUnit(info.Tree.GetRoot(cancellationToken)))
            /*.WithTrackingName("individualFileGlobalAliases_ForAttribute")*/;

        // Create an aggregated view of all global aliases across all files.  This should only update when an individual
        // file changes its global aliases or a file is added / removed from the compilation
        var collectedGlobalAliasesProvider = individualFileGlobalAliasesProvider
            .Collect()
            .WithComparer(ImmutableArrayValueComparer<GlobalAliases>.Instance)
            /*.WithTrackingName("collectedGlobalAliases_ForAttribute")*/;

        var allUpGlobalAliasesProvider = collectedGlobalAliasesProvider
            .Select(static (arrays, _) => GlobalAliases.Create(arrays.SelectMany(a => a.AliasAndSymbolNames).ToImmutableArray()))
            /*.WithTrackingName("allUpGlobalAliases_ForAttribute")*/;

#if false

        // C# does not support global aliases from compilation options.  So we can just ignore this part.

        // Regenerate our data if the compilation options changed.  VB can supply global aliases with compilation options,
        // so we have to reanalyze everything if those changed.
        var compilationGlobalAliases = _context.CompilationOptionsProvider.Select(
            (o, _) =>
            {
                var aliases = Aliases.GetInstance();
                syntaxHelper.AddAliases(o, aliases);
                return GlobalAliases.Create(aliases.ToImmutableAndFree());
            }).WithTrackingName("compilationGlobalAliases_ForAttribute");

        allUpGlobalAliasesProvider = allUpGlobalAliasesProvider
            .Combine(compilationGlobalAliases)
            .Select((tuple, _) => GlobalAliases.Concat(tuple.Left, tuple.Right))
            .WithTrackingName("allUpIncludingCompilationGlobalAliases_ForAttribute");

#endif

        // Combine the two providers so that we reanalyze every file if the global aliases change, or we reanalyze a
        // particular file when it's compilation unit changes.
        var syntaxTreeAndGlobalAliasesProvider = syntaxTreesProvider
            .Where(info => info.ContainsAttributeList)
            .Combine(allUpGlobalAliasesProvider)
            /*.WithTrackingName("compilationUnitAndGlobalAliases_ForAttribute")*/;

        // For each pair of compilation unit + global aliases, walk the compilation unit
        var result = syntaxTreeAndGlobalAliasesProvider
            .Select((tuple, c) => (tuple.Left.Tree, GetMatchingNodes(syntaxHelper, tuple.Right, tuple.Left.Tree, simpleName, predicate, c)))
            .Where(tuple => tuple.Item2.Length > 0)
            /*.WithTrackingName("result_ForAttribute")*/;

        return result;

        static GlobalAliases getGlobalAliasesInCompilationUnit(
            SyntaxNode compilationUnit)
        {
            Debug.Assert(compilationUnit is ICompilationUnitSyntax);
            var globalAliases = new Aliases(Span<(string aliasName, string symbolName)>.Empty);

            CSharpSyntaxHelper.Instance.AddAliases(compilationUnit, ref globalAliases, global: true);

            return GlobalAliases.Create(globalAliases.AsSpan().ToImmutableArray());
        }
    }

    private static SyntaxTreeInfo GetTreeInfo(
        SyntaxTree tree, ISyntaxHelper syntaxHelper, CancellationToken cancellationToken)
    {
        // prevent captures for the case where the item is in the tree.
        return s_treeToInfo.TryGetValue(tree, out var info)
            ? info
            : computeTreeInfo();

        SyntaxTreeInfo computeTreeInfo()
        {
            var root = tree.GetRoot(cancellationToken);
            var containsGlobalAliases = syntaxHelper.ContainsGlobalAliases(root);
            var containsAttributeList = ContainsAttributeList(root);

            var info = new SyntaxTreeInfo(tree, containsGlobalAliases, containsAttributeList);
            return s_treeToInfo.GetValue(tree, _ => info);
        }
    }

    private static ImmutableArray<SyntaxNode> GetMatchingNodes(
        ISyntaxHelper syntaxHelper,
        GlobalAliases globalAliases,
        SyntaxTree syntaxTree,
        string name,
        Func<SyntaxNode, CancellationToken, bool> predicate,
        CancellationToken cancellationToken)
    {
        var compilationUnit = syntaxTree.GetRoot(cancellationToken);
        Debug.Assert(compilationUnit is ICompilationUnitSyntax);

        var isCaseSensitive = syntaxHelper.IsCaseSensitive;
        var comparison = isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        // As we walk down the compilation unit and nested namespaces, we may encounter additional using aliases local
        // to this file. Keep track of them so we can determine if they would allow an attribute in code to bind to the
        // attribute being searched for.
        var localAliases = new Aliases(Span<(string, string)>.Empty);
        var nameHasAttributeSuffix = name.HasAttributeSuffix(isCaseSensitive);

        // Used to ensure that as we recurse through alias names to see if they could bind to attributeName that we
        // don't get into cycles.

        var seenNames = new ValueListBuilder<string>(Span<string>.Empty);
        var results = new ValueListBuilder<SyntaxNode>(Span<SyntaxNode>.Empty);
        var attributeTargets = new ValueListBuilder<SyntaxNode>(Span<SyntaxNode>.Empty);

        try
        {
            processCompilationUnit(compilationUnit, ref localAliases, ref seenNames, ref results, ref attributeTargets);

            if (results.Length == 0)
                return ImmutableArray<SyntaxNode>.Empty;

            return results.AsSpan().ToArray().Distinct().ToImmutableArray();
        }
        finally
        {
            attributeTargets.Dispose();
            results.Dispose();
            seenNames.Dispose();
        }

        void processCompilationUnit(
            SyntaxNode compilationUnit,
            ref Aliases localAliases,
            ref ValueListBuilder<string> seenNames,
            ref ValueListBuilder<SyntaxNode> results,
            ref ValueListBuilder<SyntaxNode> attributeTargets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            syntaxHelper.AddAliases(compilationUnit, ref localAliases, global: false);

            processCompilationOrNamespaceMembers(compilationUnit, ref localAliases, ref seenNames, ref results, ref attributeTargets);
        }

        void processCompilationOrNamespaceMembers(
            SyntaxNode node,
            ref Aliases localAliases,
            ref ValueListBuilder<string> seenNames,
            ref ValueListBuilder<SyntaxNode> results,
            ref ValueListBuilder<SyntaxNode> attributeTargets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var child in node.ChildNodesAndTokens())
            {
                if (child.IsNode)
                {
                    var childNode = child.AsNode()!;
                    if (syntaxHelper.IsAnyNamespaceBlock(childNode))
                        processNamespaceBlock(childNode, ref localAliases, ref seenNames, ref results, ref attributeTargets);
                    else
                        processMember(childNode, ref localAliases, ref seenNames, ref results, ref attributeTargets);
                }
            }
        }

        void processNamespaceBlock(
            SyntaxNode namespaceBlock,
            ref Aliases localAliases,
            ref ValueListBuilder<string> seenNames,
            ref ValueListBuilder<SyntaxNode> results,
            ref ValueListBuilder<SyntaxNode> attributeTargets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var localAliasCount = localAliases.Length;
            syntaxHelper.AddAliases(namespaceBlock, ref localAliases, global: false);

            processCompilationOrNamespaceMembers(
                namespaceBlock, ref localAliases, ref seenNames, ref results, ref attributeTargets);

            // after recursing into this namespace, dump any local aliases we added from this namespace decl itself.
            localAliases.Length = localAliasCount;
        }

        void processMember(
            SyntaxNode member,
            ref Aliases localAliases,
            ref ValueListBuilder<string> seenNames,
            ref ValueListBuilder<SyntaxNode> results,
            ref ValueListBuilder<SyntaxNode> attributeTargets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // nodes can be arbitrarily deep.  Use an explicit stack over recursion to prevent a stack-overflow.
            var nodeStack = new ValueListBuilder<SyntaxNode>(Span<SyntaxNode>.Empty);
            nodeStack.Append(member);

            try
            {
                while (nodeStack.Length > 0)
                {
                    var node = nodeStack.Pop();

                    if (syntaxHelper.IsAttributeList(node))
                    {
                        foreach (var attribute in syntaxHelper.GetAttributesOfAttributeList(node))
                        {
                            // Have to lookup both with the name in the attribute, as well as adding the 'Attribute' suffix.
                            // e.g. if there is [X] then we have to lookup with X and with XAttribute.
                            var simpleAttributeName = syntaxHelper.GetUnqualifiedIdentifierOfName(
                                syntaxHelper.GetNameOfAttribute(attribute)).ValueText;
                            if (matchesAttributeName(ref localAliases, ref seenNames, simpleAttributeName, withAttributeSuffix: false) ||
                                matchesAttributeName(ref localAliases, ref seenNames, simpleAttributeName, withAttributeSuffix: true))
                            {
                                attributeTargets.Length = 0;
                                syntaxHelper.AddAttributeTargets(node, ref attributeTargets);

                                foreach (var target in attributeTargets.AsSpan())
                                {
                                    if (predicate(target, cancellationToken))
                                        results.Append(target);
                                }

                                break;
                            }
                        }

                        // attributes can't have attributes inside of them.  so no need to recurse when we're done.
                    }
                    else
                    {
                        // For any other node, just keep recursing deeper to see if we can find an attribute. Note: we cannot
                        // terminate the search anywhere as attributes may be found on things like local functions, and that
                        // means having to dive deep into statements and expressions.
                        var childNodesAndTokens = node.ChildNodesAndTokens();

                        // Avoid performance issue in ChildSyntaxList when iterating the child list in reverse
                        // (see https://github.com/dotnet/roslyn/issues/66475) by iterating forward first to
                        // ensure child nodes are realized.
                        foreach (var childNode in childNodesAndTokens)
                        {
                        }

                        foreach (var child in childNodesAndTokens.Reverse())
                        {
                            if (child.IsNode)
                                nodeStack.Append(child.AsNode()!);
                        }
                    }

                }
            }
            finally
            {
                nodeStack.Dispose();
            }
        }

        // Checks if `name` is equal to `matchAgainst`.  if `withAttributeSuffix` is true, then
        // will check if `name` + "Attribute" is equal to `matchAgainst`
        bool matchesName(string name, string matchAgainst, bool withAttributeSuffix)
        {
            if (withAttributeSuffix)
            {
                return name.Length + "Attribute".Length == matchAgainst.Length &&
                    matchAgainst.HasAttributeSuffix(isCaseSensitive) &&
                    matchAgainst.StartsWith(name, comparison);
            }
            else
            {
                return name.Equals(matchAgainst, comparison);
            }
        }

        bool matchesAttributeName(
            ref Aliases localAliases,
            ref ValueListBuilder<string> seenNames,
            string currentAttributeName,
            bool withAttributeSuffix)
        {
            // If the names match, we're done.
            if (withAttributeSuffix)
            {
                if (nameHasAttributeSuffix &&
                    matchesName(currentAttributeName, name, withAttributeSuffix))
                {
                    return true;
                }
            }
            else
            {
                if (matchesName(currentAttributeName, name, withAttributeSuffix: false))
                    return true;
            }

            // Otherwise, keep searching through aliases.  Check that this is the first time seeing this name so we
            // don't infinite recurse in error code where aliases reference each other.
            //
            // note: as we recurse up the aliases, we do not want to add the attribute suffix anymore.  aliases must
            // reference the actual real name of the symbol they are aliasing.
            foreach (var seenName in seenNames.AsSpan())
            {
                if (seenName == currentAttributeName)
                    return false;
            }

            seenNames.Append(currentAttributeName);

            foreach (var (aliasName, symbolName) in localAliases.AsSpan())
            {
                // see if user wrote `[SomeAlias]`.  If so, if we find a `using SomeAlias = ...` recurse using the
                // ... name portion to see if it might bind to the attr name the caller is searching for.
                if (matchesName(currentAttributeName, aliasName, withAttributeSuffix) &&
                    matchesAttributeName(ref localAliases, ref seenNames, symbolName, withAttributeSuffix: false))
                {
                    return true;
                }
            }

            foreach (var (aliasName, symbolName) in globalAliases.AliasAndSymbolNames)
            {
                if (matchesName(currentAttributeName, aliasName, withAttributeSuffix) &&
                    matchesAttributeName(ref localAliases, ref seenNames, symbolName, withAttributeSuffix: false))
                {
                    return true;
                }
            }

            seenNames.Pop();
            return false;
        }
    }

    private static bool ContainsAttributeList(SyntaxNode node)
    {
        if (node.IsKind(SyntaxKind.AttributeList))
            return true;

        foreach (SyntaxNodeOrToken child in node.ChildNodesAndTokens())
        {
            if (child.IsToken)
                continue;

            SyntaxNode? childNode = child.AsNode()!;
            if (ContainsAttributeList(childNode))
                return true;
        }

        return false;
    }
}
