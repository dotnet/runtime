// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection.Metadata;

using Internal.TypeSystem;
using Internal.IL;

using ILCompiler.Dataflow;
using ILCompiler.Logging;
using ILLink.Shared;

using MethodIL = Internal.IL.MethodIL;

namespace ILCompiler
{
    public class Logger
    {
        private readonly ILogWriter _logWriter;
        private readonly CompilerGeneratedState _compilerGeneratedState;
        private readonly UnconditionalSuppressMessageAttributeState _unconditionalSuppressMessageAttributeState;

        private readonly HashSet<int> _suppressedWarnings;
        private readonly HashSet<string> _suppressedCategories;

        private readonly bool _isSingleWarn;
        private readonly HashSet<string> _singleWarnEnabledAssemblies;
        private readonly HashSet<string> _singleWarnDisabledAssemblies;
        private readonly HashSet<string> _trimWarnedAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _aotWarnedAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static Logger Null = new Logger(new TextLogWriter(TextWriter.Null), null, false);

        public bool IsVerbose { get; }

        public Logger(
            ILogWriter writer,
            ILProvider ilProvider,
            bool isVerbose,
            IEnumerable<int> suppressedWarnings,
            bool singleWarn,
            IEnumerable<string> singleWarnEnabledModules,
            IEnumerable<string> singleWarnDisabledModules,
            IEnumerable<string> suppressedCategories)
        {
            _logWriter = writer;
            IsVerbose = isVerbose;
            _suppressedWarnings = new HashSet<int>(suppressedWarnings);
            _isSingleWarn = singleWarn;
            _singleWarnEnabledAssemblies = new HashSet<string>(singleWarnEnabledModules, StringComparer.OrdinalIgnoreCase);
            _singleWarnDisabledAssemblies = new HashSet<string>(singleWarnDisabledModules, StringComparer.OrdinalIgnoreCase);
            _suppressedCategories = new HashSet<string>(suppressedCategories, StringComparer.Ordinal);
            _compilerGeneratedState = ilProvider == null ? null : new CompilerGeneratedState(ilProvider, this);
            _unconditionalSuppressMessageAttributeState = new UnconditionalSuppressMessageAttributeState(_compilerGeneratedState, this);
        }

        public Logger(TextWriter writer, ILProvider ilProvider, bool isVerbose, IEnumerable<int> suppressedWarnings, bool singleWarn, IEnumerable<string> singleWarnEnabledModules, IEnumerable<string> singleWarnDisabledModules, IEnumerable<string> suppressedCategories)
            : this(new TextLogWriter(writer), ilProvider, isVerbose, suppressedWarnings, singleWarn, singleWarnEnabledModules, singleWarnDisabledModules, suppressedCategories)
        {
        }

        public Logger(ILogWriter writer, ILProvider ilProvider, bool isVerbose)
            : this(writer, ilProvider, isVerbose, Array.Empty<int>(), singleWarn: false, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>())
        {
        }

        public Logger(TextWriter writer, ILProvider ilProvider, bool isVerbose)
            : this(new TextLogWriter(writer), ilProvider, isVerbose)
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

        internal bool IsWarningSubcategorySuppressed(string category) => _suppressedCategories.Contains(category);

        internal bool IsWarningSuppressed(int code, MessageOrigin origin)
        {
            if (_suppressedWarnings.Contains(code))
                return true;

            return _unconditionalSuppressMessageAttributeState.IsSuppressed(code, origin);
        }

        internal static bool IsWarningAsError(int _/*code*/)
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

        internal bool ShouldSuppressAnalysisWarningsForRequires(TypeSystemEntity originMember, string requiresAttribute)
             => ShouldSuppressAnalysisWarningsForRequires(originMember, requiresAttribute, out _);

        internal bool ShouldSuppressAnalysisWarningsForRequires(TypeSystemEntity originMember, string requiresAttribute, [NotNullWhen(returnValue: true)] out CustomAttributeValue<TypeDesc>? attribute)
        {
            // Check if the current scope method has Requires on it
            // since that attribute automatically suppresses all trim analysis warnings.
            // Check both the immediate origin method as well as suppression context method
            // since that will be different for compiler generated code.
            if (originMember is MethodDesc method &&
                method.IsInRequiresScope(requiresAttribute, out attribute))
                return true;

            if (originMember is FieldDesc field)
                return field.DoesFieldRequire(requiresAttribute, out attribute);

            if (originMember.GetOwningType() == null)  // Basically a way to test if the entity is a member (type, method, field, ...)
            {
                attribute = null;
                return false;
            }

            MethodDesc owningMethod;
            if (_compilerGeneratedState != null)
            {
                while (_compilerGeneratedState.TryGetOwningMethodForCompilerGeneratedMember(originMember, out owningMethod))
                {
                    Debug.Assert(owningMethod != originMember);
                    if (owningMethod.IsInRequiresScope(requiresAttribute, out attribute))
                        return true;
                    originMember = owningMethod;
                }
            }

            attribute = null;
            return false;
        }
    }
}
