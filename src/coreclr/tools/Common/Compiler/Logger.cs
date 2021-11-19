// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.IO;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using ILCompiler.Logging;

using ILSequencePoint = Internal.IL.ILSequencePoint;
using MethodIL = Internal.IL.MethodIL;

namespace ILCompiler
{
    public class Logger
    {
        private readonly HashSet<int> _suppressedWarnings;

        private readonly bool _isSingleWarn;
        private readonly HashSet<string> _singleWarnEnabledAssemblies;
        private readonly HashSet<string> _singleWarnDisabledAssemblies;
        private readonly HashSet<string> _trimWarnedAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _aotWarnedAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static Logger Null = new Logger(TextWriter.Null, false);

        public TextWriter Writer { get; }

        public bool IsVerbose { get; }

        public Logger(TextWriter writer, bool isVerbose, IEnumerable<int> suppressedWarnings, bool singleWarn, IEnumerable<string> singleWarnEnabledModules, IEnumerable<string> singleWarnDisabledModules)
        {
            Writer = TextWriter.Synchronized(writer);
            IsVerbose = isVerbose;
            _suppressedWarnings = new HashSet<int>(suppressedWarnings);
            _isSingleWarn = singleWarn;
            _singleWarnEnabledAssemblies = new HashSet<string>(singleWarnEnabledModules, StringComparer.OrdinalIgnoreCase);
            _singleWarnDisabledAssemblies = new HashSet<string>(singleWarnDisabledModules, StringComparer.OrdinalIgnoreCase);
        }

        public Logger(TextWriter writer, bool isVerbose)
            : this(writer, isVerbose, Array.Empty<int>(), singleWarn: false, Array.Empty<string>(), Array.Empty<string>())
        {
        }

        public void LogWarning(string text, int code, MessageOrigin origin, string subcategory = MessageSubCategory.None)
        {
            MessageContainer? warning = MessageContainer.CreateWarningMessage(this, text, code, origin, subcategory);
            if (warning.HasValue)
                Writer.WriteLine(warning.Value.ToMSBuildString());
        }

        public void LogWarning(string text, int code, TypeSystemEntity origin, string subcategory = MessageSubCategory.None)
        {
            MessageOrigin messageOrigin = new MessageOrigin(origin);
            MessageContainer? warning = MessageContainer.CreateWarningMessage(this, text, code, messageOrigin, subcategory);
            if (warning.HasValue)
                Writer.WriteLine(warning.Value.ToMSBuildString());
        }

        public void LogWarning(string text, int code, MethodIL origin, int ilOffset, string subcategory = MessageSubCategory.None)
        {
            string document = null;
            int? lineNumber = null;

            IEnumerable<ILSequencePoint> sequencePoints = origin.GetDebugInfo()?.GetSequencePoints();
            if (sequencePoints != null)
            {
                foreach (var sequencePoint in sequencePoints)
                {
                    if (sequencePoint.Offset <= ilOffset)
                    {
                        document = sequencePoint.Document;
                        lineNumber = sequencePoint.LineNumber;
                    }
                }
            }

            MethodDesc warnedMethod = CompilerGeneratedState.GetUserDefinedMethodForCompilerGeneratedMember(origin.OwningMethod) ?? origin.OwningMethod;

            MessageOrigin messageOrigin = new MessageOrigin(warnedMethod, document, lineNumber, null);
            LogWarning(text, code, messageOrigin, subcategory);
        }

        public void LogWarning(string text, int code, string origin, string subcategory = MessageSubCategory.None)
        {
            MessageOrigin _origin = new MessageOrigin(origin);
            LogWarning(text, code, _origin, subcategory);
        }

        internal bool IsWarningSuppressed(int code, MessageOrigin origin)
        {
            if (_suppressedWarnings.Contains(code))
                return true;

            IEnumerable<CustomAttributeValue<TypeDesc>> suppressions = null;

            // TODO: Suppressions with different scopes
            

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
                            LogWarning($"Assembly '{assemblyName}' produced trim warnings. For more information see https://aka.ms/dotnet-illink/libraries", 2104, GetModuleFileName(owningModule));
                        }
                    }
                }
                else if (messageSubcategory == MessageSubCategory.AotAnalysis)
                {
                    lock (_aotWarnedAssemblies)
                    {
                        if (_aotWarnedAssemblies.Add(assemblyName))
                        {
                            LogWarning($"Assembly '{assemblyName}' produced AOT analysis warnings.", 9702, GetModuleFileName(owningModule));
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

    public static class MessageSubCategory
    {
        public const string None = "";
        public const string TrimAnalysis = "Trim analysis";
        public const string AotAnalysis = "AOT analysis";
    }
}
