// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Speech.Internal.Synthesis
{
    [Serializable]
    internal class AudioException : Exception
    {
        #region Constructors
        internal AudioException()
        {
        }
        internal AudioException(Interop.WinMM.MMSYSERR errorCode) : base(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} - Error Code: 0x{1:x}", SR.Get(SRID.AudioDeviceError), (int)errorCode))
        {
        }

#if NET8_0_OR_GREATER
        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
#endif
        protected AudioException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        #endregion
    }
}
