// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Runtime.Serialization;

namespace System.Reflection
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class AmbiguousMatchException : SystemException
    {
        public AmbiguousMatchException()
            : base(SR.RFLCT_Ambiguous)
        {
            HResult = HResults.COR_E_AMBIGUOUSMATCH;
        }

        public AmbiguousMatchException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_AMBIGUOUSMATCH;
        }

        public AmbiguousMatchException(string? message, Exception? inner)
            : base(message, inner)
        {
            HResult = HResults.COR_E_AMBIGUOUSMATCH;
        }

        internal AmbiguousMatchException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
