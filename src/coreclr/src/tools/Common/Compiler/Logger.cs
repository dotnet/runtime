// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace ILCompiler
{
    // Poor man's logger. We can do better than this.

    public class Logger
    {
        public static Logger Null = new Logger(TextWriter.Null, false);

        public TextWriter Writer { get; }

        public bool IsVerbose { get; }

        public Logger(TextWriter writer, bool isVerbose)
        {
            Writer = TextWriter.Synchronized(writer);
            IsVerbose = isVerbose;
        }
    }
}
