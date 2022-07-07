// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.IO;

internal static class KeyMapper
{
    private const char Escape = '\u001B';
    private const char Delete = '\u007F';

    internal static bool MapBufferToConsoleKey(char[] buffer, ConsolePal.TerminalFormatStrings terminalFormatStrings, byte posixDisableValue, byte veraseCharacter,
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

    internal static void MapNew(char[] buffer, ConsolePal.TerminalFormatStrings terminalFormatStrings, byte posixDisableValue, byte veraseCharacter,
        out ConsoleKey key, out char ch, out bool isShift, out bool isAlt, out bool isCtrl, ref int startIndex, int endIndex)
    {
        if (endIndex - startIndex == 1)
        {
            DecodeFromSingleChar(buffer[startIndex], out key, out ch, out isShift, out isCtrl);
            startIndex++;
            isAlt = false;
        }
        else if (endIndex - startIndex == 2 && buffer[startIndex] == Escape)
        {
            DecodeFromSingleChar(buffer[++startIndex], out key, out ch, out isShift, out isCtrl);
            startIndex++;
            isAlt = key != default; // two char sequences starting with Escape are Alt+$Key
        }
        else
        {
            key = default;
            ch = default;
            isShift = isAlt = isCtrl = false;
        }
    }

    private static void DecodeFromSingleChar(char single, out ConsoleKey key, out char ch, out bool isShift, out bool isCtrl)
    {
        isShift = isCtrl = false;
        ch = single;

        key = single switch
        {
            // '\b' is not mapped to ConsoleKey.Backspace on purpose, as it's simply wrong mapping
            '\t' => ConsoleKey.Tab,
            '\r' or '\n' => ConsoleKey.Enter,
            ' ' => ConsoleKey.Spacebar,
            Escape => ConsoleKey.Escape, // Ctrl+[ and Ctrl+3 are also mapped to 27, but Escape is more likely to be pressed. Limitation: Ctrl+[ and Ctrl+3 can't be mapped.
            Delete => ConsoleKey.Backspace, // Ctrl+8 and Backspace are mapped to 127, but Backspace is more likely to be pressed. Limitation: Ctrl+8 can't be mapped.
            '*' => ConsoleKey.Multiply, // We can't distinguish D8+Shift and Multiply (Numeric Keypad). Limitation: Shift+D8 can't be mapped.
            '/' => ConsoleKey.Divide, // We can't distinguish OemX and Divide (Numeric Keypad). Limitation: OemX keys can't be mapped.
            '-' => ConsoleKey.Subtract, // We can't distinguish OemMinus and Subtract (Numeric Keypad). Limitation: OemMinus can't be mapped.
            '+' => ConsoleKey.Add, // We can't distinguish OemPlus and Add (Numeric Keypad). Limitation: OemPlus can't be mapped.
            '=' => default, // '+' is not mapped to OemPlus, so `=` is not mapped to Shift+OemPlus. Limitation: Shift+OemPlus can't be mapped.
            '!' or '@' or  '#' or '$' or '%' or '^' or '&' or '&' or '*' or '(' or ')' => default, // We can't make assumptions about keyboard layout neither read it. Limitation: Shift+Dx keys can't be mapped.
            ',' => ConsoleKey.OemComma, // was not previously mapped this way
            '.' => ConsoleKey.OemPeriod, // was not previously mapped this way
            _ when char.IsAsciiLetterLower(single) => ConsoleKey.A + single - 'a',
            _ when char.IsAsciiLetterUpper(single) => UppercaseCharacter(single, out isShift),
            _ when char.IsAsciiDigit(single) => ConsoleKey.D0 + single - '0', // We can't distinguish DX and Ctrl+DX as they produce same values. Limitation: Ctrl+DX can't be mapped.
            _ when char.IsBetween(single, (char)1, (char)26) => ControlAndLetterPressed(single, out ch, out isCtrl),
            _ when char.IsBetween(single, (char)28, (char)31) => ControlAndDigitPressed(single, out ch, out isCtrl),
            '\u0000' or Delete => ControlAndDigitPressed(single, out ch, out isCtrl),
            _ => default
        };

        // above we map ASCII Delete character to Backspace key, we need to map the char too
        if (key == ConsoleKey.Backspace)
        {
            ch = '\b';
        }

        static ConsoleKey UppercaseCharacter(char single, out bool isShift)
        {
            // Previous implementation assumed that all uppercase characters were typed using Shift.
            // Limitation: Caps Lock+(a-z) is always mapped to Shift+(a-z).
            isShift = true;
            return ConsoleKey.A + single - 'A';
        }

        static ConsoleKey ControlAndLetterPressed(char single, out char ch, out bool isCtrl)
        {
            // Ctrl+(a-z) characters are mapped to values from 1 to 26.
            // Ctrl+h is mapped to 8, which also maps to Ctrl+Backspace. Ctrl+h is more likely to be pressed. (TODO: discuss with others)
            // Ctrl+i is mapped to 9, which also maps to Tab. Tab (9) is more likely to be pressed.
            // Ctrl+j is mapped to 10, which also maps to Enter ('\n') and Ctrl+Enter. Enter is more likely to be pressed.
            // Ctrl+m is mapped to 13, which also maps to Enter ('\r'). Enter (13) is more likely to be pressed.
            // Limitation: Ctrl+i, Ctrl+j, Crl+m, Ctrl+Backspace and Ctrl+Enter can't be mapped. More: https://unix.stackexchange.com/questions/563469/conflict-ctrl-i-with-tab-in-normal-mode
            Debug.Assert(single != '\t' && single != '\n' && single != '\r');

            isCtrl = true;
            ch = default; // we could use the letter here, but it's impossible to distinguish upper vs lowercase (and Windows doesn't do it as well)
            return ConsoleKey.A + single - 1;
        }

        static ConsoleKey ControlAndDigitPressed(char single, out char ch, out bool isCtrl)
        {
            // Ctrl+(D3-D7) characters are mapped to values from 27 to 31. Escape (27) is more likely to be pressed.
            // Limitation: Ctrl+(D1, D3, D8, D9 and D0) can't be mapped.
            Debug.Assert(single == default || char.IsBetween(single, (char)28, (char)31));

            isCtrl = true;
            ch = default; // consistent with Windows
            return single switch
            {
                '\u0000' => ConsoleKey.D2, // This is what PuTTY does (was not previously mapped this way)
                _ => ConsoleKey.D4 + single - 28
            };
        }
    }

    private static bool TryGetSpecialConsoleKey(char[] givenChars, int startIndex, int endIndex,
        ConsolePal.TerminalFormatStrings terminalFormatStrings, byte posixDisableValue, byte veraseCharacter,
        out ConsoleKeyInfo key, out int keyLength)
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
