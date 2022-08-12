// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace System.Tests;

public class KeyParserTests
{
    private static readonly TerminalData[] Terminals =
    {
        new XTermData(),
        new GNOMETerminalData(),
        new LinuxConsole(),
        new PuTTYData_xterm(),
        new PuTTYData_linux(),
        new PuTTYData_putty(),
        new WindowsTerminalData(),
        new TmuxData(),
        new Tmux256ColorData(),
        new RxvtUnicode(),
    };

    private static IEnumerable<(char ch, ConsoleKey key)> AsciiKeys
    {
        get
        {
            yield return (' ', ConsoleKey.Spacebar);
            yield return ('\t', ConsoleKey.Tab);
            yield return ('\r', ConsoleKey.Enter);

            yield return ('+', ConsoleKey.Add);
            yield return ('-', ConsoleKey.Subtract);
            yield return ('*', ConsoleKey.Multiply);
            yield return ('/', ConsoleKey.Divide);

            yield return ('.', ConsoleKey.OemPeriod);
            yield return (',', ConsoleKey.OemComma);

            yield return ('\u001B', ConsoleKey.Escape);

            for (char i = '0'; i <= '9'; i++)
            {
                yield return (i, ConsoleKey.D0 + i - '0');
            }
            for (char i = 'a'; i <= 'z'; i++)
            {
                yield return (i, ConsoleKey.A + i - 'a');
            }
            for (char i = 'A'; i <= 'Z'; i++)
            {
                yield return (i, ConsoleKey.A + i - 'A');
            }
        }
    }

    public static IEnumerable<object[]> AsciiCharactersArguments
        => Terminals.SelectMany(terminal => AsciiKeys.Select(tuple => new object[] { terminal, tuple.ch, tuple.key }));

    [Theory]
    [MemberData(nameof(AsciiCharactersArguments))]
    public void AsciiCharacters(TerminalData terminalData, char input, ConsoleKey expectedKey)
    {
        ConsoleKeyInfo consoleKeyInfo = Parse(new[] { input }, terminalData.TerminalDb, terminalData.Verase, 1);

        Assert.Equal(input, consoleKeyInfo.KeyChar);
        Assert.Equal(expectedKey, consoleKeyInfo.Key);
        Assert.Equal(char.IsAsciiLetterUpper(input) ? ConsoleModifiers.Shift : 0, consoleKeyInfo.Modifiers);
    }

    public static IEnumerable<object[]> RecordedScenarios
    {
        get
        {
            foreach (TerminalData terminalData in Terminals)
            {
                foreach ((byte[] bytes, ConsoleKeyInfo cki) in terminalData.RecordedScenarios)
                {
                    yield return new object[] { terminalData, bytes, cki };
                }
            }

            // PuTTY has multiple settings that can customize key mappings.
            // 1. Connection => Data => Terminal details => Terminal-type string: this string controls the TERM env var.
            // The default value is xterm. Users can set it to putty, linux or any other known TERM.
            // For all these different TERMs we have different Terminfo databases.
            // On top of that, other Terminal settings can be applied (listed and tested below).
            // These settings often use different byte representation than stated in Terminfo.
            // Example: Terminfo says that F1 should be represented with X, but by using some other setting it's represented with Y.
            // That is why here we test their combinations.
            // From the implementation perspective, we test the Terminfo fallback code path (no mapping found).
            foreach (TerminalData putty in new TerminalData[] { new PuTTYData_xterm(), new PuTTYData_linux(), new PuTTYData_putty() })
            {
                // 2. Terminal => Keyboard => The Home and End keys
                // 2a: Standard
                foreach ((byte[] bytes, ConsoleKeyInfo cki) in PuTTy.StandardHomeAndEndKeys)
                {
                    yield return new object[] { putty, bytes, cki };
                }
                // 2b: rxvt
                foreach ((byte[] bytes, ConsoleKeyInfo cki) in PuTTy.RxvtHomeAndEndKeys)
                {
                    yield return new object[] { putty, bytes, cki };
                }

                // 3. Terminal => Keyboard => The function keys and keypad
                // 3a: ESC[n~
                foreach ((byte[] bytes, ConsoleKeyInfo cki) in PuTTy.ESCnFunctionKeys)
                {
                    yield return new object[] { putty, bytes, cki };
                }
                // 3b: Linux
                foreach ((byte[] bytes, ConsoleKeyInfo cki) in PuTTy.LinuxFunctionKeys)
                {
                    yield return new object[] { putty, bytes, cki };
                }
                // 3c: Xterm R6
                foreach ((byte[] bytes, ConsoleKeyInfo cki) in PuTTy.XtermR6FunctionKeys)
                {
                    yield return new object[] { putty, bytes, cki };
                }
                // 3d: VT 400
                foreach ((byte[] bytes, ConsoleKeyInfo cki) in PuTTy.VT400FunctionKeys)
                {
                    yield return new object[] { putty, bytes, cki };
                }
                // 3e: VT 100+
                foreach ((byte[] bytes, ConsoleKeyInfo cki) in PuTTy.VT100FunctionKeys)
                {
                    yield return new object[] { putty, bytes, cki };
                }
                // 3f: SCO
                foreach ((byte[] bytes, ConsoleKeyInfo cki) in PuTTy.SCOFunctionKeys)
                {
                    yield return new object[] { putty, bytes, cki };
                }
                // 3g: Xterm 216+
                foreach ((byte[] bytes, ConsoleKeyInfo cki) in PuTTy.Xterm216FunctionKeys)
                {
                    yield return new object[] { putty, bytes, cki };
                }

                // 4: Terminal => Keyboard => Shift/Ctrl/Alt with arrow keys
                // 4a: Ctrl toggles app mode
                foreach ((byte[] bytes, ConsoleKeyInfo cki) in PuTTy.CtrlTogglesAppModeArrows)
                {
                    yield return new object[] { putty, bytes, cki };
                }
                // 4b: xterm-style bitmap does not work as expected in application mode
                // as it produces same data as the setting above, so we don't test it
            }

            // I was not able to find any SCO terminal other than PuTTy which allows to emulate it
            foreach (TerminalData putty in new TerminalData[] { new PuTTYData_xterm(), new PuTTYData_linux() })
            {
                foreach ((byte[] bytes, ConsoleKeyInfo cki) in SCO.HomeKeys.Concat(SCO.ArrowKeys))
                {
                    yield return new object[] { putty, bytes, cki };
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(RecordedScenarios))]
    public void KeysAreProperlyMapped(TerminalData terminalData, byte[] recordedBytes, ConsoleKeyInfo expected)
    {
        char[] encoded = terminalData.ConsoleEncoding.GetString(recordedBytes).ToCharArray();

        ConsoleKeyInfo actual = Parse(encoded, terminalData.TerminalDb, terminalData.Verase, encoded.Length);

        Assert.Equal(expected.Key, actual.Key);
        Assert.Equal(expected.Modifiers, actual.Modifiers);
        Assert.Equal(expected.KeyChar, actual.KeyChar);
    }

    private static IEnumerable<(string chars, ConsoleKey key)> VTSequences
    {
        get
        {
            yield return (GetString(1), ConsoleKey.Home);
            yield return (GetString(2), ConsoleKey.Insert);
            yield return (GetString(3), ConsoleKey.Delete);
            yield return (GetString(4), ConsoleKey.End);
            yield return (GetString(5), ConsoleKey.PageUp);
            yield return (GetString(6), ConsoleKey.PageDown);
            yield return (GetString(7), ConsoleKey.Home);
            yield return (GetString(8), ConsoleKey.End);
            yield return (GetString(11), ConsoleKey.F1);
            yield return (GetString(12), ConsoleKey.F2);
            yield return (GetString(13), ConsoleKey.F3);
            yield return (GetString(14), ConsoleKey.F4);
            yield return (GetString(15), ConsoleKey.F5);
            yield return (GetString(17), ConsoleKey.F6);
            yield return (GetString(18), ConsoleKey.F7);
            yield return (GetString(19), ConsoleKey.F8);
            yield return (GetString(20), ConsoleKey.F9);
            yield return (GetString(21), ConsoleKey.F10);
            yield return (GetString(23), ConsoleKey.F11);
            yield return (GetString(24), ConsoleKey.F12);
            yield return (GetString(25), ConsoleKey.F13);
            yield return (GetString(26), ConsoleKey.F14);
            yield return (GetString(28), ConsoleKey.F15);
            yield return (GetString(29), ConsoleKey.F16);
            yield return (GetString(31), ConsoleKey.F17);
            yield return (GetString(32), ConsoleKey.F18);
            yield return (GetString(33), ConsoleKey.F19);
            yield return (GetString(34), ConsoleKey.F20);

            static string GetString(int i) => $"\u001B[{i}~";
        }
    }

    public static IEnumerable<object[]> VTSequencesArguments
        => Terminals.SelectMany(terminal => VTSequences.Select(tuple => new object[] { terminal, tuple.chars, tuple.key }));

    [Theory]
    [MemberData(nameof(VTSequencesArguments))]
    public void VTSequencesAreProperlyMapped(TerminalData terminalData, string input, ConsoleKey expectedKey)
    {
        if (terminalData is RxvtUnicode && input == "\u001B[4~" && expectedKey == ConsoleKey.End)
        {
            expectedKey = ConsoleKey.Select; // rxvt binds this key to Select in Terminfo and uses "^[[8~" for End key
        }

        ConsoleKeyInfo consoleKeyInfo = Parse(input.ToCharArray(), terminalData.TerminalDb, terminalData.Verase, input.Length);

        Assert.Equal(expectedKey, consoleKeyInfo.Key);
        Assert.Equal(default, consoleKeyInfo.KeyChar);
        Assert.Equal(default, consoleKeyInfo.Modifiers);
    }

    private static IEnumerable<(string chars, ConsoleKey key)> ThreeCharactersKeysRxvt
    {
        get
        {
            yield return ("\u001BOa", ConsoleKey.UpArrow);
            yield return ("\u001BOb", ConsoleKey.DownArrow);
            yield return ("\u001BOc", ConsoleKey.RightArrow);
            yield return ("\u001BOd", ConsoleKey.LeftArrow);
        }
    }

    [Fact]
    public void ExtendedStringCodePath()
    {
        RxvtUnicode terminalData = new RxvtUnicode();

        foreach ((string input, ConsoleKey expectedKey) in ThreeCharactersKeysRxvt)
        {
            ConsoleKeyInfo consoleKeyInfo = Parse(input.ToCharArray(), terminalData.TerminalDb, terminalData.Verase, input.Length);

            Assert.Equal(expectedKey, consoleKeyInfo.Key);
            Assert.Equal(default, consoleKeyInfo.KeyChar);
            Assert.Equal(ConsoleModifiers.Control, consoleKeyInfo.Modifiers);
        }
    }

    private static IEnumerable<(string chars, ConsoleKeyInfo[] keys)> EdgeCaseScenarios
    {
        get
        {
            // Backspace
            yield return (new string((char)127, 1), new[] { new ConsoleKeyInfo((char)127, ConsoleKey.Backspace, false, false, false) });
            // Ctrl+Backspace
            yield return ("\b", new[] { new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, true) });
            // Alt+Backspace
            yield return ("\u001B\u007F", new[] { new ConsoleKeyInfo((char)0x7F, ConsoleKey.Backspace, false, true, false) });
            // Ctrl+Alt+Backspace
            yield return ("\u001B\b", new[] { new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, true, true) });
            // Enter
            yield return ("\r", new[] { new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false) });
            // Ctrl+Enter
            yield return ("\n", new[] { new ConsoleKeyInfo('\n', ConsoleKey.Enter, false, false, true) });

            // Escape key pressed multiple times
            for (int i = 1; i <= 5; i++)
            {
                yield return (new string('\u001B', i), Enumerable.Repeat(new ConsoleKeyInfo('\u001B', ConsoleKey.Escape, false, false, false), i).ToArray());
            }

            // Home key (^[[H) followed by H key
            yield return ("\u001B[HH", new[]
            {
                new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, false),
                new ConsoleKeyInfo('H', ConsoleKey.H, true, false, false)
            });

            // escape sequence (F12 '^[[24~') followed by an extra tylde:
            yield return ($"\u001B[24~~", new[]
            {
                new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, false),
                new ConsoleKeyInfo('~', default, false, false, false),
            });

            // Invalid escape sequences:
            // Invalid modifiers (valid values are <2, 8>)
            foreach (int invalidModifier in new[] { 0, 1, 9 })
            {
                yield return ($"\u001B[1;{invalidModifier}H", new[]
                {
                    new ConsoleKeyInfo('\u001B', ConsoleKey.Escape, false, false, false),
                    new ConsoleKeyInfo('[', default, false, false, false),
                    new ConsoleKeyInfo('1', ConsoleKey.D1, false, false, false),
                    new ConsoleKeyInfo(';', default, false, false, false),
                    new ConsoleKeyInfo((char)('0' + invalidModifier), ConsoleKey.D0 + invalidModifier, false, false, false),
                    new ConsoleKeyInfo('H', ConsoleKey.H, true, false, false)
                });
            }
            // Invalid ID (valid values are <1, 34> except of 9, 16, 22, 27, 30 and 35)
            foreach (int invalidId in new[] { 16, 22, 27, 30, 35, 36, 77, 99 })
            {
                yield return ($"\u001B[{invalidId}~", new[]
                {
                    new ConsoleKeyInfo('\u001B', ConsoleKey.Escape, false, false, false),
                    new ConsoleKeyInfo('[', default, false, false, false),
                    new ConsoleKeyInfo((char)('0' + invalidId / 10), ConsoleKey.D0 + invalidId / 10, false, false, false),
                    new ConsoleKeyInfo((char)('0' + invalidId % 10), ConsoleKey.D0 + invalidId % 10, false, false, false),
                    new ConsoleKeyInfo('~', default, false, false, false),
                });
            }
            // too long ID (more than 2 digits)
            yield return ($"\u001B[111~", new[]
            {
                new ConsoleKeyInfo('\u001B', ConsoleKey.Escape, false, false, false),
                new ConsoleKeyInfo('[', default, false, false, false),
                new ConsoleKeyInfo('1', ConsoleKey.D1, false, false, false),
                new ConsoleKeyInfo('1', ConsoleKey.D1, false, false, false),
                new ConsoleKeyInfo('1', ConsoleKey.D1, false, false, false),
                new ConsoleKeyInfo('~', default, false, false, false),
            });
            // missing closing tag (tylde):
            yield return ($"\u001B[24", new[]
            {
                new ConsoleKeyInfo('\u001B', ConsoleKey.Escape, false, false, false),
                new ConsoleKeyInfo('[', default, false, false, false),
                new ConsoleKeyInfo('2', ConsoleKey.D2, false, false, false),
                new ConsoleKeyInfo('4', ConsoleKey.D4, false, false, false),
            });
        }
    }

    public static IEnumerable<object[]> EdgeCaseScenariosArguments
        => Terminals.SelectMany(terminal => EdgeCaseScenarios.Select(tuple => new object[] { terminal, tuple.chars, tuple.keys }));

    [Theory]
    [MemberData(nameof(EdgeCaseScenariosArguments))]
    public void EdgeCasesAreProperlyHandled(TerminalData terminalData, string input, ConsoleKeyInfo[] expectedKeys)
    {
        int startIndex = 0;
        char[] chars = input.ToCharArray();

        foreach (ConsoleKeyInfo expectedKey in expectedKeys)
        {
            ConsoleKeyInfo parsed = KeyParser.Parse(chars, terminalData.TerminalDb, 0, terminalData.Verase, ref startIndex, chars.Length);

            Assert.Equal(expectedKey.Key, parsed.Key);
            Assert.Equal(expectedKey.KeyChar, parsed.KeyChar);
            Assert.Equal(expectedKey.Modifiers, parsed.Modifiers);
        }

        Assert.Equal(chars.Length, startIndex);
    }

    [Theory]
    [MemberData(nameof(AsciiCharactersArguments))]
    public void VeraseIsRespected(TerminalData terminalData, char input, ConsoleKey ifNotVerase)
    {
        ConsoleKeyInfo consoleKeyInfo = Parse(new[] { input }, terminalData.TerminalDb, (byte)input, 1);

        Assert.Equal(input, consoleKeyInfo.KeyChar);
        Assert.Equal(ConsoleKey.Backspace, consoleKeyInfo.Key);
        Assert.NotEqual(ifNotVerase, consoleKeyInfo.Key);
        Assert.Equal((ConsoleModifiers)0, consoleKeyInfo.Modifiers);
    }

    [Fact]
    public void NewLineEscapeSequenceProducesCharacter()
    {
        XTermData xTerm = new();

        ConsoleKeyInfo consoleKeyInfo = Parse("\u001BOM".ToCharArray(), xTerm.TerminalDb, xTerm.Verase, 3);

        Assert.Equal(ConsoleKey.Enter, consoleKeyInfo.Key);
        Assert.Equal('\r', consoleKeyInfo.KeyChar);
        Assert.Equal((ConsoleModifiers)0, consoleKeyInfo.Modifiers);
    }

    [Fact]
    public void BackTabEscapeSequence()
    {
        XTermData xTerm = new();

        ConsoleKeyInfo consoleKeyInfo = Parse("\u001B[Z".ToCharArray(), xTerm.TerminalDb, xTerm.Verase, 3);

        Assert.Equal(ConsoleKey.Tab, consoleKeyInfo.Key);
        Assert.Equal(default, consoleKeyInfo.KeyChar);
        Assert.Equal(ConsoleModifiers.Shift, consoleKeyInfo.Modifiers);
    }

    private static ConsoleKeyInfo Parse(char[] chars, TerminalFormatStrings terminalFormatStrings, byte verase, int expectedStartIndex)
    {
        int startIndex = 0;

        ConsoleKeyInfo parsed = KeyParser.Parse(chars, terminalFormatStrings, 0, verase, ref startIndex, chars.Length);

        Assert.Equal(expectedStartIndex, startIndex);

        return parsed;
    }
}

public abstract class TerminalData
{
    private TerminalFormatStrings? _terminalDb;
    private Encoding? _consoleEncoding;

    protected abstract string EncodingCharset { get; }
    protected abstract string Term { get; }
    protected abstract string EncodedTerminalDb { get; }
    internal abstract byte Verase { get; }
    internal abstract IEnumerable<(byte[], ConsoleKeyInfo)> RecordedScenarios { get; }

    internal TerminalFormatStrings TerminalDb => _terminalDb ??=
        new TerminalFormatStrings(new TermInfo.Database(Term, Convert.FromBase64String(EncodedTerminalDb)));

    internal Encoding ConsoleEncoding => _consoleEncoding ??= (string.IsNullOrEmpty(EncodingCharset) ? Encoding.Default : Encoding.GetEncoding(EncodingCharset)).RemovePreamble();
}

// Below you can find test data recorded with https://github.com/adamsitnik/ReadKey
// The idea behind is to be able to verify parser changes without the need of manual verification for every known Terminal.

// Ubuntu 18.04 x64
public class GNOMETerminalData : TerminalData
{
    protected override string EncodingCharset => "utf-8";
    protected override string Term => "xterm-256color";
    internal override byte Verase => 127;
    protected override string EncodedTerminalDb => "GgElACYADwCdAQIGeHRlcm0tMjU2Y29sb3J8eHRlcm0gd2l0aCAyNTYgY29sb3JzAAABAAABAAAAAQAAAAABAQAAAAAAAAABAAABAAEBAAAAAAAAAAABAFAACAAYAP//////////////////////////AAH/fwAABAAGAAgAGQAeACYAKgAuAP//OQBKAEwAUABXAP//WQBmAP//agBuAHgAfAD/////gACEAIkAjgD//6AApQCqAP//rwC0ALkAvgDHAMsA0gD//+QA6QDvAPUA////////BwH///////8ZAf//HQH///////8fAf//JAH//////////ygBLAEyATYBOgE+AUQBSgFQAVYBXAFgAf//ZQH//2kBbgFzAXcBfgH//4UBiQGRAf////////////////////////////+ZAaIB/////6sBtAG9AcYBzwHYAeEB6gHzAfwB////////BQIJAg4CEwInAjAC/////0ICRQJQAlMCVQJYArUC//+4Av///////////////7oC//////////++Av//8wL/////9wL9Av////////////////////////////8DAwcD//////////////////////////////////////////////////////////////////8LA/////8SA///////////GQMgAycD/////y4D//81A////////zwD/////////////0MDSQNPA1YDXQNkA2sDcwN7A4MDiwOTA5sDowOrA7IDuQPAA8cDzwPXA98D5wPvA/cD/wMHBA4EFQQcBCMEKwQzBDsEQwRLBFMEWwRjBGoEcQR4BH8EhwSPBJcEnwSnBK8EtwS/BMYEzQTUBP/////////////////////////////////////////////////////////////ZBOQE6QT8BAAFCQUQBf////////////////////////////9uBf///////////////////////3MF////////////////////////////////////////////////////////////////////////////////////////eQX///////99BbwF//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////wF/wUbW1oABwANABtbJWklcDElZDslcDIlZHIAG1szZwAbW0gbWzJKABtbSwAbW0oAG1slaSVwMSVkRwAbWyVpJXAxJWQ7JXAyJWRIAAoAG1tIABtbPzI1bAAIABtbPzEybBtbPzI1aAAbW0MAG1tBABtbPzEyOzI1aAAbW1AAG1tNABsoMAAbWzVtABtbMW0AG1s/MTA0OWgbWzIyOzA7MHQAG1sybQAbWzRoABtbOG0AG1s3bQAbWzdtABtbNG0AG1slcDElZFgAGyhCABsoQhtbbQAbWz8xMDQ5bBtbMjM7MDswdAAbWzRsABtbMjdtABtbMjRtABtbPzVoJDwxMDAvPhtbPzVsABtbIXAbWz8zOzRsG1s0bBs+ABtbTAB/ABtbM34AG09CABtPUAAbWzIxfgAbT1EAG09SABtPUwAbWzE1fgAbWzE3fgAbWzE4fgAbWzE5fgAbWzIwfgAbT0gAG1syfgAbT0QAG1s2fgAbWzV+ABtPQwAbWzE7MkIAG1sxOzJBABtPQQAbWz8xbBs+ABtbPzFoGz0AG1s/MTAzNGwAG1s/MTAzNGgAG1slcDElZFAAG1slcDElZE0AG1slcDElZEIAG1slcDElZEAAG1slcDElZFMAG1slcDElZEwAG1slcDElZEQAG1slcDElZEMAG1slcDElZFQAG1slcDElZEEAG1tpABtbNGkAG1s1aQAlcDElYxtbJXAyJXsxfSUtJWRiABtjG10xMDQHABtbIXAbWz8zOzRsG1s0bBs+ABs4ABtbJWklcDElZGQAGzcACgAbTQAlPyVwOSV0GygwJWUbKEIlOxtbMCU/JXA2JXQ7MSU7JT8lcDUldDsyJTslPyVwMiV0OzQlOyU/JXAxJXAzJXwldDs3JTslPyVwNCV0OzUlOyU/JXA3JXQ7OCU7bQAbSAAJABtPRQBgYGFhZmZnZ2lpampra2xsbW1ubm9vcHBxcXJyc3N0dHV1dnZ3d3h4eXl6ent7fHx9fX5+ABtbWgAbWz83aAAbWz83bAAbT0YAG09NABtbMzsyfgAbWzE7MkYAG1sxOzJIABtbMjsyfgAbWzE7MkQAG1s2OzJ+ABtbNTsyfgAbWzE7MkMAG1syM34AG1syNH4AG1sxOzJQABtb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"; // /lib/terminfo/x/xterm-256color

