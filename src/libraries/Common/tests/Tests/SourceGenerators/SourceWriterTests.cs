// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using SourceGenerators;
using Xunit;

namespace Common.Tests
{
    public sealed class SourceWriterTests
    {
        [Fact]
        public void CanHandleVariousLineEndings()
        {
            string testTemplate = "public static void Main(){0}{{{1}\tConsole.WriteLine(\"Hello, world\");{2}}}";
            SourceWriter writer = new();

            CheckCanWrite(string.Format(testTemplate, "\n", "\n", "\n"));
            CheckCanWrite(string.Format(testTemplate, "\r\n", "\r\n", "\r\n"));
            CheckCanWrite(string.Format(testTemplate, "\n", "\r\n", "\n"));

            // Regression test for https://github.com/dotnet/runtime/issues/88918.
            CheckCanWrite("    global::Microsoft.Extensions.DependencyInjection.OptionsServiceCollectionExtensions.AddOptions(services);\r\n    global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::Microsoft.Extensions.Options.IOptionsChangeTokenSource<TOptions>>(services, new global::Microsoft.Extensions.Options.ConfigurationChangeTokenSource<TOptions>(name, configuration));\r\n    return global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<global::Microsoft.Extensions.Options.IConfigureOptions<TOptions>>(services, new global::Microsoft.Extensions.Options.ConfigureNamedOptions<TOptions>(name, obj => global::Microsoft.Extensions.Configuration.Binder.SourceGeneration.CoreBindingHelper.BindCoreUntyped(configuration, obj, typeof(TOptions), configureOptions)));\r\n}");

            void CheckCanWrite(string source)
            {
                // No exception expected.
                writer.WriteLine(source);
                writer.Reset();
            }
        }
    }
}
