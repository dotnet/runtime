// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.IO;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using ILCompiler.Logging;
using ILLink.Shared;

using ILSequencePoint = Internal.IL.ILSequencePoint;
using MethodIL = Internal.IL.MethodIL;

namespace ILCompiler
{
    public class Logger
    {
        private readonly ILogWriter _logWriter;

        private readonly HashSet<int> _suppressedWarnings;

        private readonly bool _isSingleWarn;
        private readonly HashSet<string> _singleWarnEnabledAssemblies;
        private readonly HashSet<string> _singleWarnDisabledAssemblies;
        private readonly HashSet<string> _trimWarnedAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _aotWarnedAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static Logger Null = new Logger(new TextLogWriter(TextWriter.Null), false);

        public bool IsVerbose { get; }

        public Logger(ILogWriter writer, bool isVerbose, IEnumerable<int> suppressedWarnings, bool singleWarn, IEnumerable<string> singleWarnEnabledModules, IEnumerable<string> singleWarnDisabledModules)
        {
            _logWriter = writer;
            IsVerbose = isVerbose;
            _suppressedWarnings = new HashSet<int>(suppressedWarnings);
            _isSingleWarn = singleWarn;
            _singleWarnEnabledAssemblies = new HashSet<string>(singleWarnEnabledModules, StringComparer.OrdinalIgnoreCase);
            _singleWarnDisabledAssemblies = new HashSet<string>(singleWarnDisabledModules, StringComparer.OrdinalIgnoreCase);
        }

        public Logger(TextWriter writer, bool isVerbose, IEnumerable<int> suppressedWarnings, bool singleWarn, IEnumerable<string> singleWarnEnabledModules, IEnumerable<string> singleWarnDisabledModules)
            : this(new TextLogWriter(writer), isVerbose, suppressedWarnings, singleWarn, singleWarnEnabledModules, singleWarnDisabledModules)
        {
        }

        public Logger(ILogWriter writer, bool isVerbose)
            : this(writer, isVerbose, Array.Empty<int>(), singleWarn: false, Array.Empty<string>(), Array.Empty<string>())
        {
        }

        public Logger(TextWriter writer, bool isVerbose)
            : this(new TextLogWriter(writer), isVerbose)
        {
        }

        public void LogMessage(string message)
        {
            MessageContainer? messageContainer = MessageContainer.CreateInfoMessage(message);
            if (messageContainer.HasValue)
                _logWriter.WriteMessage(messageContainer.Value);
        }

        public void LogWarning(string text, int code, MessageOrigin origin, string subcategory = MessageSubCategory.None)
        {
            MessageContainer? warning = MessageContainer.CreateWarningMessage(this, text, code, origin, subcategory);
            if (warning.HasValue)
                _logWriter.WriteWarning(warning.Value);
        }

        public void LogWarning(MessageOrigin origin, DiagnosticId id, params string[] args)
        {
            MessageContainer? warning = MessageContainer.CreateWarningMessage(this, origin, id, args);
            if (warning.HasValue)
                _logWriter.WriteWarning(warning.Value);
        }

        public void LogWarning(string text, int code, TypeSystemEntity origin, string subcategory = MessageSubCategory.None) =>
            LogWarning(text, code, new MessageOrigin(origin), subcategory);

        public void LogWarning(TypeSystemEntity origin, DiagnosticId id, params string[] args) =>
            LogWarning(new MessageOrigin(origin), id, args);

        public void LogWarning(string text, int code, MethodIL origin, int ilOffset, string subcategory = MessageSubCategory.None)
        {
            MessageOrigin messageOrigin = new MessageOrigin(origin, ilOffset);
            LogWarning(text, code, messageOrigin, subcategory);
        }

        public void LogWarning(MethodIL origin, int ilOffset, DiagnosticId id, params string[] args)
        {
            MessageOrigin messageOrigin = new MessageOrigin(origin, ilOffset);
            LogWarning(messageOrigin, id, args);
        }

        public void LogWarning(string text, int code, string origin, string subcategory = MessageSubCategory.None)
        {
            MessageOrigin _origin = new MessageOrigin(origin);
            LogWarning(text, code, _origin, subcategory);
        }

        public void LogWarning(string origin, DiagnosticId id, params string[] args)
        {
            MessageOrigin _origin = new MessageOrigin(origin);
            LogWarning(_origin, id, args);
        }

