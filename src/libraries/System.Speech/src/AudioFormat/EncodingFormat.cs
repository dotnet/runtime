// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Speech.AudioFormat
{

    /// TODOC <_include file='doc\EncodingFormat.uex' path='docs/doc[@for="AudioFormatTag"]/*' />
    // These enumeration values are the same values used in the WAVEFORMATEX structure used in wave files.
    public
        enum EncodingFormat
    {
        /// TODOC <_include file='doc\EncodingFormat.uex' path='docs/doc[@for="EncodingFormatTag.PCM"]/*' />
        Pcm = 0x0001,

        /// TODOC <_include file='doc\EncodingFormat.uex' path='docs/doc[@for="EncodingFormatTag.ALaw"]/*' />
        ALaw = 0x0006,

        /// TODOC <_include file='doc\EncodingFormat.uex' path='docs/doc[@for="EncodingFormatTag.ULaw"]/*' />
        ULaw = 0x0007
    }

}
