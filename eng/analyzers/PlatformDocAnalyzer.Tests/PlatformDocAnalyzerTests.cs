// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.DotNet.Analyzers.PlatformDoc;
using Xunit;

namespace PlatformDocAnalyzer.Tests
{
    using AnalyzerTest = CSharpAnalyzerTest<Microsoft.DotNet.Analyzers.PlatformDoc.PlatformDocAnalyzer, DefaultVerifier>;

    public class PlatformDocAnalyzerTests
    {
        private static readonly (string Key, string Value)[] s_platformTfmOptions = new[]
        {
            ("build_property.TargetFramework", "net10.0-windows"),
            ("build_property.UseCompilerGeneratedDocXmlFile", "true"),
        };

        private static readonly (string Key, string Value)[] s_nonPlatformTfmOptions = new[]
        {
            ("build_property.TargetFramework", "net10.0"),
            ("build_property.UseCompilerGeneratedDocXmlFile", "true"),
        };

        private static AnalyzerTest CreateTest(
            (string Key, string Value)[] globalOptions,
            params (string FileName, string Source)[] sources)
        {
            var test = new AnalyzerTest
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
            };

            foreach ((string fileName, string source) in sources)
            {
                test.TestState.Sources.Add((fileName, source));
            }

            string optionsText = "is_global = true\r\n";
            foreach ((string key, string value) in globalOptions)
            {
                optionsText += $"{key} = {value}\r\n";
            }

            test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", optionsText));
            return test;
        }

