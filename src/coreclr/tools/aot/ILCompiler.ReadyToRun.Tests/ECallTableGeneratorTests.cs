// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias crossgen2;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

using CoreCLR.Tools;
using crossgen2::ILCompiler;
using crossgen2::ILCompiler.PortableCallHelpers;

using ILCompiler.ReadyToRun.Tests.TestCasesRunner;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Xunit;

namespace ILCompiler.ReadyToRun.Tests;

public class ECallTableGeneratorTests
{
    private const string Prefix = "ecall_entry_";
    private const string Manifest = $$"""
        {
          "version": 1,
          "targetOS": "browser",
          "classes": [
            { "name": "Z", "namespace": "System", "entries": [
              { "name": "Unused", "signature": null, "symbol": "{{Prefix}}1" }
            ] },
            { "name": "A", "namespace": "Z", "entries": [
              { "name": "Shared", "signature": null, "symbol": "{{Prefix}}2" }
            ] },
            { "name": "A", "namespace": "A", "entries": [
              { "name": "First", "signature": null, "symbol": "{{Prefix}}3" },
              { "name": "Second", "signature": null, "symbol": "{{Prefix}}4" }
            ] },
            { "name": "String", "namespace": "System", "entries": [
              { "name": ".ctor", "signature": "instance System.Void(System.Char[])", "symbol": "{{Prefix}}5" },
              { "name": ".ctor", "signature": "instance System.Void(System.Char,System.Int32)", "symbol": "{{Prefix}}6" }
            ] }
          ]
        }
        """;

