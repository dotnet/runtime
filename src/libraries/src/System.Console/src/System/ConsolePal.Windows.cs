// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace System
{
    // Provides Windows-based support for System.Console.
    internal static class ConsolePal
    {
        private const int DefaultConsoleBufferSize = 256; // default size of buffer used in stream readers/writers

        private static IntPtr s_InvalidHandleValue = new IntPtr(-1);

        public static Stream OpenStandardInput()
        {
            return GetStandardFile(InputHandle, FileAccess.Read);
        }

        public static Stream OpenStandardOutput()
        {
            return GetStandardFile(OutputHandle, FileAccess.Write);
        }

        public static Stream OpenStandardError()
        {
            return GetStandardFile(ErrorHandle, FileAccess.Write);
        }

        private static IntPtr InputHandle
        {
            get { return Interop.mincore.GetStdHandle(Interop.mincore.HandleTypes.STD_INPUT_HANDLE); }
        }

        private static IntPtr OutputHandle
        {
            get { return Interop.mincore.GetStdHandle(Interop.mincore.HandleTypes.STD_OUTPUT_HANDLE); }
        }

        private static IntPtr ErrorHandle
        {
            get { return Interop.mincore.GetStdHandle(Interop.mincore.HandleTypes.STD_ERROR_HANDLE); }
        }

        private static Stream GetStandardFile(IntPtr handle, FileAccess access)
        {
            // If someone launches a managed process via CreateProcess, stdout,
            // stderr, & stdin could independently be set to INVALID_HANDLE_VALUE.
            // Additionally they might use 0 as an invalid handle.  We also need to
            // ensure that if the handle is meant to be writable it actually is.
            if (handle == IntPtr.Zero || handle == s_InvalidHandleValue ||
                (access != FileAccess.Read && !ConsoleHandleIsWritable(handle)))
            {
                return Stream.Null;
            }

            return new WindowsConsoleStream(handle, access);
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
            int r = Interop.mincore.WriteFile(outErrHandle, &junkByte, 0, out bytesWritten, IntPtr.Zero);
            return r != 0; // In Win32 apps w/ no console, bResult should be 0 for failure.
        }

        // Note if we ever support different encodings:
        // We always use file APIs in WindowsConsoleStream since WriteConsole is called only when the Encoding is Unicode and 
        // the handle is not redirected. Since changing the Input/OutputEncoding is not currently supported, we will always have 
        // the either the GetConsoleCP encoding or the UTF8Encoding fallback, in which case we always use the Read/WriteFile native
        // API.  If that ever changes, WindowsConsoleStream will need to be changed, too.

        public static Encoding InputEncoding
        {
            get { return GetEncoding((int)Interop.mincore.GetConsoleCP()); }
        }

        public static Encoding OutputEncoding
        {
            get { return GetEncoding((int)Interop.mincore.GetConsoleOutputCP()); }
        }

        private static Encoding GetEncoding(int codePage)
        {
            Encoding enc = EncodingHelper.GetSupportedConsoleEncoding(codePage);
            Debug.Assert(!(enc is UnicodeEncoding)); // if this ever changes, will need to update how we read/write Windows console stream

            return new ConsoleEncoding(enc); // ensure encoding doesn't output a preamble
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
            uint fileType = Interop.mincore.GetFileType(handle);
            if ((fileType & Interop.mincore.FileTypes.FILE_TYPE_CHAR) != Interop.mincore.FileTypes.FILE_TYPE_CHAR)
                return true;

            // We are on a char device if GetConsoleMode succeeds and so we are not redirected.
            return (!Interop.mincore.IsGetConsoleModeCallSuccessful(handle));
        }

        internal static TextReader GetOrCreateReader()
        {
            Stream inputStream = OpenStandardInput();
            return SyncTextReader.GetSynchronizedTextReader(inputStream == Stream.Null ?
                StreamReader.Null :
                new StreamReader(
                    stream: inputStream,
                    encoding: InputEncoding,
                    detectEncodingFromByteOrderMarks: false,
                    bufferSize: DefaultConsoleBufferSize,
                    leaveOpen: true));
        }

        // Use this for blocking in Console.ReadKey, which needs to protect itself in case multiple threads call it simultaneously.
        // Use a ReadKey-specific lock though, to allow other fields to be initialized on this type.
        private static readonly Object s_readKeySyncObject = new object();

        // ReadLine & Read can't use this because they need to use ReadFile
        // to be able to handle redirected input.  We have to accept that
        // we will lose repeated keystrokes when someone switches from
        // calling ReadKey to calling Read or ReadLine.  Those methods should 
        // ideally flush this cache as well.
        [System.Security.SecurityCritical] // auto-generated
        private static Interop.InputRecord _cachedInputRecord;

        // Skip non key events. Generally we want to surface only KeyDown event 
        // and suppress KeyUp event from the same Key press but there are cases
        // where the assumption of KeyDown-KeyUp pairing for a given key press 
        // is invalid. For example in IME Unicode keyboard input, we often see
        // only KeyUp until the key is released.  
        [System.Security.SecurityCritical]  // auto-generated
        private static bool IsKeyDownEvent(Interop.InputRecord ir)
        {
            return (ir.eventType == Interop.KEY_EVENT && ir.keyEvent.keyDown);
        }

        [System.Security.SecurityCritical]  // auto-generated
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
        [System.Security.SecurityCritical]  // auto-generated
        private static bool IsAltKeyDown(Interop.InputRecord ir)
        {
            return (((ControlKeyState)ir.keyEvent.controlKeyState)
                              & (ControlKeyState.LeftAltPressed | ControlKeyState.RightAltPressed)) != 0;
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
                    bool r = Interop.mincore.PeekConsoleInput(InputHandle, out ir, 1, out numEventsRead);
                    if (!r)
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        if (errorCode == Interop.mincore.Errors.ERROR_INVALID_HANDLE)
                            throw new InvalidOperationException(SR.InvalidOperation_ConsoleKeyAvailableOnFile);
                        throw Win32Marshal.GetExceptionForWin32Error(errorCode, "stdin");
                    }

                    if (numEventsRead == 0)
                        return false;

                    // Skip non key-down && mod key events.
                    if (!IsKeyDownEvent(ir) || IsModKey(ir))
                    {
                        r = Interop.mincore.ReadConsoleInput(InputHandle, out ir, 1, out numEventsRead);

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
                        r = Interop.mincore.ReadConsoleInput(InputHandle, out ir, 1, out numEventsRead);
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

        // For ResetColor
        private static volatile bool _haveReadDefaultColors;
        private static volatile byte _defaultColors;

        public static ConsoleColor BackgroundColor
        {
            get
            {
                bool succeeded;
                Interop.mincore.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo(false, out succeeded);
                return succeeded ?
                    ColorAttributeToConsoleColor((Interop.mincore.Color)csbi.wAttributes & Interop.mincore.Color.BackgroundMask) :
                    ConsoleColor.Black; // for code that may be used from Windows app w/ no console
            }
            set
            {
                Interop.mincore.Color c = ConsoleColorToColorAttribute(value, true);

                bool succeeded;
                Interop.mincore.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo(false, out succeeded);
                // For code that may be used from Windows app w/ no console
                if (!succeeded)
                    return;

                Debug.Assert(_haveReadDefaultColors, "Setting the background color before we've read the default foreground color!");

                short attrs = csbi.wAttributes;
                attrs &= ~((short)Interop.mincore.Color.BackgroundMask);
                // C#'s bitwise-or sign-extends to 32 bits.
                attrs = (short)(((uint)(ushort)attrs) | ((uint)(ushort)c));
                // Ignore errors here - there are some scenarios for running code that wants
                // to print in colors to the console in a Windows application.
                Interop.mincore.SetConsoleTextAttribute(OutputHandle, attrs);
            }
        }

        public static ConsoleColor ForegroundColor
        {
            get
            {
                bool succeeded;
                Interop.mincore.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo(false, out succeeded);

                // For code that may be used from Windows app w/ no console
                return succeeded ?
                    ColorAttributeToConsoleColor((Interop.mincore.Color)csbi.wAttributes & Interop.mincore.Color.ForegroundMask) :
                    ConsoleColor.Gray;
            }
            set
            {
                Interop.mincore.Color c = ConsoleColorToColorAttribute(value, false);

                bool succeeded;
                Interop.mincore.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo(false, out succeeded);
                // For code that may be used from Windows app w/ no console
                if (!succeeded)
                    return;

                Debug.Assert(_haveReadDefaultColors, "Setting the foreground color before we've read the default foreground color!");

                short attrs = csbi.wAttributes;
                attrs &= ~((short)Interop.mincore.Color.ForegroundMask);
                // C#'s bitwise-or sign-extends to 32 bits.
                attrs = (short)(((uint)(ushort)attrs) | ((uint)(ushort)c));
                // Ignore errors here - there are some scenarios for running code that wants
                // to print in colors to the console in a Windows application.
                Interop.mincore.SetConsoleTextAttribute(OutputHandle, attrs);
            }
        }

        public static void ResetColor()
        {
            bool succeeded;
            Interop.mincore.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo(false, out succeeded);
            if (!succeeded)
                return; // For code that may be used from Windows app w/ no console

            Debug.Assert(_haveReadDefaultColors, "Resetting color before we've read the default foreground color!");

            // Ignore errors here - there are some scenarios for running code that wants
            // to print in colors to the console in a Windows application.
            Interop.mincore.SetConsoleTextAttribute(OutputHandle, (short)(ushort)_defaultColors);
        }

        public static bool CursorVisible
        {
            get
            {
                Interop.mincore.CONSOLE_CURSOR_INFO cci;
                if (!Interop.mincore.GetConsoleCursorInfo(OutputHandle, out cci))
                    throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());

                return cci.bVisible;
            }
            set
            {
                Interop.mincore.CONSOLE_CURSOR_INFO cci;
                if (!Interop.mincore.GetConsoleCursorInfo(OutputHandle, out cci))
                    throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());

                cci.bVisible = value;
                if (!Interop.mincore.SetConsoleCursorInfo(OutputHandle, ref cci))
                    throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
            }
        }

        public static int CursorLeft
        {
            get
            {
                Interop.mincore.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();
                return csbi.dwCursorPosition.X;
            }
        }

        public static int CursorTop
        {
            get
            {
                Interop.mincore.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();
                return csbi.dwCursorPosition.Y;
            }
        }

        // Although msdn states that the max allowed limit is 65K,
        // desktop limits this to 24500 as buffer sizes greater than it
        // throw.
        private const int MaxConsoleTitleLength = 24500;

        public static string Title
        {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                string title = null;
                int titleLength = -1;
                Int32 r = Interop.mincore.GetConsoleTitle(out title, out titleLength);

                if (0 != r)
                {
                    throw Win32Marshal.GetExceptionForWin32Error(r, string.Empty);
                }

                if (titleLength > MaxConsoleTitleLength)
                    throw new InvalidOperationException(SR.ArgumentOutOfRange_ConsoleTitleTooLong);

                Debug.Assert(title.Length == titleLength);
                return title;
            }

            [System.Security.SecuritySafeCritical]  // auto-generated
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");
                if (value.Length > MaxConsoleTitleLength)
                    throw new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_ConsoleTitleTooLong);
                Contract.EndContractBlock();

                if (!Interop.mincore.SetConsoleTitle(value))
                    throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
            }
        }

        const int beepFrequencyInHz = 800;
        const int beepDurationInMs = 200;

        public static void Beep()
        {
            Interop.mincore.Beep(beepFrequencyInHz, beepDurationInMs);
        }

        public static void Clear()
        {
            Interop.mincore.COORD coordScreen = new Interop.mincore.COORD();
            Interop.mincore.CONSOLE_SCREEN_BUFFER_INFO csbi;
            bool success;
            int conSize;

            IntPtr hConsole = OutputHandle;
            if (hConsole == s_InvalidHandleValue)
                throw new IOException(SR.IO_NoConsole);

            // get the number of character cells in the current buffer
            // Go through my helper method for fetching a screen buffer info
            // to correctly handle default console colors.
            csbi = GetBufferInfo();
            conSize = csbi.dwSize.X * csbi.dwSize.Y;

            // fill the entire screen with blanks

            int numCellsWritten = 0;
            success = Interop.mincore.FillConsoleOutputCharacter(hConsole, ' ',
                conSize, coordScreen, out numCellsWritten);
            if (!success)
                throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());

            // now set the buffer's attributes accordingly

            numCellsWritten = 0;
            success = Interop.mincore.FillConsoleOutputAttribute(hConsole, csbi.wAttributes,
                conSize, coordScreen, out numCellsWritten);
            if (!success)
                throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());

            // put the cursor at (0, 0)

            success = Interop.mincore.SetConsoleCursorPosition(hConsole, coordScreen);
            if (!success)
                throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
        }

        public static void SetCursorPosition(int left, int top)
        {
            // Note on argument checking - the upper bounds are NOT correct 
            // here!  But it looks slightly expensive to compute them.  Let
            // Windows calculate them, then we'll give a nice error message.
            if (left < 0 || left >= Int16.MaxValue)
                throw new ArgumentOutOfRangeException("left", left, SR.ArgumentOutOfRange_ConsoleBufferBoundaries);
            if (top < 0 || top >= Int16.MaxValue)
                throw new ArgumentOutOfRangeException("top", top, SR.ArgumentOutOfRange_ConsoleBufferBoundaries);
            Contract.EndContractBlock();

            IntPtr hConsole = OutputHandle;
            Interop.mincore.COORD coords = new Interop.mincore.COORD();
            coords.X = (short)left;
            coords.Y = (short)top;
            if (!Interop.mincore.SetConsoleCursorPosition(hConsole, coords))
            {
                // Give a nice error message for out of range sizes
                int errorCode = Marshal.GetLastWin32Error();
                Interop.mincore.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();
                if (left < 0 || left >= csbi.dwSize.X)
                    throw new ArgumentOutOfRangeException("left", left, SR.ArgumentOutOfRange_ConsoleBufferBoundaries);
                if (top < 0 || top >= csbi.dwSize.Y)
                    throw new ArgumentOutOfRangeException("top", top, SR.ArgumentOutOfRange_ConsoleBufferBoundaries);

                throw Win32Marshal.GetExceptionForWin32Error(errorCode);
            }
        }

        public static int BufferWidth
        {
            // TODO #4636: Implement this
            get { return ConsolePal.BufferWidth; }
            set { ConsolePal.BufferWidth = value; }
        }

        public static int BufferHeight
        {
            // TODO #4636: Implement this
            get { return ConsolePal.BufferHeight; }
            set { ConsolePal.BufferHeight = value; }
        }

        public static int WindowLeft
        {
            get
            {
                Interop.mincore.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();
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
                Interop.mincore.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();
                return csbi.srWindow.Left;
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
                Interop.mincore.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();
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
                Interop.mincore.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();
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
            Interop.mincore.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();

            Interop.mincore.SMALL_RECT srWindow = csbi.srWindow;

            // Check for arithmetic underflows & overflows.
            int newRight = left + srWindow.Right - srWindow.Left + 1;
            if (left < 0 || newRight > csbi.dwSize.X || newRight < 0)
                throw new ArgumentOutOfRangeException("left", left, SR.ArgumentOutOfRange_ConsoleWindowPos);
            int newBottom = top + srWindow.Bottom - srWindow.Top + 1;
            if (top < 0 || newBottom > csbi.dwSize.Y || newBottom < 0)
                throw new ArgumentOutOfRangeException("top", top, SR.ArgumentOutOfRange_ConsoleWindowPos);

            // Preserve the size, but move the position.
            srWindow.Bottom -= (short)(srWindow.Top - top);
            srWindow.Right -= (short)(srWindow.Left - left);
            srWindow.Left = (short)left;
            srWindow.Top = (short)top;

            bool r = Interop.mincore.SetConsoleWindowInfo(OutputHandle, true, &srWindow);
            if (!r)
                throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
        }

        public static unsafe void SetWindowSize(int width, int height)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException("width", width, SR.ArgumentOutOfRange_NeedPosNum);
            if (height <= 0)
                throw new ArgumentOutOfRangeException("height", height, SR.ArgumentOutOfRange_NeedPosNum);

            // Get the position of the current console window
            Interop.mincore.CONSOLE_SCREEN_BUFFER_INFO csbi = GetBufferInfo();

            // If the buffer is smaller than this new window size, resize the
            // buffer to be large enough.  Include window position.
            bool resizeBuffer = false;
            Interop.mincore.COORD size = new Interop.mincore.COORD();
            size.X = csbi.dwSize.X;
            size.Y = csbi.dwSize.Y;
            if (csbi.dwSize.X < csbi.srWindow.Left + width)
            {
                if (csbi.srWindow.Left >= Int16.MaxValue - width)
                    throw new ArgumentOutOfRangeException("width", SR.ArgumentOutOfRange_ConsoleWindowBufferSize);
                size.X = (short)(csbi.srWindow.Left + width);
                resizeBuffer = true;
            }
            if (csbi.dwSize.Y < csbi.srWindow.Top + height)
            {
                if (csbi.srWindow.Top >= Int16.MaxValue - height)
                    throw new ArgumentOutOfRangeException("height", SR.ArgumentOutOfRange_ConsoleWindowBufferSize);
                size.Y = (short)(csbi.srWindow.Top + height);
                resizeBuffer = true;
            }
            if (resizeBuffer)
            {
                if (!Interop.mincore.SetConsoleScreenBufferSize(OutputHandle, size))
                    throw Win32Marshal.GetExceptionForWin32Error(Marshal.GetLastWin32Error());
            }

            Interop.mincore.SMALL_RECT srWindow = csbi.srWindow;
            // Preserve the position, but change the size.
            srWindow.Bottom = (short)(srWindow.Top + height - 1);
            srWindow.Right = (short)(srWindow.Left + width - 1);

            if (!Interop.mincore.SetConsoleWindowInfo(OutputHandle, true, &srWindow))
            {
                int errorCode = Marshal.GetLastWin32Error();

                // If we resized the buffer, un-resize it.
                if (resizeBuffer)
                {
                    Interop.mincore.SetConsoleScreenBufferSize(OutputHandle, csbi.dwSize);
                }

                // Try to give a better error message here
               Interop.mincore.COORD bounds = Interop.mincore.GetLargestConsoleWindowSize(OutputHandle);
                if (width > bounds.X)
                    throw new ArgumentOutOfRangeException("width", width, SR.Format(SR.ArgumentOutOfRange_ConsoleWindowSize_Size, bounds.X));
                if (height > bounds.Y)
                    throw new ArgumentOutOfRangeException("height", height, SR.Format(SR.ArgumentOutOfRange_ConsoleWindowSize_Size, bounds.Y));

                throw Win32Marshal.GetExceptionForWin32Error(errorCode);
            }
        }


        private static Interop.mincore.Color ConsoleColorToColorAttribute(ConsoleColor color, bool isBackground)
        {
            if ((((int)color) & ~0xf) != 0)
                throw new ArgumentException(SR.Arg_InvalidConsoleColor);
            Contract.EndContractBlock();

            Interop.mincore.Color c = (Interop.mincore.Color)color;

            // Make these background colors instead of foreground
            if (isBackground)
                c = (Interop.mincore.Color)((int)c << 4);
            return c;
        }

        private static ConsoleColor ColorAttributeToConsoleColor(Interop.mincore.Color c)
        {
            // Turn background colors into foreground colors.
            if ((c & Interop.mincore.Color.BackgroundMask) != 0)
            {
                c = (Interop.mincore.Color)(((int)c) >> 4);
            }
            return (ConsoleColor)c;
        }

        private static Interop.mincore.CONSOLE_SCREEN_BUFFER_INFO GetBufferInfo()
        {
            bool unused;
            return GetBufferInfo(true, out unused);
        }

        // For apps that don't have a console (like Windows apps), they might
        // run other code that includes color console output.  Allow a mechanism
        // where that code won't throw an exception for simple errors.
        private static Interop.mincore.CONSOLE_SCREEN_BUFFER_INFO GetBufferInfo(bool throwOnNoConsole, out bool succeeded)
        {
            succeeded = false;

            IntPtr outputHandle = OutputHandle;
            if (outputHandle == s_InvalidHandleValue)
            {
                if (throwOnNoConsole)
                {
                    throw new IOException(SR.IO_NoConsole);
                }
                return new Interop.mincore.CONSOLE_SCREEN_BUFFER_INFO();
            }

            // Note that if stdout is redirected to a file, the console handle may be a file.  
            // First try stdout; if this fails, try stderr and then stdin.
            Interop.mincore.CONSOLE_SCREEN_BUFFER_INFO csbi;
            if (!Interop.mincore.GetConsoleScreenBufferInfo(outputHandle, out csbi) &&
                !Interop.mincore.GetConsoleScreenBufferInfo(ErrorHandle, out csbi) &&
                !Interop.mincore.GetConsoleScreenBufferInfo(InputHandle, out csbi))
            {
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode == Interop.mincore.Errors.ERROR_INVALID_HANDLE && !throwOnNoConsole)
                    return new Interop.mincore.CONSOLE_SCREEN_BUFFER_INFO();
                throw Win32Marshal.GetExceptionForWin32Error(errorCode);
            }

            if (!_haveReadDefaultColors)
            {
                // Fetch the default foreground and background color for the ResetColor method.
                Debug.Assert((int)Interop.mincore.Color.ColorMask == 0xff, "Make sure one byte is large enough to store a Console color value!");
                _defaultColors = (byte)(csbi.wAttributes & (short)Interop.mincore.Color.ColorMask);
                _haveReadDefaultColors = true;
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

            internal WindowsConsoleStream(IntPtr handle, FileAccess access)
                : base(access)
            {
                Debug.Assert(handle != IntPtr.Zero && handle != s_InvalidHandleValue, "ConsoleStream expects a valid handle!");
                _handle = handle;
                _isPipe = Interop.mincore.GetFileType(handle) == Interop.mincore.FileTypes.FILE_TYPE_PIPE;
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

            public override int Read([In, Out] byte[] buffer, int offset, int count)
            {
                ValidateRead(buffer, offset, count);

                int bytesRead;
                int errCode = ReadFileNative(_handle, buffer, offset, count, _isPipe, out bytesRead);
                if (Interop.mincore.Errors.ERROR_SUCCESS != errCode)
                    throw Win32Marshal.GetExceptionForWin32Error(errCode);
                return bytesRead;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                ValidateWrite(buffer, offset, count);

                int errCode = WriteFileNative(_handle, buffer, offset, count);
                if (Interop.mincore.Errors.ERROR_SUCCESS != errCode)
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

            private unsafe static int ReadFileNative(IntPtr hFile, byte[] bytes, int offset, int count, bool isPipe, out int bytesRead)
            {
                Contract.Requires(offset >= 0, "offset >= 0");
                Contract.Requires(count >= 0, "count >= 0");
                Contract.Requires(bytes != null, "bytes != null");
                // Don't corrupt memory when multiple threads are erroneously writing
                // to this stream simultaneously.
                if (bytes.Length - offset < count)
                    throw new IndexOutOfRangeException(SR.IndexOutOfRange_IORaceCondition);
                Contract.EndContractBlock();

                // You can't use the fixed statement on an array of length 0.
                if (bytes.Length == 0)
                {
                    bytesRead = 0;
                    return Interop.mincore.Errors.ERROR_SUCCESS;
                }

                bool readSuccess;
                fixed (byte* p = bytes)
                {
                    readSuccess = (0 != Interop.mincore.ReadFile(hFile, p + offset, count, out bytesRead, IntPtr.Zero));

                    // If the code page could be Unicode, we should use ReadConsole instead, e.g.
                    // int charsRead;
                    // readSuccess = Interop.mincore.ReadConsole(hFile, p + offset, count / BytesPerWChar, out charsRead, IntPtr.Zero);
                    // bytesRead = charsRead * BytesPerWChar;
                }
                if (readSuccess)
                    return Interop.mincore.Errors.ERROR_SUCCESS;

                // For pipes that are closing or broken, just stop.
                // (E.g. ERROR_NO_DATA ("pipe is being closed") is returned when we write to a console that is closing;
                // ERROR_BROKEN_PIPE ("pipe was closed") is returned when stdin was closed, which is mot an error, but EOF.)
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode == Interop.mincore.Errors.ERROR_NO_DATA || errorCode == Interop.mincore.Errors.ERROR_BROKEN_PIPE)
                    return Interop.mincore.Errors.ERROR_SUCCESS;
                return errorCode;
            }

            private static unsafe int WriteFileNative(IntPtr hFile, byte[] bytes, int offset, int count)
            {
                Contract.Requires(offset >= 0, "offset >= 0");
                Contract.Requires(count >= 0, "count >= 0");
                Contract.Requires(bytes != null, "bytes != null");
                Contract.Requires(bytes.Length >= offset + count, "bytes.Length >= offset + count");

                // You can't use the fixed statement on an array of length 0.
                if (bytes.Length == 0)
                    return Interop.mincore.Errors.ERROR_SUCCESS;

                bool writeSuccess;
                fixed (byte* p = bytes)
                {
                    int numBytesWritten;
                    writeSuccess = (0 != Interop.mincore.WriteFile(hFile, p + offset, count, out numBytesWritten, IntPtr.Zero));
                    Debug.Assert(!writeSuccess || count == numBytesWritten);

                    // If the code page could be Unicode, we should use ReadConsole instead, e.g.
                    // // Note that WriteConsoleW has a max limit on num of chars to write (64K)
                    // // [http://msdn.microsoft.com/en-us/library/ms687401.aspx]
                    // // However, we do not need to worry about that because the StreamWriter in Console has
                    // // a much shorter buffer size anyway.
                    // Int32 charsWritten;
                    // writeSuccess = Interop.mincore.WriteConsole(hFile, p + offset, count / BytesPerWChar, out charsWritten, IntPtr.Zero);
                    // Debug.Assert(!writeSuccess || count / BytesPerWChar == charsWritten);
                }
                if (writeSuccess)
                    return Interop.mincore.Errors.ERROR_SUCCESS;

                // For pipes that are closing or broken, just stop.
                // (E.g. ERROR_NO_DATA ("pipe is being closed") is returned when we write to a console that is closing;
                // ERROR_BROKEN_PIPE ("pipe was closed") is returned when stdin was closed, which is mot an error, but EOF.)
                int errorCode = Marshal.GetLastWin32Error();
                if (errorCode == Interop.mincore.Errors.ERROR_NO_DATA || errorCode == Interop.mincore.Errors.ERROR_BROKEN_PIPE)
                    return Interop.mincore.Errors.ERROR_SUCCESS;
                return errorCode;
            }
        }

        internal sealed class ControlCHandlerRegistrar
        {
            private bool _handlerRegistered;
            private Interop.mincore.ConsoleCtrlHandlerRoutine _handler;

            internal ControlCHandlerRegistrar()
            {
                _handler = new Interop.mincore.ConsoleCtrlHandlerRoutine(BreakEvent);
            }

            internal void Register()
            {
                Debug.Assert(!_handlerRegistered);

                bool r = Interop.mincore.SetConsoleCtrlHandler(_handler, true);
                if (!r)
                {
                    throw Win32Marshal.GetExceptionForLastWin32Error();
                }

                _handlerRegistered = true;
            }

            internal void Unregister()
            {
                Debug.Assert(_handlerRegistered);

                bool r = Interop.mincore.SetConsoleCtrlHandler(_handler, false);
                if (!r)
                {
                    throw Win32Marshal.GetExceptionForLastWin32Error();
                }
                _handlerRegistered = false;
            }

            private static bool BreakEvent(int controlType)
            {
                if (controlType != Interop.mincore.CTRL_C_EVENT &&
                    controlType != Interop.mincore.CTRL_BREAK_EVENT)
                {
                    return false;
                }

                return Console.HandleBreakEvent(controlType == Interop.mincore.CTRL_C_EVENT ? ConsoleSpecialKey.ControlC : ConsoleSpecialKey.ControlBreak);
            }
        }
    }
}