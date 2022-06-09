// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

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
    }
}
