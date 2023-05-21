// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingGenerator
    {
        private sealed partial class Emitter
        {
            private enum InitializationKind
            {
                None = 0,
                SimpleAssignment = 1,
                AssignmentWithNullCheck = 2,
                Declaration = 3,
            }

            private static class Expression
            {
                public const string sectionKey = "section.Key";
                public const string sectionPath = "section.Path";
                public const string sectionValue = "section.Value";

                public const string GetBinderOptions = $"{FullyQualifiedDisplayName.Helpers}.{Identifier.GetBinderOptions}";
            }

            private static class FullyQualifiedDisplayName
            {
                public const string ActionOfBinderOptions = $"global::System.Action<global::Microsoft.Extensions.Configuration.BinderOptions>";
                public const string Helpers = $"global::{ProjectName}.{Identifier.Helpers}";
                public const string IConfiguration = "global::Microsoft.Extensions.Configuration.IConfiguration";
                public const string IConfigurationSection = IConfiguration + "Section";
                public const string InvalidOperationException = "global::System.InvalidOperationException";
                public const string IServiceCollection = "global::Microsoft.Extensions.DependencyInjection.IServiceCollection";
                public const string NotSupportedException = "global::System.NotSupportedException";
                public const string Type = $"global::System.Type";
            }

            public static class Identifier
            {
                public const string binderOptions = nameof(binderOptions);
                public const string configureActions = nameof(configureActions);
                public const string configuration = nameof(configuration);
                public const string defaultValue = nameof(defaultValue);
                public const string element = nameof(element);
                public const string enumValue = nameof(enumValue);
                public const string exception = nameof(exception);
                public const string getPath = nameof(getPath);
                public const string key = nameof(key);
                public const string obj = nameof(obj);
                public const string originalCount = nameof(originalCount);
                public const string path = nameof(path);
                public const string section = nameof(section);
                public const string services = nameof(services);
                public const string stringValue = nameof(stringValue);
                public const string temp = nameof(temp);
                public const string type = nameof(type);

                public const string Add = nameof(Add);
                public const string Any = nameof(Any);
                public const string Array = nameof(Array);
                public const string Bind = nameof(Bind);
                public const string BindCore = nameof(BindCore);
                public const string BinderOptions = nameof(BinderOptions);
                public const string Configure = nameof(Configure);
                public const string CopyTo = nameof(CopyTo);
                public const string ContainsKey = nameof(ContainsKey);
                public const string Count = nameof(Count);
                public const string CultureInfo = nameof(CultureInfo);
                public const string CultureNotFoundException = nameof(CultureNotFoundException);
                public const string Enum = nameof(Enum);
                public const string ErrorOnUnknownConfiguration = nameof(ErrorOnUnknownConfiguration);
                public const string GeneratedConfigurationBinder = nameof(GeneratedConfigurationBinder);
                public const string Get = nameof(Get);
                public const string GetBinderOptions = nameof(GetBinderOptions);
                public const string GetCore = nameof(GetCore);
                public const string GetChildren = nameof(GetChildren);
                public const string GetSection = nameof(GetSection);
                public const string GetValue = nameof(GetValue);
                public const string GetValueCore = nameof(GetValueCore);
                public const string HasChildren = nameof(HasChildren);
                public const string HasValueOrChildren = nameof(HasValueOrChildren);
                public const string HasValue = nameof(HasValue);
                public const string Helpers = nameof(Helpers);
                public const string IConfiguration = nameof(IConfiguration);
                public const string IConfigurationSection = nameof(IConfigurationSection);
                public const string Int32 = "int";
                public const string InvalidOperationException = nameof(InvalidOperationException);
                public const string InvariantCulture = nameof(InvariantCulture);
                public const string Length = nameof(Length);
                public const string Parse = nameof(Parse);
                public const string Path = nameof(Path);
                public const string Resize = nameof(Resize);
                public const string TryCreate = nameof(TryCreate);
                public const string TryGetValue = nameof(TryGetValue);
                public const string TryParse = nameof(TryParse);
                public const string Uri = nameof(Uri);
                public const string Value = nameof(Value);
            }

            private void EmitBlankLineIfRequired()
            {
                if (_precedingBlockExists)
                {
                    _writer.WriteBlankLine();
                }
                {
                    _precedingBlockExists = true;
                }
            }

            private void EmitCheckForNullArgument_WithBlankLine_IfRequired(bool isValueType)
            {
                if (!isValueType)
                {
                    EmitCheckForNullArgument_WithBlankLine(Identifier.obj);
                }
            }

            private void EmitCheckForNullArgument_WithBlankLine(string argName, bool useFullyQualifiedNames = false)
            {
                string exceptionTypeDisplayString = useFullyQualifiedNames
                    ? "global::System.ArgumentNullException"
                    : "ArgumentNullException";

                _writer.WriteBlock($$"""
                    if ({{argName}} is null)
                    {
                        throw new {{exceptionTypeDisplayString}}(nameof({{argName}}));
                    }
                    """);

                _writer.WriteBlankLine();
            }

            private string GetIncrementalVarName(string prefix) => $"{prefix}{_parseValueCount++}";

            private string GetTypeDisplayString(TypeSpec type) => _useFullyQualifiedNames ? type.FullyQualifiedDisplayString : type.MinimalDisplayString;

            private string GetHelperMethodDisplayString(string methodName)
            {
                if (_useFullyQualifiedNames)
                {
                    methodName = FullyQualifiedDisplayName.Helpers + "." + methodName;
                }

                return methodName;
            }
        }
    }
}
