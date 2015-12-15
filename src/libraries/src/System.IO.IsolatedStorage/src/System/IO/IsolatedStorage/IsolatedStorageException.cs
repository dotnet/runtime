// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.IO.IsolatedStorage
{
    public class IsolatedStorageException : Exception
    {
        private const int COR_E_ISOSTORE = unchecked((int)0x80131450);

        // All the exceptions from IsolatedStorage are wrapped as IsolatedStorageException,
        // this field is used to provide the underlying exception under debugger.
        internal Exception _underlyingException;

        public IsolatedStorageException()
            : base(SR.IsolatedStorage_Exception)
        {
            SetErrorCode(COR_E_ISOSTORE);
        }

        public IsolatedStorageException(String message)
            : base(message)
        {
            SetErrorCode(COR_E_ISOSTORE);
        }

        public IsolatedStorageException(String message, Exception inner)
            : base(message, inner)
        {
            SetErrorCode(COR_E_ISOSTORE);
        }

        private void SetErrorCode(int hr)
        {
            this.HResult = hr;
        }
    }
}