// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using ILCompiler.Logging;

namespace ILCompiler
{
    public class TextLogWriter : ILogWriter
    {
        private TextWriter _writer;

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
            _writer.WriteLine(warning.ToMSBuildString());
        }

        public void WriteError(MessageContainer error)
        {
            _writer.WriteLine(error.ToMSBuildString());
        }
    }
}
