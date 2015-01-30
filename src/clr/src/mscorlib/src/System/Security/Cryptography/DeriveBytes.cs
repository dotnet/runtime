// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Security.Cryptography {
[System.Runtime.InteropServices.ComVisible(true)]
    public abstract class DeriveBytes
    : IDisposable
    {
        //
        // public methods
        //

        public abstract byte[] GetBytes(int cb);
        public abstract void Reset();

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            return;
        }
    }
}
