// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Text
{
    using System;
    using System.Runtime.Serialization;
    using System.Globalization;

    [Serializable]
    public sealed class DecoderExceptionFallback : DecoderFallback
    {
        // Construction
        public DecoderExceptionFallback()
        {
        }

        public override DecoderFallbackBuffer CreateFallbackBuffer()
        {
            return new DecoderExceptionFallbackBuffer();
        }

        // Maximum number of characters that this instance of this fallback could return
        public override int MaxCharCount
        {
            get
            {
                return 0;
            }
        }

        public override bool Equals(Object value)
        {
            DecoderExceptionFallback that = value as DecoderExceptionFallback;
            if (that != null)
            {
                return (true);
            }
            return (false);
        }

        public override int GetHashCode()
        {
            return 879;
        }
    }


    public sealed class DecoderExceptionFallbackBuffer : DecoderFallbackBuffer
    {
        public override bool Fallback(byte[] bytesUnknown, int index)
        {
            Throw(bytesUnknown, index);
            return true;
        }

        public override char GetNextChar()
        {
            return (char)0;
        }

        public override bool MovePrevious()
        {
            // Exception fallback doesn't have anywhere to back up to.
            return false;
        }

        // Exceptions are always empty
        public override int Remaining
        {
            get
            {
                return 0;
            }
        }

        private void Throw(byte[] bytesUnknown, int index)
        {
            // Create a string representation of our bytes.            
            StringBuilder strBytes = new StringBuilder(bytesUnknown.Length * 3);

            int i;
            for (i = 0; i < bytesUnknown.Length && i < 20; i++)
            {
                strBytes.Append("[");
                strBytes.Append(bytesUnknown[i].ToString("X2", CultureInfo.InvariantCulture));
                strBytes.Append("]");
            }
            
            // In case the string's really long
            if (i == 20)
                strBytes.Append(" ...");

            // Known index
            throw new DecoderFallbackException(
                Environment.GetResourceString("Argument_InvalidCodePageBytesIndex",
                   strBytes, index), bytesUnknown, index);           
        }

    }

    // Exception for decoding unknown byte sequences.
    [Serializable]
    public sealed class DecoderFallbackException : ArgumentException
    {
        byte[]    bytesUnknown = null;
        int       index = 0;

        public DecoderFallbackException()
            : base(Environment.GetResourceString("Arg_ArgumentException"))
        {
            SetErrorCode(__HResults.COR_E_ARGUMENT);
        }

        public DecoderFallbackException(String message)
            : base(message)
        {
            SetErrorCode(__HResults.COR_E_ARGUMENT);
        }

        public DecoderFallbackException(String message, Exception innerException)
            : base(message, innerException)
        {
            SetErrorCode(__HResults.COR_E_ARGUMENT);
        }

        internal DecoderFallbackException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public DecoderFallbackException(
            String message, byte[] bytesUnknown, int index) : base(message)
        {
            this.bytesUnknown = bytesUnknown;
            this.index = index;
        }

        public byte[] BytesUnknown
        {
            get
            {
                return (bytesUnknown);
            }
        }

        public int Index
        {
            get
            {
                return this.index;
            }
        }
    }
}
