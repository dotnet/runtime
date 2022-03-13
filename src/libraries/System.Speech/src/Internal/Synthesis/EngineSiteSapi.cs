// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Speech.Internal.SapiInterop;
using System.Speech.Synthesis.TtsEngine;

#pragma warning disable 56500 // Remove all the catch all statements warnings used by the interop layer

namespace System.Speech.Internal.Synthesis
{
    [ComVisible(true)]
    internal class EngineSiteSapi : ISpEngineSite
    {
        #region Constructors

        internal EngineSiteSapi(EngineSite site, ResourceLoader resourceLoader)
        {
            _site = site;
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Adds events directly to an event sink.
        /// </summary>
        void ISpEngineSite.AddEvents([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] SpeechEventSapi[] eventsSapi, int ulCount)
        {
            SpeechEventInfo[] events = new SpeechEventInfo[eventsSapi.Length];
            for (int i = 0; i < eventsSapi.Length; i++)
            {
                SpeechEventSapi sapiEvt = eventsSapi[i];
                events[i].EventId = sapiEvt.EventId;
                events[i].ParameterType = sapiEvt.ParameterType;
                events[i].Param1 = (int)sapiEvt.Param1;
                events[i].Param2 = sapiEvt.Param2;
            }
            _site.AddEvents(events, ulCount);
        }

        /// <summary>
        /// Passes back the event interest for the voice.
        /// </summary>
        void ISpEngineSite.GetEventInterest(out long eventInterest)
        {
            eventInterest = (uint)_site.EventInterest;
        }

        /// <summary>
        ///  Queries the voice object to determine which real-time action(s) to perform
        /// </summary>
        [PreserveSig]
        int ISpEngineSite.GetActions()
        {
            return _site.Actions;
        }

        /// <summary>
        /// Queries the voice object to determine which real-time action(s) to perform.
        /// </summary>
        void ISpEngineSite.Write(IntPtr pBuff, int cb, IntPtr pcbWritten)
        {
            pcbWritten = (IntPtr)_site.Write(pBuff, cb);
        }

        /// <summary>
        ///  Retrieves the current TTS rendering rate adjustment that should be used by the engine.
        /// </summary>
        void ISpEngineSite.GetRate(out int pRateAdjust)
        {
            pRateAdjust = _site.Rate;
        }

        /// <summary>
        /// Retrieves the base output volume level the engine should use during synthesis.
        /// </summary>
        void ISpEngineSite.GetVolume(out short pusVolume)
        {
            pusVolume = (short)_site.Volume;
        }

        /// <summary>
        /// Retrieves the number and type of items to be skipped in the text stream.
        /// </summary>
        void ISpEngineSite.GetSkipInfo(out int peType, out int plNumItems)
        {
            SkipInfo si = _site.GetSkipInfo();
            if (si != null)
            {
                peType = si.Type;
                plNumItems = si.Count;
            }
            else
            {
                peType = 1; // BSPVSKIPTYPE.SPVST_SENTENCE;
                plNumItems = 0;
            }
        }

        /// <summary>
        /// Notifies that the last skip request has been completed and to pass it the results.
        /// </summary>
        void ISpEngineSite.CompleteSkip(int ulNumSkipped)
        {
            _site.CompleteSkip(ulNumSkipped);
        }

        /// <summary>
        /// Load a file either from a local network or from the Internet.
        /// </summary>
        void ISpEngineSite.LoadResource(string uri, ref string mediaType, out IStream stream)
        {
            mediaType = null;
#pragma warning disable 56518 // BinaryReader can't be disposed because underlying stream still in use.
            try
            {
                // Get the mime type
                Stream localStream = _site.LoadResource(new Uri(uri, UriKind.RelativeOrAbsolute), mediaType);
                BinaryReader reader = new(localStream);
                byte[] waveFormat = System.Speech.Internal.Synthesis.AudioBase.GetWaveFormat(reader);
                mediaType = null;
                if (waveFormat != null)
                {
                    WAVEFORMATEX hdr = WAVEFORMATEX.ToWaveHeader(waveFormat);
                    switch ((WaveFormatId)hdr.wFormatTag)
                    {
                        case WaveFormatId.Alaw:
                        case WaveFormatId.Mulaw:
                        case WaveFormatId.Pcm:
                            mediaType = "audio/x-wav";
                            break;
                    }
                }
                localStream.Position = 0;
                stream = new SpStreamWrapper(localStream);
            }
            catch
            {
                stream = null;
            }
#pragma warning restore 56518
        }

        #endregion

        #region private Fields

        private EngineSite _site;

        private enum WaveFormatId
        {
            Pcm = 1,
            Alaw = 0x0006,
            Mulaw = 0x0007,
        }

        #endregion
    }

    #region Internal Interfaces
    [ComImport, Guid("9880499B-CCE9-11D2-B503-00C04F797396"), System.Runtime.InteropServices.InterfaceTypeAttribute(1)]
    internal interface ISpEngineSite
    {
        void AddEvents([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] SpeechEventSapi[] events, int count);
        void GetEventInterest(out long eventInterest);
        [PreserveSig]
        int GetActions();
        void Write(IntPtr data, int count, IntPtr bytesWritten);
        void GetRate(out int rate);
        void GetVolume(out short volume);
        void GetSkipInfo(out int type, out int count);
        void CompleteSkip(int skipped);
        void LoadResource([MarshalAs(UnmanagedType.LPWStr)] string resource, ref string mediaType, out IStream stream);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SpeechEventSapi : IEquatable<SpeechEventSapi>
    {
        public short EventId;
        public short ParameterType;
        public int StreamNumber;
        public long AudioStreamOffset;
        public IntPtr Param1;   // Always just a numeric type - contains no unmanaged resources so does not need special clean-up.
        public IntPtr Param2;   // Can be a numeric type, or pointer to string or object. Use SafeSapiLParamHandle to cleanup.

        public static bool operator ==(SpeechEventSapi event1, SpeechEventSapi event2) => event1.Equals(event2);
        public static bool operator !=(SpeechEventSapi event1, SpeechEventSapi event2) => !event1.Equals(event2);

        public override bool Equals(object obj) =>
            obj is SpeechEventSapi other && Equals(other);

        public bool Equals(SpeechEventSapi other) =>
            EventId == other.EventId &&
            ParameterType == other.ParameterType &&
            StreamNumber == other.StreamNumber &&
            AudioStreamOffset == other.AudioStreamOffset &&
            Param1 == other.Param1 &&
            Param2 == other.Param2;

        public override int GetHashCode() => base.GetHashCode();
    }

    #endregion
}
