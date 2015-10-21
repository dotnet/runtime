// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

namespace System.IO
{
    /* This class is used by for reading from the stdin.
     * It is designed to read stdin in raw mode for interpreting
     * key press events and maintain its own buffer for the same.
     * which is then used for all the Read operations
     */
    internal class StdInStreamReader : StreamReader
    {
        private readonly char[] _unprocessedBufferToBeRead; // Buffer that might have already been read from stdin but not yet processed.
        private const int BytesToBeRead = 1024; // No. of bytes to be read from the stream at a time.
        private int _startIndex; // First unprocessed index in the buffer;
        private int _endIndex; // Last unprocessed index in the buffer;
        private readonly StringBuilder _readLineSB; // SB that holds readLine output

        internal StdInStreamReader(Stream stream, Encoding encoding, int bufferSize) : base(stream: stream, encoding: encoding, detectEncodingFromByteOrderMarks: false, bufferSize: bufferSize, leaveOpen: true)
        {
            _unprocessedBufferToBeRead = new char[encoding.GetMaxCharCount(BytesToBeRead)];
            _startIndex = 0;
            _endIndex = -1; // There is no unprocessed index yet.
            _readLineSB = new StringBuilder();
        }

        /// <summary> Checks whether the unprocessed buffer is empty. </summary>
        bool IsExtraBufferEmpty()
        {
            return _startIndex > _endIndex; // Everything has been processed;
        }

        /// <summary> Reads the buffer in the raw mode. </summary>
        unsafe void ReadExtraBuffer()
        {
            Debug.Assert(IsExtraBufferEmpty());

            byte* bufPtr = stackalloc byte[BytesToBeRead];
            int result;
            Interop.CheckIo(result = Interop.Sys.ReadStdinUnbuffered(bufPtr, BytesToBeRead));

            Debug.Assert(result > 0 && result <= BytesToBeRead);

            // Now we need to convert the byte buffer to char buffer.
            fixed (char* unprocessedBufPtr = _unprocessedBufferToBeRead)
            {
                _startIndex = 0;
                _endIndex = CurrentEncoding.GetChars(bufPtr, result, unprocessedBufPtr, _unprocessedBufferToBeRead.Length) - 1;
            }
        }

        public override string ReadLine()
        {
            string readLineStr;

            //Check if there is anything left in the _unprocessedBufferToBeRead.
            while (!IsExtraBufferEmpty())
            {
                ConsoleKeyInfo keyInfo = ReadKey();

                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    readLineStr = _readLineSB.ToString();
                    _readLineSB.Clear();
                    return readLineStr;
                }

                else if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    int len = _readLineSB.Length;
                    if (len > 0)
                    {
                        _readLineSB.Length = len - 1;
                    }
                }
                else
                {
                    _readLineSB.Append(keyInfo.KeyChar);
                }
            }

            readLineStr = _readLineSB.Append(base.ReadLine()).ToString();
            _readLineSB.Clear();

            return readLineStr;
        }


        public override int Read()
        {
            // Convert byte to char.
            return IsExtraBufferEmpty() ? base.Read() : (int)_unprocessedBufferToBeRead[_startIndex++];
        }

        internal ConsoleKey GetKeyFromCharValue(char x, out bool isShift, out bool isCtrl)
        {
            isShift = false;
            isCtrl = false;

            switch (x)
            {
                case '\b':
                    return ConsoleKey.Backspace;

                case '\t':
                    return ConsoleKey.Tab;

                case '\n':
                    return ConsoleKey.Enter;

                case (char)(int)(0x1B):
                    return ConsoleKey.Escape;

                case '*':
                    return ConsoleKey.Multiply;

                case '+':
                    return ConsoleKey.Add;

                case '-':
                    return ConsoleKey.Subtract;

                case '/':
                    return ConsoleKey.Divide;

                case (char)(int)(0x7F):
                    return ConsoleKey.Delete;

                case ' ':
                    return ConsoleKey.Spacebar;

                default:
                    // 1. Ctrl A to Ctrl Z.
                    if (x >= 1 && x <= 26)
                    {
                        isCtrl = true;
                        return ConsoleKey.A + x - 1;
                    }

                    // 2. Numbers from 0 to 9.
                    if (x >= '0' && x <= '9')
                    {
                        return ConsoleKey.D0 + x - '0';
                    }

                    //3. A to Z
                    if (x >= 'A' && x <= 'Z')
                    {
                        isShift = true;
                        return ConsoleKey.A + (x - 'A');
                    }

                    // 4. a to z.
                    if (x >= 'a' && x <= 'z')
                    {
                        return ConsoleKey.A + (x - 'a');
                    }

                    break;
            }

            return default(ConsoleKey);
        }

        internal bool MapBufferToConsoleKey(out ConsoleKey key, out char ch, out bool isShift, out bool isAlt, out bool isCtrl)
        {
            Debug.Assert(!IsExtraBufferEmpty());

            isAlt = isCtrl = isShift = false;
            int keyLength;

            // Try to get the special key match from the TermInfo static information.
            if (ConsolePal.TryGetSpecialConsoleKey(_unprocessedBufferToBeRead, _startIndex, _endIndex, out key, out keyLength))
            {
                ch = ((keyLength == 1) ? _unprocessedBufferToBeRead[_startIndex] : '\0');
                _startIndex += keyLength;
                return true;
            }

            // Check if we can match Esc + combination and guess if alt was pressed.
            if (isAlt == false &&
                _unprocessedBufferToBeRead[_startIndex] == (char)(int)(0x1B) /*Alt is send as an escape character*/ &&
                _endIndex - _startIndex + 1 >= 2 /*We have at least two characters to read.*/)
            {
                isAlt = true; // Since the alt is pressed.
                _startIndex++;
                if (MapBufferToConsoleKey(out key, out ch, out isShift, out isAlt, out isCtrl))
                {
                    isAlt = true;
                    return true;
                }
                else
                {
                    // We could not find a matching key here so, Alt+ combination assumption is in-correct.
                    // The current key needs to be marked as Esc key.
                    // Also, we do not increment _startIndex as we already did it.
                    key = ConsoleKey.Escape;
                    ch = (char)(int)(0x1B);
                    isAlt = false;
                    return true;
                }
            }

            // Try reading the first char in the buffer and interpret it as a key.
            ch = _unprocessedBufferToBeRead[_startIndex++];
            key = GetKeyFromCharValue(ch, out isShift, out isCtrl);

            return key != default(ConsoleKey);
        }

        /// <summary>
        /// Try to intercept the key pressed.
        /// 
        /// Unlike Windows Unix has no concept of virtual key codes.
        /// Hence, in case we do not recognize a key, we can't really
        /// get the ConsoleKey key code associated with it.
        /// As a result, we try to recognize the key, and if that does
        /// not work, we simply return the char associated with that
        /// key with ConsoleKey set to default value.
        /// </summary>
        public ConsoleKeyInfo ReadKey()
        {
            ConsoleKey key;
            char ch;
            bool isAlt, isCtrl, isShift;

            if (IsExtraBufferEmpty())
            {
                ReadExtraBuffer();
            }

            MapBufferToConsoleKey(out key, out ch, out isShift, out isAlt, out isCtrl);
            return new ConsoleKeyInfo(ch, key, isShift, isAlt, isCtrl);
        }
    }
}
