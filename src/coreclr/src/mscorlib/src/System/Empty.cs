// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
////////////////////////////////////////////////////////////////////////////////
// Empty
//    This class represents an empty variant
////////////////////////////////////////////////////////////////////////////////
using System.Diagnostics.Contracts;
namespace System {
    
    using System;
    using System.Runtime.Remoting;
    using System.Runtime.Serialization;

    [Serializable]
    internal sealed class Empty : ISerializable
    {
        private Empty() {
        }
    
        public static readonly Empty Value = new Empty();
        
        public override String ToString()
        {
            return String.Empty;
        }
    
        [System.Security.SecurityCritical]  // auto-generated
        public void GetObjectData(SerializationInfo info, StreamingContext context) {
            if (info==null) {
                throw new ArgumentNullException("info");
            }
            Contract.EndContractBlock();
            UnitySerializationHolder.GetUnitySerializationInfo(info, UnitySerializationHolder.EmptyUnity, null, null);
        }
    }
}
