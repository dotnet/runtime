// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.IO;

internal static class KeyParser
{
    private const char Escape = '\u001B';
    private const char Delete = '\u007F';
    private const char VtSequenceEndTag = '~';
    private const char ModifierSeparator = ';';
    private const int MinimalSequenceLength = 3;
    private const int SequencePrefixLength = 2; // ^[[ ("^[" stands for Escape)

    internal static void Parse(char[] buffer, ConsolePal.TerminalFormatStrings terminalFormatStrings, byte posixDisableValue, byte veraseCharacter,
        out ConsoleKey key, out char character, out bool isShift, out bool isAlt, out bool isCtrl, ref int startIndex, int endIndex)
    {
        int length = endIndex - startIndex;
        Debug.Assert(length > 0);

        // VERASE overrides anything from Terminfo. Both settings can be different for Linux and macOS.
        if (buffer[startIndex] != posixDisableValue && buffer[startIndex] == veraseCharacter)
        {
            isShift = isAlt = isCtrl = false;
            character = buffer[startIndex++]; // the original char is preserved on purpose (backward compat + consistency)
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
            startIndex--;
        }
        else if (length >= MinimalSequenceLength && TryParseTerminalInputSequence(buffer, terminalFormatStrings, out key, out character, out isShift, out isAlt, out isCtrl, ref startIndex, endIndex))
        {
            return;
        }

        if (length == 2 && buffer[startIndex] == Escape && buffer[startIndex + 1] != Escape)
        {
            ParseFromSingleChar(buffer[++startIndex], out key, out character, out isShift, out isCtrl);
            startIndex++;
            isAlt = key != default; // two char sequences starting with Escape are Alt+$Key
        }
        else
        {
            ParseFromSingleChar(buffer[startIndex], out key, out character, out isShift, out isCtrl);
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

        // sequences start with either "^[[" or "^[O". "^[" stands for Escape (27).
        if (input.Length < MinimalSequenceLength || input[0] != Escape || (input[1] != '[' && input[1] != 'O'))
        {
            return false;
        }

        // Is it a three character sequence? (examples: '^[[H' (Home), '^[OP' (F1))
        if (input[1] == 'O' || char.IsAsciiLetter(input[2]) || input.Length == MinimalSequenceLength)
        {
            if (!TryMapUsingTerminfoDb(buffer.AsMemory(startIndex, MinimalSequenceLength), terminalFormatStrings, ref key, ref isShift, ref isAlt, ref isCtrl))
            {
                // All terminals which use "^[O{letter}" escape sequences don't define conflicting mappings.
                // Example: ^[OH either means Home or simply is not used by given terminal.
                // But with "^[[{character}" sequences, there are conflicts between rxvt and SCO.
                // Example: "^[[a" is Shift+UpArrow for rxvt and Shift+F3 for SCO.
                if (input[1] == 'O'|| terminalFormatStrings.IsRxvtTerm)
                {
                    key = MapKeyIdOXterm(input[2]); // fallback to well known mappings

                    if (key != default)
                    {
                        // lowercase characters are used by rxvt to express Shift modifier for the arrow keys
                        isShift = char.IsBetween(input[2], 'a', 'd');
                    }
                }
                else
                {
                    (key, ConsoleModifiers mod) = MapSCO(input[2]); // fallback to well known mappings

                    if (key != default)
                    {
                        Apply(mod, ref isShift, ref isAlt, ref isCtrl);
                    }
                }

                if (key == default)
                {
                    return false; // it was not a known sequence
                }
            }
            character = key == ConsoleKey.Enter ? '\r' : default; // "^[OM" should produce new line character (was not previously mapped this way)
            startIndex += MinimalSequenceLength;
            return true;
        }

        // Is it a four character sequence used by Linux Console or PuTTy configured to emulate it? (examples: '^[[[A' (F1), '^[[[B' (F2))
        if (input[1] == '[' && input[2] == '[' && char.IsBetween(input[3], 'A', 'E'))
        {
            if (!TryMapUsingTerminfoDb(buffer.AsMemory(startIndex, 4), terminalFormatStrings, ref key, ref isShift, ref isAlt, ref isCtrl))
            {
                key = ConsoleKey.F1 + input[3] - 'A';
            }
            startIndex += 4;
            return true;
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

        if (input[SequencePrefixLength + digitCount] is VtSequenceEndTag or '^' or '$' or '@') // it's a VT Sequence like ^[[11~ or rxvt like ^[[11^
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
        // it can be followed only by a Modifier Separator, Modifier (2-8) and Key ID or VT Sequence End Tag.
        if (input[SequencePrefixLength + digitCount] is not ModifierSeparator
            || SequencePrefixLength + digitCount + 2 >= input.Length
            || !char.IsBetween(input[SequencePrefixLength + digitCount + 1], '2', '8')
            || (!char.IsAsciiLetterUpper(input[SequencePrefixLength + digitCount + 2]) && input[SequencePrefixLength + digitCount + 2] is not VtSequenceEndTag))
        {
            return false;
        }

        ConsoleModifiers modifiers = MapXtermModifiers(input[SequencePrefixLength + digitCount + 1]);

        key = input[SequencePrefixLength + digitCount + 2] is VtSequenceEndTag
            ? MapEscapeSequenceNumber(byte.Parse(input.Slice(SequencePrefixLength, digitCount)))
            : MapKeyIdOXterm(input[SequencePrefixLength + digitCount + 2]);

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

        // maps "^[O{character}" for all Terminals and "^[[{character}" for rxvt Terminals
        static ConsoleKey MapKeyIdOXterm(char character)
            => character switch
            {
                'A' or 'a' => ConsoleKey.UpArrow,
                'B' or 'b' => ConsoleKey.DownArrow,
                'C' or 'c' => ConsoleKey.RightArrow,
                'D' or 'd' => ConsoleKey.LeftArrow,
                'F' or 'w' => ConsoleKey.End,
                'H' => ConsoleKey.Home,
                'P' => ConsoleKey.F1,
                'Q' => ConsoleKey.F2,
                'R' => ConsoleKey.F3,
                'S' => ConsoleKey.F4,
                'T' => ConsoleKey.F5, // VT 100+
                'U' => ConsoleKey.F6, // VT 100+
                'V' => ConsoleKey.F7, // VT 100+
                'W' => ConsoleKey.F8, // VT 100+
                'X' => ConsoleKey.F9, // VT 100+
                'Y' => ConsoleKey.F10, // VT 100+
                'Z' => ConsoleKey.F11, // VT 100+
                '[' => ConsoleKey.F12, // VT 100+
                _ => default
            };

        // maps "^[[{character}" for SCO terminals, based on https://vt100.net/docs/vt510-rm/chapter6.html
        static (ConsoleKey key, ConsoleModifiers modifiers) MapSCO(char character)
            => character switch
            {
                'H' => (ConsoleKey.Home, 0),
                _ when char.IsBetween(character, 'M', 'X') => (ConsoleKey.F1 + character - 'M', 0),
                _ when char.IsBetween(character, 'Y', 'Z') => (ConsoleKey.F1 + character - 'Y', ConsoleModifiers.Shift),
                _ when char.IsBetween(character, 'a', 'j') => (ConsoleKey.F3 + character - 'a', ConsoleModifiers.Shift),
                _ when char.IsBetween(character, 'k', 'v') => (ConsoleKey.F1 + character - 'k', ConsoleModifiers.Control),
                _ when char.IsBetween(character, 'w', 'z') => (ConsoleKey.F1 + character - 'w', ConsoleModifiers.Control | ConsoleModifiers.Shift),
                '@' => (ConsoleKey.F5, ConsoleModifiers.Control | ConsoleModifiers.Shift),
                '[' => (ConsoleKey.F6, ConsoleModifiers.Control | ConsoleModifiers.Shift),
                '<' or '\\' => (ConsoleKey.F7, ConsoleModifiers.Control | ConsoleModifiers.Shift), // the Spec says <, PuTTy uses \.
                ']' => (ConsoleKey.F8, ConsoleModifiers.Control | ConsoleModifiers.Shift),
                '^' => (ConsoleKey.F9, ConsoleModifiers.Control | ConsoleModifiers.Shift),
                '_' => (ConsoleKey.F10, ConsoleModifiers.Control | ConsoleModifiers.Shift),
                '`' => (ConsoleKey.F11, ConsoleModifiers.Control | ConsoleModifiers.Shift),
                '{' => (ConsoleKey.F12, ConsoleModifiers.Control | ConsoleModifiers.Shift),
                _ => default
            };

        // based on https://en.wikipedia.org/wiki/ANSI_escape_code#Fe_Escape_sequences
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
                // 9, 16, 22, 27, 30 and 35 have no mapping
                _ => default
            };

        // based on https://www.xfree86.org/current/ctlseqs.html
        static ConsoleModifiers MapXtermModifiers(char modifier)
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

    private static void ParseFromSingleChar(char single, out ConsoleKey key, out char ch, out bool isShift, out bool isCtrl)
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
            // Ctrl+I is mapped to 9, which also maps to Tab.
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
}
