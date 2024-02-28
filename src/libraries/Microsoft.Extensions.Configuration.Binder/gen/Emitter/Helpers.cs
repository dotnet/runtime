// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingGenerator
    {
        private sealed partial class Emitter
        {
            internal static readonly AssemblyName s_assemblyName = typeof(ConfigurationBindingGenerator).Assembly.GetName();

            private string? _emittedExtsTargetType;

            private enum InitializationKind
            {
                None = 0,
                SimpleAssignment = 1,
                AssignmentWithNullCheck = 2,
                Declaration = 3,
            }

            /// <summary>
            /// The type of defaulting for a property if it does not have a config entry.
            /// This should only be applied for "Get" cases, not "Bind" and is also conditioned
            /// on the source generated for a particular property as to whether it uses this value.
            /// Note this is different than "InitializationKind.Declaration" since it only applied to
            /// complex types and not arrays\enumerables.
            /// </summary>
            private enum ValueDefaulting
            {
                None = 0,

                /// <summary>
                /// Call the setter with the default value for the property's Type.
                /// </summary>
                CallSetter = 1,
            }

            private static class Expression
            {
                public const string configurationGetSection = "configuration.GetSection";
                public const string sectionKey = "section.Key";
                public const string sectionPath = "section.Path";
                public const string sectionValue = "section.Value";

                public static string GeneratedCodeAnnotation = $@"[GeneratedCode(""{s_assemblyName.Name}"", ""{s_assemblyName.Version}"")]";
            }

            private static class TypeDisplayString
            {
                public const string NullableActionOfBinderOptions = "Action<BinderOptions>?";
                public const string OptionsBuilderOfTOptions = $"OptionsBuilder<{Identifier.TOptions}>";
                public const string HashSetOfString = "HashSet<string>";
                public const string LazyHashSetOfString = "Lazy<HashSet<string>>";
                public const string ListOfString = "List<string>";
            }

            private static class Identifier
            {
                public const string binderOptions = nameof(binderOptions);
                public const string config = nameof(config);
                public const string configureBinder = nameof(configureBinder);
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
                public const string instance = nameof(instance);
                public const string optionsBuilder = nameof(optionsBuilder);
                public const string originalCount = nameof(originalCount);
                public const string section = nameof(section);
                public const string sectionKey = nameof(sectionKey);
                public const string services = nameof(services);
                public const string sp = nameof(sp);
                public const string temp = nameof(temp);
                public const string type = nameof(type);
                public const string typedObj = nameof(typedObj);
                public const string validateKeys = nameof(validateKeys);
                public const string value = nameof(value);

                public const string Add = nameof(Add);
                public const string AddSingleton = nameof(AddSingleton);
                public const string Any = nameof(Any);
                public const string Array = nameof(Array);
                public const string Bind = nameof(Bind);
                public const string BinderOptions = nameof(BinderOptions);
                public const string BindingExtensions = nameof(BindingExtensions);
                public const string ConfigurationChangeTokenSource = nameof(ConfigurationChangeTokenSource);
                public const string Configure = nameof(Configure);
                public const string CopyTo = nameof(CopyTo);
                public const string ContainsKey = nameof(ContainsKey);
                public const string Count = nameof(Count);
                public const string CultureInfo = nameof(CultureInfo);
                public const string CultureNotFoundException = nameof(CultureNotFoundException);
                public const string Enum = nameof(Enum);
                public const string ErrorOnUnknownConfiguration = nameof(ErrorOnUnknownConfiguration);
                public const string Exception = nameof(Exception);
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
                public const string InterceptsLocation = nameof(InterceptsLocation);
                public const string InvalidOperationException = nameof(InvalidOperationException);
                public const string InvariantCulture = nameof(InvariantCulture);
                public const string IOptionsChangeTokenSource = nameof(IOptionsChangeTokenSource);
                public const string IServiceCollection = nameof(IServiceCollection);
                public const string Length = nameof(Length);
                public const string Name = nameof(Name);
                public const string NumberStyles = nameof(NumberStyles);
                public const string Parse = nameof(Parse);
                public const string Path = nameof(Path);
                public const string Resize = nameof(Resize);
                public const string Services = nameof(Services);
                public const string TOptions = nameof(TOptions);
                public const string TryCreate = nameof(TryCreate);
                public const string TryGetValue = nameof(TryGetValue);
                public const string Type = nameof(Type);
                public const string Uri = nameof(Uri);
                public const string ValidateConfigurationKeys = nameof(ValidateConfigurationKeys);
                public const string Value = nameof(Value);
            }

            private bool ShouldEmitMethods(MethodsToGen methods) => (_interceptorInfo.MethodsToGen & methods) != 0;

            private void EmitInterceptsLocationAnnotations(MethodsToGen overload)
            {
                IEnumerable<InvocationLocationInfo>? infoList = _interceptorInfo.GetInfo(overload);
                bool interceptsCalls = infoList is not null;

                // The only time a generated binding method won't have any locations to
                // intercept is when either of these methods are used as helpers for
                // other generated OptionsBuilder or ServiceCollection binding extensions.
                Debug.Assert(interceptsCalls ||
                    overload is MethodsToGen.ServiceCollectionExt_Configure_T_name_BinderOptions ||
                    overload is MethodsToGen.OptionsBuilderExt_Bind_T_BinderOptions);

                if (interceptsCalls)
                {
                    EmitInterceptsLocationAnnotations(infoList!);
                }
            }

            private void EmitInterceptsLocationAnnotations(IEnumerable<InvocationLocationInfo> infoList)
            {
                foreach (InvocationLocationInfo info in infoList)
                {
                    _writer.WriteLine($@"[{Identifier.InterceptsLocation}(@""{info.FilePath}"", {info.LineNumber}, {info.CharacterNumber})]");
                }
            }

            private void EmitBindingExtStartRegion(string targetType)
            {
                Debug.Assert(_emittedExtsTargetType is null);

                EmitBlankLineIfRequired();
                _emittedExtsTargetType = targetType;
                EmitBindingExtRegionText(isStart: true);
                _emitBlankLineBeforeNextStatement = false;
            }

            private void EmitBindingExtEndRegion()
            {
                Debug.Assert(_emittedExtsTargetType is not null);

                EmitBindingExtRegionText(isStart: false);
                _emittedExtsTargetType = null;
                _emitBlankLineBeforeNextStatement = true;
            }

            private void EmitBindingExtRegionText(bool isStart)
            {
                string endSource = isStart ? string.Empty : "end";
                _writer.WriteLine($"#{endSource}region {_emittedExtsTargetType} extensions.");
            }

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

            private void EmitCheckForNullArgument_WithBlankLine(string paramName, bool useThrowIfNullMethod, bool voidReturn = false)
            {
                if (voidReturn)
                {
                    _writer.WriteLine($$"""
                    if ({{paramName}} is null)
                    {
                        return;
                    }
                    """);
                }
                else
                {
                    string throwIfNullExpr = useThrowIfNullMethod
                    ? $"ArgumentNullException.ThrowIfNull({paramName});"
                    : $$"""
                    if ({{paramName}} is null)
                    {
                        throw new ArgumentNullException(nameof({{paramName}}));
                    }
                    """;

                    _writer.WriteLine(throwIfNullExpr);
                }

                _writer.WriteLine();
            }

            private string GetIncrementalIdentifier(string prefix) => $"{prefix}{_valueSuffixIndex++}";

            private static string GetInitializeMethodDisplayString(ObjectSpec type) =>
                $"{nameof(MethodsToGen_CoreBindingHelper.Initialize)}{type.IdentifierCompatibleSubstring}";
        }
    }
}
