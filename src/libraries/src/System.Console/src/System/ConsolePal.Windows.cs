// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace System
{
    // Provides Windows-based support for System.Console.
    internal static class ConsolePal
    {
        private static IntPtr InvalidHandleValue => new IntPtr(-1);

        private static bool IsWindows7()
        {
            // Version lies for all apps from the OS kick in starting with Windows 8 (6.2). They can
            // also be added via appcompat (by the OS or the users) so this can only be used as a hint.
            Version version = Environment.OSVersion.Version;
            return version.Major == 6 && version.Minor == 1;
        }

        public static Stream OpenStandardInput()
        {
            return GetStandardFile(Interop.Kernel32.HandleTypes.STD_INPUT_HANDLE, FileAccess.Read);
        }

        public static Stream OpenStandardOutput()
        {
            return GetStandardFile(Interop.Kernel32.HandleTypes.STD_OUTPUT_HANDLE, FileAccess.Write);
        }

        public static Stream OpenStandardError()
        {
            return GetStandardFile(Interop.Kernel32.HandleTypes.STD_ERROR_HANDLE, FileAccess.Write);
        }

        private static IntPtr InputHandle
        {
            get { return Interop.Kernel32.GetStdHandle(Interop.Kernel32.HandleTypes.STD_INPUT_HANDLE); }
        }

        private static IntPtr OutputHandle
        {
            get { return Interop.Kernel32.GetStdHandle(Interop.Kernel32.HandleTypes.STD_OUTPUT_HANDLE); }
        }

        private static IntPtr ErrorHandle
        {
            get { return Interop.Kernel32.GetStdHandle(Interop.Kernel32.HandleTypes.STD_ERROR_HANDLE); }
        }

        private static Stream GetStandardFile(int handleType, FileAccess access)
        {
            IntPtr handle = Interop.Kernel32.GetStdHandle(handleType);

            // If someone launches a managed process via CreateProcess, stdout,
            // stderr, & stdin could independently be set to INVALID_HANDLE_VALUE.
            // Additionally they might use 0 as an invalid handle.  We also need to
            // ensure that if the handle is meant to be writable it actually is.
            if (handle == IntPtr.Zero || handle == InvalidHandleValue ||
                (access != FileAccess.Read && !ConsoleHandleIsWritable(handle)))
            {
                return Stream.Null;
            }

            return new WindowsConsoleStream(handle, access, GetUseFileAPIs(handleType));
        }

        // Checks whether stdout or stderr are writable.  Do NOT pass
        // stdin here! The console handles are set to values like 3, 7, 
        // and 11 OR if you've been created via CreateProcess, possibly -1
        // or 0.  -1 is definitely invalid, while 0 is probably invalid.
        // Also note each handle can independently be invalid or good.
        // For Windows apps, the console handles are set to values like 3, 7, 
        // and 11 but are invalid handles - you may not write to them.  However,
        // you can still spawn a Windows app via CreateProcess and read stdout
        // and stderr. So, we always need to check each handle independently for validity
        // by trying to write or read to it, unless it is -1.
        private static unsafe bool ConsoleHandleIsWritable(IntPtr outErrHandle)
        {
            // Windows apps may have non-null valid looking handle values for 
            // stdin, stdout and stderr, but they may not be readable or 
            // writable.  Verify this by calling WriteFile in the 
            // appropriate modes. This must handle console-less Windows apps.
            int bytesWritten;
            byte junkByte = 0x41;
            int r = Interop.Kernel32.WriteFile(outErrHandle, &junkByte, 0, out bytesWritten, IntPtr.Zero);
            return r != 0; // In Win32 apps w/ no console, bResult should be 0 for failure.
        }

        public static Encoding InputEncoding
        {
            get { return EncodingHelper.GetSupportedConsoleEncoding((int)Interop.Kernel32.GetConsoleCP()); }
        }

        public static void SetConsoleInputEncoding(Encoding enc)
        {
            if (enc.CodePage != Encoding.Unicode.CodePage)
            {
                if (!Interop.Kernel32.SetConsoleCP(enc.CodePage))
                    throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
            }
        }

        public static Encoding OutputEncoding
        {
            get { return EncodingHelper.GetSupportedConsoleEncoding((int)Interop.Kernel32.GetConsoleOutputCP()); }
        }

        public static void SetConsoleOutputEncoding(Encoding enc)
        {
            if (enc.CodePage != Encoding.Unicode.CodePage)
            {
                if (!Interop.Kernel32.SetConsoleOutputCP(enc.CodePage))
                    throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
            }
        }

        private static bool GetUseFileAPIs(int handleType)
        {
            switch (handleType)
            {
                case Interop.Kernel32.HandleTypes.STD_INPUT_HANDLE:
                    return Console.InputEncoding.CodePage != Encoding.Unicode.CodePage || Console.IsInputRedirected;

                case Interop.Kernel32.HandleTypes.STD_OUTPUT_HANDLE:
                    return Console.OutputEncoding.CodePage != Encoding.Unicode.CodePage || Console.IsOutputRedirected;

                case Interop.Kernel32.HandleTypes.STD_ERROR_HANDLE:
                    return Console.OutputEncoding.CodePage != Encoding.Unicode.CodePage || Console.IsErrorRedirected;

                default:
                    // This can never happen.
                    Debug.Fail("Unexpected handleType value (" + handleType + ")");
                    return true;
            }
        }

        /// <summary>Gets whether Console.In is targeting a terminal display.</summary>
        public static bool IsInputRedirectedCore()
        {
            return IsHandleRedirected(InputHandle);
        }

        /// <summary>Gets whether Console.Out is targeting a terminal display.</summary>
        public static bool IsOutputRedirectedCore()
        {
            return IsHandleRedirected(OutputHandle);
        }

        /// <summary>Gets whether Console.In is targeting a terminal display.</summary>
        public static bool IsErrorRedirectedCore()
        {
            return IsHandleRedirected(ErrorHandle);
        }

        private static bool IsHandleRedirected(IntPtr handle)
        {
            // If handle is not to a character device, we must be redirected:
            uint fileType = Interop.Kernel32.GetFileType(handle);
            if ((fileType & Interop.Kernel32.FileTypes.FILE_TYPE_CHAR) != Interop.Kernel32.FileTypes.FILE_TYPE_CHAR)
                return true;

            // We are on a char device if GetConsoleMode succeeds and so we are not redirected.
            return (!Interop.Kernel32.IsGetConsoleModeCallSuccessful(handle));
        }

        internal static TextReader GetOrCreateReader()
        {
            Stream inputStream = OpenStandardInput();
            return SyncTextReader.GetSynchronizedTextReader(inputStream == Stream.Null ?
                StreamReader.Null :
                new StreamReader(
                    stream: inputStream,
                    encoding: new ConsoleEncoding(Console.InputEncoding),
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: Console.ReadBufferSize,
                    leaveOpen: true));
        }

        // Use this for blocking in Console.ReadKey, which needs to protect itself in case multiple threads call it simultaneously.
        // Use a ReadKey-specific lock though, to allow other fields to be initialized on this type.
        private static readonly object s_readKeySyncObject = new object();

        // ReadLine & Read can't use this because they need to use ReadFile
        // to be able to handle redirected input.  We have to accept that
        // we will lose repeated keystrokes when someone switches from
        // calling ReadKey to calling Read or ReadLine.  Those methods should 
        // ideally flush this cache as well.
        private static Interop.InputRecord _cachedInputRecord;

        // Skip non key events. Generally we want to surface only KeyDown event 
        // and suppress KeyUp event from the same Key press but there are cases
        // where the assumption of KeyDown-KeyUp pairing for a given key press 
        // is invalid. For example in IME Unicode keyboard input, we often see
        // only KeyUp until the key is released.  
        private static bool IsKeyDownEvent(Interop.InputRecord ir)
        {
            return (ir.eventType == Interop.KEY_EVENT && ir.keyEvent.keyDown != Interop.BOOL.FALSE);
        }

        private static bool IsModKey(Interop.InputRecord ir)
        {
            // We should also skip over Shift, Control, and Alt, as well as caps lock.
            // Apparently we don't need to check for 0xA0 through 0xA5, which are keys like 
            // Left Control & Right Control. See the ConsoleKey enum for these values.
            short keyCode = ir.keyEvent.virtualKeyCode;
            return ((keyCode >= 0x10 && keyCode <= 0x12)
                    || keyCode == 0x14 || keyCode == 0x90 || keyCode == 0x91);
        }

        [Flags]
        internal enum ControlKeyState
        {
            RightAltPressed = 0x0001,
            LeftAltPressed = 0x0002,
            RightCtrlPressed = 0x0004,
            LeftCtrlPressed = 0x0008,
            ShiftPressed = 0x0010,
            NumLockOn = 0x0020,
            ScrollLockOn = 0x0040,
            CapsLockOn = 0x0080,
            EnhancedKey = 0x0100
        }

        // For tracking Alt+NumPad unicode key sequence. When you press Alt key down 
        // and press a numpad unicode decimal sequence and then release Alt key, the
        // desired effect is to translate the sequence into one Unicode KeyPress. 
        // We need to keep track of the Alt+NumPad sequence and surface the final
        // unicode char alone when the Alt key is released. 
        private static bool IsAltKeyDown(Interop.InputRecord ir)
        {
            return (((ControlKeyState)ir.keyEvent.controlKeyState)
                              & (ControlKeyState.LeftAltPressed | ControlKeyState.RightAltPressed)) != 0;
        }

        private const int NumberLockVKCode = 0x90;
        private const int CapsLockVKCode = 0x14;

        public static bool NumberLock
        {
            get
            {
                try
                {
                    short s = Interop.User32.GetKeyState(NumberLockVKCode);
                    return (s & 1) == 1;
                }
                catch (Exception)
                {
                    // Since we depend on an extension api-set here
                    // it is not guaranteed to work across the board.
                    // In case of exception we simply throw PNSE
                    throw new PlatformNotSupportedException();
                }
            }
        }

        public static bool CapsLock
        {
            get
            {
                try
                {
                    short s = Interop.User32.GetKeyState(CapsLockVKCode);
                    return (s & 1) == 1;
                }
                catch (Exception)
                {
                    // Since we depend on an extension api-set here
                    // it is not guaranteed to work across the board.
                    // In case of exception we simply throw PNSE
                    throw new PlatformNotSupportedException();
                }
            }
        }

        public static bool KeyAvailable
        {
            get
            {
                if (_cachedInputRecord.eventType == Interop.KEY_EVENT)
                    return true;

                Interop.InputRecord ir = new Interop.InputRecord();
                int numEventsRead = 0;
                while (true)
                {
                    bool r = Interop.Kernel32.PeekConsoleInput(InputHandle, out ir, 1, out numEventsRead);
                    if (!r)
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        if (errorCode == Interop.Errors.ERROR_INVALID_HANDLE)
                            throw new InvalidOperationException(SR.InvalidOperation_ConsoleKeyAvailableOnFile);
                        throw Win32Marshal.GetExceptionForWin32Error(errorCode, "stdin");
                    }

                    if (numEventsRead == 0)
                        return false;

                    // Skip non key-down && mod key events.
                    if (!IsKeyDownEvent(ir) || IsModKey(ir))
                    {
                        r = Interop.Kernel32.ReadConsoleInput(InputHandle, out ir, 1, out numEventsRead);

                        if (!r)
                            throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
                    }
                    else
                    {
                        return true;
                    }
                }
            }  // get
        }

        private const short AltVKCode = 0x12;

        public static ConsoleKeyInfo ReadKey(bool intercept)
        {
            Interop.InputRecord ir;
            int numEventsRead = -1;
            bool r;

            lock (s_readKeySyncObject)
            {
                if (_cachedInputRecord.eventType == Interop.KEY_EVENT)
                {
                    // We had a previous keystroke with repeated characters.
                    ir = _cachedInputRecord;
                    if (_cachedInputRecord.keyEvent.repeatCount == 0)
                        _cachedInputRecord.eventType = -1;
                    else
                    {
                        _cachedInputRecord.keyEvent.repeatCount--;
                    }
                    // We will return one key from this method, so we decrement the
                    // repeatCount here, leaving the cachedInputRecord in the "queue".

                }
                else
                { // We did NOT have a previous keystroke with repeated characters:

                    while (true)
                    {
                        r = Interop.Kernel32.ReadConsoleInput(InputHandle, out ir, 1, out numEventsRead);
                        if (!r || numEventsRead == 0)
                        {
                            // This will fail when stdin is redirected from a file or pipe. 
                            // We could theoretically call Console.Read here, but I 
                            // think we might do some things incorrectly then.
                            throw new InvalidOperationException(SR.InvalidOperation_ConsoleReadKeyOnFile);
                        }

                        short keyCode = ir.keyEvent.virtualKeyCode;

                        // First check for non-keyboard events & discard them. Generally we tap into only KeyDown events and ignore the KeyUp events
                        // but it is possible that we are dealing with a Alt+NumPad unicode key sequence, the final unicode char is revealed only when 
                        // the Alt key is released (i.e when the sequence is complete). To avoid noise, when the Alt key is down, we should eat up 
                        // any intermediate key strokes (from NumPad) that collectively forms the Unicode character.  

                        if (!IsKeyDownEvent(ir))
                        {
                            // REVIEW: Unicode IME input comes through as KeyUp event with no accompanying KeyDown.
                            if (keyCode != AltVKCode)
                                continue;
                        }

                        char ch = (char)ir.keyEvent.uChar;

                        // In a Alt+NumPad unicode sequence, when the alt key is released uChar will represent the final unicode character, we need to 
                        // surface this. VirtualKeyCode for this event will be Alt from the Alt-Up key event. This is probably not the right code, 
                        // especially when we don't expose ConsoleKey.Alt, so this will end up being the hex value (0x12). VK_PACKET comes very 
                        // close to being useful and something that we could look into using for this purpose... 

                        if (ch == 0)
                        {
                            // Skip mod keys.
                            if (IsModKey(ir))
                                continue;
                        }

                        // When Alt is down, it is possible that we are in the middle of a Alt+NumPad unicode sequence.
                        // Escape any intermediate NumPad keys whether NumLock is on or not (notepad behavior)
                        ConsoleKey key = (ConsoleKey)keyCode;
                        if (IsAltKeyDown(ir) && ((key >= ConsoleKey.NumPad0 && key <= ConsoleKey.NumPad9)
                                             || (key == ConsoleKey.Clear) || (key == ConsoleKey.Insert)
                                             || (key >= ConsoleKey.PageUp && key <= ConsoleKey.DownArrow)))
                        {
                            continue;
                        }

                        if (ir.keyEvent.repeatCount > 1)
                        {
                            ir.keyEvent.repeatCount--;
                            _cachedInputRecord = ir;
                        }
                        break;
                    }
                }  // we did NOT have a previous keystroke with repeated characters.
            }

            ControlKeyState state = (ControlKeyState)ir.keyEvent.controlKeyState;
            bool shift = (state & ControlKeyState.ShiftPressed) != 0;
            bool alt = (state & (ControlKeyState.LeftAltPressed | ControlKeyState.RightAltPressed)) != 0;
            bool control = (state & (ControlKeyState.LeftCtrlPressed | ControlKeyState.RightCtrlPressed)) != 0;

            ConsoleKeyInfo info = new ConsoleKeyInfo((char)ir.keyEvent.uChar, (ConsoleKey)ir.keyEvent.virtualKeyCode, shift, alt, control);

            if (!intercept)
                Console.Write(ir.keyEvent.uChar);
            return info;
        }

        public static bool TreatControlCAsInput
        {
            get
            {
                IntPtr handle = InputHandle;
                if (handle == InvalidHandleValue)
                    throw new IOException(SR.IO_NoConsole);

                int mode = 0;
                if (!Interop.Kernel32.GetConsoleMode(handle, out mode))
                    throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());

                return (mode & Interop.Kernel32.ENABLE_PROCESSED_INPUT) == 0;
            }
            set
            {
                IntPtr handle = InputHandle;
                if (handle == InvalidHandleValue)
                    throw new IOException(SR.IO_NoConsole);

                int mode = 0;
                Interop.Kernel32.GetConsoleMode(handle, out mode); // failure ignored in full framework

                if (value)
                {
                    mode &= ~Interop.Kernel32.ENABLE_PROCESSED_INPUT;
                }
                else
                {
                    mode |= Interop.Kernel32.ENABLE_PROCESSED_INPUT;
                }

                if (!Interop.Kernel32.SetConsoleMode(handle, mode))
                    throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
            }
        }

        // For ResetColor
        private static volatile bool _haveReadDefaultColors;
        private static volatile byte _defaultColors;

        public static ConsoleColor BackgroundColor
        {
            get
            {
                bool succeeded;
                Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo(false, out succeeded);
                return succeeded ?
                    ColorAttributeToConsoleColor((Interop.Kernel32.Color)csbi.wAttributes & Interop.Kernel32.Color.BackgroundMask) :
                    ConsoleColor.Black; // for code that may be used from Windows app w/ no console
            }
            set
            {
                Interop.Kernel32.Color c = ConsoleColorToColorAttribute(value, true);

                bool succeeded;
                Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo(false, out succeeded);
                // For code that may be used from Windows app w/ no console
                if (!succeeded)
                    return;

                Debug.Assert(_haveReadDefaultColors, "Setting the background color before we've read the default foreground color!");

                short attrs = csbi.wAttributes;
                attrs &= ~((short)Interop.Kernel32.Color.BackgroundMask);
                // C#'s bitwise-or sign-extends to 32 bits.
                attrs = (short)(((uint)(ushort)attrs) | ((uint)(ushort)c));
                // Ignore errors here - there are some scenarios for running code that wants
                // to print in colors to the console in a Windows application.
                Interop.Kernel32.SetConsoleTextAttribute(OutputHandle, attrs);
            }
        }

        public static ConsoleColor ForegroundColor
        {
            get
            {
                bool succeeded;
                Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo(false, out succeeded);

                // For code that may be used from Windows app w/ no console
                return succeeded ?
                    ColorAttributeToConsoleColor((Interop.Kernel32.Color)csbi.wAttributes & Interop.Kernel32.Color.ForegroundMask) :
                    ConsoleColor.Gray;
            }
            set
            {
                Interop.Kernel32.Color c = ConsoleColorToColorAttribute(value, false);

                bool succeeded;
                Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo(false, out succeeded);
                // For code that may be used from Windows app w/ no console
                if (!succeeded)
                    return;

                Debug.Assert(_haveReadDefaultColors, "Setting the foreground color before we've read the default foreground color!");

                short attrs = csbi.wAttributes;
                attrs &= ~((short)Interop.Kernel32.Color.ForegroundMask);
                // C#'s bitwise-or sign-extends to 32 bits.
                attrs = (short)(((uint)(ushort)attrs) | ((uint)(ushort)c));
                // Ignore errors here - there are some scenarios for running code that wants
                // to print in colors to the console in a Windows application.
                Interop.Kernel32.SetConsoleTextAttribute(OutputHandle, attrs);
            }
        }

        public static void ResetColor()
        {
            if (!_haveReadDefaultColors) // avoid the costs of GetBufferInfo if we already know we checked it
            {
                bool succeeded;
                GetBufferInfo(false, out succeeded);
                if (!succeeded)
                    return; // For code that may be used from Windows app w/ no console

                Debug.Assert(_haveReadDefaultColors, "Resetting color before we've read the default foreground color!");
            }

            // Ignore errors here - there are some scenarios for running code that wants
            // to print in colors to the console in a Windows application.
            Interop.Kernel32.SetConsoleTextAttribute(OutputHandle, (short)(ushort)_defaultColors);
        }

        public static int CursorSize
        {
            get
            {
                Interop.Kernel32.CONSOLE_CURSOR_INFO cci;
                if (!Interop.Kernel32.GetConsoleCursorInfo(OutputHandle, out cci))
                    throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());

                return cci.dwSize;
            }
            set
            {
                // Value should be a percentage from [1, 100].
                if (value < 1 || value > 100)
                    throw new ArgumentOutOfRangeException(nameof(value), value, SR.ArgumentOutOfRange_CursorSize);

                Interop.Kernel32.CONSOLE_CURSOR_INFO cci;
                if (!Interop.Kernel32.GetConsoleCursorInfo(OutputHandle, out cci))
                    throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());

                cci.dwSize = value;
                if (!Interop.Kernel32.SetConsoleCursorInfo(OutputHandle, ref cci))
                    throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
            }
        }

        public static bool CursorVisible
        {
            get
            {
                Interop.Kernel32.CONSOLE_CURSOR_INFO cci;
                if (!Interop.Kernel32.GetConsoleCursorInfo(OutputHandle, out cci))
                    throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());

                return cci.bVisible;
            }
            set
            {
                Interop.Kernel32.CONSOLE_CURSOR_INFO cci;
                if (!Interop.Kernel32.GetConsoleCursorInfo(OutputHandle, out cci))
                    throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());

                cci.bVisible = value;
                if (!Interop.Kernel32.SetConsoleCursorInfo(OutputHandle, ref cci))
                    throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
            }
        }

        public static int CursorLeft
        {
            get
            {
                Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();
                return csbi.dwCursorPosition.X;
            }
        }

        public static int CursorTop
        {
            get
            {
                Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();
                return csbi.dwCursorPosition.Y;
            }
        }

        public unsafe static string Title
        {
            get
            {
                Span<char> initialBuffer = stackalloc char[256];
                ValueStringBuilder builder = new ValueStringBuilder(initialBuffer);

                while (true)
                {
                    uint result = Interop.Errors.ERROR_SUCCESS;

                    fixed (char* c = &builder.GetPinnableReference())
                    {
                        result = Interop.Kernel32.GetConsoleTitleW(c, (uint)builder.Capacity);
                    }

                    // The documentation asserts that the console's title is stored in a shared 64KB buffer.
                    // The magic number that used to exist here (24500) is likely related to that.
                    // A full UNICODE_STRING is 32K chars...
                    Debug.Assert(result <= short.MaxValue, "shouldn't be possible to grow beyond UNICODE_STRING size");

                    if (result == 0)
                    {
                        int error = Marshal.GetLastWin32Error();
                        switch (error)
                        {
                            case Interop.Errors.ERROR_INSUFFICIENT_BUFFER:
                                // Typically this API truncates but there was a bug in RS2 so we'll make an attempt to handle
                                builder.EnsureCapacity(builder.Capacity * 2);
                                continue;
                            case Interop.Errors.ERROR_SUCCESS:
                                // The title is empty.
                                break;
                            default:
                                throw Win32Marshal.GetExceptionForWin32Error(error, string.Empty);
                        }
                    }
                    else if (result >= builder.Capacity - 1 || (IsWindows7() && result >= builder.Capacity / sizeof(char) - 1))
                    {
                        // Our buffer was full. As this API truncates we need to increase our size and reattempt.
                        // Note that Windows 7 copies count of bytes into the output buffer but returns count of chars
                        // and as such our buffer is only "half" its actual size.
                        //
                        // (If we're Windows 10 with a version lie to 7 this will be inefficient so we'll want to remove
                        //  this workaround when we no longer support Windows 7)
                        builder.EnsureCapacity(builder.Capacity * 2);
                        continue;
                    }

                    builder.Length = (int)result;
                    break;
                }

                return builder.ToString();
            }
            set
            {
                if (!Interop.Kernel32.SetConsoleTitle(value))
                    throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
            }
        }

        public static void Beep()
        {
            const int BeepFrequencyInHz = 800;
            const int BeepDurationInMs = 200;
            Interop.Kernel32.Beep(BeepFrequencyInHz, BeepDurationInMs);
        }

        public static void Beep(int frequency, int duration)
        {
            const int MinBeepFrequency = 37;
            const int MaxBeepFrequency = 32767;

            if (frequency < MinBeepFrequency || frequency > MaxBeepFrequency)
                throw new ArgumentOutOfRangeException(nameof(frequency), frequency, SR.Format(SR.ArgumentOutOfRange_BeepFrequency, MinBeepFrequency, MaxBeepFrequency));
            if (duration <= 0)
                throw new ArgumentOutOfRangeException(nameof(duration), duration, SR.ArgumentOutOfRange_NeedPosNum);

            Interop.Kernel32.Beep(frequency, duration);
        }

        public static unsafe void MoveBufferArea(int sourceLeft, int sourceTop,
            int sourceWidth, int sourceHeight, int targetLeft, int targetTop,
            char sourceChar, ConsoleColor sourceForeColor,
            ConsoleColor sourceBackColor)
        {
            if (sourceForeColor < ConsoleColor.Black || sourceForeColor > ConsoleColor.White)
                throw new ArgumentException(SR.Arg_InvalidConsoleColor, nameof(sourceForeColor));
            if (sourceBackColor < ConsoleColor.Black || sourceBackColor > ConsoleColor.White)
                throw new ArgumentException(SR.Arg_InvalidConsoleColor, nameof(sourceBackColor));

            Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();
            Interop.Kernel32.COORD bufferSize = csbi.dwSize;
            if (sourceLeft < 0 || sourceLeft > bufferSize.X)
                throw new ArgumentOutOfRangeException(nameof(sourceLeft), sourceLeft, SR.ArgumentOutOfRange_ConsoleBufferBoundaries);
            if (sourceTop < 0 || sourceTop > bufferSize.Y)
                throw new ArgumentOutOfRangeException(nameof(sourceTop), sourceTop, SR.ArgumentOutOfRange_ConsoleBufferBoundaries);
            if (sourceWidth < 0 || sourceWidth > bufferSize.X - sourceLeft)
                throw new ArgumentOutOfRangeException(nameof(sourceWidth), sourceWidth, SR.ArgumentOutOfRange_ConsoleBufferBoundaries);
            if (sourceHeight < 0 || sourceTop > bufferSize.Y - sourceHeight)
                throw new ArgumentOutOfRangeException(nameof(sourceHeight), sourceHeight, SR.ArgumentOutOfRange_ConsoleBufferBoundaries);

            // Note: if the target range is partially in and partially out
            // of the buffer, then we let the OS clip it for us.
            if (targetLeft < 0 || targetLeft > bufferSize.X)
                throw new ArgumentOutOfRangeException(nameof(targetLeft), targetLeft, SR.ArgumentOutOfRange_ConsoleBufferBoundaries);
            if (targetTop < 0 || targetTop > bufferSize.Y)
                throw new ArgumentOutOfRangeException(nameof(targetTop), targetTop, SR.ArgumentOutOfRange_ConsoleBufferBoundaries);

            // If we're not doing any work, bail out now (Windows will return
            // an error otherwise)
            if (sourceWidth == 0 || sourceHeight == 0)
                return;

            // Read data from the original location, blank it out, then write
            // it to the new location.  This will handle overlapping source and
            // destination regions correctly.

            // Read the old data
            Interop.Kernel32.CHAR_INFO[] data = new Interop.Kernel32.CHAR_INFO[sourceWidth * sourceHeight];
            bufferSize.X = (short)sourceWidth;
            bufferSize.Y = (short)sourceHeight;
            Interop.Kernel32.COORD bufferCoord = new Interop.Kernel32.COORD();
            Interop.Kernel32.SMALL_RECT readRegion = new Interop.Kernel32.SMALL_RECT();
            readRegion.Left = (short)sourceLeft;
            readRegion.Right = (short)(sourceLeft + sourceWidth - 1);
            readRegion.Top = (short)sourceTop;
            readRegion.Bottom = (short)(sourceTop + sourceHeight - 1);

            bool r;
            fixed (Interop.Kernel32.CHAR_INFO* pCharInfo = data)
                r = Interop.Kernel32.ReadConsoleOutput(OutputHandle, pCharInfo, bufferSize, bufferCoord, ref readRegion);
            if (!r)
                throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());

            // Overwrite old section
            Interop.Kernel32.COORD writeCoord = new Interop.Kernel32.COORD();
            writeCoord.X = (short)sourceLeft;
            Interop.Kernel32.Color c = ConsoleColorToColorAttribute(sourceBackColor, true);
            c |= ConsoleColorToColorAttribute(sourceForeColor, false);
            short attr = (short)c;
            int numWritten;
            for (int i = sourceTop; i < sourceTop + sourceHeight; i++)
            {
                writeCoord.Y = (short)i;
                r = Interop.Kernel32.FillConsoleOutputCharacter(OutputHandle, sourceChar, sourceWidth, writeCoord, out numWritten);
                Debug.Assert(numWritten == sourceWidth, "FillConsoleOutputCharacter wrote the wrong number of chars!");
                if (!r)
                    throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());

                r = Interop.Kernel32.FillConsoleOutputAttribute(OutputHandle, attr, sourceWidth, writeCoord, out numWritten);
                if (!r)
                    throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
            }

            // Write text to new location
            Interop.Kernel32.SMALL_RECT writeRegion = new Interop.Kernel32.SMALL_RECT();
            writeRegion.Left = (short)targetLeft;
            writeRegion.Right = (short)(targetLeft + sourceWidth);
            writeRegion.Top = (short)targetTop;
            writeRegion.Bottom = (short)(targetTop + sourceHeight);

            fixed (Interop.Kernel32.CHAR_INFO* pCharInfo = data)
                Interop.Kernel32.WriteConsoleOutput(OutputHandle, pCharInfo, bufferSize, bufferCoord, ref writeRegion);
        }

        public static void Clear()
        {
            Interop.Kernel32.COORD coordScreen = new Interop.Kernel32.COORD();
            Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO csbi;
            bool success;
            int conSize;

            IntPtr hConsole = OutputHandle;
            if (hConsole == InvalidHandleValue)
                throw new IOException(SR.IO_NoConsole);

            // get the number of character cells in the current buffer
            // Go through my helper method for fetching a screen buffer info
            // to correctly handle default console colors.
            csbi = GetBufferInfo();
            conSize = csbi.dwSize.X * csbi.dwSize.Y;

            // fill the entire screen with blanks

            int numCellsWritten = 0;
            success = Interop.Kernel32.FillConsoleOutputCharacter(hConsole, ' ',
                conSize, coordScreen, out numCellsWritten);
            if (!success)
                throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());

            // now set the buffer's attributes accordingly

            numCellsWritten = 0;
            success = Interop.Kernel32.FillConsoleOutputAttribute(hConsole, csbi.wAttributes,
                conSize, coordScreen, out numCellsWritten);
            if (!success)
                throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());

            // put the cursor at (0, 0)

            success = Interop.Kernel32.SetConsoleCursorPosition(hConsole, coordScreen);
            if (!success)
                throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
        }

        public static void SetCursorPosition(int left, int top)
        {
            IntPtr hConsole = OutputHandle;
            Interop.Kernel32.COORD coords = new Interop.Kernel32.COORD();
            coords.X = (short)left;
            coords.Y = (short)top;
            if (!Interop.Kernel32.SetConsoleCursorPosition(hConsole, coords))
            {
                // Give a nice error message for out of range sizes
                int errorCode = Marshal.GetLastWin32Error();
                Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();
                if (left >= csbi.dwSize.X)
                    throw new ArgumentOutOfRangeException(nameof(left), left, SR.ArgumentOutOfRange_ConsoleBufferBoundaries);
                if (top >= csbi.dwSize.Y)
                    throw new ArgumentOutOfRangeException(nameof(top), top, SR.ArgumentOutOfRange_ConsoleBufferBoundaries);

                throw Win32Marshal.GetExceptionForWin32Error(errorCode);
            }
        }

        public static int BufferWidth
        {
            get
            {
                Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();
                return csbi.dwSize.X;
            }
            set
            {
                SetBufferSize(value, BufferHeight);
            }
        }

        public static int BufferHeight
        {
            get
            {
                Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();
                return csbi.dwSize.Y;
            }
            set
            {
                SetBufferSize(BufferWidth, value);
            }
        }

        public static void SetBufferSize(int width, int height)
        {
            // Ensure the new size is not smaller than the console window
            Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();
            Interop.Kernel32.SMALL_RECT srWindow = csbi.srWindow;
            if (width < srWindow.Right + 1 || width >= short.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(width), width, SR.ArgumentOutOfRange_ConsoleBufferLessThanWindowSize);
            if (height < srWindow.Bottom + 1 || height >= short.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(height), height, SR.ArgumentOutOfRange_ConsoleBufferLessThanWindowSize);

            Interop.Kernel32.COORD size = new Interop.Kernel32.COORD();
            size.X = (short)width;
            size.Y = (short)height;
            if (!Interop.Kernel32.SetConsoleScreenBufferSize(OutputHandle, size))
            {
                throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
            }
        }

        public static int LargestWindowWidth
        {
            get
            {
                // Note this varies based on current screen resolution and 
                // current console font.  Do not cache this value.
                Interop.Kernel32.COORD bounds = Interop.Kernel32.GetLargestConsoleWindowSize(OutputHandle);
                return bounds.X;
            }
        }

        public static int LargestWindowHeight
        {
            get
            {
                // Note this varies based on current screen resolution and 
                // current console font.  Do not cache this value.
                Interop.Kernel32.COORD bounds = Interop.Kernel32.GetLargestConsoleWindowSize(OutputHandle);
                return bounds.Y;
            }
        }


        public static int WindowLeft
        {
            get
            {
                Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();
                return csbi.srWindow.Left;
            }
            set
            {
                SetWindowPosition(value, WindowTop);
            }
        }

        public static int WindowTop
        {
            get
            {
                Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();
                return csbi.srWindow.Top;
            }
            set
            {
                SetWindowPosition(WindowLeft, value);
            }
        }

        public static int WindowWidth
        {
            get
            {
                Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();
                return csbi.srWindow.Right - csbi.srWindow.Left + 1;
            }
            set
            {
                SetWindowSize(value, WindowHeight);
            }
        }

        public static int WindowHeight
        {
            get
            {
                Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();
                return csbi.srWindow.Bottom - csbi.srWindow.Top + 1;
            }
            set
            {
                SetWindowSize(WindowWidth, value);
            }
        }

        public static unsafe void SetWindowPosition(int left, int top)
        {
            // Get the size of the current console window
            Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();

            Interop.Kernel32.SMALL_RECT srWindow = csbi.srWindow;

            // Check for arithmetic underflows & overflows.
            int newRight = left + srWindow.Right - srWindow.Left + 1;
            if (left < 0 || newRight > csbi.dwSize.X || newRight < 0)
                throw new ArgumentOutOfRangeException(nameof(left), left, SR.ArgumentOutOfRange_ConsoleWindowPos);
            int newBottom = top + srWindow.Bottom - srWindow.Top + 1;
            if (top < 0 || newBottom > csbi.dwSize.Y || newBottom < 0)
                throw new ArgumentOutOfRangeException(nameof(top), top, SR.ArgumentOutOfRange_ConsoleWindowPos);

            // Preserve the size, but move the position.
            srWindow.Bottom -= (short)(srWindow.Top - top);
            srWindow.Right -= (short)(srWindow.Left - left);
            srWindow.Left = (short)left;
            srWindow.Top = (short)top;

            bool r = Interop.Kernel32.SetConsoleWindowInfo(OutputHandle, true, &srWindow);
            if (!r)
                throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
        }

        public static unsafe void SetWindowSize(int width, int height)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), width, SR.ArgumentOutOfRange_NeedPosNum);
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), height, SR.ArgumentOutOfRange_NeedPosNum);

            // Get the position of the current console window
            Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();

            // If the buffer is smaller than this new window size, resize the
            // buffer to be large enough.  Include window position.
            bool resizeBuffer = false;
            Interop.Kernel32.COORD size = new Interop.Kernel32.COORD();
            size.X = csbi.dwSize.X;
            size.Y = csbi.dwSize.Y;
            if (csbi.dwSize.X < csbi.srWindow.Left + width)
            {
                if (csbi.srWindow.Left >= short.MaxValue - width)
                    throw new ArgumentOutOfRangeException(nameof(width), SR.Format(SR.ArgumentOutOfRange_ConsoleWindowBufferSize, short.MaxValue - width));
                size.X = (short)(csbi.srWindow.Left + width);
                resizeBuffer = true;
            }
            if (csbi.dwSize.Y < csbi.srWindow.Top + height)
            {
                if (csbi.srWindow.Top >= short.MaxValue - height)
                    throw new ArgumentOutOfRangeException(nameof(height), SR.Format(SR.ArgumentOutOfRange_ConsoleWindowBufferSize, short.MaxValue - height));
                size.Y = (short)(csbi.srWindow.Top + height);
                resizeBuffer = true;
            }
            if (resizeBuffer)
            {
                if (!Interop.Kernel32.SetConsoleScreenBufferSize(OutputHandle, size))
                    throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
            }

            Interop.Kernel32.SMALL_RECT srWindow = csbi.srWindow;
            // Preserve the position, but change the size.
            srWindow.Bottom = (short)(srWindow.Top + height - 1);
            srWindow.Right = (short)(srWindow.Left + width - 1);

            if (!Interop.Kernel32.SetConsoleWindowInfo(OutputHandle, true, &srWindow))
            {
                int errorCode = Marshal.GetLastWin32Error();

                // If we resized the buffer, un-resize it.
                if (resizeBuffer)
                {
                    Interop.Kernel32.SetConsoleScreenBufferSize(OutputHandle, csbi.dwSize);
                }

                // Try to give a better error message here
               Interop.Kernel32.COORD bounds = Interop.Kernel32.GetLargestConsoleWindowSize(OutputHandle);
                if (width > bounds.X)
                    throw new ArgumentOutOfRangeException(nameof(width), width, SR.Format(SR.ArgumentOutOfRange_ConsoleWindowSize_Size, bounds.X));
                if (height > bounds.Y)
                    throw new ArgumentOutOfRangeException(nameof(height), height, SR.Format(SR.ArgumentOutOfRange_ConsoleWindowSize_Size, bounds.Y));

                throw Win32Marshal.GetExceptionForWin32Error(errorCode);
            }
        }


        private static Interop.Kernel32.Color ConsoleColorToColorAttribute(ConsoleColor color, bool isBackground)
        {
            if ((((int)color) & ~0xf) != 0)
                throw new ArgumentException(SR.Arg_InvalidConsoleColor);

            Interop.Kernel32.Color c = (Interop.Kernel32.Color)color;

            // Make these background colors instead of foreground
            if (isBackground)
                c = (Interop.Kernel32.Color)((int)c << 4);
            return c;
        }

        private static ConsoleColor ColorAttributeToConsoleColor(Interop.Kernel32.Color c)
        {
            // Turn background colors into foreground colors.
            if ((c & Interop.Kernel32.Color.BackgroundMask) != 0)
            {
                c = (Interop.Kernel32.Color)(((int)c) >> 4);
            }
            return (ConsoleColor)c;
        }

        private static Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO GetBufferInfo()
        {
            bool unused;
            return GetBufferInfo(true, out unused);
        }

        // For apps that don't have a console (like Windows apps), they might
        // run other code that includes color console output.  Allow a mechanism
        // where that code won't throw an exception for simple errors.
        private static Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO GetBufferInfo(bool throwOnNoConsole, out bool succeeded)
        {
            succeeded = false;

            IntPtr outputHandle = OutputHandle;
            if (outputHandle == InvalidHandleValue)
            {
                if (throwOnNoConsole)
                {
                    throw new IOException(SR.IO_NoConsole);
                }
                return new Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO();
            }

            // Note that if stdout is redirected to a file, the console handle may be a file.  
            // First try stdout; if this fails, try stderr and then stdin.
            Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO csbi;
            if (!Interop.Kernel32.GetConsoleScreenBufferInfo(outputHandle, out csbi) &&
                !Interop.Kernel32.GetConsoleScreenBufferInfo(ErrorHandle, out csbi) &&
                !Interop.Kernel32.GetConsoleScreenBufferInfo(InputHandle, out csbi))
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode == Interop.Errors.ERROR_INVALID_HANDLE && !throwOnNoConsole)
                    return new Interop.Kernel32.CONSOLE_SCREEN_BUFFER_INFO();
                throw Win32Marshal.GetExceptionForWin32Error(errorCode);
            }

            if (!_haveReadDefaultColors)
            {
                // Fetch the default foreground and background color for the ResetColor method.
                Debug.Assert((int)Interop.Kernel32.Color.ColorMask == 0xff, "Make sure one byte is large enough to store a Console color value!");
                _defaultColors = (byte)(csbi.wAttributes & (short)Interop.Kernel32.Color.ColorMask);
                _haveReadDefaultColors = true; // also used by ResetColor to know when GetBufferInfo has been called successfully
            }

            succeeded = true;
            return csbi;
        }

        private sealed class WindowsConsoleStream : ConsoleStream
        {
            // We know that if we are using console APIs rather than file APIs, then the encoding
            // is Encoding.Unicode implying 2 bytes per character:                
            const int BytesPerWChar = 2;

            private readonly bool _isPipe; // When reading from pipes, we need to properly handle EOF cases.
            private IntPtr _handle;
            private readonly bool _useFileAPIs;

            internal WindowsConsoleStream(IntPtr handle, FileAccess access, bool useFileAPIs)
                : base(access)
            {
                Debug.Assert(handle != IntPtr.Zero && handle != InvalidHandleValue, "ConsoleStream expects a valid handle!");
                _handle = handle;
                _isPipe = Interop.Kernel32.GetFileType(handle) == Interop.Kernel32.FileTypes.FILE_TYPE_PIPE;
                _useFileAPIs = useFileAPIs;
            }

            protected override void Dispose(bool disposing)
            {
                // We're probably better off not closing the OS handle here.  First,
                // we allow a program to get multiple instances of ConsoleStreams
                // around the same OS handle, so closing one handle would invalidate
                // them all.  Additionally, we want a second AppDomain to be able to 
                // write to stdout if a second AppDomain quits.
                _handle = IntPtr.Zero;
                base.Dispose(disposing);
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                ValidateRead(buffer, offset, count);

                int bytesRead;
                int errCode = ReadFileNative(_handle, buffer, offset, count, _isPipe, out bytesRead, _useFileAPIs);
                if (Interop.Errors.ERROR_SUCCESS != errCode)
                    throw Win32Marshal.GetExceptionForWin32Error(errCode);
                return bytesRead;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                ValidateWrite(buffer, offset, count);

                int errCode = WriteFileNative(_handle, buffer, offset, count, _useFileAPIs);
                if (Interop.Errors.ERROR_SUCCESS != errCode)
                    throw Win32Marshal.GetExceptionForWin32Error(errCode);
            }

            public override void Flush()
            {
                if (_handle == IntPtr.Zero) throw Error.GetFileNotOpen();
                base.Flush();
            }

            // P/Invoke wrappers for writing to and from a file, nearly identical
            // to the ones on FileStream.  These are duplicated to save startup/hello
            // world working set and to avoid requiring a reference to the
            // System.IO.FileSystem contract.

            private static unsafe int ReadFileNative(IntPtr hFile, byte[] bytes, int offset, int count, bool isPipe, out int bytesRead, bool useFileAPIs)
            {
                Debug.Assert(offset >= 0, "offset >= 0");
                Debug.Assert(count >= 0, "count >= 0");
                Debug.Assert(bytes != null, "bytes != null");
                // Don't corrupt memory when multiple threads are erroneously writing
                // to this stream simultaneously.
                if (bytes.Length - offset < count)
                    throw new IndexOutOfRangeException(SR.IndexOutOfRange_IORaceCondition);

                // You can't use the fixed statement on an array of length 0.
                if (bytes.Length == 0)
                {
                    bytesRead = 0;
                    return Interop.Errors.ERROR_SUCCESS;
                }

                bool readSuccess;
                fixed (byte* p = &bytes[0])
                {
                    if (useFileAPIs)
                    {
                        readSuccess = (0 != Interop.Kernel32.ReadFile(hFile, p + offset, count, out bytesRead, IntPtr.Zero));
                    }
                    else
                    {
                        // If the code page could be Unicode, we should use ReadConsole instead, e.g.
                        int charsRead;
                        readSuccess = Interop.Kernel32.ReadConsole(hFile, p + offset, count / BytesPerWChar, out charsRead, IntPtr.Zero);
                        bytesRead = charsRead * BytesPerWChar;
                    }
                }
                if (readSuccess)
                    return Interop.Errors.ERROR_SUCCESS;

                // For pipes that are closing or broken, just stop.
                // (E.g. ERROR_NO_DATA ("pipe is being closed") is returned when we write to a console that is closing;
                // ERROR_BROKEN_PIPE ("pipe was closed") is returned when stdin was closed, which is mot an error, but EOF.)
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode == Interop.Errors.ERROR_NO_DATA || errorCode == Interop.Errors.ERROR_BROKEN_PIPE)
                    return Interop.Errors.ERROR_SUCCESS;
                return errorCode;
            }

            private static unsafe int WriteFileNative(IntPtr hFile, byte[] bytes, int offset, int count, bool useFileAPIs)
            {
                Debug.Assert(offset >= 0, "offset >= 0");
                Debug.Assert(count >= 0, "count >= 0");
                Debug.Assert(bytes != null, "bytes != null");
                Debug.Assert(bytes.Length >= offset + count, "bytes.Length >= offset + count");

                // You can't use the fixed statement on an array of length 0.
                if (bytes.Length == 0)
                    return Interop.Errors.ERROR_SUCCESS;

                bool writeSuccess;
                fixed (byte* p = &bytes[0])
                {
                    if (useFileAPIs)
                    {
                        int numBytesWritten;
                        writeSuccess = (0 != Interop.Kernel32.WriteFile(hFile, p + offset, count, out numBytesWritten, IntPtr.Zero));
                        // In some cases we have seen numBytesWritten returned that is twice count;
                        // so we aren't asserting the value of it. See corefx #24508
                    }
                    else
                    {

                        // If the code page could be Unicode, we should use ReadConsole instead, e.g.
                        // Note that WriteConsoleW has a max limit on num of chars to write (64K)
                        // [https://docs.microsoft.com/en-us/windows/console/writeconsole]
                        // However, we do not need to worry about that because the StreamWriter in Console has
                        // a much shorter buffer size anyway.
                        int charsWritten;
                        writeSuccess = Interop.Kernel32.WriteConsole(hFile, p + offset, count / BytesPerWChar, out charsWritten, IntPtr.Zero);
                        Debug.Assert(!writeSuccess || count / BytesPerWChar == charsWritten);
                    }
                }
                if (writeSuccess)
                    return Interop.Errors.ERROR_SUCCESS;

                // For pipes that are closing or broken, just stop.
                // (E.g. ERROR_NO_DATA ("pipe is being closed") is returned when we write to a console that is closing;
                // ERROR_BROKEN_PIPE ("pipe was closed") is returned when stdin was closed, which is not an error, but EOF.)
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode == Interop.Errors.ERROR_NO_DATA || errorCode == Interop.Errors.ERROR_BROKEN_PIPE)
                    return Interop.Errors.ERROR_SUCCESS;
                return errorCode;
            }
        }

        internal sealed class ControlCHandlerRegistrar
        {
            private bool _handlerRegistered;
            private Interop.Kernel32.ConsoleCtrlHandlerRoutine _handler;

            internal ControlCHandlerRegistrar()
            {
                _handler = new Interop.Kernel32.ConsoleCtrlHandlerRoutine(BreakEvent);
            }

            internal void Register()
            {
                Debug.Assert(!_handlerRegistered);

                bool r = Interop.Kernel32.SetConsoleCtrlHandler(_handler, true);
                if (!r)
                {
                    throw Win32Marshal.GetExceptionForLastWin32Error();
                }

                _handlerRegistered = true;
            }

            internal void Unregister()
            {
                Debug.Assert(_handlerRegistered);

                bool r = Interop.Kernel32.SetConsoleCtrlHandler(_handler, false);
                if (!r)
                {
                    throw Win32Marshal.GetExceptionForLastWin32Error();
                }
                _handlerRegistered = false;
            }

            private static bool BreakEvent(int controlType)
            {
                if (controlType != Interop.Kernel32.CTRL_C_EVENT &&
                    controlType != Interop.Kernel32.CTRL_BREAK_EVENT)
                {
                    return false;
                }

                return Console.HandleBreakEvent(controlType == Interop.Kernel32.CTRL_C_EVENT ? ConsoleSpecialKey.ControlC : ConsoleSpecialKey.ControlBreak);
            }
        }
    }
}