    internal override IEnumerable<(byte[], ConsoleKeyInfo)> RecordedScenarios
    {
        get
        {
            yield return (new byte[] { 90 }, new ConsoleKeyInfo('Z', ConsoleKey.Z, true, false, false));
            yield return (new byte[] { 97 }, new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false));
            yield return (new byte[] { 1 }, new ConsoleKeyInfo(default, ConsoleKey.A, false, false, true));
            yield return (new byte[] { 27, 97 }, new ConsoleKeyInfo('a', ConsoleKey.A, false, true, false));
            yield return (new byte[] { 27, 1 }, new ConsoleKeyInfo(default, ConsoleKey.A, false, true, true));
            yield return (new byte[] { 49 }, new ConsoleKeyInfo('1', ConsoleKey.D1, false, false, false));
            yield return (new byte[] { 27, 49 }, new ConsoleKeyInfo('1', ConsoleKey.D1, false, true, false));
            yield return (new byte[] { 33 }, new ConsoleKeyInfo('!', default, false, false, false));
            yield return (new byte[] { 50 }, new ConsoleKeyInfo('2', ConsoleKey.D2, false, false, false));
            yield return (new byte[] { 0 }, new ConsoleKeyInfo(default, ConsoleKey.D2, false, false, true));
            yield return (new byte[] { 27, 50 }, new ConsoleKeyInfo('2', ConsoleKey.D2, false, true, false));
            yield return (new byte[] { 64 }, new ConsoleKeyInfo('@', default, false, false, false));
            yield return (new byte[] { 61 }, new ConsoleKeyInfo('=', default, false, false, false));
            yield return (new byte[] { 43 }, new ConsoleKeyInfo('+', ConsoleKey.Add, false, false, false));
            yield return (new byte[] { 27, 61 }, new ConsoleKeyInfo('=', default, false, false, false));
            yield return (new byte[] { 27 }, new ConsoleKeyInfo((char)27, ConsoleKey.Escape, false, false, false));
            yield return (new byte[] { 127 }, new ConsoleKeyInfo((char)127, ConsoleKey.Backspace, false, false, false)); // verase
            yield return (new byte[] { 27, 127 }, new ConsoleKeyInfo((char)127, ConsoleKey.Backspace, false, true, false));
            yield return (new byte[] { 49 }, new ConsoleKeyInfo('1', ConsoleKey.D1, false, false, false));
            yield return (new byte[] { 43 }, new ConsoleKeyInfo('+', ConsoleKey.Add, false, false, false));
            yield return (new byte[] { 45 }, new ConsoleKeyInfo('-', ConsoleKey.Subtract, false, false, false));
            yield return (new byte[] { 27, 79, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, false));
            yield return (new byte[] { 27, 79, 80 }, new ConsoleKeyInfo(default, ConsoleKey.F1, false, false, false)); // F1
            yield return (new byte[] { 27, 79, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F2, false, false, false)); // F2
            yield return (new byte[] { 27, 79, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, false, false)); // F3
            yield return (new byte[] { 27, 79, 83 }, new ConsoleKeyInfo(default, ConsoleKey.F4, false, false, false)); // F4
            yield return (new byte[] { 27, 91, 49, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, false, false, false)); // F5
            yield return (new byte[] { 27, 91, 49, 55, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, false, false, false)); // F6
            yield return (new byte[] { 27, 91, 49, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, false, false, false)); // F7
            yield return (new byte[] { 27, 91, 49, 57, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, false, false, false)); // F8
            yield return (new byte[] { 27, 91, 50, 48, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, false, false)); // F9
            yield return (new byte[] { 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, false)); // F12
            yield return (new byte[] { 27, 91, 49, 59, 53, 80 }, new ConsoleKeyInfo(default, ConsoleKey.F1, false, false, true)); // Ctrl+F1
            yield return (new byte[] { 27, 91, 49, 59, 53, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F2, false, false, true)); // Ctrl+F2
            yield return (new byte[] { 27, 91, 49, 59, 53, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, false, true)); // Ctrl+F3
            yield return (new byte[] { 27, 91, 49, 59, 53, 83 }, new ConsoleKeyInfo(default, ConsoleKey.F4, false, false, true)); // Ctrl+F4
            yield return (new byte[] { 27, 91, 49, 53, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, false, false, true)); // Ctrl+F5
            yield return (new byte[] { 27, 91, 49, 55, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, false, false, true)); // Ctrl+F6
            yield return (new byte[] { 27, 91, 49, 56, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, false, false, true)); // Ctrl+F7
            yield return (new byte[] { 27, 91, 49, 57, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, false, false, true)); // Ctrl+F8
            yield return (new byte[] { 27, 91, 50, 48, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, false, true)); // Ctrl+F9
            yield return (new byte[] { 27, 91, 50, 49, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F10, false, false, true)); // Ctrl+F10
            yield return (new byte[] { 27, 91, 50, 51, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, false, true)); // Ctrl+F11
            yield return (new byte[] { 27, 91, 50, 52, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, true)); // Ctrl+F12
            yield return (new byte[] { 27, 91, 50, 48, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, true, false)); // Alt+F9
            yield return (new byte[] { 27, 91, 50, 51, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, true, false)); // Alt+F11
            yield return (new byte[] { 27, 91, 50, 52, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, true, false)); // Alt+F12
            yield return (new byte[] { 27, 91, 49, 59, 50, 80 }, new ConsoleKeyInfo(default, ConsoleKey.F1, true, false, false)); // Shift+F1
            yield return (new byte[] { 27, 91, 49, 59, 50, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F2, true, false, false)); // Shift+F2
            yield return (new byte[] { 27, 91, 49, 59, 50, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, true, false, false)); // Shift+F3
            yield return (new byte[] { 27, 91, 49, 59, 50, 83 }, new ConsoleKeyInfo(default, ConsoleKey.F4, true, false, false)); // Shift+F4
            yield return (new byte[] { 27, 91, 49, 53, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, true, false, false)); // Shift+F5
            yield return (new byte[] { 27, 91, 49, 55, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, true, false, false)); // Shift+F6
            yield return (new byte[] { 27, 91, 49, 56, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, true, false, false)); // Shift+F7
            yield return (new byte[] { 27, 91, 49, 57, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, true, false, false)); // Shift+F8
            yield return (new byte[] { 27, 91, 50, 48, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, true, false, false)); // Shift+F9
            yield return (new byte[] { 27, 91, 50, 51, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, true, false, false)); // Shift+F11
            yield return (new byte[] { 27, 91, 50, 52, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, true, false, false)); // Shift+F12
            yield return (new byte[] { 27, 91, 49, 59, 56, 80 }, new ConsoleKeyInfo(default, ConsoleKey.F1, true, true, true)); // Ctrl+Alt+Shift+F1
            yield return (new byte[] { 27, 91, 49, 59, 56, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F2, true, true, true)); // Ctrl+Alt+Shift+F2
            yield return (new byte[] { 27, 91, 49, 59, 56, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, true, true, true)); // Ctrl+Alt+Shift+F3
            yield return (new byte[] { 27, 91, 49, 59, 56, 83 }, new ConsoleKeyInfo(default, ConsoleKey.F4, true, true, true)); // Ctrl+Alt+Shift+F4
            yield return (new byte[] { 27, 91, 49, 53, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, true, true, true)); // Ctrl+Alt+Shift+F5
            yield return (new byte[] { 27, 91, 49, 55, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, true, true, true)); // Ctrl+Alt+Shift+F6
            yield return (new byte[] { 27, 91, 49, 56, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, true, true, true)); // Ctrl+Alt+Shift+F7
            yield return (new byte[] { 27, 91, 49, 57, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, true, true, true)); // Ctrl+Alt+Shift+F8
            yield return (new byte[] { 27, 91, 50, 48, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, true, true, true)); // Ctrl+Alt+Shift+F9
            yield return (new byte[] { 27, 91, 50, 49, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F10, true, true, true)); // Ctrl+Alt+Shift+F10
            yield return (new byte[] { 27, 91, 50, 51, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, true, true, true)); // Ctrl+Alt+Shift+F11
            yield return (new byte[] { 27, 91, 50, 52, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, true, true, true)); // Ctrl+Alt+Shift+F12
            yield return (new byte[] { 27, 79, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, false)); // Home
            yield return (new byte[] { 27, 91, 49, 59, 53, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, true)); // Ctrl+Home
            yield return (new byte[] { 27, 91, 49, 59, 51, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, true, false)); // Alt+Home
            yield return (new byte[] { 27, 91, 49, 59, 55, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, true, true)); // Ctrl+Alt+Home
            yield return (new byte[] { 27, 79, 70 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, false, false)); // End
            yield return (new byte[] { 27, 91, 49, 59, 53, 70 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, false, true)); // Ctrl+End
            yield return (new byte[] { 27, 91, 49, 59, 51, 70 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, true, false)); // Alt+End
            yield return (new byte[] { 27, 91, 49, 59, 55, 70 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, true, true)); // Ctrl+Alt+End
            yield return (new byte[] { 27, 91, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, false, false)); // PageUp
            yield return (new byte[] { 27, 91, 53, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, false, true)); // Ctrl+PageUp
            yield return (new byte[] { 27, 91, 53, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, true, false)); // Alt+PageUp
            yield return (new byte[] { 27, 91, 53, 59, 55, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, true, true)); // Ctrl+Alt+PageUp
            yield return (new byte[] { 27, 91, 54, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, false, false)); // PageDown
            yield return (new byte[] { 27, 91, 54, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, false, true)); // Ctrl+PageDown
            yield return (new byte[] { 27, 91, 54, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, true, false)); // Alt+PageDown
            yield return (new byte[] { 27, 91, 54, 59, 55, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, true, true)); // Ctrl+Alt+PageDown
            yield return (new byte[] { 27, 79, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, false)); // LeftArrow
            yield return (new byte[] { 27, 91, 49, 59, 53, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, true)); // Ctrl+LeftArrow
            yield return (new byte[] { 27, 91, 49, 59, 51, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, true, false)); // Alt+LeftArrow
            yield return (new byte[] { 27, 91, 49, 59, 50, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, true, false, false)); // Shift+LeftArrow
            yield return (new byte[] { 27, 91, 49, 59, 52, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, true, true, false)); // Shift+Alt+LeftArrow
            yield return (new byte[] { 27, 79, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, false, false, false)); // UpArrow
            yield return (new byte[] { 27, 91, 49, 59, 53, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, false, false, true)); // Ctrl+UpArrow
            yield return (new byte[] { 27, 91, 49, 59, 51, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, false, true, false)); // Alt+UpArrow
            yield return (new byte[] { 27, 91, 49, 59, 50, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, true, false, false)); // Shift+UpArrow
            yield return (new byte[] { 27, 91, 49, 59, 52, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, true, true, false)); // Shift+Alt+UpArrow
            yield return (new byte[] { 27, 79, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, false, false, false)); // DownArrow
            yield return (new byte[] { 27, 91, 49, 59, 53, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, false, false, true)); // Ctrl+DownArrow
            yield return (new byte[] { 27, 91, 49, 59, 51, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, false, true, false)); // Alt+DownArrow
            yield return (new byte[] { 27, 91, 49, 59, 50, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, true, false, false)); // Shift+DownArrow
            yield return (new byte[] { 27, 91, 49, 59, 52, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, true, true, false)); // Shift+Alt+DownArrow
            yield return (new byte[] { 27, 79, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, false, false, false)); // RightArrow
            yield return (new byte[] { 27, 91, 49, 59, 53, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, false, false, true)); // Ctrl+RightArrow
            yield return (new byte[] { 27, 91, 49, 59, 51, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, false, true, false)); // Alt+RightArrow
            yield return (new byte[] { 27, 91, 49, 59, 50, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, true, false, false)); // Shift+RightArrow
            yield return (new byte[] { 27, 91, 49, 59, 52, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, true, true, false)); // Shift+Alt+RightArrow
            yield return (new byte[] { 27, 91, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Insert, false, false, false)); // Insert
            yield return (new byte[] { 27, 91, 50, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Insert, false, true, false)); // Alt+Insert
            yield return (new byte[] { 27, 91, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, false, false)); // Delete
            yield return (new byte[] { 27, 91, 51, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, false, true)); // Ctrl+Delete
            yield return (new byte[] { 27, 91, 51, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, true, false)); // Alt+Delete
            yield return (new byte[] { 27, 91, 51, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, true, false, false)); // Shift+Delete
            yield return (new byte[] { 27, 91, 51, 59, 54, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, true, false, true)); // Ctrl+Shift+Delete
            yield return (new byte[] { 27, 91, 51, 59, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, true, true, false)); // Alt+Shift+Delete
            yield return (new byte[] { 27, 91, 51, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, true, true, true)); // Ctrl+Alt+Shift+Delete
            // Numeric Keypad
            yield return (new byte[] { 48 }, new ConsoleKeyInfo('0', ConsoleKey.D0, false, false, false));
            yield return (new byte[] { 49 }, new ConsoleKeyInfo('1', ConsoleKey.D1, false, false, false));
            yield return (new byte[] { 50 }, new ConsoleKeyInfo('2', ConsoleKey.D2, false, false, false));
            yield return (new byte[] { 51 }, new ConsoleKeyInfo('3', ConsoleKey.D3, false, false, false));
            yield return (new byte[] { 52 }, new ConsoleKeyInfo('4', ConsoleKey.D4, false, false, false));
            yield return (new byte[] { 53 }, new ConsoleKeyInfo('5', ConsoleKey.D5, false, false, false));
            yield return (new byte[] { 54 }, new ConsoleKeyInfo('6', ConsoleKey.D6, false, false, false));
            yield return (new byte[] { 55 }, new ConsoleKeyInfo('7', ConsoleKey.D7, false, false, false));
            yield return (new byte[] { 56 }, new ConsoleKeyInfo('8', ConsoleKey.D8, false, false, false));
            yield return (new byte[] { 57 }, new ConsoleKeyInfo('9', ConsoleKey.D9, false, false, false));
            yield return (new byte[] { 47 }, new ConsoleKeyInfo('/', ConsoleKey.Divide, false, false, false));
            yield return (new byte[] { 42 }, new ConsoleKeyInfo('*', ConsoleKey.Multiply, false, false, false));
            yield return (new byte[] { 45 }, new ConsoleKeyInfo('-', ConsoleKey.Subtract, false, false, false));
            yield return (new byte[] { 43 }, new ConsoleKeyInfo('+', ConsoleKey.Add, false, false, false));
            yield return (new byte[] { 13 }, new ConsoleKeyInfo((char)13, ConsoleKey.Enter, false, false, false)); // Enter (using Numeric Keypad))
            yield return (new byte[] { 46 }, new ConsoleKeyInfo('.', ConsoleKey.OemPeriod, false, false, false)); // . (period using Numeric Keypad))
            // Num Lock toggle
            yield return (new byte[] { 27, 91, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Insert, false, false, false)); // Insert
            yield return (new byte[] { 27, 91, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, false, false)); // Delete
            yield return (new byte[] { 27, 79, 70 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, false, false)); // End
            yield return (new byte[] { 27, 79, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, false, false, false)); // Down Arrow
            yield return (new byte[] { 27, 91, 54, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, false, false)); // Page Down
            yield return (new byte[] { 27, 79, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, false)); // Left Arrow
            yield return (new byte[] { 27, 79, 69 }, new ConsoleKeyInfo(default, ConsoleKey.NoName, false, false, false)); // Begin (5)
            yield return (new byte[] { 27, 79, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, false, false, false)); // Right Arrow
            yield return (new byte[] { 27, 79, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, false)); // Home
            yield return (new byte[] { 27, 79, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, false, false, false)); // Up Arrow
            yield return (new byte[] { 27, 91, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, false, false)); // Page Up
            yield return (new byte[] { 27, 79, 111 }, new ConsoleKeyInfo('/', ConsoleKey.Divide, false, false, false)); // / (divide sign using Numeric Keypad))
            yield return (new byte[] { 27, 79, 106 }, new ConsoleKeyInfo('*', ConsoleKey.Multiply, false, false, false)); // * (multiply sign using Numeric Keypad))
            yield return (new byte[] { 27, 79, 109 }, new ConsoleKeyInfo('-', ConsoleKey.Subtract, false, false, false)); // - (minus sign using Numeric Keypad))
            yield return (new byte[] { 27, 79, 107 }, new ConsoleKeyInfo('+', ConsoleKey.Add, false, false, false)); // + (plus sign using Numeric Keypad))
            yield return (new byte[] { 27, 79, 77 }, new ConsoleKeyInfo((char)13, ConsoleKey.Enter, false, false, false)); // Enter (using Numeric Keypad))
        }
    }
}

// Ubuntu 18.04 x64
public class XTermData : TerminalData
{
    protected override string EncodingCharset => "utf-8";
    protected override string Term => "xterm";
    internal override byte Verase => 127;
    protected override string EncodedTerminalDb => "GgEpACYADwCdAbgFeHRlcm18eHRlcm0tZGViaWFufFgxMSB0ZXJtaW5hbCBlbXVsYXRvcgAAAQAAAQAAAAEAAAAAAQEAAAAAAAAAAQAAAQAAAQAAAAAAAAAAAQBQAAgAGAD//////////////////////////wgAQAAAAAQABgAIABkAHgAmACoALgD//zkASgBMAFAAVwD//1kAZgD//2oAbgB4AHwA/////4AAhACJAI4A//+gAKUAqgD//68AtAC5AL4AxwDLANIA///kAOkA7wD1AP///////wcB////////GQH//x0B////////HwH//yQB//////////8oASwBMgE2AToBPgFEAUoBUAFWAVwBYAH//2UB//9pAW4BcwF3AX4B//+FAYkBkQH/////////////////////////////mQGiAf////+rAbQBvQHGAc8B2AHhAeoB8wH8Af///////wUCCQIOAhMCJwIqAv////88Aj8CSgJNAk8CUgKvAv//sgL///////////////+0Av//////////uAL//+0C//////EC9wL//////////////////////////////QIBA///////////////////////////////////////////////////////////////////BQP/////DAP//////////xMDGgMhA/////8oA///LwP///////82A/////////////89A0MDSQNQA1cDXgNlA20DdQN9A4UDjQOVA50DpQOsA7MDugPBA8kD0QPZA+ED6QPxA/kDAQQIBA8EFgQdBCUELQQ1BD0ERQRNBFUEXQRkBGsEcgR5BIEEiQSRBJkEoQSpBLEEuQTABMcEzgT/////////////////////////////////////////////////////////////0wTeBOME9gT6BP//////////AwVJBf//////////////////jwX///////////////////////+UBf///////////////////////////////////////////////////////////////////////////////////////5oF////////ngWoBf//////////////////////////////////////////////////////////////////////////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"; // /lib/terminfo/x/xterm

