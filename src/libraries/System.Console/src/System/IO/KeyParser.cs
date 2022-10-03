// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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

    internal static ConsoleKeyInfo Parse(char[] buffer, TerminalFormatStrings terminalFormatStrings, byte posixDisableValue, byte veraseCharacter, ref int startIndex, int endIndex)
    {
        int length = endIndex - startIndex;
        Debug.Assert(length > 0);

        // VERASE overrides anything from Terminfo. Both settings can be different for Linux and macOS.
        if (buffer[startIndex] != posixDisableValue && buffer[startIndex] == veraseCharacter)
        {
            // the original char is preserved on purpose (backward compat + consistency)
            return new ConsoleKeyInfo(buffer[startIndex++], ConsoleKey.Backspace, false, false, false);
        }

        // Escape Sequences start with Escape. But some terminals like PuTTY and rxvt prepend Escape to express that for given sequence Alt was pressed.
        if (length >= MinimalSequenceLength + 1 && buffer[startIndex] == Escape && buffer[startIndex + 1] == Escape)
        {
            startIndex++;
            if (TryParseTerminalInputSequence(buffer, terminalFormatStrings, out ConsoleKeyInfo parsed, ref startIndex, endIndex))
            {
                return new ConsoleKeyInfo(parsed.KeyChar, parsed.Key, (parsed.Modifiers & ConsoleModifiers.Shift) != 0, alt: true, (parsed.Modifiers & ConsoleModifiers.Control) != 0);
            }
            startIndex--;
        }
        else if (length >= MinimalSequenceLength && TryParseTerminalInputSequence(buffer, terminalFormatStrings, out ConsoleKeyInfo parsed, ref startIndex, endIndex))
        {
            return parsed;
        }

        if (length == 2 && buffer[startIndex] == Escape && buffer[startIndex + 1] != Escape)
        {
            startIndex++; // skip the Escape
            return ParseFromSingleChar(buffer[startIndex++], isAlt: true);
        }

        return ParseFromSingleChar(buffer[startIndex++], isAlt: false);
    }

    private static bool TryParseTerminalInputSequence(char[] buffer, TerminalFormatStrings terminalFormatStrings, out ConsoleKeyInfo parsed, ref int startIndex, int endIndex)
    {
        ReadOnlySpan<char> input = buffer.AsSpan(startIndex, endIndex - startIndex);
        parsed = default;

        // sequences start with either "^[[" or "^[O". "^[" stands for Escape (27).
        if (input.Length < MinimalSequenceLength || input[0] != Escape || (input[1] != '[' && input[1] != 'O'))
        {
            return false;
        }

        Dictionary<ReadOnlyMemory<char>, ConsoleKeyInfo> terminfoDb = terminalFormatStrings.KeyFormatToConsoleKey; // the most important source of truth
        ConsoleModifiers modifiers = 0;
        ConsoleKey key;

        // Is it a three character sequence? (examples: '^[[H' (Home), '^[OP' (F1))
        if (input[1] == 'O' || char.IsAsciiLetter(input[2]) || input.Length == MinimalSequenceLength)
        {
            if (!terminfoDb.TryGetValue(buffer.AsMemory(startIndex, MinimalSequenceLength), out parsed))
            {
                // All terminals which use "^[O{letter}" escape sequences don't define conflicting mappings.
                // Example: ^[OH either means Home or simply is not used by given terminal.
                // But with "^[[{character}" sequences, there are conflicts between rxvt and SCO.
                // Example: "^[[a" is Shift+UpArrow for rxvt and Shift+F3 for SCO.
                (key, modifiers) = input[1] == 'O' || terminalFormatStrings.IsRxvtTerm
                    ? MapKeyIdOXterm(input[2], terminalFormatStrings.IsRxvtTerm)
                    : MapSCO(input[2]);

                if (key == default)
                {
                    return false; // it was not a known sequence
                }

                char keyChar = key switch
                {
                    ConsoleKey.Enter => '\r', // "^[OM" should produce new line character (was not previously mapped this way)
                    ConsoleKey.Add => '+',
                    ConsoleKey.Subtract => '-',
                    ConsoleKey.Divide => '/',
                    ConsoleKey.Multiply => '*',
                    _ => default
                };
                parsed = Create(keyChar, key, modifiers);
            }

            startIndex += MinimalSequenceLength;
            return true;
        }

        // Is it a four character sequence used by Linux Console or PuTTy configured to emulate it? (examples: '^[[[A' (F1), '^[[[B' (F2))
        if (input[1] == '[' && input[2] == '[' && char.IsBetween(input[3], 'A', 'E'))
        {
            if (!terminfoDb.TryGetValue(buffer.AsMemory(startIndex, 4), out parsed))
            {
                parsed = new ConsoleKeyInfo(default, ConsoleKey.F1 + input[3] - 'A', false, false, false);
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
            parsed = default;
            return false;
        }

        if (IsSequenceEndTag(input[SequencePrefixLength + digitCount]))
        {
            // it's a VT Sequence like ^[[11~ or rxvt like ^[[11^
            int sequenceLength = SequencePrefixLength + digitCount + 1;
            if (!terminfoDb.TryGetValue(buffer.AsMemory(startIndex, sequenceLength), out parsed))
            {
                key = MapEscapeSequenceNumber(byte.Parse(input.Slice(SequencePrefixLength, digitCount)));
                if (key == default)
                {
                    return false; // it was not a known sequence
                }

                if (IsRxvtModifier(input[SequencePrefixLength + digitCount]))
                {
                    modifiers = MapRxvtModifiers(input[SequencePrefixLength + digitCount]);
                }

                parsed = Create(default, key, modifiers);
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

        modifiers = MapXtermModifiers(input[SequencePrefixLength + digitCount + 1]);

        key = input[SequencePrefixLength + digitCount + 2] is VtSequenceEndTag
            ? MapEscapeSequenceNumber(byte.Parse(input.Slice(SequencePrefixLength, digitCount)))
            : MapKeyIdOXterm(input[SequencePrefixLength + digitCount + 2], terminalFormatStrings.IsRxvtTerm).key;

        if (key == default)
        {
            return false;
        }

        startIndex += SequencePrefixLength + digitCount + 3; // 3 stands for separator, modifier and end tag or id
        parsed = Create(default, key, modifiers);
        return true;

        // maps "^[O{character}" for all Terminals and "^[[{character}" for rxvt Terminals
        static (ConsoleKey key, ConsoleModifiers modifiers) MapKeyIdOXterm(char character, bool isRxvt)
            => character switch
            {
                'A' or 'x' => (ConsoleKey.UpArrow, 0), // lowercase used by rxvt
                'a' => (ConsoleKey.UpArrow, ConsoleModifiers.Shift), // rxvt
                'B' or 'r' => (ConsoleKey.DownArrow, 0), // lowercase used by rxv
                'b' => (ConsoleKey.DownArrow, ConsoleModifiers.Shift), // used by rxvt
                'C' or 'v' => (ConsoleKey.RightArrow, 0), // lowercase used by rxv
                'c' => (ConsoleKey.RightArrow, ConsoleModifiers.Shift), // used by rxvt
                'D' or 't' => (ConsoleKey.LeftArrow, 0), // lowercase used by rxv
                'd' => (ConsoleKey.LeftArrow, ConsoleModifiers.Shift), // used by rxvt
                'E' => (ConsoleKey.NoName, 0), // ^[OE maps to Begin, but we don't have such Key. To reproduce press Num5.
                'F' or 'q' => (ConsoleKey.End, 0),
                'H' => (ConsoleKey.Home, 0),
                'j' => (ConsoleKey.Multiply, 0), // used by both xterm and rxvt
                'k' => (ConsoleKey.Add, 0), // used by both xterm and rxvt
                'm' => (ConsoleKey.Subtract, 0), // used by both xterm and rxvt
                'M' => (ConsoleKey.Enter, 0), // used by xterm, rxvt (they have it Terminfo) and tmux (no record in Terminfo)
                'n' => (ConsoleKey.Delete, 0), // rxvt
                'o' => (ConsoleKey.Divide, 0), // used by both xterm and rxvt
                'P' => (ConsoleKey.F1, 0),
                'p' => (ConsoleKey.Insert, 0), // rxvt
                'Q' => (ConsoleKey.F2, 0),
                'R' => (ConsoleKey.F3, 0),
                'S' => (ConsoleKey.F4, 0),
                's' => (ConsoleKey.PageDown, 0), // rxvt
                'T' => (ConsoleKey.F5, 0), // VT 100+
                'U' => (ConsoleKey.F6, 0), // VT 100+
                'u' => (ConsoleKey.NoName, 0), // it should be Begin, but we don't have such (press Num5 in rxvt to reproduce)
                'V' => (ConsoleKey.F7, 0), // VT 100+
                'W' => (ConsoleKey.F8, 0), // VT 100+
                'w' when isRxvt => (ConsoleKey.Home, 0),
                'w' when !isRxvt => (ConsoleKey.End, 0),
                'X' => (ConsoleKey.F9, 0), // VT 100+
                'Y' => (ConsoleKey.F10, 0), // VT 100+
                'y' => (ConsoleKey.PageUp, 0), // rxvt
                'Z' => (ConsoleKey.F11, 0), // VT 100+
                '[' => (ConsoleKey.F12, 0), // VT 100+
                _ => default
            };

        // maps "^[[{character}" for SCO terminals, based on https://vt100.net/docs/vt510-rm/chapter6.html
        static (ConsoleKey key, ConsoleModifiers modifiers) MapSCO(char character)
            => character switch
            {
                'A' => (ConsoleKey.UpArrow, 0),
                'B' => (ConsoleKey.DownArrow, 0),
                'C' => (ConsoleKey.RightArrow, 0),
                'D' => (ConsoleKey.LeftArrow, 0),
                'F' => (ConsoleKey.End, 0),
                'G' => (ConsoleKey.PageDown, 0),
                'H' => (ConsoleKey.Home, 0),
                'I' => (ConsoleKey.PageUp, 0),
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

        static bool IsSequenceEndTag(char character) => character is VtSequenceEndTag || IsRxvtModifier(character);

        static bool IsRxvtModifier(char character) => MapRxvtModifiers(character) != default;

        static ConsoleModifiers MapRxvtModifiers(char modifier)
            => modifier switch
            {
                '^' => ConsoleModifiers.Control,
                '$' => ConsoleModifiers.Shift,
                '@' => ConsoleModifiers.Control | ConsoleModifiers.Shift,
                _ => default
            };

        static ConsoleKeyInfo Create(char keyChar, ConsoleKey key, ConsoleModifiers modifiers)
            => new(keyChar, key, (modifiers & ConsoleModifiers.Shift) != 0, (modifiers & ConsoleModifiers.Alt) != 0, (modifiers & ConsoleModifiers.Control) != 0);
    }

    private static ConsoleKeyInfo ParseFromSingleChar(char single, bool isAlt)
    {
        bool isShift = false, isCtrl = false;
        char keyChar = single;

        ConsoleKey key = single switch
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
            _ when char.IsBetween(single, (char)1, (char)26) => ControlAndLetterPressed(single, out keyChar, out isCtrl),
            _ when char.IsBetween(single, (char)28, (char)31) => ControlAndDigitPressed(single, out keyChar, out isCtrl),
            '\u0000' => ControlAndDigitPressed(single, out keyChar, out isCtrl),
            _ => default
        };

        if (single is '\b' or '\n')
        {
            isCtrl = true; // Ctrl+Backspace is mapped to '\b' (8), Ctrl+Enter to '\n' (10)
        }

        if (isAlt)
        {
            isAlt = key != default; // two char sequences starting with Escape are Alt+$Key only when we can recognize the key
        }

        return new ConsoleKeyInfo(keyChar, key, isShift, isAlt, isCtrl);

        static ConsoleKey UppercaseCharacter(char single, out bool isShift)
        {
            // Previous implementation assumed that all uppercase characters were typed using Shift.
            // Limitation: Caps Lock+(a-z) is always mapped to Shift+(a-z).
            isShift = true;
            return ConsoleKey.A + single - 'A';
        }

        static ConsoleKey ControlAndLetterPressed(char single, out char keyChar, out bool isCtrl)
        {
            // Ctrl+(a-z) characters are mapped to values from 1 to 26.
            // Ctrl+H is mapped to 8, which also maps to Ctrl+Backspace.
            // Ctrl+I is mapped to 9, which also maps to Tab.
            // Ctrl+J is mapped to 10, which also maps to Ctrl+Enter ('\n').
            // Ctrl+M is mapped to 13, which also maps to Enter ('\r').
            // Limitation: Ctrl+H, Ctrl+I, Ctrl+J and Crl+M can't be mapped. More: https://unix.stackexchange.com/questions/563469/conflict-ctrl-i-with-tab-in-normal-mode
            Debug.Assert(single != 'b' && single != '\t' && single != '\n' && single != '\r');

            isCtrl = true;
            keyChar = default; // we could use the letter here, but it's impossible to distinguish upper vs lowercase (and Windows doesn't do it as well)
            return ConsoleKey.A + single - 1;
        }

        static ConsoleKey ControlAndDigitPressed(char single, out char keyChar, out bool isCtrl)
        {
            // Ctrl+(D3-D7) characters are mapped to values from 27 to 31. Escape is also mapped to 27.
            // Limitation: Ctrl+(D1, D3, D8, D9 and D0) can't be mapped.
            Debug.Assert(single == default || char.IsBetween(single, (char)28, (char)31));

            isCtrl = true;
            keyChar = default; // consistent with Windows
            return single switch
            {
                '\u0000' => ConsoleKey.D2, // was not previously mapped this way
                _ => ConsoleKey.D4 + single - 28
            };
        }
    }
}
