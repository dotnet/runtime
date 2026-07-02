// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration.Memory;
using Xunit;

namespace Microsoft.Extensions.Configuration.Test
{
    public class ConfigurationReferenceTests
    {
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
                    foreach (var pair in InitialData)
                    {
                        Built.Set(pair.Key, pair.Value);
                    }
                }
                return Built;
            }
        }

        private static IDictionary<string, string?> Dict(params (string Key, string? Value)[] entries)
        {
            var d = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var (k, v) in entries) d[k] = v;
            return d;
        }

        private static IConfigurationBuilder BuilderWith(IDictionary<string, string?> data)
        {
            var b = new ConfigurationBuilder();
            b.Add(new MemoryConfigurationSource { InitialData = data });
            return b;
        }

        // === Fixed-mode (single concrete target) ===

        [Fact]
        public void Fixed_RefLiteral_ResolvesToTarget()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Shared:Credential", "secret"),
                    ("Client1:Credential", "ref(Shared:Credential)")))
                .AllowReferences(r => r.Allow("Client1:Credential", "Shared:Credential"))
                .Build();

            Assert.Equal("secret", root["Client1:Credential"]);
        }

        [Fact]
        public void Fixed_NonEmptyLiteral_Wins()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Shared:Credential", "shared-secret"),
                    ("Client1:Credential", "direct-secret")))
                .AllowReferences(r => r.Allow("Client1:Credential", "Shared:Credential"))
                .Build();

            Assert.Equal("direct-secret", root["Client1:Credential"]);
        }

        [Fact]
        public void Fixed_SubTreeResolution_ReadsThroughSuffix()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Shared:Credential:Type", "ManagedIdentity"),
                    ("Shared:Credential:ClientId", "abc"),
                    ("Client1:Credential", "ref(Shared:Credential)")))
                .AllowReferences(r => r.Allow("Client1:Credential", "Shared:Credential"))
                .Build();

            Assert.Equal("ManagedIdentity", root["Client1:Credential:Type"]);
            Assert.Equal("abc", root["Client1:Credential:ClientId"]);
        }

        [Fact]
        public void Fixed_SegmentBoundaryRequired()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Other", "real"),
                    ("Client1:Cred", "ref(Other)")))
                .AllowReferences(r => r.Allow("Client1:Cred", "Other"))
                .Build();

            Assert.Equal("real", root["Client1:Cred"]);
            Assert.Null(root["Client1:Credential"]);
        }

        [Fact]
        public void Fixed_Chaining_ResolvesAcrossHops()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Shared:Credential", "secret"),
                    ("Client1:Credential", "ref(Client2:Credential)"),
                    ("Client2:Credential", "ref(Shared:Credential)")))
                .AllowReferences(r => r
                    .Allow("Client1:Credential", "Client2:Credential")
                    .Allow("Client2:Credential", "Shared:Credential"))
                .Build();

            Assert.Equal("secret", root["Client1:Credential"]);
        }

        [Fact]
        public void Fixed_LongestSubjectWins()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("ShortTarget", "short"),
                    ("LongTarget:X", "long"),
                    ("Foo", "ref(ShortTarget)"),
                    ("Foo:X", "ref(LongTarget:X)")))
                .AllowReferences(r => r
                    .Allow("Foo", "ShortTarget")
                    .Allow("Foo:X", "LongTarget:X"))
                .Build();

            Assert.Equal("long", root["Foo:X"]);
            Assert.Equal("short", root["Foo"]);
        }

        [Fact]
        public void Fixed_GetChildren_UnionsLiteralAndTarget()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Shared:Credential:Type", "MI"),
                    ("Client1:Credential", "ref(Shared:Credential)"),
                    ("Client1:Credential:Extra", "extra")))
                .AllowReferences(r => r.Allow("Client1:Credential", "Shared:Credential"))
                .Build();

            HashSet<string> children = root.GetSection("Client1:Credential").GetChildren().Select(c => c.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            Assert.Contains("Type", children);
            Assert.Contains("Extra", children);
        }

        [Fact]
        public void Fixed_GetChildren_SubjectAppearsEvenWithoutProviderData()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Shared:Credential", "x"),
                    ("Client1:Credential", "ref(Shared:Credential)")))
                .AllowReferences(r => r.Allow("Client1:Credential", "Shared:Credential"))
                .Build();

            HashSet<string> top = root.GetChildren().Select(c => c.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            Assert.Contains("Client1", top);
        }

        // === Configurable mode (multi-target) ===

        [Fact]
        public void Configurable_ExplicitSelection_PicksFirstTarget()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Shared:Prod:Credential", "prod"),
                    ("Shared:Staging:Credential", "staging"),
                    ("Client1:Credential", "ref(Shared:Prod:Credential)")))
                .AllowReferences(r => r.Allow("Client1:Credential", "Shared:Prod:Credential").Allow("Client1:Credential", "Shared:Staging:Credential"))
                .Build();

            Assert.Equal("prod", root["Client1:Credential"]);
        }

        [Fact]
        public void Configurable_ValidSelection_ResolvesThrough()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Shared:Prod:Credential", "prod"),
                    ("Shared:Staging:Credential", "staging"),
                    ("Client1:Credential", "ref(Shared:Prod:Credential)")))
                .AllowReferences(r => r.Allow("Client1:Credential", "Shared:Prod:Credential").Allow("Client1:Credential", "Shared:Staging:Credential"))
                .Build();

            Assert.Equal("prod", root["Client1:Credential"]);
        }

        [Fact]
        public void Configurable_OutOfRuleSelection_TreatedAsDirectValue()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Client1:Credential", "Shared:Random:Credential")))
                .AllowReferences(r => r.Allow("Client1:Credential", "Shared:Prod:Credential").Allow("Client1:Credential", "Shared:Staging:Credential"))
                .Build();

            // Literal at the subject doesn't match any rule target: it's a direct value.
            Assert.Equal("Shared:Random:Credential", root["Client1:Credential"]);
        }

        [Fact]
        public void Configurable_OutOfRuleSelection_NoSuffixRewrite()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Shared:Prod:Credential:Type", "MI"),
                    ("Client1:Credential", "Shared:Random:Credential")))
                .AllowReferences(r => r.Allow("Client1:Credential", "Shared:Prod:Credential").Allow("Client1:Credential", "Shared:Staging:Credential"))
                .Build();

            // Subject literal is a direct value, not a selection: suffix reads don't rewrite.
            Assert.Null(root["Client1:Credential:Type"]);
        }

        [Fact]
        public void Configurable_SubTreeResolution()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Shared:Prod:Credential:Type", "MI"),
                    ("Shared:Prod:Credential:ClientId", "abc"),
                    ("Client1:Credential", "ref(Shared:Prod:Credential)")))
                .AllowReferences(r => r.Allow("Client1:Credential", "Shared:Prod:Credential").Allow("Client1:Credential", "Shared:Staging:Credential"))
                .Build();

            Assert.Equal("MI", root["Client1:Credential:Type"]);
            Assert.Equal("abc", root["Client1:Credential:ClientId"]);
            HashSet<string> children = root.GetSection("Client1:Credential").GetChildren().Select(c => c.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
            Assert.Contains("Type", children);
            Assert.Contains("ClientId", children);
        }

        // === Template mode (wildcards) ===

        [Fact]
        public void Template_MatchesWildcardSegment()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Shared:Prod:Credential", "prod-secret"),
                    ("Client1:Credential", "ref(Shared:Prod:Credential)")))
                .AllowReferences(r => r.Allow("Client1:Credential", "Shared:*:Credential"))
                .Build();

            Assert.Equal("prod-secret", root["Client1:Credential"]);
        }

        [Fact]
        public void Template_DoesNotMatchOutOfShape()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Client1:Credential", "DifferentRoot:Prod:Credential")))
                .AllowReferences(r => r.Allow("Client1:Credential", "Shared:*:Credential"))
                .Build();

            // Out-of-shape literal isn't a selection; it's returned as a direct value.
            Assert.Equal("DifferentRoot:Prod:Credential", root["Client1:Credential"]);
        }

        [Fact]
        public void Template_InvalidWildcardSegment_ThrowsAtRegistration()
        {
            var b = new ConfigurationBuilder();
            // ** must be a whole segment; mixing it with literal text is invalid.
            Assert.Throws<ArgumentException>(() => b.AllowReferences(r => r.Allow("X", "Shared:Foo**:Credential")));
        }

        // === Template subjects (subject wildcards) ===

        [Fact]
        public void SubjectTemplate_StarSegment_MatchesEveryUpstreamKey()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Secrets:Db", "db-secret"),
                    ("Secrets:Cache", "cache-secret"),
                    ("Services:A:Conn", "ref(Secrets:Db)"),
                    ("Services:B:Conn", "ref(Secrets:Cache)")))
                .AllowReferences(r => r.Allow("Services:*:Conn", "Secrets:*"))
                .Build();

            Assert.Equal("db-secret", root["Services:A:Conn"]);
            Assert.Equal("cache-secret", root["Services:B:Conn"]);
        }

        [Fact]
        public void SubjectTemplate_DoubleStar_MatchesCrossSegments()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Secrets:X", "x"),
                    ("Apps:Web:Db:Conn", "ref(Secrets:X)"),
                    ("Apps:Worker:Conn", "ref(Secrets:X)")))
                .AllowReferences(r => r.Allow("Apps:**:Conn", "Secrets:*"))
                .Build();

            Assert.Equal("x", root["Apps:Web:Db:Conn"]);
            Assert.Equal("x", root["Apps:Worker:Conn"]);
        }

        [Fact]
        public void SubjectTemplate_StarWithinSegment_DoesNotCrossSegments()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Secrets:X", "x"),
                    ("DbProd", "ref(Secrets:X)"),
                    ("Db:Prod", "ref(Secrets:X)")))
                .AllowReferences(r => r.Allow("Db*", "Secrets:*"))
                .Build();

            // Within-segment glob picks up the single-segment key only.
            Assert.Equal("x", root["DbProd"]);
            // The colon-separated path is two segments; the single-segment template doesn't match.
            Assert.Equal("ref(Secrets:X)", root["Db:Prod"]);
        }

        [Fact]
        public void SubjectTemplate_ConcreteRulePreemptsTemplate()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("ExplicitTarget", "explicit"),
                    ("WildcardTarget", "wildcard"),
                    ("Services:A:Conn", "ref(ExplicitTarget)")))
                .AllowReferences(r => r
                    .Allow("Services:*:Conn", "WildcardTarget")
                    .Allow("Services:A:Conn", "ExplicitTarget"))
                .Build();

            // Concrete rule wins at Services:A:Conn even though the template would match too.
            Assert.Equal("explicit", root["Services:A:Conn"]);
        }

        [Fact]
        public void SubjectTemplate_MoreSpecificTemplateWins()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Secrets:A", "a"),
                    ("Secrets:B", "b"),
                    ("Services:A:Conn", "ref(Secrets:A)")))
                .AllowReferences(r => r
                    .Allow("Services:**", "Secrets:B")
                    .Allow("Services:*:Conn", "Secrets:A"))
                .Build();

            // The more-specific Services:*:Conn (literal-rich, no **) takes precedence over Services:**.
            Assert.Equal("a", root["Services:A:Conn"]);
        }

        [Fact]
        public void SubjectTemplate_NoMatchingUpstreamKey_NoOp()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Secrets:A", "a")))
                .AllowReferences(r => r.Allow("Services:*:Conn", "Secrets:*"))
                .Build();

            // No upstream key matches the template subject, so nothing is materialised.
            Assert.Null(root["Services:A:Conn"]);
        }

        [Fact]
        public void SubjectTemplate_InvalidPattern_Throws()
        {
            var b = new ConfigurationBuilder();
            Assert.Throws<ArgumentException>(() => b.AllowReferences(r => r.Allow("Foo:**bar", "T")));
        }

        // === AddReference validation ===

        [Fact]
        public void Allow_NullSubject_Throws() =>
            Assert.Throws<ArgumentNullException>(() => new ConfigurationBuilder().AllowReferences(r => r.Allow(null!, "T")));

        [Fact]
        public void Allow_EmptySubject_Throws() =>
            Assert.Throws<ArgumentException>(() => new ConfigurationBuilder().AllowReferences(r => r.Allow(":::", "T")));

        [Fact]
        public void Allow_EmptyTarget_Throws() =>
            Assert.Throws<ArgumentException>(() => new ConfigurationBuilder().AllowReferences(r => r.Allow("X", "")));

        [Fact]
        public void Allow_DuplicateCalls_MergeTargets()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Shared:Prod:Credential", "prod"),
                    ("Shared:Staging:Credential", "staging"),
                    ("Client1:Credential", "ref(Shared:Staging:Credential)")))
                .AllowReferences(r => r
                    .Allow("Client1:Credential", "Shared:Prod:Credential")
                    .Allow("Client1:Credential", "Shared:Staging:Credential"))
                .Build();

            Assert.Equal("staging", root["Client1:Credential"]);
        }

        [Fact]
        public void Allow_DuplicateTarget_Deduped()
        {
            // Adding the same target twice must not cause spurious rule mode change.
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Shared:Credential", "secret"),
                    ("Client1:Credential", "ref(Shared:Credential)")))
                .AllowReferences(r => r
                    .Allow("Client1:Credential", "Shared:Credential")
                    .Allow("Client1:Credential", "Shared:Credential"))
                .Build();

            Assert.Equal("secret", root["Client1:Credential"]);
        }

        [Fact]
        public void Allow_MultipleTargets_InOneCall_AllPermitted()
        {
            // Two targets supplied to a single Allow via the params overload; both must resolve.
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Db:Primary", "p"),
                    ("Db:Secondary", "s"),
                    ("App:One", "ref(Db:Primary)"),
                    ("App:Two", "ref(Db:Secondary)")))
                .AllowReferences(r => r.Allow("App:*", "Db:Primary", "Db:Secondary"))
                .Build();

            Assert.Equal("p", root["App:One"]);
            Assert.Equal("s", root["App:Two"]);
        }

        [Fact]
        public void Deny_MultipleTargets_InOneCall_AllVetoed()
        {
            // Both vetoed targets come from a single Deny via the params overload.
            foreach (string blocked in new[] { "Secrets:A", "Secrets:B" })
            {
                IConfigurationBuilder builder = BuilderWith(Dict(
                        ("Secrets:A", "a"),
                        ("Secrets:B", "b"),
                        ("App:X", $"ref({blocked})")))
                    .AllowReferences(r => r
                        .Allow("App:X", "Secrets:*")
                        .Deny("App:X", "Secrets:A", "Secrets:B"));

                Assert.Throws<InvalidOperationException>(() => builder.Build());
            }
        }

        // === Reload validation ===

        [Fact]
        public void Reload_SelectionFlipsBetweenTargets()
        {
            var src = new ReloadableMemorySource
            {
                InitialData = new[]
                {
                    new KeyValuePair<string, string?>("Shared:Prod:Credential", "prod"),
                    new KeyValuePair<string, string?>("Shared:Staging:Credential", "staging"),
                    new KeyValuePair<string, string?>("Client1:Credential", "ref(Shared:Prod:Credential)"),
                }
            };
            var b = new ConfigurationBuilder();
            b.Add(src);
            b.AllowReferences(r => r.Allow("Client1:Credential", "Shared:Prod:Credential").Allow("Client1:Credential", "Shared:Staging:Credential"));
            IConfigurationRoot root = b.Build();
            Assert.Equal("prod", root["Client1:Credential"]);

            src.Built!.Set("Client1:Credential", "ref(Shared:Staging:Credential)");
            src.Built!.TriggerReload();

            Assert.Equal("staging", root["Client1:Credential"]);
        }

        // === Cycle detection ===

        [Fact]
        public void Cycle_DirectFixed_Throws()
        {
            IConfigurationBuilder b = BuilderWith(Dict(
                    ("A", "ref(B)"),
                    ("B", "ref(A)")))
                .AllowReferences(r => r
                    .Allow("A", "B")
                    .Allow("B", "A"));
            Assert.Throws<InvalidOperationException>(() => b.Build());
        }

        [Fact]
        public void Cycle_IndirectFixed_Throws()
        {
            IConfigurationBuilder b = BuilderWith(Dict(
                    ("A", "ref(B)"),
                    ("B", "ref(C)"),
                    ("C", "ref(A)")))
                .AllowReferences(r => r
                    .Allow("A", "B")
                    .Allow("B", "C")
                    .Allow("C", "A"));
            Assert.Throws<InvalidOperationException>(() => b.Build());
        }

        // === ConfigurationManager parity ===

        [Fact]
        public void ConfigurationManager_AppliesReference()
        {
            using var cm = new ConfigurationManager();
            ((IConfigurationBuilder)cm).AllowReferences(r => r.Allow("Client1:Credential", "Shared:Credential"));
            cm["Shared:Credential"] = "secret";
            cm["Client1:Credential"] = "ref(Shared:Credential)";
            Assert.Equal("secret", cm["Client1:Credential"]);
        }

        // === ref(...) sigil syntax ===

        [Fact]
        public void RefSigil_ResolvesThrough()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Shared:Prod:Credential", "prod"),
                    ("Shared:Staging:Credential", "staging"),
                    ("Client1:Credential", "ref(Shared:Staging:Credential)")))
                .AllowReferences(r => r.Allow("Client1:Credential", "Shared:Prod:Credential").Allow("Client1:Credential", "Shared:Staging:Credential"))
                .Build();

            Assert.Equal("staging", root["Client1:Credential"]);
        }

        [Fact]
        public void RefSigil_NonSigilLiteralIsDirectValue()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Shared:Prod:Credential", "prod"),
                    ("Client1:Credential", "hunter2")))
                .AllowReferences(r => r.Allow("Client1:Credential", "Shared:Prod:Credential").Allow("Client1:Credential", "Shared:Staging:Credential"))
                .Build();

            Assert.Equal("hunter2", root["Client1:Credential"]);
        }

        [Fact]
        public void RefSigil_LiteralMatchingTargetWithoutSigilIsDirectValue()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Shared:Prod:Credential", "prod"),
                    ("Client1:Credential", "Shared:Prod:Credential")))
                .AllowReferences(r => r.Allow("Client1:Credential", "Shared:Prod:Credential").Allow("Client1:Credential", "Shared:Staging:Credential"))
                .Build();

            // Without the ref(...) sigil, even a string equal to a target is a direct value.
            Assert.Equal("Shared:Prod:Credential", root["Client1:Credential"]);
        }

        [Fact]
        public void RefSigil_OutOfRule_Throws()
        {
            IConfigurationBuilder b = BuilderWith(Dict(
                    ("Client1:Credential", "ref(Forbidden:Key)")))
                .AllowReferences(r => r.Allow("Client1:Credential", "Shared:Prod:Credential").Allow("Client1:Credential", "Shared:Staging:Credential"));

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => b.Build());
            Assert.Contains("Forbidden:Key", ex.Message);
        }

        [Fact]
        public void RefSigil_EmptySelection_ReturnsNull()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Shared:Prod:Credential", "prod"),
                    ("Shared:Staging:Credential", "staging")))
                .AllowReferences(r => r.Allow("Client1:Credential", "Shared:Prod:Credential").Allow("Client1:Credential", "Shared:Staging:Credential"))
                .Build();

            // Without a ref(...) literal at the subject, no resolution happens.
            Assert.Null(root["Client1:Credential"]);
        }

        [Theory]
        [InlineData("ref()")]
        [InlineData("ref( )")]
        [InlineData("REF(Shared:Prod:Credential)")]   // case-sensitive prefix
        [InlineData("ref(Shared:Prod:Credential")]    // missing closing paren
        public void RefSigil_MalformedIsDirectValue(string literal)
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Shared:Prod:Credential", "prod"),
                    ("Client1:Credential", literal)))
                .AllowReferences(r => r.Allow("Client1:Credential", "Shared:Prod:Credential").Allow("Client1:Credential", "Shared:Staging:Credential"))
                .Build();

            Assert.Equal(literal, root["Client1:Credential"]);
        }

        [Fact]
        public void RefSigil_WhitespaceInsideTrimmed()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Shared:Prod:Credential", "prod"),
                    ("Client1:Credential", "ref(  Shared:Prod:Credential  )")))
                .AllowReferences(r => r.Allow("Client1:Credential", "Shared:Prod:Credential").Allow("Client1:Credential", "Shared:Staging:Credential"))
                .Build();

            Assert.Equal("prod", root["Client1:Credential"]);
        }

        // === format(...) sigil syntax ===

        [Fact]
        public void FormatSigil_ComposesReferences()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Db:Host", "dbhost"),
                    ("Db:Name", "orders"),
                    ("App:Conn", "format(Server={0};Database={1}, Db:Host, Db:Name)")))
                .AllowReferences(r => r.Allow("App:Conn", "Db:*"))
                .Build();

            Assert.Equal("Server=dbhost;Database=orders", root["App:Conn"]);
        }

        [Fact]
        public void FormatSigil_SingleReference()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Db:Host", "dbhost"),
                    ("App:Url", "format(https://{0}/, Db:Host)")))
                .AllowReferences(r => r.Allow("App:Url", "Db:Host"))
                .Build();

            Assert.Equal("https://dbhost/", root["App:Url"]);
        }

        [Fact]
        public void FormatSigil_EscapedCommaInTemplateIsLiteralComma()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Db:Host", "dbhost"),
                    ("App:Conn", "format(Server={0}\\,1433, Db:Host)")))
                .AllowReferences(r => r.Allow("App:Conn", "Db:Host"))
                .Build();

            // "\," in the template is a literal comma, so it does not separate template from references.
            Assert.Equal("Server=dbhost,1433", root["App:Conn"]);
        }

        [Fact]
        public void FormatSigil_NoReferences_IsVerbatimTemplate()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("App:Text", "format(plain text {0})")))
                .AllowReferences(r => r.Allow("App:Text", "**"))
                .Build();

            // With no references the template is taken verbatim, so the placeholder is left untouched.
            Assert.Equal("plain text {0}", root["App:Text"]);
        }

        [Fact]
        public void FormatSigil_ReferenceWhitespaceTrimmed()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Db:Host", "dbhost"),
                    ("Db:Name", "orders"),
                    ("App:Conn", "format({0}/{1},   Db:Host ,  Db:Name )")))
                .AllowReferences(r => r.Allow("App:Conn", "Db:*"))
                .Build();

            Assert.Equal("dbhost/orders", root["App:Conn"]);
        }

        [Fact]
        public void FormatSigil_OutOfRuleReference_Throws()
        {
            IConfigurationBuilder b = BuilderWith(Dict(
                    ("App:Conn", "format({0}, Forbidden:Key)")))
                .AllowReferences(r => r.Allow("App:Conn", "Db:Host"));

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => b.Build());
            Assert.Contains("Forbidden:Key", ex.Message);
        }

        [Fact]
        public void FormatSigil_EscapedMarkerIsLiteral()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("App:Literal", "\\format({0}, Db:Host)")))
                .AllowReferences(r => r.Allow("App:Literal", "**"))
                .Build();

            Assert.Equal("format({0}, Db:Host)", root["App:Literal"]);
        }

        [Theory]
        [InlineData("format()")]
        [InlineData("FORMAT({0}, Db:Host)")]   // case-sensitive prefix
        [InlineData("format({0}, Db:Host")]    // missing closing paren
        public void FormatSigil_MalformedIsDirectValue(string literal)
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Db:Host", "dbhost"),
                    ("App:Conn", literal)))
                .AllowReferences(r => r.Allow("App:Conn", "Db:Host"))
                .Build();

            Assert.Equal(literal, root["App:Conn"]);
        }

        [Fact]
        public void Parser_SeesSubjectKey()
        {
            // Both subjects hold the same value and are permitted; the parser recognises a reference only for
            // Client1, keying off ctx.Key, so Client2 stays verbatim. This proves the subject key is threaded.
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Shared:Credential", "secret"),
                    ("Client1:Credential", "go"),
                    ("Client2:Credential", "go")))
                .AllowReferences(r =>
                {
                    r.Allow("Client1:Credential", "Shared:Credential").Allow("Client2:Credential", "Shared:Credential");
                    r.Parser = static ctx => ctx.Key == "Client1:Credential"
                        ? ConfigurationExpansion.Reference("Shared:Credential")
                        : null;
                })
                .Build();

            Assert.Equal("secret", root["Client1:Credential"]);
            Assert.Equal("go", root["Client2:Credential"]);
        }

        [Fact]
        public void Parser_CanInspectProvidersToChooseTarget()
        {
            // The recogniser probes the raw provider snapshot (no IConfiguration) to pick which key to name;
            // the framework still rule-checks whatever it names.
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Db:Primary", "primary"),
                    ("App:Conn", "pick")))
                .AllowReferences(r =>
                {
                    r.Allow("App:Conn", "Db:*");
                    r.Parser = static ctx =>
                    {
                        if (ctx.Value != "pick")
                        {
                            return null;
                        }
                        foreach (IConfigurationProvider p in ctx.Providers)
                        {
                            if (p.TryGet("Db:Primary", out _))
                            {
                                return ConfigurationExpansion.Reference("Db:Primary");
                            }
                        }
                        return ConfigurationExpansion.Reference("Db:Secondary");
                    };
                })
                .Build();

            Assert.Equal("primary", root["App:Conn"]);
        }

        [Fact]
        public void Literal_Null_YieldsNoValue()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("App:Path", "blank")))
                .AllowReferences(r =>
                {
                    r.Allow("App:Path", "**");
                    r.Parser = static ctx => ctx.Value == "blank" ? ConfigurationExpansion.Literal(null) : null;
                })
                .Build();

            Assert.Null(root["App:Path"]);
        }

        [Fact]
        public void Value_Spec_IsTakenVerbatimWithoutFormatting()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("App:Path", "literal")))
                .AllowReferences(r =>
                {
                    r.Allow("App:Path", "**");
                    r.Parser = static ctx => ctx.Value == "literal"
                        ? ConfigurationExpansion.Literal("C:\\Program Files\\{app}")
                        : null;
                })
                .Build();

            Assert.Equal("C:\\Program Files\\{app}", root["App:Path"]);
        }

        [Fact]
        public void Format_Spec_ComposesReferencedValues()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Db:Host", "host-1"),
                    ("Db:Name", "app"),
                    ("App:Conn", "compose")))
                .AllowReferences(r =>
                {
                    r.Allow("App:Conn", "Db:*");
                    r.Parser = static ctx => ctx.Value == "compose"
                        ? ConfigurationExpansion.Format("Server={0};Database={1}", "Db:Host", "Db:Name")
                        : null;
                })
                .Build();

            Assert.Equal("Server=host-1;Database=app", root["App:Conn"]);
        }

        [Fact]
        public void Format_Spec_WithNoReferencesYieldsTemplate()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("App:Greeting", "shout")))
                .AllowReferences(r =>
                {
                    r.Allow("App:Greeting", "**");
                    r.Parser = static ctx => ctx.Value == "shout"
                        ? ConfigurationExpansion.Format("HELLO")
                        : null;
                })
                .Build();

            Assert.Equal("HELLO", root["App:Greeting"]);
        }

        [Fact]
        public void Format_WithNoReferences_IsVerbatim_LikeValue()
        {
            // With no references there is nothing to compose, so a zero-reference Format is taken verbatim,
            // exactly like Value: no string.Format pass, so "{{x}}" is not unescaped.
            IConfigurationRoot root = BuilderWith(Dict(
                    ("App:Formatted", "fmt"),
                    ("App:Verbatim", "val")))
                .AllowReferences(r =>
                {
                    r.Allow("App:*", "**");
                    r.Parser = static ctx => ctx.Value switch
                    {
                        "fmt" => ConfigurationExpansion.Format("{{x}}"),
                        "val" => ConfigurationExpansion.Literal("{{x}}"),
                        _ => null,
                    };
                })
                .Build();

            Assert.Equal("{{x}}", root["App:Formatted"]);
            Assert.Equal("{{x}}", root["App:Verbatim"]);
        }

        [Fact]
        public void Format_Spec_OverIndexingTemplate_Throws()
        {
            // The template references {1} but only one reference is supplied. Composition formats from an
            // exact-length argument window, so this must throw rather than read a stale/empty slot.
            IConfigurationBuilder builder = BuilderWith(Dict(
                    ("Db:Only", "host-1"),
                    ("App:Conn", "compose")))
                .AllowReferences(r =>
                {
                    r.Allow("App:Conn", "Db:*");
                    r.Parser = static ctx => ctx.Value == "compose"
                        ? ConfigurationExpansion.Format("{0}-{1}", "Db:Only")
                        : null;
                });

            Assert.Throws<InvalidOperationException>(() => builder.Build());
        }

        [Fact]
        public void Format_Spec_ChainsThroughAnotherReference()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Db:RealHost", "host-1"),
                    ("Db:Host", "ref(Db:RealHost)"),
                    ("App:Conn", "compose")))
                .AllowReferences(r =>
                {
                    r.Allow("App:Conn", "Db:Host").Allow("Db:Host", "Db:RealHost");
                    r.Parser = static ctx => ctx.Value switch
                    {
                        "compose" => ConfigurationExpansion.Format("Server={0}", "Db:Host"),
                        _ when ctx.Value.StartsWith("ref(", StringComparison.Ordinal) && ctx.Value.EndsWith(")", StringComparison.Ordinal)
                            => ConfigurationExpansion.Reference(ctx.Value.Substring(4, ctx.Value.Length - 5)),
                        _ => null,
                    };
                })
                .Build();

            Assert.Equal("Server=host-1", root["App:Conn"]);
        }

        [Fact]
        public void Parser_CanDelegateToCapturedDefault()
        {
            // A custom recogniser handles its own "$ref " syntax and captures the default to fall back to it for
            // the built-in ref(...) form.
            IConfigurationRoot root = BuilderWith(Dict(
                    ("Shared:Credential", "secret"),
                    ("Client1:Credential", "$ref Shared:Credential"),
                    ("Client2:Credential", "ref(Shared:Credential)")))
                .AllowReferences(r =>
                {
                    r.Allow("Client1:Credential", "Shared:Credential").Allow("Client2:Credential", "Shared:Credential");

                    Func<ConfigurationReferenceContext, ConfigurationExpansion?> fallback = r.Parser;
                    r.Parser = ctx => ctx.Value.StartsWith("$ref ", StringComparison.Ordinal)
                        ? ConfigurationExpansion.Reference(ctx.Value.Substring("$ref ".Length).Trim())
                        : fallback(ctx);
                })
                .Build();

            Assert.Equal("secret", root["Client1:Credential"]);
            Assert.Equal("secret", root["Client2:Credential"]);
        }

        [Fact]
        public void Deny_VetoesAPermittedTarget()
        {
            IConfigurationBuilder b = BuilderWith(Dict(
                    ("Secrets:Public", "ok"),
                    ("Secrets:Private", "no"),
                    ("App:Value", "ref(Secrets:Private)")))
                .AllowReferences(r => r
                    .Allow("App:Value", "Secrets:*")
                    .Deny("App:Value", "Secrets:Private"));

            Assert.Throws<InvalidOperationException>(() => b.Build());
        }

        [Fact]
        public void EscapedMarker_IsTakenAsLiteral()
        {
            IConfigurationRoot root = BuilderWith(Dict(
                    ("App:Literal", "\\ref(NotAReference)")))
                .AllowReferences(r => r.Allow("App:Literal", "**"))
                .Build();

            Assert.Equal("ref(NotAReference)", root["App:Literal"]);
        }
    }
}
