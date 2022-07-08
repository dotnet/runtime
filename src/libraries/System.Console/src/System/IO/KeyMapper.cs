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
        int length = endIndex - startIndex;

        // TODO: add VERASE handling

        // Escape sequences start with Escape. But some terminals (e.g. PuTTY) use Escape to express that for given sequence Alt was pressed.
        if (length >= 4 && buffer[startIndex] == Escape && buffer[startIndex + 1] == Escape)
        {
            startIndex++;
            if (TryDecodeTerminalInputSequence(buffer, terminalFormatStrings, out key, out isShift, out _, out isCtrl, ref startIndex, endIndex))
            {
                isAlt = true;
                ch = default; // these special keys never produce any char (Home, Arrow, F1 etc)
                return;
            }
            else
            {
                startIndex--;
            }
        }
        else if (length >= 3 && TryDecodeTerminalInputSequence(buffer, terminalFormatStrings, out key, out isShift, out isAlt, out isCtrl, ref startIndex, endIndex))
        {
            // these special keys never produce any char (Home, Arrow, F1 etc)
            ch = default;
            return;
        }

        if (length == 2 && buffer[startIndex] == Escape)
        {
            DecodeFromSingleChar(buffer[++startIndex], out key, out ch, out isShift, out isCtrl);
            startIndex++;
            isAlt = key != default; // two char sequences starting with Escape are Alt+$Key
        }
        else
        {
            DecodeFromSingleChar(buffer[startIndex], out key, out ch, out isShift, out isCtrl);
            startIndex++;
            isAlt = false;
        }
    }

    private static bool TryDecodeTerminalInputSequence(char[] buffer, ConsolePal.TerminalFormatStrings terminalFormatStrings,
        out ConsoleKey key, out bool isShift, out bool isAlt, out bool isCtrl, ref int startIndex, int endIndex)
    {
        ReadOnlySpan<char> input = buffer.AsSpan(startIndex, endIndex - startIndex);
        isShift = isAlt = isCtrl = false;
        key = default;

        // xterm and VT sequences start with "^[[", some xterm start with "^[O" ("^[" stands for Escape (27))
        if (input.Length < 3 || input[0] != Escape || (input[1] != '[' && input[1] != 'O'))
        {
            return false;
        }

        if (input[1] == 'O' || char.IsAsciiLetterUpper(input[2])) // "^[O" or "^[["
        {
            if (!TryMapUsingDatabase(buffer.AsMemory(startIndex, 3), terminalFormatStrings, ref key, ref isShift, ref isAlt, ref isCtrl))
            {
                key = Map(input[2]);
                Debug.Assert(key != default, $"Missing '{input.Slice(0, 3)}' mapping");
            }
            startIndex += 3;
            return true;
        }

        int digitCount = 0;
        ReadOnlySpan<char> unparsed = input.Slice(2);
        while (!unparsed.IsEmpty && char.IsAsciiDigit(unparsed[0]))
        {
            digitCount++;
            unparsed = unparsed.Slice(1);
        }

        if (digitCount == 0)
        {
            return false;
        }

        if (unparsed[0] == '~') // it's a VT Sequence like ^[[11~
        {
            int sequenceLength = 2 + digitCount + 1; // prefix + digit count + ~
            if (!TryMapUsingDatabase(buffer.AsMemory(startIndex, sequenceLength), terminalFormatStrings, ref key, ref isShift, ref isAlt, ref isCtrl))
            {
                key = MapEscapeSequenceNumber(byte.Parse(input.Slice(2, digitCount)));
                Debug.Assert(key != default, $"Missing '{input.Slice(0, sequenceLength)}' mapping");
            }
            startIndex += sequenceLength;
            return true;
        }

        if (unparsed[0] != ';' || unparsed.Length < 2 || !char.IsDigit(unparsed[1]) || !(unparsed[2] == '~' || char.IsAsciiLetterUpper(unparsed[2])))
        {
            return false;
        }

        // after ; comes the modifiers:
        ConsoleModifiers modifiers = MapModifiers(unparsed[1]);
        if (char.IsAsciiLetterUpper(unparsed[2]))
        {
            // after the modifiers it's either a letter (key id)
            key = Map(unparsed[2]);
        }
        else
        {
            // or a tylde and the whole thing is a VT Sequence like ^[[24;5~
            Debug.Assert(unparsed[2] == '~');
            int sequenceLength = 2 + digitCount + 1; // prefix + digit
            key = MapEscapeSequenceNumber(byte.Parse(input.Slice(2, digitCount)));
            Debug.Assert(key != default, $"Missing '{input.Slice(0, sequenceLength)}' mapping");
        }

        if (key != default)
        {
            startIndex += 2 + digitCount + 1 + 1; // prefix + digit count + modifier + ~ or single char
            isShift = (modifiers & ConsoleModifiers.Shift) != 0;
            isAlt = (modifiers & ConsoleModifiers.Alt) != 0;
            isCtrl = (modifiers & ConsoleModifiers.Control) != 0;
            return true;
        }

        return false;

        static bool TryMapUsingDatabase(ReadOnlyMemory<char> inputSequence, ConsolePal.TerminalFormatStrings terminalFormatStrings,
            ref ConsoleKey key, ref bool isShift, ref bool isAlt, ref bool isCtrl)
        {
            // Check if the string prefix matches.
            if (terminalFormatStrings.KeyFormatToConsoleKey.TryGetValue(inputSequence, out ConsoleKeyInfo consoleKeyInfo))
            {
                key = consoleKeyInfo.Key;
                isShift = (consoleKeyInfo.Modifiers & ConsoleModifiers.Shift) != 0;
                isAlt = (consoleKeyInfo.Modifiers & ConsoleModifiers.Alt) != 0;
                isCtrl = (consoleKeyInfo.Modifiers & ConsoleModifiers.Control) != 0;
                return true;
            }
            return false;
        }

        static ConsoleKey Map(char single)
            => single switch
            {
                'A' => ConsoleKey.UpArrow,
                'B' => ConsoleKey.DownArrow,
                'C' => ConsoleKey.RightArrow,
                'D' => ConsoleKey.LeftArrow,
                'F' or 'w' => ConsoleKey.End, // 'w' can be used by rxvt
                'H' => ConsoleKey.Home,
                'P' => ConsoleKey.F1,
                'Q' => ConsoleKey.F2,
                'R' => ConsoleKey.F3,
                'S' => ConsoleKey.F4,
                _ => default
            };

        static ConsoleKey MapEscapeSequenceNumber(byte number)
            => number switch
            {
                1 or 7 => ConsoleKey.Home,
                2 => ConsoleKey.Insert,
                3 => ConsoleKey.Delete,
                4 or 8 => ConsoleKey.End,
                5 => ConsoleKey.PageUp,
                6 => ConsoleKey.PageDown,
                // Limitation: 10 is mapped to F0, ConsoleKey does not define it so it's not supported.
                11 => ConsoleKey.F1,
                12 => ConsoleKey.F2,
                13 => ConsoleKey.F3,
                14 => ConsoleKey.F4,
                15 => ConsoleKey.F5,
                17 => ConsoleKey.F6,
                18 => ConsoleKey.F7,
                19 => ConsoleKey.F8,
                20 => ConsoleKey.F9,
                21 => ConsoleKey.F10,
                23 => ConsoleKey.F11,
                24 => ConsoleKey.F12,
                25 => ConsoleKey.F13,
                26 => ConsoleKey.F14,
                28 => ConsoleKey.F15,
                29 => ConsoleKey.F16,
                31 => ConsoleKey.F17,
                32 => ConsoleKey.F18,
                33 => ConsoleKey.F19,
                34 => ConsoleKey.F20,
                // 9, 16, 22, 27, 30 and 35 have no mapping (https://en.wikipedia.org/wiki/ANSI_escape_code#Fe_Escape_sequences)
                _ => default
            };

        static ConsoleModifiers MapModifiers(char modifier)
            => modifier switch
            {
                '2' => ConsoleModifiers.Shift,
                '3' => ConsoleModifiers.Alt,
                '4' => ConsoleModifiers.Shift | ConsoleModifiers.Alt,
                '5' => ConsoleModifiers.Control,
                '6' => ConsoleModifiers.Shift | ConsoleModifiers.Control,
                '7' => ConsoleModifiers.Alt | ConsoleModifiers.Control,
                '8' => ConsoleModifiers.Shift | ConsoleModifiers.Alt | ConsoleModifiers.Control,
                _ => default
            };
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
            '!' or '@' or '#' or '$' or '%' or '^' or '&' or '&' or '*' or '(' or ')' => default, // We can't make assumptions about keyboard layout neither read it. Limitation: Shift+Dx keys can't be mapped.
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
            // Ctrl+h is mapped to 8, which also maps to Ctrl+Backspace. Ctrl+h is more likely to be pressed. (TODO: change it)
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
                '\u0000' => ConsoleKey.D2, // was not previously mapped this way
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
