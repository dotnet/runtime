// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class SystemException : Exception
    {
        public SystemException()
            : base(SR.Arg_SystemException)
        {
            HResult = HResults.COR_E_SYSTEM;
        }

        public SystemException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_SYSTEM;
        }

        public SystemException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_SYSTEM;
        }

        protected SystemException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
