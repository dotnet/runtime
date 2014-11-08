// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Reflection.Internal;
using System.Diagnostics;

namespace System.Reflection.Metadata
{
    /// <summary>
    /// Provides the <see cref="MetadataReader"/> with a custom mechansim for decoding
    /// byte sequences in metadata that represent text.
    /// </summary>
    /// <remarks>
    /// This can be used for the following purposes:
    /// 
    /// 1) To customize the treatment of invalid input. When no decoder is provided,
    ///    the <see cref="MetadataReader"/> uses the default fallback replacement 
    ///    with \uFFFD)
    ///
    /// 2) To reuse existing strings instead of allocating a new one for each decoding
    ///    operation.
    /// </remarks>
    public class MetadataStringDecoder
    {
        private static readonly MetadataStringDecoder defaultUTF8 = new MetadataStringDecoder(Encoding.UTF8);
        private readonly Encoding encoding;

        /// <summary>
        /// The default decoder used by <see cref="MetadataReader"/> to decode UTF-8 when
        /// no decoder is provided to the constructor.
        /// </summary>
        public static MetadataStringDecoder DefaultUTF8
        {
            get { return defaultUTF8; }
        }

        /// <summary>
        /// Creates a <see cref="MetadataStringDecoder"/> for the given encoding.
        /// </summary>
        /// <param name="encoding">The encoding to use.</param>
        /// <remarks>
        /// To cache and reuse existing strings. Create a derived class and override <see cref="GetString(byte*, int)"/> 
        /// </remarks>
        public MetadataStringDecoder(Encoding encoding)
        {
            if (encoding == null)
            {
                throw new ArgumentNullException("encoding");
            }

            // Non-enforcement of (encoding is UTF8Encoding) here is by design.
            //
            // This type is not itself aware of any particular encoding. However, the constructor argument that accepts a 
            // MetadataStringDecoderargument is validated however because it must be a UTF8 decoder.
            //
            // Above architectural purity, the fact that you can get our default implementation of Encoding.GetString
            // is a hidden feature to use our light-up of unsafe Encoding.GetSring outside this assembly on an arbitrary 
            // encoding. I'm more comfortable sharing that hack than having the reflection over internal 
            // CreateStringFromEncoding spread.

            this.encoding = encoding;
        }

        /// <summary>
        /// Gets the encoding used by this instance. 
        /// </summary>
        public Encoding Encoding
        {
            get { return this.encoding; }
        }

        /// <summary>
        /// The mechanism through which the <see cref="MetadataReader"/> obtains strings
        /// for byte sequences in metadata. Override this to cache strings if required.
        /// Otherwise, it is implemented by forwarding straight to <see cref="Encoding"/>
        /// and every call will allocate a new string.
        /// </summary>
        /// <param name="bytes">Pointer to bytes to decode.</param>
        /// <param name="byteCount">Number of bytes to decode.</param>
        /// <returns>The decoded string.</returns>
        public unsafe virtual String GetString(byte* bytes, int byteCount)
        {
            Debug.Assert(this.encoding != null);

            // Note that this call is currently wired to the light-up extension in EncodingHelper
            // for portability.
            return this.encoding.GetString(bytes, byteCount);
        }
    }
}
