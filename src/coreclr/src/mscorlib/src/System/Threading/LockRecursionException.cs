// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
//
//
//
// Purpose: 
// This exception represents a failed attempt to recursively
// acquire a lock, because the particular lock kind doesn't
// support it in its current state.
============================================================*/

using System;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    [Serializable]
    public class LockRecursionException : System.Exception
    {
        public LockRecursionException() { }
        public LockRecursionException(string message) : base(message) { }
        protected LockRecursionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        public LockRecursionException(string message, Exception innerException) : base(message, innerException) { }
    }
}