    internal override IEnumerable<(byte[], ConsoleKeyInfo)> RecordedScenarios
    {
        get
        {
            yield return (new byte[] { 90 }, new ConsoleKeyInfo('Z', ConsoleKey.Z, true, false, false));
            yield return (new byte[] { 97 }, new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false));
            yield return (new byte[] { 1 }, new ConsoleKeyInfo(default, ConsoleKey.A, false, false, true));
            yield return (new byte[] { 195, 161 }, new ConsoleKeyInfo('\u00E1', default, false, false, false));
            yield return (new byte[] { 194, 129 }, new ConsoleKeyInfo('\u0081', default, false, false, false));
            yield return (new byte[] { 49 }, new ConsoleKeyInfo('1', ConsoleKey.D1, false, false, false));
            yield return (new byte[] { 194, 177 }, new ConsoleKeyInfo('\u00B1', default, false, false, false));
            yield return (new byte[] { 33 }, new ConsoleKeyInfo('!', default, false, false, false));
            yield return (new byte[] { 50 }, new ConsoleKeyInfo('2', ConsoleKey.D2, false, false, false));
            yield return (new byte[] { 0 }, new ConsoleKeyInfo(default, ConsoleKey.D2, false, false, true));
            yield return (new byte[] { 194, 178 }, new ConsoleKeyInfo('\u00B2', default, false, false, false));
            yield return (new byte[] { 64 }, new ConsoleKeyInfo('@', default, false, false, false));
            yield return (new byte[] { 61 }, new ConsoleKeyInfo('=', default, false, false, false));
            yield return (new byte[] { 43 }, new ConsoleKeyInfo('+', ConsoleKey.Add, false, false, false));
            yield return (new byte[] { 194, 189 }, new ConsoleKeyInfo('\u00BD', default, false, false, false));
            yield return (new byte[] { 27 }, new ConsoleKeyInfo((char)27, ConsoleKey.Escape, false, false, false));
            yield return (new byte[] { 127 }, new ConsoleKeyInfo((char)127, ConsoleKey.Backspace, false, false, false)); // verase
            yield return (new byte[] { 195, 191 }, new ConsoleKeyInfo('\u00FF', default, false, false, false));
            yield return (new byte[] { 194, 136 }, new ConsoleKeyInfo('\u0088', default, false, false, false));
            yield return (new byte[] { 27, 79, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, false));
            yield return (new byte[] { 49 }, new ConsoleKeyInfo('1', ConsoleKey.D1, false, false, false));
            yield return (new byte[] { 43 }, new ConsoleKeyInfo('+', ConsoleKey.Add, false, false, false));
            yield return (new byte[] { 45 }, new ConsoleKeyInfo('-', ConsoleKey.Subtract, false, false, false));

            yield return (new byte[] { 27, 79, 80 }, new ConsoleKeyInfo(default, ConsoleKey.F1, false, false, false)); // F1
            yield return (new byte[] { 27, 79, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F2, false, false, false)); // F2
            yield return (new byte[] { 27, 79, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, false, false)); // F3
            yield return (new byte[] { 27, 79, 83 }, new ConsoleKeyInfo(default, ConsoleKey.F4, false, false, false)); // F4
            yield return (new byte[] { 27, 91, 49, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, false, false, false)); // F5
            yield return (new byte[] { 27, 91, 49, 55, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, false, false, false)); // F6
            yield return (new byte[] { 27, 91, 49, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, false, false, false)); // F7
            yield return (new byte[] { 27, 91, 49, 57, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, false, false, false)); // F8
            yield return (new byte[] { 27, 91, 50, 48, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, false, false)); // F9
            yield return (new byte[] { 27, 91, 50, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F10, false, false, false)); // F10
            yield return (new byte[] { 27, 91, 50, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, false, false)); // F11
            yield return (new byte[] { 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, false)); // F12
            yield return (new byte[] { 27, 91, 49, 59, 53, 80 }, new ConsoleKeyInfo(default, ConsoleKey.F1, false, false, true)); // Ctrl+F1
            yield return (new byte[] { 27, 91, 49, 59, 53, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F2, false, false, true)); // Ctrl+F2
            yield return (new byte[] { 27, 91, 49, 59, 53, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, false, true)); // Ctrl+F3
            yield return (new byte[] { 27, 91, 49, 59, 53, 83 }, new ConsoleKeyInfo(default, ConsoleKey.F4, false, false, true)); // Ctrl+F4
            yield return (new byte[] { 27, 91, 49, 53, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, false, false, true)); // Ctrl+F5
            yield return (new byte[] { 27, 91, 49, 55, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, false, false, true)); // Ctrl+F6
            yield return (new byte[] { 27, 91, 49, 56, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, false, false, true)); // Ctrl+F7
            yield return (new byte[] { 27, 91, 49, 57, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, false, false, true)); // Ctrl+F8
            yield return (new byte[] { 27, 91, 50, 48, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, false, true)); // Ctrl+F9
            yield return (new byte[] { 27, 91, 50, 49, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F10, false, false, true)); // Ctrl+F10
            yield return (new byte[] { 27, 91, 50, 51, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, false, true)); // Ctrl+F11
            yield return (new byte[] { 27, 91, 50, 52, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, true)); // Ctrl+F12
            yield return (new byte[] { 27, 91, 49, 59, 51, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, true, false)); // Alt+F3
            yield return (new byte[] { 27, 91, 50, 48, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, true, false)); // Alt+F9
            yield return (new byte[] { 27, 91, 50, 51, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, true, false)); // Alt+F11
            yield return (new byte[] { 27, 91, 50, 52, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, true, false)); // Alt+F12
            yield return (new byte[] { 27, 91, 49, 59, 50, 80 }, new ConsoleKeyInfo(default, ConsoleKey.F1, true, false, false)); // Shift+F1
            yield return (new byte[] { 27, 91, 49, 59, 50, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F2, true, false, false)); // Shift+F2
            yield return (new byte[] { 27, 91, 49, 59, 50, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, true, false, false)); // Shift+F3
            yield return (new byte[] { 27, 91, 49, 59, 50, 83 }, new ConsoleKeyInfo(default, ConsoleKey.F4, true, false, false)); // Shift+F4
            yield return (new byte[] { 27, 91, 49, 53, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, true, false, false)); // Shift+F5
            yield return (new byte[] { 27, 91, 49, 55, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, true, false, false)); // Shift+F6
            yield return (new byte[] { 27, 91, 49, 56, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, true, false, false)); // Shift+F7
            yield return (new byte[] { 27, 91, 49, 57, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, true, false, false)); // Shift+F8
            yield return (new byte[] { 27, 91, 50, 48, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, true, false, false)); // Shift+F9
            yield return (new byte[] { 27, 91, 50, 49, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F10, true, false, false)); // Shift+F10
            yield return (new byte[] { 27, 91, 50, 51, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, true, false, false)); // Shift+F11
            yield return (new byte[] { 27, 91, 50, 52, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, true, false, false)); // Shift+F12
            yield return (new byte[] { 27, 91, 49, 59, 56, 80 }, new ConsoleKeyInfo(default, ConsoleKey.F1, true, true, true)); // Ctrl+Alt+Shift+F1
            yield return (new byte[] { 27, 91, 49, 59, 56, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F2, true, true, true)); // Ctrl+Alt+Shift+F2
            yield return (new byte[] { 27, 91, 49, 59, 56, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, true, true, true)); // Ctrl+Alt+Shift+F3
            yield return (new byte[] { 27, 91, 49, 59, 56, 83 }, new ConsoleKeyInfo(default, ConsoleKey.F4, true, true, true)); // Ctrl+Alt+Shift+F4
            yield return (new byte[] { 27, 91, 49, 53, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, true, true, true)); // Ctrl+Alt+Shift+F5
            yield return (new byte[] { 27, 91, 49, 55, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, true, true, true)); // Ctrl+Alt+Shift+F6
            yield return (new byte[] { 27, 91, 49, 56, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, true, true, true)); // Ctrl+Alt+Shift+F7
            yield return (new byte[] { 27, 91, 49, 57, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, true, true, true)); // Ctrl+Alt+Shift+F8
            yield return (new byte[] { 27, 91, 50, 48, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, true, true, true)); // Ctrl+Alt+Shift+F9
            yield return (new byte[] { 27, 91, 50, 49, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F10, true, true, true)); // Ctrl+Alt+Shift+F10
            yield return (new byte[] { 27, 91, 50, 51, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, true, true, true)); // Ctrl+Alt+Shift+F11
            yield return (new byte[] { 27, 91, 50, 52, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, true, true, true)); // Ctrl+Alt+Shift+F12
            yield return (new byte[] { 27, 79, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, false)); // Home
            yield return (new byte[] { 27, 91, 49, 59, 53, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, true)); // Ctrl+Home
            yield return (new byte[] { 27, 91, 49, 59, 51, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, true, false)); // Alt+Home
            yield return (new byte[] { 27, 91, 49, 59, 55, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, true, true)); // Ctrl+Alt+Home
            yield return (new byte[] { 27, 79, 70 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, false, false)); // End
            yield return (new byte[] { 27, 91, 49, 59, 53, 70 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, false, true)); // Ctrl+End
            yield return (new byte[] { 27, 91, 49, 59, 51, 70 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, true, false)); // Alt+End
            yield return (new byte[] { 27, 91, 49, 59, 55, 70 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, true, true)); // Ctrl+Alt+End
            yield return (new byte[] { 27, 91, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, false, false)); // PageUp
            yield return (new byte[] { 27, 91, 53, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, false, true)); // Ctrl+PageUp
            yield return (new byte[] { 27, 91, 53, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, true, false)); // Alt+PageUp
            yield return (new byte[] { 27, 91, 53, 59, 55, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, true, true)); // Ctrl+Alt+PageUp
            yield return (new byte[] { 27, 91, 54, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, false, false)); // PageDown
            yield return (new byte[] { 27, 91, 54, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, false, true)); // Ctrl+PageDown
            yield return (new byte[] { 27, 91, 54, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, true, false)); // Alt+PageDown
            yield return (new byte[] { 27, 91, 54, 59, 55, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, true, true)); // Ctrl+Alt+PageDown
            yield return (new byte[] { 27, 79, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, false)); // LeftArrow
            yield return (new byte[] { 27, 91, 49, 59, 53, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, true)); // Ctrl+LeftArrow
            yield return (new byte[] { 27, 91, 49, 59, 51, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, true, false)); // Alt+LeftArrow
            yield return (new byte[] { 27, 91, 49, 59, 50, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, true, false, false)); // Shift+LeftArrow
            yield return (new byte[] { 27, 91, 49, 59, 52, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, true, true, false)); // Shift+Alt+LeftArrow
            yield return (new byte[] { 27, 79, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, false, false, false)); // UpArrow
            yield return (new byte[] { 27, 91, 49, 59, 53, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, false, false, true)); // Ctrl+UpArrow
            yield return (new byte[] { 27, 91, 49, 59, 51, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, false, true, false)); // Alt+UpArrow
            yield return (new byte[] { 27, 91, 49, 59, 50, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, true, false, false)); // Shift+UpArrow
            yield return (new byte[] { 27, 91, 49, 59, 52, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, true, true, false)); // Shift+Alt+UpArrow
            yield return (new byte[] { 27, 79, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, false, false, false)); // DownArrow
            yield return (new byte[] { 27, 91, 49, 59, 53, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, false, false, true)); // Ctrl+DownArrow
            yield return (new byte[] { 27, 91, 49, 59, 51, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, false, true, false)); // Alt+DownArrow
            yield return (new byte[] { 27, 91, 49, 59, 50, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, true, false, false)); // Shift+DownArrow
            yield return (new byte[] { 27, 91, 49, 59, 52, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, true, true, false)); // Shift+Alt+DownArrow
            yield return (new byte[] { 27, 79, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, false, false, false)); // RightArrow
            yield return (new byte[] { 27, 91, 49, 59, 53, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, false, false, true)); // Ctrl+RightArrow
            yield return (new byte[] { 27, 91, 49, 59, 51, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, false, true, false)); // Alt+RightArrow
            yield return (new byte[] { 27, 91, 49, 59, 50, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, true, false, false)); // Shift+RightArrow
            yield return (new byte[] { 27, 91, 49, 59, 52, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, true, true, false)); // Shift+Alt+RightArrow
            yield return (new byte[] { 27, 91, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Insert, false, false, false)); // Insert
            yield return (new byte[] { 27, 91, 50, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Insert, false, true, false)); // Alt+Insert
            yield return (new byte[] { 27, 91, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, false, false)); // Delete
            yield return (new byte[] { 27, 91, 51, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, false, true)); // Ctrl+Delete
            yield return (new byte[] { 27, 91, 51, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, true, false)); // Alt+Delete
            yield return (new byte[] { 27, 91, 51, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, true, false, false)); // Shift+Delete
            yield return (new byte[] { 27, 91, 51, 59, 54, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, true, false, true)); // Ctrl+Shift+Delete
            yield return (new byte[] { 27, 91, 51, 59, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, true, true, false)); // Alt+Shift+Delete
            yield return (new byte[] { 27, 91, 51, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, true, true, true)); // Ctrl+Alt+Shift+Delete
            // Numeric Keypad
            yield return (new byte[] { 48 }, new ConsoleKeyInfo('0', ConsoleKey.D0, false, false, false));
            yield return (new byte[] { 49 }, new ConsoleKeyInfo('1', ConsoleKey.D1, false, false, false));
            yield return (new byte[] { 50 }, new ConsoleKeyInfo('2', ConsoleKey.D2, false, false, false));
            yield return (new byte[] { 51 }, new ConsoleKeyInfo('3', ConsoleKey.D3, false, false, false));
            yield return (new byte[] { 52 }, new ConsoleKeyInfo('4', ConsoleKey.D4, false, false, false));
            yield return (new byte[] { 53 }, new ConsoleKeyInfo('5', ConsoleKey.D5, false, false, false));
            yield return (new byte[] { 54 }, new ConsoleKeyInfo('6', ConsoleKey.D6, false, false, false));
            yield return (new byte[] { 55 }, new ConsoleKeyInfo('7', ConsoleKey.D7, false, false, false));
            yield return (new byte[] { 56 }, new ConsoleKeyInfo('8', ConsoleKey.D8, false, false, false));
            yield return (new byte[] { 57 }, new ConsoleKeyInfo('9', ConsoleKey.D9, false, false, false));
            yield return (new byte[] { 47 }, new ConsoleKeyInfo('/', ConsoleKey.Divide, false, false, false));
            yield return (new byte[] { 42 }, new ConsoleKeyInfo('*', ConsoleKey.Multiply, false, false, false));
            yield return (new byte[] { 45 }, new ConsoleKeyInfo('-', ConsoleKey.Subtract, false, false, false));
            yield return (new byte[] { 43 }, new ConsoleKeyInfo('+', ConsoleKey.Add, false, false, false));
            yield return (new byte[] { 13 }, new ConsoleKeyInfo((char)13, ConsoleKey.Enter, false, false, false)); // Enter (using Numeric Keypad))
            yield return (new byte[] { 46 }, new ConsoleKeyInfo('.', ConsoleKey.OemPeriod, false, false, false)); // . (period using Numeric Keypad))
            yield return (new byte[] { 27, 91, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Insert, false, false, false)); // Insert
            yield return (new byte[] { 27, 91, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, false, false)); // Delete
            yield return (new byte[] { 27, 79, 70 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, false, false)); // End
            yield return (new byte[] { 27, 79, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, false, false, false)); // Down Arrow
            yield return (new byte[] { 27, 91, 54, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, false, false)); // Page Down
            yield return (new byte[] { 27, 79, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, false)); // Left Arrow
            yield return (new byte[] { 27, 79, 69 }, new ConsoleKeyInfo(default, ConsoleKey.NoName, false, false, false)); // Begin (5)
            yield return (new byte[] { 27, 79, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, false, false, false)); // Right Arrow
            yield return (new byte[] { 27, 79, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, false)); // Home
            yield return (new byte[] { 27, 79, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, false, false, false)); // Up Arrow
            yield return (new byte[] { 27, 91, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, false, false)); // Page Up
            yield return (new byte[] { 27, 79, 111 }, new ConsoleKeyInfo('/', ConsoleKey.Divide, false, false, false)); // / (divide sign using Numeric Keypad))
            yield return (new byte[] { 27, 79, 106 }, new ConsoleKeyInfo('*', ConsoleKey.Multiply, false, false, false)); // * (multiply sign using Numeric Keypad))
            yield return (new byte[] { 27, 79, 109 }, new ConsoleKeyInfo('-', ConsoleKey.Subtract, false, false, false)); // - (minus sign using Numeric Keypad))
            yield return (new byte[] { 27, 79, 107 }, new ConsoleKeyInfo('+', ConsoleKey.Add, false, false, false)); // + (plus sign using Numeric Keypad))
            yield return (new byte[] { 27, 79, 77 }, new ConsoleKeyInfo((char)13, ConsoleKey.Enter, false, false, false)); // Enter (using Numeric Keypad))
        }
    }
}

// Ubuntu 18.04 x64 (Press Ctrl+ALt+F6 to enter the text console, then switch back to GUI by pressing Ctrl+ALt+F1)
public class LinuxConsole : TerminalData
{
    protected override string EncodingCharset => "utf-8";
    protected override string Term => "linux";
    internal override byte Verase => 127;
    protected override string EncodedTerminalDb => "GgEUAB0AEAB9AUUDbGludXh8bGludXggY29uc29sZQAAAQAAAQEAAAAAAAAAAQEAAAAAAAEAAAAAAAABAQD//wgA/////////////////////////////wgAQAASAP//AAACAAQAFQAaACEAJQApAP//NABFAEcASwBXAP//WQBlAP//aQBtAHkAfQD/////gQCDAIgA/////40AkgD/////lwCcAKEApgCvALEA/////7YAuwDBAMcA////////////////2QDdAP//4QD////////jAP//6AD//////////+wA8QD3APwAAQEGAQsBEQEXAR0BIwEoAf//LQH//zEBNgE7Af///////z8B////////////////////////////////////////QwH//0YBTwFYAWEB//9qAXMBfAH//4UB//////////////////+OAf///////5QBlwGiAaUBpwGqAQEC//8EAv///////////////wYC//////////8KAv//TQL/////UQJXAv////9dAv////////////////////9hAv//////////////////////////////////////////////////ZgL//////////////////////////////////////////////////////////////////////////////////2gCbgJ0AnoCgAKGAowCkgKYAp4C//////////////////////////////////////////////////////////////////////////////////////////////////////////////////+kAv////////////////////////////////////////////////////////////+pArQCuQK/AsMCzALQAv//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////IQP///////8lAy8D////////////////////////////////////////////////OQM/AwcADQAbWyVpJXAxJWQ7JXAyJWRyABtbM2cAG1tIG1tKABtbSwAbW0oAG1slaSVwMSVkRwAbWyVpJXAxJWQ7JXAyJWRIAAoAG1tIABtbPzI1bBtbPzFjAAgAG1s/MjVoG1s/MGMAG1tDABtbQQAbWz8yNWgbWz84YwAbW1AAG1tNAA4AG1s1bQAbWzFtABtbMm0AG1s0aAAbWzdtABtbN20AG1s0bQAbWyVwMSVkWAAPABtbbQ8AG1s0bAAbWzI3bQAbWzI0bQAbWz81aCQ8MjAwLz4bWz81bAAbW0AAG1tMAH8AG1szfgAbW0IAG1tbQQAbWzIxfgAbW1tCABtbW0MAG1tbRAAbW1tFABtbMTd+ABtbMTh+ABtbMTl+ABtbMjB+ABtbMX4AG1syfgAbW0QAG1s2fgAbWzV+ABtbQwAbW0EADQoAG1slcDElZFAAG1slcDElZE0AG1slcDElZEIAG1slcDElZEAAG1slcDElZEwAG1slcDElZEQAG1slcDElZEMAG1slcDElZEEAG2MbXVIAGzgAG1slaSVwMSVkZAAbNwAKABtNABtbMDsxMCU/JXAxJXQ7NyU7JT8lcDIldDs0JTslPyVwMyV0OzclOyU/JXA0JXQ7NSU7JT8lcDUldDsyJTslPyVwNiV0OzElO20lPyVwOSV0DiVlDyU7ABtIAAkAG1tHACsrLCwtLS4uMDBfX2BgYWFmZmdnaGhpaWpqa2tsbG1tbm5vb3BwcXFycnNzdHR1dXZ2d3d4eHl5enp7e3x8fWN+fgAbW1oAG1s/N2gAG1s/N2wAGykwABtbNH4AGgAbWzIzfgAbWzI0fgAbWzI1fgAbWzI2fgAbWzI4fgAbWzI5fgAbWzMxfgAbWzMyfgAbWzMzfgAbWzM0fgAbWzFLABtbJWklZDslZFIAG1s2bgAbWz82YwAbW2MAG1szOTs0OW0AG11SABtdUCVwMSV4JXAyJXsyNTV9JSolezEwMDB9JS8lMDJ4JXAzJXsyNTV9JSolezEwMDB9JS8lMDJ4JXA0JXsyNTV9JSolezEwMDB9JS8lMDJ4ABtbTQAbWzMlcDElZG0AG1s0JXAxJWRtABtbMTFtABtbMTBtAAADAAEACwAaADwAAQAAAAEA//8AAP///////////////////////wAAAwAGAAkADAAPABIAFQAYAB4AJAAoACwAMAA0ABtbM0oAQVgARzAAWFQAVTgARTAARTMAUzAAWE0Aa0VORDUAa0hPTTUAa2EyAGtiMQBrYjMAa2MyAHhtAA=="; // /lib/terminfo/l/linux

    internal override IEnumerable<(byte[], ConsoleKeyInfo)> RecordedScenarios
    {
        get
        {
            yield return (new byte[] { 27, 91, 91, 65 }, new ConsoleKeyInfo(default, ConsoleKey.F1, false, false, false)); // F1
            yield return (new byte[] { 27, 91, 91, 66 }, new ConsoleKeyInfo(default, ConsoleKey.F2, false, false, false)); // F2
            yield return (new byte[] { 27, 91, 91, 67 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, false, false)); // F3
            yield return (new byte[] { 27, 91, 91, 68 }, new ConsoleKeyInfo(default, ConsoleKey.F4, false, false, false)); // F4
            yield return (new byte[] { 27, 91, 91, 69 }, new ConsoleKeyInfo(default, ConsoleKey.F5, false, false, false)); // F5
            yield return (new byte[] { 27, 91, 49, 55, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, false, false, false)); // F6
            yield return (new byte[] { 27, 91, 49, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, false, false, false)); // F7
            yield return (new byte[] { 27, 91, 49, 57, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, false, false, false)); // F8
            yield return (new byte[] { 27, 91, 50, 48, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, false, false)); // F9
            yield return (new byte[] { 27, 91, 50, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F10, false, false, false)); // F10
            yield return (new byte[] { 27, 91, 50, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, false, false)); // F11
            yield return (new byte[] { 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, false)); // F12
            yield return (new byte[] { 27, 91, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, false)); // Home
            yield return (new byte[] { 27, 91, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, false, false)); // End
            yield return (new byte[] { 27, 91, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, false, false)); // PageUp
            yield return (new byte[] { 27, 91, 54, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, false, false)); // PageDown
            yield return (new byte[] { 27, 91, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, false)); // LeftArrow
            yield return (new byte[] { 27, 91, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, false, false, false)); // UpArrow
            yield return (new byte[] { 27, 91, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, false, false, false)); // DownArrow
            yield return (new byte[] { 27, 91, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, false, false, false)); // RightArrow
            yield return (new byte[] { 27, 91, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Insert, false, false, false)); // Insert
            yield return (new byte[] { 27, 91, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, false, false)); // Delete
            // Numeric Keypad
            yield return (new byte[] { 48 }, new ConsoleKeyInfo('0', ConsoleKey.D0, false, false, false));
            yield return (new byte[] { 49 }, new ConsoleKeyInfo('1', ConsoleKey.D1, false, false, false));
            yield return (new byte[] { 50 }, new ConsoleKeyInfo('2', ConsoleKey.D2, false, false, false));
            yield return (new byte[] { 51 }, new ConsoleKeyInfo('3', ConsoleKey.D3, false, false, false));
            yield return (new byte[] { 52 }, new ConsoleKeyInfo('4', ConsoleKey.D4, false, false, false));
            yield return (new byte[] { 53 }, new ConsoleKeyInfo('5', ConsoleKey.D5, false, false, false));
            yield return (new byte[] { 54 }, new ConsoleKeyInfo('6', ConsoleKey.D6, false, false, false));
            yield return (new byte[] { 55 }, new ConsoleKeyInfo('7', ConsoleKey.D7, false, false, false));
            yield return (new byte[] { 56 }, new ConsoleKeyInfo('8', ConsoleKey.D8, false, false, false));
            yield return (new byte[] { 57 }, new ConsoleKeyInfo('9', ConsoleKey.D9, false, false, false));
            yield return (new byte[] { 47 }, new ConsoleKeyInfo('/', ConsoleKey.Divide, false, false, false));
            yield return (new byte[] { 42 }, new ConsoleKeyInfo('*', ConsoleKey.Multiply, false, false, false));
            yield return (new byte[] { 45 }, new ConsoleKeyInfo('-', ConsoleKey.Subtract, false, false, false));
            yield return (new byte[] { 43 }, new ConsoleKeyInfo('+', ConsoleKey.Add, false, false, false));
            yield return (new byte[] { 13 }, new ConsoleKeyInfo((char)13, ConsoleKey.Enter, false, false, false)); // Enter (using Numeric Keypad))
            yield return (new byte[] { 46 }, new ConsoleKeyInfo('.', ConsoleKey.OemPeriod, false, false, false)); // . (period using Numeric Keypad))
            yield return (new byte[] { 27, 91, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Insert, false, false, false)); // Insert
            yield return (new byte[] { 27, 91, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, false, false)); // Delete
            yield return (new byte[] { 27, 91, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, false, false)); // End
            yield return (new byte[] { 27, 91, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, false, false, false)); // Down Arrow
            yield return (new byte[] { 27, 91, 54, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, false, false)); // Page Down
            yield return (new byte[] { 27, 91, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, false)); // Left Arrow
            yield return (new byte[] { 27, 91, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, false, false, false)); // Right Arrow
            yield return (new byte[] { 27, 91, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, false)); // Home
            yield return (new byte[] { 27, 91, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, false, false, false)); // Up Arrow
            yield return (new byte[] { 27, 91, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, false, false)); // Page Up
            yield return (new byte[] { 47 }, new ConsoleKeyInfo('/', ConsoleKey.Divide, false, false, false)); // / (divide sign using Numeric Keypad))
            yield return (new byte[] { 42 }, new ConsoleKeyInfo('*', ConsoleKey.Multiply, false, false, false)); // * (multiply sign using Numeric Keypad))
            yield return (new byte[] { 45 }, new ConsoleKeyInfo('-', ConsoleKey.Subtract, false, false, false)); // - (minus sign using Numeric Keypad))
            yield return (new byte[] { 43 }, new ConsoleKeyInfo('+', ConsoleKey.Add, false, false, false)); // + (plus sign using Numeric Keypad))
            yield return (new byte[] { 13 }, new ConsoleKeyInfo((char)13, ConsoleKey.Enter, false, false, false)); // Enter (using Numeric Keypad))
        }
    }
}

// Windows 11 machine connected via PuTTY to Ubuntu 20.04 arm64 machine using TERM=xterm (default)
public class PuTTYData_xterm : TerminalData
{
    protected override string EncodingCharset => "";
    protected override string Term => "xterm";
    internal override byte Verase => 127;
    protected override string EncodedTerminalDb => "GgEpACYADwCdAaQFeHRlcm18eHRlcm0tZGViaWFufFgxMSB0ZXJtaW5hbCBlbXVsYXRvcgAAAQAAAQAAAAEAAAAAAQEAAAAAAAAAAQAAAQAAAQAAAAAAAAAAAQBQAAgAGAD//////////////////////////wgAQAAAAAQABgAIABkAHgAmACoALgD//zkASgBMAFAAVwD//1kAZgD//2oAbgB4AHwA/////4AAhACJAI4A//+gAKUAqgD//68AtAC5AL4AxwDLANIA///kAOkA7wD1AP///////wcB////////GQH//x0B////////HwH//yQB//////////8oASwBMgE2AToBPgFEAUoBUAFWAVwBYAH//2UB//9pAW4BcwF3AX4B//+FAYkBkQH/////////////////////////////mQGiAf////+rAbQBvQHGAc8B2AHhAeoB8wH8Af///////wUCCQIOAv//EwIWAv////8oAisCNgI5AjsCPgKbAv//ngL///////////////+gAv//////////pAL//9kC/////90C4wL/////////////////////////////6QLtAv//////////////////////////////////////////////////////////////////8QL/////+AL///////////8CBgMNA/////8UA///GwP///////8iA/////////////8pAy8DNQM8A0MDSgNRA1kDYQNpA3EDeQOBA4kDkQOYA58DpgOtA7UDvQPFA80D1QPdA+UD7QP0A/sDAgQJBBEEGQQhBCkEMQQ5BEEESQRQBFcEXgRlBG0EdQR9BIUEjQSVBJ0EpQSsBLMEugT/////////////////////////////////////////////////////////////vwTKBM8E4gTmBP//////////7wQ1Bf//////////////////ewX///////////////////////+ABf///////////////////////////////////////////////////////////////////////////////////////4YF////////igWUBf//////////////////////////////////////////////////////////////////////////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"; // /lib/terminfo/x/xterm

    internal override IEnumerable<(byte[], ConsoleKeyInfo)> RecordedScenarios => Array.Empty<(byte[], ConsoleKeyInfo)>();
}