    [ConditionalFact(typeof(TestPaths), nameof(TestPaths.IsWasmTarget))]
    public void ECallManifestRetainsMatchingCoreLibInternalCalls()
    {
        string manifest = Path.Combine(AppContext.BaseDirectory, "ecall-manifest.json");
        Assert.True(File.Exists(manifest), $"Native ECall manifest not found: {manifest}");
        ReadyToRunCompilerContext context = WasmTestHelpers.CreateContext();
        var generator = new ECallTableGenerator(manifest, TestPaths.TargetOS);
        foreach (MetadataType type in context.SystemModule.GetAllTypes())
        {
            if (type is EcmaType ecmaType)
                generator.ScanType(ecmaType);
        }
        var writer = new StringWriter();
        generator.Emit(writer);
        string source = writer.ToString();
        Assert.Contains("\"String\", \"System\"", source);
        Assert.Contains("\"Math\", \"System\"", source);
        Assert.Contains("extern \"C\" const ECFunc ecall_entry_", source);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RetainsOnlySelectedDescriptorsInSortedNonemptyClasses(bool retainSecond)
    {
        WithManifest(Manifest, generator =>
        {
            generator.Retain("Z", "A", "Shared", "static System.Void()");
            generator.Retain("A", "A", retainSecond ? "Second" : "First", "static System.Void()");
            generator.Retain("System", "String", ".ctor", "instance System.Void(System.Char,System.Int32)");
            var writer = new StringWriter();
            generator.Emit(writer);
            string source = writer.ToString();

            Assert.Contains($"&{Prefix}2,", source);
            Assert.Contains($"&{Prefix}{(retainSecond ? 4 : 3)},", source);
            Assert.DoesNotContain($"{Prefix}{(retainSecond ? 3 : 4)}", source);
            Assert.Contains($"&{Prefix}6,", source);
            Assert.DoesNotContain($"{Prefix}5", source);
            Assert.DoesNotContain($"{Prefix}1", source);
            Assert.Contains("{ \"A\", \"A\", ecall_class_0 },", source);
            Assert.Contains("{ \"A\", \"Z\", ecall_class_1 },", source);
            Assert.Contains("{ \"String\", \"System\", ecall_class_2 },", source);
            Assert.Contains("extern const int c_nECClasses = 3;", source);
        });
    }

    [Fact]
    public void EmptyInputEmitsAValidEmptyTable()
    {
        WithManifest(Manifest, generator =>
        {
            var writer = new StringWriter();
            generator.Emit(writer);
            string source = writer.ToString();
            Assert.DoesNotContain(Prefix, source);
            Assert.Contains("{ nullptr, nullptr, nullptr }", source);
            Assert.Contains("extern const int c_nECClasses = 0;", source);
        });
    }

    [Fact]
    public void RepeatedRetentionIsIdempotentAndPreservesManifestEntryOrder()
    {
        WithManifest(Manifest, generator =>
        {
            generator.Retain("A", "A", "Second", "static System.Void()");
            var writer = new StringWriter();
            generator.Emit(writer);
            string retainedSecond = writer.ToString();

            generator.Retain("A", "A", "Second", "static System.Void()");
            writer.GetStringBuilder().Clear();
            generator.Emit(writer);
            Assert.Equal(retainedSecond, writer.ToString());

            generator.Retain("A", "A", "First", "static System.Void()");
            writer.GetStringBuilder().Clear();
            generator.Emit(writer);
            Assert.Contains($"    &{Prefix}3,{Environment.NewLine}    &{Prefix}4,", writer.ToString());
            Assert.Contains("extern const int c_nECClasses = 1;", writer.ToString());
        });
    }

    [Theory]
    [InlineData("System.<Nested>&'+", "\"System.<Nested>&'+\"")]
    [InlineData("System.\"Quoted\\Type", "\"System.\\\"Quoted\\\\Type\"")]
    [InlineData("System.\n\r\tType", "\"System.\\012\\015\\011Type\"")]
    [InlineData("System.\u00017", "\"System.\\0017\"")]
    [InlineData("System.\u03BB", "\"System.\\316\\273\"")]
    [InlineData("System.\U0001F600", "\"System.\\360\\237\\230\\200\"")]
    public void EmitsCppStringLiteralsWithTheOriginalUtf8Bytes(string ns, string expectedLiteral)
    {
        string manifest = Manifest.Replace("\"namespace\": \"Z\"", $"\"namespace\": \"{JsonEncodedText.Encode(ns)}\"");
        WithManifest(manifest, generator =>
        {
            generator.Retain(ns, "A", "Shared", "static System.Void()");
            var writer = new StringWriter();
            generator.Emit(writer);
            Assert.Contains($"{{ \"A\", {expectedLiteral}, ecall_class_0 }},", writer.ToString());
        });
    }

    [Theory]
    [InlineData("System", "Missing", "Missing", "static System.Void()")]
    [InlineData("System", "String", "Missing", "static System.Void()")]
    [InlineData("System", "String", ".ctor", "instance System.Void(System.SByte*)")]
    [InlineData("System", "Action", ".ctor", null)]
    [InlineData("System.Runtime.CompilerServices", "RuntimeHelpers", "NewobjHelperDummy", null)]
    public void MethodsAbsentFromTheManifestAreIgnored(string ns, string type, string method, string signature)
    {
        WithManifest(Manifest, generator =>
        {
            generator.Retain("A", "A", "First", "static System.Void()");
            var writer = new StringWriter();
            generator.Emit(writer);
            string expected = writer.ToString();

            generator.Retain(ns, type, method, signature);
            writer.GetStringBuilder().Clear();
            generator.Emit(writer);
            Assert.Equal(expected, writer.ToString());
        });
    }

    [Fact]
    public void AmbiguousMappingsFailRatherThanSelectingTheFirst()
    {
        WithManifest(Manifest.Replace("\"name\": \"Second\"", "\"name\": \"First\""), generator =>
        {
            LogAsErrorException error = Assert.Throws<LogAsErrorException>(() =>
                generator.Retain("A", "A", "First", "static System.Void()"));
            Assert.Contains("Ambiguous", error.Message);
        });
    }

    [Theory]
    [InlineData("\"version\": 1", "\"version\": 2")]
    [InlineData("\"targetOS\": \"browser\"", "\"targetOS\": \"wasi\"")]
    [InlineData("\"name\": \"Second\"", "\"name\": null")]
    [InlineData("\"name\": \"Second\"", "\"name\": \"\"")]
    [InlineData("\"name\": \"Second\"", "\"name\": 42")]
    [InlineData("\"name\": \"Second\",", "")]
    [InlineData("\"namespace\": \"System\"", "\"namespace\": null")]
    [InlineData("\"namespace\": \"System\",", "")]
    [InlineData("\"signature\": null,", "")]
    [InlineData("\"signature\": null", "\"signature\": 42")]
    [InlineData("\"symbol\":", "\"missingSymbol\":")]
    [InlineData("\"symbol\":", "\"symbol\": null, \"missingSymbol\":")]
    [InlineData("\"version\": 1,", "")]
    [InlineData("\"version\": 1", "\"version\": null")]
    [InlineData("\"targetOS\": \"browser\",", "")]
    [InlineData("\"classes\":", "\"missingClasses\":")]
    [InlineData("\"entries\":", "\"missingEntries\":")]
    [InlineData("ecall_entry_3", "invalid_entry_3")]
    [InlineData("ecall_entry_3", "ecall_entry_")]
    [InlineData("ecall_entry_3", "ecall_entry_-3")]
    [InlineData("ecall_entry_3", "ecall_entry_+3")]
    [InlineData("ecall_entry_3", "ecall_entry_3a")]
    [InlineData("ecall_entry_3", "ecall_entry_3_0")]
    [InlineData("ecall_entry_3", "ecall_entry_3 ")]
    [InlineData("ecall_entry_3", "ecall_entry_3\\n")]
    [InlineData("ecall_entry_3", "ecall_entry_\u0663")]
    [InlineData("ecall_entry_4", "ecall_entry_3")]
    public void InvalidManifestFails(string original, string replacement)
    {
        Assert.Throws<LogAsErrorException>(() => WithManifest(Manifest.Replace(original, replacement), _ => { }));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("""{"version": 1, "targetOS": "browser", "classes": null}""")]
    [InlineData("""{"version": 1, "targetOS": "browser", "classes": [null]}""")]
    [InlineData("""{"version": 1, "targetOS": "browser", "classes": [{"name": "A", "namespace": "System", "entries": null}]}""")]
    [InlineData("""{"version": 1, "targetOS": "browser", "classes": [{"name": "A", "namespace": "System", "entries": [null]}]}""")]
    public void InvalidManifestStructureFailsWithADiagnostic(string manifest)
    {
        LogAsErrorException error = Assert.Throws<LogAsErrorException>(() => WithManifest(manifest, _ => { }));
        Assert.Contains("Invalid ECall manifest", error.Message);
    }

    private static void WithManifest(string manifest, Action<ECallTableGenerator> action)
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, manifest);
            action(new ECallTableGenerator(path, "browser"));
        }
        finally
        {
            File.Delete(path);
        }
    }
}

