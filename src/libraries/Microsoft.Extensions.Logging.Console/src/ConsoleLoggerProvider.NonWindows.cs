// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging.Console
{
    /// <summary>
    /// A provider of <see cref="ConsoleLogger"/> instances.
    /// </summary>
    public partial class ConsoleLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private static bool DoesConsoleSupportAnsi() => true;
    }
}
