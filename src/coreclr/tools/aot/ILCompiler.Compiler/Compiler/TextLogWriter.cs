// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using ILCompiler.Logging;

namespace ILCompiler
{
    public class TextLogWriter : ILogWriter
    {
        private TextWriter _writer;
        private bool _hasLoggedErrors;

        public TextLogWriter(TextWriter writer)
        {
            _writer = TextWriter.Synchronized(writer);
        }

        public void WriteMessage(MessageContainer message)
        {
            _writer.WriteLine(message.ToMSBuildString());
        }

        public void WriteWarning(MessageContainer warning)
        {
            // Warnings treated as errors should also set the error flag
            if (warning.Category == MessageCategory.WarningAsError)
                _hasLoggedErrors = true;

            _writer.WriteLine(warning.ToMSBuildString());
        }

        public void WriteError(MessageContainer error)
        {
            _hasLoggedErrors = true;
            _writer.WriteLine(error.ToMSBuildString());
        }

        public bool HasLoggedErrors => _hasLoggedErrors;
    }
}
