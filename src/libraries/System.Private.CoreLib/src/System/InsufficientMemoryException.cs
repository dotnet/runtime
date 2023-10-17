// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    /// <summary>
    /// Purpose: The exception class for running out of memory
    /// but most likely in a non-fatal way that shouldn't
    /// be affected by escalation policy.  Use this for cases
    /// like MemoryFailPoint or a TryAllocate method, where you
    /// expect OOM's with no shared state corruption and you
    /// want to recover from these errors.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class InsufficientMemoryException : OutOfMemoryException
    {
        public InsufficientMemoryException() : base(GetDefaultMessage())
        {
            HResult = HResults.COR_E_INSUFFICIENTMEMORY;
        }

        public InsufficientMemoryException(string? message)
            : base(message ?? GetDefaultMessage())
        {
            HResult = HResults.COR_E_INSUFFICIENTMEMORY;
        }

        public InsufficientMemoryException(string? message, Exception? innerException)
            : base(message ?? GetDefaultMessage(), innerException)
        {
            HResult = HResults.COR_E_INSUFFICIENTMEMORY;
        }

        private static string GetDefaultMessage()
#if CORECLR
            => GetMessageFromNativeResources(ExceptionMessageKind.OutOfMemory);
#else
            => SR.Arg_OutOfMemoryException;
#endif

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        private InsufficientMemoryException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
