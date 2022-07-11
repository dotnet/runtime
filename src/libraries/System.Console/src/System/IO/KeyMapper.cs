// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.IO;

internal static class KeyMapper
{
    private const char Escape = '\u001B';
    private const char Delete = '\u007F';
    private const char VTSeqenceEndTag = '~';
    private const char ModifierSeparator = ';';
    private const int MinimalSequenceLength = 3;
    private const int SequencePrefixLength = 2; // ^[[ ("^[" stands for Escape)

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
        out ConsoleKey key, out char character, out bool isShift, out bool isAlt, out bool isCtrl, ref int startIndex, int endIndex)
    {
        int length = endIndex - startIndex;

        // VERASE overrides anything from Terminfo
        if (buffer[startIndex] != posixDisableValue && buffer[startIndex] == veraseCharacter)
        {
            isShift = isAlt = isCtrl = false;
            character = buffer[startIndex++];
            key = ConsoleKey.Backspace;
            return;
        }

        // Escape Sequences start with Escape. But some terminals like PuTTY and rxvt use Escape to express that for given sequence Alt was pressed.
        if (length >= MinimalSequenceLength + 1 && buffer[startIndex] == Escape && buffer[startIndex + 1] == Escape)
        {
            startIndex++;
            if (TryParseTerminalInputSequence(buffer, terminalFormatStrings, out key, out character, out isShift, out _, out isCtrl, ref startIndex, endIndex))
            {
                isAlt = true;
                return;
            }
            else
            {
                startIndex--;
            }
        }
        else if (length >= MinimalSequenceLength && TryParseTerminalInputSequence(buffer, terminalFormatStrings, out key, out character, out isShift, out isAlt, out isCtrl, ref startIndex, endIndex))
        {
            return;
        }

        if (length == 2 && buffer[startIndex] == Escape && buffer[startIndex + 1] != Escape)
        {
            DecodeFromSingleChar(buffer[++startIndex], out key, out character, out isShift, out isCtrl);
            startIndex++;
            isAlt = key != default; // two char sequences starting with Escape are Alt+$Key
        }
        else
        {
            DecodeFromSingleChar(buffer[startIndex], out key, out character, out isShift, out isCtrl);
            startIndex++;
            isAlt = false;
        }
    }

    private static bool TryParseTerminalInputSequence(char[] buffer, ConsolePal.TerminalFormatStrings terminalFormatStrings,
        out ConsoleKey key, out char character, out bool isShift, out bool isAlt, out bool isCtrl, ref int startIndex, int endIndex)
    {
        ReadOnlySpan<char> input = buffer.AsSpan(startIndex, endIndex - startIndex);
        isShift = isAlt = isCtrl = false;
        character = default;
        key = default;

        // xterm and VT sequences start with "^[[", some xterm start with "^[O" ("^[" stands for Escape (27))
        if (input.Length < MinimalSequenceLength || input[0] != Escape || (input[1] != '[' && input[1] != 'O'))
        {
            return false;
        }

        // Is it a three character sequence? (examples: '^[[H' (Home), '^[OP' (F1), '^[Ow' (End))
        if (input[1] == 'O' || char.IsAsciiLetter(input[2]))
        {
            if (!TryMapUsingTerminfoDb(buffer.AsMemory(startIndex, MinimalSequenceLength), terminalFormatStrings, ref key, ref isShift, ref isAlt, ref isCtrl))
            {
                key = MapKeyId(input[2]); // fallback to well known mappings

                if (key == default)
                {
                    return false; // it was not a known sequence
                }
            }
            character = key == ConsoleKey.Enter ? '\r' : default; // "^[OM" should produce new line character (was not previously mapped this way)
            startIndex += MinimalSequenceLength;
            return true;
        }

        if (input.Length == MinimalSequenceLength)
        {
            return false;
        }

        // If sequence does not start with a letter, it must start with one or two digits that represent the Sequence Number
        int digitCount = !char.IsBetween(input[SequencePrefixLength], '1', '9') // not using IsAsciiDigit as 0 is invalid
            ? 0
            : char.IsDigit(input[SequencePrefixLength + 1]) ? 2 : 1;

        if (digitCount == 0 // it does not start with a digit, it's not a sequence
            || SequencePrefixLength + digitCount >= input.Length) // it's too short to be a complete sequence
        {
            return false;
        }

        if (input[SequencePrefixLength + digitCount] is VTSeqenceEndTag or '^' or '$' or '@') // it's a VT Sequence like ^[[11~
        {
            int sequenceLength = SequencePrefixLength + digitCount + 1;
            if (!TryMapUsingTerminfoDb(buffer.AsMemory(startIndex, sequenceLength), terminalFormatStrings, ref key, ref isShift, ref isAlt, ref isCtrl))
            {
                key = MapEscapeSequenceNumber(byte.Parse(input.Slice(SequencePrefixLength, digitCount)));

                if (key == default)
                {
                    return false; // it was not a known sequence
                }
            }

            if (input[SequencePrefixLength + digitCount] is '^' or '$' or '@') // rxvt modifiers
            {
                Apply(MapRxvtModifiers(input[SequencePrefixLength + digitCount]), ref isShift, ref isAlt, ref isCtrl);
            }

            startIndex += sequenceLength;
            return true;
        }

        // If Sequence Number is not followed by the VT Seqence End Tag,
        // it can be followed only by a Modifier Separator, Modifier (2-8) and Key ID or VT Seqence End Tag.
        if (input[SequencePrefixLength + digitCount] is not ModifierSeparator
            || SequencePrefixLength + digitCount + 2 >= input.Length
            || !char.IsBetween(input[SequencePrefixLength + digitCount + 1], '2', '8')
            || (!char.IsAsciiLetterUpper(input[SequencePrefixLength + digitCount + 2]) && input[SequencePrefixLength + digitCount + 2] is not VTSeqenceEndTag))
        {
            return false;
        }

        ConsoleModifiers modifiers = MapModifiers(input[SequencePrefixLength + digitCount + 1]);

        key = input[SequencePrefixLength + digitCount + 2] is VTSeqenceEndTag
            ? MapEscapeSequenceNumber(byte.Parse(input.Slice(SequencePrefixLength, digitCount)))
            : MapKeyId(input[SequencePrefixLength + digitCount + 2]);

        if (key != default)
        {
            startIndex += SequencePrefixLength + digitCount + 3; // 3 stands for separator, modifier and end tag or id
            Apply(modifiers, ref isShift, ref isAlt, ref isCtrl);
            return true;
        }

        return false;

        static bool TryMapUsingTerminfoDb(ReadOnlyMemory<char> inputSequence, ConsolePal.TerminalFormatStrings terminalFormatStrings,
            ref ConsoleKey key, ref bool isShift, ref bool isAlt, ref bool isCtrl)
        {
            // Check if the string prefix matches.
            if (terminalFormatStrings.KeyFormatToConsoleKey.TryGetValue(inputSequence, out ConsoleKeyInfo consoleKeyInfo))
            {
                key = consoleKeyInfo.Key;
                Apply(consoleKeyInfo.Modifiers, ref isShift, ref isAlt, ref isCtrl);
                return true;
            }
            return false;
        }

        // lowercase characters are used by rxvt
        static ConsoleKey MapKeyId(char single)
            => single switch
            {
                'A' or 'a' => ConsoleKey.UpArrow,
                'B' or 'b' => ConsoleKey.DownArrow,
                'C' or 'c' => ConsoleKey.RightArrow,
                'D' or 'd' => ConsoleKey.LeftArrow,
                'F' or 'w' => ConsoleKey.End,
                'H' => ConsoleKey.Home,
                'M' => ConsoleKey.Enter,
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

        static ConsoleModifiers MapRxvtModifiers(char modifier)
            => modifier switch
            {
                '^' => ConsoleModifiers.Control,
                '$' => ConsoleModifiers.Shift,
                '@' => ConsoleModifiers.Control | ConsoleModifiers.Shift,
                _ => default
            };

        static void Apply(ConsoleModifiers modifiers, ref bool isShift, ref bool isAlt, ref bool isCtrl)
        {
            isShift = (modifiers & ConsoleModifiers.Shift) != 0;
            isAlt = (modifiers & ConsoleModifiers.Alt) != 0;
            isCtrl = (modifiers & ConsoleModifiers.Control) != 0;
        }
    }

    private static void DecodeFromSingleChar(char single, out ConsoleKey key, out char ch, out bool isShift, out bool isCtrl)
    {
        isShift = isCtrl = false;
        ch = single;

        key = single switch
        {
            '\b' => ConsoleKey.Backspace,
            '\t' => ConsoleKey.Tab,
            '\r' or '\n' => ConsoleKey.Enter,
            ' ' => ConsoleKey.Spacebar,
            Escape => ConsoleKey.Escape, // Ctrl+[ and Ctrl+3 are also mapped to 27. Limitation: Ctrl+[ and Ctrl+3 can't be mapped.
            Delete => ConsoleKey.Backspace, // Ctrl+8 and Backspace are mapped to 127 (ASCII Delete key). Limitation: Ctrl+8 can't be mapped.
            '*' => ConsoleKey.Multiply, // We can't distinguish Dx+Shift and Multiply (Numeric Keypad). Limitation: Shift+Dx can't be mapped.
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
            '\u0000' => ControlAndDigitPressed(single, out ch, out isCtrl),
            _ => default
        };

        if (single is '\b' or '\n')
        {
            isCtrl = true; // Ctrl+Backspace is mapped to '\b' (8), Ctrl+Enter to '\n' (10)
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
            // Ctrl+H is mapped to 8, which also maps to Ctrl+Backspace.
            // Ctrl+I is mapped to 9, which also maps to Tab. Tab (9) is more likely to be pressed.
            // Ctrl+J is mapped to 10, which also maps to Ctrl+Enter ('\n').
            // Ctrl+M is mapped to 13, which also maps to Enter ('\r').
            // Limitation: Ctrl+H, Ctrl+I, Ctrl+J and Crl+M can't be mapped. More: https://unix.stackexchange.com/questions/563469/conflict-ctrl-i-with-tab-in-normal-mode
            Debug.Assert(single != 'b' && single != '\t' && single != '\n' && single != '\r');

            isCtrl = true;
            ch = default; // we could use the letter here, but it's impossible to distinguish upper vs lowercase (and Windows doesn't do it as well)
            return ConsoleKey.A + single - 1;
        }

        static ConsoleKey ControlAndDigitPressed(char single, out char ch, out bool isCtrl)
        {
            // Ctrl+(D3-D7) characters are mapped to values from 27 to 31. Escape is also mapped to 27.
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
