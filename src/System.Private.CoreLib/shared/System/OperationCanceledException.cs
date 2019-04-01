// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: Exception for cancelled IO requests.
**
**
===========================================================*/

#nullable enable
using System;
using System.Runtime.Serialization;
using System.Threading;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public class OperationCanceledException : SystemException
    {
        [NonSerialized]
        private CancellationToken _cancellationToken;

        public CancellationToken CancellationToken
        {
            get { return _cancellationToken; }
            private set { _cancellationToken = value; }
        }

        public OperationCanceledException()
            : base(SR.OperationCanceled)
        {
            HResult = HResults.COR_E_OPERATIONCANCELED;
        }

        public OperationCanceledException(string? message)
            : base(message)
        {
            HResult = HResults.COR_E_OPERATIONCANCELED;
        }

        public OperationCanceledException(string? message, Exception? innerException)
            : base(message, innerException)
        {
            HResult = HResults.COR_E_OPERATIONCANCELED;
        }


        public OperationCanceledException(CancellationToken token)
            : this()
        {
            CancellationToken = token;
        }

        public OperationCanceledException(string? message, CancellationToken token)
            : this(message)
        {
            CancellationToken = token;
        }

        public OperationCanceledException(string? message, Exception? innerException, CancellationToken token)
            : this(message, innerException)
        {
            CancellationToken = token;
        }

        protected OperationCanceledException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
