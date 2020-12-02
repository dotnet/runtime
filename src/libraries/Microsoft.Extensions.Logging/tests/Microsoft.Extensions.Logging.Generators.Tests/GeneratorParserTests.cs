// © Microsoft Corporation. All rights reserved.

namespace Microsoft.Extensions.Logging.Generators.Tests
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using Xunit;

    public class GeneratorParserTests
    {
        [Fact]
        public void InvalidMethodName()
        {
            var (lc, d) = TryCode(@"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    static partial void __M1(ILogger logger);
                }
            ");

            Assert.Single(lc);
            Assert.Single(d);
            Assert.Equal("LG0000", d[0].Id);
        }

        [Fact]
        public void InvalidMessage()
        {
            var (lc, d) = TryCode(@"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, """")]
                    static partial void M1(ILogger logger);
                }
            ");

            Assert.Single(lc);
            Assert.Single(d);
            Assert.Equal("LG0001", d[0].Id);
        }

        [Fact]
        public void InvalidParameterName()
        {
            var (lc, d) = TryCode(@"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    static partial void M1(ILogger logger, string __foo);
                }
            ");

            Assert.Single(lc);
            Assert.Single(d);
            Assert.Equal("LG0002", d[0].Id);
        }

        [Fact]
        public void NestedType()
        {
            var (lc, d) = TryCode(@"
                partial class C
                {
                    public partial class Nested
                    {
                        [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                        static partial void M1(ILogger logger);
                    }
                }
            ");

            Assert.Empty(lc);
            Assert.Single(d);
            Assert.Equal("LG0003", d[0].Id);
        }

        [Fact]
        public void RequiredTypes()
        {
            var (lc, d) = TryCode(@"
                namespace System
                {
                    public class Object
                    {
                    }

                    public class Void
                    {
                    }
                }
                namespace Microsoft.Extensions.Logging
                {
                }
                partial class C
                {
                }
            ", false, includeReferences: false);

            Assert.Empty(lc);
            Assert.Single(d);
            Assert.Equal("LG0004", d[0].Id);
            
            (lc, d) = TryCode(@"
                partial class C
                {
                }
            ", false);

            Assert.Empty(lc);
            Assert.Single(d);
            Assert.Equal("LG0004", d[0].Id);

            (lc, d) = TryCode(@"
                namespace Microsoft.Extensions.Logging
                {
                    public sealed class LoggerMessageAttribute : System.Attribute {}
                }
                partial class C
                {
                }
            ", false);

            Assert.Empty(lc);
            Assert.Single(d);
            Assert.Equal("LG0004", d[0].Id);
        }

        [Fact]
        public void EventIdReuse()
        {
            var (lc, d) = TryCode(@"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    static partial void M1(ILogger logger);

                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    static partial void M2(ILogger logger);
                }
            ");

            Assert.Single(lc);
            Assert.Single(d);
            Assert.Equal("LG0005", d[0].Id);
        }

        [Fact]
        public void MethodReturnType()
        {
            var (lc, d) = TryCode(@"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    public static partial int M1(ILogger logger);

                    public static partial int M1(ILogger logger) { return 0; }
                }
            ");

            Assert.Empty(lc);
            Assert.Single(d);
            Assert.Equal("LG0006", d[0].Id);
        }

        [Fact]
        public void FirstArgILogger()
        {
            var (lc, d) = TryCode(@"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    static partial void M1(int p1, ILogger logger);
                }
            ");

            Assert.Empty(lc);
            Assert.Single(d);
            Assert.Equal("LG0007", d[0].Id);
        }

        [Fact]
        public void NotStatic()
        {
            var (lc, d) = TryCode(@"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    partial void M1(ILogger logger);
                }
            ");

            Assert.Empty(lc);
            Assert.Single(d);
            Assert.Equal("LG0008", d[0].Id);
        }

        [Fact]
        public void NotPartial()
        {
            var (lc, d) = TryCode(@"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    static void M1(ILogger logger) {}
                }
            ");

            Assert.Empty(lc);
            Assert.Single(d);
            Assert.Equal("LG0009", d[0].Id);
        }

        [Fact]
        public void MethodGeneric()
        {
            var (lc, d) = TryCode(@"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    static partial void M1<T>(ILogger logger);
                }
            ");

            Assert.Empty(lc);
            Assert.Single(d);
            Assert.Equal("LG0010", d[0].Id);
        }

        [Fact]
        public void Templates()
        {
            var (lc, d) = TryCode(@"
                partial class C
                {
                    [LoggerMessage(1, LogLevel.Debug, ""M1"")]
                    static partial void M1(ILogger logger, string arg1, string arg2);

                    [LoggerMessage(2, LogLevel.Debug, ""M2 {arg1} {arg2}"")]
                    static partial void M2(ILogger logger, string arg1, string arg2);

                    [LoggerMessage(3, LogLevel.Debug, ""M3 {arg1"")]
                    static partial void M3(ILogger logger, string arg1);

                    [LoggerMessage(4, LogLevel.Debug, ""M4 arg1}"")]
                    static partial void M4(ILogger logger, string arg1);

                    [LoggerMessage(5, LogLevel.Debug, ""M5 {"")]
                    static partial void M5(ILogger logger, string arg1);
 
                    [LoggerMessage(6, LogLevel.Debug, ""}M6 "")]
                    static partial void M6(ILogger logger, string arg1);

                    [LoggerMessage(7, LogLevel.Debug, ""M7 {{arg1}}"")]
                    static partial void M7(ILogger logger, string arg1);
                }
            ");

            Assert.Single(lc);
            Assert.False(lc[0].Methods[0].MessageHasTemplates);
            Assert.True(lc[0].Methods[1].MessageHasTemplates);
            Assert.False(lc[0].Methods[2].MessageHasTemplates);
            Assert.False(lc[0].Methods[3].MessageHasTemplates);
            Assert.False(lc[0].Methods[4].MessageHasTemplates);
            Assert.False(lc[0].Methods[5].MessageHasTemplates);
            Assert.True(lc[0].Methods[6].MessageHasTemplates);
            Assert.Empty(d);
        }

        [Fact]
        public void Namespace()
        {
            var (lc, d) = TryCode(@"
                namespace Foo
                {
                    partial class C
                    {
                        [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                        static partial void M1(ILogger logger);
                    }
                }
            ");

            Assert.Single(lc);
            Assert.Equal("Test.Foo", lc[0].Namespace);
            Assert.Equal("C", lc[0].Name);
            Assert.Empty(d);

            (lc, d) = TryCode(@"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    static partial void M1(ILogger logger);
                }
            ");

            Assert.Single(lc);
            Assert.Equal("Test", lc[0].Namespace);
            Assert.Equal("C", lc[0].Name);
            Assert.Empty(d);

            (lc, d) = TryCode(@"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    static partial void M1(ILogger logger);
                }
            ", true, false);

            Assert.Single(lc);
            Assert.Equal(string.Empty, lc[0].Namespace);
            Assert.Equal("C", lc[0].Name);
            Assert.Empty(d);
        }

        [Fact]
        public void Generic()
        {
            var (lc, d) = TryCode(@"
                partial class C<T>
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    static partial void M1(ILogger logger);
                }
            ");

            Assert.Single(lc);
            Assert.Equal("Test", lc[0].Namespace);
            Assert.Equal("C<T>", lc[0].Name);
            Assert.Empty(d);
        }

        [Fact]
        public void EventName()
        {
            var (lc, d) = TryCode(@"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1"", EventName = ""MyEvent"")]
                    static partial void M1(ILogger logger);
                }
            ");

            Assert.Single(lc);
            Assert.Equal("Test", lc[0].Namespace);
            Assert.Equal("C", lc[0].Name);
            Assert.Equal("MyEvent", lc[0].Methods[0].EventName);
            Assert.Empty(d);
        }

        [Fact]
        public void Cancellation()
        {
            var (lc, d) = TryCode(@"
                partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    static partial void M1(ILogger logger);
                }
            ", cancellationToken: new CancellationToken(true));

            Assert.Empty(lc);
            Assert.Empty(d);
        }

        [Fact]
        public void RandomAttribute()
        {
            var (lc, d) = TryCode(@"
                partial class C
                {
                    [System.Obsolete(""Foo"")]
                    static partial void M1(ILogger logger);
                }
            ");

            Assert.Empty(lc);
            Assert.Empty(d);
        }

        [Fact]
        public void ExtensionMethod()
        {
            var (lc, d) = TryCode(@"
                static partial class C
                {
                    [LoggerMessage(0, LogLevel.Debug, ""Hello"")]
                    static partial void M1(this ILogger logger);
                }
            ");

            Assert.True(lc[0].Methods[0].IsExtensionMethod);
            Assert.Empty(d);
        }

        [Fact]
        public void SourceErrors()
        {
            var (lc, d) = TryCode(@"
                static partial class C
                {
                    // bogus argument type
                    [LoggerMessage(0, "", ""Hello"")]
                    static partial void M1(ILogger logger);

                    // attribute applied to something other than a method
                    [LoggerMessage(0, "", ""Hello"")]
                    int field;

                    // missing parameter name
                    [LoggerMessage(0, LogLevel.Debug, ""Hello"")]
                    static partial void M2(ILogger);

                    // bogus parameter type
                    [LoggerMessage(0, LogLevel.Debug, ""Hello"")]
                    static partial void M2(XILogger logger);
                }
            ", checkDiags: false);

            Assert.Empty(lc);
            Assert.Empty(d);    // should fail quietly on broken code
        }

        private static (IReadOnlyList<LoggingGenerator.LoggerClass>, IReadOnlyList<Diagnostic>) TryCode(
            string code,
            bool wrap = true,
            bool inNamespace = true,
            bool includeReferences = true,
            bool checkDiags = true,
            CancellationToken cancellationToken = default)
        {
            var text = code;
            if (wrap)
            {
                var nsStart = "namespace Test {";
                var nsEnd = "}";
                if (!inNamespace)
                {
                    nsStart = "";
                    nsEnd = "";
                }

                text = $@"
                    {nsStart}
                    using Microsoft.Extensions.Logging;
                    {code}
                    {nsEnd}

                    namespace Microsoft.Extensions.Logging
                    {{
                        public enum LogLevel
                        {{
                            Trace,
                            Debug,
                            Information,
                            Warning,
                            Error,
                            Critical,
                        }}

                        [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false)]
                        public sealed class LoggerMessageAttribute : System.Attribute
                        {{
                            public LoggerMessageAttribute(int eventId, LogLevel level, string message) => (EventId, Level, Message) = (eventId, level, message);
                            public int EventId {{ get; set; }}
                            public string? EventName {{ get; set; }}
                            public LogLevel Level {{ get; set; }}
                            public string Message {{ get; set; }}
                        }}

                        public interface ILogger
                        {{
                        }}
                    }}
                ";
            }

            var refs = Array.Empty<PortableExecutableReference>();
            if (includeReferences)
            {
                refs = new[] { MetadataReference.CreateFromFile(Assembly.GetAssembly(typeof(System.Exception))!.Location) };
            }

            var compilation = CSharpCompilation.Create(
                "example.dll",
                new[] { CSharpSyntaxTree.ParseText(text, cancellationToken: CancellationToken.None) },
                refs)
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(NullableContextOptions.Enable));

            if (checkDiags)
            {
                // make sure we have valid syntax
                Assert.Empty(compilation.GetDiagnostics(CancellationToken.None));
            }

            var results = new List<Diagnostic>();
            var p = new Microsoft.Extensions.Logging.Generators.LoggingGenerator.Parser(compilation, (d) => {
                results.Add(d);
            }, cancellationToken);

            var allNodes = compilation.SyntaxTrees.SelectMany(s => s.GetRoot().DescendantNodes());
            var allClasses = allNodes.Where(d => d.IsKind(SyntaxKind.ClassDeclaration)).OfType<ClassDeclarationSyntax>();
            var lc = p.GetLogClasses(allClasses);

            return (lc, results);
        }
    }
}
