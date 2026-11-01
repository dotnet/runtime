// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
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

        // The configuration every table-driven case is read against. "Probe" holds the value under test.
        private static Dictionary<string, string?> Fixture() => new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Target"] = "hit",
            ["Deep:Target"] = "deep",
            // Section itself holds no value, only children.
            ["Section:Child"] = "child",
            // Both holds a value and has children.
            ["Both"] = "both-value",
            ["Both:Child"] = "both-child",
            ["Pointer"] = "Deep",
            ["PointerToPointer"] = "Pointer",
            ["Suffix"] = "Target",
            ["Odd:{x}:Key"] = "braced",
            // Reached by splicing the key above in, which is how a quoted brace inside a sub-reference is checked to be
            // content rather than a sub-reference of its own.
            ["braced"] = "quoted-brace",
            ["a'b"] = "quoted-name",
            ["Weird(1):Key"] = "parens",
            ["With Space"] = "spaced",
            ["Chain:First"] = "$ref(Chain:Second)",
            ["Chain:Second"] = "$ref(Target)",
            ["Empty"] = "",
        };

        // (value stored at "Probe", value "Probe" reads back as).
        private static (string Value, string? Expected)[] Cases() => new (string, string?)[]
        {
            // Resolving.
            ("$ref(Target)", "hit"),
            ("$ref(TARGET)", "hit"),
            ("$REF(Target)", "hit"),
            ("$ref( Target )", "hit"),
            ("$ref(Both)", "both-value"),
            ("$ref(Chain:First)", "hit"),
            ("$ref(Weird(1):Key)", "parens"),
            ("$ref('With Space')", "spaced"),

            // Sub-references. They sit side by side rather than one inside another, and a brace within a quoted run is
            // content, so it neither opens one nor nests one.
            ("$ref({Pointer}:Target)", "deep"),
            ("$ref({Pointer}:{Suffix})", "deep"),
            ("$ref({Odd:'{x}':Key})", "quoted-brace"),
            ("$ref(Odd:'{x}':Key)", "braced"),
            ("$ref(Odd:\"{x}\":Key)", "braced"),
            ("$ref('a''b')", "quoted-name"),

            // A target that holds the empty string was found; a target that is missing was not. The two are distinct.
            ("$ref(Empty)", ""),

            // Well-formed but pointing at nothing, which reads as absent.
            ("$ref(Missing)", null),
            ("$ref(Section)", null),
            ("$ref({Missing}:Target)", null),
            ("$ref(Odd:{x}:Key)", null),
            // The closing parenthesis is the last character, so the extra one is part of the key rather than a syntax
            // error. No such key exists, so this reads as absent, not as a literal.
            ("$ref(Target))", null),

            // Not reference syntax, so literal.
            ("literal", "literal"),
            ("$ref", "$ref"),
            ("$ref(", "$ref("),
            ("$ref(Target", "$ref(Target"),
            ("$refx(Target)", "$refx(Target)"),
            ("prefix $ref(Target)", "prefix $ref(Target)"),
            ("$ref(Target) trailing", "$ref(Target) trailing"),
            // The sigil has to occupy the whole value, so even one trailing space makes the text a literal - sigil and
            // all - rather than a reference.
            ("$ref(Target) ", "$ref(Target) "),
            (" $ref(Target)", " $ref(Target)"),

            // There is no escape, so a doubled sigil is not a way of saying the syntax; it is text that never was the
            // syntax, and it keeps every sigil it was written with.
            ("$$ref(Target)", "$$ref(Target)"),
            ("$$$ref(Target)", "$$$ref(Target)"),
            ("$$REF(Target)", "$$REF(Target)"),
            ("$$refx(Target)", "$$refx(Target)"),
            ("$$(Target)", "$$(Target)"),
        };

        // Written as a reference but not writable as one. These are reported rather than handed back as literals: the
        // author plainly meant a reference, and quietly returning the text would hide the mistake behind a value that
        // happens to look like what was typed.
        public static IEnumerable<object[]> MalformedValues()
        {
            string[] values =
            {
                "$ref()",
                "$ref( )",
                "$ref(Deep:{Target)",
                "$ref(Deep:'Target)",
                "$ref(Deep:})",
                "$ref({})",
                "$ref({ })",
                // A syntax error written after a sub-reference, which the scan reaches once that sub-reference has
                // been put in.
                "$ref({Pointer}:{})",
                "$ref({Pointer}:{Deep)",
            };

            foreach (RootKind kind in new[] { RootKind.Builder, RootKind.Manager })
            {
                foreach (string value in values)
                {
                    yield return new object[] { kind, value };
                }
            }
        }

        [Theory]
        [MemberData(nameof(MalformedValues))]
        public void Malformed_ThrowsNamingTheKeyThatHoldsIt(RootKind kind, string value)
        {
            Dictionary<string, string?> data = Fixture();
            data["Probe"] = value;

            IConfigurationRoot root = BuildRoot(kind, Source(data));

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => root["Probe"]);
            Assert.Contains("Probe", error.Message);

            // A configuration value is the likeliest place in an application for a secret, and exception messages end
            // up in logs and bug reports, so the text at fault is named by its key and never quoted.
            Assert.DoesNotContain(value, error.Message);

            Assert.Throws<InvalidOperationException>(() => TryGetViaSection(root, "Probe", out _));
        }

        public static IEnumerable<object[]> NestedSubReferences()
        {
            string[] values =
            {
                "$ref({{PointerToPointer}}:Target)",
                "$ref(Deep:{Section:{Pointer}})",
                "$ref({Pointer}:{Deep:{Suffix}})",
            };

            foreach (RootKind kind in new[] { RootKind.Builder, RootKind.Manager })
            {
                foreach (string value in values)
                {
                    yield return new object[] { kind, value };
                }
            }
        }

        [Theory]
        [MemberData(nameof(NestedSubReferences))]
        public void NestedSubReference_ThrowsSayingSo(RootKind kind, string value)
        {
            // A sub-reference names a key; one that has to work out that name by reading another key is indirection
            // written inline, and whatever the inner one computes is worth a key of its own. Saying so plainly beats
            // reporting it as malformed, because the author meant something specific that is simply not supported.
            Dictionary<string, string?> data = Fixture();
            data["Probe"] = value;

            IConfigurationRoot root = BuildRoot(kind, Source(data));

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => root["Probe"]);
            Assert.Equal(SR.Format(SR.Error_NestedSubReference, "Probe"), error.Message);
            Assert.DoesNotContain(value, error.Message);

            Assert.Throws<InvalidOperationException>(() => TryGetViaSection(root, "Probe", out _));
        }

        public static IEnumerable<object[]> Resolutions()
        {
            foreach (RootKind kind in new[] { RootKind.Builder, RootKind.Manager })
            {
                foreach ((string value, string? expected) in Cases())
                {
                    yield return new object[] { kind, value, expected! };
                }
            }
        }

        [Theory]
        [MemberData(nameof(Resolutions))]
        public void Value_ReadsAsExpected(RootKind kind, string value, string? expected)
        {
            Dictionary<string, string?> data = Fixture();
            data["Probe"] = value;

            IConfigurationRoot root = BuildRoot(kind, Source(data));

            Assert.Equal(expected, root["Probe"]);

            // ConfigurationSection.Value is the indexer again, so the third read path - the one the binders use - has to
            // be asked separately.
            Assert.Equal(expected is not null, TryGetViaSection(root, "Probe", out string? viaTryGet));
            Assert.Equal(expected, viaTryGet);
        }

        // ConfigurationSection.TryGetValue is the only way into IConfigurationRoot.TryGetConfiguration, and it is not on
        // IConfigurationSection, which is why the binders test for the concrete type before calling it.
        private static bool TryGetViaSection(IConfigurationRoot root, string key, out string? value)
            => ((ConfigurationSection)root.GetSection(key)).TryGetValue(null, out value);

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void Malformed_IsReportedOnlyWhereTheScanReachesIt(RootKind kind)
        {
            // A sub-reference that names nothing settles the answer, so the scan stops there and never reaches what
            // follows. The same text is reported where the sub-reference resolves and the scan carries on into it.
            string[] values =
            {
                "$ref({Pointer}:})",
                "$ref({Pointer}:'X)",
            };

            foreach (string value in values)
            {
                IConfigurationRoot resolves = BuildRoot(kind, Source(("Probe", value), ("Pointer", "Target"), ("Target", "hit")));
                IConfigurationRoot missing = BuildRoot(kind, Source(("Probe", value)));

                Assert.Throws<InvalidOperationException>(() => resolves["Probe"]);
                Assert.Null(missing["Probe"]);
            }
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void Malformed_NamesTheKeyItWasWrittenAtRatherThanTheOneBeingRead(RootKind kind)
        {
            // The key that was asked for is rarely the key at fault: a chain and a sub-reference both arrive at the bad
            // text from somewhere else, and the author needs to be told where it is written, not where the read began.
            IConfigurationRoot chain = BuildRoot(kind, Source(("Probe", "$ref(Hop)"), ("Hop", "$ref(})")));
            IConfigurationRoot nested = BuildRoot(kind, Source(("Probe", "$ref({Pointer}:Target)"), ("Pointer", "$ref(})")));

            Assert.Contains("Hop", Assert.Throws<InvalidOperationException>(() => chain["Probe"]).Message);
            Assert.Contains("Pointer", Assert.Throws<InvalidOperationException>(() => nested["Probe"]).Message);
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void Reference_ResolvesAgainstTheMergedConfiguration(RootKind kind)
        {
            // The target lives in the lowest provider and is overridden in the highest; the reference must see the
            // value the configuration as a whole reports, not the one in its own provider.
            IConfigurationRoot root = BuildRoot(kind, new[]
            {
                Source(("Shared:Credential", "original")),
                Source(("Client:Credential", "$ref(Shared:Credential)")),
                Source(("Shared:Credential", "override")),
            });

            Assert.Equal("override", root["Client:Credential"]);
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void ChainedConfiguration_ResolvesItsOwnReferencesAgainstItsOwnKeys(RootKind kind)
        {
            // The merged view stops at a chain boundary. The inner configuration has already answered for "Target", so
            // the outer's override of it comes too late to change what the reference there resolved to.
            IConfigurationRoot inner = BuildRoot(kind, Source(
                ("Target", "inner"),
                ("Probe", "$ref(Target)")));

            IConfigurationRoot outer = new ConfigurationBuilder()
                .AddConfiguration(inner)
                .Add(Source(("Target", "outer")))
                .Build();

            Assert.Equal("inner", outer["Probe"]);
            Assert.Equal("outer", outer["Target"]);
        }

        [Fact]
        public void ChainedSection_ResolvesAgainstItsRootRatherThanTheKeysItIsChainedAs()
        {
            // Chaining a section re-roots its keys, so "Nest:Probe" is read as "Probe" from outside. Resolution is not
            // re-rooted with them: the inner root answers the reference, where the key it names still has its full path.
            IConfigurationRoot inner = new ConfigurationBuilder()
                .Add(Source(
                    ("Nest:Probe", "$ref(Nest:Target)"),
                    ("Nest:Target", "hit")))
                .Build();

            IConfigurationRoot outer = new ConfigurationBuilder()
                .AddConfiguration(inner.GetSection("Nest"))
                .Build();

            Assert.Equal("hit", outer["Probe"]);
        }

        [Fact]
        public void ChainedValue_IsFinal_SoTextThatLooksLikeAReferenceIsLeftAlone()
        {
            // A configuration that is not one of ours interprets nothing, so the reference text arrives intact. It is
            // still that configuration's answer, and a chained answer is taken as given.
            IConfigurationRoot outer = new ConfigurationBuilder()
                .AddConfiguration(new PlainConfiguration(("Probe", "$ref(Target)")))
                .Add(Source(("Target", "hit")))
                .Build();

            Assert.Equal("$ref(Target)", outer["Probe"]);
        }

        [Fact]
        public void ChainedValue_IsFinal_SoMalformedTextIsNotReportedEither()
        {
            // Nothing reads the body of a chained value, so there is no syntax in it to be wrong.
            IConfigurationRoot outer = new ConfigurationBuilder()
                .AddConfiguration(new PlainConfiguration(("Probe", "$ref(})")))
                .Build();

            Assert.Equal("$ref(})", outer["Probe"]);
        }

        [Fact]
        public void ChainedValue_IsFinal_EvenWhenAReferenceElsewhereLandsOnIt()
        {
            // The finality belongs to the chained value, not to the key that was asked for: a reference held by an
            // ordinary provider stops on what the chain hands back rather than following the text it finds there.
            IConfigurationRoot outer = new ConfigurationBuilder()
                .AddConfiguration(new PlainConfiguration(("Hop", "$ref(Target)")))
                .Add(Source(("Probe", "$ref(Hop)"), ("Target", "hit")))
                .Build();

            Assert.Equal("$ref(Target)", outer["Probe"]);
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void Reference_IsOverriddenByAHigherProvider(RootKind kind)
        {
            IConfigurationRoot root = BuildRoot(kind, new[]
            {
                Source(("Shared:Credential", "secret"), ("Client:Credential", "$ref(Shared:Credential)")),
                Source(("Client:Credential", "explicit")),
            });

            Assert.Equal("explicit", root["Client:Credential"]);
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void Reference_DoesNotAffectEnumeration(RootKind kind)
        {
            IConfigurationRoot root = BuildRoot(kind, Source(
                ("Shared:Credential", "secret"),
                ("Client:Credential", "$ref(Shared:Credential)"),
                ("Client:Name", "app")));

            IConfigurationSection[] children = root.GetSection("Client").GetChildren().ToArray();

            Assert.Equal(new[] { "Credential", "Name" }, children.Select(c => c.Key).ToArray());
            // A reference names a single key, so it never brings the target's children along with it.
            Assert.Empty(root.GetSection("Client:Credential").GetChildren());
            // Values read through enumeration resolve like any other read.
            Assert.Equal(new[] { "secret", "app" }, children.Select(c => c.Value).ToArray());
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void Reference_ToSectionDoesNotExposeTheTargetsChildren(RootKind kind)
        {
            IConfigurationRoot root = BuildRoot(kind, Source(
                ("Shared:Credential", "secret"),
                ("Client", "$ref(Shared)")));

            Assert.Null(root["Client"]);
            Assert.Empty(root.GetSection("Client").GetChildren());
            Assert.Null(root["Client:Credential"]);
        }

        // === Cycles ===

        public static IEnumerable<object[]> Cycles()
        {
            var cases = new[]
            {
                // Both shapes recurse now, so both are caught the same way, by writing the path down on the way back
                // out. A chain spends the hop budget getting there and a sub-reference spends the nesting one.
                new string?[] { "Self", "$ref(Self)", "Self -> Self" },
                new string?[] { "Braced", "$ref({Braced})", "Braced -> Braced" },
            };

            foreach (RootKind kind in new[] { RootKind.Builder, RootKind.Manager })
            {
                foreach (string?[] pair in cases)
                {
                    yield return new object?[] { kind, pair[0], pair[1], pair[2] };
                }
            }
        }

        [Theory]
        [MemberData(nameof(Cycles))]
        public void Cycle_Throws(RootKind kind, string key, string value, string? loop)
        {
            IConfigurationRoot root = BuildRoot(kind, Source((key, value)));

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => root[key]);
            Assert.Contains(key, exception.Message, StringComparison.OrdinalIgnoreCase);

            if (loop is not null)
            {
                Assert.Contains(loop, exception.Message, StringComparison.Ordinal);
            }
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void Cycle_Indirect_NamesTheLoopAndTheKeyThatWasRead(RootKind kind)
        {
            IConfigurationRoot root = BuildRoot(kind, Source(
                ("Entry", "$ref(A)"),
                ("A", "$ref(B)"),
                ("B", "$ref(C)"),
                ("C", "$ref(A)")));

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => root["Entry"]);

            // Entry led into the loop rather than round it, so it is named as the key that was read and left out of
            // the loop itself. Which key the loop is named from is whichever one the bound was reached on, which says
            // nothing about the configuration; what is worth pinning is every step round it, the way round they are
            // declared. Three keys rather than two, so that a loop reported backwards is not still the same text.
            Assert.Contains("Entry", exception.Message, StringComparison.Ordinal);
            Assert.Contains("A -> B", exception.Message, StringComparison.Ordinal);
            Assert.Contains("B -> C", exception.Message, StringComparison.Ordinal);
            Assert.Contains("C -> A", exception.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("Entry -> A", exception.Message, StringComparison.Ordinal);
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void Cycle_DoesNotAffectUnrelatedKeys(RootKind kind)
        {
            IConfigurationRoot root = BuildRoot(kind, Source(
                ("A", "$ref(B)"),
                ("B", "$ref(A)"),
                ("Fine", "$ref(Target)"),
                ("Target", "hit")));

            Assert.Equal("hit", root["Fine"]);
            Assert.Throws<InvalidOperationException>(() => root["A"]);
        }

        [Fact]
        public void RepeatedKeyInSeparateSubReferences_IsNotACycle()
        {
            // Reading the same key twice is ordinary reuse, not a loop. Counting hops cannot mistake one for the other,
            // which a set of visited keys could and once had to be written carefully to avoid.
            IConfigurationRoot root = BuildRoot(RootKind.Builder, Source(
                ("Alias", "$ref(Part)"),
                ("Part", "X"),
                ("X:X", "hit"),
                ("Probe", "$ref({Alias}:{Alias})")));

            Assert.Equal("hit", root["Probe"]);
        }

        [Fact]
        public void Nesting_BeyondTheLimit_Throws()
        {
            // Every construct that recurses must be bounded, or configuration deep enough to exhaust the stack takes the
            // process down with an uncatchable StackOverflowException. Sub-references no longer nest inside one another,
            // so what recurses is reading one: its target may hold a reference with a sub-reference of its own, and that
            // has to be resolved before the key holding it can be built. A chain recurses too, but is counted against
            // its own bound and covered by Chain_BeyondTheLimit_Throws.
            const int Depth = 40;

            var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Probe"] = "$ref({Step0})",
            };

            for (int i = 0; i < Depth; i++)
            {
                data["Step" + i] = "$ref({Step" + (i + 1) + "})";
            }

            data["Step" + Depth] = "Target";
            data["Target"] = "hit";

            IConfigurationRoot root = BuildRoot(RootKind.Builder, Source(data));

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => root["Probe"]);
            Assert.Contains("Probe -> Step0 -> Step1", error.Message, StringComparison.Ordinal);
            Assert.Contains("nests", error.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void OverBudget_IsReportedRatherThanWhateverTheRestOfTheExpressionSays()
        {
            // Once a bound has been reached the read is over and the answer is known, so the rest of the expression is
            // never looked at. The stray brace here would be reported as malformed if the reading carried on, which
            // would hide why the read actually stopped.
            const int Depth = 40;

            var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Probe"] = "$ref({Step0}})",
            };

            for (int i = 0; i < Depth; i++)
            {
                data["Step" + i] = "$ref({Step" + (i + 1) + "})";
            }

            data["Step" + Depth] = "Target";
            data["Target"] = "hit";

            IConfigurationRoot root = BuildRoot(RootKind.Builder, Source(data));

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => root["Probe"]);
            Assert.Contains("Probe", error.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("malformed", error.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Nesting_WithinTheLimit_Resolves()
        {
            // The limit has to leave room for configuration that genuinely goes a step or two, so check the shallow end
            // still works. Each step names the key the step below it resolved to, so the walk goes down the steps and
            // comes back up along the names, ending at the key that finally holds the answer.
            const int Depth = 8;

            var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < Depth; i++)
            {
                data["Step" + i] = "$ref({Step" + (i + 1) + "})";
            }

            data["Step" + Depth] = "Name1";

            for (int i = 1; i < Depth; i++)
            {
                data["Name" + i] = "Name" + (i + 1);
            }

            data["Name" + Depth] = "hit";
            data["Probe"] = "$ref(Step0)";

            IConfigurationRoot root = BuildRoot(RootKind.Builder, Source(data));

            Assert.Equal("hit", root["Probe"]);
        }

        [Fact]
        public void Chain_OfManyHops_DoesNotSpendTheNestingBudget()
        {
            // Hops and nesting are counted separately, so a chain may run its own length without touching what the
            // expressions along it are allowed to nest. The hop count here is comfortably past the nesting limit, so
            // if the two were ever confused this would throw instead of resolving.
            const int Hops = 40;
            var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < Hops; i++)
            {
                data["Key" + i] = "$ref(Key" + (i + 1) + ")";
            }
            data["Key" + Hops] = "end";

            IConfigurationRoot root = BuildRoot(RootKind.Builder, Source(data));

            Assert.Equal("end", root["Key0"]);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(500)]
        public void Chain_BeyondTheLimit_Throws(int hops)
        {
            // Without a bound a cycle would take the stack down rather than fail the read, so a chain that goes on too
            // long is treated the same way a cycle is. Reaching the bound writes the path down on the way back out and
            // looks for a key that was read twice; this chain has none, so it reports the bound rather than a loop.
            // Both lengths are past the bound, the shorter one only far enough to show that what stops the read is the
            // bound and not the end of the chain.
            var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < hops; i++)
            {
                data["Key" + i] = "$ref(Key" + (i + 1) + ")";
            }
            data["Key" + hops] = "end";

            IConfigurationRoot root = BuildRoot(RootKind.Builder, Source(data));

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => root["Key0"]);
            Assert.Contains("Key0", error.Message, StringComparison.Ordinal);
            Assert.Contains("Key0 -> Key1 -> Key2", error.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Cycle_BeyondTheChainLimit_ReportsTheLimitRatherThanTheLoop()
        {
            // A read only writes the path down once a bound is reached, and this one runs out of budget at the bound,
            // long before the loop closes at the far end. So every key it recorded is a different one and there is no
            // loop to name; an over-budget read is reported as exactly that, and names the keys it went through so
            // that whoever reads it can see the chain rather than be told to go looking for a cycle.
            const int Hops = 200;
            var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < Hops; i++)
            {
                data["Key" + i] = "$ref(Key" + (i + 1) + ")";
            }
            data["Key" + Hops] = "$ref(Key0)";

            IConfigurationRoot root = BuildRoot(RootKind.Builder, Source(data));

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => root["Key0"]);
            Assert.Contains("Key0 -> Key1 -> Key2", error.Message, StringComparison.Ordinal);
            Assert.DoesNotContain("cycle", error.Message, StringComparison.OrdinalIgnoreCase);
        }

        // === Writes and reloads ===

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void Set_StoresTheValueVerbatimAndResolvesOnRead(RootKind kind)
        {
            IConfigurationRoot root = BuildRoot(kind, Source(("Target", "hit")));

            root["Probe"] = "$ref(Target)";

            // Nothing is cached, so a value written after the root was built resolves on the very next read.
            Assert.Equal("hit", root["Probe"]);
        }

        [Fact]
        public void Reload_PicksUpANewTarget()
        {
            var source = new ReloadableMemorySource
            {
                InitialData = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["Target"] = "before",
                    ["Probe"] = "$ref(Target)",
                }
            };

            IConfigurationRoot root = new ConfigurationBuilder().Add(source).Build();
            Assert.Equal("before", root["Probe"]);

            source.Built!.Set("Target", "after");
            source.Built.TriggerReload();

            Assert.Equal("after", root["Probe"]);
        }

        // === Root kinds and providers ===

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ThirdPartyRoot_ResolvesReferences(bool lazyProviders)
        {
            IConfigurationRoot inner = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Target"] = "hit",
                    ["Probe"] = "$ref(Target)",
                })
                .Build();

            var root = new ThirdPartyRoot(inner, lazyProviders);

            // Reading through the section is the path a third-party root reaches us on. Its own indexer is its own
            // business, and this one forwards to a root that would resolve the reference by itself anyway.
            Assert.True(TryGetViaSection(root, "Probe", out string? value));
            Assert.Equal("hit", value);
            Assert.Equal(new[] { "Probe", "Target" }, root.GetChildren().Select(c => c.Key).OrderBy(k => k).ToArray());
        }

        [Fact]
        public void ProviderImplementedDirectly_ResolvesReferences()
        {
            // A provider that implements IConfigurationProvider rather than deriving from ConfigurationProvider is read
            // through TryGet like any other.
            IConfigurationRoot root = new ConfigurationBuilder()
                .Add(new MinimalSource(("Target", "hit"), ("Probe", "$ref(Target)")))
                .Build();

            Assert.Equal("hit", root["Probe"]);
        }

        // === Binding ===

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void Binder_ResolvesReferences(RootKind kind)
        {
            IConfigurationRoot root = BuildRoot(kind, Source(new Dictionary<string, string?>
            {
                ["Endpoint"] = "https://example.test",
                ["Port"] = "8080",
                ["Client:Url"] = "$ref(Endpoint)",
                ["Client:Port"] = "$ref(Port)",
                ["Client:Fallback"] = "$ref(Nowhere)",
                ["Client:Explicit"] = null,
            }));

#pragma warning disable IL2026, IL3050 // https://github.com/dotnet/runtime/issues/126862
            var options = root.GetSection("Client").Get<ClientOptions>();
#pragma warning restore IL2026, IL3050

            Assert.NotNull(options);
            Assert.Equal("https://example.test", options.Url);
            Assert.Equal(8080, options.Port);

            // A reference that finds nothing reads as absent, so the binder leaves the property at its default rather
            // than binding the text of the reference. That is not the same as a key written as null, which is present
            // and so does reach the property; the two assertions together pin the difference.
            Assert.Equal("unset", options.Fallback);
            Assert.Null(options.Explicit);
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void GetValue_ResolvesReferences(RootKind kind)
        {
            IConfigurationRoot root = BuildRoot(kind, Source(new Dictionary<string, string?>
            {
                ["Port"] = "8080",
                ["Probe"] = "$ref(Port)",
                ["Absent"] = "$ref(Nowhere)",
            }));

#pragma warning disable IL2026, IL3050 // https://github.com/dotnet/runtime/issues/126862
            Assert.Equal(8080, root.GetValue<int>("Probe"));
            Assert.Equal(-1, root.GetValue("Absent", -1));
#pragma warning restore IL2026, IL3050
        }

        private sealed class ClientOptions
        {
            public string? Url { get; set; }
            public int Port { get; set; }
            public string? Fallback { get; set; } = "unset";
            public string? Explicit { get; set; } = "unset";
        }

        [Fact]
        public void DisposedProvider_IsPassedOverByEveryReadPath()
        {
            // Reading the providers is one stage now, so a provider that has been disposed under a concurrent change to
            // the sources is passed over the same way whichever read path asked. Resolving a reference must not change
            // that, or turning the feature off would change how a disposed provider behaves.
            var manager = new ConfigurationManager();
            ((IConfigurationBuilder)manager).Add(new ThrowOnDisposeSource(("Target", "gone")));
            ((IConfigurationBuilder)manager).Add(new MemoryConfigurationSource
            {
                InitialData = new Dictionary<string, string?> { ["Probe"] = "$ref(Target)" }
            });

            Assert.Equal("gone", manager["Probe"]);

            manager.Dispose();

            // "Probe" is still readable, but the provider holding its target is not, so the reference has nothing to
            // resolve to and the key reads as absent.
            Assert.Null(manager["Probe"]);
            Assert.False(TryGetViaSection(manager, "Probe", out string? value));
            Assert.Null(value);
        }

        [Fact]
        public void DisposedProvider_BehavesTheSameWhetherOrNotAValueIsAReference()
        {
            // The same answers for a plain value.
            var manager = new ConfigurationManager();
            ((IConfigurationBuilder)manager).Add(new ThrowOnDisposeSource(("Probe", "plain")));

            Assert.Equal("plain", manager["Probe"]);

            manager.Dispose();

            Assert.Null(manager["Probe"]);
            Assert.False(TryGetViaSection(manager, "Probe", out string? value));
            Assert.Null(value);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)] // RuntimeConfigurationOptions are not supported on .NET Framework.
        public void GloballyDisabled_ReferenceIsLiteral()
        {
            var options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions.Add("Microsoft.Extensions.Configuration.DisableConfigurationTransformations", bool.TrueString);

            using RemoteInvokeHandle handle = RemoteExecutor.Invoke(static () =>
            {
                IConfigurationRoot root = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Target"] = "hit",
                        ["Probe"] = "$ref(Target)",
                    })
                    .Build();

                Assert.Equal("$ref(Target)", root["Probe"]);
                Assert.Equal("$ref(Target)", root.GetSection("Probe").Value);
            }, options);
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)] // RuntimeConfigurationOptions are not supported on .NET Framework.
        public void GloballyDisabled_DisposedProviderBehavesTheSame()
        {
            // The switch turns transformations off; it must not also decide how a read path treats a disposed provider.
            // These are the same assertions the two DisposedProvider tests make with references on.
            var options = new RemoteInvokeOptions();
            options.RuntimeConfigurationOptions.Add("Microsoft.Extensions.Configuration.DisableConfigurationTransformations", bool.TrueString);

            using RemoteInvokeHandle handle = RemoteExecutor.Invoke(static () =>
            {
                var manager = new ConfigurationManager();
                ((IConfigurationBuilder)manager).Add(new ThrowOnDisposeSource(("Probe", "plain")));

                Assert.Equal("plain", manager["Probe"]);

                manager.Dispose();

                Assert.Null(manager["Probe"]);
                Assert.False(TryGetViaSection(manager, "Probe", out string? value));
                Assert.Null(value);
            }, options);
        }

        // === Reading through the provider list ===

        [Theory]
        [InlineData("Plain", "plain", 1)]
        [InlineData("Target", "hit", 0)]
        [InlineData("Probe", "hit", 1)]
        [InlineData("Combined", "deep", 1)]
        public void ReadReportsTheProviderThatDeclaredTheKey(string key, string expected, int expectedProvider)
        {
            // A resolved value is attributed to the provider holding the text that was read, not to whichever provider
            // held the value it resolved to. "Combined" is built from two keys in the other provider, so no single
            // provider holds it; where it was declared is the only answer, and the more useful one either way.
            IList<IConfigurationProvider> providers =
            [
                Provider(("Target", "hit"), ("Pointer", "Deep"), ("Deep:Target", "deep")),
                Provider(
                    ("Plain", "plain"),
                    ("Probe", "$ref(Target)"),
                    ("Combined", "$ref({Pointer}:Target)")),
            ];

            Assert.True(ConfigurationEngine.Default.Get(providers, key, out string? value, out int providerIndex));
            Assert.Equal(expected, value);
            Assert.Equal(expectedProvider, providerIndex);
        }

        [Theory]
        [InlineData("Absent")]
        [InlineData("Dangling")]
        public void ReadThatFindsNothingNamesNoProvider(string key)
        {
            // Nothing was read, so nothing can be attributed: a read that produced nothing has no value at all. That
            // holds for a key nothing declared and for one declared as a reference that led nowhere.
            IList<IConfigurationProvider> providers = [Provider(("Dangling", "$ref(Missing)"))];

            Assert.False(ConfigurationEngine.Default.Get(providers, key, out string? value, out int providerIndex));
            Assert.Null(value);
            Assert.Equal(-1, providerIndex);
        }

        private static IConfigurationProvider Provider(params (string Key, string? Value)[] values)
        {
            var provider = new MemoryConfigurationProvider(new MemoryConfigurationSource
            {
                InitialData = values.ToDictionary(v => v.Key, v => v.Value)
            });

            provider.Load();
            return provider;
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void ProviderWalk_SeesTheReferenceAsWritten(RootKind kind)
        {
            // Resolution happens in the root's read paths, not in a provider, so code that goes round the root and asks
            // the providers directly gets the value as the source wrote it. That is deliberate: a provider answers for
            // what it holds, and what it holds is the reference itself.
            IConfigurationRoot root = BuildRoot(kind, Source(new Dictionary<string, string?>
            {
                ["Target"] = "hit",
                ["Probe"] = "$ref(Target)",
            }));

            Assert.Equal("hit", root["Probe"]);
            Assert.Equal("$ref(Target)", ReadByWalkingProviders(root, "Probe"));
            Assert.Equal("hit", ReadByWalkingProviders(root, "Target"));
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void DebugView_ShowsTheReferenceAsWritten(RootKind kind)
        {
            // GetDebugView is that walk, so it reports what each source declares rather than what a read would produce.
            // Its provider argument is what callers key value redaction on, and every value is still attributed to the
            // source actually holding it.
            IConfigurationRoot root = BuildRoot(kind, Source(new Dictionary<string, string?>
            {
                ["Target"] = "hit",
                ["Probe"] = "$ref(Target)",
            }));

            string view = root.GetDebugView();

            Assert.Contains($"Target=hit ({nameof(MemoryConfigurationProvider)})", view);
            Assert.Contains($"Probe=$ref(Target) ({nameof(MemoryConfigurationProvider)})", view);
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void Enumeration_SeesNoExtraKeys(RootKind kind)
        {
            // Resolution never invents a key, so enumeration is exactly what the sources declare.
            IConfigurationRoot root = BuildRoot(kind, Source(new Dictionary<string, string?>
            {
                ["Target"] = "hit",
                ["Probe"] = "$ref(Target)",
            }));

            Assert.Equal(new[] { "Probe", "Target" }, root.GetChildren().Select(c => c.Key).OrderBy(k => k, StringComparer.Ordinal));
        }

        [Fact]
        public void References_ResolveThroughEverySourceMutation()
        {
            // ConfigurationManager rebuilds its providers on every change to Sources, and a reference has to resolve
            // against the current set through all of them.
            var manager = new ConfigurationManager();
            IList<IConfigurationSource> sources = ((IConfigurationBuilder)manager).Sources;

            IConfigurationSource probe = Source(("Target", "first"), ("Probe", "$ref(Target)"));
            sources.Add(probe);
            Assert.Equal("first", manager["Probe"]);

            IConfigurationSource second = Source(("Target", "second"));
            sources.Add(second);
            Assert.Equal("second", manager["Probe"]);

            sources.Insert(0, Source(("Target", "outranked")));
            Assert.Equal("second", manager["Probe"]);

            sources[sources.IndexOf(probe)] = Source(("Probe", "$ref(Target)"));
            Assert.Equal("second", manager["Probe"]);

            Assert.True(sources.Remove(second));
            Assert.Equal("outranked", manager["Probe"]);

            sources.RemoveAt(0);
            Assert.Null(manager["Probe"]);

            sources.Clear();
            Assert.Empty(((IConfigurationRoot)manager).Providers);
        }

        // The reverse walk of IConfigurationRoot.Providers that GetDebugView and any equivalent third-party code do.
        private static string? ReadByWalkingProviders(IConfigurationRoot root, string key)
        {
            foreach (IConfigurationProvider provider in root.Providers.Reverse())
            {
                if (provider.TryGet(key, out string? value))
                {
                    return value;
                }
            }

            return null;
        }

        // === Relative targets ===

        // The value under test is written at "A:B:C:Probe", so ".." from there names the section holding it and
        // "..:X" a sibling, while "." stays on the key itself and ".:X" names a child.
        private static Dictionary<string, string?> Nested() => new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Root"] = "root-value",
            ["A"] = "a-value",
            ["A:Uncle"] = "uncle",
            ["A:B"] = "b-value",
            ["A:B:Cousin"] = "cousin",
            ["A:B:C"] = "c-value",
            ["A:B:C:Sibling"] = "sibling",
            ["A:B:C:Sibling:Child"] = "nephew",
            ["A:B:C:Leaf"] = "Sibling",
            ["A:B:C:."] = "dot-key",
            ["A:B:C:Microsoft.AspNetCore"] = "dotted",
            ["A:B:C:Acme Corp."] = "abbreviated",
            ["A:B:C:.NET"] = "dot-net",
            [".NET:Version"] = "net-version",
            ["A:B:C:Probe:Own"] = "own",
            ["Pointer"] = "A:B:C:Sibling",
            ["Up"] = "..",
            ["Down"] = ".:Own",
            ["Relay"] = "$ref(..:A:Uncle)",
        };

        public static IEnumerable<object[]> RelativeCases()
        {
            (string Value, string? Expected)[] cases = new (string, string?)[]
            {
                // ".." moves to the section holding the key, so it names a sibling of the key holding the reference.
                ("$ref(..:Sibling)", "sibling"),
                ("$ref(..:Sibling:Child)", "nephew"),
                // Each further ".." moves up another level.
                ("$ref(..:..:Cousin)", "cousin"),
                ("$ref(..:..:..:Uncle)", "uncle"),
                // Landing on a section that holds a value of its own reads that value.
                ("$ref(..:..)", "b-value"),
                // "." stays on the key holding the reference, so it names a child of it.
                ("$ref(.:Own)", "own"),
                // Mid-expression "." is a step that goes nowhere, which is allowed and changes nothing.
                ("$ref(.:..:Sibling)", "sibling"),
                ("$ref(A:.:B:Cousin)", "cousin"),
                // "A:B:C:Probe" has four segments, so four moves land on the root and an ordinary top level key follows.
                ("$ref(..:..:..:..:Root)", "root-value"),
                // A fifth would be above the root, which names nothing.
                ("$ref(..:..:..:..:..:Root)", null),
                // A sibling that does not exist is absent, as an absolute target would be.
                ("$ref(..:Nothing)", null),
                // Absolute targets are unaffected: no opening move means no relativity.
                ("$ref(A:B:C:Sibling)", "sibling"),
                ("$ref(Sibling)", null),
                // Elsewhere in the expression ".." still moves to the parent, so this is A:Uncle.
                ("$ref(A:B:..:Uncle)", "uncle"),
                ("$ref(A:..:Root)", "root-value"),
                // Ending on a move names the section it lands on rather than leaving a dangling separator.
                ("$ref(A:B:..)", "a-value"),
                ("$ref(A:B:.)", "b-value"),
                // A sub-reference splices in a key, and ".." moves up from wherever that leaves off. "Pointer" holds
                // "A:B:C:Sibling", so two moves reach A:B.
                ("$ref({Pointer}:..:..:Cousin)", "cousin"),
                // A sub-reference is written at the same key, so it is relative to the same place.
                ("$ref(..:{..:Leaf})", "sibling"),
                // A move a sub-reference brings in opens the expression just as a written one does, so it moves from
                // the key the reference was found at rather than from the root. "Up" holds "..".
                ("$ref({Up}:Sibling)", "sibling"),
                ("$ref({Up}:{Up}:Cousin)", "cousin"),
                ("$ref({Up})", "c-value"),
                ("$ref({Down})", "own"),
                // "Pointer" holds an absolute key, so bringing that in leaves the expression absolute.
                ("$ref({Pointer})", "sibling"),
                // A dot is a move only when it is a whole segment: it has to start one, so a segment that merely ends
                // in a dot is a key as written, and it has to fill one, so a segment that begins with a dot is too.
                ("$ref(..:Acme Corp.)", "abbreviated"),
                ("$ref(..:Microsoft.AspNetCore)", "dotted"),
                ("$ref(..:.NET)", "dot-net"),
                // Which is what keeps an expression opening with such a segment absolute rather than relative.
                ("$ref(.NET:Version)", "net-version"),
                // Three dots are neither "." nor "..", so they are a key as written.
                ("$ref(..:...)", null),
                // Quoting makes a dot ordinary text, which is how a segment that really is "." is named.
                ("$ref(..:'.')", "dot-key"),
                ("$ref('..:Sibling')", null),
                // Quoting cannot make a separator ordinary, since a key is one flat string and has no other way to
                // spell one. So a dot after a quoted run starts a segment exactly when the key built so far ends at
                // one, whichever side of the closing quote the separator happens to be written on.
                ("$ref('A:B':..:Uncle)", "uncle"),
                ("$ref('A:B:'..:Uncle)", "uncle"),
                ("$ref('A:B':.:Cousin)", "cousin"),
                ("$ref('A:B:'.:Cousin)", "cousin"),
                // A quoted run that leaves off mid-segment does not, so there the dots are part of a name.
                ("$ref('A:B'..:Uncle)", null),
                // A reference reached through another one is relative to where it is written, not where the read began.
                ("$ref(Relay)", "uncle"),
            };

            foreach (RootKind kind in new[] { RootKind.Builder, RootKind.Manager })
            {
                foreach ((string value, string? expected) in cases)
                {
                    yield return new object[] { kind, value, expected! };
                }
            }
        }

        [Theory]
        [MemberData(nameof(RelativeCases))]
        public void RelativeTarget_ReadsAsExpected(RootKind kind, string value, string? expected)
        {
            Dictionary<string, string?> data = Nested();
            data["A:B:C:Probe"] = value;

            IConfigurationRoot root = BuildRoot(kind, Source(data));

            Assert.Equal(expected, root["A:B:C:Probe"]);
            Assert.Equal(expected is not null, TryGetViaSection(root, "A:B:C:Probe", out string? viaTryGet));
            Assert.Equal(expected, viaTryGet);
        }

        [Theory]
        [InlineData("A:B:Probe", "$ref(..:Probe)")]
        [InlineData("A:B:Probe", "$ref(.)")]
        // A key ending in a separator ends in an empty segment, so staying put stays on that key rather than stepping
        // out of it.
        [InlineData("A:B:", "$ref(.)")]
        public void RelativeTarget_PointingAtItself_IsACycle(string key, string value)
        {
            IConfigurationRoot root = BuildRoot(RootKind.Builder, Source(new Dictionary<string, string?>
            {
                [key] = value,
            }));

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => root[key]);
            Assert.Contains(key, error.Message);
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void RelativeTarget_MovesFromTheWholeKey_EmptyLastSegmentAndAll(RootKind kind)
        {
            // "One:" names two segments, the last of them empty, which is what ConfigurationPath.GetParentPath says of
            // it too. The separator it ends with is where that empty segment begins rather than a join onto the move,
            // so one move lands on "One" and two land on the root.
            IConfigurationRoot root = BuildRoot(kind, Source(new Dictionary<string, string?>
            {
                ["One:Sibling"] = "sibling",
                ["One:"] = "$ref(..:Sibling)",
                ["Two"] = "two-value",
                ["Two:"] = "$ref(..)",
                ["Three:Uncle"] = "uncle",
                ["Three:Sub:"] = "$ref(..:..:Uncle)",
            }));

            Assert.Equal("sibling", root["One:"]);
            Assert.Equal("two-value", root["Two:"]);
            Assert.Equal("uncle", root["Three:Sub:"]);
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void ValuePutIn_IsReadWhereItLands(RootKind kind)
        {
            // The two probes come to the same text, "Deep:..:Target", and read the same way. A value goes in where the
            // placeholder was and the reading carries on from there, so a move arriving in one counts where it lands
            // rather than being carried through as part of a name.
            IConfigurationRoot root = BuildRoot(kind, Source(new Dictionary<string, string?>
            {
                ["Target"] = "hit",
                ["Pointer"] = "Deep:..",
                ["Written"] = "$ref(Deep:..:Target)",
                ["BroughtIn"] = "$ref({Pointer}:Target)",
            }));

            Assert.Equal("hit", root["Written"]);
            Assert.Equal("hit", root["BroughtIn"]);
        }

        [Theory]
        [MemberData(nameof(RootKinds))]
        public void ValuePutIn_MayCarryAPlaceholderOfItsOwn(RootKind kind)
        {
            // One level bans a placeholder written inside another's braces. A value that happens to carry one is not
            // that: it is read where it lands, so this resolves in two steps rather than as a second level.
            IConfigurationRoot root = BuildRoot(kind, Source(new Dictionary<string, string?>
            {
                ["Section"] = "Deep",
                ["Pointer"] = "{Section}:Target",
                ["Deep:Target"] = "hit",
                ["Probe"] = "$ref({Pointer})",
            }));

            Assert.Equal("hit", root["Probe"]);
        }

        public static IEnumerable<object[]> QuotedInValueCases()
        {
            (string Pointer, string Key)[] cases = new[]
            {
                // A dot is a move wherever it lands, so a value that really does name a segment of ".." keeps it by
                // quoting it.
                ("Odd:'..':Key", "Odd:..:Key"),
                // A brace is no different. What a sub-reference brings in is meant to form a key, so a value carrying
                // braces of its own - a registry-shaped GUID, a format string - says so the same way.
                ("'{6B29FC40-CA47-1067-B31D-00DD010662DA}'", "{6B29FC40-CA47-1067-B31D-00DD010662DA}"),
                ("'Data{':Suffix", "Data{:Suffix"),
            };

            foreach (RootKind kind in new[] { RootKind.Builder, RootKind.Manager })
            {
                foreach ((string pointer, string key) in cases)
                {
                    yield return new object[] { kind, pointer, key };
                }
            }
        }

        [Theory]
        [MemberData(nameof(QuotedInValueCases))]
        public void ValuePutIn_CanQuoteSyntaxToKeepItAsText(RootKind kind, string pointer, string key)
        {
            IConfigurationRoot root = BuildRoot(kind, Source(new Dictionary<string, string?>
            {
                [key] = "found",
                ["Pointer"] = pointer,
                ["Probe"] = "$ref({Pointer})",
            }));

            Assert.Equal("found", root["Probe"]);
        }

        [Fact]
        public void ValuePutIn_ThatBringsBackThePlaceholder_Throws()
        {
            // A value carrying the placeholder that brought it in costs neither a hop nor a level, so counting what
            // goes in is the only thing that stops it going round - and is why it is reported as expansion rather than
            // as either of those.
            IConfigurationRoot root = BuildRoot(RootKind.Builder, Source(
                ("Loop", "{Loop}"),
                ("Probe", "$ref({Loop})")));

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => root["Probe"]);
            Assert.Contains("Probe", error.Message);
            Assert.Contains("expands more than 32", error.Message);
            Assert.DoesNotContain("nests", error.Message);
        }

        [Theory]
        [InlineData(32, false)]
        [InlineData(33, true)]
        public void ValuesPutIn_AreBoundedPerExpression_WithoutBeingCalledNesting(int count, bool throws)
        {
            // Placeholders written side by side are not nested in one another, so the bound they run into has to say so
            // rather than send the reader looking for depth that is not there.
            var data = new Dictionary<string, string?>
            {
                ["Bit"] = "x",
                [new string('x', count)] = "found",
                ["Probe"] = "$ref(" + string.Concat(Enumerable.Repeat("{Bit}", count)) + ")",
            };

            IConfigurationRoot root = BuildRoot(RootKind.Builder, Source(data));

            if (!throws)
            {
                Assert.Equal("found", root["Probe"]);
                return;
            }

            InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => root["Probe"]);
            Assert.Contains("expands more than 32", error.Message);
            Assert.DoesNotContain("nests", error.Message);
        }

        // === Helpers ===

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

        private static IConfigurationSource Source(params (string Key, string? Value)[] entries)
            => Source(Dict(entries));

        private static IConfigurationSource Source(IDictionary<string, string?> data)
            => new MemoryConfigurationSource { InitialData = data };

        private static Dictionary<string, string?> Dict((string Key, string? Value)[] entries)
        {
            var dictionary = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach ((string key, string? value) in entries)
            {
                dictionary[key] = value;
            }
            return dictionary;
        }

        private sealed class MinimalSource : IConfigurationSource
        {
            private readonly Dictionary<string, string?> _data;

            public MinimalSource(params (string Key, string? Value)[] entries) => _data = Dict(entries);

            public IConfigurationProvider Build(IConfigurationBuilder builder) => new MinimalProvider(_data);
        }

        private sealed class MinimalProvider : IConfigurationProvider
        {
            private readonly Dictionary<string, string?> _data;
            private readonly ConfigurationReloadToken _reloadToken = new ConfigurationReloadToken();

            public MinimalProvider(Dictionary<string, string?> data) => _data = data;

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
                    if (entry.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
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

        // A provider that throws ObjectDisposedException from every read once disposed, to model one that holds
        // native or OS resources.
        private sealed class ThrowOnDisposeSource : IConfigurationSource
        {
            private readonly Dictionary<string, string?> _data;

            public ThrowOnDisposeSource(params (string Key, string? Value)[] entries) => _data = Dict(entries);

            public IConfigurationProvider Build(IConfigurationBuilder builder) => new ThrowOnDisposeProvider(_data);
        }

        private sealed class ThrowOnDisposeProvider : IConfigurationProvider, IDisposable
        {
            private readonly Dictionary<string, string?> _data;
            private readonly ConfigurationReloadToken _reloadToken = new ConfigurationReloadToken();
            private bool _disposed;

            public ThrowOnDisposeProvider(Dictionary<string, string?> data) => _data = data;

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
                return earlierKeys;
            }

            public void Dispose() => _disposed = true;

            private void ThrowIfDisposed()
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(ThrowOnDisposeProvider));
                }
            }
        }

        // A configuration that is not one of ours: it has no providers and interprets nothing, so a value written as a
        // reference reaches the chained provider as the text it is. Only values are read through it.
        private sealed class PlainConfiguration : IConfiguration
        {
            private readonly Dictionary<string, string?> _data;
            private readonly ConfigurationReloadToken _reloadToken = new ConfigurationReloadToken();

            public PlainConfiguration(params (string Key, string? Value)[] entries) => _data = Dict(entries);

            public string? this[string key]
            {
                get => _data.TryGetValue(key, out string? value) ? value : null;
                set => _data[key] = value;
            }

            public IEnumerable<IConfigurationSection> GetChildren() => Array.Empty<IConfigurationSection>();

            public IChangeToken GetReloadToken() => _reloadToken;

            public IConfigurationSection GetSection(string key) => throw new NotSupportedException();
        }

        // A minimal third-party IConfigurationRoot: neither ConfigurationRoot nor ConfigurationManager, so the read
        // paths reach it through IConfigurationRoot.Providers rather than a known root type.
        private sealed class ThirdPartyRoot : IConfigurationRoot
        {
            private readonly IConfigurationRoot _inner;
            private readonly bool _lazyProviders;

            public ThirdPartyRoot(IConfigurationRoot inner, bool lazyProviders)
            {
                _inner = inner;
                _lazyProviders = lazyProviders;
            }

            public string? this[string key]
            {
                get => _inner[key];
                set => _inner[key] = value;
            }

            // A root that filters or projects its sources hands out something that is not a list, so we have to copy it
            // before reading. Both shapes are exercised.
            public IEnumerable<IConfigurationProvider> Providers =>
                _lazyProviders ? _inner.Providers.Select(p => p) : _inner.Providers;

            public IEnumerable<IConfigurationSection> GetChildren() => this.GetChildrenImplementation(null);

            public IChangeToken GetReloadToken() => _inner.GetReloadToken();

            public IConfigurationSection GetSection(string key) => new ConfigurationSection(this, key);

            public void Reload() => _inner.Reload();
        }
    }
}
