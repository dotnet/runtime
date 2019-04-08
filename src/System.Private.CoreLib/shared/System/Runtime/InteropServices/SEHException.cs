// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Runtime.Serialization;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Exception for Structured Exception Handler exceptions.
    /// </summary>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class SEHException : ExternalException
    {
        public SEHException()
            : base()
        {
            HResult = HResults.E_FAIL;
        }

        public SEHException(string? message)
            : base(message)
        {
            HResult = HResults.E_FAIL;
        }

        public SEHException(string? message, Exception? inner)
            : base(message, inner)
        {
            HResult = HResults.E_FAIL;
        }

        protected SEHException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        // Exceptions can be resumable, meaning a filtered exception
        // handler can correct the problem that caused the exception,
        // and the code will continue from the point that threw the
        // exception.
        //
        // Resumable exceptions aren't implemented in this version,
        // but this method exists and always returns false.
        //
        public virtual bool CanResume() => false;
    }
}