        [Fact]
        public async Task NoDiagnosticForNonPlatformTfm()
        {
            // A non-platform TFM should not trigger any diagnostics even if naming is wrong.
            var test = CreateTest(
                s_nonPlatformTfmOptions,
                ("WrongName.cs", @"
public class Foo
{
    /// <summary>Some docs</summary>
    public void Bar() { }
}"));

            await test.RunAsync(CancellationToken.None);
        }

        [Fact]
        public async Task NoDiagnosticWhenUseCompilerGeneratedDocXmlFileIsFalse()
        {
            var options = new[]
            {
                ("build_property.TargetFramework", "net10.0-windows"),
                ("build_property.UseCompilerGeneratedDocXmlFile", "false"),
            };

            var test = CreateTest(
                options,
                ("WrongName.cs", @"
public class Foo
{
    /// <summary>Some docs</summary>
    public void Bar() { }
}"));

            await test.RunAsync(CancellationToken.None);
        }

        [Fact]
        public async Task NoDiagnosticForCorrectSetup()
        {
            // Primary file matches type name, no docs on partial files.
            var test = CreateTest(
                s_platformTfmOptions,
                ("Foo.cs", @"
/// <summary>Foo type</summary>
public partial class Foo
{
    /// <summary>Bar method</summary>
    public void Bar() { }
}"),
                ("Foo.Windows.cs", @"
public partial class Foo
{
    public void PlatformSpecificMethod() { }
}"));

            await test.RunAsync(CancellationToken.None);
        }

        [Fact]
        public async Task PLATDOC001_PublicTypeMissingPrimaryFile()
        {
            var test = CreateTest(
                s_platformTfmOptions,
                ("Foo.Windows.cs", @"
public partial class {|#0:Foo|}
{
    public void Bar() { }
}"),
                ("Foo.Unix.cs", @"
public partial class {|#1:Foo|}
{
    public void Baz() { }
}"));

            test.ExpectedDiagnostics.Add(new DiagnosticResult(Microsoft.DotNet.Analyzers.PlatformDoc.PlatformDocAnalyzer.MissingPrimaryFileRule)
                .WithLocation(0).WithArguments("Foo"));
            test.ExpectedDiagnostics.Add(new DiagnosticResult(Microsoft.DotNet.Analyzers.PlatformDoc.PlatformDocAnalyzer.MissingPrimaryFileRule)
                .WithLocation(1).WithArguments("Foo"));

            await test.RunAsync(CancellationToken.None);
        }

        [Fact]
        public async Task PLATDOC002_BadPartialFileName()
        {
            var test = CreateTest(
                s_platformTfmOptions,
                ("Foo.cs", @"
/// <summary>Foo type</summary>
public partial class Foo
{
    /// <summary>Bar method</summary>
    public void Bar() { }
}"),
                ("Helpers.cs", @"
public partial class {|#0:Foo|}
{
    public void PlatformSpecificHelper() { }
}"));

            test.ExpectedDiagnostics.Add(new DiagnosticResult(Microsoft.DotNet.Analyzers.PlatformDoc.PlatformDocAnalyzer.BadPartialFileNameRule)
                .WithLocation(0).WithArguments("Helpers.cs", "Foo"));

            await test.RunAsync(CancellationToken.None);
        }

        [Fact]
        public async Task PLATDOC003_DocsOnNonPrimaryFile()
        {
            var test = CreateTest(
                s_platformTfmOptions,
                ("Foo.cs", @"
/// <summary>Foo type</summary>
public partial class Foo
{
    /// <summary>Bar method</summary>
    public void Bar() { }
}"),
                ("Foo.Windows.cs", @"
public partial class Foo
{
    /// <summary>This doc should be in Foo.cs</summary>
    public void {|#0:PlatformSpecificMethod|}() { }
}"));

            test.ExpectedDiagnostics.Add(new DiagnosticResult(Microsoft.DotNet.Analyzers.PlatformDoc.PlatformDocAnalyzer.DocsOnNonPrimaryFileRule)
                .WithLocation(0).WithArguments("PlatformSpecificMethod", "Foo.Windows.cs", "Foo"));

            await test.RunAsync(CancellationToken.None);
        }

        [Fact]
        public async Task PLATDOC003_DocsOnNonPrimaryFile_MultipleMembers()
        {
            var test = CreateTest(
                s_platformTfmOptions,
                ("MyService.cs", @"
/// <summary>MyService type</summary>
public partial class MyService
{
    /// <summary>Start method</summary>
    public void Start() { }
}"),
                ("MyService.Windows.cs", @"
public partial class MyService
{
    /// <summary>Windows-specific start</summary>
    public void {|#0:StartWindows|}() { }

    /// <summary>Windows handle</summary>
    public int {|#1:Handle|} { get; set; }

    // No docs - this is fine
    public void InternalHelper() { }
}"));

            test.ExpectedDiagnostics.Add(new DiagnosticResult(Microsoft.DotNet.Analyzers.PlatformDoc.PlatformDocAnalyzer.DocsOnNonPrimaryFileRule)
                .WithLocation(0).WithArguments("StartWindows", "MyService.Windows.cs", "MyService"));
            test.ExpectedDiagnostics.Add(new DiagnosticResult(Microsoft.DotNet.Analyzers.PlatformDoc.PlatformDocAnalyzer.DocsOnNonPrimaryFileRule)
                .WithLocation(1).WithArguments("Handle", "MyService.Windows.cs", "MyService"));

            await test.RunAsync(CancellationToken.None);
        }

        [Fact]
        public async Task NoDiagnosticForNonPublicType()
        {
            var test = CreateTest(
                s_platformTfmOptions,
                ("WrongName.cs", @"
internal class Foo
{
    /// <summary>Some docs</summary>
    public void Bar() { }
}"));

            await test.RunAsync(CancellationToken.None);
        }

        [Fact]
        public async Task NoDiagnosticForPrivateMemberDocs()
        {
            // Private/internal members in non-primary files can have docs
            var test = CreateTest(
                s_platformTfmOptions,
                ("Foo.cs", @"
/// <summary>Foo type</summary>
public partial class Foo
{
    /// <summary>Bar method</summary>
    public void Bar() { }
}"),
                ("Foo.Windows.cs", @"
public partial class Foo
{
    /// <summary>Private helper - docs are fine here</summary>
    private void PrivateHelper() { }

    /// <summary>Internal helper - docs are fine here</summary>
    internal void InternalHelper() { }
}"));

            await test.RunAsync(CancellationToken.None);
        }

        [Fact]
        public async Task NoDiagnosticForNestedType()
        {
            // Nested types are not checked (only top-level types are)
            var test = CreateTest(
                s_platformTfmOptions,
                ("Outer.cs", @"
/// <summary>Outer type</summary>
public class Outer
{
    /// <summary>Inner type</summary>
    public class Inner
    {
        /// <summary>Method</summary>
        public void Method() { }
    }
}"));

            await test.RunAsync(CancellationToken.None);
        }

        [Fact]
        public async Task PLATDOC003_InterfaceMembersAreImplicitlyPublic()
        {
            var test = CreateTest(
                s_platformTfmOptions,
                ("IService.cs", @"
/// <summary>IService interface</summary>
public partial interface IService
{
    /// <summary>Start method</summary>
    void Start();
}"),
                ("IService.Windows.cs", @"
public partial interface IService
{
    /// <summary>Windows-specific method</summary>
    void {|#0:StartWindows|}();
}"));

            test.ExpectedDiagnostics.Add(new DiagnosticResult(Microsoft.DotNet.Analyzers.PlatformDoc.PlatformDocAnalyzer.DocsOnNonPrimaryFileRule)
                .WithLocation(0).WithArguments("StartWindows", "IService.Windows.cs", "IService"));

            await test.RunAsync(CancellationToken.None);
        }

        [Fact]
        public async Task PLATDOC001_StructType()
        {
            var test = CreateTest(
                s_platformTfmOptions,
                ("WrongName.cs", @"
public partial struct {|#0:MyStruct|}
{
    public int Value;
}"),
                ("WrongName2.cs", @"
public partial struct {|#1:MyStruct|}
{
    public int Value2;
}"));

            test.ExpectedDiagnostics.Add(new DiagnosticResult(Microsoft.DotNet.Analyzers.PlatformDoc.PlatformDocAnalyzer.MissingPrimaryFileRule)
                .WithLocation(0).WithArguments("MyStruct"));
            test.ExpectedDiagnostics.Add(new DiagnosticResult(Microsoft.DotNet.Analyzers.PlatformDoc.PlatformDocAnalyzer.MissingPrimaryFileRule)
                .WithLocation(1).WithArguments("MyStruct"));
            test.ExpectedDiagnostics.Add(new DiagnosticResult(Microsoft.DotNet.Analyzers.PlatformDoc.PlatformDocAnalyzer.BadPartialFileNameRule)
                .WithLocation(0).WithArguments("WrongName.cs", "MyStruct"));
            test.ExpectedDiagnostics.Add(new DiagnosticResult(Microsoft.DotNet.Analyzers.PlatformDoc.PlatformDocAnalyzer.BadPartialFileNameRule)
                .WithLocation(1).WithArguments("WrongName2.cs", "MyStruct"));

            await test.RunAsync(CancellationToken.None);
        }

        [Fact]
        public async Task NoDiagnosticForSingleFileType()
        {
            // A type defined in only one file should not trigger any diagnostics,
            // even if the file name doesn't match the type name.
            var test = CreateTest(
                s_platformTfmOptions,
                ("Helpers.cs", @"
/// <summary>Some type</summary>
public class Foo
{
    /// <summary>Some method</summary>
    public void Bar() { }
}"));

            await test.RunAsync(CancellationToken.None);
        }

        [Fact]
        public async Task NoDiagnosticForNestedTypeDocsOnNonPrimaryFile()
        {
            // Nested type declarations define their own doc scope and should not be flagged.
            var test = CreateTest(
                s_platformTfmOptions,
                ("Outer.cs", @"
/// <summary>Outer type</summary>
public partial class Outer
{
    /// <summary>Method</summary>
    public void Method() { }
}"),
                ("Outer.Inner.cs", @"
public partial class Outer
{
    /// <summary>Nested type docs are fine here</summary>
    public class Inner
    {
        /// <summary>Inner method</summary>
        public void InnerMethod() { }
    }
}"));

            await test.RunAsync(CancellationToken.None);
        }

        private static AnalyzerTest CreateTestWithCanonicalDoc(
            (string Key, string Value)[] globalOptions,
            string canonicalDocXml,
            params (string FileName, string Source)[] sources)
        {
            var test = CreateTest(globalOptions, sources);

            // Replace the existing globalconfig with one that also configures the additional file.
            test.TestState.AnalyzerConfigFiles.Clear();
            string optionsText = "is_global = true\r\n";
            foreach ((string key, string value) in globalOptions)
            {
                optionsText += $"{key} = {value}\r\n";
            }
            optionsText += "[/canonical.xml]\r\n";
            optionsText += "build_metadata.AdditionalFiles.PlatformDocCanonical = true\r\n";
            test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", optionsText));

            test.TestState.AdditionalFiles.Add(("/canonical.xml", canonicalDocXml));
            return test;
        }

        [Fact]
        public async Task PLATDOC004_DocsMatchCanonical_NoDiagnostic()
        {
            string canonicalDocXml = @"<?xml version=""1.0""?>
<doc>
  <assembly><name>TestAssembly</name></assembly>
  <members>
    <member name=""T:Foo"">
      <summary>Foo type</summary>
    </member>
    <member name=""M:Foo.Bar"">
      <summary>Bar method</summary>
    </member>
  </members>
</doc>";

            var test = CreateTestWithCanonicalDoc(
                s_platformTfmOptions,
                canonicalDocXml,
                ("Foo.cs", @"
/// <summary>Foo type</summary>
public class Foo
{
    /// <summary>Bar method</summary>
    public void Bar() { }
}"));

            await test.RunAsync(CancellationToken.None);
        }

        [Fact]
        public async Task PLATDOC004_DocsDifferFromCanonical()
        {
            string canonicalDocXml = @"<?xml version=""1.0""?>
<doc>
  <assembly><name>TestAssembly</name></assembly>
  <members>
    <member name=""T:Foo"">
      <summary>Foo type</summary>
    </member>
    <member name=""M:Foo.Bar"">
      <summary>Bar method</summary>
    </member>
  </members>
</doc>";

            var test = CreateTestWithCanonicalDoc(
                s_platformTfmOptions,
                canonicalDocXml,
                ("Foo.cs", @"
/// <summary>Foo type</summary>
public class Foo
{
    /// <summary>DIFFERENT Bar method docs</summary>
    public void {|#0:Bar|}() { }
}"));

            test.ExpectedDiagnostics.Add(new DiagnosticResult(Microsoft.DotNet.Analyzers.PlatformDoc.PlatformDocAnalyzer.DocMismatchRule)
                .WithLocation(0).WithArguments("Foo.Bar()"));

            await test.RunAsync(CancellationToken.None);
        }

        [Fact]
        public async Task PLATDOC004_PlatformOnlyApi_NoDiagnostic()
        {
            // APIs that don't exist in the canonical doc XML are platform-specific and can have any docs.
            string canonicalDocXml = @"<?xml version=""1.0""?>
<doc>
  <assembly><name>TestAssembly</name></assembly>
  <members>
    <member name=""T:Foo"">
      <summary>Foo type</summary>
    </member>
  </members>
</doc>";

            var test = CreateTestWithCanonicalDoc(
                s_platformTfmOptions,
                canonicalDocXml,
                ("Foo.cs", @"
/// <summary>Foo type</summary>
public class Foo
{
    /// <summary>Platform-specific method not in canonical</summary>
    public void WindowsOnly() { }
}"));

            await test.RunAsync(CancellationToken.None);
        }

        [Fact]
        public async Task PLATDOC004_NoCanonicalDoc_NoDiagnostic()
        {
            // When no canonical doc XML is provided, PLATDOC004 should not fire.
            var test = CreateTest(
                s_platformTfmOptions,
                ("Foo.cs", @"
/// <summary>DIFFERENT docs</summary>
public class Foo
{
    /// <summary>Some method</summary>
    public void Bar() { }
}"));

            await test.RunAsync(CancellationToken.None);
        }
    }
}
