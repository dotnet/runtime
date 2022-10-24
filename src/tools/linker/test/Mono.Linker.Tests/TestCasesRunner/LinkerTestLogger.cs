// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Mono.Linker.Tests.TestCasesRunner
{
    public class LinkerTestLogger : ILogger
    {
        readonly List<MessageContainer> MessageContainers;

        public LinkerTestLogger()
        {
            MessageContainers = new List<MessageContainer>();
        }

        public List<MessageContainer> GetLoggedMessages()
        {
            return MessageContainers;
        }

        public void LogMessage(MessageContainer message)
        {
            // This is to force Cecil to load all the information from the assembly
            // When the message is logged, the assembly is still opened by the linker and available
            // later on during validation, it may already be closed and Cecil's lazy loading might fail.
            message.ToString();

            MessageContainers.Add(message);
        }
    }
}
