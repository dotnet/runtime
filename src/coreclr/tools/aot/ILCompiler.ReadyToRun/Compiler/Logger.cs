// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using ILCompiler.Logging;
using ILLink.Shared;
using Internal.TypeSystem;

namespace ILCompiler
{
    public class Logger
    {
        public TextWriter Writer;

        public static Logger Null = new Logger(TextWriter.Null, false);

        public bool IsVerbose { get; }

        public Logger(TextWriter writer, bool isVerbose)
        {
            Writer = TextWriter.Synchronized(writer);
            IsVerbose = isVerbose;
        }

        public void LogMessage(string message)
        {
            Writer.WriteLine(message);
        }

        public void LogWarning(MessageOrigin origin, DiagnosticId id, params string[] args)
        {
            MessageContainer? warning = MessageContainer.CreateWarningMessage(this, origin, id, args);
            if (warning.HasValue)
                Writer.WriteLine(warning.Value);
        }

        public void LogWarning(TypeSystemEntity origin, DiagnosticId id, params string[] args) =>
            LogWarning(new MessageOrigin(origin), id, args);

        internal bool IsWarningSuppressed(int code, MessageOrigin origin) => false; // TODO - unify with ILCompiler.Compiler/Compiler/Logger.cs
        internal bool IsWarningSubcategorySuppressed(string category) => false; // dtto
        internal bool IsSingleWarn(ModuleDesc owningModule, string messageSubcategory) => false; // dtto
        internal static bool IsWarningAsError(int _/*code*/) => false; // dtto
    }
}
