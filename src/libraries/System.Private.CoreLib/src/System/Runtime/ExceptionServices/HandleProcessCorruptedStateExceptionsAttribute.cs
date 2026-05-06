// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.ExceptionServices
{
    // This attribute can be applied to methods to indicate that ProcessCorruptedState
    // Exceptions should be delivered to them.
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    [Obsolete(Obsoletions.CorruptedStateRecoveryMessage, DiagnosticId = Obsoletions.CorruptedStateRecoveryDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
    public sealed class HandleProcessCorruptedStateExceptionsAttribute : Attribute
    {
        public HandleProcessCorruptedStateExceptionsAttribute()
        {
        }
    }
}
