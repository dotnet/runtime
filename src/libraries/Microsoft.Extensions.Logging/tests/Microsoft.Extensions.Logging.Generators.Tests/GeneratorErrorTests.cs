// © Microsoft Corporation. All rights reserved.

namespace Microsoft.Extensions.Logging.Generators.Tests
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using Xunit;

    public class GeneratorTests
    {
        [Fact]
        public void InvalidMethodName()
        {
            var d = TryCode(@"
                partial class C
                {{
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    public static partial void __M1(ILogger logger);
                }}
            ");

            Assert.Single(d);
            Assert.Equal("LG0", d[0].Id);
        }

        [Fact]
        public void InvalidMessage()
        {
            var d = TryCode(@"
                partial class C
                {{
                    [LoggerMessage(0, LogLevel.Debug, """")]
                    public static partial void M1(ILogger logger);
                }}
            ");

            Assert.Single(d);
            Assert.Equal("LG1", d[0].Id);
        }

        [Fact]
        public void InvalidParameterName()
        {
            var d = TryCode(@"
                partial class C
                {{
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    public static partial void M1(ILogger logger. string __foo);
                }}
            ");

            Assert.Single(d);
            Assert.Equal("LG2", d[0].Id);
        }

        [Fact]
        public void NestedType()
        {
            var d = TryCode(@"
                partial class C
                {{
                    public class Nested
                    {{
                        [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                        public static partial void M1(ILogger logger);
                    }}
                }}
            ");

            Assert.Single(d);
            Assert.Equal("LG3", d[0].Id);
        }

        [Fact]
        public void RequiredType()
        {
            var d = TryCode(@"
                partial class C
                {{
                }}
            ", false);

            Assert.Single(d);
            Assert.Equal("LG4", d[0].Id);
        }

        [Fact]
        public void EventIdReuse()
        {
            var d = TryCode(@"
                partial class C
                {{
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    public static partial void M1(ILogger logger);

                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    public static partial void M2(ILogger logger);
                }}
            ");

            Assert.Single(d);
            Assert.Equal("LG5", d[0].Id);
        }

        [Fact]
        public void MethodReturnType()
        {
            var d = TryCode(@"
                partial class C
                {{
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    public static partial int M1(ILogger logger);
                }}
            ");

            Assert.Single(d);
            Assert.Equal("LG6", d[0].Id);
        }

        [Fact]
        public void FirstArgILogger()
        {
            var d = TryCode(@"
                partial class C
                {{
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    public static partial void M1(int p1, ILogger logger);
                }}
            ");

            Assert.Single(d);
            Assert.Equal("LG7", d[0].Id);
        }

        [Fact]
        public void NotStatic()
        {
            var d = TryCode(@"
                partial class C
                {{
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    public partial void M1(ILogger logger);
                }}
            ");

            Assert.Single(d);
            Assert.Equal("LG8", d[0].Id);
        }

        [Fact]
        public void NotPartial()
        {
            var d = TryCode(@"
                partial class C
                {{
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    public static void M1(ILogger logger) {}
                }}
            ");

            Assert.Single(d);
            Assert.Equal("LG9", d[0].Id);
        }

        [Fact]
        public void MethodGeneric()
        {
            var d = TryCode(@"
                partial class C
                {{
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    public static partial void M1<T>(ILogger logger);
                }}
            ");

            Assert.Single(d);
            Assert.Equal("LG10", d[0].Id);
        }

        [Fact]
        public void ParameterGeneric()
        {
            var d = TryCode(@"
                partial class C<T>
                {{
                    [LoggerMessage(0, LogLevel.Debug, ""M1"")]
                    public static partial void M1(ILogger logger, T foo) {}
                }}
            ");

            Assert.Single(d);
            Assert.Equal("LG11", d[0].Id);
        }

        private static IReadOnlyList<Diagnostic> TryCode(string code, bool wrap = true)
        {
            var results = new List<Diagnostic>();

            var text = code;
            if (wrap)
            {
                text = $@"
                    namespace Microsoft.Extensions.Logging
                    {{
                        public enum LogLevel
                        {{
                            Debug,
                            Information,
                            Warning,
                            Error,
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

                    namespace Test
                    {{
                        using Microsoft.Extensions.Logging;

                        {code}
                    }}
                ";
            }

            var compilation = CSharpCompilation.Create(
                "example.dll",
                new[]
                {
                    CSharpSyntaxTree.ParseText(text),
                },
                new[]
                {
                    MetadataReference.CreateFromFile(Assembly.GetAssembly(typeof(System.Exception))!.Location),
                });

            var p = new Microsoft.Extensions.Logging.Generators.LoggingGenerator.Parser(compilation, (d) => {
                results.Add(d);
            }, CancellationToken.None);

            var allNodes = compilation.SyntaxTrees.SelectMany(s => s.GetRoot().DescendantNodes());
            var allClasses = allNodes.Where(d => d.IsKind(SyntaxKind.ClassDeclaration)).OfType<ClassDeclarationSyntax>();
            _ = p.GetLogClasses(allClasses);

            return results;
        }
    }
}
