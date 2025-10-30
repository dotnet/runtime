// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using ILCompiler;
using ILCompiler.Logging;

namespace Mono.Linker.Tests.TestCasesRunner
{
    public class TrimmingTestLogger : ILogWriter
    {
        private readonly StringWriter _infoStringWriter;
        private readonly TextWriter _infoWriter;

        private readonly List<MessageContainer> _messageContainers;
        private bool _hasLoggedErrors;

        public TrimmingTestLogger()
        {
            _infoStringWriter = new StringWriter();
            _infoWriter = TextWriter.Synchronized(_infoStringWriter);
            _messageContainers = new List<MessageContainer>();
        }

        public TextWriter Writer => _infoWriter;

        public bool HasLoggedErrors => _hasLoggedErrors;

        public ImmutableArray<MessageContainer> GetLoggedMessages()
        {
            return _messageContainers.ToImmutableArray();
        }

        public void WriteError(MessageContainer error)
        {
            _hasLoggedErrors = true;
            lock (_messageContainers)
            {
                _messageContainers.Add(error);
            }
        }

        public void WriteMessage(MessageContainer message)
        {
            lock (_messageContainers)
            {
                _messageContainers.Add(message);
            }
        }

        public void WriteWarning(MessageContainer warning)
        {
            // Warnings treated as errors should also set the error flag
            if (warning.Category == MessageCategory.WarningAsError)
                _hasLoggedErrors = true;

            lock (_messageContainers)
            {
                _messageContainers.Add(warning);
            }
        }
    }
}