public class ECallManifestGeneratorTests
{
    private const string Prefix = "ecall_entry_";
    private const string ECalls = """
        FCFuncStart(z)
            FCFuncElement("First", Native::Shared)
            FCFuncElement("Second", Native::Shared)
        FCFuncEnd()
        FCFuncStart(a)
            FCDynamicSig(COR_CTOR_METHOD_NAME, &gsig_IM_Chars, ECall::CtorCharArrayManaged)
            FCDynamic("Allocate", ECall::FastAllocateString)
        FCFuncEnd()
        FCFuncStart(empty)
        FCFuncEnd()
        FCClassElement("Z", g_SystemNS, z)
        FCClassElement("A", "System", a)
        FCClassElement("Empty", "System", empty)
        """;
    private const string Namespaces = """
        #define g_SystemNS "System"
        #define g_RuntimeNS g_SystemNS ".Runtime"
        #define g_InteropNS g_RuntimeNS ".InteropServices"
        #define g_TextNS g_SystemNS ".Text"
        """;
    private const string CoreLib = """
        DEFINE_CLASS(READONLY_SPAN, System, ReadOnlySpan`1)
        DEFINE_CLASS(ENCODING, Text, Encoding)
        """;
    private const string Metasigs = """
        #ifdef DEFINE_METASIG
        DEFINE_METASIG(IM(Chars, a(u), v))
        DEFINE_METASIG_T(IM(SpanChars, GI(g(READONLY_SPAN), 1, u), v))
        DEFINE_METASIG(SM(StaticChars, a(u), v))
        #endif
        """;

