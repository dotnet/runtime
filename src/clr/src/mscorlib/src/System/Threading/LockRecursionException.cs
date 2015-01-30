// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
//
//
//
// Purpose: 
// This exception represents a failed attempt to recursively
// acquire a lock, because the particular lock kind doesn't
// support it in its current state.
============================================================*/

namespace System.Threading
{
    using System;
    using System.Runtime.Serialization;
    using System.Runtime.CompilerServices;

    [Serializable]
    [System.Security.Permissions.HostProtection(MayLeakOnAbort = true)]
    [TypeForwardedFrom("System.Core, Version=3.5.0.0, Culture=Neutral, PublicKeyToken=b77a5c561934e089")]
    public class LockRecursionException : System.Exception
    {
        public LockRecursionException() { }
        public LockRecursionException(string message) : base(message) { }
        protected LockRecursionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        public LockRecursionException(string message, Exception innerException) : base(message, innerException) { }
    }

}
