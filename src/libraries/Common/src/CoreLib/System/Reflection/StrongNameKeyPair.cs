// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.Serialization;

namespace System.Reflection
{
    public class StrongNameKeyPair : IDeserializationCallback, ISerializable
    {
        // Build key pair from file.
        public StrongNameKeyPair(FileStream keyPairFile)
        {
            if (keyPairFile == null)
                throw new ArgumentNullException(nameof(keyPairFile));

            int length = (int)keyPairFile.Length;
            byte[] keyPairArray = new byte[length];
            keyPairFile.Read(keyPairArray, 0, length);
        }

        // Build key pair from byte array in memory.
        public StrongNameKeyPair(byte[] keyPairArray)
        {
            if (keyPairArray == null)
                throw new ArgumentNullException(nameof(keyPairArray));
        }

        protected StrongNameKeyPair(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        public StrongNameKeyPair(string keyPairContainer)
        {
            throw new PlatformNotSupportedException(SR.PlatformNotSupported_StrongNameSigning);
        }

        public byte[] PublicKey
        {
            get
            {
                throw new PlatformNotSupportedException(SR.PlatformNotSupported_StrongNameSigning);
            }
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        void IDeserializationCallback.OnDeserialization(object sender)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