    [Theory]
    [InlineData("a(u)", new[] { "System.Char[]" })]
    [InlineData("a(u) i i", new[] { "System.Char[]", "System.Int32", "System.Int32" })]
    [InlineData("P(u)", new[] { "System.Char*" })]
    [InlineData("P(u) i i", new[] { "System.Char*", "System.Int32", "System.Int32" })]
    [InlineData("u i", new[] { "System.Char", "System.Int32" })]
    [InlineData("GI(g(READONLY_SPAN), 1, u)", new[] { "System.ReadOnlySpan`1<System.Char>" })]
    [InlineData("P(B)", new[] { "System.SByte*" })]
    [InlineData("P(B) i i", new[] { "System.SByte*", "System.Int32", "System.Int32" })]
    [InlineData("P(B) i i C(ENCODING)", new[] { "System.SByte*", "System.Int32", "System.Int32", "System.Text.Encoding" })]
    [InlineData("GI(g(READONLY_SPAN), 01, u)", new[] { "System.ReadOnlySpan`1<System.Char>" })]
    [InlineData("\t a ( r ( P ( u ) ) ) \r\n i ", new[] { "System.Char*&[]", "System.Int32" })]
    [InlineData(" \u2003i\u2003 ", new[] { "System.Int32" })]
    [InlineData("", new string[] { })]
    [InlineData(" \t\r\n ", new string[] { })]
    [InlineData("_", new string[] { })]
    [InlineData("_ \t _", new string[] { })]
    public void ReadsMetasigTypes(string signature, string[] expected)
    {
        var types = new Dictionary<string, string>
        {
            ["READONLY_SPAN"] = "System.ReadOnlySpan`1",
            ["ENCODING"] = "System.Text.Encoding",
        };
        Assert.Equal(expected, ECallManifestGenerator.ParseTypes(signature, types));
    }

    [Theory]
    [InlineData("P(u")]
    [InlineData("C(UNKNOWN)")]
    [InlineData("Unsupported(u)")]
    [InlineData("GI(g(UNKNOWN), 1, u)")]
    [InlineData("i@")]
    [InlineData("!i")]
    [InlineData("a(u)#")]
    [InlineData("_unknown")]
    [InlineData("i\u00E9")]
    [InlineData("GI(g(READONLY_SPAN), 1x, u)")]
    public void InvalidMetasigsFail(string signature)
    {
        Assert.Throws<InvalidDataException>(() => ECallManifestGenerator.ParseTypes(signature, new Dictionary<string, string>()));
    }

    [Fact]
    public void ResolvesNamespaceDefinitionsWithNestedPrefixes()
    {
        using JsonDocument manifest = JsonDocument.Parse(Generate(ECalls.Replace("\"System\", a", "g_InteropNS, a")));
        Assert.Equal("System.Runtime.InteropServices", manifest.RootElement.GetProperty("classes")[0].GetProperty("namespace").GetString());
    }

    [Fact]
    public void ExportsNativeDescriptorSymbolsAndOverloadSignaturesInSortedNonemptyClasses()
    {
        string result = Generate();
        using JsonDocument manifest = JsonDocument.Parse(result);
        JsonElement classes = manifest.RootElement.GetProperty("classes");
        Assert.Equal(2, classes.GetArrayLength());
        Assert.Equal("A", classes[0].GetProperty("name").GetString());
        Assert.Equal("Z", classes[1].GetProperty("name").GetString());
        Assert.Equal("instance System.Void(System.Char[])", classes[0].GetProperty("entries")[0].GetProperty("signature").GetString());
        Assert.Equal(Prefix + "6", classes[0].GetProperty("entries")[0].GetProperty("symbol").GetString());
        Assert.Equal(Prefix + "2", classes[1].GetProperty("entries")[0].GetProperty("symbol").GetString());
        Assert.Equal(Prefix + "3", classes[1].GetProperty("entries")[1].GetProperty("symbol").GetString());
        Assert.Equal(result, Generate(ECalls.ReplaceLineEndings("\r\n")));
        using JsonDocument wasiManifest = JsonDocument.Parse(Generate(targetOS: "wasi"));
        Assert.Equal("wasi", wasiManifest.RootElement.GetProperty("targetOS").GetString());
        Assert.Equal(classes.GetRawText(), wasiManifest.RootElement.GetProperty("classes").GetRawText());
    }