        public void LogError(string text, int code, string subcategory = MessageSubCategory.None, MessageOrigin? origin = null)
        {
            MessageContainer? error = MessageContainer.CreateErrorMessage(text, code, subcategory, origin);
            if (error.HasValue)
                _logWriter.WriteError(error.Value);
        }

        public void LogError(MessageOrigin? origin, DiagnosticId id, params string[] args)
        {
            MessageContainer? error = MessageContainer.CreateErrorMessage(origin, id, args);
            if (error.HasValue)
                _logWriter.WriteError(error.Value);
        }

        public void LogError(string text, int code, TypeSystemEntity origin, string subcategory = MessageSubCategory.None) =>
            LogError(text, code, subcategory, new MessageOrigin(origin));

        public void LogError(TypeSystemEntity origin, DiagnosticId id, params string[] args) =>
            LogError(new MessageOrigin(origin), id, args);

        internal bool IsWarningSuppressed(int code, MessageOrigin origin)
        {
            // This is causing too much noise
            // https://github.com/dotnet/runtimelab/issues/1591
            if (code == 2110 || code == 2111 || code == 2113 || code == 2115)
                return true;

            if (_suppressedWarnings.Contains(code))
                return true;

            IEnumerable<CustomAttributeValue<TypeDesc>> suppressions = null;

            // TODO: Suppressions with different scopes

            if (origin.MemberDefinition is TypeDesc type)
            {
                var ecmaType = type.GetTypeDefinition() as EcmaType;
                suppressions = ecmaType?.GetDecodedCustomAttributes("System.Diagnostics.CodeAnalysis", "UnconditionalSuppressMessageAttribute");
            }

            if (origin.MemberDefinition is MethodDesc method)
            {
                method = CompilerGeneratedState.GetUserDefinedMethodForCompilerGeneratedMember(method) ?? method;

                var ecmaMethod = method.GetTypicalMethodDefinition() as EcmaMethod;
                suppressions = ecmaMethod?.GetDecodedCustomAttributes("System.Diagnostics.CodeAnalysis", "UnconditionalSuppressMessageAttribute");
            }

            if (suppressions != null)
            {
                foreach (CustomAttributeValue<TypeDesc> suppression in suppressions)
                {
                    if (suppression.FixedArguments.Length != 2
                        || suppression.FixedArguments[1].Value is not string warningId
                        || warningId.Length < 6
                        || !warningId.StartsWith("IL")
                        || (warningId.Length > 6 && warningId[6] != ':')
                        || !int.TryParse(warningId.Substring(2, 4), out int suppressedCode))
                    {
                        continue;
                    }

                    if (code == suppressedCode)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal bool IsWarningAsError(int code)
        {
            // TODO: warnaserror
            return false;
        }

        internal bool IsSingleWarn(ModuleDesc owningModule, string messageSubcategory)
        {
            string assemblyName = owningModule.Assembly.GetName().Name;

            bool result = false;

            if ((_isSingleWarn || _singleWarnEnabledAssemblies.Contains(assemblyName))
                && !_singleWarnDisabledAssemblies.Contains(assemblyName))
            {
                result = true;

                if (messageSubcategory == MessageSubCategory.TrimAnalysis)
                {
                    lock (_trimWarnedAssemblies)
                    {
                        if (_trimWarnedAssemblies.Add(assemblyName))
                        {
                            LogWarning(GetModuleFileName(owningModule), DiagnosticId.AssemblyProducedTrimWarnings, assemblyName);
                        }
                    }
                }
                else if (messageSubcategory == MessageSubCategory.AotAnalysis)
                {
                    lock (_aotWarnedAssemblies)
                    {
                        if (_aotWarnedAssemblies.Add(assemblyName))
                        {
                            LogWarning(GetModuleFileName(owningModule), DiagnosticId.AssemblyProducedAOTWarnings, assemblyName);
                        }
                    }
                }
            }

            return result;
        }

        private static string GetModuleFileName(ModuleDesc module)
        {
            string assemblyName = module.Assembly.GetName().Name;
            var context = (CompilerTypeSystemContext)module.Context;
            if (context.ReferenceFilePaths.TryGetValue(assemblyName, out string result)
                || context.InputFilePaths.TryGetValue(assemblyName, out result))
            {
                return result;
            }
            return assemblyName;
        }
    }
}
