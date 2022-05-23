// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using ILCompiler.Logging;

namespace ILCompiler
{
    public class TextLogWriter : ILogWriter
    {
        public TextWriter Writer { get; }

        public TextLogWriter(TextWriter writer)
        {
            Writer = TextWriter.Synchronized(writer);
        }

#if !READYTORUN
        public void WriteMessage(MessageContainer message)
        {
            Writer.WriteLine(message.ToMSBuildString());
        }

        public void WriteWarning(MessageContainer warning)
        {
            Writer.WriteLine(warning.ToMSBuildString());
        }

        public void WriteError(MessageContainer error)
        {
            Writer.WriteLine(error.ToMSBuildString());
        }
#endif
    }
}