// Windows 11 machine connected via PuTTY to Ubuntu 20.04 arm64 machine using TERM=linux
public class PuTTYData_linux : TerminalData
{
    protected override string EncodingCharset => "";
    protected override string Term => "linux";
    internal override byte Verase => 127;
    protected override string EncodedTerminalDb => "GgEUAB0AEAB9AUUDbGludXh8bGludXggY29uc29sZQAAAQAAAQEAAAAAAAAAAQEAAAAAAAEAAAAAAAABAQD//wgA/////////////////////////////wgAQAASAP//AAACAAQAFQAaACEAJQApAP//NABFAEcASwBXAP//WQBlAP//aQBtAHkAfQD/////gQCDAIgA/////40AkgD/////lwCcAKEApgCvALEA/////7YAuwDBAMcA////////////////2QDdAP//4QD////////jAP//6AD//////////+wA8QD3APwAAQEGAQsBEQEXAR0BIwEoAf//LQH//zEBNgE7Af///////z8B////////////////////////////////////////QwH//0YBTwFYAWEB//9qAXMBfAH//4UB//////////////////+OAf///////5QBlwGiAaUBpwGqAQEC//8EAv///////////////wYC//////////8KAv//TQL/////UQJXAv////9dAv////////////////////9hAv//////////////////////////////////////////////////ZgL//////////////////////////////////////////////////////////////////////////////////2gCbgJ0AnoCgAKGAowCkgKYAp4C//////////////////////////////////////////////////////////////////////////////////////////////////////////////////+kAv////////////////////////////////////////////////////////////+pArQCuQK/AsMCzALQAv//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////IQP///////8lAy8D////////////////////////////////////////////////OQM/AwcADQAbWyVpJXAxJWQ7JXAyJWRyABtbM2cAG1tIG1tKABtbSwAbW0oAG1slaSVwMSVkRwAbWyVpJXAxJWQ7JXAyJWRIAAoAG1tIABtbPzI1bBtbPzFjAAgAG1s/MjVoG1s/MGMAG1tDABtbQQAbWz8yNWgbWz84YwAbW1AAG1tNAA4AG1s1bQAbWzFtABtbMm0AG1s0aAAbWzdtABtbN20AG1s0bQAbWyVwMSVkWAAPABtbbQ8AG1s0bAAbWzI3bQAbWzI0bQAbWz81aCQ8MjAwLz4bWz81bAAbW0AAG1tMAH8AG1szfgAbW0IAG1tbQQAbWzIxfgAbW1tCABtbW0MAG1tbRAAbW1tFABtbMTd+ABtbMTh+ABtbMTl+ABtbMjB+ABtbMX4AG1syfgAbW0QAG1s2fgAbWzV+ABtbQwAbW0EADQoAG1slcDElZFAAG1slcDElZE0AG1slcDElZEIAG1slcDElZEAAG1slcDElZEwAG1slcDElZEQAG1slcDElZEMAG1slcDElZEEAG2MbXVIAGzgAG1slaSVwMSVkZAAbNwAKABtNABtbMDsxMCU/JXAxJXQ7NyU7JT8lcDIldDs0JTslPyVwMyV0OzclOyU/JXA0JXQ7NSU7JT8lcDUldDsyJTslPyVwNiV0OzElO20lPyVwOSV0DiVlDyU7ABtIAAkAG1tHACsrLCwtLS4uMDBfX2BgYWFmZmdnaGhpaWpqa2tsbG1tbm5vb3BwcXFycnNzdHR1dXZ2d3d4eHl5enp7e3x8fWN+fgAbW1oAG1s/N2gAG1s/N2wAGykwABtbNH4AGgAbWzIzfgAbWzI0fgAbWzI1fgAbWzI2fgAbWzI4fgAbWzI5fgAbWzMxfgAbWzMyfgAbWzMzfgAbWzM0fgAbWzFLABtbJWklZDslZFIAG1s2bgAbWz82YwAbW2MAG1szOTs0OW0AG11SABtdUCVwMSV4JXAyJXsyNTV9JSolezEwMDB9JS8lMDJ4JXAzJXsyNTV9JSolezEwMDB9JS8lMDJ4JXA0JXsyNTV9JSolezEwMDB9JS8lMDJ4ABtbTQAbWzMlcDElZG0AG1s0JXAxJWRtABtbMTFtABtbMTBtAAABAAEAAQAEAA4AAQABAAAAAAADAAYAG1szSgBBWABVOABFMwA="; // /lib/terminfo/l/linux

    internal override IEnumerable<(byte[], ConsoleKeyInfo)> RecordedScenarios => Array.Empty<(byte[], ConsoleKeyInfo)>();
}

// Windows 11 machine connected via PuTTY to Ubuntu 20.04 arm64 machine using TERM=putty
public class PuTTYData_putty : TerminalData
{
    protected override string EncodingCharset => "";
    protected override string Term => "putty";
    internal override byte Verase => 127;
    protected override string EncodedTerminalDb => "GgEeAB0AEAB9AXwEcHV0dHl8UHVUVFkgdGVybWluYWwgZW11bGF0b3IAAQEAAAEAAAAAAQAAAAEBAAAAAAABAAAAAAAAAQEA//8IAP////////////////////////////8IAEAAFgAAAAQABgAIABkAHgAlACkALQD//zgASQBMAFAAVwD//1kAYAD//2QA//9nAGsAbwD//3UAdwB8AIEA/////4gA/////40AkgCXAJwApQCnAKwA//+3ALwAwgDIAP//2gD//9wA/////////gD//wIB////////BAH//wkB//////////8NARMBGQEfASUBKwExATcBPQFDAUkBTgH//1MB//9XAVwBYQFlAWkB//9tAXEBeQH//////////////////////////////////4EB//+EAY0BlgH//58BqAGxAboBwwHMAf/////////////////////VAf/////2AfkBBAIHAgkCDAJUAv//VwJZAv////////////9eAv//////////YgL//5UC/////5kCnwL/////pQL/////////////////////rAL//////////////////////////////////////////////////7EC//////////////////////////////////////////+zAv////////////////////+3Av////////////+7AsECxwLNAtMC2QLfAuUC6wLxAv//////////////////////////////////////////////////////////////////////////////////////////////////////////////////9wL//////////////////////////////////////////////////////////////AIHAwwDEgMWAx8DIwP//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////3QD////////eAOCA////////4wDkgOYA/////////////////////////////+eA3AEdgQbW1oABwANABtbJWklcDElZDslcDIlZHIAG1szZwAbW0gbW0oAG1tLABtbSgAbWyVpJXAxJWRHABtbJWklcDElZDslcDIlZEgAG0QAG1tIABtbPzI1bAAIABtbPzI1aAAbW0MAG00AG1tQABtbTQAbXTA7BwAOABtbNW0AG1sxbQAbWz80N2gAG1s0aAAbWzdtABtbN20AG1s0bQAbWyVwMSVkWAAPABtbbQ8AG1syShtbPzQ3bAAbWzRsABtbMjdtABtbMjRtABtbPzVoJDwxMDAvPhtbPzVsAAcAGzcbW3IbW20bWz83aBtbPzE7NDs2bBtbNGwbOBs+G11SABtbTAB/ABtbM34AG09CABtbMTF+ABtbMjF+ABtbMTJ+ABtbMTN+ABtbMTR+ABtbMTV+ABtbMTd+ABtbMTh+ABtbMTl+ABtbMjB+ABtbMX4AG1syfgAbT0QAG1s2fgAbWzV+ABtPQwAbW0IAG1tBABtPQQAbWz8xbBs+ABtbPzFoGz0ADQoAG1slcDElZFAAG1slcDElZE0AG1slcDElZEIAG1slcDElZFMAG1slcDElZEwAG1slcDElZEQAG1slcDElZEMAG1slcDElZFQAG1slcDElZEEAGzwbWyJwG1s1MDs2InAbYxtbPzNsG11SG1s/MTAwMGwAGzgAG1slaSVwMSVkZAAbNwAKABtNABtbMCU/JXAxJXA2JXwldDsxJTslPyVwMiV0OzQlOyU/JXAxJXAzJXwldDs3JTslPyVwNCV0OzUlO20lPyVwOSV0DiVlDyU7ABtIAAkAG10wOwAbW0cAYGBhYWZmZ2dqamtrbGxtbW5ub29wcHFxcnJzc3R0dXV2dnd3eHh5eXp6e3t8fH19fn4AG1taABtbPzdoABtbPzdsABsoQhspMAAbWzR+ABoAG1tEABtbQwAbWzIzfgAbWzI0fgAbWzI1fgAbWzI2fgAbWzI4fgAbWzI5fgAbWzMxfgAbWzMyfgAbWzMzfgAbWzM0fgAbWzFLABtbJWklZDslZFIAG1s2bgAbWz82YwAbW2MAG1szOTs0OW0AG11SABtdUCVwMSV4JXAyJXsyNTV9JSolezEwMDB9JS8lMDJ4JXAzJXsyNTV9JSolezEwMDB9JS8lMDJ4JXA0JXsyNTV9JSolezEwMDB9JS8lMDJ4ABtbPAAbWzMlcDElZG0AG1s0JXAxJWRtABtbMTBtABtbMTFtABtbMTJtACU/JXAxJXs4fSU9JXQbJSVH4peYGyUlQCVlJXAxJXsxMH0lPSV0GyUlR+KXmRslJUAlZSVwMSV7MTJ9JT0ldBslJUfimYAbJSVAJWUlcDElezEzfSU9JXQbJSVH4pmqGyUlQCVlJXAxJXsxNH0lPSV0GyUlR+KZqxslJUAlZSVwMSV7MTV9JT0ldBslJUfimLwbJSVAJWUlcDElezI3fSU9JXQbJSVH4oaQGyUlQCVlJXAxJXsxNTV9JT0ldBslJUfggqIbJSVAJWUlcDElYyU7ABtbMTFtABtbMTBtAAEAAQAEAAoAYQABAAEAAAAFAAoAKgAAAAMABgAJAAwADwAbWzNKABtdMDsAG1s/MTAwNjsxMDAwJT8lcDElezF9JT0ldGglZWwlOwAbWzwlaSVwMyVkOyVwMSVkOyVwMiVkOyU/JXA0JXRNJWVtJTsAWFQAVTgARTMAVFMAWE0AeG0A"; // /usr/share/terminfo/p/putty

    internal override IEnumerable<(byte[], ConsoleKeyInfo)> RecordedScenarios => Array.Empty<(byte[], ConsoleKeyInfo)>();
}

// Windows (11) Terminal connected via SSH to Ubuntu 20.04 arm64
public class WindowsTerminalData : TerminalData
{
    protected override string EncodingCharset => "";
    protected override string Term => "xterm-256color";
    internal override byte Verase => 127;
    protected override string EncodedTerminalDb => "HgIlACYADwCdAe4FeHRlcm0tMjU2Y29sb3J8eHRlcm0gd2l0aCAyNTYgY29sb3JzAAABAAABAAAAAQAAAAABAQAAAAAAAAABAAABAAEBAAAAAAAAAAABAFAAAAAIAAAAGAAAAP////////////////////////////////////////////////////8AAQAAAAABAAAABAAGAAgAGQAeACYAKgAuAP//OQBKAEwAUABXAP//WQBmAP//agBuAHgAfAD/////gACEAIkAjgD//6AApQCqAP//rwC0ALkAvgDHAMsA0gD//+QA6QDvAPUA////////BwH///////8ZAf//HQH///////8fAf//JAH//////////ygBLAEyATYBOgE+AUQBSgFQAVYBXAFgAf//ZQH//2kBbgFzAXcBfgH//4UBiQGRAf////////////////////////////+ZAaIB/////6sBtAG9AcYBzwHYAeEB6gHzAfwB////////BQIJAg4C//8TAhwC/////y4CMQI8Aj8CQQJEAqEC//+kAv///////////////6YC//////////+qAv//3wL/////4wLpAv/////////////////////////////vAvMC///////////////////////////////////////////////////////////////////3Av/////+Av//////////BQMMAxMD/////xoD//8hA////////ygD/////////////y8DNQM7A0IDSQNQA1cDXwNnA28DdwN/A4cDjwOXA54DpQOsA7MDuwPDA8sD0wPbA+MD6wPzA/oDAQQIBA8EFwQfBCcELwQ3BD8ERwRPBFYEXQRkBGsEcwR7BIMEiwSTBJsEowSrBLIEuQTABP/////////////////////////////////////////////////////////////FBNAE1QToBOwE9QT8BP////////////////////////////9aBf///////////////////////18F////////////////////////////////////////////////////////////////////////////////////////ZQX///////9pBagF/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////+gF6wUbW1oABwANABtbJWklcDElZDslcDIlZHIAG1szZwAbW0gbWzJKABtbSwAbW0oAG1slaSVwMSVkRwAbWyVpJXAxJWQ7JXAyJWRIAAoAG1tIABtbPzI1bAAIABtbPzEybBtbPzI1aAAbW0MAG1tBABtbPzEyOzI1aAAbW1AAG1tNABsoMAAbWzVtABtbMW0AG1s/MTA0OWgbWzIyOzA7MHQAG1sybQAbWzRoABtbOG0AG1s3bQAbWzdtABtbNG0AG1slcDElZFgAGyhCABsoQhtbbQAbWz8xMDQ5bBtbMjM7MDswdAAbWzRsABtbMjdtABtbMjRtABtbPzVoJDwxMDAvPhtbPzVsABtbIXAbWz8zOzRsG1s0bBs+ABtbTAB/ABtbM34AG09CABtPUAAbWzIxfgAbT1EAG09SABtPUwAbWzE1fgAbWzE3fgAbWzE4fgAbWzE5fgAbWzIwfgAbT0gAG1syfgAbT0QAG1s2fgAbWzV+ABtPQwAbWzE7MkIAG1sxOzJBABtPQQAbWz8xbBs+ABtbPzFoGz0AG1s/MTAzNGwAG1s/MTAzNGgAG1slcDElZFAAG1slcDElZE0AG1slcDElZEIAG1slcDElZEAAG1slcDElZFMAG1slcDElZEwAG1slcDElZEQAG1slcDElZEMAG1slcDElZFQAG1slcDElZEEAG1tpABtbNGkAG1s1aQAbYxtdMTA0BwAbWyFwG1s/Mzs0bBtbNGwbPgAbOAAbWyVpJXAxJWRkABs3AAoAG00AJT8lcDkldBsoMCVlGyhCJTsbWzAlPyVwNiV0OzElOyU/JXA1JXQ7MiU7JT8lcDIldDs0JTslPyVwMSVwMyV8JXQ7NyU7JT8lcDQldDs1JTslPyVwNyV0OzglO20AG0gACQAbT0UAYGBhYWZmZ2dpaWpqa2tsbG1tbm5vb3BwcXFycnNzdHR1dXZ2d3d4eHl5enp7e3x8fX1+fgAbW1oAG1s/N2gAG1s/N2wAG09GABtPTQAbWzM7Mn4AG1sxOzJGABtbMTsySAAbWzI7Mn4AG1sxOzJEABtbNjsyfgAbWzU7Mn4AG1sxOzJDABtbMjN+ABtbMjR+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"; // /lib/terminfo/x/xterm-256color

    internal override IEnumerable<(byte[], ConsoleKeyInfo)> RecordedScenarios
    {
        get
        {
            yield return (new byte[] { 90 }, new ConsoleKeyInfo('Z', ConsoleKey.Z, true, false, false));
            yield return (new byte[] { 97 }, new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false));
            yield return (new byte[] { 1 }, new ConsoleKeyInfo(default, ConsoleKey.A, false, false, true));
            yield return (new byte[] { 27, 97 }, new ConsoleKeyInfo('a', ConsoleKey.A, false, true, false));
            yield return (new byte[] { 27, 1 }, new ConsoleKeyInfo(default, ConsoleKey.A, false, true, true));
            yield return (new byte[] { 49 }, new ConsoleKeyInfo('1', ConsoleKey.D1, false, false, false));
            yield return (new byte[] { 27, 49 }, new ConsoleKeyInfo('1', ConsoleKey.D1, false, true, false));
            yield return (new byte[] { 33 }, new ConsoleKeyInfo('!', default, false, false, false));
            yield return (new byte[] { 50 }, new ConsoleKeyInfo('2', ConsoleKey.D2, false, false, false));
            yield return (new byte[] { 27, 50 }, new ConsoleKeyInfo('2', ConsoleKey.D2, false, true, false));
            yield return (new byte[] { 64 }, new ConsoleKeyInfo('@', default, false, false, false));
            yield return (new byte[] { 61 }, new ConsoleKeyInfo('=', default, false, false, false));
            yield return (new byte[] { 43 }, new ConsoleKeyInfo('+', ConsoleKey.Add, false, false, false));
            yield return (new byte[] { 27, 61 }, new ConsoleKeyInfo('=', default, false, false, false));
            yield return (new byte[] { 27 }, new ConsoleKeyInfo((char)27, ConsoleKey.Escape, false, false, false));
            yield return (new byte[] { 127 }, new ConsoleKeyInfo((char)127, ConsoleKey.Backspace, false, false, false)); // verase
            yield return (new byte[] { 27, 127 }, new ConsoleKeyInfo((char)127, ConsoleKey.Backspace, false, true, false));
            yield return (new byte[] { 49 }, new ConsoleKeyInfo('1', ConsoleKey.D1, false, false, false));
            yield return (new byte[] { 43 }, new ConsoleKeyInfo('+', ConsoleKey.Add, false, false, false));
            yield return (new byte[] { 45 }, new ConsoleKeyInfo('-', ConsoleKey.Subtract, false, false, false));

            yield return (new byte[] { 27, 79, 80 }, new ConsoleKeyInfo(default, ConsoleKey.F1, false, false, false)); // F1
            yield return (new byte[] { 27, 79, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F2, false, false, false)); // F2
            yield return (new byte[] { 27, 79, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, false, false)); // F3
            yield return (new byte[] { 27, 79, 83 }, new ConsoleKeyInfo(default, ConsoleKey.F4, false, false, false)); // F4
            yield return (new byte[] { 27, 91, 49, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, false, false, false)); // F5
            yield return (new byte[] { 27, 91, 49, 55, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, false, false, false)); // F6
            yield return (new byte[] { 27, 91, 49, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, false, false, false)); // F7
            yield return (new byte[] { 27, 91, 49, 57, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, false, false, false)); // F8
            yield return (new byte[] { 27, 91, 50, 48, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, false, false)); // F9
            yield return (new byte[] { 27, 91, 50, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F10, false, false, false)); // F10
            yield return (new byte[] { 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, false)); // F12
            yield return (new byte[] { 27, 91, 49, 59, 53, 80 }, new ConsoleKeyInfo(default, ConsoleKey.F1, false, false, true)); // Ctrl+F1
            yield return (new byte[] { 27, 91, 49, 59, 53, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F2, false, false, true)); // Ctrl+F2
            yield return (new byte[] { 27, 91, 49, 59, 53, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, false, true)); // Ctrl+F3
            yield return (new byte[] { 27, 91, 49, 59, 53, 83 }, new ConsoleKeyInfo(default, ConsoleKey.F4, false, false, true)); // Ctrl+F4
            yield return (new byte[] { 27, 91, 49, 53, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, false, false, true)); // Ctrl+F5
            yield return (new byte[] { 27, 91, 49, 55, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, false, false, true)); // Ctrl+F6
            yield return (new byte[] { 27, 91, 49, 56, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, false, false, true)); // Ctrl+F7
            yield return (new byte[] { 27, 91, 49, 57, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, false, false, true)); // Ctrl+F8
            yield return (new byte[] { 27, 91, 50, 48, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, false, true)); // Ctrl+F9
            yield return (new byte[] { 27, 91, 50, 49, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F10, false, false, true)); // Ctrl+F10
            yield return (new byte[] { 27, 91, 50, 51, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, false, true)); // Ctrl+F11
            yield return (new byte[] { 27, 91, 50, 52, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, true)); // Ctrl+F12
            yield return (new byte[] { 27, 91, 49, 59, 51, 80 }, new ConsoleKeyInfo(default, ConsoleKey.F1, false, true, false)); // Alt+F1
            yield return (new byte[] { 27, 91, 49, 59, 51, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F2, false, true, false)); // Alt+F2
            yield return (new byte[] { 27, 91, 49, 59, 51, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, true, false)); // Alt+F3
            yield return (new byte[] { 27, 91, 49, 53, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, false, true, false)); // Alt+F5
            yield return (new byte[] { 27, 91, 49, 55, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, false, true, false)); // Alt+F6
            yield return (new byte[] { 27, 91, 49, 56, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, false, true, false)); // Alt+F7
            yield return (new byte[] { 27, 91, 49, 57, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, false, true, false)); // Alt+F8
            yield return (new byte[] { 27, 91, 50, 48, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, true, false)); // Alt+F9
            yield return (new byte[] { 27, 91, 50, 49, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F10, false, true, false)); // Alt+F10
            yield return (new byte[] { 27, 91, 50, 51, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, true, false)); // Alt+F11
            yield return (new byte[] { 27, 91, 50, 52, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, true, false)); // Alt+F12
            yield return (new byte[] { 27, 91, 49, 59, 50, 80 }, new ConsoleKeyInfo(default, ConsoleKey.F1, true, false, false)); // Shift+F1
            yield return (new byte[] { 27, 91, 49, 59, 50, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F2, true, false, false)); // Shift+F2
            yield return (new byte[] { 27, 91, 49, 59, 50, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, true, false, false)); // Shift+F3
            yield return (new byte[] { 27, 91, 49, 59, 50, 83 }, new ConsoleKeyInfo(default, ConsoleKey.F4, true, false, false)); // Shift+F4
            yield return (new byte[] { 27, 91, 49, 53, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, true, false, false)); // Shift+F5
            yield return (new byte[] { 27, 91, 49, 55, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, true, false, false)); // Shift+F6
            yield return (new byte[] { 27, 91, 49, 56, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, true, false, false)); // Shift+F7
            yield return (new byte[] { 27, 91, 49, 57, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, true, false, false)); // Shift+F8
            yield return (new byte[] { 27, 91, 50, 48, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, true, false, false)); // Shift+F9
            yield return (new byte[] { 27, 91, 50, 49, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F10, true, false, false)); // Shift+F10
            yield return (new byte[] { 27, 91, 50, 51, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, true, false, false)); // Shift+F11
            yield return (new byte[] { 27, 91, 50, 52, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, true, false, false)); // Shift+F12
            yield return (new byte[] { 27, 91, 49, 59, 56, 80 }, new ConsoleKeyInfo(default, ConsoleKey.F1, true, true, true)); // Ctrl+Alt+Shift+F1
            yield return (new byte[] { 27, 91, 49, 59, 56, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F2, true, true, true)); // Ctrl+Alt+Shift+F2
            yield return (new byte[] { 27, 91, 49, 59, 56, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, true, true, true)); // Ctrl+Alt+Shift+F3
            yield return (new byte[] { 27, 91, 49, 59, 56, 83 }, new ConsoleKeyInfo(default, ConsoleKey.F4, true, true, true)); // Ctrl+Alt+Shift+F4
            yield return (new byte[] { 27, 91, 49, 53, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, true, true, true)); // Ctrl+Alt+Shift+F5
            yield return (new byte[] { 27, 91, 49, 55, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, true, true, true)); // Ctrl+Alt+Shift+F6
            yield return (new byte[] { 27, 91, 49, 56, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, true, true, true)); // Ctrl+Alt+Shift+F7
            yield return (new byte[] { 27, 91, 49, 57, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, true, true, true)); // Ctrl+Alt+Shift+F8
            yield return (new byte[] { 27, 91, 50, 48, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, true, true, true)); // Ctrl+Alt+Shift+F9
            yield return (new byte[] { 27, 91, 50, 49, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F10, true, true, true)); // Ctrl+Alt+Shift+F10
            yield return (new byte[] { 27, 91, 50, 51, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, true, true, true)); // Ctrl+Alt+Shift+F11
            yield return (new byte[] { 27, 91, 50, 52, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, true, true, true)); // Ctrl+Alt+Shift+F12
            yield return (new byte[] { 27, 79, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, false)); // Home
            yield return (new byte[] { 27, 91, 49, 59, 53, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, true)); // Ctrl+Home
            yield return (new byte[] { 27, 91, 49, 59, 51, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, true, false)); // Alt+Home
            yield return (new byte[] { 27, 91, 49, 59, 55, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, true, true)); // Ctrl+Alt+Home
            yield return (new byte[] { 27, 79, 70 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, false, false)); // End
            yield return (new byte[] { 27, 91, 49, 59, 53, 70 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, false, true)); // Ctrl+End
            yield return (new byte[] { 27, 91, 49, 59, 51, 70 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, true, false)); // Alt+End
            yield return (new byte[] { 27, 91, 49, 59, 55, 70 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, true, true)); // Ctrl+Alt+End
            yield return (new byte[] { 27, 91, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, false, false)); // PageUp
            yield return (new byte[] { 27, 91, 53, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, false, true)); // Ctrl+PageUp
            yield return (new byte[] { 27, 91, 53, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, true, false)); // Alt+PageUp
            yield return (new byte[] { 27, 91, 53, 59, 55, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, true, true)); // Ctrl+Alt+PageUp
            yield return (new byte[] { 27, 91, 54, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, false, false)); // PageDown
            yield return (new byte[] { 27, 91, 54, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, false, true)); // Ctrl+PageDown
            yield return (new byte[] { 27, 91, 54, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, true, false)); // Alt+PageDown
            yield return (new byte[] { 27, 91, 54, 59, 55, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, true, true)); // Ctrl+Alt+PageDown
            yield return (new byte[] { 27, 79, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, false)); // LeftArrow
            yield return (new byte[] { 27, 91, 49, 59, 53, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, true)); // Ctrl+LeftArrow
            yield return (new byte[] { 27, 91, 49, 59, 51, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, true, false)); // Alt+LeftArrow
            yield return (new byte[] { 27, 79, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, false, false, false)); // UpArrow
            yield return (new byte[] { 27, 91, 49, 59, 53, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, false, false, true)); // Ctrl+UpArrow
            yield return (new byte[] { 27, 91, 49, 59, 51, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, false, true, false)); // Alt+UpArrow
            yield return (new byte[] { 27, 91, 49, 59, 50, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, true, false, false)); // Shift+UpArrow
            yield return (new byte[] { 27, 79, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, false, false, false)); // DownArrow
            yield return (new byte[] { 27, 91, 49, 59, 53, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, false, false, true)); // Ctrl+DownArrow
            yield return (new byte[] { 27, 91, 49, 59, 51, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, false, true, false)); // Alt+DownArrow
            yield return (new byte[] { 27, 91, 49, 59, 50, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, true, false, false)); // Shift+DownArrow
            yield return (new byte[] { 27, 79, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, false, false, false)); // RightArrow
            yield return (new byte[] { 27, 91, 49, 59, 53, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, false, false, true)); // Ctrl+RightArrow
            yield return (new byte[] { 27, 91, 49, 59, 51, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, false, true, false)); // Alt+RightArrow
            yield return (new byte[] { 27, 91, 49, 59, 50, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, true, false, false)); // Shift+RightArrow
            yield return (new byte[] { 27, 91, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Insert, false, false, false)); // Insert
            yield return (new byte[] { 27, 91, 50, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Insert, false, true, false)); // Alt+Insert
            yield return (new byte[] { 27, 91, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, false, false)); // Delete
            yield return (new byte[] { 27, 91, 51, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, false, true)); // Ctrl+Delete
            yield return (new byte[] { 27, 91, 51, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, true, false)); // Alt+Delete
            yield return (new byte[] { 27, 91, 51, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, true, false, false)); // Shift+Delete
            yield return (new byte[] { 27, 91, 51, 59, 54, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, true, false, true)); // Ctrl+Shift+Delete
            yield return (new byte[] { 27, 91, 51, 59, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, true, true, false)); // Alt+Shift+Delete
            yield return (new byte[] { 27, 91, 51, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, true, true, true)); // Ctrl+Alt+Shift+Delete
        }
    }
}

