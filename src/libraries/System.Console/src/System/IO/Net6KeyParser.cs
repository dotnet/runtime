// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO;

internal static class Net6KeyParser
{
    internal static ConsoleKeyInfo Parse(char[] buffer, TerminalFormatStrings terminalFormatStrings, byte posixDisableValue, byte veraseCharacter, ref int startIndex, int endIndex)
    {
        MapBufferToConsoleKey(buffer, terminalFormatStrings, posixDisableValue, veraseCharacter, out ConsoleKey key,
            out char ch, out bool isShift, out bool isAlt, out bool isCtrl, ref startIndex, endIndex);

        // Replace the '\n' char for Enter by '\r' to match Windows behavior.
        if (key == ConsoleKey.Enter && ch == '\n')
        {
            ch = '\r';
        }

        return new ConsoleKeyInfo(ch, key, isShift, isAlt, isCtrl);
    }

    private static bool MapBufferToConsoleKey(char[] buffer, TerminalFormatStrings terminalFormatStrings, byte posixDisableValue, byte veraseCharacter,
        out ConsoleKey key, out char ch, out bool isShift, out bool isAlt, out bool isCtrl, ref int startIndex, int endIndex)
    {
        // Try to get the special key match from the TermInfo static information.
        if (TryGetSpecialConsoleKey(buffer, startIndex, endIndex, terminalFormatStrings, posixDisableValue, veraseCharacter, out ConsoleKeyInfo keyInfo, out int keyLength))
        {
            key = keyInfo.Key;
            isShift = (keyInfo.Modifiers & ConsoleModifiers.Shift) != 0;
            isAlt = (keyInfo.Modifiers & ConsoleModifiers.Alt) != 0;
            isCtrl = (keyInfo.Modifiers & ConsoleModifiers.Control) != 0;

            ch = ((keyLength == 1) ? buffer[startIndex] : '\0'); // ignore keyInfo.KeyChar
            startIndex += keyLength;
            return true;
        }

        // Check if we can match Esc + combination and guess if alt was pressed.
        if (buffer[startIndex] == (char)0x1B && // Alt is send as an escape character
            endIndex - startIndex >= 2) // We have at least two characters to read
        {
            startIndex++;
            if (MapBufferToConsoleKey(buffer, terminalFormatStrings, posixDisableValue, veraseCharacter, out key, out ch, out isShift, out _, out isCtrl, ref startIndex, endIndex))
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
        ch = buffer[startIndex++];
        key = GetKeyFromCharValue(ch, out isShift, out isCtrl);
        isAlt = false;
        return key != default(ConsoleKey);
    }

    private static bool TryGetSpecialConsoleKey(char[] givenChars, int startIndex, int endIndex,
        TerminalFormatStrings terminalFormatStrings, byte posixDisableValue, byte veraseCharacter, out ConsoleKeyInfo key, out int keyLength)
    {
        int unprocessedCharCount = endIndex - startIndex;

        // First process special control character codes.  These override anything from terminfo.
        if (unprocessedCharCount > 0)
        {
            // Is this an erase / backspace?
            char c = givenChars[startIndex];
            if (c != posixDisableValue && c == veraseCharacter)
            {
                key = new ConsoleKeyInfo(c, ConsoleKey.Backspace, shift: false, alt: false, control: false);
                keyLength = 1;
                return true;
            }
        }

        // Then process terminfo mappings.
        int minRange = terminalFormatStrings.MinKeyFormatLength;
        if (unprocessedCharCount >= minRange)
        {
            int maxRange = Math.Min(unprocessedCharCount, terminalFormatStrings.MaxKeyFormatLength);

            for (int i = maxRange; i >= minRange; i--)
            {
                var currentString = new ReadOnlyMemory<char>(givenChars, startIndex, i);

                // Check if the string prefix matches.
                if (terminalFormatStrings.KeyFormatToConsoleKey.TryGetValue(currentString, out key))
                {
                    keyLength = currentString.Length;
                    return true;
                }
            }
        }

        // Otherwise, not a known special console key.
        key = default(ConsoleKeyInfo);
        keyLength = 0;
        return false;
    }

    private static ConsoleKey GetKeyFromCharValue(char x, out bool isShift, out bool isCtrl)
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
                if (char.IsBetween(x, (char)1, (char)26))
                {
                    isCtrl = true;
                    return ConsoleKey.A + x - 1;
                }

                // 2. Numbers from 0 to 9.
                if (char.IsAsciiDigit(x))
                {
                    return ConsoleKey.D0 + x - '0';
                }

                //3. A to Z
                if (char.IsAsciiLetterUpper(x))
                {
                    isShift = true;
                    return ConsoleKey.A + (x - 'A');
                }

                // 4. a to z.
                if (char.IsAsciiLetterLower(x))
                {
                    return ConsoleKey.A + (x - 'a');
                }

                break;
        }

        return default(ConsoleKey);
    }
}
