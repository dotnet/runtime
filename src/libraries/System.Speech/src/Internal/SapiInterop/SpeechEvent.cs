// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Speech.AudioFormat;

namespace System.Speech.Internal.SapiInterop
{
    // Internal helper class that wraps a SAPI event structure.
    // A new instance is created by calling SpeechEvent.TryCreateSpeechEvent
    // Disposing this class will dispose all unmanaged memory.
    internal class SpeechEvent : IDisposable
    {
        #region Constructors

        private SpeechEvent(SPEVENTENUM eEventId, SPEVENTLPARAMTYPE elParamType,
            ulong ullAudioStreamOffset, IntPtr wParam, IntPtr lParam)
        {
            // We make a copy of the SPEVENTEX data but that's okay because the lParam will only be deleted once.
            _eventId = eEventId;
            _paramType = elParamType;
            _audioStreamOffset = ullAudioStreamOffset;
            _wParam = (ulong)wParam.ToInt64();
            _lParam = (ulong)lParam;

            // Let the GC know if we have a unmanaged object with a given size
            if (_paramType == SPEVENTLPARAMTYPE.SPET_LPARAM_IS_POINTER || _paramType == SPEVENTLPARAMTYPE.SPET_LPARAM_IS_STRING)
            {
                GC.AddMemoryPressure(_sizeMemoryPressure = sizeof(ulong));
            }
        }

        private SpeechEvent(SPEVENT sapiEvent, SpeechAudioFormatInfo audioFormat)
            : this(sapiEvent.eEventId, sapiEvent.elParamType, sapiEvent.ullAudioStreamOffset, sapiEvent.wParam, sapiEvent.lParam)
        {
            if (audioFormat == null || audioFormat.EncodingFormat == 0)
            {
                _audioPosition = TimeSpan.Zero;
            }
            else
            {
                _audioPosition = audioFormat.AverageBytesPerSecond > 0 ? new TimeSpan((long)((sapiEvent.ullAudioStreamOffset * TimeSpan.TicksPerSecond) / (ulong)audioFormat.AverageBytesPerSecond)) : TimeSpan.Zero;
            }
        }

        private SpeechEvent(SPEVENTEX sapiEventEx) : this(sapiEventEx.eEventId, sapiEventEx.elParamType, sapiEventEx.ullAudioStreamOffset, sapiEventEx.wParam, sapiEventEx.lParam)
        {
            _audioPosition = new TimeSpan((long)sapiEventEx.ullAudioTimeOffset);
        }

        ~SpeechEvent()
        {
            Dispose();
        }

        public void Dispose()
        {
            // General code to free event data
            if (_lParam != 0)
            {
                if (_paramType == SPEVENTLPARAMTYPE.SPET_LPARAM_IS_TOKEN || _paramType == SPEVENTLPARAMTYPE.SPET_LPARAM_IS_OBJECT)
                {
                    Marshal.Release((IntPtr)_lParam);
                }
                else
                {
                    if (_paramType == SPEVENTLPARAMTYPE.SPET_LPARAM_IS_POINTER || _paramType == SPEVENTLPARAMTYPE.SPET_LPARAM_IS_STRING)
                    {
                        Marshal.FreeCoTaskMem((IntPtr)_lParam);
                    }
                }

                // Update the GC
                if (_sizeMemoryPressure > 0)
                {
                    GC.RemoveMemoryPressure(_sizeMemoryPressure);
                    _sizeMemoryPressure = 0;
                }

                // Mark the object as being freed
                _lParam = 0;
            }
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Internal Methods

        // This tries to get an event from the ISpEventSource.
        // If there are no events queued then null is returned.
        // Otherwise a new SpeechEvent is created and returned.
        internal static SpeechEvent TryCreateSpeechEvent(ISpEventSource sapiEventSource, bool additionalSapiFeatures, SpeechAudioFormatInfo audioFormat)
        {
            uint fetched;
            SpeechEvent speechEvent = null;
            if (additionalSapiFeatures)
            {
                SPEVENTEX sapiEventEx;
                ((ISpEventSource2)sapiEventSource).GetEventsEx(1, out sapiEventEx, out fetched);
                if (fetched == 1)
                {
                    speechEvent = new SpeechEvent(sapiEventEx);
                }
            }
            else
            {
                SPEVENT sapiEvent;
                sapiEventSource.GetEvents(1, out sapiEvent, out fetched);
                if (fetched == 1)
                {
                    speechEvent = new SpeechEvent(sapiEvent, audioFormat);
                }
            }

            return speechEvent;
        }

        #endregion

        #region Internal Properties

        internal SPEVENTENUM EventId
        {
            get { return _eventId; }
        }
        internal ulong AudioStreamOffset
        {
            get { return _audioStreamOffset; }
        }

        // The WParam is returned as a 64-bit value since unmanaged wParam is always 32 or 64 depending on architecture.
        // This is always some kind of numeric value in SAPI - it is never a pointer that needs to freed.
        internal ulong WParam
        {
            get { return _wParam; }
        }

        internal ulong LParam
        {
            get { return _lParam; }
        }

        internal TimeSpan AudioPosition
        {
            get { return _audioPosition; }
        }

        #endregion

        #region Private Fields

        private SPEVENTENUM _eventId;
        private SPEVENTLPARAMTYPE _paramType;
        private ulong _audioStreamOffset;
        private ulong _wParam;
        private ulong _lParam;
        private TimeSpan _audioPosition;
        private int _sizeMemoryPressure;

        #endregion
    }

    internal enum SPEVENTLPARAMTYPE : ushort
    {
        SPET_LPARAM_IS_UNDEFINED = 0x0000,
        SPET_LPARAM_IS_TOKEN = 0x0001,
        SPET_LPARAM_IS_OBJECT = 0x0002,
        SPET_LPARAM_IS_POINTER = 0x0003,
        SPET_LPARAM_IS_STRING = 0x0004
    }
}