    [Theory]
    [InlineData("browser")]
    [InlineData("wasi")]
    public void ExportedManifestUsesSharedSchemaAndRoundTripsThroughTableGenerator(string targetOS)
    {
        string result = Generate(targetOS: targetOS);
        ECallManifest manifest = Assert.IsType<ECallManifest>(
            JsonSerializer.Deserialize(result, ECallManifestJsonContext.Default.ECallManifest));
        Assert.Equal(1, manifest.Version);
        Assert.Equal(targetOS, manifest.TargetOS);
        Assert.NotNull(manifest.Classes);
        Assert.Equal(2, manifest.Classes.Length);
        ECallManifestClass cls = Assert.IsType<ECallManifestClass>(manifest.Classes[1]);
        Assert.Equal("Z", cls.Name);
        Assert.Equal("System", cls.Namespace);
        Assert.NotNull(cls.Entries);
        ECallManifestEntry entry = Assert.IsType<ECallManifestEntry>(cls.Entries[0]);
        Assert.Equal("First", entry.Name);
        Assert.Null(entry.Signature);
        Assert.Equal(Prefix + "2", entry.Symbol);

        WithDirectory(directory =>
        {
            string path = Path.Combine(directory, "ecall-manifest.json");
            File.WriteAllText(path, result);
            var generator = new ECallTableGenerator(path, targetOS);
            generator.Retain("System", "A", ".ctor", "instance System.Void(System.Char[])");
            generator.Retain("System", "Z", "First", "static System.Void()");
            var writer = new StringWriter();
            generator.Emit(writer);
            string source = writer.ToString();
            Assert.Contains($"&{Prefix}2,", source);
            Assert.DoesNotContain(Prefix + "3", source);
            Assert.Contains($"&{Prefix}6,", source);
            Assert.Contains("extern const int c_nECClasses = 2;", source);
            return 0;
        });
    }

    [Fact]
    public void SafelyEscapesGenericSignaturesWithoutChangingTheirValues()
    {
        string result = Generate(ECalls.Replace("&gsig_IM_Chars", "&gsig_IM_SpanChars"));
        Assert.Contains("\\u003CSystem.Char\\u003E", result);
        using JsonDocument manifest = JsonDocument.Parse(result);
        JsonElement entry = manifest.RootElement.GetProperty("classes")[0].GetProperty("entries")[0];
        Assert.Equal("instance System.Void(System.ReadOnlySpan`1<System.Char>)", entry.GetProperty("signature").GetString());
    }

    [Theory]
    [InlineData("FCFuncStart(a)")]
    [InlineData("FCFuncEnd()")]
    [InlineData("FCFuncStart(a)\nFCFuncStart(b)")]
    [InlineData("FCFuncStart(a)\nFCFuncEnd()")]
    [InlineData("FCFuncStart(a)\nFCFuncEnd()\nFCFuncStart(a)")]
    [InlineData("FCFuncElement(\"Outside\", Native::Method)")]
    [InlineData("FCClassElement(\"A\", \"System\", missing)")]
    [InlineData("FCFuncStart(a)\nFCClassElement(\"A\", \"System\", a)")]
    [InlineData("Unexpected()")]
    [InlineData("#include \"other.h\"")]
    [InlineData("#if defined(FEATURE_ON)\n#endif")]
    [InlineData("#ifdef FEATURE_ON\n#elif FEATURE_OTHER\n#endif")]
    [InlineData("#ifdef FEATURE_ON\n#else\n#else\n#endif")]
    [InlineData("#ifdef FEATURE_ON")]
    [InlineData("#else")]
    [InlineData("#endif")]
    [InlineData("/* unterminated")]
    [InlineData("FCFuncStart(a)\\")]
    public void InvalidHeadersFailWithADiagnostic(string source)
    {
        Assert.Throws<InvalidDataException>(() => Generate(source));
    }