// Ubuntu 18.04 x64
public class RxvtUnicode : TerminalData
{
    protected override string EncodingCharset => "utf-8";
    protected override string Term => "rxvt-unicode-256color";
    internal override byte Verase => 127;
    protected override string EncodedTerminalDb => "GgFOAB0AHwBwASkFcnh2dC11bmljb2RlLTI1NmNvbG9yfHJ4dnQtdW5pY29kZSB0ZXJtaW5hbCB3aXRoIDI1NiBjb2xvcnMgKFggV2luZG93IFN5c3RlbSkAAQEAAAEBAAABAQAAAAEBAAAAAAABAAEAAAEAAQEAUAAIABgAAAD///////////////////////8AAf9/AAD/////////////////////////////////////BQD//wAAAgAEABUAGgAiACYAKgD//zUARgBIAEwAUwD//1UAYgD//2YAagB0AHgAfAD//4IAhgCLAJAA/////5kA/////54AowCoAK0AtgC6AMEA///NANIA2ADeAP//7wDxAPYA/////y4BMgH//zYB////////OAH//z0B//9BAf////9GAUwBUgFYAV4BZAFqAXABdgF8AYIBhwH//4wB//+QAZUBmgH///////+eAaIBpQH///////////////////////////////////////+oAbEBugHDAcwB1QHeAecB8AH5Af///////wICBgILAv//EAITAv////9HAkoCVQJYAloCXQKuAv//sQKzAv///////7gCvALAAsQCyAL/////zAL//w0D/////xEDFwP/////HQP/////////////////////HgMjA///JwP/////////////////////////////////////////////////////////////LAP//zEDNgP/////OwP//0ADRQNKA/////9OA///UwP///////9YA/////////////9cA2IDaANuA3QDegOAA4YDjAOSA///////////////////////////////////////////////////////////////////////////////////////////////////////////////////mAP/////////////////////////////////////////////////////////////nQOoA60DtQO5A///wgP/////JgSKBP//////////////////7gT////////////////////////zBP////////////////////////////////////////////////////////////////////////////////////////kE////////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"; // /lib/terminfo/r/rxvt-unicode-256color

    internal override IEnumerable<(byte[], ConsoleKeyInfo)> RecordedScenarios
    {
        get
        {
            yield return (new byte[] { 27, 91, 49, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F1, false, false, false)); // F1
            yield return (new byte[] { 27, 91, 49, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F2, false, false, false)); // F2
            yield return (new byte[] { 27, 91, 49, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, false, false)); // F3
            yield return (new byte[] { 27, 91, 49, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F4, false, false, false)); // F4
            yield return (new byte[] { 27, 91, 49, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, false, false, false)); // F5
            yield return (new byte[] { 27, 91, 49, 55, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, false, false, false)); // F6
            yield return (new byte[] { 27, 91, 49, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, false, false, false)); // F7
            yield return (new byte[] { 27, 91, 49, 57, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, false, false, false)); // F8
            yield return (new byte[] { 27, 91, 50, 48, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, false, false)); // F9
            yield return (new byte[] { 27, 91, 50, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F10, false, false, false)); // F10
            yield return (new byte[] { 27, 91, 50, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, false, false)); // F11
            yield return (new byte[] { 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, false)); // F12
            yield return (new byte[] { 27, 91, 49, 49, 94 }, new ConsoleKeyInfo(default, ConsoleKey.F1, false, false, true)); // Ctrl+F1
            yield return (new byte[] { 27, 91, 49, 50, 94 }, new ConsoleKeyInfo(default, ConsoleKey.F2, false, false, true)); // Ctrl+F2
            yield return (new byte[] { 27, 91, 49, 51, 94 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, false, true)); // Ctrl+F3
            yield return (new byte[] { 27, 91, 49, 52, 94 }, new ConsoleKeyInfo(default, ConsoleKey.F4, false, false, true)); // Ctrl+F4
            yield return (new byte[] { 27, 91, 49, 53, 94 }, new ConsoleKeyInfo(default, ConsoleKey.F5, false, false, true)); // Ctrl+F5
            yield return (new byte[] { 27, 91, 49, 55, 94 }, new ConsoleKeyInfo(default, ConsoleKey.F6, false, false, true)); // Ctrl+F6
            yield return (new byte[] { 27, 91, 49, 56, 94 }, new ConsoleKeyInfo(default, ConsoleKey.F7, false, false, true)); // Ctrl+F7
            yield return (new byte[] { 27, 91, 49, 57, 94 }, new ConsoleKeyInfo(default, ConsoleKey.F8, false, false, true)); // Ctrl+F8
            yield return (new byte[] { 27, 91, 50, 48, 94 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, false, true)); // Ctrl+F9
            yield return (new byte[] { 27, 91, 50, 49, 94 }, new ConsoleKeyInfo(default, ConsoleKey.F10, false, false, true)); // Ctrl+F10
            yield return (new byte[] { 27, 91, 50, 51, 94 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, false, true)); // Ctrl+F11
            yield return (new byte[] { 27, 91, 50, 52, 94 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, true)); // Ctrl+F12
            yield return (new byte[] { 27, 27, 91, 50, 48, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, true, false)); // Alt+F9
            yield return (new byte[] { 27, 27, 91, 50, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, true, false)); // Alt+F11
            yield return (new byte[] { 27, 91, 50, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, false, false)); // Shift+F1=F11
            yield return (new byte[] { 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, false)); // Shift+F2=F12
            yield return (new byte[] { 27, 91, 50, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F13, false, false, false)); // Shift+F3=F13
            yield return (new byte[] { 27, 91, 50, 54, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F14, false, false, false)); // Shift+F4=F14
            yield return (new byte[] { 27, 91, 50, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F15, false, false, false)); // Shift+F5=F14
            yield return (new byte[] { 27, 91, 50, 57, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F16, false, false, false)); // Shift+F6=F16
            yield return (new byte[] { 27, 91, 51, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F17, false, false, false)); // Shift+F7=F17
            yield return (new byte[] { 27, 91, 51, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F18, false, false, false)); // Shift+F8=F18
            yield return (new byte[] { 27, 91, 51, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F19, false, false, false)); // Shift+F9=F19
            yield return (new byte[] { 27, 91, 51, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F20, false, false, false)); // Shift+F10=F20
            yield return (new byte[] { 27, 91, 50, 51, 36 }, new ConsoleKeyInfo(default, ConsoleKey.F11, true, false, false)); // Shift+F11
            yield return (new byte[] { 27, 91, 50, 52, 36 }, new ConsoleKeyInfo(default, ConsoleKey.F12, true, false, false)); // Shift+F12
            yield return (new byte[] { 27, 27, 91, 50, 51, 94 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, true, true)); // Ctrl+Alt+Shift+F1=Ctrl+Alt+F11
            yield return (new byte[] { 27, 27, 91, 50, 52, 94 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, true, true)); // Ctrl+Alt+Shift+F2=Ctrl+Alt+F12
            yield return (new byte[] { 27, 27, 91, 50, 53, 94 }, new ConsoleKeyInfo(default, ConsoleKey.F13, false, true, true)); // Ctrl+Alt+Shift+F3=Ctrl+Alt+F13
            yield return (new byte[] { 27, 27, 91, 50, 54, 94 }, new ConsoleKeyInfo(default, ConsoleKey.F14, false, true, true)); // Ctrl+Alt+Shift+F4=Ctrl+Alt+F14
            yield return (new byte[] { 27, 27, 91, 50, 56, 94 }, new ConsoleKeyInfo(default, ConsoleKey.F15, false, true, true)); // Ctrl+Alt+Shift+F5=Ctrl+Alt+F15
            yield return (new byte[] { 27, 27, 91, 50, 57, 94 }, new ConsoleKeyInfo(default, ConsoleKey.F16, false, true, true)); // Ctrl+Alt+Shift+F6=Ctrl+Alt+F16
            yield return (new byte[] { 27, 27, 91, 51, 49, 94 }, new ConsoleKeyInfo(default, ConsoleKey.F17, false, true, true)); // Ctrl+Alt+Shift+F7=Ctrl+Alt+F17
            yield return (new byte[] { 27, 27, 91, 51, 50, 94 }, new ConsoleKeyInfo(default, ConsoleKey.F18, false, true, true)); // Ctrl+Alt+Shift+F8=Ctrl+Alt+F18
            yield return (new byte[] { 27, 27, 91, 51, 51, 94 }, new ConsoleKeyInfo(default, ConsoleKey.F19, false, true, true)); // Ctrl+Alt+Shift+F9=Ctrl+Alt+F19
            yield return (new byte[] { 27, 27, 91, 51, 52, 94 }, new ConsoleKeyInfo(default, ConsoleKey.F20, false, true, true)); // Ctrl+Alt+Shift+F10=Ctrl+Alt+F20
            yield return (new byte[] { 27, 27, 91, 50, 51, 64 }, new ConsoleKeyInfo(default, ConsoleKey.F11, true, true, true)); // Ctrl+Alt+Shift+F11
            yield return (new byte[] { 27, 27, 91, 50, 52, 64 }, new ConsoleKeyInfo(default, ConsoleKey.F12, true, true, true)); // Ctrl+Alt+Shift+F12
            yield return (new byte[] { 27, 91, 55, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, false)); // Home
            yield return (new byte[] { 27, 91, 55, 94 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, true)); // Ctrl+Home
            yield return (new byte[] { 27, 27, 91, 55, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, true, false)); // Alt+Home
            yield return (new byte[] { 27, 27, 91, 55, 94 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, true, true)); // Ctrl+Alt+Home
            yield return (new byte[] { 27, 91, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, false, false)); // End
            yield return (new byte[] { 27, 91, 56, 94 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, false, true)); // Ctrl+End
            yield return (new byte[] { 27, 27, 91, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, true, false)); // Alt+End
            yield return (new byte[] { 27, 27, 91, 56, 94 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, true, true)); // Ctrl+Alt+End
            yield return (new byte[] { 27, 91, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, false, false)); // PageUp
            yield return (new byte[] { 27, 91, 53, 94 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, false, true)); // Ctrl+PageUp
            yield return (new byte[] { 27, 27, 91, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, true, false)); // Alt+PageUp
            yield return (new byte[] { 27, 27, 91, 53, 94 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, true, true)); // Ctrl+Alt+PageUp
            yield return (new byte[] { 27, 91, 54, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, false, false)); // PageDown
            yield return (new byte[] { 27, 91, 54, 94 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, false, true)); // Ctrl+PageDown
            yield return (new byte[] { 27, 27, 91, 54, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, true, false)); // Alt+PageDown
            yield return (new byte[] { 27, 27, 91, 54, 94 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, true, true)); // Ctrl+Alt+PageDown
            yield return (new byte[] { 27, 91, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, false)); // LeftArrow
            yield return (new byte[] { 27, 79, 100 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, true)); // Ctrl+LeftArrow
            yield return (new byte[] { 27, 27, 91, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, true, false)); // Alt+LeftArrow
            yield return (new byte[] { 27, 91, 100 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, true, false, false)); // Shift+LeftArrow
            yield return (new byte[] { 27, 27, 91, 100 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, true, true, false)); // Shift+Alt+LeftArrow
            yield return (new byte[] { 27, 91, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, false, false, false)); // UpArrow
            yield return (new byte[] { 27, 79, 97 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, false, false, true)); // Ctrl+UpArrow
            yield return (new byte[] { 27, 27, 91, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, false, true, false)); // Alt+UpArrow
            yield return (new byte[] { 27, 91, 97 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, true, false, false)); // Shift+UpArrow
            yield return (new byte[] { 27, 27, 91, 97 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, true, true, false)); // Shift+Alt+UpArrow
            yield return (new byte[] { 27, 91, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, false, false, false)); // DownArrow
            yield return (new byte[] { 27, 79, 98 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, false, false, true)); // Ctrl+DownArrow
            yield return (new byte[] { 27, 27, 91, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, false, true, false)); // Alt+DownArrow
            yield return (new byte[] { 27, 91, 98 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, true, false, false)); // Shift+DownArrow
            yield return (new byte[] { 27, 27, 91, 98 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, true, true, false)); // Shift+Alt+DownArrow
            yield return (new byte[] { 27, 91, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, false, false, false)); // RightArrow
            yield return (new byte[] { 27, 79, 99 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, false, false, true)); // Ctrl+RightArrow
            yield return (new byte[] { 27, 27, 91, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, false, true, false)); // Alt+RightArrow
            yield return (new byte[] { 27, 91, 99 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, true, false, false)); // Shift+RightArrow
            yield return (new byte[] { 27, 27, 91, 99 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, true, true, false)); // Shift+Alt+RightArrow
            yield return (new byte[] { 27, 91, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Insert, false, false, false)); // Insert
            yield return (new byte[] { 27, 27, 91, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Insert, false, true, false)); // Alt+Insert
            yield return (new byte[] { 27, 91, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, false, false)); // Delete
            yield return (new byte[] { 27, 91, 51, 94 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, false, true)); // Ctrl+Delete
            yield return (new byte[] { 27, 27, 91, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, true, false)); // Alt+Delete
            yield return (new byte[] { 27, 91, 51, 36 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, true, false, false)); // Shift+Delete
            yield return (new byte[] { 27, 91, 51, 64 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, true, false, true)); // Ctrl+Shift+Delete
            yield return (new byte[] { 27, 27, 91, 51, 36 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, true, true, false)); // Alt+Shift+Delete
            yield return (new byte[] { 27, 27, 91, 51, 64 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, true, true, true)); // Ctrl+Alt+Shift+Delete
            // Numeric Keypad
            yield return (new byte[] { 48 }, new ConsoleKeyInfo('0', ConsoleKey.D0, false, false, false));
            yield return (new byte[] { 49 }, new ConsoleKeyInfo('1', ConsoleKey.D1, false, false, false));
            yield return (new byte[] { 50 }, new ConsoleKeyInfo('2', ConsoleKey.D2, false, false, false));
            yield return (new byte[] { 51 }, new ConsoleKeyInfo('3', ConsoleKey.D3, false, false, false));
            yield return (new byte[] { 52 }, new ConsoleKeyInfo('4', ConsoleKey.D4, false, false, false));
            yield return (new byte[] { 53 }, new ConsoleKeyInfo('5', ConsoleKey.D5, false, false, false));
            yield return (new byte[] { 54 }, new ConsoleKeyInfo('6', ConsoleKey.D6, false, false, false));
            yield return (new byte[] { 55 }, new ConsoleKeyInfo('7', ConsoleKey.D7, false, false, false));
            yield return (new byte[] { 56 }, new ConsoleKeyInfo('8', ConsoleKey.D8, false, false, false));
            yield return (new byte[] { 57 }, new ConsoleKeyInfo('9', ConsoleKey.D9, false, false, false));
            yield return (new byte[] { 47 }, new ConsoleKeyInfo('/', ConsoleKey.Divide, false, false, false));
            yield return (new byte[] { 42 }, new ConsoleKeyInfo('*', ConsoleKey.Multiply, false, false, false));
            yield return (new byte[] { 45 }, new ConsoleKeyInfo('-', ConsoleKey.Subtract, false, false, false));
            yield return (new byte[] { 43 }, new ConsoleKeyInfo('+', ConsoleKey.Add, false, false, false));
            yield return (new byte[] { 13 }, new ConsoleKeyInfo((char)13, ConsoleKey.Enter, false, false, false)); // Enter (using Numeric Keypad))
            yield return (new byte[] { 46 }, new ConsoleKeyInfo('.', ConsoleKey.OemPeriod, false, false, false)); // . (period using Numeric Keypad))
            yield return (new byte[] { 27, 79, 112 }, new ConsoleKeyInfo(default, ConsoleKey.Insert, false, false, false)); // Insert
            yield return (new byte[] { 27, 79, 110 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, false, false)); // Delete
            yield return (new byte[] { 27, 79, 113 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, false, false)); // End
            yield return (new byte[] { 27, 79, 114 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, false, false, false)); // Down Arrow
            yield return (new byte[] { 27, 79, 115 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, false, false)); // Page Down
            yield return (new byte[] { 27, 79, 116 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, false)); // Left Arrow
            yield return (new byte[] { 27, 79, 117 }, new ConsoleKeyInfo(default, ConsoleKey.NoName, false, false, false)); // Begin (5)
            yield return (new byte[] { 27, 79, 118 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, false, false, false)); // Right Arrow
            yield return (new byte[] { 27, 79, 119 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, false)); // Home
            yield return (new byte[] { 27, 79, 120 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, false, false, false)); // Up Arrow
            yield return (new byte[] { 27, 79, 121 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, false, false)); // Page Up
            yield return (new byte[] { 27, 79, 111 }, new ConsoleKeyInfo('/', ConsoleKey.Divide, false, false, false)); // / (divide sign using Numeric Keypad))
            yield return (new byte[] { 27, 79, 106 }, new ConsoleKeyInfo('*', ConsoleKey.Multiply, false, false, false)); // * (multiply sign using Numeric Keypad))
            yield return (new byte[] { 27, 79, 109 }, new ConsoleKeyInfo('-', ConsoleKey.Subtract, false, false, false)); // - (minus sign using Numeric Keypad))
            yield return (new byte[] { 27, 79, 107 }, new ConsoleKeyInfo('+', ConsoleKey.Add, false, false, false)); // + (plus sign using Numeric Keypad))
            yield return (new byte[] { 27, 79, 77 }, new ConsoleKeyInfo((char)13, ConsoleKey.Enter, false, false, false)); // Enter (using Numeric Keypad))
        }
    }
}

// Ubuntu 18.04 x64
public class TmuxData : TerminalData
{
    protected override string EncodingCharset => "utf-8";
    protected override string Term => "screen";
    internal override byte Verase => 127;
    protected override string EncodedTerminalDb => "GgEqACsAEABpAZkCc2NyZWVufFZUIDEwMC9BTlNJIFgzLjY0IHZpcnR1YWwgdGVybWluYWwAAAEAAAEAAAABAAAAAAEBAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAEAAAAAAQBQAAgAGAD//////////////////////////wgAQAD+/wAABAAGAAgAGQAeACUAKQAtAP//OABJAEsATwBWAP//WABkAP//aABrAHEAdQD/////eQB7AIAAhQD//44AkwD/////mACdAKIA//+nAKkArgD//7cAvADCAMgA////////ywD////////PAP//0wD////////VAP//2gD//////////94A4gDoAOwA8AD0APoAAAEGAQwBEgEXAf//HAH//yABJQEqAf///////y4BMgE6Af//////////////////////////////////QgH//0UBTgFXAWABaQFyAXsBhAH//40B/////////////////////5YB/////6cBqgG1AbgBugG9AREC//8UAv////////////////////////////8WAv//VwL///////////////9bAv////////////////////9iAv///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////2cCbQL///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////9zAv///////////////////////////////////////////////////////////////////////3gC////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////gQL///////+FAo8CG1taAAcADQAbWyVpJXAxJWQ7JXAyJWRyABtbM2cAG1tIG1tKABtbSwAbW0oAG1slaSVwMSVkRwAbWyVpJXAxJWQ7JXAyJWRIAAoAG1tIABtbPzI1bAAIABtbMzRoG1s/MjVoABtbQwAbTQAbWzM0bAAbW1AAG1tNAA4AG1s1bQAbWzFtABtbPzEwNDloABtbMm0AG1s0aAAbWzdtABtbM20AG1s0bQAPABtbbQ8AG1s/MTA0OWwAG1s0bAAbWzIzbQAbWzI0bQAbZwAbKTAAG1tMAH8AG1szfgAbT0IAG09QABtbMjF+ABtPUQAbT1IAG09TABtbMTV+ABtbMTd+ABtbMTh+ABtbMTl+ABtbMjB+ABtbMX4AG1syfgAbT0QAG1s2fgAbWzV+ABtPQwAbT0EAG1s/MWwbPgAbWz8xaBs9ABtFABtbJXAxJWRQABtbJXAxJWRNABtbJXAxJWRCABtbJXAxJWRAABtbJXAxJWRTABtbJXAxJWRMABtbJXAxJWREABtbJXAxJWRDABtbJXAxJWRBABtjG1s/MTAwMGwbWz8yNWgAGzgAG1slaSVwMSVkZAAbNwAKABtNABtbMCU/JXA2JXQ7MSU7JT8lcDEldDszJTslPyVwMiV0OzQlOyU/JXAzJXQ7NyU7JT8lcDQldDs1JTslPyVwNSV0OzIlO20lPyVwOSV0DiVlDyU7ABtIAAkAKyssLC0tLi4wMGBgYWFmZmdnaGhpaWpqa2tsbG1tbm5vb3BwcXFycnNzdHR1dXZ2d3d4eHl5enp7e3x8fX1+fgAbW1oAGyhCGykwABtbNH4AG1syM34AG1syNH4AG1sxSwAbWzM5OzQ5bQAbW00AG1szJXAxJWRtABtbNCVwMSVkbQAAAwABAAsAGgBDAAEBAAABAAAA//8EAP////////////////////8AAAMABgAJAAwADwASABUAGAAeACQAKAAsADAANAAbKEIAGyglcDElYwBBWABHMABYVABVOABFMABFMwBTMABYTQBrRU5ENQBrSE9NNQBrYTIAa2IxAGtiMwBrYzIAeG0A"; // /lib/terminfo/s/screen

