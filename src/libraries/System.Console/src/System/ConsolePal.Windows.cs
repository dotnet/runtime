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
    // Required exposed methods:
    // - OpenStandardInput, OpenStandardOutput, OpenStandardError
    // - InputEncoding, OutputEncoding
    // - ForegroundColor, BackgroundColor, ResetColor
    internal static class ConsolePal
    {
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
                if (_handle == IntPtr.Zero) throw __Error.GetFileNotOpen();
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