// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*============================================================
 *
// 
 *
 *
 * Purpose: The exceptions in IsolatedStorage
 *
 *
 ===========================================================*/
namespace System.IO.IsolatedStorage {

    using System;
    using System.Runtime.Serialization;
    [Serializable]
[System.Runtime.InteropServices.ComVisible(true)]
    public class IsolatedStorageException : Exception
    {

#if FEATURE_CORECLR
        internal Exception m_UnderlyingException;
#endif 
        public IsolatedStorageException()
            : base(Environment.GetResourceString("IsolatedStorage_Exception"))
        {
            SetErrorCode(__HResults.COR_E_ISOSTORE);
        }

        public IsolatedStorageException(String message)
            : base(message)
        {
            SetErrorCode(__HResults.COR_E_ISOSTORE);
        }

        public IsolatedStorageException(String message, Exception inner)
            : base(message, inner)
        {
            SetErrorCode(__HResults.COR_E_ISOSTORE);
        }

        protected IsolatedStorageException(SerializationInfo info, StreamingContext context) : base (info, context) {
        }
    }
}