    internal override IEnumerable<(byte[], ConsoleKeyInfo)> RecordedScenarios
    {
        get
        {
            yield return (new byte[] { 27, 79, 80 }, new ConsoleKeyInfo(default, ConsoleKey.F1, false, false, false)); // F1
            yield return (new byte[] { 27, 79, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F2, false, false, false)); // F2
            yield return (new byte[] { 27, 79, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, false, false)); // F3
            yield return (new byte[] { 27, 79, 83 }, new ConsoleKeyInfo(default, ConsoleKey.F4, false, false, false)); // F4
            yield return (new byte[] { 27, 91, 49, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, false, false, false)); // F5
            yield return (new byte[] { 27, 91, 49, 55, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, false, false, false)); // F6
            yield return (new byte[] { 27, 91, 49, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, false, false, false)); // F7
            yield return (new byte[] { 27, 91, 49, 57, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, false, false, false)); // F8
            yield return (new byte[] { 27, 91, 50, 48, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, false, false)); // F9
            yield return (new byte[] { 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, false)); // F12
            yield return (new byte[] { 27, 91, 49, 59, 53, 80 }, new ConsoleKeyInfo(default, ConsoleKey.F1, false, false, true)); // Ctrl+F1
            yield return (new byte[] { 27, 91, 49, 59, 53, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F2, false, false, true)); // Ctrl+F2
            yield return (new byte[] { 27, 91, 49, 59, 53, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, false, true)); // Ctrl+F3
            yield return (new byte[] { 27, 91, 49, 59, 53, 83 }, new ConsoleKeyInfo(default, ConsoleKey.F4, false, false, true)); // Ctrl+F4
            yield return (new byte[] { 27, 91, 49, 53, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, false, false, true)); // Ctrl+F5
            yield return (new byte[] { 27, 91, 49, 55, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, false, false, true)); // Ctrl+F6
            yield return (new byte[] { 27, 91, 49, 56, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, false, false, true)); // Ctrl+F7
            yield return (new byte[] { 27, 91, 49, 57, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, false, false, true)); // Ctrl+F8
            yield return (new byte[] { 27, 91, 50, 48, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, false, true)); // Ctrl+F9
            yield return (new byte[] { 27, 91, 50, 49, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F10, false, false, true)); // Ctrl+F10
            yield return (new byte[] { 27, 91, 50, 51, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, false, true)); // Ctrl+F11
            yield return (new byte[] { 27, 91, 50, 52, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, true)); // Ctrl+F12
            yield return (new byte[] { 27, 91, 49, 59, 51, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, true, false)); // Alt+F3
            yield return (new byte[] { 27, 91, 50, 48, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, true, false)); // Alt+F9
            yield return (new byte[] { 27, 91, 50, 51, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, true, false)); // Alt+F11
            yield return (new byte[] { 27, 91, 50, 52, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, true, false)); // Alt+F12
            yield return (new byte[] { 27, 91, 49, 59, 50, 80 }, new ConsoleKeyInfo(default, ConsoleKey.F1, true, false, false)); // Shift+F1
            yield return (new byte[] { 27, 91, 49, 59, 50, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F2, true, false, false)); // Shift+F2
            yield return (new byte[] { 27, 91, 49, 59, 50, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, true, false, false)); // Shift+F3
            yield return (new byte[] { 27, 91, 49, 59, 50, 83 }, new ConsoleKeyInfo(default, ConsoleKey.F4, true, false, false)); // Shift+F4
            yield return (new byte[] { 27, 91, 49, 53, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, true, false, false)); // Shift+F5
            yield return (new byte[] { 27, 91, 49, 55, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, true, false, false)); // Shift+F6
            yield return (new byte[] { 27, 91, 49, 56, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, true, false, false)); // Shift+F7
            yield return (new byte[] { 27, 91, 49, 57, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, true, false, false)); // Shift+F8
            yield return (new byte[] { 27, 91, 50, 48, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, true, false, false)); // Shift+F9
            yield return (new byte[] { 27, 91, 50, 51, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, true, false, false)); // Shift+F11
            yield return (new byte[] { 27, 91, 50, 52, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, true, false, false)); // Shift+F12
            yield return (new byte[] { 27, 91, 49, 59, 56, 80 }, new ConsoleKeyInfo(default, ConsoleKey.F1, true, true, true)); // Ctrl+Alt+Shift+F1
            yield return (new byte[] { 27, 91, 49, 59, 56, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F2, true, true, true)); // Ctrl+Alt+Shift+F2
            yield return (new byte[] { 27, 91, 49, 59, 56, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, true, true, true)); // Ctrl+Alt+Shift+F3
            yield return (new byte[] { 27, 91, 49, 59, 56, 83 }, new ConsoleKeyInfo(default, ConsoleKey.F4, true, true, true)); // Ctrl+Alt+Shift+F4
            yield return (new byte[] { 27, 91, 49, 53, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, true, true, true)); // Ctrl+Alt+Shift+F5
            yield return (new byte[] { 27, 91, 49, 55, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, true, true, true)); // Ctrl+Alt+Shift+F6
            yield return (new byte[] { 27, 91, 49, 56, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, true, true, true)); // Ctrl+Alt+Shift+F7
            yield return (new byte[] { 27, 91, 49, 57, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, true, true, true)); // Ctrl+Alt+Shift+F8
            yield return (new byte[] { 27, 91, 50, 48, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, true, true, true)); // Ctrl+Alt+Shift+F9
            yield return (new byte[] { 27, 91, 50, 49, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F10, true, true, true)); // Ctrl+Alt+Shift+F10
            yield return (new byte[] { 27, 91, 50, 51, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, true, true, true)); // Ctrl+Alt+Shift+F11
            yield return (new byte[] { 27, 91, 50, 52, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, true, true, true)); // Ctrl+Alt+Shift+F12
            yield return (new byte[] { 27, 91, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, false)); // Home
            yield return (new byte[] { 27, 91, 49, 59, 53, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, true)); // Ctrl+Home
            yield return (new byte[] { 27, 91, 49, 59, 51, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, true, false)); // Alt+Home
            yield return (new byte[] { 27, 91, 49, 59, 55, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, true, true)); // Ctrl+Alt+Home
            yield return (new byte[] { 27, 91, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, false, false)); // End
            yield return (new byte[] { 27, 91, 49, 59, 53, 70 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, false, true)); // Ctrl+End
            yield return (new byte[] { 27, 91, 49, 59, 51, 70 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, true, false)); // Alt+End
            yield return (new byte[] { 27, 91, 49, 59, 55, 70 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, true, true)); // Ctrl+Alt+End
            yield return (new byte[] { 27, 91, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, false, false)); // PageUp
            yield return (new byte[] { 27, 91, 53, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, false, true)); // Ctrl+PageUp
            yield return (new byte[] { 27, 91, 53, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, true, false)); // Alt+PageUp
            yield return (new byte[] { 27, 91, 53, 59, 55, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, true, true)); // Ctrl+Alt+PageUp
            yield return (new byte[] { 27, 91, 54, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, false, false)); // PageDown
            yield return (new byte[] { 27, 91, 54, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, false, true)); // Ctrl+PageDown
            yield return (new byte[] { 27, 91, 54, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, true, false)); // Alt+PageDown
            yield return (new byte[] { 27, 91, 54, 59, 55, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, true, true)); // Ctrl+Alt+PageDown
            yield return (new byte[] { 27, 79, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, false)); // LeftArrow
            yield return (new byte[] { 27, 91, 49, 59, 53, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, true)); // Ctrl+LeftArrow
            yield return (new byte[] { 27, 91, 49, 59, 51, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, true, false)); // Alt+LeftArrow
            yield return (new byte[] { 27, 91, 49, 59, 50, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, true, false, false)); // Shift+LeftArrow
            yield return (new byte[] { 27, 91, 49, 59, 52, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, true, true, false)); // Shift+Alt+LeftArrow
            yield return (new byte[] { 27, 79, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, false, false, false)); // UpArrow
            yield return (new byte[] { 27, 91, 49, 59, 53, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, false, false, true)); // Ctrl+UpArrow
            yield return (new byte[] { 27, 91, 49, 59, 51, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, false, true, false)); // Alt+UpArrow
            yield return (new byte[] { 27, 91, 49, 59, 50, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, true, false, false)); // Shift+UpArrow
            yield return (new byte[] { 27, 91, 49, 59, 52, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, true, true, false)); // Shift+Alt+UpArrow
            yield return (new byte[] { 27, 79, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, false, false, false)); // DownArrow
            yield return (new byte[] { 27, 91, 49, 59, 53, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, false, false, true)); // Ctrl+DownArrow
            yield return (new byte[] { 27, 91, 49, 59, 51, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, false, true, false)); // Alt+DownArrow
            yield return (new byte[] { 27, 91, 49, 59, 50, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, true, false, false)); // Shift+DownArrow
            yield return (new byte[] { 27, 91, 49, 59, 52, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, true, true, false)); // Shift+Alt+DownArrow
            yield return (new byte[] { 27, 79, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, false, false, false)); // RightArrow
            yield return (new byte[] { 27, 91, 49, 59, 53, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, false, false, true)); // Ctrl+RightArrow
            yield return (new byte[] { 27, 91, 49, 59, 51, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, false, true, false)); // Alt+RightArrow
            yield return (new byte[] { 27, 91, 49, 59, 50, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, true, false, false)); // Shift+RightArrow
            yield return (new byte[] { 27, 91, 49, 59, 52, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, true, true, false)); // Shift+Alt+RightArrow
            yield return (new byte[] { 27, 91, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Insert, false, false, false)); // Insert
            yield return (new byte[] { 27, 91, 50, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Insert, false, true, false)); // Alt+Insert
            yield return (new byte[] { 27, 91, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, false, false)); // Delete
            yield return (new byte[] { 27, 91, 51, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, false, true)); // Ctrl+Delete
            yield return (new byte[] { 27, 91, 51, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, true, false)); // Alt+Delete
            yield return (new byte[] { 27, 91, 51, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, true, false, false)); // Shift+Delete
            yield return (new byte[] { 27, 91, 51, 59, 54, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, true, false, true)); // Ctrl+Shift+Delete
            yield return (new byte[] { 27, 91, 51, 59, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, true, true, false)); // Alt+Shift+Delete
            yield return (new byte[] { 27, 91, 51, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, true, true, true)); // Ctrl+Alt+Shift+Delete
            // Numeric Keypad
            yield return (new byte[] { 48 }, new ConsoleKeyInfo('0', ConsoleKey.D0, false, false, false));
            yield return (new byte[] { 49 }, new ConsoleKeyInfo('1', ConsoleKey.D1, false, false, false));
            yield return (new byte[] { 50 }, new ConsoleKeyInfo('2', ConsoleKey.D2, false, false, false));
            yield return (new byte[] { 51 }, new ConsoleKeyInfo('3', ConsoleKey.D3, false, false, false));
            yield return (new byte[] { 52 }, new ConsoleKeyInfo('4', ConsoleKey.D4, false, false, false));
            yield return (new byte[] { 53 }, new ConsoleKeyInfo('5', ConsoleKey.D5, false, false, false));
            yield return (new byte[] { 54 }, new ConsoleKeyInfo('6', ConsoleKey.D6, false, false, false));
            yield return (new byte[] { 55 }, new ConsoleKeyInfo('7', ConsoleKey.D7, false, false, false));
            yield return (new byte[] { 56 }, new ConsoleKeyInfo('8', ConsoleKey.D8, false, false, false));
            yield return (new byte[] { 57 }, new ConsoleKeyInfo('9', ConsoleKey.D9, false, false, false));
            yield return (new byte[] { 47 }, new ConsoleKeyInfo('/', ConsoleKey.Divide, false, false, false));
            yield return (new byte[] { 42 }, new ConsoleKeyInfo('*', ConsoleKey.Multiply, false, false, false));
            yield return (new byte[] { 45 }, new ConsoleKeyInfo('-', ConsoleKey.Subtract, false, false, false));
            yield return (new byte[] { 43 }, new ConsoleKeyInfo('+', ConsoleKey.Add, false, false, false));
            yield return (new byte[] { 13 }, new ConsoleKeyInfo((char)13, ConsoleKey.Enter, false, false, false)); // Enter (using Numeric Keypad))
            yield return (new byte[] { 46 }, new ConsoleKeyInfo('.', ConsoleKey.OemPeriod, false, false, false)); // . (period using Numeric Keypad))
            yield return (new byte[] { 27, 91, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Insert, false, false, false)); // Insert
            yield return (new byte[] { 27, 91, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, false, false)); // Delete
            yield return (new byte[] { 27, 91, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, false, false)); // End
            yield return (new byte[] { 27, 79, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, false, false, false)); // Down Arrow
            yield return (new byte[] { 27, 91, 54, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, false, false)); // Page Down
            yield return (new byte[] { 27, 79, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, false)); // Left Arrow
            yield return (new byte[] { 27, 79, 69 }, new ConsoleKeyInfo(default, ConsoleKey.NoName, false, false, false)); // Begin (5)
            yield return (new byte[] { 27, 79, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, false, false, false)); // Right Arrow
            yield return (new byte[] { 27, 91, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, false)); // Home
            yield return (new byte[] { 27, 79, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, false, false, false)); // Up Arrow
            yield return (new byte[] { 27, 91, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, false, false)); // Page Up
            yield return (new byte[] { 27, 79, 111 }, new ConsoleKeyInfo('/', ConsoleKey.Divide, false, false, false)); // / (divide sign using Numeric Keypad))
            yield return (new byte[] { 27, 79, 106 }, new ConsoleKeyInfo('*', ConsoleKey.Multiply, false, false, false)); // * (multiply sign using Numeric Keypad))
            yield return (new byte[] { 27, 79, 109 }, new ConsoleKeyInfo('-', ConsoleKey.Subtract, false, false, false)); // - (minus sign using Numeric Keypad))
            yield return (new byte[] { 27, 79, 107 }, new ConsoleKeyInfo('+', ConsoleKey.Add, false, false, false)); // + (plus sign using Numeric Keypad))
            yield return (new byte[] { 27, 79, 77 }, new ConsoleKeyInfo((char)13, ConsoleKey.Enter, false, false, false)); // Enter (using Numeric Keypad))
        }
    }
}

// Ubuntu 18.04 x64 Tmux configured to use 256 colours (https://github.com/tmux/tmux/wiki/FAQ) started from GNOME Terminal using `tmux new` command
public class Tmux256ColorData : TerminalData
{
    protected override string EncodingCharset => "utf-8";
    protected override string Term => "screen-256color";
    internal override byte Verase => 127;
    protected override string EncodedTerminalDb => "GgErACsADwBpAQQDc2NyZWVuLTI1NmNvbG9yfEdOVSBTY3JlZW4gd2l0aCAyNTYgY29sb3JzAAABAAABAAAAAQAAAAABAQAAAAAAAAAAAAAAAAAAAAAAAAAAAAABAAAAAAFQAAgAGAD//////////////////////////wAB/38AAAQABgAIABkAHgAlACkALQD//zgASQBLAE8AVgD//1gAZAD//2gAawBxAHUA/////3kAewCAAIUA//+OAJMA/////5gAnQCiAP//pwCpAK4A//+3ALwAwgDIAP///////8sA////////zwD//9MA////////1QD//9oA///////////eAOIA6ADsAPAA9AD6AAABBgEMARIBFwH//xwB//8gASUBKgH///////8uATIBOgH//////////////////////////////////0IB//9FAU4BVwFgAWkBcgF7AYQB//+NAf////////////////////+WAf////+nAaoBtQG4AboBvQERAv//FAL/////////////////////////////FgL//1cC////////////////WwL/////////////////////YgL///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////9nAm0C////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////cwL///////////////////////////////////////////////////////////////////////94Av///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////4EC////////hQLEAhtbWgAHAA0AG1slaSVwMSVkOyVwMiVkcgAbWzNnABtbSBtbSgAbW0sAG1tKABtbJWklcDElZEcAG1slaSVwMSVkOyVwMiVkSAAKABtbSAAbWz8yNWwACAAbWzM0aBtbPzI1aAAbW0MAG00AG1szNGwAG1tQABtbTQAOABtbNW0AG1sxbQAbWz8xMDQ5aAAbWzJtABtbNGgAG1s3bQAbWzNtABtbNG0ADwAbW20PABtbPzEwNDlsABtbNGwAG1syM20AG1syNG0AG2cAGykwABtbTAB/ABtbM34AG09CABtPUAAbWzIxfgAbT1EAG09SABtPUwAbWzE1fgAbWzE3fgAbWzE4fgAbWzE5fgAbWzIwfgAbWzF+ABtbMn4AG09EABtbNn4AG1s1fgAbT0MAG09BABtbPzFsGz4AG1s/MWgbPQAbRQAbWyVwMSVkUAAbWyVwMSVkTQAbWyVwMSVkQgAbWyVwMSVkQAAbWyVwMSVkUwAbWyVwMSVkTAAbWyVwMSVkRAAbWyVwMSVkQwAbWyVwMSVkQQAbYxtbPzEwMDBsG1s/MjVoABs4ABtbJWklcDElZGQAGzcACgAbTQAbWzAlPyVwNiV0OzElOyU/JXAxJXQ7MyU7JT8lcDIldDs0JTslPyVwMyV0OzclOyU/JXA0JXQ7NSU7JT8lcDUldDsyJTttJT8lcDkldA4lZQ8lOwAbSAAJACsrLCwtLS4uMDBgYGFhZmZnZ2hoaWlqamtrbGxtbW5ub29wcHFxcnJzc3R0dXV2dnd3eHh5eXp6e3t8fH19fn4AG1taABsoQhspMAAbWzR+ABtbMjN+ABtbMjR+ABtbMUsAG1szOTs0OW0AG1tNABtbJT8lcDElezh9JTwldDMlcDElZCVlJXAxJXsxNn0lPCV0OSVwMSV7OH0lLSVkJWUzODs1OyVwMSVkJTttABtbJT8lcDElezh9JTwldDQlcDElZCVlJXAxJXsxNn0lPCV0MTAlcDElezh9JS0lZCVlNDg7NTslcDElZCU7bQADAAEAIABEALYAAQEAAAEAAAAEAP///////////////////////////////////////////////////////////////////////////////wAAAwAGAAkADAAPABIAFQAaAB8AJAApAC4AMgA3ADwAQQBGAEsAUQBXAF0AYwBpAG8AdQB7AIEAhwCNAJMAlwCbAJ8AowCnABsoQgAbKCVwMSVjAEFYAEcwAFhUAFU4AEUwAFMwAFhNAGtEQzMAa0RDNABrREM1AGtEQzYAa0RDNwBrRE4Aa0ROMwBrRE40AGtETjUAa0RONgBrRE43AGtFTkQ1AGtIT001AGtMRlQzAGtMRlQ0AGtMRlQ1AGtMRlQ2AGtMRlQ3AGtSSVQzAGtSSVQ0AGtSSVQ1AGtSSVQ2AGtSSVQ3AGtVUABrYTIAa2IxAGtiMwBrYzIAeG0A"; // /lib/terminfo/s/screen-256color

