// © Microsoft Corporation. All rights reserved.

using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.Extensions.Logging.Generators.Tests
{
    static class RoslynTestUtils
    {
        public static Project CreateTestProject()
        {
#pragma warning disable CA2000 // Dispose objects before losing scope
            return new AdhocWorkspace()
                        .AddSolution(SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Create()))
                        .AddProject("Test", "test.dll", "C#")
                            .WithMetadataReferences(new[] { MetadataReference.CreateFromFile(Assembly.GetAssembly(typeof(System.Exception))!.Location) })
                            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(NullableContextOptions.Enable));
#pragma warning restore CA2000 // Dispose objects before losing scope
        }

        public static void Dispose(this Project proj)
        {
            proj.Solution.Workspace.Dispose();
        }

        public static Task CommitChanges(this Project proj, params string[] ignorables)
        {
            Assert.True(proj.Solution.Workspace.TryApplyChanges(proj.Solution));
            return AssertNoDiagnostic(proj, ignorables);
        }

        public static async Task AssertNoDiagnostic(this Project proj, params string[] ignorables)
        {
            foreach (var doc in proj.Documents)
            {
                var sm = await doc.GetSemanticModelAsync(CancellationToken.None).ConfigureAwait(false);
                Assert.NotNull(sm);

                foreach (var d in sm!.GetDiagnostics())
                {
                    bool ignore = false;
                    foreach (var ig in ignorables)
                    {
                        if (d.Id == ig)
                        {
                            ignore = true;
                            break;
                        }
                    }

                    Assert.True(ignore, d.ToString());
                }
            }
        }

        public static Project WithDocument(this Project proj, string name, string text)
        {
            return proj.AddDocument(name, text).Project;
        }

        public const string LoggingBoilerplate = @"
            namespace Microsoft.Extensions.Logging
            {
                using System;

                public enum LogLevel
                {
                    Trace,
                    Debug,
                    Information,
                    Warning,
                    Error,
                    Critical,
                    None,
                }

                [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false)]
                public sealed class LoggerMessageAttribute : System.Attribute
                {
                    public LoggerMessageAttribute(int eventId, LogLevel level, string message) => (EventId, Level, Message) = (eventId, level, message);
                    public int EventId { get; set; }
                    public string? EventName { get; set; }
                    public LogLevel Level { get; set; }
                    public string Message { get; set; }
                }

                public interface ILogger
                {
                }

                public interface ILogger<T> : ILogger
                {
                }

                public struct EventId
                {
                    public EventId(int id, string name) {}
                }

                public static class LoggerExtensions
                {
                    public static void Log(this ILogger logger, LogLevel logLevel, Exception exception, string message, params object[] args){}
                    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, string message, params object[] args){}
                    public static void Log(this ILogger logger, LogLevel logLevel, string message, params object[] args){}
                    public static void Log(this ILogger logger, LogLevel logLevel, EventId eventId, Exception exception, string message, params object[] args){}
                    public static void LogCritical(this ILogger logger, string message, params object[] args){}
                    public static void LogCritical(this ILogger logger, Exception exception, string message, params object[] args){}
                    public static void LogCritical(this ILogger logger, EventId eventId, string message, params object[] args){}
                    public static void LogCritical(this ILogger logger, EventId eventId, Exception exception, string message, params object[] args){}
                    public static void LogDebug(this ILogger logger, EventId eventId, Exception exception, string message, params object[] args){}
                    public static void LogDebug(this ILogger logger, EventId eventId, string message, params object[] args){}
                    public static void LogDebug(this ILogger logger, Exception exception, string message, params object[] args){}
                    public static void LogDebug(this ILogger logger, string message, params object[] args){}
                    public static void LogError(this ILogger logger, string message, params object[] args){}
                    public static void LogError(this ILogger logger, Exception exception, string message, params object[] args){}
                    public static void LogError(this ILogger logger, EventId eventId, Exception exception, string message, params object[] args){}
                    public static void LogError(this ILogger logger, EventId eventId, string message, params object[] args){}
                    public static void LogInformation(this ILogger logger, EventId eventId, string message, params object[] args){}
                    public static void LogInformation(this ILogger logger, Exception exception, string message, params object[] args){}
                    public static void LogInformation(this ILogger logger, EventId eventId, Exception exception, string message, params object[] args){}
                    public static void LogInformation(this ILogger logger, string message, params object[] args){}
                    public static void LogTrace(this ILogger logger, string message, params object[] args){}
                    public static void LogTrace(this ILogger logger, Exception exception, string message, params object[] args){}
                    public static void LogTrace(this ILogger logger, EventId eventId, string message, params object[] args){}
                    public static void LogTrace(this ILogger logger, EventId eventId, Exception exception, string message, params object[] args){}
                    public static void LogWarning(this ILogger logger, EventId eventId, string message, params object[] args){}
                    public static void LogWarning(this ILogger logger, EventId eventId, Exception exception, string message, params object[] args){}
                    public static void LogWarning(this ILogger logger, string message, params object[] args){}
                    public static void LogWarning(this ILogger logger, Exception exception, string message, params object[] args){}
                }
            }
        ";

        public static Project WithLoggingBoilerplate(this Project proj)
        {
            return proj.AddDocument("boilerplate.cs", LoggingBoilerplate).Project;
        }

        public static Document FindDocument(this Project proj, string name)
        {
            foreach (var doc in proj.Documents)
            {
                if (doc.Name == name)
                {
                    return doc;
                }
            }

            throw new FileNotFoundException(name);
        }

        /// <summary>
        /// Looks for /*N+*/ and /*-N*/ markers in a string and creates a TextSpan containing the enclosed text.
        /// </summary>
        public static TextSpan MakeSpan(string text, int spanNum)
        {
            int start = text.IndexOf($"/*{spanNum}+*/", StringComparison.Ordinal);
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(spanNum));
            }
            start += 6;

            int end = text.IndexOf($"/*-{spanNum}*/", StringComparison.Ordinal);
            if (end < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(spanNum));
            }
            end -= 1;

            return new TextSpan(start, end - start);
        }
    }
}
