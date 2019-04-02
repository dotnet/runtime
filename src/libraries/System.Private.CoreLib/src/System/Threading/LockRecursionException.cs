// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Runtime.Serialization;

namespace System.Threading
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class LockRecursionException : System.Exception
    {
        public LockRecursionException()
        {
        }

        public LockRecursionException(string? message)
            : base(message)
        {
        }

        public LockRecursionException(string? message, Exception? innerException)
            : base(message, innerException)
        {
        }

        protected LockRecursionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
