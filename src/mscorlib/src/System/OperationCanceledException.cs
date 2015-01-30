// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
**
**
**
** Purpose: Exception for cancelled IO requests.
**
**
===========================================================*/

using System;
using System.Runtime.Serialization;
using System.Threading;

namespace System {

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class OperationCanceledException : SystemException
    {
        [NonSerialized] 
        private CancellationToken _cancellationToken;
        
        public CancellationToken CancellationToken
        {
            get { return _cancellationToken;}
            private set { _cancellationToken = value;}
        }

        public OperationCanceledException() 
            : base(Environment.GetResourceString("OperationCanceled")) {
            SetErrorCode(__HResults.COR_E_OPERATIONCANCELED);
        }
        
        public OperationCanceledException(String message) 
            : base(message) {
            SetErrorCode(__HResults.COR_E_OPERATIONCANCELED);
        }
    
        public OperationCanceledException(String message, Exception innerException) 
            : base(message, innerException) {
            SetErrorCode(__HResults.COR_E_OPERATIONCANCELED);
        }


        public OperationCanceledException(CancellationToken token)
            :this()
        {
            CancellationToken = token;
        }

        public OperationCanceledException(String message, CancellationToken token) 
            : this(message)
        {
            CancellationToken = token;
        }

        public OperationCanceledException(String message, Exception innerException, CancellationToken token)
            : this(message, innerException)
        {
            CancellationToken = token;
        }

        protected OperationCanceledException(SerializationInfo info, StreamingContext context) : base (info, context) {
        }
    }
}
