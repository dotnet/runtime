// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// WARNING:
//
// This is just an IObjectReference proxy for the Code Page Encodings.
namespace System.Text
{
    using System;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Diagnostics.Contracts;

    /*=================================CodePageEncoding==================================
    ** This class is here only to deserialize the Code Page classes from Everett (V1.1) into
    ** Appropriate Whidbey (V2.0) objects.  We also serialize the Whidbey classes
    ** using this proxy since we pretty much need one anyway and that solves Whidbey
    ** to Everett compatibility as well.
    ==============================================================================*/

    [Serializable]
    internal sealed class CodePageEncoding : IObjectReference, ISerializable
    {
        // Temp stuff
        [NonSerialized]
        private int m_codePage;
        [NonSerialized]
        private bool m_isReadOnly;
        [NonSerialized]
        private bool m_deserializedFromEverett = false;

        [NonSerialized]
        private EncoderFallback encoderFallback = null;
        [NonSerialized]
        private DecoderFallback decoderFallback = null;

        // Might need this when GetRealObjecting
        [NonSerialized]
        private Encoding realEncoding = null;

        // Constructor called by serialization.
        internal CodePageEncoding(SerializationInfo info, StreamingContext context)
        {
            // Any info?
            if (info==null) throw new ArgumentNullException("info");
            Contract.EndContractBlock();

            // All versions have a code page
            this.m_codePage = (int)info.GetValue("m_codePage", typeof(int));

            // See if we have a code page
            try
            {
                //
                // Try Whidbey V2.0 Fields
                //
                this.m_isReadOnly = (bool)info.GetValue("m_isReadOnly", typeof(bool));

                this.encoderFallback = (EncoderFallback)info.GetValue("encoderFallback", typeof(EncoderFallback));
                this.decoderFallback = (DecoderFallback)info.GetValue("decoderFallback", typeof(DecoderFallback));
            }
            catch (SerializationException)
            {
                //
                // Didn't have Whidbey things, must be Everett
                //
                this.m_deserializedFromEverett = true;

                // May as well be read only
                this.m_isReadOnly = true;
            }
        }

        // Just get it from GetEncoding
        [System.Security.SecurityCritical]  // auto-generated
        public Object GetRealObject(StreamingContext context)
        {
            // Get our encoding (Note: This has default fallbacks for readonly and everett cases)
            this.realEncoding = Encoding.GetEncoding(this.m_codePage);

            // If its read only then it uses default fallbacks, otherwise pick up the new ones
            // Otherwise we want to leave the new one read only
            if (!this.m_deserializedFromEverett && !this.m_isReadOnly)
            {
                this.realEncoding = (Encoding)this.realEncoding.Clone();
                this.realEncoding.EncoderFallback = this.encoderFallback;
                this.realEncoding.DecoderFallback = this.decoderFallback;
            }

            return this.realEncoding;
        }

        // ISerializable implementation
        [System.Security.SecurityCritical]  // auto-generated_required
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // We cannot ever call this.
            Contract.Assert(false, "Didn't expect to make it to CodePageEncoding ISerializable.GetObjectData");
            throw new ArgumentException(Environment.GetResourceString("Arg_ExecutionEngineException"));
        }

        // Same problem with the Decoder, this only happens with Everett Decoders
        [Serializable]
        internal sealed class Decoder : IObjectReference, ISerializable
        {
            // Might need this when GetRealObjecting
            [NonSerialized]
            private Encoding realEncoding = null;

            // Constructor called by serialization, have to handle deserializing from Everett
            internal Decoder(SerializationInfo info, StreamingContext context)
            {
                // Any info?
                if (info==null) throw new ArgumentNullException("info");
                Contract.EndContractBlock();

                this.realEncoding = (Encoding)info.GetValue("encoding", typeof(Encoding));
            }

            // Just get it from GetDecider
            [System.Security.SecurityCritical]  // auto-generated
            public Object GetRealObject(StreamingContext context)
            {
                return this.realEncoding.GetDecoder();
            }

            // ISerializable implementation, get data for this object
            [System.Security.SecurityCritical]  // auto-generated_required
            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            {
                // We cannot ever call this.
                Contract.Assert(false, "Didn't expect to make it to CodePageEncoding.Decoder.GetObjectData");
                throw new ArgumentException(Environment.GetResourceString("Arg_ExecutionEngineException"));
            }
        }
    }
}
