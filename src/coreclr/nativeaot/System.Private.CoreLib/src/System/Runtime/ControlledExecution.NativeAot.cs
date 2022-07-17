// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Runtime
{
    public static class ControlledExecution
    {
        [Obsolete("ControlledExecution.Run method may corrupt the process and should not be used in production code.", DiagnosticId = "SYSLIB0046", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
        public static void Run(Action action, CancellationToken cancellationToken)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
