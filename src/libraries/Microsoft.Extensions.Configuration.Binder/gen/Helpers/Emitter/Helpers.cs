// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingGenerator
    {
        private sealed partial class Emitter
        {
            private static readonly AssemblyName s_assemblyName = typeof(Emitter).Assembly.GetName();

            private enum InitializationKind
            {
                None = 0,
                SimpleAssignment = 1,
                AssignmentWithNullCheck = 2,
                Declaration = 3,
            }
            private static class Expression
            {
                public const string configurationGetSection = "configuration.GetSection";
                public const string sectionKey = "section.Key";
                public const string sectionPath = "section.Path";
                public const string sectionValue = "section.Value";

                public const string GetBinderOptions = $"{FullyQualifiedDisplayString.CoreBindingHelper}.{Identifier.GetBinderOptions}";
            }

            private static class FullyQualifiedDisplayString
            {
                public const string ActionOfBinderOptions = $"global::System.Action<global::Microsoft.Extensions.Configuration.BinderOptions>";
                public const string AddSingleton = $"{ServiceCollectionServiceExtensions}.AddSingleton";
                public const string ConfigurationChangeTokenSource = "global::Microsoft.Extensions.Options.ConfigurationChangeTokenSource";
                public const string CoreBindingHelper = $"global::{ProjectName}.{Identifier.CoreBindingHelper}";
                public const string IConfiguration = "global::Microsoft.Extensions.Configuration.IConfiguration";
                public const string IConfigurationSection = IConfiguration + "Section";
                public const string IOptionsChangeTokenSource = "global::Microsoft.Extensions.Options.IOptionsChangeTokenSource";
                public const string InvalidOperationException = "global::System.InvalidOperationException";
                public const string IServiceCollection = "global::Microsoft.Extensions.DependencyInjection.IServiceCollection";
                public const string NotSupportedException = "global::System.NotSupportedException";
                public const string OptionsBuilderOfTOptions = $"global::Microsoft.Extensions.Options.OptionsBuilder<{Identifier.TOptions}>";
                public const string ServiceCollectionServiceExtensions = "global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions";
                public const string Type = $"global::System.Type";
            }

            private static class MinimalDisplayString
            {
                public const string NullableActionOfBinderOptions = "Action<BinderOptions>?";
                public const string HashSetOfString = "HashSet<string>";
                public const string LazyHashSetOfString = "Lazy<HashSet<string>>";
                public const string ListOfString = "List<string>";
            }

            private static class Identifier
            {
                public const string binderOptions = nameof(binderOptions);
                public const string configureOptions = nameof(configureOptions);
                public const string configuration = nameof(configuration);
                public const string configSectionPath = nameof(configSectionPath);
                public const string defaultValue = nameof(defaultValue);
                public const string element = nameof(element);
                public const string enumValue = nameof(enumValue);
                public const string exception = nameof(exception);
                public const string getPath = nameof(getPath);
                public const string key = nameof(key);
                public const string name = nameof(name);
                public const string obj = nameof(obj);
                public const string optionsBuilder = nameof(optionsBuilder);
                public const string originalCount = nameof(originalCount);
                public const string section = nameof(section);
                public const string sectionKey = nameof(sectionKey);
                public const string services = nameof(services);
                public const string temp = nameof(temp);
                public const string type = nameof(type);
                public const string validateKeys = nameof(validateKeys);
                public const string value = nameof(value);

                public const string Add = nameof(Add);
                public const string AddSingleton = nameof(AddSingleton);
                public const string Any = nameof(Any);
                public const string Array = nameof(Array);
                public const string AsConfigWithChildren = nameof(AsConfigWithChildren);
                public const string Bind = nameof(Bind);
                public const string BinderOptions = nameof(BinderOptions);
                public const string Configure = nameof(Configure);
                public const string CopyTo = nameof(CopyTo);
                public const string ContainsKey = nameof(ContainsKey);
                public const string CoreBindingHelper = nameof(CoreBindingHelper);
                public const string Count = nameof(Count);
                public const string CultureInfo = nameof(CultureInfo);
                public const string CultureNotFoundException = nameof(CultureNotFoundException);
                public const string Enum = nameof(Enum);
                public const string ErrorOnUnknownConfiguration = nameof(ErrorOnUnknownConfiguration);
                public const string GeneratedConfigurationBinder = nameof(GeneratedConfigurationBinder);
                public const string GeneratedOptionsBuilderBinder = nameof(GeneratedOptionsBuilderBinder);
                public const string GeneratedServiceCollectionBinder = nameof(GeneratedServiceCollectionBinder);
                public const string Get = nameof(Get);
                public const string GetBinderOptions = nameof(GetBinderOptions);
                public const string GetChildren = nameof(GetChildren);
                public const string GetSection = nameof(GetSection);
                public const string GetValue = nameof(GetValue);
                public const string HasConfig = nameof(HasConfig);
                public const string HasValueOrChildren = nameof(HasValueOrChildren);
                public const string HasValue = nameof(HasValue);
                public const string IConfiguration = nameof(IConfiguration);
                public const string IConfigurationSection = nameof(IConfigurationSection);
                public const string Int32 = "int";
                public const string InvalidOperationException = nameof(InvalidOperationException);
                public const string InvariantCulture = nameof(InvariantCulture);
                public const string Length = nameof(Length);
                public const string Parse = nameof(Parse);
                public const string Path = nameof(Path);
                public const string Resize = nameof(Resize);
                public const string Services = nameof(Services);
                public const string TOptions = nameof(TOptions);
                public const string TryCreate = nameof(TryCreate);
                public const string TryGetValue = nameof(TryGetValue);
                public const string TryParse = nameof(TryParse);
                public const string Uri = nameof(Uri);
                public const string ValidateConfigurationKeys = nameof(ValidateConfigurationKeys);
                public const string Value = nameof(Value);
            }

            private bool ShouldEmitBinders() =>
                ShouldEmitMethods(MethodsToGen_ConfigurationBinder.Any) ||
                ShouldEmitMethods(MethodsToGen_Extensions_OptionsBuilder.Any) ||
                ShouldEmitMethods(MethodsToGen_Extensions_ServiceCollection.Any);

            /// <summary>
            /// Starts a block of source code.
            /// </summary>
            /// <param name="source">Source to write after the open brace.</param>
            public void EmitStartBlock(string? source = null)
            {
                if (source is not null)
                {
                    _writer.WriteLine(source);
                }

                _writer.WriteLine("{");
                _writer.Indentation++;
            }

            /// <summary>
            /// Ends a block of source code.
            /// </summary>
            /// <param name="source">Source to write before the close brace.</param>
            /// <param name="endBraceTrailingSource">Trailing source after the end brace, e.g. ";" to end an init statement.</param>
            public void EmitEndBlock(string? source = null, string? endBraceTrailingSource = null)
            {
                if (source is not null)
                {
                    _writer.WriteLine(source);
                }

                string endBlockSource = endBraceTrailingSource is null ? "}" : $"}}{endBraceTrailingSource}";
                _writer.Indentation--;
                _writer.WriteLine(endBlockSource);
            }

            private void EmitBlankLineIfRequired()
            {
                if (_emitBlankLineBeforeNextStatement)
                {
                    _writer.WriteLine();
                }

                _emitBlankLineBeforeNextStatement = true;
            }

            private void EmitCheckForNullArgument_WithBlankLine_IfRequired(bool isValueType)
            {
                if (!isValueType)
                {
                    EmitCheckForNullArgument_WithBlankLine(Identifier.obj);
                }
            }

            private void EmitCheckForNullArgument_WithBlankLine(string paramName)
            {
                string exceptionTypeDisplayString = _useFullyQualifiedNames
                    ? "global::System.ArgumentNullException"
                    : "ArgumentNullException";

                _writer.WriteLine($$"""
                    if ({{paramName}} is null)
                    {
                        throw new {{exceptionTypeDisplayString}}(nameof({{paramName}}));
                    }
                    """);

                _writer.WriteLine();
            }

            private bool EmitInitException(TypeSpec type)
            {
                Debug.Assert(type.InitializationStrategy is not InitializationStrategy.None);

                if (!type.CanInitialize)
                {
                    _writer.WriteLine(GetInitException(type.InitExceptionMessage) + ";");
                    return true;
                }

                return false;
            }

            private void EmitRootBindingClassStartBlock(string className)
            {
                EmitBlankLineIfRequired();
                EmitStartBlock($$"""
                    /// <summary>Generated helper providing an AOT and linking compatible implementation for configuration binding.</summary>
                    {{GetGeneratedCodeAttributeSrc()}}
                    internal static class {{className}}
                    """);

                _emitBlankLineBeforeNextStatement = false;
            }

            private string GetGeneratedCodeAttributeSrc()
            {
                string attributeRefExpr = _useFullyQualifiedNames ? $"global::System.CodeDom.Compiler.GeneratedCodeAttribute" : "GeneratedCode";
                return $@"[{attributeRefExpr}(""{s_assemblyName.Name}"", ""{s_assemblyName.Version}"")]";
            }

            private string GetInitException(string message) => $@"throw new {GetInvalidOperationDisplayName()}(""{message}"")";

            private string GetIncrementalIdentifier(string prefix) => $"{prefix}{_valueSuffixIndex++}";

            private string GetInitalizeMethodDisplayString(ObjectSpec type) =>
                GetHelperMethodDisplayString($"{nameof(MethodsToGen_CoreBindingHelper.Initialize)}{type.DisplayStringWithoutSpecialCharacters}");

            private string GetTypeDisplayString(TypeSpec type) => _useFullyQualifiedNames ? type.FullyQualifiedDisplayString : type.MinimalDisplayString;

            private string GetHelperMethodDisplayString(string methodName)
            {
                if (_useFullyQualifiedNames)
                {
                    methodName = FullyQualifiedDisplayString.CoreBindingHelper + "." + methodName;
                }

                return methodName;
            }

            private string GetInvalidOperationDisplayName() => _useFullyQualifiedNames ? FullyQualifiedDisplayString.InvalidOperationException : Identifier.InvalidOperationException;
        }
    }
}
