// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Runtime
{
    public static class ControlledExecution
    {
        [Obsolete(Obsoletions.ControlledExecutionRunMessage, DiagnosticId = Obsoletions.ControlledExecutionRunDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        public static void Run(Action action, CancellationToken cancellationToken)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
