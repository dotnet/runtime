// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.Serialization;

namespace System.Reflection
{
    public class StrongNameKeyPair : IDeserializationCallback, ISerializable
    {
        private bool _keyPairExported;
        private byte[] _keyPairArray;
        private string _keyPairContainer;
        private byte[] _publicKey;

        // Build key pair from file.
        public StrongNameKeyPair(FileStream keyPairFile)
        {
            if (keyPairFile == null)
                throw new ArgumentNullException(nameof(keyPairFile));

            int length = (int)keyPairFile.Length;
            _keyPairArray = new byte[length];
            keyPairFile.Read(_keyPairArray, 0, length);

            _keyPairExported = true;
        }

        // Build key pair from byte array in memory.
        public StrongNameKeyPair(byte[] keyPairArray)
        {
            if (keyPairArray == null)
                throw new ArgumentNullException(nameof(keyPairArray));

            _keyPairArray = new byte[keyPairArray.Length];
            Array.Copy(keyPairArray, _keyPairArray, keyPairArray.Length);

            _keyPairExported = true;
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
