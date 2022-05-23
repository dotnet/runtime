// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using ILCompiler.Logging;

namespace ILCompiler
{
    public interface ILogWriter
    {
        TextWriter Writer { get; }
#if !READYTORUN
        void WriteMessage(MessageContainer message);
        void WriteWarning(MessageContainer warning);
        void WriteError(MessageContainer error);
#endif
    }
}
