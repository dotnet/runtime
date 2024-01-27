// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System
{
    /// <summary>
    /// The exception that is thrown when there is an attempt to read or write protected memory.
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class AccessViolationException : SystemException
    {
        public AccessViolationException()
            : base(SR.Arg_AccessViolationException)
        {
            HResult = HResults.E_POINTER;
        }

        public AccessViolationException(string? message)
            : base(message ?? SR.Arg_AccessViolationException)
        {
            HResult = HResults.E_POINTER;
        }

        public AccessViolationException(string? message, Exception? innerException)
            : base(message ?? SR.Arg_AccessViolationException, innerException)
        {
            HResult = HResults.E_POINTER;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected AccessViolationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

#pragma warning disable CA1823, 169, IDE0044 // Fields are not used from managed
        private IntPtr _ip;      // Address of faulting instruction.
        private IntPtr _target;  // Address that could not be accessed.
        private int _accessType; // 0:read, 1:write
#pragma warning restore CA1823, 169, IDE0044
    }
}