    [Theory]
    [InlineData("Native::Shared)", "Native::Shared, Other)")]
    [InlineData("\"First\"", "Unquoted")]
    [InlineData("\"First\"", "\"\"")]
    [InlineData("\"First\"", "\"First\\u0020\"")]
    [InlineData("\"First\"", "\"First\\\"Quoted\"")]
    [InlineData("\"First\"", "\"First\" \"Second\"")]
    [InlineData("FCFuncStart(", "0FCFuncStart(")]
    [InlineData("FCFuncStart(", "FCFuncStart!(")]
    [InlineData("FCFuncStart(", "FCFuncStart extra(")]
    [InlineData("FCFuncStart(", "\u00E9FCFuncStart(")]
    [InlineData("&gsig_IM_Chars", "&gsig_IM_Missing")]
    [InlineData("&gsig_IM_Chars", "IM_Chars")]
    [InlineData("FCFuncElement(", "FCUnreferenced(")]
    [InlineData("FCFuncEnd()", "FCFuncEnd(unexpected)")]
    [InlineData("FCClassElement(\"Empty\", \"System\", empty)", "FCClassElement(\"Z\", \"System\", empty)")]
    [InlineData("FCClassElement(\"Empty\", \"System\", empty)", "FCClassElement(\"Empty\", \"System\", z)")]
    [InlineData("FCClassElement(\"Empty\", \"System\", empty)", "FCClassElement(\"Empty\", g_MissingNS, empty)")]
    public void MalformedMappingsFailRatherThanDroppingEntries(string original, string replacement)
    {
        Assert.Throws<InvalidDataException>(() => Generate(ECalls.Replace(original, replacement)));
    }

