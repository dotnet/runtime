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

        public static Logger Null = new Logger(TextWriter.Null, false);

        public TextWriter Writer { get; }

        public bool IsVerbose { get; }

        public Logger(TextWriter writer, bool isVerbose, IEnumerable<int> suppressedWarnings)
        {
            Writer = TextWriter.Synchronized(writer);
            IsVerbose = isVerbose;
            _suppressedWarnings = new HashSet<int>(suppressedWarnings);
        }

        public Logger(TextWriter writer, bool isVerbose)
            : this(writer, isVerbose, Array.Empty<int>())
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

            MessageOrigin messageOrigin = new MessageOrigin(origin.OwningMethod, document, lineNumber, null);
            LogWarning(text, code, messageOrigin, subcategory);
        }

        internal bool IsWarningSuppressed(int code, MessageOrigin origin)
        {
            if (_suppressedWarnings.Contains(code))
                return true;

            IEnumerable<CustomAttributeValue<TypeDesc>> suppressions = null;

            // TODO: Suppressions with different scopes
            

            if (origin.MemberDefinition is MethodDesc method)
            {
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
    }

    public static class MessageSubCategory
    {
        public const string None = "";
        public const string TrimAnalysis = "Trim analysis";
        public const string AotAnalysis = "AOT analysis";
    }
}
