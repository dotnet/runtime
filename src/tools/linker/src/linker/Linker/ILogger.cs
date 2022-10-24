// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Mono.Linker
{
    public interface ILogger
    {
        void LogMessage(MessageContainer message);
    }
}