    [Theory]
    [InlineData("FCFuncStart")]
    [InlineData("FCFuncEnd")]
    [InlineData("FCFuncElement")]
    [InlineData("FCClassElement")]
    [InlineData("FCDynamic")]
    [InlineData("FCDynamicSig")]
    public void AllowsWhitespaceBetweenMacroNamesAndArguments(string macro)
    {
        Assert.Equal(Generate(), Generate(ECalls.Replace(macro + "(", macro + " \t(")));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TracksNestedConditionsWithoutRenumberingDescriptorSymbols(bool enabled)
    {
        string source = """
            FCFuncStart(a)
            #ifdef FEATURE_ON
            #ifndef FEATURE_OFF // comment
            FCFuncElement("Enabled", Native::Enabled)
            #endif
            #else
            #if FEATURE_OFF
            FCFuncElement("InactiveNestedElse", Native::Unused)
            #else
            FCFuncElement("Disabled", Native::Disabled)
            #endif
            #endif
            FCFuncEnd()
            FCClassElement("A", "System", a)
            """;
        string manifest = Generate(source, defines: enabled ? ["FEATURE_ON"] : []);
        Assert.Contains(enabled ? "\"Enabled\"" : "\"Disabled\"", manifest);
        Assert.DoesNotContain(enabled ? "\"Disabled\"" : "\"Enabled\"", manifest);
        Assert.DoesNotContain("InactiveNestedElse", manifest);
        Assert.Contains(Prefix + (enabled ? "4" : "10"), manifest);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExcludesDisabledAndEmptyClasses(bool enabled)
    {
        string source = "#ifdef FEATURE_ON\n" + ECalls + "\n#endif\n";
        using JsonDocument manifest = JsonDocument.Parse(Generate(source, defines: enabled ? ["FEATURE_ON"] : []));
        Assert.Equal(enabled ? 2 : 0, manifest.RootElement.GetProperty("classes").GetArrayLength());
    }

    [Theory]
    [InlineData("a(u)", "instance System.Void(System.Char[])")]
    [InlineData("GI(g(READONLY_SPAN), 1, u)", "instance System.Void(System.ReadOnlySpan`1<System.Char>)")]
    [InlineData("P(B) i i C(ENCODING)", "instance System.Void(System.SByte*,System.Int32,System.Int32,System.Text.Encoding)")]
    public void ResolvesReferencedSignaturesAndBinderTypes(string args, string expected)
    {
        string metasigs = "DEFINE_METASIG_T(IM(Chars, " + args + ", v))";
        using JsonDocument manifest = JsonDocument.Parse(Generate(metasigs: metasigs));
        JsonElement entry = manifest.RootElement.GetProperty("classes")[0].GetProperty("entries")[0];
        Assert.Equal(expected, entry.GetProperty("signature").GetString());
    }

    [Fact]
    public void ResolvesStaticSignatureEntries()
    {
        string source = ECalls.Replace("FCDynamicSig(COR_CTOR_METHOD_NAME, &gsig_IM_Chars",
            "FCFuncElementSig(\"StaticMethod\", &gsig_SM_StaticChars");
        Assert.Contains("static System.Void(System.Char[])", Generate(source));
    }

    [Theory]
    [InlineData("metasig", "DEFINE_METASIG(IM(Chars, a(u), v v))")]
    [InlineData("metasig", "DEFINE_METASIG(IM(Chars, a(u), v))\nDEFINE_METASIG(IM(Chars, a(u), v))")]
    [InlineData("corelib", "DEFINE_CLASS(READONLY_SPAN, Missing, ReadOnlySpan`1)")]
    [InlineData("corelib", "DEFINE_CLASS(READONLY_SPAN, System, ReadOnlySpan`1)\nDEFINE_CLASS(READONLY_SPAN, System, Other)")]
    [InlineData("namespace", "#define g_NestedNS g_MissingNS \".Nested\"")]
    [InlineData("namespace", "#define invalid")]
    public void InvalidSupportingHeadersFailWithADiagnostic(string header, string source)
    {
        Assert.Throws<InvalidDataException>(() => Generate(
            metasigs: header == "metasig" ? source : Metasigs,
            corelib: header == "corelib" ? source : CoreLib,
            namespaces: header == "namespace" ? source : Namespaces));
    }

    [Fact]
    public void HandlesCommentsAndContinuedSignaturesAndPreservesPhysicalEntryLines()
    {
        string source = "// FCFuncElement(\"Ignore\", Native::Ignore)\n/* FCFuncStart(ignore)\nFCFuncEnd() */\n" +
            ECalls.Replace("FCFuncElement(\"First\", Native::Shared)", "FCFuncElement(/* comment */ \"First\", Native::Shared) // comment");
        string metasigs = "#ifdef DEFINE_METASIG\nDEFINE_METASIG(IM(Chars, \\\n a(u), v))\n#endif";
        string manifest = Generate(source, metasigs: metasigs);
        Assert.Contains(Prefix + "5", manifest);
        Assert.Contains("instance System.Void(System.Char[])", manifest);
        Assert.DoesNotContain("Ignore", manifest);
    }

    [Theory]
    [InlineData("FCFuncElement(\"First\", Native::Shared)", 2)]
    [InlineData("FCFuncElement( \\\n\"First\", \\\nNative::Shared)", 4)]
    public void DescriptorSymbolsUseTheFinalPhysicalLineOfMacroExpansions(string entry, int expectedLine)
    {
        string source = $"FCFuncStart(table)\n{entry}\nFCFuncEnd()\nFCClassElement(\"C\", \"System\", table)";
        using JsonDocument manifest = JsonDocument.Parse(Generate(source));
        JsonElement entries = manifest.RootElement.GetProperty("classes")[0].GetProperty("entries");
        Assert.Equal(Prefix + expectedLine, entries[0].GetProperty("symbol").GetString());
    }

    [Fact]
    public void EmptyMappingEmitsValidEmptyTables()
    {
        string result = Generate("");
        using JsonDocument manifest = JsonDocument.Parse(result);
        Assert.Equal(0, manifest.RootElement.GetProperty("classes").GetArrayLength());
    }

    [Theory]
    [InlineData("first\nsecond\n")]
    [InlineData("\u03BB\n")]
    public void WritesUtf8WithoutBomAndPreservesUnchangedTimestamps(string content)
    {
        WithDirectory(directory =>
        {
            string path = Path.Combine(directory, "output.cpp");
            ECallManifestGenerator.WriteIfChanged(path, content);
            Assert.Equal(Encoding.UTF8.GetBytes(content), File.ReadAllBytes(path));
            DateTime timestamp = File.GetLastWriteTimeUtc(path);
            ECallManifestGenerator.WriteIfChanged(path, content);
            Assert.Equal(timestamp, File.GetLastWriteTimeUtc(path));
            return 0;
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CommandLineSupportsDirectAndResponseArgumentsAndRegeneratesChangedInput(bool useResponseFile)
    {
        WithSources(directory =>
        {
            string output = Path.Combine(directory, "generated files");
            string response = Path.Combine(directory, "arguments.rsp");
            string[] arguments = ["--source", directory, "--output", output, "--target-os", "browser",
                "--defines", "FEATURE_ON"];
            File.WriteAllLines(response, arguments);
            if (useResponseFile)
                arguments = ["@" + response];

            Assert.Equal(0, ECallManifestGenerator.Run(arguments));
            AssertOutputs();
            string manifestPath = Path.Combine(output, "ecall-manifest.json");
            DateTime timestamp = File.GetLastWriteTimeUtc(manifestPath);
            Assert.Equal(0, ECallManifestGenerator.Run(arguments));
            Assert.Equal(timestamp, File.GetLastWriteTimeUtc(manifestPath));
            File.WriteAllText(Path.Combine(directory, "ecalllist.h"), ECalls.Replace("First", "UpdatedMethod"));
            Assert.Equal(0, ECallManifestGenerator.Run(arguments));
            AssertOutputs();
            return 0;

            void AssertOutputs()
            {
                string result = ECallManifestGenerator.Generate(directory, "browser", ["FEATURE_ON"]);
                Assert.Single(Directory.GetFiles(output));
                Assert.Equal(result, File.ReadAllText(Path.Combine(output, "ecall-manifest.json")));
            }
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData("--unknown")]
    [InlineData("--table-id unused")]
    [InlineData("--source")]
    [InlineData("--source first --source second")]
    public void InvalidCommandLinesFail(string arguments)
    {
        Assert.NotEqual(0, ECallManifestGenerator.Run(arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries)));
    }

    [Fact]
    public void RecursiveResponseFilesFail()
    {
        WithDirectory(directory =>
        {
            string path = Path.Combine(directory, "recursive.args");
            File.WriteAllText(path, "@" + path);
            Assert.NotEqual(0, ECallManifestGenerator.Run(["@" + path]));
            return 0;
        });
    }

    private static string Generate(string source = ECalls, string metasigs = Metasigs,
        string corelib = CoreLib, string namespaces = Namespaces, string targetOS = "browser", string[]? defines = null)
    {
        return WithSources(directory =>
        {
            return ECallManifestGenerator.Generate(directory, targetOS, defines ?? []);
        }, source, metasigs, corelib, namespaces);
    }

    private static T WithSources<T>(Func<string, T> action, string source = ECalls, string metasigs = Metasigs,
        string corelib = CoreLib, string namespaces = Namespaces)
    {
        return WithDirectory(directory =>
        {
            File.WriteAllText(Path.Combine(directory, "ecalllist.h"), source);
            File.WriteAllText(Path.Combine(directory, "metasig.h"), metasigs);
            File.WriteAllText(Path.Combine(directory, "corelib.h"), corelib);
            File.WriteAllText(Path.Combine(directory, "namespace.h"), namespaces);
            return action(directory);
        });
    }

    private static T WithDirectory<T>(Func<string, T> action)
    {
        string directory = Directory.CreateTempSubdirectory("ecall generation ").FullName;
        try
        {
            return action(directory);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
