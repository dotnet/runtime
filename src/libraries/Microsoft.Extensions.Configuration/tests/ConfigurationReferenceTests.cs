// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.Extensions.Configuration.Test
{
    public class ConfigurationReferenceTests
    {
        public enum RootKind
        {
            Builder,
            Manager,
        }

        public static IEnumerable<object[]> RootKinds() => new[]
        {
            new object[] { RootKind.Builder },
            new object[] { RootKind.Manager },
        };

        // === Opt-in ===

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void OptedInProvider_ResolvesReference(RootKind kind)
        {
            IConfigurationRoot root = BuildRoot(kind, Enabled(
                ("Shared:Credential", "secret"),
                ("Client:Credential", "ref(Shared:Credential)")));

            Assert.Equal("secret", root["Client:Credential"]);
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void NonOptedProvider_ReferenceIsLiteral(RootKind kind)
        {
            // Without ConfigurationReferences:Enabled the provider does not participate: a ref(...) value is verbatim.
            IConfigurationRoot root = BuildRoot(kind, Plain(
                ("Shared:Credential", "secret"),
                ("Client:Credential", "ref(Shared:Credential)")));

            Assert.Equal("ref(Shared:Credential)", root["Client:Credential"]);
            Assert.Null(root["Client:Credential:Anything"]);
        }

        [Fact]
        public void EnableKey_IsVisible()
        {
            IConfigurationRoot root = BuildRoot(RootKind.Builder, Enabled(("A", "b")));

            Assert.Equal("true", root["ConfigurationReferences:Enabled"]);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)] // RuntimeConfigurationOptions are not supported on .NET Framework.
        public void GloballyDisabled_ReferenceNotResolved()
        {
            var options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions.Add("Microsoft.Extensions.Configuration.DisableConfigurationReferences", bool.TrueString);

            using RemoteInvokeHandle handle = RemoteExecutor.Invoke(static () =>
            {
                IConfigurationRoot root = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConfigurationReferences:Enabled"] = "true",
                        ["Shared:Credential"] = "secret",
                        ["Client:Credential"] = "ref(Shared:Credential)",
                    })
                    .Build();

                // With references globally disabled, even an opted-in provider's marker is returned verbatim.
                Assert.Equal("ref(Shared:Credential)", root["Client:Credential"]);
            }, options);
        }

        // === Basic resolution ===

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void PlainValue_IsUnchanged(RootKind kind)
        {
            IConfigurationRoot root = BuildRoot(kind, Enabled(("Client:Credential", "literal")));

            Assert.Equal("literal", root["Client:Credential"]);
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void MissingTarget_ResolvesToNull(RootKind kind)
        {
            IConfigurationRoot root = BuildRoot(kind, Enabled(("Client:Credential", "ref(Missing)")));

            Assert.Null(root["Client:Credential"]);
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void Chain_ResolvesAcrossHops(RootKind kind)
        {
            IConfigurationRoot root = BuildRoot(kind, Enabled(
                ("A", "ref(B)"),
                ("B", "ref(C)"),
                ("C", "value")));

            Assert.Equal("value", root["A"]);
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void Reference_AliasIntoReferencedSubtree_ResolvesAcrossEntries(RootKind kind)
        {
            // A aliases its whole subtree to X, and X:B is itself a reference to Y, so reading A:B hops twice:
            // A:B -> X:B -> Y. This cross-entry chain cannot be pre-flattened (A:<suffix> is not an index key), so
            // resolution must iterate; a single redirect would stop at the literal "ref(Y)".
            IConfigurationRoot root = BuildRoot(kind, Enabled(
                ("A", "ref(X)"),
                ("X:B", "ref(Y)"),
                ("Y", "value")));

            Assert.Equal("value", root["A:B"]);
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void Reference_IsCaseInsensitive(RootKind kind)
        {
            IConfigurationRoot root = BuildRoot(kind, Enabled(
                ("Shared:Credential", "secret"),
                ("Client:Credential", "ref(SHARED:credential)")));

            Assert.Equal("secret", root["client:CREDENTIAL"]);
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void Reference_BodyIsTrimmed(RootKind kind)
        {
            // Whitespace around the target inside ref(...) is trimmed.
            IConfigurationRoot root = BuildRoot(kind, Enabled(
                ("Shared", "secret"),
                ("Client", "ref( Shared )")));

            Assert.Equal("secret", root["Client"]);
        }

        // === Subtree mirroring ===

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void Subtree_IsMirroredThroughSuffix(RootKind kind)
        {
            IConfigurationRoot root = BuildRoot(kind, Enabled(
                ("Shared:Credential:ClientId", "id-123"),
                ("Client:Credential", "ref(Shared:Credential)")));

            Assert.Equal("id-123", root["Client:Credential:ClientId"]);
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void Subtree_MirrorsGrandchildren(RootKind kind)
        {
            IConfigurationRoot root = BuildRoot(kind, Enabled(
                ("Shared:Credential:Nested:Leaf", "deep"),
                ("Client:Credential", "ref(Shared:Credential)")));

            Assert.Equal("deep", root["Client:Credential:Nested:Leaf"]);
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void Subtree_GetChildren_ListsTargetChildren_IgnoringDescendantKeys(RootKind kind)
        {
            // Client:Credential aliases to Shared:Credential, so its children are the target's children only; a key
            // defined under the reference (Client:Credential:Extra) is inside the alias and is not surfaced.
            IConfigurationRoot root = BuildRoot(kind, Enabled(
                ("Shared:Credential:ClientId", "id"),
                ("Client:Credential", "ref(Shared:Credential)"),
                ("Client:Credential:Extra", "own")));

            string[] children = root.GetSection("Client:Credential").GetChildren().Select(c => c.Key).OrderBy(k => k).ToArray();
            Assert.Equal(new[] { "ClientId" }, children);
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void Reference_ToSectionWithoutScalar_HasNullValueButMirrorsChildren(RootKind kind)
        {
            IConfigurationRoot root = BuildRoot(kind, Enabled(
                ("Shared:Credential:ClientId", "id"),
                ("Client:Credential", "ref(Shared:Credential)")));

            Assert.Null(root["Client:Credential"]);
            Assert.Equal(new[] { "ClientId" }, root.GetSection("Client:Credential").GetChildren().Select(c => c.Key).ToArray());
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void TryGetValue_ThroughReference_BehavesLikeTheTarget(RootKind kind)
        {
            // A reference is read like any other key once resolved, so TryGetValue reports the target's presence: a
            // reference to a value is found; a reference to a valueless section or to a missing key is not found, exactly
            // as reading the target directly would be.
            IConfigurationRoot root = BuildRoot(kind, Enabled(
                ("Shared:Scalar", "v"),
                ("Shared:Section:Child", "c"),
                ("RefToValue", "ref(Shared:Scalar)"),
                ("RefToSection", "ref(Shared:Section)"),
                ("RefToMissing", "ref(Nowhere)")));

            Assert.True(((ConfigurationSection)root.GetSection("RefToValue")).TryGetValue(null, out string? scalar));
            Assert.Equal("v", scalar);

            Assert.False(((ConfigurationSection)root.GetSection("RefToSection")).TryGetValue(null, out string? section));
            Assert.Null(section);

            Assert.False(((ConfigurationSection)root.GetSection("RefToMissing")).TryGetValue(null, out string? missing));
            Assert.Null(missing);
        }

        [Fact]
        public void Subtree_GetChildren_SuppressesChildrenShadowedByHigherReference()
        {
            // A:B:X in a lower provider is shadowed by the higher A:B=ref(E), which owns the subtree and does not define
            // X. GetChildren("A:B") must agree with the indexer (which resolves A:B:X to null), listing only C.
            IConfigurationRoot root = BuildLayered(
                Plain(("A:B:X", "5"), ("E:C:D", "2")),
                Enabled(("A:B", "ref(E)")));

            Assert.Null(root["A:B:X"]);
            Assert.Equal("2", root["A:B:C:D"]);
            Assert.Equal(new[] { "C" }, root.GetSection("A:B").GetChildren().Select(c => c.Key).ToArray());
        }

        [Fact]
        public void Subtree_GetChildren_LowerChildAlsoInMirror_AppearsOnceWithMirrorValue()
        {
            // A:B:X exists in a lower provider (shadowed) and in the mirror target E:X. It appears once and resolves to
            // the mirror's value, so enumeration and the indexer agree.
            IConfigurationRoot root = BuildLayered(
                Plain(("A:B:X", "low"), ("E:X", "fromE")),
                Enabled(("A:B", "ref(E)")));

            Assert.Equal("fromE", root["A:B:X"]);
            Assert.Equal(new[] { "X" }, root.GetSection("A:B").GetChildren().Select(c => c.Key).ToArray());
        }

        // === Cycle detection (no depth bound) ===

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void Cycle_Direct_Throws(RootKind kind)
        {
            IConfigurationRoot root = BuildRoot(kind, Enabled(
                ("A", "ref(B)"),
                ("B", "ref(A)")));

            Assert.Throws<System.InvalidOperationException>(() => _ = root["A"]);
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void Cycle_Self_Throws(RootKind kind)
        {
            IConfigurationRoot root = BuildRoot(kind, Enabled(("Loop", "ref(Loop)")));

            var ex = Assert.Throws<System.InvalidOperationException>(() => _ = root["Loop"]);
            Assert.Contains("Loop", ex.Message);
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void SelfReferential_TargetWithinOwnSubtree_Throws(RootKind kind)
        {
            // A reference whose target lies inside its own subtree would mirror forever (A -> A:B -> A:B:B -> ...),
            // producing ever-growing keys that no cycle set can catch; it is rejected structurally rather than hanging.
            IConfigurationRoot root = BuildRoot(kind, Enabled(("A", "ref(A:B)")));

            var ex = Assert.Throws<System.InvalidOperationException>(() => _ = root["A"]);
            Assert.Contains("A", ex.Message);
        }

        [Fact]
        public void Reference_SharingPrefixButNotAncestor_DoesNotThrow()
        {
            // AB is not within A's subtree (no ':' boundary), so A=ref(AB) is a normal reference, not self-referential.
            IConfigurationRoot root = BuildRoot(RootKind.Builder, Enabled(
                ("A", "ref(AB)"),
                ("AB", "value")));

            Assert.Equal("value", root["A"]);
        }

        [Fact]
        public void Cycle_Indirect_MessageNamesTheLoop()
        {
            IConfigurationRoot root = BuildRoot(RootKind.Builder, Enabled(
                ("A", "ref(B)"),
                ("B", "ref(C)"),
                ("C", "ref(A)")));

            var ex = Assert.Throws<System.InvalidOperationException>(() => _ = root["A"]);
            Assert.Contains("A", ex.Message);
            Assert.Contains("B", ex.Message);
            Assert.Contains("C", ex.Message);
            Assert.Contains("->", ex.Message);
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void Cycle_GrowingIndirect_Throws(RootKind kind)
        {
            // A -> B with B -> A:C never resolves and never repeats a key: A:x mirrors to B:x to A:C:x to B:C:x to
            // A:C:C:x, growing without bound. A repeat-based guard cannot catch it, but the reference graph does
            // (A -> B -> A), so it is rejected rather than looped on.
            IConfigurationRoot root = BuildRoot(kind, Enabled(
                ("A", "ref(B)"),
                ("B", "ref(A:C)")));

            Assert.Throws<System.InvalidOperationException>(() => _ = root["A"]);
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void Cycle_CrossSuffix_Growing_Throws(RootKind kind)
        {
            // The cycle only forms once a suffix is appended: A aliases to B, and B:C references back under A. Reading
            // A:C mirrors A:C -> B:C -> A:C:D -> B:C:D -> A:C:D:D..., growing without bound. The first hop's target (B)
            // is not itself a reference key, so a successor-graph that inspects only targets misses it; flattening the
            // real chain (with suffixes) catches the key that redirects twice.
            IConfigurationRoot root = BuildRoot(kind, Enabled(
                ("A", "ref(B)"),
                ("B:C", "ref(A:C:D)")));

            Assert.Throws<System.InvalidOperationException>(() => _ = root["A:C"]);
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void Cycle_CrossSuffix_Repeating_Throws(RootKind kind)
        {
            // A aliases to X, and X:B references A:B, so A:B mirrors A:B -> X:B -> A:B forever. Again the first hop's
            // target (X) is not a reference key, so it is only caught by walking the real chain.
            IConfigurationRoot root = BuildRoot(kind, Enabled(
                ("A", "ref(X)"),
                ("X:B", "ref(A:B)")));

            Assert.Throws<System.InvalidOperationException>(() => _ = root["A:B"]);
        }

        [Fact]
        public void Cycle_DoesNotAffectUnrelatedKeys_ButThrowsWhenReadInto()
        {
            // Cycles are detected by the resolution walk, not up front, so a cycle only surfaces when a read actually
            // walks into it. An unrelated key - and an unrelated healthy reference - resolve normally even though a
            // cycle exists elsewhere; only a read that walks into the cycle throws.
            IConfigurationRoot root = BuildRoot(RootKind.Builder, Enabled(
                ("Unrelated", "value"),
                ("Healthy", "ref(Unrelated)"),
                ("A", "ref(B)"),
                ("B", "ref(A)")));

            Assert.Equal("value", root["Unrelated"]);
            Assert.Equal("value", root["Healthy"]);
            Assert.Throws<System.InvalidOperationException>(() => _ = root["A"]);
        }

        [Fact]
        public void Cycle_GetChildren_Throws()
        {
            // A section-reference cycle is caught during enumeration (thrown, not looped forever).
            IConfigurationRoot root = BuildRoot(RootKind.Builder, Enabled(
                ("A:Sub", "x"),
                ("A", "ref(B)"),
                ("B", "ref(A)")));

            Assert.Throws<System.InvalidOperationException>(() => root.GetSection("A").GetChildren().ToList());
        }

        [Fact]
        public void DeepChain_ResolvesWithoutDepthLimit()
        {
            // No depth cap: a legitimate chain far longer than any old bound must still resolve.
            var data = new Dictionary<string, string?>(System.StringComparer.OrdinalIgnoreCase)
            {
                [ConfigurationReferencesEnabledKey] = "true",
            };
            const int Hops = 500;
            for (int i = 0; i < Hops; i++)
            {
                data[$"R{i}"] = $"ref(R{i + 1})";
            }
            data[$"R{Hops}"] = "final";

            IConfigurationRoot root = new ConfigurationBuilder()
                .Add(new MemoryConfigurationSource { InitialData = data })
                .Build();

            Assert.Equal("final", root["R0"]);
        }

        [Fact]
        public void DeepChain_GetChildren_DoesNotOverflowStack()
        {
            // Enumeration follows the mirror chain iteratively, so a deep (finite) chain of section references must not
            // overflow the stack: R0=ref(R1), ..., R{N-1}=ref(R{N}); R{N} has a child.
            var data = new Dictionary<string, string?>(System.StringComparer.OrdinalIgnoreCase)
            {
                [ConfigurationReferencesEnabledKey] = "true",
            };
            const int Hops = 5000;
            for (int i = 0; i < Hops; i++)
            {
                data[$"R{i}"] = $"ref(R{i + 1})";
            }
            data[$"R{Hops}:Leaf"] = "deep";

            IConfigurationRoot root = new ConfigurationBuilder()
                .Add(new MemoryConfigurationSource { InitialData = data })
                .Build();

            Assert.Equal(new[] { "Leaf" }, root.GetSection("R0").GetChildren().Select(c => c.Key).ToArray());
        }

        // === Scope: a reference resolves against the effective (fully merged) configuration ===

        [Fact]
        public void Reference_ResolvesToEffectiveValue_TargetInLowerProvider()
        {
            // Client=ref(Shared) in the higher provider; Shared is in the lower provider.
            IConfigurationRoot root = BuildLayered(
                Plain(("Shared", "secret")),
                Enabled(("Client", "ref(Shared)")));

            Assert.Equal("secret", root["Client"]);
        }

        [Fact]
        public void Reference_ResolvesToEffectiveValue_TargetInHigherProvider()
        {
            // Client=ref(Shared) in the lower provider; Shared is only in the higher provider. A reference resolves
            // against the effective configuration, so it still sees the higher Shared.
            IConfigurationRoot root = BuildLayered(
                Enabled(("Client", "ref(Shared)")),
                Plain(("Shared", "secret")));

            Assert.Equal("secret", root["Client"]);
        }

        [Fact]
        public void Reference_PicksUpHigherProviderOverrideOfTarget()
        {
            // The base + environment-override pattern: a reference declared in the base file picks up a higher
            // provider's override of its target (ref(X) == reading X).
            IConfigurationRoot root = BuildLayered(
                Enabled(("Shared:Conn", "dev"), ("Db:Conn", "ref(Shared:Conn)")),
                Plain(("Shared:Conn", "prod")));

            Assert.Equal("prod", root["Db:Conn"]);
        }

        // === Redirection: shallowest reference wins; a ref key still respects provider precedence ===

        [Fact]
        public void HigherAncestorReference_OverridesLowerDescendantValue()
        {
            IConfigurationRoot root = BuildLayered(
                Plain(("A:B:C:D", "1"), ("E:C:D", "2")),
                Enabled(("A:B", "ref(E)")));

            Assert.Equal("2", root["A:B:C:D"]);
        }

        [Fact]
        public void ShallowestReferenceOnPathWins_NestedReferenceIgnored()
        {
            // Both A and A:B are references; the shallower A wins and redirects the whole subtree, so A:B:C reads X:B:C.
            // The nested A:B reference is inside A's alias and is ignored.
            IConfigurationRoot root = BuildRoot(RootKind.Builder, Enabled(
                ("A", "ref(X)"),
                ("A:B", "ref(Y)"),
                ("X:B:C", "fromX"),
                ("Y:C", "fromY")));

            Assert.Equal("fromX", root["A:B:C"]);
        }

        [Fact]
        public void Reference_HigherDescendantValue_OverridesMirror()
        {
            // A:B aliases its subtree to E, but a provider above the reference overrides the individual key A:B:C:D,
            // which wins over the mirrored E:C:D (recursive merge patches keys inside the referenced subtree).
            IConfigurationRoot root = BuildLayered(
                Enabled(("A:B", "ref(E)"), ("E:C:D", "2")),
                Plain(("A:B:C:D", "1")));

            Assert.Equal("1", root["A:B:C:D"]);
        }

        [Fact]
        public void HigherReference_MirrorAbsent_ResolvesToNull()
        {
            // The fallback is dropped: a higher reference owns its subtree, so a descendant the target does not define
            // resolves to null rather than surfacing the lower value.
            IConfigurationRoot root = BuildLayered(
                Plain(("A:B:X", "5"), ("E:C:D", "2")),
                Enabled(("A:B", "ref(E)")));

            Assert.Equal("2", root["A:B:C:D"]);
            Assert.Null(root["A:B:X"]);
        }

        [Fact]
        public void Reference_RedirectsSubtree_IgnoringSameProviderDescendantValue()
        {
            // Even within one provider, A:B redirects the whole subtree to E; the direct A:B:C:D is inside the alias and
            // is ignored.
            IConfigurationRoot root = BuildRoot(RootKind.Builder, Enabled(
                ("E:C:D", "2"),
                ("A:B", "ref(E)"),
                ("A:B:C:D", "1")));

            Assert.Equal("2", root["A:B:C:D"]);
        }

        [Fact]
        public void HigherNonOptedLiteral_ShadowsLowerReferenceAndSubtree()
        {
            // A lower opted-in provider makes A:B a reference to section E; a higher non-opted provider overrides A:B
            // with a literal. The literal wins ("later set wins") and, being a scalar, suppresses the mirrored subtree.
            IConfigurationRoot root = BuildLayered(
                Enabled(("A:B", "ref(E)"), ("E:X", "5"), ("E:Y", "6")),
                Plain(("A:B", "literal")));

            Assert.Equal("literal", root["A:B"]);
            Assert.Null(root["A:B:X"]);
            Assert.Empty(root.GetSection("A:B").GetChildren());
        }

        [Fact]
        public void Reference_HigherDescendantOverride_WinsWhileSiblingsMirror()
        {
            // A provider above the reference overrides one descendant (A:B:X); that key wins over the mirror while its
            // siblings still mirror the target: A:B:X reads the override, A:B:Y reads E:Y.
            IConfigurationRoot root = BuildLayered(
                Enabled(("A:B", "ref(E)"), ("E:X", "5"), ("E:Y", "6")),
                Plain(("A:B:X", "override")));

            Assert.Equal("override", root["A:B:X"]);
            Assert.Equal("6", root["A:B:Y"]);
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void HigherDescendantReference_WinsOverLowerAncestorReference(RootKind kind)
        {
            // A lower opted-in provider aliases the whole A subtree to X, but a higher opted-in provider declares a
            // more specific reference at A:B. Provider precedence beats prefix depth, so the A:B subtree resolves
            // through the higher A:B -> Y reference, not the lower A -> X one. Reading A:B must resolve Y's value (not
            // leak the raw "ref(Y)" marker), and A:B:C must mirror Y:C rather than X:B:C.
            IConfigurationRoot root = BuildRoot(kind, new[]
            {
                Enabled(("A", "ref(X)"), ("X:B:C", "lower")),
                Enabled(("A:B", "ref(Y)"), ("Y", "yval"), ("Y:C", "higher")),
            });

            Assert.Equal("yval", root["A:B"]);
            Assert.Equal("higher", root["A:B:C"]);
            Assert.Equal(new[] { "C" }, root.GetSection("A:B").GetChildren().Select(c => c.Key).ToArray());
        }

        // === Recursive merge: overrides apply at every hop of a chain ===

        [Theory]
        [InlineData("X:Y:A:B:C", "1")] // patch on the outermost reference's subtree
        [InlineData("X:Y:A:B:F", "2")] // patch on the interior reference (D)'s subtree - only recursive merge sees this
        [InlineData("X:Y:A:B:I", "3")] // no patch: mirrors the final target G
        public void RecursiveMerge_PatchesEveryHop(string key, string expected)
        {
            // Chain X:Y:A -> D -> G, where D is itself a reference. A higher provider patches a key on the outermost
            // subtree (X:Y:A:B:C) and one on the interior subtree (D:B:F). Recursive merge honours both; the untouched
            // path mirrors G.
            IConfigurationRoot root = BuildLayered(
                Enabled(("X:Y:A", "ref(D)"), ("D", "ref(G)"), ("G:B:I", "3")),
                Plain(("X:Y:A:B:C", "1"), ("D:B:F", "2")));

            Assert.Equal(expected, root[key]);
        }

        [Fact]
        public void RecursiveMerge_GetChildren_UnionsOverridesAtEveryHop()
        {
            // GetChildren agrees with the reads: the children of X:Y:A:B are the outermost patch (C), the interior patch
            // (F) and the mirrored target key (I).
            IConfigurationRoot root = BuildLayered(
                Enabled(("X:Y:A", "ref(D)"), ("D", "ref(G)"), ("G:B:I", "3")),
                Plain(("X:Y:A:B:C", "1"), ("D:B:F", "2")));

            Assert.Equal(new[] { "C", "F", "I" },
                root.GetSection("X:Y:A:B").GetChildren().Select(c => c.Key).OrderBy(k => k).ToArray());
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void RecursiveMerge_GetChildren_SortsMergedChildrenByConfigurationKeyComparer(RootKind kind)
        {
            // The children of a resolved section are a union of a higher provider's overrides and the mirrored target's
            // children, each block arriving sorted on its own. GetChildren must return the union in
            // ConfigurationKeyComparer order (numeric-aware: "2" before "10"), not in block-concatenation order.
            IConfigurationRoot root = BuildRoot(kind, new[]
            {
                Enabled(("A", "ref(Target)"), ("Target:2", "two")),
                Plain(("A:10", "ten")),
            });

            Assert.Equal(new[] { "2", "10" }, root.GetSection("A").GetChildren().Select(c => c.Key).ToArray());
            Assert.Equal("two", root["A:2"]);
            Assert.Equal("ten", root["A:10"]);
        }

        [Fact]
        public void RecursiveMerge_AliasMirrorsTargetFaithfully()
        {
            // X:Y:A aliases D, so reading through X:Y:A matches reading D directly, including a higher provider's patch
            // that lands inside the interior reference D. (Under a single-level merge these would diverge.)
            IConfigurationRoot root = BuildLayered(
                Enabled(("X:Y:A", "ref(D)"), ("D", "ref(G)"), ("G:B:I", "3")),
                Plain(("D:B:F", "2")));

            Assert.Equal("2", root["D:B:F"]);
            Assert.Equal(root["D:B:F"], root["X:Y:A:B:F"]);
        }

        // === Reload ===

        [Fact]
        public void Reload_ReflectsChangedTargetValue_AndFiresToken()
        {
            var source = new ReloadableMemorySource
            {
                InitialData = new[]
                {
                    new KeyValuePair<string, string?>(ConfigurationReferencesEnabledKey, "true"),
                    new KeyValuePair<string, string?>("Shared:Credential", "old"),
                    new KeyValuePair<string, string?>("Client:Credential", "ref(Shared:Credential)"),
                }
            };
            var builder = new ConfigurationBuilder();
            builder.Add(source);
            IConfigurationRoot root = builder.Build();
            Assert.Equal("old", root["Client:Credential"]);

            bool fired = false;
            ChangeToken.OnChange(root.GetReloadToken, () => fired = true);

            source.Built!.Set("Shared:Credential", "new");
            source.Built!.TriggerReload();

            Assert.True(fired);
            Assert.Equal("new", root["Client:Credential"]);
        }

        // === ConfigurationManager ===

        [Fact]
        public void Manager_IndexerSetsLiteral_ResolvedOnRead()
        {
            using var manager = new ConfigurationManager();

            manager["ConfigurationReferences:Enabled"] = "true";
            manager["Shared:Credential"] = "secret";
            manager["Client:Credential"] = "ref(Shared:Credential)";

            Assert.Equal("secret", manager["Client:Credential"]);
        }

        [Fact]
        public void Manager_IndexerEnablesReferencesAfterRead_TakesEffect()
        {
            using var manager = new ConfigurationManager();
            manager["Plain"] = "x";
            _ = manager["Plain"]; // prime the cache while references are disabled (caches "no provider opted in")

            manager["ConfigurationReferences:Enabled"] = "true";
            manager["Shared"] = "secret";
            manager["Client"] = "ref(Shared)";

            Assert.Equal("secret", manager["Client"]);
        }

        [Fact]
        public void Root_IndexerChangesReferenceTarget_TakesEffect()
        {
            IConfigurationRoot root = BuildRoot(RootKind.Builder, Enabled(
                ("A", "ref(B)"),
                ("B", "vB"),
                ("C", "vC")));
            Assert.Equal("vB", root["A"]); // prime the index (A -> B)

            root["A"] = "ref(C)";

            Assert.Equal("vC", root["A"]);
        }

        [Fact]
        public void Manager_ConcurrentSourceAddWhileReadingReferences_DoesNotThrow()
        {
            using var manager = new ConfigurationManager();
            var builder = (IConfigurationBuilder)manager;

            // Enable references and warm the cache so the opted-in flags are memoised at the current provider count.
            builder.Add(Enabled(("Shared:Credential", "secret"), ("Client:Credential", "ref(Shared:Credential)")));
            Assert.Equal("secret", manager["Client:Credential"]);

            System.Exception? failure = null;
            bool done = false;
            using var started = new ManualResetEventSlim(false);

            var reader = new Thread(() =>
            {
                started.Set();
                try
                {
                    while (!Volatile.Read(ref done))
                    {
                        // "Probe" is redefined by every added provider, so its winning provider is a high index - the
                        // exact shape that paired an old (short) opted-in array with a longer provider snapshot and threw
                        // IndexOutOfRangeException before the cache was bound to the pinned provider generation.
                        _ = manager["Probe"];
                        _ = manager["Client:Credential"];
                    }
                }
                catch (System.Exception ex)
                {
                    failure = ex;
                }
            });

            reader.Start();
            started.Wait();

            for (int i = 0; i < 300; i++)
            {
                builder.Add(Plain(("Probe", "v" + i)));
            }

            Volatile.Write(ref done, true);
            reader.Join();

            Assert.Null(failure);
            Assert.Equal("secret", manager["Client:Credential"]);
        }

        [Fact]
        public void Manager_ReadAfterDispose_WithReferences_DoesNotThrow()
        {
            // Reading a ConfigurationManager after it is disposed is deliberately tolerated (see
            // DisposedReferenceCountedProviders): ConfigurationSection.TryGetValue skips a provider that throws
            // ObjectDisposedException and returns false. Building the reference index scans providers too, so it must
            // tolerate the same rather than letting the exception escape that read.
            var manager = new ConfigurationManager();
            ((IConfigurationBuilder)manager).Add(new ThrowOnDisposeSource(Dict(optIn: true, new (string, string?)[]
            {
                ("Shared:Credential", "secret"),
                ("Client:Credential", "ref(Shared:Credential)"),
            })));

            var section = new ConfigurationSection(manager, "Client");
            Assert.True(section.TryGetValue("Credential", out string? value));
            Assert.Equal("secret", value);

            manager.Dispose();

            Assert.False(section.TryGetValue("Credential", out value));
            Assert.Null(value);
        }

        [Fact]
        public void NonScannableOptedProvider_ReferenceResolves()
        {
            IConfigurationRoot root = BuildLayered(
                Unscannable(
                    ("ConfigurationReferences:Enabled", "true"),
                    ("Shared:Credential", "secret"),
                    ("Client:Credential", "ref(Shared:Credential)")));

            Assert.Equal("secret", root["Client:Credential"]);
        }

        [Fact]
        public void Reference_TargetInChainedConfiguration_Resolves()
        {
            // A chained (AddConfiguration) provider is a plain value source to the outer engine, so an outer reference
            // can target its keys.
            IConfigurationRoot inner = new ConfigurationBuilder()
                .Add(Plain(("Shared:Conn", "dev")))
                .Build();

            IConfigurationRoot outer = new ConfigurationBuilder()
                .AddConfiguration(inner)
                .Add(Enabled(("Db:Conn", "ref(Shared:Conn)")))
                .Build();

            Assert.Equal("dev", outer["Db:Conn"]);
        }

        [Fact]
        public void Reference_InChainedConfiguration_IsNotResolvedByOuterEngine()
        {
            // The chained config opts in (its second provider sets Enabled), but the ref(...) value lives in a
            // different, non-opted inner provider, so the inner config keeps it a literal. The outer engine sees only
            // the chained provider's merged view and cannot tell which inner provider each key came from, so it skips
            // the chained provider rather than misreading that literal as a reference.
            IConfigurationRoot inner = new ConfigurationBuilder()
                .Add(Plain(("Literal", "ref(Target)")))
                .Add(Enabled())
                .Build();

            Assert.Equal("ref(Target)", inner["Literal"]);

            IConfigurationRoot outer = new ConfigurationBuilder()
                .AddConfiguration(inner)
                .Build();

            Assert.Equal("ref(Target)", outer["Literal"]);
        }

        // === Third-party root ===

        [Fact]
        public void GetChildren_ThirdPartyRoot_EnumeratesWithoutResolving()
        {
            // A root that is neither ConfigurationRoot nor ConfigurationManager exposes no reference engine, so there is
            // nothing to resolve against. GetChildren must still enumerate the section's children (references simply do
            // not apply to such a root) rather than dereferencing a null engine.
            IConfigurationRoot inner = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Parent:ChildA"] = "1",
                    ["Parent:ChildB"] = "2",
                })
                .Build();

            IConfigurationSection section = new ConfigurationSection(new ThirdPartyRoot(inner), "Parent");

            string[] children = section.GetChildren().Select(c => c.Key).OrderBy(k => k).ToArray();
            Assert.Equal(new[] { "ChildA", "ChildB" }, children);
        }

        // === Helpers ===

        internal const string ConfigurationReferencesEnabledKey = "ConfigurationReferences:Enabled";

        private static IConfigurationRoot BuildRoot(RootKind kind, IConfigurationSource source) => BuildRoot(kind, new[] { source });

        private static IConfigurationRoot BuildRoot(RootKind kind, IConfigurationSource[] sources)
        {
            if (kind == RootKind.Builder)
            {
                var builder = new ConfigurationBuilder();
                foreach (IConfigurationSource source in sources)
                {
                    builder.Add(source);
                }
                return builder.Build();
            }

            var manager = new ConfigurationManager();
            foreach (IConfigurationSource source in sources)
            {
                ((IConfigurationBuilder)manager).Add(source);
            }
            return manager;
        }

        private static IConfigurationRoot BuildLayered(params IConfigurationSource[] sources)
        {
            var builder = new ConfigurationBuilder();
            foreach (IConfigurationSource source in sources)
            {
                builder.Add(source);
            }
            return builder.Build();
        }

        private static IConfigurationSource Enabled(params (string Key, string? Value)[] entries)
            => new MemoryConfigurationSource { InitialData = Dict(optIn: true, entries) };

        private static IConfigurationSource Plain(params (string Key, string? Value)[] entries)
            => new MemoryConfigurationSource { InitialData = Dict(optIn: false, entries) };

        private static IConfigurationSource Unscannable(params (string Key, string? Value)[] entries)
            => new UnscannableSource(Dict(optIn: false, entries));

        private static IDictionary<string, string?> Dict(bool optIn, (string Key, string? Value)[] entries)
        {
            var dictionary = new Dictionary<string, string?>(System.StringComparer.OrdinalIgnoreCase);
            if (optIn)
            {
                dictionary[ConfigurationReferencesEnabledKey] = "true";
            }
            foreach ((string key, string? value) in entries)
            {
                dictionary[key] = value;
            }
            return dictionary;
        }

        // A provider that implements IConfigurationProvider directly (not via ConfigurationProvider), so the reference
        // engine must scan it via GetChildKeys rather than its loaded dictionary.
        private sealed class UnscannableSource : IConfigurationSource
        {
            private readonly IDictionary<string, string?> _data;

            public UnscannableSource(IDictionary<string, string?> data) => _data = data;

            public IConfigurationProvider Build(IConfigurationBuilder builder) => new UnscannableProvider(_data);
        }

        private sealed class UnscannableProvider : IConfigurationProvider
        {
            private readonly Dictionary<string, string?> _data;
            private readonly ConfigurationReloadToken _reloadToken = new();

            public UnscannableProvider(IDictionary<string, string?> data)
                => _data = new Dictionary<string, string?>(data, System.StringComparer.OrdinalIgnoreCase);

            public bool TryGet(string key, out string? value) => _data.TryGetValue(key, out value);

            public void Set(string key, string? value) => _data[key] = value;

            public IChangeToken GetReloadToken() => _reloadToken;

            public void Load() { }

            public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string? parentPath)
            {
                string prefix = parentPath is null ? string.Empty : parentPath + ConfigurationPath.KeyDelimiter;
                var results = new List<string>(earlierKeys);
                foreach (KeyValuePair<string, string?> entry in _data)
                {
                    if (entry.Key.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                    {
                        string rest = entry.Key.Substring(prefix.Length);
                        int delimiter = rest.IndexOf(ConfigurationPath.KeyDelimiter[0]);
                        results.Add(delimiter < 0 ? rest : rest.Substring(0, delimiter));
                    }
                }
                return results;
            }
        }

        private sealed class ReloadableMemoryProvider : MemoryConfigurationProvider
        {
            public ReloadableMemoryProvider(MemoryConfigurationSource source) : base(source) { }

            public void TriggerReload() => OnReload();
        }

        // A provider that throws ObjectDisposedException from every read once disposed, to model a provider that holds
        // native/OS resources. Used to check that reading a disposed ConfigurationManager tolerates such a provider.
        private sealed class ThrowOnDisposeSource : IConfigurationSource
        {
            private readonly IDictionary<string, string?> _data;

            public ThrowOnDisposeSource(IDictionary<string, string?> data) => _data = data;

            public IConfigurationProvider Build(IConfigurationBuilder builder) => new ThrowOnDisposeProvider(_data);
        }

        private sealed class ThrowOnDisposeProvider : IConfigurationProvider, System.IDisposable
        {
            private readonly Dictionary<string, string?> _data;
            private readonly ConfigurationReloadToken _reloadToken = new();
            private bool _disposed;

            public ThrowOnDisposeProvider(IDictionary<string, string?> data)
                => _data = new Dictionary<string, string?>(data, System.StringComparer.OrdinalIgnoreCase);

            public void Dispose() => _disposed = true;

            public bool TryGet(string key, out string? value)
            {
                ThrowIfDisposed();
                return _data.TryGetValue(key, out value);
            }

            public void Set(string key, string? value)
            {
                ThrowIfDisposed();
                _data[key] = value;
            }

            public IChangeToken GetReloadToken() => _reloadToken;

            public void Load() { }

            public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string? parentPath)
            {
                ThrowIfDisposed();
                string prefix = parentPath is null ? string.Empty : parentPath + ConfigurationPath.KeyDelimiter;
                var results = new List<string>(earlierKeys);
                foreach (KeyValuePair<string, string?> entry in _data)
                {
                    if (entry.Key.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                    {
                        string rest = entry.Key.Substring(prefix.Length);
                        int delimiter = rest.IndexOf(ConfigurationPath.KeyDelimiter[0]);
                        results.Add(delimiter < 0 ? rest : rest.Substring(0, delimiter));
                    }
                }
                return results;
            }

            private void ThrowIfDisposed()
            {
                if (_disposed)
                {
                    throw new System.ObjectDisposedException(nameof(ThrowOnDisposeProvider));
                }
            }
        }

        private sealed class ReloadableMemorySource : MemoryConfigurationSource, IConfigurationSource
        {
            public ReloadableMemoryProvider? Built { get; private set; }

            public new IConfigurationProvider Build(IConfigurationBuilder builder)
            {
                Built = new ReloadableMemoryProvider(this);
                if (InitialData is not null)
                {
                    foreach (KeyValuePair<string, string?> pair in InitialData)
                    {
                        Built.Set(pair.Key, pair.Value);
                    }
                }
                return Built;
            }
        }

        // A minimal third-party IConfigurationRoot: neither ConfigurationRoot nor ConfigurationManager, so it exposes no
        // reference engine. Delegates everything to a real root so the reference-aware read paths can be exercised
        // against a root type they do not recognise.
        private sealed class ThirdPartyRoot : IConfigurationRoot
        {
            private readonly IConfigurationRoot _inner;

            public ThirdPartyRoot(IConfigurationRoot inner) => _inner = inner;

            public string? this[string key]
            {
                get => _inner[key];
                set => _inner[key] = value;
            }

            public IEnumerable<IConfigurationProvider> Providers => _inner.Providers;

            public IEnumerable<IConfigurationSection> GetChildren() => _inner.GetChildren();

            public IChangeToken GetReloadToken() => _inner.GetReloadToken();

            public IConfigurationSection GetSection(string key) => _inner.GetSection(key);

            public void Reload() => _inner.Reload();
        }
    }
}
