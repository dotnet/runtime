// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    public abstract class RandomNumberGenerator : IDisposable
    {
        protected RandomNumberGenerator()
        {
        }

        public static RandomNumberGenerator Create()
        {
            return new RNGCryptoServiceProvider();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
            return;
        }

        protected virtual void Dispose(bool disposing)
        {
            return;
        }

        public abstract void GetBytes(byte[] data);

        internal void ValidateGetBytesArgs(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
        }
    }
}

