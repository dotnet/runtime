// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Speech.Internal.SapiInterop
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SPEVENT
    {
        public SPEVENTENUM eEventId;
        public SPEVENTLPARAMTYPE elParamType;
        public uint ulStreamNum;
        public ulong ullAudioStreamOffset;
        public IntPtr wParam;   // Always just a numeric type - contains no unmanaged resources so does not need special clean-up.
        public IntPtr lParam;   // Can be a numeric type, or pointer to string or object. Use SafeSapiLParamHandle to cleanup.
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SPEVENTEX
    {
        public SPEVENTENUM eEventId;
        public SPEVENTLPARAMTYPE elParamType;
        public uint ulStreamNum;
        public ulong ullAudioStreamOffset;
        public IntPtr wParam;   // Always just a numeric type - contains no unmanaged resources so does not need special clean-up.
        public IntPtr lParam;   // Can be a numeric type, or pointer to string or object. Use SafeSapiLParamHandle to cleanup.
        public ulong ullAudioTimeOffset;
    }

    internal enum SPEVENTENUM : ushort
    {
        SPEI_UNDEFINED = 0,

        // TTS engine
        SPEI_START_INPUT_STREAM = 1,
        SPEI_END_INPUT_STREAM = 2,
        SPEI_VOICE_CHANGE = 3,   // LPARAM_IS_TOKEN
        SPEI_TTS_BOOKMARK = 4,   // LPARAM_IS_STRING
        SPEI_WORD_BOUNDARY = 5,
        SPEI_PHONEME = 6,
        SPEI_SENTENCE_BOUNDARY = 7,
        SPEI_VISEME = 8,
        SPEI_TTS_AUDIO_LEVEL = 9,   // wParam contains current output audio level

        // TTS engine vendors use these reserved bits
        SPEI_TTS_PRIVATE = 15,
        SPEI_MIN_TTS = 1,
        SPEI_MAX_TTS = 15,

        // Speech Recognition
        SPEI_END_SR_STREAM = 34,  // LPARAM contains HRESULT, WPARAM contains flags (SPESF_xxx)
        SPEI_SOUND_START = 35,
        SPEI_SOUND_END = 36,
        SPEI_PHRASE_START = 37,
        SPEI_RECOGNITION = 38,
        SPEI_HYPOTHESIS = 39,
        SPEI_SR_BOOKMARK = 40,
        SPEI_PROPERTY_NUM_CHANGE = 41,  // LPARAM points to a string, WPARAM is the attrib value
        SPEI_PROPERTY_STRING_CHANGE = 42,  // LPARAM pointer to buffer.  Two concatenated null terminated strings.
        SPEI_FALSE_RECOGNITION = 43,  // apparent speech with no valid recognition
        SPEI_INTERFERENCE = 44,  // LPARAM is any combination of SPINTERFERENCE flags
        SPEI_REQUEST_UI = 45,  // LPARAM is string.
        SPEI_RECO_STATE_CHANGE = 46,  // wParam contains new reco state
        SPEI_ADAPTATION = 47,  // we are now ready to accept the adaptation buffer
        SPEI_START_SR_STREAM = 48,
        SPEI_RECO_OTHER_CONTEXT = 49,  // Phrase finished and recognized, but for other context
        SPEI_SR_AUDIO_LEVEL = 50,  // wParam contains current input audio level
        SPEI_SR_RETAINEDAUDIO = 51,
        SPEI_SR_PRIVATE = 52,
        SPEI_ACTIVE_CATEGORY_CHANGED = 53,  // LPARAM is a pointer to the new active category
        SPEI_TEXTFEEDBACK = 54,  // LPARAM is a pointer to FILETIME + FeedbackText
        SPEI_RECOGNITION_ALL = 55,
        SPEI_BARGE_IN = 56,

        // SPEI_MIN_SR = 34,
        // SPEI_MAX_SR = 56,
        SPEI_RESERVED1 = 30,  // do not use
        SPEI_RESERVED2 = 33,  // do not use
        SPEI_RESERVED3 = 63   // do not use
    }

    [ComImport, Guid("5EFF4AEF-8487-11D2-961C-00C04F8EE628"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpNotifySource
    {
        // ISpNotifySource Methods
        void SetNotifySink(ISpNotifySink pNotifySink);
        void SetNotifyWindowMessage(uint hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        void Slot3(); // void SetNotifyCallbackFunction(ref IntPtr pfnCallback, IntPtr wParam, IntPtr lParam);
        void Slot4(); // void SetNotifyCallbackInterface(ref IntPtr pSpCallback, IntPtr wParam, IntPtr lParam);
        void Slot5(); // void SetNotifyWin32Event();
        [PreserveSig]
        int WaitForNotifyEvent(uint dwMilliseconds);
        void Slot7(); // IntPtr GetNotifyEventHandle();
    }

    [ComImport, Guid("BE7A9CCE-5F9E-11D2-960F-00C04F8EE628"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpEventSource : ISpNotifySource
    {
        // ISpNotifySource Methods
        new void SetNotifySink(ISpNotifySink pNotifySink);
        new void SetNotifyWindowMessage(uint hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        new void Slot3(); // void SetNotifyCallbackFunction(ref IntPtr pfnCallback, IntPtr wParam, IntPtr lParam);
        new void Slot4(); // void SetNotifyCallbackInterface(ref IntPtr pSpCallback, IntPtr wParam, IntPtr lParam);
        new void Slot5(); // void SetNotifyWin32Event();
        [PreserveSig]
        new int WaitForNotifyEvent(uint dwMilliseconds);
        new void Slot7(); // IntPtr GetNotifyEventHandle();

        // ISpEventSource Methods
        void SetInterest(ulong ullEventInterest, ulong ullQueuedInterest);
        void GetEvents(uint ulCount, out SPEVENT pEventArray, out uint pulFetched);
        void Slot10(); // void GetInfo(out SPEVENTSOURCEINFO pInfo);
    }

    [ComImport, Guid("2373A435-6A4B-429e-A6AC-D4231A61975B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpEventSource2 : ISpEventSource
    {
        // ISpNotifySource Methods
        new void SetNotifySink(ISpNotifySink pNotifySink);
        new void SetNotifyWindowMessage(uint hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        new void Slot3(); // void SetNotifyCallbackFunction(ref IntPtr pfnCallback, IntPtr wParam, IntPtr lParam);
        new void Slot4(); // void SetNotifyCallbackInterface(ref IntPtr pSpCallback, IntPtr wParam, IntPtr lParam);
        new void Slot5(); // void SetNotifyWin32Event();
        [PreserveSig]
        new int WaitForNotifyEvent(uint dwMilliseconds);
        new void Slot7(); // IntPtr GetNotifyEventHandle();

        // ISpEventSource Methods
        new void SetInterest(ulong ullEventInterest, ulong ullQueuedInterest);
        new void GetEvents(uint ulCount, out SPEVENT pEventArray, out uint pulFetched);
        new void Slot10(); // void GetInfo(out SPEVENTSOURCEINFO pInfo);

        // ISpEventSource2 Methods
        void GetEventsEx(uint ulCount, out SPEVENTEX pEventArray, out uint pulFetched);
    }

    [ComImport, Guid("259684DC-37C3-11D2-9603-00C04F8EE628"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpNotifySink
    {
        // ISpNotifySink Methods
        void Notify();
    }
}
