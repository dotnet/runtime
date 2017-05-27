// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System.Reflection
{
    public sealed class AmbiguousMatchException : SystemException
    {
        public AmbiguousMatchException()
            : base(SR.RFLCT_Ambiguous)
        {
            HResult = __HResults.COR_E_AMBIGUOUSMATCH;
        }

        public AmbiguousMatchException(string message)
            : base(message)
        {
            HResult = __HResults.COR_E_AMBIGUOUSMATCH;
        }

        public AmbiguousMatchException(string message, Exception inner)
            : base(message, inner)
        {
            HResult = __HResults.COR_E_AMBIGUOUSMATCH;
        }
    }
}
