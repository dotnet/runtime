// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Logging.Console
{
    [Obsolete("This method is retained only for compatibility. The recommended alternative is ConsoleLoggerOptions.")]
    public interface IConsoleLoggerSettings
    {
        bool IncludeScopes { get; }

        IChangeToken? ChangeToken { get; }

        bool TryGetSwitch(string name, out LogLevel level);

        IConsoleLoggerSettings Reload();
    }
}
