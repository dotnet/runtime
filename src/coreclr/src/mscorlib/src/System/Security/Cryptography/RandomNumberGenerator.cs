// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography {

#if !FEATURE_CORECLR
    [System.Runtime.InteropServices.ComVisible(true)]
#endif // !FEATURE_CORECLR
    public abstract class RandomNumberGenerator : IDisposable
    {
        protected RandomNumberGenerator() {
        }
    
        //
        // public methods
        //

        static public RandomNumberGenerator Create() {
            return Create("System.Security.Cryptography.RandomNumberGenerator");
        }

        static public RandomNumberGenerator Create(String rngName) {
            return (RandomNumberGenerator) CryptoConfig.CreateFromName(rngName);
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            return;
        }

        public abstract void GetBytes(byte[] data);

        public virtual void GetBytes(byte[] data, int offset, int count) {
            if (data == null) throw new ArgumentNullException("data");
            if (offset < 0) throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0) throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (offset + count > data.Length) throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));

            if (count > 0) {
                byte[] tempData = new byte[count];
                GetBytes(tempData);
                Array.Copy(tempData, 0, data, offset, count);
            }
        }

        public virtual void GetNonZeroBytes(byte[] data)
        {
            // This method does not exist on Silverlight, so for compatibility we cannot have it be abstract
            // on the desktop (otherwise any type deriving from RandomNumberGenerator on Silverlight cannot
            // compile against the desktop CLR).  Since this technically is an abstract method with no
            // implementation, we'll just throw NotImplementedException.
            throw new NotImplementedException();
        }
    }
}
