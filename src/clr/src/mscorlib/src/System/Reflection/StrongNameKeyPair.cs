// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
** 
**
**
** Purpose: Encapsulate access to a public/private key pair
**          used to sign strong name assemblies.
**
**
===========================================================*/
namespace System.Reflection
{
    using System;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.InteropServices;
    using System.Runtime.Serialization;
    using System.Security;
    using System.Security.Permissions;
    using System.Runtime.Versioning;
    using Microsoft.Win32;
    using System.Diagnostics.Contracts;
#if !FEATURE_CORECLR
    using Microsoft.Runtime.Hosting;
#endif

#if FEATURE_CORECLR
    // Dummy type to avoid ifdefs in signature definitions
    public class StrongNameKeyPair
    {       
        private StrongNameKeyPair()
        {
            throw new NotSupportedException();
        }
    }
#else
    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class StrongNameKeyPair : IDeserializationCallback, ISerializable 
    {
        private bool    _keyPairExported;
        private byte[]  _keyPairArray;
        private String  _keyPairContainer;
        private byte[]  _publicKey;

        // Build key pair from file.
        [System.Security.SecuritySafeCritical]  // auto-generated
#pragma warning disable 618
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.UnmanagedCode)]
#pragma warning restore 618
        public StrongNameKeyPair(FileStream keyPairFile)
        {
            if (keyPairFile == null)
                throw new ArgumentNullException("keyPairFile");
            Contract.EndContractBlock();

            int length = (int)keyPairFile.Length;
            _keyPairArray = new byte[length];
            keyPairFile.Read(_keyPairArray, 0, length);

            _keyPairExported = true;
        }

        // Build key pair from byte array in memory.
        [System.Security.SecuritySafeCritical]  // auto-generated
#pragma warning disable 618
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.UnmanagedCode)]
#pragma warning restore 618
        public StrongNameKeyPair(byte[] keyPairArray)
        {
            if (keyPairArray == null)
                throw new ArgumentNullException("keyPairArray");
            Contract.EndContractBlock();

            _keyPairArray = new byte[keyPairArray.Length];
            Array.Copy(keyPairArray, _keyPairArray, keyPairArray.Length);

            _keyPairExported = true;
        }

        // Reference key pair in named key container.
        [System.Security.SecuritySafeCritical]  // auto-generated
#pragma warning disable 618
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.UnmanagedCode)]
#pragma warning restore 618
        public StrongNameKeyPair(String keyPairContainer)
        {
            if (keyPairContainer == null)
                throw new ArgumentNullException("keyPairContainer");
            Contract.EndContractBlock();

            _keyPairContainer = keyPairContainer;

            _keyPairExported = false;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
#pragma warning disable 618
        [SecurityPermissionAttribute(SecurityAction.Demand, Flags=SecurityPermissionFlag.UnmanagedCode)]
#pragma warning restore 618
        protected StrongNameKeyPair (SerializationInfo info, StreamingContext context) {
            _keyPairExported = (bool) info.GetValue("_keyPairExported", typeof(bool));
            _keyPairArray = (byte[]) info.GetValue("_keyPairArray", typeof(byte[]));
            _keyPairContainer = (string) info.GetValue("_keyPairContainer", typeof(string));
            _publicKey = (byte[]) info.GetValue("_publicKey", typeof(byte[]));
        }

        // Get the public portion of the key pair.
        public byte[] PublicKey
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                if (_publicKey == null)
                {
                    _publicKey = ComputePublicKey();
                }

                byte[] publicKey = new byte[_publicKey.Length];
                Array.Copy(_publicKey, publicKey, _publicKey.Length);

                return publicKey;
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        private unsafe byte[] ComputePublicKey()
        {
            byte[] publicKey = null;

            // Make sure pbPublicKey is not leaked with async exceptions
            RuntimeHelpers.PrepareConstrainedRegions();
            try {
            }
            finally
            {
                IntPtr pbPublicKey = IntPtr.Zero;
                int cbPublicKey = 0;

                try
                {
                    bool result;
                    if (_keyPairExported)
                    {
                        result = StrongNameHelpers.StrongNameGetPublicKey(null, _keyPairArray, _keyPairArray.Length,
                            out pbPublicKey, out cbPublicKey);
                    }
                    else
                    {
                        result = StrongNameHelpers.StrongNameGetPublicKey(_keyPairContainer, null, 0,
                            out pbPublicKey, out cbPublicKey);
                    }
                    if (!result)
                        throw new ArgumentException(Environment.GetResourceString("Argument_StrongNameGetPublicKey"));

                    publicKey = new byte[cbPublicKey];
                    Buffer.Memcpy(publicKey, 0, (byte*)(pbPublicKey.ToPointer()), 0, cbPublicKey);
                }
                finally
                {
                    if (pbPublicKey != IntPtr.Zero)
                        StrongNameHelpers.StrongNameFreeBuffer(pbPublicKey);
                }
            }
            return publicKey;
        }

        /// <internalonly/>
        [System.Security.SecurityCritical]
        void ISerializable.GetObjectData (SerializationInfo info, StreamingContext context) {
            info.AddValue("_keyPairExported", _keyPairExported);
            info.AddValue("_keyPairArray", _keyPairArray);
            info.AddValue("_keyPairContainer", _keyPairContainer);
            info.AddValue("_publicKey", _publicKey);
        }

        /// <internalonly/>
        void IDeserializationCallback.OnDeserialization (Object sender) {}

        // Internal routine used to retrieve key pair info from unmanaged code.
        private bool GetKeyPair(out Object arrayOrContainer)
        {
            arrayOrContainer = _keyPairExported ? (Object)_keyPairArray : (Object)_keyPairContainer;
            return _keyPairExported;
        }
    }
#endif // FEATURE_CORECLR
}
