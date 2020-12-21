// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Speech.Synthesis.TtsEngine;
using System.Speech.Internal.SapiInterop;

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
        /// <param name="eventsSapi"></param>
        /// <param name="ulCount"></param>
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
        /// <returns></returns>
        [PreserveSig]
        int ISpEngineSite.GetActions()
        {
            return _site.Actions;
        }

        /// <summary>
        /// Queries the voice object to determine which real-time action(s) to perform.
        /// </summary>
        /// <param name="pBuff"></param>
        /// <param name="cb"></param>
        /// <param name="pcbWritten"></param>
        void ISpEngineSite.Write(IntPtr pBuff, int cb, IntPtr pcbWritten)
        {
            pcbWritten = (IntPtr)_site.Write(pBuff, cb);
        }

        /// <summary>
        ///  Retrieves the current TTS rendering rate adjustment that should be used by the engine.
        /// </summary>
        /// <param name="pRateAdjust"></param>
        void ISpEngineSite.GetRate(out int pRateAdjust)
        {
            pRateAdjust = _site.Rate;
        }

        /// <summary>
        /// Retrieves the base output volume level the engine should use during synthesis.
        /// </summary>
        /// <param name="pusVolume"></param>
        void ISpEngineSite.GetVolume(out short pusVolume)
        {
            pusVolume = (short)_site.Volume;
        }

        /// <summary>
        /// Retrieves the number and type of items to be skipped in the text stream.
        /// </summary>
        /// <param name="peType"></param>
        /// <param name="plNumItems"></param>
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
        /// <param name="ulNumSkipped"></param>
        void ISpEngineSite.CompleteSkip(int ulNumSkipped)
        {
            _site.CompleteSkip(ulNumSkipped);
        }

        /// <summary>
        /// Load a file either from a local network or from the Internet.
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="mediaType"></param>
        /// <param name="stream"></param>
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

    /// <summary>
    /// TODOC
    /// </summary>
    [ComImport, Guid("9880499B-CCE9-11D2-B503-00C04F797396"), System.Runtime.InteropServices.InterfaceTypeAttribute(1)]
    internal interface ISpEngineSite
    {
        /// <summary>
        /// TODOC
        /// </summary>
        void AddEvents([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] SpeechEventSapi[] events, int count);
        /// <summary>
        /// TODOC
        /// </summary>
        void GetEventInterest(out long eventInterest);
        /// <summary>
        /// TODOC
        /// </summary>
        [PreserveSig]
        int GetActions();
        /// <summary>
        /// TODOC
        /// </summary>
        void Write(IntPtr data, int count, IntPtr bytesWritten);
        /// <summary>
        /// TODOC
        /// </summary>
        void GetRate(out int rate);
        /// <summary>
        /// TODOC
        /// </summary>
        void GetVolume(out short volume);
        /// <summary>
        /// TODOC
        /// </summary>
        void GetSkipInfo(out int type, out int count);
        /// <summary>
        /// TODOC
        /// </summary>
        void CompleteSkip(int skipped);
        /// <summary>
        /// TODOC
        /// </summary>
        void LoadResource([MarshalAs(UnmanagedType.LPWStr)] string resource, ref string mediaType, out IStream stream);
    }

    /// <summary>
    /// TODOC
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct SpeechEventSapi
    {
        /// <summary>
        /// TODOC
        /// </summary>
        public short EventId;
        /// <summary>
        /// TODOC
        /// </summary>
        public short ParameterType;
        /// <summary>
        /// TODOC
        /// </summary>
        public int StreamNumber;
        /// <summary>
        /// TODOC
        /// </summary>
        public long AudioStreamOffset;
        /// <summary>
        /// TODOC
        /// </summary>
        public IntPtr Param1;   // Always just a numeric type - contains no unmanaged resources so does not need special clean-up.
        /// <summary>
        /// TODOC
        /// </summary>
        public IntPtr Param2;   // Can be a numeric type, or pointer to string or object. Use SafeSapiLParamHandle to cleanup.

        /// TODOC
        public static bool operator ==(SpeechEventSapi event1, SpeechEventSapi event2)
        {
            return event1.EventId == event2.EventId && event1.ParameterType == event2.ParameterType && event1.StreamNumber == event2.StreamNumber && event1.AudioStreamOffset == event2.AudioStreamOffset && event1.Param1 == event2.Param1 && event1.Param2 == event2.Param2;
        }

        /// TODOC
        public static bool operator !=(SpeechEventSapi event1, SpeechEventSapi event2)
        {
            return !(event1 == event2);
        }

        /// TODOC
        public override bool Equals(object obj)
        {
            if (!(obj is SpeechEventSapi))
            {
                return false;
            }

            return this == (SpeechEventSapi)obj;
        }

        /// TODOC
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    #endregion
}
