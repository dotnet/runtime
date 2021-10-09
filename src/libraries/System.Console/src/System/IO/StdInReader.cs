// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace System.IO
{
    /* This class is used by for reading from the stdin.
     * It is designed to read stdin in raw mode for interpreting
     * key press events and maintain its own buffer for the same.
     * which is then used for all the Read operations
     */
    internal sealed class StdInReader : TextReader
    {
        private static string? s_moveLeftString; // string written to move the cursor to the left
        private static string? s_clearToEol;     // string written to clear from cursor to end of line

        private readonly StringBuilder _readLineSB; // SB that holds readLine output.  This is a field simply to enable reuse; it's only used in ReadLine.
        private readonly Stack<ConsoleKeyInfo> _tmpKeys = new Stack<ConsoleKeyInfo>(); // temporary working stack; should be empty outside of ReadLine
        private readonly Stack<ConsoleKeyInfo> _availableKeys = new Stack<ConsoleKeyInfo>(); // a queue of already processed key infos available for reading
        private readonly Encoding _encoding;
        private Encoder? _bufferReadEncoder;

        private char[] _unprocessedBufferToBeRead; // Buffer that might have already been read from stdin but not yet processed.
        private const int BytesToBeRead = 1024; // No. of bytes to be read from the stream at a time.
        private int _startIndex; // First unprocessed index in the buffer;
        private int _endIndex; // Index after last unprocessed index in the buffer;

        internal StdInReader(Encoding encoding, int bufferSize)
        {
            _encoding = encoding;
            _unprocessedBufferToBeRead = new char[encoding.GetMaxCharCount(BytesToBeRead)];
            _startIndex = 0;
            _endIndex = 0;
            _readLineSB = new StringBuilder();
        }

        /// <summary> Checks whether the unprocessed buffer is empty. </summary>
        internal bool IsUnprocessedBufferEmpty()
        {
            return _startIndex >= _endIndex; // Everything has been processed;
        }

        internal void AppendExtraBuffer(ReadOnlySpan<byte> buffer)
        {
            // Ensure a reasonable upper bound applies to the stackalloc
            Debug.Assert(buffer.Length <= 1024);

            // Then convert the bytes to chars
            Span<char> chars = stackalloc char[_encoding.GetMaxCharCount(buffer.Length)];
            int charLen = _encoding.GetChars(buffer, chars);
            chars = chars.Slice(0, charLen);

            // Ensure our buffer is large enough to hold all of the data
            if (IsUnprocessedBufferEmpty())
            {
                _startIndex = _endIndex = 0;
            }
            else
            {
                Debug.Assert(_endIndex > 0);
                int spaceRemaining = _unprocessedBufferToBeRead.Length - _endIndex;
                if (spaceRemaining < chars.Length)
                {
                    Array.Resize(ref _unprocessedBufferToBeRead, _unprocessedBufferToBeRead.Length * 2);
                }
            }

            // Copy the data into our buffer
            chars.CopyTo(_unprocessedBufferToBeRead.AsSpan(_endIndex));
            _endIndex += charLen;
        }

        internal unsafe int ReadStdin(byte* buffer, int bufferSize)
        {
            int result = Interop.CheckIo(Interop.Sys.ReadStdin(buffer, bufferSize));
            Debug.Assert(result >= 0 && result <= bufferSize); // may be 0 if hits EOL
            return result;
        }

        public override string? ReadLine()
        {
            bool isEnter = ReadLineCore(consumeKeys: true);
            string? line = null;
            if (isEnter || _readLineSB.Length > 0)
            {
                line = _readLineSB.ToString();
                _readLineSB.Clear();
            }
            return line;
        }

        public int ReadLine(Span<byte> buffer)
        {
            if (buffer.IsEmpty)
            {
                return 0;
            }

            // Don't read a new line if there are remaining characters in the StringBuilder.
            if (_readLineSB.Length == 0)
            {
                bool isEnter = ReadLineCore(consumeKeys: true);
                if (isEnter)
                {
                    _readLineSB.Append('\n');
                }
            }

            // Encode line into buffer.
            Encoder encoder = _bufferReadEncoder ??= _encoding.GetEncoder();
            int bytesUsedTotal = 0;
            int charsUsedTotal = 0;
            foreach (ReadOnlyMemory<char> chunk in _readLineSB.GetChunks())
            {
                encoder.Convert(chunk.Span, buffer, flush: false, out int charsUsed, out int bytesUsed, out bool completed);
                buffer = buffer.Slice(bytesUsed);
                bytesUsedTotal += bytesUsed;
                charsUsedTotal += charsUsed;

                if (charsUsed == 0)
                {
                    break;
                }
            }
            _readLineSB.Remove(0, charsUsedTotal);
            return bytesUsedTotal;
        }

        // Reads a line in _readLineSB when consumeKeys is true,
        //              or _availableKeys when consumeKeys is false.
        // Returns whether the line was terminated using the Enter key.
        private bool ReadLineCore(bool consumeKeys)
        {
            Debug.Assert(_tmpKeys.Count == 0);
            Debug.Assert(consumeKeys || _availableKeys.Count == 0);

            // _availableKeys either contains a line that was already read,
            // or we need to read a new line from stdin.
            bool freshKeys = _availableKeys.Count == 0;

           // Don't carry over chars from previous ReadLine call.
            _readLineSB.Clear();

            Interop.Sys.InitializeConsoleBeforeRead();
            try
            {
                // Read key-by-key until we've read a line.
                while (true)
                {
                    ConsoleKeyInfo keyInfo = freshKeys ? ReadKey() : _availableKeys.Pop();
                    if (!consumeKeys && keyInfo.Key != ConsoleKey.Backspace) // backspace is the only character not written out in the below if/elses.
                    {
                        _tmpKeys.Push(keyInfo);
                    }

                    // Handle the next key.  Since for other functions we may have ended up reading some of the user's
                    // input, we need to be able to handle manually processing that input, and so we do that processing
                    // for all input.  As such, we need to special-case a few characters, e.g. recognizing when Enter is
                    // pressed to end a line.  We also need to handle Backspace specially, to fix up both our buffer of
                    // characters and the position of the cursor.  More advanced processing would be possible, but we
                    // try to keep this very simple, at least for now.
                    if (keyInfo.Key == ConsoleKey.Enter)
                    {
                        if (freshKeys)
                        {
                            Console.WriteLine();
                        }
                        return true;
                    }
                    else if (IsEol(keyInfo.KeyChar))
                    {
                        return false;
                    }
                    else if (keyInfo.Key == ConsoleKey.Backspace)
                    {
                        bool removed = false;
                        if (consumeKeys)
                        {
                            int len = _readLineSB.Length;
                            if (len > 0)
                            {
                                _readLineSB.Length = len - 1;
                                removed = true;
                            }
                        }
                        else
                        {
                            removed = _tmpKeys.TryPop(out _);
                        }

                        if (removed && freshKeys)
                        {
                            // The ReadLine input may wrap across terminal rows and we need to handle that.
                            // note: ConsolePal will cache the cursor position to avoid making many slow cursor position fetch operations.
                            if (ConsolePal.TryGetCursorPosition(out int left, out int top, reinitializeForRead: true) &&
                                left == 0 && top > 0)
                            {
                                if (s_clearToEol == null)
                                {
                                    s_clearToEol = ConsolePal.TerminalFormatStrings.Instance.ClrEol ?? string.Empty;
                                }

                                // Move to end of previous line
                                ConsolePal.SetCursorPosition(ConsolePal.WindowWidth - 1, top - 1);
                                // Clear from cursor to end of the line
                                ConsolePal.WriteStdoutAnsiString(s_clearToEol, mayChangeCursorPosition: false);
                            }
                            else
                            {
                                if (s_moveLeftString == null)
                                {
                                    string? moveLeft = ConsolePal.TerminalFormatStrings.Instance.CursorLeft;
                                    s_moveLeftString = !string.IsNullOrEmpty(moveLeft) ? moveLeft + " " + moveLeft : string.Empty;
                                }

                                Console.Write(s_moveLeftString);
                            }
                        }
                    }
                    else if (keyInfo.Key == ConsoleKey.Tab)
                    {
                        if (consumeKeys)
                        {
                            _readLineSB.Append(keyInfo.KeyChar);
                        }
                        if (freshKeys)
                        {
                            Console.Write(' ');
                        }
                    }
                    else if (keyInfo.Key == ConsoleKey.Clear)
                    {
                        _readLineSB.Clear();
                        if (freshKeys)
                        {
                            Console.Clear();
                        }
                    }
                    else if (keyInfo.KeyChar != '\0')
                    {
                        if (consumeKeys)
                        {
                            _readLineSB.Append(keyInfo.KeyChar);
                        }
                        if (freshKeys)
                        {
                            Console.Write(keyInfo.KeyChar);
                        }
                    }
                }
            }
            finally
            {
                Interop.Sys.UninitializeConsoleAfterRead();

                // If we're not consuming the read input, make the keys available for a future read
                while (_tmpKeys.Count > 0)
                {
                    _availableKeys.Push(_tmpKeys.Pop());
                }
            }
        }

        public override int Read() => ReadOrPeek(peek: false);

        public override int Peek() => ReadOrPeek(peek: true);

        private int ReadOrPeek(bool peek)
        {
            // If there aren't any keys in our processed keys stack, read a line to populate it.
            if (_availableKeys.Count == 0)
            {
                ReadLineCore(consumeKeys: false);
            }

            // Now if there are keys, use the first.
            if (_availableKeys.Count > 0)
            {
                ConsoleKeyInfo keyInfo = peek ? _availableKeys.Peek() : _availableKeys.Pop();
                if (!IsEol(keyInfo.KeyChar))
                {
                    return keyInfo.KeyChar;
                }
            }

            // EOL
            return -1;
        }

        private static bool IsEol(char c)
        {
            return
                c != ConsolePal.s_posixDisableValue &&
                (c == ConsolePal.s_veolCharacter || c == ConsolePal.s_veol2Character || c == ConsolePal.s_veofCharacter);
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
                case '\r':
                    return ConsoleKey.Enter;

                case (char)(0x1B):
                    return ConsoleKey.Escape;

                case '*':
                    return ConsoleKey.Multiply;

                case '+':
                    return ConsoleKey.Add;

                case '-':
                    return ConsoleKey.Subtract;

                case '/':
                    return ConsoleKey.Divide;

                case (char)(0x7F):
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
            Debug.Assert(!IsUnprocessedBufferEmpty());

            // Try to get the special key match from the TermInfo static information.
            ConsoleKeyInfo keyInfo;
            int keyLength;
            if (ConsolePal.TryGetSpecialConsoleKey(_unprocessedBufferToBeRead, _startIndex, _endIndex, out keyInfo, out keyLength))
            {
                key = keyInfo.Key;
                isShift = (keyInfo.Modifiers & ConsoleModifiers.Shift) != 0;
                isAlt = (keyInfo.Modifiers & ConsoleModifiers.Alt) != 0;
                isCtrl = (keyInfo.Modifiers & ConsoleModifiers.Control) != 0;

                ch = ((keyLength == 1) ? _unprocessedBufferToBeRead[_startIndex] : '\0'); // ignore keyInfo.KeyChar
                _startIndex += keyLength;
                return true;
            }

            // Check if we can match Esc + combination and guess if alt was pressed.
            isAlt = isCtrl = isShift = false;
            if (_unprocessedBufferToBeRead[_startIndex] == (char)0x1B && // Alt is send as an escape character
                _endIndex - _startIndex >= 2) // We have at least two characters to read
            {
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
                    ch = (char)0x1B;
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
        /// Unlike Windows, Unix has no concept of virtual key codes.
        /// Hence, in case we do not recognize a key, we can't really
        /// get the ConsoleKey key code associated with it.
        /// As a result, we try to recognize the key, and if that does
        /// not work, we simply return the char associated with that
        /// key with ConsoleKey set to default value.
        /// </summary>
        public ConsoleKeyInfo ReadKey(out bool previouslyProcessed)
        {
            if (_availableKeys.Count > 0)
            {
                previouslyProcessed = true;
                return _availableKeys.Pop();
            }

            previouslyProcessed = false;
            return ReadKey();
        }

        private unsafe ConsoleKeyInfo ReadKey()
        {
            Debug.Assert(_availableKeys.Count == 0);

            Interop.Sys.InitializeConsoleBeforeRead();
            try
            {
                ConsoleKey key;
                char ch;
                bool isAlt, isCtrl, isShift;

                if (IsUnprocessedBufferEmpty())
                {
                    // Read in bytes
                    byte* bufPtr = stackalloc byte[BytesToBeRead];
                    int result = ReadStdin(bufPtr, BytesToBeRead);
                    if (result > 0)
                    {
                        // Append them
                        AppendExtraBuffer(new ReadOnlySpan<byte>(bufPtr, result));
                    }
                    else
                    {
                        // Could be empty if EOL entered on its own.  Pick one of the EOL characters we have,
                        // or just use 0 if none are available.
                        return new ConsoleKeyInfo((char)
                            (ConsolePal.s_veolCharacter != ConsolePal.s_posixDisableValue ? ConsolePal.s_veolCharacter :
                             ConsolePal.s_veol2Character != ConsolePal.s_posixDisableValue ? ConsolePal.s_veol2Character :
                             ConsolePal.s_veofCharacter != ConsolePal.s_posixDisableValue ? ConsolePal.s_veofCharacter :
                             0),
                            default(ConsoleKey), false, false, false);
                    }
                }

                MapBufferToConsoleKey(out key, out ch, out isShift, out isAlt, out isCtrl);

                // Replace the '\n' char for Enter by '\r' to match Windows behavior.
                if (key == ConsoleKey.Enter && ch == '\n')
                {
                    ch = '\r';
                }

                return new ConsoleKeyInfo(ch, key, isShift, isAlt, isCtrl);
            }
            finally
            {
                Interop.Sys.UninitializeConsoleAfterRead();
            }
        }

        /// <summary>Gets whether there's input waiting on stdin.</summary>
        internal bool StdinReady { get { return Interop.Sys.StdinReady(); } }
    }
}
