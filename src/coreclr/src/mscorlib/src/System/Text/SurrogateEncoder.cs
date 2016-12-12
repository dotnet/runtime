// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// WARNING:
//
// This is just an IObjectReference proxy for the former V1.1 Surrogate Encoder
// All this does is make an encoder of the correct type, it DOES NOT maintain state.
namespace System.Text
{
    using System;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;

    /*=================================SurrogateEncoder==================================
    ** This class is here only to deserialize the SurrogateEncoder class from Everett (V1.1) into
    ** Appropriate Whidbey (V2.0) objects.
    ==============================================================================*/

    [Serializable]
    internal sealed class SurrogateEncoder : IObjectReference, ISerializable
    {
        // Might need this when GetRealObjecting
        [NonSerialized]
        private Encoding realEncoding = null;

        // Constructor called by serialization.
        internal SurrogateEncoder(SerializationInfo info, StreamingContext context)
        {
            // Any info?
            if (info==null) throw new ArgumentNullException(nameof(info));
            Contract.EndContractBlock();

            // All versions have a code page
            this.realEncoding = (Encoding)info.GetValue("m_encoding", typeof(Encoding));
        }

        // Just get it from GetEncoding
        public Object GetRealObject(StreamingContext context)
        {
            // Need to get our Encoding's Encoder
            return this.realEncoding.GetEncoder();
        }

        // ISerializable implementation
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // We cannot ever call this.
            Debug.Assert(false, "Didn't expect to make it to SurrogateEncoder.GetObjectData");
            throw new ArgumentException(Environment.GetResourceString("Arg_ExecutionEngineException"));
        }
    }
}