    internal override IEnumerable<(byte[], ConsoleKeyInfo)> RecordedScenarios
    {
        get
        {
            yield return (new byte[] { 27, 79, 80 }, new ConsoleKeyInfo(default, ConsoleKey.F1, false, false, false)); // F1
            yield return (new byte[] { 27, 79, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F2, false, false, false)); // F2
            yield return (new byte[] { 27, 79, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, false, false)); // F3
            yield return (new byte[] { 27, 79, 83 }, new ConsoleKeyInfo(default, ConsoleKey.F4, false, false, false)); // F4
            yield return (new byte[] { 27, 91, 49, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, false, false, false)); // F5
            yield return (new byte[] { 27, 91, 49, 55, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, false, false, false)); // F6
            yield return (new byte[] { 27, 91, 49, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, false, false, false)); // F7
            yield return (new byte[] { 27, 91, 49, 57, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, false, false, false)); // F8
            yield return (new byte[] { 27, 91, 50, 48, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, false, false)); // F9
            yield return (new byte[] { 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, false)); // F12
            yield return (new byte[] { 27, 91, 49, 59, 53, 80 }, new ConsoleKeyInfo(default, ConsoleKey.F1, false, false, true)); // Ctrl+F1
            yield return (new byte[] { 27, 91, 49, 59, 53, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F2, false, false, true)); // Ctrl+F2
            yield return (new byte[] { 27, 91, 49, 59, 53, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, false, true)); // Ctrl+F3
            yield return (new byte[] { 27, 91, 49, 59, 53, 83 }, new ConsoleKeyInfo(default, ConsoleKey.F4, false, false, true)); // Ctrl+F4
            yield return (new byte[] { 27, 91, 49, 53, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, false, false, true)); // Ctrl+F5
            yield return (new byte[] { 27, 91, 49, 55, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, false, false, true)); // Ctrl+F6
            yield return (new byte[] { 27, 91, 49, 56, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, false, false, true)); // Ctrl+F7
            yield return (new byte[] { 27, 91, 49, 57, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, false, false, true)); // Ctrl+F8
            yield return (new byte[] { 27, 91, 50, 48, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, false, true)); // Ctrl+F9
            yield return (new byte[] { 27, 91, 50, 49, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F10, false, false, true)); // Ctrl+F10
            yield return (new byte[] { 27, 91, 50, 51, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, false, true)); // Ctrl+F11
            yield return (new byte[] { 27, 91, 50, 52, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, true)); // Ctrl+F12
            yield return (new byte[] { 27, 91, 49, 59, 51, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, true, false)); // Alt+F3
            yield return (new byte[] { 27, 91, 50, 48, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, true, false)); // Alt+F9
            yield return (new byte[] { 27, 91, 50, 51, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, true, false)); // Alt+F11
            yield return (new byte[] { 27, 91, 50, 52, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, true, false)); // Alt+F12
            yield return (new byte[] { 27, 91, 49, 59, 50, 80 }, new ConsoleKeyInfo(default, ConsoleKey.F1, true, false, false)); // Shift+F1
            yield return (new byte[] { 27, 91, 49, 59, 50, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F2, true, false, false)); // Shift+F2
            yield return (new byte[] { 27, 91, 49, 59, 50, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, true, false, false)); // Shift+F3
            yield return (new byte[] { 27, 91, 49, 59, 50, 83 }, new ConsoleKeyInfo(default, ConsoleKey.F4, true, false, false)); // Shift+F4
            yield return (new byte[] { 27, 91, 49, 53, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, true, false, false)); // Shift+F5
            yield return (new byte[] { 27, 91, 49, 55, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, true, false, false)); // Shift+F6
            yield return (new byte[] { 27, 91, 49, 56, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, true, false, false)); // Shift+F7
            yield return (new byte[] { 27, 91, 49, 57, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, true, false, false)); // Shift+F8
            yield return (new byte[] { 27, 91, 50, 48, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, true, false, false)); // Shift+F9
            yield return (new byte[] { 27, 91, 50, 52, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, true, false, false)); // Shift+F12
            yield return (new byte[] { 27, 91, 49, 59, 56, 80 }, new ConsoleKeyInfo(default, ConsoleKey.F1, true, true, true)); // Ctrl+Alt+Shift+F1
            yield return (new byte[] { 27, 91, 49, 59, 56, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F2, true, true, true)); // Ctrl+Alt+Shift+F2
            yield return (new byte[] { 27, 91, 49, 59, 56, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, true, true, true)); // Ctrl+Alt+Shift+F3
            yield return (new byte[] { 27, 91, 49, 59, 56, 83 }, new ConsoleKeyInfo(default, ConsoleKey.F4, true, true, true)); // Ctrl+Alt+Shift+F4
            yield return (new byte[] { 27, 91, 49, 53, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, true, true, true)); // Ctrl+Alt+Shift+F5
            yield return (new byte[] { 27, 91, 49, 55, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, true, true, true)); // Ctrl+Alt+Shift+F6
            yield return (new byte[] { 27, 91, 49, 56, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, true, true, true)); // Ctrl+Alt+Shift+F7
            yield return (new byte[] { 27, 91, 49, 57, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, true, true, true)); // Ctrl+Alt+Shift+F8
            yield return (new byte[] { 27, 91, 50, 48, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, true, true, true)); // Ctrl+Alt+Shift+F9
            yield return (new byte[] { 27, 91, 50, 49, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F10, true, true, true)); // Ctrl+Alt+Shift+F10
            yield return (new byte[] { 27, 91, 50, 51, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, true, true, true)); // Ctrl+Alt+Shift+F11
            yield return (new byte[] { 27, 91, 50, 52, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, true, true, true)); // Ctrl+Alt+Shift+F12
            yield return (new byte[] { 27, 91, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, false)); // Home
            yield return (new byte[] { 27, 91, 49, 59, 53, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, true)); // Ctrl+Home
            yield return (new byte[] { 27, 91, 49, 59, 51, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, true, false)); // Alt+Home
            yield return (new byte[] { 27, 91, 49, 59, 55, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, true, true)); // Ctrl+Alt+Home
            yield return (new byte[] { 27, 91, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, false, false)); // End
            yield return (new byte[] { 27, 91, 49, 59, 53, 70 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, false, true)); // Ctrl+End
            yield return (new byte[] { 27, 91, 49, 59, 51, 70 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, true, false)); // Alt+End
            yield return (new byte[] { 27, 91, 49, 59, 55, 70 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, true, true)); // Ctrl+Alt+End
            yield return (new byte[] { 27, 91, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, false, false)); // PageUp
            yield return (new byte[] { 27, 91, 53, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, false, true)); // Ctrl+PageUp
            yield return (new byte[] { 27, 91, 53, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, true, false)); // Alt+PageUp
            yield return (new byte[] { 27, 91, 53, 59, 55, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, true, true)); // Ctrl+Alt+PageUp
            yield return (new byte[] { 27, 91, 54, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, false, false)); // PageDown
            yield return (new byte[] { 27, 91, 54, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, false, true)); // Ctrl+PageDown
            yield return (new byte[] { 27, 91, 54, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, true, false)); // Alt+PageDown
            yield return (new byte[] { 27, 91, 54, 59, 55, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, true, true)); // Ctrl+Alt+PageDown
            yield return (new byte[] { 27, 79, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, false)); // LeftArrow
            yield return (new byte[] { 27, 91, 49, 59, 53, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, true)); // Ctrl+LeftArrow
            yield return (new byte[] { 27, 91, 49, 59, 51, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, true, false)); // Alt+LeftArrow
            yield return (new byte[] { 27, 91, 49, 59, 50, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, true, false, false)); // Shift+LeftArrow
            yield return (new byte[] { 27, 91, 49, 59, 52, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, true, true, false)); // Shift+Alt+LeftArrow
            yield return (new byte[] { 27, 79, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, false, false, false)); // UpArrow
            yield return (new byte[] { 27, 91, 49, 59, 53, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, false, false, true)); // Ctrl+UpArrow
            yield return (new byte[] { 27, 91, 49, 59, 51, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, false, true, false)); // Alt+UpArrow
            yield return (new byte[] { 27, 91, 49, 59, 50, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, true, false, false)); // Shift+UpArrow
            yield return (new byte[] { 27, 91, 49, 59, 52, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, true, true, false)); // Shift+Alt+UpArrow
            yield return (new byte[] { 27, 79, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, false, false, false)); // DownArrow
            yield return (new byte[] { 27, 91, 49, 59, 53, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, false, false, true)); // Ctrl+DownArrow
            yield return (new byte[] { 27, 91, 49, 59, 51, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, false, true, false)); // Alt+DownArrow
            yield return (new byte[] { 27, 91, 49, 59, 50, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, true, false, false)); // Shift+DownArrow
            yield return (new byte[] { 27, 91, 49, 59, 52, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, true, true, false)); // Shift+Alt+DownArrow
            yield return (new byte[] { 27, 79, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, false, false, false)); // RightArrow
            yield return (new byte[] { 27, 91, 49, 59, 53, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, false, false, true)); // Ctrl+RightArrow
            yield return (new byte[] { 27, 91, 49, 59, 51, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, false, true, false)); // Alt+RightArrow
            yield return (new byte[] { 27, 91, 49, 59, 50, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, true, false, false)); // Shift+RightArrow
            yield return (new byte[] { 27, 91, 49, 59, 52, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, true, true, false)); // Shift+Alt+RightArrow
            yield return (new byte[] { 27, 91, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Insert, false, false, false)); // Insert
            yield return (new byte[] { 27, 91, 50, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Insert, false, true, false)); // Alt+Insert
            yield return (new byte[] { 27, 91, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, false, false)); // Delete
            yield return (new byte[] { 27, 91, 51, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, false, true)); // Ctrl+Delete
            yield return (new byte[] { 27, 91, 51, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, true, false)); // Alt+Delete
            yield return (new byte[] { 27, 91, 51, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, true, false, false)); // Shift+Delete
            yield return (new byte[] { 27, 91, 51, 59, 54, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, true, false, true)); // Ctrl+Shift+Delete
            yield return (new byte[] { 27, 91, 51, 59, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, true, true, false)); // Alt+Shift+Delete
            yield return (new byte[] { 27, 91, 51, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, true, true, true)); // Ctrl+Alt+Shift+Delete
            yield return (new byte[] { 48 }, new ConsoleKeyInfo('0', ConsoleKey.D0, false, false, false)); // 0 (using Numeric Keypad)
            yield return (new byte[] { 49 }, new ConsoleKeyInfo('1', ConsoleKey.D1, false, false, false)); // 1 (using Numeric Keypad)
            yield return (new byte[] { 50 }, new ConsoleKeyInfo('2', ConsoleKey.D2, false, false, false)); // 2 (using Numeric Keypad)
            yield return (new byte[] { 51 }, new ConsoleKeyInfo('3', ConsoleKey.D3, false, false, false)); // 3 (using Numeric Keypad)
            yield return (new byte[] { 52 }, new ConsoleKeyInfo('4', ConsoleKey.D4, false, false, false)); // 4 (using Numeric Keypad)
            yield return (new byte[] { 53 }, new ConsoleKeyInfo('5', ConsoleKey.D5, false, false, false)); // 5 (using Numeric Keypad)
            yield return (new byte[] { 54 }, new ConsoleKeyInfo('6', ConsoleKey.D6, false, false, false)); // 6 (using Numeric Keypad)
            yield return (new byte[] { 55 }, new ConsoleKeyInfo('7', ConsoleKey.D7, false, false, false)); // 7 (using Numeric Keypad)
            yield return (new byte[] { 56 }, new ConsoleKeyInfo('8', ConsoleKey.D8, false, false, false)); // 8 (using Numeric Keypad)
            yield return (new byte[] { 57 }, new ConsoleKeyInfo('9', ConsoleKey.D9, false, false, false)); // 9 (using Numeric Keypad)
            yield return (new byte[] { 47 }, new ConsoleKeyInfo('/', ConsoleKey.Divide, false, false, false)); // / (divide sign using Numeric Keypad))
            yield return (new byte[] { 42 }, new ConsoleKeyInfo('*', ConsoleKey.Multiply, false, false, false)); // * (multiply sign using Numeric Keypad))
            yield return (new byte[] { 45 }, new ConsoleKeyInfo('-', ConsoleKey.Subtract, false, false, false)); // - (minus sign using Numeric Keypad))
            yield return (new byte[] { 43 }, new ConsoleKeyInfo('+', ConsoleKey.Add, false, false, false)); // + (plus sign using Numeric Keypad))
            yield return (new byte[] { 13 }, new ConsoleKeyInfo((char)13, ConsoleKey.Enter, false, false, false)); // Enter (using Numeric Keypad))
            yield return (new byte[] { 46 }, new ConsoleKeyInfo('.', ConsoleKey.OemPeriod, false, false, false)); // . (period using Numeric Keypad))
            yield return (new byte[] { 27, 91, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Insert, false, false, false)); // Insert
            yield return (new byte[] { 27, 91, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, false, false)); // Delete
            yield return (new byte[] { 27, 91, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, false, false)); // End
            yield return (new byte[] { 27, 79, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, false, false, false)); // Down Arrow
            yield return (new byte[] { 27, 91, 54, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, false, false)); // Page Down
            yield return (new byte[] { 27, 79, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, false)); // Left Arrow
            yield return (new byte[] { 27, 79, 69 }, new ConsoleKeyInfo(default, ConsoleKey.NoName, false, false, false)); // Begin (5)
            yield return (new byte[] { 27, 79, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, false, false, false)); // Right Arrow
            yield return (new byte[] { 27, 91, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, false)); // Home
            yield return (new byte[] { 27, 79, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, false, false, false)); // Up Arrow
            yield return (new byte[] { 27, 91, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, false, false)); // Page Up
            yield return (new byte[] { 27, 79, 111 }, new ConsoleKeyInfo('/', ConsoleKey.Divide, false, false, false)); // / (divide sign using Numeric Keypad))
            yield return (new byte[] { 27, 79, 106 }, new ConsoleKeyInfo('*', ConsoleKey.Multiply, false, false, false)); // * (multiply sign using Numeric Keypad))
            yield return (new byte[] { 27, 79, 109 }, new ConsoleKeyInfo('-', ConsoleKey.Subtract, false, false, false)); // - (minus sign using Numeric Keypad))
            yield return (new byte[] { 27, 79, 107 }, new ConsoleKeyInfo('+', ConsoleKey.Add, false, false, false)); // + (plus sign using Numeric Keypad))
            yield return (new byte[] { 27, 79, 77 }, new ConsoleKeyInfo((char)13, ConsoleKey.Enter, false, false, false)); // Enter (using Numeric Keypad))
        }
    }
}

internal static class PuTTy
{
    // PuTTy: Home and End keys: Standard
    internal static IEnumerable<(byte[], ConsoleKeyInfo)> StandardHomeAndEndKeys
    {
        get
        {
            yield return (new byte[] { 27, 91, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, false)); // Home
            yield return (new byte[] { 27, 27, 91, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, true, false)); // Alt+Home
            yield return (new byte[] { 27, 91, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, false, false)); // End
            yield return (new byte[] { 27, 27, 91, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, true, false)); // Alt+End
            yield return (new byte[] { 27, 91, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, false, false)); // PageUp
            yield return (new byte[] { 27, 27, 91, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, true, false)); // Alt+PageUp
            yield return (new byte[] { 27, 91, 54, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, false, false)); // PageDown
            yield return (new byte[] { 27, 27, 91, 54, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, true, false)); // Alt+PageDown
        }
    }

    // PuTTy: Home and End keys: rxvt
    internal static IEnumerable<(byte[], ConsoleKeyInfo)> RxvtHomeAndEndKeys
    {
        get
        {
            yield return (new byte[] { 27, 91, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, false)); // Home
            yield return (new byte[] { 27, 27, 91, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, true, false)); // Alt+Home
            yield return (new byte[] { 27, 79, 119 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, false, false)); // End
            yield return (new byte[] { 27, 27, 79, 119 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, true, false)); // Alt+End
            yield return (new byte[] { 27, 91, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, false, false)); // PageUp
            yield return (new byte[] { 27, 27, 91, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, true, false)); // Alt+PageUp
            yield return (new byte[] { 27, 91, 54, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, false, false)); // PageDown
            yield return (new byte[] { 27, 27, 91, 54, 126 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, true, false)); // Alt+PageDown
        }
    }

    // PuTTy: The function keys and keypad: ESC[n~
    internal static IEnumerable<(byte[], ConsoleKeyInfo)> ESCnFunctionKeys
    {
        get
        {
            yield return (new byte[] { 27, 91, 49, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F1, false, false, false)); // F1
            yield return (new byte[] { 27, 91, 49, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F2, false, false, false)); // F2
            yield return (new byte[] { 27, 91, 49, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, false, false)); // F3
            yield return (new byte[] { 27, 91, 49, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F4, false, false, false)); // F4
            yield return (new byte[] { 27, 91, 49, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, false, false, false)); // F5
            yield return (new byte[] { 27, 91, 49, 55, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, false, false, false)); // F6
            yield return (new byte[] { 27, 91, 49, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, false, false, false)); // F7
            yield return (new byte[] { 27, 91, 49, 57, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, false, false, false)); // F8
            yield return (new byte[] { 27, 91, 50, 48, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, false, false)); // F9
            yield return (new byte[] { 27, 91, 50, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F10, false, false, false)); // F10
            yield return (new byte[] { 27, 91, 50, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, false, false)); // F11
            yield return (new byte[] { 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, false)); // F12
            yield return (new byte[] { 27, 27, 91, 49, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F1, false, true, false)); // Alt+F1
            yield return (new byte[] { 27, 27, 91, 49, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F2, false, true, false)); // Alt+F2
            yield return (new byte[] { 27, 27, 91, 49, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, true, false)); // Alt+F3
            yield return (new byte[] { 27, 27, 91, 49, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, false, true, false)); // Alt+F5
            yield return (new byte[] { 27, 27, 91, 49, 55, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, false, true, false)); // Alt+F6
            yield return (new byte[] { 27, 27, 91, 49, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, false, true, false)); // Alt+F7
            yield return (new byte[] { 27, 27, 91, 49, 57, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, false, true, false)); // Alt+F8
            yield return (new byte[] { 27, 27, 91, 50, 48, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, true, false)); // Alt+F9
            yield return (new byte[] { 27, 27, 91, 50, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F10, false, true, false)); // Alt+F10
            yield return (new byte[] { 27, 27, 91, 50, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, true, false)); // Alt+F11
            yield return (new byte[] { 27, 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, true, false)); // Alt+F12
            yield return (new byte[] { 27, 91, 50, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, false, false)); // Shift+F1=F11
            yield return (new byte[] { 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, false)); // Shift+F2=F12
            yield return (new byte[] { 27, 91, 50, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F13, false, false, false)); // Shift+F3=F13
            yield return (new byte[] { 27, 91, 50, 54, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F14, false, false, false)); // Shift+F4=F14
            yield return (new byte[] { 27, 91, 50, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F15, false, false, false)); // Shift+F5=F15
            yield return (new byte[] { 27, 91, 50, 57, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F16, false, false, false)); // Shift+F6=F16
            yield return (new byte[] { 27, 91, 51, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F17, false, false, false)); // Shift+F7=F17
            yield return (new byte[] { 27, 91, 51, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F18, false, false, false)); // Shift+F8=F18
            yield return (new byte[] { 27, 91, 51, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F19, false, false, false)); // Shift+F9=F19
            yield return (new byte[] { 27, 91, 51, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F20, false, false, false)); // Shift+F10=F20
        }
    }

    // PuTTy: The function keys and keypad: Linux
    internal static IEnumerable<(byte[], ConsoleKeyInfo)> LinuxFunctionKeys
    {
        get
        {
            yield return (new byte[] { 27, 91, 91, 65 }, new ConsoleKeyInfo(default, ConsoleKey.F1, false, false, false)); // F1
            yield return (new byte[] { 27, 91, 91, 66 }, new ConsoleKeyInfo(default, ConsoleKey.F2, false, false, false)); // F2
            yield return (new byte[] { 27, 91, 91, 67 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, false, false)); // F3
            yield return (new byte[] { 27, 91, 91, 68 }, new ConsoleKeyInfo(default, ConsoleKey.F4, false, false, false)); // F4
            yield return (new byte[] { 27, 91, 91, 69 }, new ConsoleKeyInfo(default, ConsoleKey.F5, false, false, false)); // F5
            yield return (new byte[] { 27, 91, 49, 55, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, false, false, false)); // F6
            yield return (new byte[] { 27, 91, 49, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, false, false, false)); // F7
            yield return (new byte[] { 27, 91, 49, 57, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, false, false, false)); // F8
            yield return (new byte[] { 27, 91, 50, 48, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, false, false)); // F9
            yield return (new byte[] { 27, 91, 50, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F10, false, false, false)); // F10
            yield return (new byte[] { 27, 91, 50, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, false, false)); // F11
            yield return (new byte[] { 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, false)); // F12
            yield return (new byte[] { 27, 27, 91, 91, 65 }, new ConsoleKeyInfo(default, ConsoleKey.F1, false, true, false)); // Alt+F1
            yield return (new byte[] { 27, 27, 91, 91, 66 }, new ConsoleKeyInfo(default, ConsoleKey.F2, false, true, false)); // Alt+F2
            yield return (new byte[] { 27, 27, 91, 91, 67 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, true, false)); // Alt+F3
            yield return (new byte[] { 27, 27, 91, 91, 69 }, new ConsoleKeyInfo(default, ConsoleKey.F5, false, true, false)); // Alt+F5
            yield return (new byte[] { 27, 27, 91, 49, 55, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, false, true, false)); // Alt+F6
            yield return (new byte[] { 27, 27, 91, 49, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, false, true, false)); // Alt+F7
            yield return (new byte[] { 27, 27, 91, 49, 57, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, false, true, false)); // Alt+F8
            yield return (new byte[] { 27, 27, 91, 50, 48, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, true, false)); // Alt+F9
            yield return (new byte[] { 27, 27, 91, 50, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F10, false, true, false)); // Alt+F10
            yield return (new byte[] { 27, 27, 91, 50, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, true, false)); // Alt+F11
            yield return (new byte[] { 27, 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, true, false)); // Alt+F12
            yield return (new byte[] { 27, 91, 50, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, false, false)); // Shift+F1=F11
            yield return (new byte[] { 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, false)); // Shift+F2=F12
            yield return (new byte[] { 27, 91, 50, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F13, false, false, false)); // Shift+F3=F13
            yield return (new byte[] { 27, 91, 50, 54, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F14, false, false, false)); // Shift+F4=F14
            yield return (new byte[] { 27, 91, 50, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F15, false, false, false)); // Shift+F5=F15
            yield return (new byte[] { 27, 91, 50, 57, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F16, false, false, false)); // Shift+F6=F16
            yield return (new byte[] { 27, 91, 51, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F17, false, false, false)); // Shift+F7=F17
            yield return (new byte[] { 27, 91, 51, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F18, false, false, false)); // Shift+F8=F18
            yield return (new byte[] { 27, 91, 51, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F19, false, false, false)); // Shift+F9=F19
            yield return (new byte[] { 27, 91, 51, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F20, false, false, false)); // Shift+F10=F20
        }
    }

    // PuTTy: The function keys and keypad: Xterm R6
    internal static IEnumerable<(byte[], ConsoleKeyInfo)> XtermR6FunctionKeys
    {
        get
        {
            yield return (new byte[] { 27, 79, 80 }, new ConsoleKeyInfo(default, ConsoleKey.F1, false, false, false)); // F1
            yield return (new byte[] { 27, 79, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F2, false, false, false)); // F2
            yield return (new byte[] { 27, 79, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, false, false)); // F3
            yield return (new byte[] { 27, 79, 83 }, new ConsoleKeyInfo(default, ConsoleKey.F4, false, false, false)); // F4
            yield return (new byte[] { 27, 91, 49, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, false, false, false)); // F5
            yield return (new byte[] { 27, 91, 49, 55, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, false, false, false)); // F6
            yield return (new byte[] { 27, 91, 49, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, false, false, false)); // F7
            yield return (new byte[] { 27, 91, 49, 57, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, false, false, false)); // F8
            yield return (new byte[] { 27, 91, 50, 48, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, false, false)); // F9
            yield return (new byte[] { 27, 91, 50, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F10, false, false, false)); // F10
            yield return (new byte[] { 27, 91, 50, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, false, false)); // F11
            yield return (new byte[] { 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, false)); // F12
            yield return (new byte[] { 27, 27, 79, 80 }, new ConsoleKeyInfo(default, ConsoleKey.F1, false, true, false)); // Alt+F1
            yield return (new byte[] { 27, 27, 79, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F2, false, true, false)); // Alt+F2
            yield return (new byte[] { 27, 27, 79, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, true, false)); // Alt+F3
            yield return (new byte[] { 27, 27, 91, 49, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, false, true, false)); // Alt+F5
            yield return (new byte[] { 27, 27, 91, 49, 55, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, false, true, false)); // Alt+F6
            yield return (new byte[] { 27, 27, 91, 49, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, false, true, false)); // Alt+F7
            yield return (new byte[] { 27, 27, 91, 49, 57, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, false, true, false)); // Alt+F8
            yield return (new byte[] { 27, 27, 91, 50, 48, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, true, false)); // Alt+F9
            yield return (new byte[] { 27, 27, 91, 50, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F10, false, true, false)); // Alt+F10
            yield return (new byte[] { 27, 27, 91, 50, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, true, false)); // Alt+F11
            yield return (new byte[] { 27, 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, true, false)); // Alt+F12
            yield return (new byte[] { 27, 91, 50, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, false, false)); // Shift+F1=F11
            yield return (new byte[] { 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, false)); // Shift+F2=F12
            yield return (new byte[] { 27, 91, 50, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F13, false, false, false)); // Shift+F3=F13
            yield return (new byte[] { 27, 91, 50, 54, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F14, false, false, false)); // Shift+F4=F14
            yield return (new byte[] { 27, 91, 50, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F15, false, false, false)); // Shift+F5=F15
            yield return (new byte[] { 27, 91, 50, 57, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F16, false, false, false)); // Shift+F6=F16
            yield return (new byte[] { 27, 91, 51, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F17, false, false, false)); // Shift+F7=F17
            yield return (new byte[] { 27, 91, 51, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F18, false, false, false)); // Shift+F8=F18
            yield return (new byte[] { 27, 91, 51, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F19, false, false, false)); // Shift+F9=F19
            yield return (new byte[] { 27, 91, 51, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F20, false, false, false)); // Shift+F10=F20
        }
    }

    // PuTTy: The function keys and keypad: VT 400
    internal static IEnumerable<(byte[], ConsoleKeyInfo)> VT400FunctionKeys
    {
        get
        {
            yield return (new byte[] { 27, 91, 49, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F1, false, false, false)); // F1
            yield return (new byte[] { 27, 91, 49, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F2, false, false, false)); // F2
            yield return (new byte[] { 27, 91, 49, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, false, false)); // F3
            yield return (new byte[] { 27, 91, 49, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F4, false, false, false)); // F4
            yield return (new byte[] { 27, 91, 49, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, false, false, false)); // F5
            yield return (new byte[] { 27, 91, 49, 55, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, false, false, false)); // F6
            yield return (new byte[] { 27, 91, 49, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, false, false, false)); // F7
            yield return (new byte[] { 27, 91, 49, 57, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, false, false, false)); // F8
            yield return (new byte[] { 27, 91, 50, 48, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, false, false)); // F9
            yield return (new byte[] { 27, 91, 50, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F10, false, false, false)); // F10
            yield return (new byte[] { 27, 91, 50, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, false, false)); // F11
            yield return (new byte[] { 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, false)); // F12
            yield return (new byte[] { 27, 27, 91, 49, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F1, false, true, false)); // Alt+F1
            yield return (new byte[] { 27, 27, 91, 49, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F2, false, true, false)); // Alt+F2
            yield return (new byte[] { 27, 27, 91, 49, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, true, false)); // Alt+F3
            yield return (new byte[] { 27, 27, 91, 49, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, false, true, false)); // Alt+F5
            yield return (new byte[] { 27, 27, 91, 49, 55, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, false, true, false)); // Alt+F6
            yield return (new byte[] { 27, 27, 91, 49, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, false, true, false)); // Alt+F7
            yield return (new byte[] { 27, 27, 91, 49, 57, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, false, true, false)); // Alt+F8
            yield return (new byte[] { 27, 27, 91, 50, 48, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, true, false)); // Alt+F9
            yield return (new byte[] { 27, 27, 91, 50, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F10, false, true, false)); // Alt+F10
            yield return (new byte[] { 27, 27, 91, 50, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, true, false)); // Alt+F11
            yield return (new byte[] { 27, 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, true, false)); // Alt+F12
            yield return (new byte[] { 27, 91, 50, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, false, false)); // Shift+F1=F11
            yield return (new byte[] { 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, false)); // Shift+F2=F12
            yield return (new byte[] { 27, 91, 50, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F13, false, false, false)); // Shift+F3=F13
            yield return (new byte[] { 27, 91, 50, 54, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F14, false, false, false)); // Shift+F4=F14
            yield return (new byte[] { 27, 91, 50, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F15, false, false, false)); // Shift+F5=F15
            yield return (new byte[] { 27, 91, 50, 57, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F16, false, false, false)); // Shift+F6=F16
            yield return (new byte[] { 27, 91, 51, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F17, false, false, false)); // Shift+F7=F17
            yield return (new byte[] { 27, 91, 51, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F18, false, false, false)); // Shift+F8=F18
            yield return (new byte[] { 27, 91, 51, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F19, false, false, false)); // Shift+F9=F19
            yield return (new byte[] { 27, 91, 51, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F20, false, false, false)); // Shift+F10=F20
        }
    }

    // PuTTy: The function keys and keypad: VT 100+
    internal static IEnumerable<(byte[], ConsoleKeyInfo)> VT100FunctionKeys
    {
        get
        {
            yield return (new byte[] { 27, 79, 80 }, new ConsoleKeyInfo(default, ConsoleKey.F1, false, false, false)); // F1
            yield return (new byte[] { 27, 79, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F2, false, false, false)); // F2
            yield return (new byte[] { 27, 79, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, false, false)); // F3
            yield return (new byte[] { 27, 79, 83 }, new ConsoleKeyInfo(default, ConsoleKey.F4, false, false, false)); // F4
            yield return (new byte[] { 27, 79, 84 }, new ConsoleKeyInfo(default, ConsoleKey.F5, false, false, false)); // F5
            yield return (new byte[] { 27, 79, 85 }, new ConsoleKeyInfo(default, ConsoleKey.F6, false, false, false)); // F6
            yield return (new byte[] { 27, 79, 86 }, new ConsoleKeyInfo(default, ConsoleKey.F7, false, false, false)); // F7
            yield return (new byte[] { 27, 79, 87 }, new ConsoleKeyInfo(default, ConsoleKey.F8, false, false, false)); // F8
            yield return (new byte[] { 27, 79, 88 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, false, false)); // F9
            yield return (new byte[] { 27, 79, 89 }, new ConsoleKeyInfo(default, ConsoleKey.F10, false, false, false)); // F10
            yield return (new byte[] { 27, 79, 90 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, false, false)); // F11
            yield return (new byte[] { 27, 79, 91 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, false)); // F12
            yield return (new byte[] { 27, 27, 79, 80 }, new ConsoleKeyInfo(default, ConsoleKey.F1, false, true, false)); // Alt+F1
            yield return (new byte[] { 27, 27, 79, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F2, false, true, false)); // Alt+F2
            yield return (new byte[] { 27, 27, 79, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, true, false)); // Alt+F3
            yield return (new byte[] { 27, 27, 79, 84 }, new ConsoleKeyInfo(default, ConsoleKey.F5, false, true, false)); // Alt+F5
            yield return (new byte[] { 27, 27, 79, 85 }, new ConsoleKeyInfo(default, ConsoleKey.F6, false, true, false)); // Alt+F6
            yield return (new byte[] { 27, 27, 79, 86 }, new ConsoleKeyInfo(default, ConsoleKey.F7, false, true, false)); // Alt+F7
            yield return (new byte[] { 27, 27, 79, 87 }, new ConsoleKeyInfo(default, ConsoleKey.F8, false, true, false)); // Alt+F8
            yield return (new byte[] { 27, 27, 79, 88 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, true, false)); // Alt+F9
            yield return (new byte[] { 27, 27, 79, 89 }, new ConsoleKeyInfo(default, ConsoleKey.F10, false, true, false)); // Alt+F10
            yield return (new byte[] { 27, 27, 79, 90 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, true, false)); // Alt+F11
            yield return (new byte[] { 27, 27, 79, 91 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, true, false)); // Alt+F12
            yield return (new byte[] { 27, 79, 90 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, false, false)); // Shift+F1=F11
            yield return (new byte[] { 27, 79, 91 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, false)); // Shift+F2=F12
            yield return (new byte[] { 27, 91, 50, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F13, false, false, false)); // Shift+F3=F13
            yield return (new byte[] { 27, 91, 50, 54, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F14, false, false, false)); // Shift+F4=F14
            yield return (new byte[] { 27, 91, 50, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F15, false, false, false)); // Shift+F5=F15
            yield return (new byte[] { 27, 91, 50, 57, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F16, false, false, false)); // Shift+F6=F16
            yield return (new byte[] { 27, 91, 51, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F17, false, false, false)); // Shift+F7=F17
            yield return (new byte[] { 27, 91, 51, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F18, false, false, false)); // Shift+F8=F18
            yield return (new byte[] { 27, 91, 51, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F19, false, false, false)); // Shift+F9=F19
            yield return (new byte[] { 27, 91, 51, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F20, false, false, false)); // Shift+F10=F20
        }
    }

    // PuTTy: The function keys and keypad: SCO
    internal static IEnumerable<(byte[], ConsoleKeyInfo)> SCOFunctionKeys
    {
        get
        {
            yield return (new byte[] { 27, 91, 77 }, new ConsoleKeyInfo(default, ConsoleKey.F1, false, false, false)); // F1
            yield return (new byte[] { 27, 91, 78 }, new ConsoleKeyInfo(default, ConsoleKey.F2, false, false, false)); // F2
            yield return (new byte[] { 27, 91, 79 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, false, false)); // F3
            yield return (new byte[] { 27, 91, 80 }, new ConsoleKeyInfo(default, ConsoleKey.F4, false, false, false)); // F4
            yield return (new byte[] { 27, 91, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F5, false, false, false)); // F5
            yield return (new byte[] { 27, 91, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F6, false, false, false)); // F6
            yield return (new byte[] { 27, 91, 83 }, new ConsoleKeyInfo(default, ConsoleKey.F7, false, false, false)); // F7
            yield return (new byte[] { 27, 91, 84 }, new ConsoleKeyInfo(default, ConsoleKey.F8, false, false, false)); // F8
            yield return (new byte[] { 27, 91, 85 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, false, false)); // F9
            yield return (new byte[] { 27, 91, 86 }, new ConsoleKeyInfo(default, ConsoleKey.F10, false, false, false)); // F10
            yield return (new byte[] { 27, 91, 87 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, false, false)); // F11
            yield return (new byte[] { 27, 91, 88 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, false)); // F12
            yield return (new byte[] { 27, 91, 107 }, new ConsoleKeyInfo(default, ConsoleKey.F1, false, false, true)); // Ctrl+F1
            yield return (new byte[] { 27, 91, 108 }, new ConsoleKeyInfo(default, ConsoleKey.F2, false, false, true)); // Ctrl+F2
            yield return (new byte[] { 27, 91, 109 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, false, true)); // Ctrl+F3
            yield return (new byte[] { 27, 91, 110 }, new ConsoleKeyInfo(default, ConsoleKey.F4, false, false, true)); // Ctrl+F4
            yield return (new byte[] { 27, 91, 111 }, new ConsoleKeyInfo(default, ConsoleKey.F5, false, false, true)); // Ctrl+F5
            yield return (new byte[] { 27, 91, 112 }, new ConsoleKeyInfo(default, ConsoleKey.F6, false, false, true)); // Ctrl+F6
            yield return (new byte[] { 27, 91, 113 }, new ConsoleKeyInfo(default, ConsoleKey.F7, false, false, true)); // Ctrl+F7
            yield return (new byte[] { 27, 91, 114 }, new ConsoleKeyInfo(default, ConsoleKey.F8, false, false, true)); // Ctrl+F8
            yield return (new byte[] { 27, 91, 115 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, false, true)); // Ctrl+F9
            yield return (new byte[] { 27, 91, 116 }, new ConsoleKeyInfo(default, ConsoleKey.F10, false, false, true)); // Ctrl+F10
            yield return (new byte[] { 27, 91, 117 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, false, true)); // Ctrl+F11
            yield return (new byte[] { 27, 91, 118 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, true)); // Ctrl+F12
            yield return (new byte[] { 27, 27, 91, 77 }, new ConsoleKeyInfo(default, ConsoleKey.F1, false, true, false)); // Alt+F1
            yield return (new byte[] { 27, 27, 91, 78 }, new ConsoleKeyInfo(default, ConsoleKey.F2, false, true, false)); // Alt+F2
            yield return (new byte[] { 27, 27, 91, 79 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, true, false)); // Alt+F3
            yield return (new byte[] { 27, 27, 91, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F5, false, true, false)); // Alt+F5
            yield return (new byte[] { 27, 27, 91, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F6, false, true, false)); // Alt+F6
            yield return (new byte[] { 27, 27, 91, 83 }, new ConsoleKeyInfo(default, ConsoleKey.F7, false, true, false)); // Alt+F7
            yield return (new byte[] { 27, 27, 91, 84 }, new ConsoleKeyInfo(default, ConsoleKey.F8, false, true, false)); // Alt+F8
            yield return (new byte[] { 27, 27, 91, 85 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, true, false)); // Alt+F9
            yield return (new byte[] { 27, 27, 91, 86 }, new ConsoleKeyInfo(default, ConsoleKey.F10, false, true, false)); // Alt+F10
            yield return (new byte[] { 27, 27, 91, 87 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, true, false)); // Alt+F11
            yield return (new byte[] { 27, 27, 91, 88 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, true, false)); // Alt+F12
            yield return (new byte[] { 27, 91, 89 }, new ConsoleKeyInfo(default, ConsoleKey.F1, true, false, false)); // Shift+F1
            // { 27, 91, 90 } is not supported in this case as Terminfo binds it to Shift+Tab (Backtab)
            // yield return (new byte[] { 27, 91, 90 }, new ConsoleKeyInfo(default, ConsoleKey.F2, true, false, false)); // Shift+F2
            yield return (new byte[] { 27, 91, 97 }, new ConsoleKeyInfo(default, ConsoleKey.F3, true, false, false)); // Shift+F3
            yield return (new byte[] { 27, 91, 98 }, new ConsoleKeyInfo(default, ConsoleKey.F4, true, false, false)); // Shift+F4
            yield return (new byte[] { 27, 91, 99 }, new ConsoleKeyInfo(default, ConsoleKey.F5, true, false, false)); // Shift+F5
            yield return (new byte[] { 27, 91, 100 }, new ConsoleKeyInfo(default, ConsoleKey.F6, true, false, false)); // Shift+F6
            yield return (new byte[] { 27, 91, 101 }, new ConsoleKeyInfo(default, ConsoleKey.F7, true, false, false)); // Shift+F7
            yield return (new byte[] { 27, 91, 102 }, new ConsoleKeyInfo(default, ConsoleKey.F8, true, false, false)); // Shift+F8
            yield return (new byte[] { 27, 91, 103 }, new ConsoleKeyInfo(default, ConsoleKey.F9, true, false, false)); // Shift+F9
            yield return (new byte[] { 27, 91, 104 }, new ConsoleKeyInfo(default, ConsoleKey.F10, true, false, false)); // Shift+F10
            yield return (new byte[] { 27, 91, 105 }, new ConsoleKeyInfo(default, ConsoleKey.F11, true, false, false)); // Shift+F11
            yield return (new byte[] { 27, 91, 106 }, new ConsoleKeyInfo(default, ConsoleKey.F12, true, false, false)); // Shift+F12
            yield return (new byte[] { 27, 91, 119 }, new ConsoleKeyInfo(default, ConsoleKey.F1, true, false, true)); // Ctrl+Shift+F1
            yield return (new byte[] { 27, 91, 120 }, new ConsoleKeyInfo(default, ConsoleKey.F2, true, false, true)); // Ctrl+Shift+F2
            yield return (new byte[] { 27, 91, 121 }, new ConsoleKeyInfo(default, ConsoleKey.F3, true, false, true)); // Ctrl+Shift+F3
            yield return (new byte[] { 27, 91, 122 }, new ConsoleKeyInfo(default, ConsoleKey.F4, true, false, true)); // Ctrl+Shift+F4
            yield return (new byte[] { 27, 91, 64 }, new ConsoleKeyInfo(default, ConsoleKey.F5, true, false, true)); // Ctrl+Shift+F5
            yield return (new byte[] { 27, 91, 91 }, new ConsoleKeyInfo(default, ConsoleKey.F6, true, false, true)); // Ctrl+Shift+F6
            yield return (new byte[] { 27, 91, 92 }, new ConsoleKeyInfo(default, ConsoleKey.F7, true, false, true)); // Ctrl+Shift+F7
            yield return (new byte[] { 27, 91, 93 }, new ConsoleKeyInfo(default, ConsoleKey.F8, true, false, true)); // Ctrl+Shift+F8
            yield return (new byte[] { 27, 91, 94 }, new ConsoleKeyInfo(default, ConsoleKey.F9, true, false, true)); // Ctrl+Shift+F9
            yield return (new byte[] { 27, 91, 95 }, new ConsoleKeyInfo(default, ConsoleKey.F10, true, false, true)); // Ctrl+Shift+F10
            yield return (new byte[] { 27, 91, 96 }, new ConsoleKeyInfo(default, ConsoleKey.F11, true, false, true)); // Ctrl+Shift+F11
            yield return (new byte[] { 27, 91, 123 }, new ConsoleKeyInfo(default, ConsoleKey.F12, true, false, true)); // Ctrl+Shift+F12
            yield return (new byte[] { 27, 27, 91, 119 }, new ConsoleKeyInfo(default, ConsoleKey.F1, true, true, true)); // Ctrl+Alt+Shift+F1
            yield return (new byte[] { 27, 27, 91, 120 }, new ConsoleKeyInfo(default, ConsoleKey.F2, true, true, true)); // Ctrl+Alt+Shift+F2
            yield return (new byte[] { 27, 27, 91, 121 }, new ConsoleKeyInfo(default, ConsoleKey.F3, true, true, true)); // Ctrl+Alt+Shift+F3
            yield return (new byte[] { 27, 27, 91, 64 }, new ConsoleKeyInfo(default, ConsoleKey.F5, true, true, true)); // Ctrl+Alt+Shift+F5
            yield return (new byte[] { 27, 27, 91, 91 }, new ConsoleKeyInfo(default, ConsoleKey.F6, true, true, true)); // Ctrl+Alt+Shift+F6
            yield return (new byte[] { 27, 27, 91, 92 }, new ConsoleKeyInfo(default, ConsoleKey.F7, true, true, true)); // Ctrl+Alt+Shift+F7
            yield return (new byte[] { 27, 27, 91, 93 }, new ConsoleKeyInfo(default, ConsoleKey.F8, true, true, true)); // Ctrl+Alt+Shift+F8
            yield return (new byte[] { 27, 27, 91, 94 }, new ConsoleKeyInfo(default, ConsoleKey.F9, true, true, true)); // Ctrl+Alt+Shift+F9
            yield return (new byte[] { 27, 27, 91, 95 }, new ConsoleKeyInfo(default, ConsoleKey.F10, true, true, true)); // Ctrl+Alt+Shift+F10
            yield return (new byte[] { 27, 27, 91, 96 }, new ConsoleKeyInfo(default, ConsoleKey.F11, true, true, true)); // Ctrl+Alt+Shift+F11
            yield return (new byte[] { 27, 27, 91, 123 }, new ConsoleKeyInfo(default, ConsoleKey.F12, true, true, true)); // Ctrl+Alt+Shift+F12
        }
    }

    // PuTTy: The function keys and keypad: Xterm 216+
    internal static IEnumerable<(byte[], ConsoleKeyInfo)> Xterm216FunctionKeys
    {
        get
        {
            yield return (new byte[] { 27, 79, 80 }, new ConsoleKeyInfo(default, ConsoleKey.F1, false, false, false)); // F1
            yield return (new byte[] { 27, 79, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F2, false, false, false)); // F2
            yield return (new byte[] { 27, 79, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, false, false)); // F3
            yield return (new byte[] { 27, 79, 83 }, new ConsoleKeyInfo(default, ConsoleKey.F4, false, false, false)); // F4
            yield return (new byte[] { 27, 91, 49, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, false, false, false)); // F5
            yield return (new byte[] { 27, 91, 49, 55, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, false, false, false)); // F6
            yield return (new byte[] { 27, 91, 49, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, false, false, false)); // F7
            yield return (new byte[] { 27, 91, 49, 57, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, false, false, false)); // F8
            yield return (new byte[] { 27, 91, 50, 48, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, false, false)); // F9
            yield return (new byte[] { 27, 91, 50, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F10, false, false, false)); // F10
            yield return (new byte[] { 27, 91, 50, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, false, false)); // F11
            yield return (new byte[] { 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, false)); // F12
            yield return (new byte[] { 27, 91, 49, 59, 53, 80 }, new ConsoleKeyInfo(default, ConsoleKey.F1, false, false, true)); // Ctrl+F1
            yield return (new byte[] { 27, 91, 49, 59, 53, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F2, false, false, true)); // Ctrl+F2
            yield return (new byte[] { 27, 91, 49, 59, 53, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, false, true)); // Ctrl+F3
            yield return (new byte[] { 27, 91, 49, 59, 53, 83 }, new ConsoleKeyInfo(default, ConsoleKey.F4, false, false, true)); // Ctrl+F4
            yield return (new byte[] { 27, 91, 49, 53, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, false, false, true)); // Ctrl+F5
            yield return (new byte[] { 27, 91, 49, 55, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, false, false, true)); // Ctrl+F6
            yield return (new byte[] { 27, 91, 49, 56, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, false, false, true)); // Ctrl+F7
            yield return (new byte[] { 27, 91, 49, 57, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, false, false, true)); // Ctrl+F8
            yield return (new byte[] { 27, 91, 50, 48, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, false, true)); // Ctrl+F9
            yield return (new byte[] { 27, 91, 50, 49, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F10, false, false, true)); // Ctrl+F10
            yield return (new byte[] { 27, 91, 50, 51, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, false, true)); // Ctrl+F11
            yield return (new byte[] { 27, 91, 50, 52, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, true)); // Ctrl+F12
            yield return (new byte[] { 27, 27, 91, 49, 59, 51, 80 }, new ConsoleKeyInfo(default, ConsoleKey.F1, false, true, false)); // Alt+F1
            yield return (new byte[] { 27, 27, 91, 49, 59, 51, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F2, false, true, false)); // Alt+F2
            yield return (new byte[] { 27, 27, 91, 49, 59, 51, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, false, true, false)); // Alt+F3
            yield return (new byte[] { 27, 27, 91, 49, 53, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, false, true, false)); // Alt+F5
            yield return (new byte[] { 27, 27, 91, 49, 55, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, false, true, false)); // Alt+F6
            yield return (new byte[] { 27, 27, 91, 49, 56, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, false, true, false)); // Alt+F7
            yield return (new byte[] { 27, 27, 91, 49, 57, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, false, true, false)); // Alt+F8
            yield return (new byte[] { 27, 27, 91, 50, 48, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, false, true, false)); // Alt+F9
            yield return (new byte[] { 27, 27, 91, 50, 49, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F10, false, true, false)); // Alt+F10
            yield return (new byte[] { 27, 27, 91, 50, 51, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, false, true, false)); // Alt+F11
            yield return (new byte[] { 27, 27, 91, 50, 52, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, true, false)); // Alt+F12
            yield return (new byte[] { 27, 91, 49, 59, 50, 80 }, new ConsoleKeyInfo(default, ConsoleKey.F1, true, false, false)); // Shift+F1
            yield return (new byte[] { 27, 91, 49, 59, 50, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F2, true, false, false)); // Shift+F2
            yield return (new byte[] { 27, 91, 49, 59, 50, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, true, false, false)); // Shift+F3
            yield return (new byte[] { 27, 91, 49, 59, 50, 83 }, new ConsoleKeyInfo(default, ConsoleKey.F4, true, false, false)); // Shift+F4
            yield return (new byte[] { 27, 91, 49, 53, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, true, false, false)); // Shift+F5
            yield return (new byte[] { 27, 91, 49, 55, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, true, false, false)); // Shift+F6
            yield return (new byte[] { 27, 91, 49, 56, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, true, false, false)); // Shift+F7
            yield return (new byte[] { 27, 91, 49, 57, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, true, false, false)); // Shift+F8
            yield return (new byte[] { 27, 91, 50, 48, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, true, false, false)); // Shift+F9
            yield return (new byte[] { 27, 91, 50, 49, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F10, true, false, false)); // Shift+F10
            yield return (new byte[] { 27, 91, 50, 51, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, true, false, false)); // Shift+F11
            yield return (new byte[] { 27, 91, 50, 52, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, true, false, false)); // Shift+F12
            yield return (new byte[] { 27, 27, 91, 49, 59, 56, 80 }, new ConsoleKeyInfo(default, ConsoleKey.F1, true, true, true)); // Ctrl+Alt+Shift+F1
            yield return (new byte[] { 27, 27, 91, 49, 59, 56, 81 }, new ConsoleKeyInfo(default, ConsoleKey.F2, true, true, true)); // Ctrl+Alt+Shift+F2
            yield return (new byte[] { 27, 27, 91, 49, 59, 56, 82 }, new ConsoleKeyInfo(default, ConsoleKey.F3, true, true, true)); // Ctrl+Alt+Shift+F3
            yield return (new byte[] { 27, 27, 91, 49, 53, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F5, true, true, true)); // Ctrl+Alt+Shift+F5
            yield return (new byte[] { 27, 27, 91, 49, 55, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F6, true, true, true)); // Ctrl+Alt+Shift+F6
            yield return (new byte[] { 27, 27, 91, 49, 56, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F7, true, true, true)); // Ctrl+Alt+Shift+F7
            yield return (new byte[] { 27, 27, 91, 49, 57, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F8, true, true, true)); // Ctrl+Alt+Shift+F8
            yield return (new byte[] { 27, 27, 91, 50, 48, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F9, true, true, true)); // Ctrl+Alt+Shift+F9
            yield return (new byte[] { 27, 27, 91, 50, 49, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F10, true, true, true)); // Ctrl+Alt+Shift+F10
            yield return (new byte[] { 27, 27, 91, 50, 51, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F11, true, true, true)); // Ctrl+Alt+Shift+F11
            yield return (new byte[] { 27, 27, 91, 50, 52, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, true, true, true)); // Ctrl+Alt+Shift+F12
        }
    }

    // PuTTY: Shift/Ctrl/Alt with arrow keys: Ctrl toggles app mode
    internal static IEnumerable<(byte[], ConsoleKeyInfo)> CtrlTogglesAppModeArrows
    {
        get
        {
            yield return (new byte[] { 27, 79, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, false)); // LeftArrow
            yield return (new byte[] { 27, 27, 79, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, true, false)); // Alt+LeftArrow
            yield return (new byte[] { 27, 79, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, false, false, false)); // UpArrow
            yield return (new byte[] { 27, 27, 79, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, false, true, false)); // Alt+UpArrow
            yield return (new byte[] { 27, 79, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, false, false, false)); // DownArrow
            yield return (new byte[] { 27, 27, 79, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, false, true, false)); // Alt+DownArrow
            yield return (new byte[] { 27, 79, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, false, false, false)); // RightArrow
            yield return (new byte[] { 27, 27, 79, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, false, true, false)); // Alt+RightArrow
            // Ctrl and Shift key modifiers are not supported for PuTTy arrow keys as they use mappings that contradict data stored in Terminfo.
            // example: PuTTy configured with "putty" Terminal-type string sets TERM=putty.
            // "putty" Terminfo says that "27, 91, 68" (\[[D) stored under KeySLeft is LeftArrow with Shift,
            // but PuTTy itself uses it for Ctrl+LeftArrow.
        }
    }
}

public static class SCO
{
    internal static IEnumerable<(byte[], ConsoleKeyInfo)> ArrowKeys
    {
        get
        {
            yield return (new byte[] { 27, 91, 65 }, new ConsoleKeyInfo(default, ConsoleKey.UpArrow, false, false, false)); // UpArrow
            yield return (new byte[] { 27, 91, 66 }, new ConsoleKeyInfo(default, ConsoleKey.DownArrow, false, false, false)); // DownArrow
            yield return (new byte[] { 27, 91, 67 }, new ConsoleKeyInfo(default, ConsoleKey.RightArrow, false, false, false)); // LeftArrow
            yield return (new byte[] { 27, 91, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, false)); // RightArrow
        }
    }

    internal static IEnumerable<(byte[], ConsoleKeyInfo)> HomeKeys
    {
        get
        {
            yield return (new byte[] { 27, 91, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, false)); // Home
            yield return (new byte[] { 27, 91, 70 }, new ConsoleKeyInfo(default, ConsoleKey.End, false, false, false)); // End
            yield return (new byte[] { 27, 91, 73 }, new ConsoleKeyInfo(default, ConsoleKey.PageUp, false, false, false)); // PageUp
            yield return (new byte[] { 27, 91, 71 }, new ConsoleKeyInfo(default, ConsoleKey.PageDown, false, false, false)); // PageDown
        }
    }
}
