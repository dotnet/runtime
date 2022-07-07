// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace System.Tests;

public class KeyMapperTests
{
    private static readonly TerminalData[] Terminals =
    {
        new XTermData(),
        new GNOMETerminalData(),
        new UXTermData(),
        new PuTTYData_xterm(),
        new PuTTYData_linux(),
        new PuTTYData_putty(),
        new WindowsTerminalData()
    };

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
        }
    }

    [Theory]
    [MemberData(nameof(RecordedScenarios))]
    public void KeysAreProperlyMapped(TerminalData terminalData, byte[] recordedBytes, ConsoleKeyInfo expected)
    {
        char[] encoded = terminalData.ConsoleEncoding.GetString(recordedBytes).ToCharArray();

        ConsoleKeyInfo actual = Map(encoded, terminalData.TerminalDb, terminalData.Verase);

        Assert.Equal(expected.Key, actual.Key);
        Assert.Equal(expected.Modifiers, actual.Modifiers);
        Assert.Equal(expected.KeyChar, actual.KeyChar);
    }

    private static ConsoleKeyInfo Map(char[] chars, ConsolePal.TerminalFormatStrings terminalFormatStrings, byte verase)
    {
        int startIndex = 0;

        KeyMapper.MapNew(chars, terminalFormatStrings, 0, verase,
            out ConsoleKey consoleKey, out char ch, out bool isShift, out bool isAlt, out bool isCtrl, ref startIndex, chars.Length);
        //Assert.True(KeyMapper.MapBufferToConsoleKey(chars, terminalFormatStrings, 0, verase,
        //    out ConsoleKey consoleKey, out char ch, out bool isShift, out bool isAlt, out bool isCtrl, ref startIndex, chars.Length));
        Assert.True(startIndex > 0);

        return new ConsoleKeyInfo(ch, consoleKey, isShift, isAlt, isCtrl);
    }

    private static IEnumerable<(char, ConsoleKey)> AsciiKeys
    {
        get
        {
            yield return (' ', ConsoleKey.Spacebar);
            yield return ('\t', ConsoleKey.Tab);
            yield return ('\r', ConsoleKey.Enter);
            yield return ('\n', ConsoleKey.Enter);

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
    {
        get
        {
            foreach (TerminalData terminalData in Terminals)
            {
                foreach ((char ch, ConsoleKey key) in AsciiKeys)
                {
                    yield return new object[] { terminalData, ch, key };
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(AsciiCharactersArguments))]
    public void AsciiCharacters(TerminalData terminalData, char input, ConsoleKey expectedKey)
    {
        ConsoleKeyInfo consoleKeyInfo = Map(new[] { input }, terminalData.TerminalDb, terminalData.Verase);

        Assert.Equal(input, consoleKeyInfo.KeyChar);
        Assert.Equal(expectedKey, consoleKeyInfo.Key);
        Assert.Equal(char.IsAsciiLetterUpper(input) ? ConsoleModifiers.Shift : 0, consoleKeyInfo.Modifiers);
    }
}

public abstract class TerminalData
{
    private ConsolePal.TerminalFormatStrings? _terminalDb;
    private Encoding? _consoleEncoding;

    protected abstract string EncodingCharset { get; }
    protected abstract string Term { get; }
    protected abstract string EncodedTerminalDb { get; }
    internal abstract byte Verase { get; }
    internal abstract IEnumerable<(byte[], ConsoleKeyInfo)> RecordedScenarios { get; }

    internal ConsolePal.TerminalFormatStrings TerminalDb => _terminalDb ??=
        new ConsolePal.TerminalFormatStrings(new TermInfo.Database(Term, Convert.FromBase64String(EncodedTerminalDb)));

    internal Encoding ConsoleEncoding => _consoleEncoding ??= (string.IsNullOrEmpty(EncodingCharset) ? Encoding.Default : Encoding.GetEncoding(EncodingCharset)).RemovePreamble();
}

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
            yield return (new byte[] { 127 }, new ConsoleKeyInfo((char)8, ConsoleKey.Backspace, false, false, false));
            yield return (new byte[] { 27, 127 }, new ConsoleKeyInfo((char)8, ConsoleKey.Backspace, false, true, false));
            yield return (new byte[] { 27, 91, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, false, false));
            yield return (new byte[] { 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, false));
            yield return (new byte[] { 27, 91, 50, 52, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, true));
            yield return (new byte[] { 27, 91, 50, 52, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, true, false));
            yield return (new byte[] { 27, 91, 50, 52, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, true, false, false));
            yield return (new byte[] { 27, 91, 50, 52, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, true, true, true));
            yield return (new byte[] { 27, 79, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, false));
            yield return (new byte[] { 27, 91, 49, 59, 53, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, true));
            yield return (new byte[] { 27, 91, 49, 59, 51, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, true, false));
            yield return (new byte[] { 27, 91, 49, 59, 55, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, true, true));
            yield return (new byte[] { 27, 91, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Insert, false, false, false));
            yield return (new byte[] { 27, 79, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, false));
            yield return (new byte[] { 27, 91, 49, 59, 53, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, true));
            yield return (new byte[] { 27, 91, 49, 59, 51, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, true, false));
            yield return (new byte[] { 10 }, new ConsoleKeyInfo((char)10, ConsoleKey.Enter, false, false, false));
            yield return (new byte[] { 13 }, new ConsoleKeyInfo((char)13, ConsoleKey.Enter, false, false, false));
            yield return (new byte[] { 27, 10 }, new ConsoleKeyInfo((char)10, ConsoleKey.Enter, false, true, false));
            yield return (new byte[] { 49 }, new ConsoleKeyInfo('1', ConsoleKey.D1, false, false, false));
            yield return (new byte[] { 43 }, new ConsoleKeyInfo('+', ConsoleKey.Add, false, false, false));
            yield return (new byte[] { 45 }, new ConsoleKeyInfo('-', ConsoleKey.Subtract, false, false, false));
            yield return (new byte[] { 27, 79, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, false));
            yield return (new byte[] { 27, 91, 49, 59, 53, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, true));
            yield return (new byte[] { 27, 91, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Insert, false, false, false));
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
            yield return (new byte[] { 195, 161 }, new ConsoleKeyInfo('a', ConsoleKey.A, false, true, false));
            yield return (new byte[] { 194, 129 }, new ConsoleKeyInfo('a', ConsoleKey.A, false, true, true));
            yield return (new byte[] { 49 }, new ConsoleKeyInfo('1', ConsoleKey.D1, false, false, false));
            yield return (new byte[] { 194, 177 }, new ConsoleKeyInfo('1', ConsoleKey.D1, false, true, false));
            yield return (new byte[] { 33 }, new ConsoleKeyInfo('!', default, false, false, false));
            yield return (new byte[] { 50 }, new ConsoleKeyInfo('2', ConsoleKey.D2, false, false, false));
            yield return (new byte[] { 0 }, new ConsoleKeyInfo(default, ConsoleKey.D2, false, false, true));
            yield return (new byte[] { 194, 178 }, new ConsoleKeyInfo('2', ConsoleKey.D2, false, true, false));
            yield return (new byte[] { 64 }, new ConsoleKeyInfo('@', default, false, false, false));
            yield return (new byte[] { 61 }, new ConsoleKeyInfo('=', default, false, false, false));
            yield return (new byte[] { 43 }, new ConsoleKeyInfo('+', ConsoleKey.Add, false, false, false));
            yield return (new byte[] { 194, 189 }, new ConsoleKeyInfo('=', default, false, false, false));
            yield return (new byte[] { 27 }, new ConsoleKeyInfo((char)27, ConsoleKey.Escape, false, false, false));
            yield return (new byte[] { 127 }, new ConsoleKeyInfo((char)8, ConsoleKey.Backspace, false, false, false));
            yield return (new byte[] { 195, 191 }, new ConsoleKeyInfo((char)8, ConsoleKey.Backspace, false, true, false));
            yield return (new byte[] { 194, 136 }, new ConsoleKeyInfo((char)8, ConsoleKey.Backspace, false, true, true));
            yield return (new byte[] { 27, 91, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, false, false));
            yield return (new byte[] { 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, false));
            yield return (new byte[] { 27, 91, 50, 52, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, true));
            yield return (new byte[] { 27, 91, 50, 52, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, true, false));
            yield return (new byte[] { 27, 91, 50, 52, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, true, false, false));
            yield return (new byte[] { 27, 91, 50, 52, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, true, true, true));
            yield return (new byte[] { 27, 79, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, false));
            yield return (new byte[] { 27, 91, 49, 59, 53, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, true));
            yield return (new byte[] { 27, 91, 49, 59, 51, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, true, false));
            yield return (new byte[] { 27, 91, 49, 59, 55, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, true, true));
            yield return (new byte[] { 27, 91, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Insert, false, false, false));
            yield return (new byte[] { 27, 79, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, false));
            yield return (new byte[] { 27, 91, 49, 59, 53, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, true));
            yield return (new byte[] { 27, 91, 49, 59, 51, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, true, false));
            yield return (new byte[] { 10 }, new ConsoleKeyInfo((char)10, ConsoleKey.Enter, false, false, false));
            yield return (new byte[] { 49 }, new ConsoleKeyInfo('1', ConsoleKey.D1, false, false, false));
            yield return (new byte[] { 43 }, new ConsoleKeyInfo('+', ConsoleKey.Add, false, false, false));
            yield return (new byte[] { 45 }, new ConsoleKeyInfo('-', ConsoleKey.Subtract, false, false, false));
            yield return (new byte[] { 27, 79, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, false));
            yield return (new byte[] { 27, 91, 49, 59, 53, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, true));
            yield return (new byte[] { 27, 91, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Insert, false, false, false));
        }
    }
}

// Ubuntu 18.04 x64
public class UXTermData : TerminalData
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
            yield return (new byte[] { 195, 161 }, new ConsoleKeyInfo('a', ConsoleKey.A, false, true, false));
            yield return (new byte[] { 194, 129 }, new ConsoleKeyInfo('a', ConsoleKey.A, false, true, true));
            yield return (new byte[] { 49 }, new ConsoleKeyInfo('1', ConsoleKey.D1, false, false, false));
            yield return (new byte[] { 194, 177 }, new ConsoleKeyInfo('1', ConsoleKey.D1, false, true, false));
            yield return (new byte[] { 33 }, new ConsoleKeyInfo('!', default, false, false, false));
            yield return (new byte[] { 50 }, new ConsoleKeyInfo('2', ConsoleKey.D2, false, false, false));
            yield return (new byte[] { 0 }, new ConsoleKeyInfo(default, ConsoleKey.D2, false, false, true));
            yield return (new byte[] { 194, 178 }, new ConsoleKeyInfo('2', ConsoleKey.D2, false, true, false));
            yield return (new byte[] { 64 }, new ConsoleKeyInfo('@', default, false, false, false));
            yield return (new byte[] { 61 }, new ConsoleKeyInfo('=', default, false, false, false));
            yield return (new byte[] { 43 }, new ConsoleKeyInfo('+', ConsoleKey.Add, false, false, false));
            yield return (new byte[] { 194, 189 }, new ConsoleKeyInfo('=', default, false, false, false));
            yield return (new byte[] { 27 }, new ConsoleKeyInfo((char)27, ConsoleKey.Escape, false, false, false));
            yield return (new byte[] { 127 }, new ConsoleKeyInfo((char)8, ConsoleKey.Backspace, false, false, false));
            yield return (new byte[] { 195, 191 }, new ConsoleKeyInfo((char)8, ConsoleKey.Backspace, false, true, false));
            yield return (new byte[] { 194, 136 }, new ConsoleKeyInfo((char)8, ConsoleKey.Backspace, false, true, true));
            yield return (new byte[] { 27, 91, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, false, false));
            yield return (new byte[] { 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, false));
            yield return (new byte[] { 27, 91, 50, 52, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, true));
            yield return (new byte[] { 27, 91, 50, 52, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, true, false));
            yield return (new byte[] { 27, 91, 50, 52, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, true, false, false));
            yield return (new byte[] { 27, 91, 50, 52, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, true, true, true));
            yield return (new byte[] { 27, 79, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, false));
            yield return (new byte[] { 27, 91, 49, 59, 53, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, true));
            yield return (new byte[] { 27, 91, 49, 59, 51, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, true, false));
            yield return (new byte[] { 27, 91, 49, 59, 55, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, true, true));
            yield return (new byte[] { 27, 91, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Insert, false, false, false));
            yield return (new byte[] { 27, 79, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, false));
            yield return (new byte[] { 27, 91, 49, 59, 53, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, true));
            yield return (new byte[] { 27, 91, 49, 59, 51, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, true, false));
            yield return (new byte[] { 10 }, new ConsoleKeyInfo((char)10, ConsoleKey.Enter, false, false, false));
            yield return (new byte[] { 49 }, new ConsoleKeyInfo('1', ConsoleKey.D1, false, false, false));
            yield return (new byte[] { 43 }, new ConsoleKeyInfo('+', ConsoleKey.Add, false, false, false));
            yield return (new byte[] { 45 }, new ConsoleKeyInfo('-', ConsoleKey.Subtract, false, false, false));
            yield return (new byte[] { 27, 79, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, false));
            yield return (new byte[] { 27, 91, 49, 59, 53, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, true));
            yield return (new byte[] { 27, 91, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Insert, false, false, false));
        }
    }
}

// Windows 11 machine connected via PuTTY to Ubuntu 20.04 arm64 machine using default settings ("xterm" Terminal-type setting)
public class PuTTYData_xterm : TerminalData
{
    protected override string EncodingCharset => "";
    protected override string Term => "xterm";
    internal override byte Verase => 127;
    protected override string EncodedTerminalDb => "GgEpACYADwCdAaQFeHRlcm18eHRlcm0tZGViaWFufFgxMSB0ZXJtaW5hbCBlbXVsYXRvcgAAAQAAAQAAAAEAAAAAAQEAAAAAAAAAAQAAAQAAAQAAAAAAAAAAAQBQAAgAGAD//////////////////////////wgAQAAAAAQABgAIABkAHgAmACoALgD//zkASgBMAFAAVwD//1kAZgD//2oAbgB4AHwA/////4AAhACJAI4A//+gAKUAqgD//68AtAC5AL4AxwDLANIA///kAOkA7wD1AP///////wcB////////GQH//x0B////////HwH//yQB//////////8oASwBMgE2AToBPgFEAUoBUAFWAVwBYAH//2UB//9pAW4BcwF3AX4B//+FAYkBkQH/////////////////////////////mQGiAf////+rAbQBvQHGAc8B2AHhAeoB8wH8Af///////wUCCQIOAv//EwIWAv////8oAisCNgI5AjsCPgKbAv//ngL///////////////+gAv//////////pAL//9kC/////90C4wL/////////////////////////////6QLtAv//////////////////////////////////////////////////////////////////8QL/////+AL///////////8CBgMNA/////8UA///GwP///////8iA/////////////8pAy8DNQM8A0MDSgNRA1kDYQNpA3EDeQOBA4kDkQOYA58DpgOtA7UDvQPFA80D1QPdA+UD7QP0A/sDAgQJBBEEGQQhBCkEMQQ5BEEESQRQBFcEXgRlBG0EdQR9BIUEjQSVBJ0EpQSsBLMEugT/////////////////////////////////////////////////////////////vwTKBM8E4gTmBP//////////7wQ1Bf//////////////////ewX///////////////////////+ABf///////////////////////////////////////////////////////////////////////////////////////4YF////////igWUBf//////////////////////////////////////////////////////////////////////////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"; // /lib/terminfo/x/xterm

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
            yield return (new byte[] { 127 }, new ConsoleKeyInfo((char)8, ConsoleKey.Backspace, false, false, false));
            yield return (new byte[] { 27, 127 }, new ConsoleKeyInfo((char)8, ConsoleKey.Backspace, false, true, false));
            yield return (new byte[] { 27, 91, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, false, false));
            yield return (new byte[] { 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, false));
            yield return (new byte[] { 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, true));
            yield return (new byte[] { 27, 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, true, false));
            yield return (new byte[] { 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, true, false, false));
            yield return (new byte[] { 27, 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, true, true, true));
            yield return (new byte[] { 27, 91, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, false));
            yield return (new byte[] { 27, 27, 91, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, true, false));
            yield return (new byte[] { 27, 91, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Insert, false, false, false));
            yield return (new byte[] { 27, 79, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, false));
            yield return (new byte[] { 27, 91, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, true));
            yield return (new byte[] { 27, 27, 79, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, true, false));
            yield return (new byte[] { 10 }, new ConsoleKeyInfo((char)10, ConsoleKey.Enter, false, false, false));
            yield return (new byte[] { 13 }, new ConsoleKeyInfo((char)13, ConsoleKey.Enter, false, false, false));
            yield return (new byte[] { 27, 13 }, new ConsoleKeyInfo((char)13, ConsoleKey.Enter, false, true, false));
            yield return (new byte[] { 27, 10 }, new ConsoleKeyInfo((char)10, ConsoleKey.Enter, false, true, false));
        }
    }
}

// Windows 11 machine connected via PuTTY to Ubuntu 20.04 arm64 machine using "linux" Terminal-type setting
public class PuTTYData_linux : TerminalData
{
    protected override string EncodingCharset => "";
    protected override string Term => "linux";
    internal override byte Verase => 127;
    protected override string EncodedTerminalDb => "GgEUAB0AEAB9AUUDbGludXh8bGludXggY29uc29sZQAAAQAAAQEAAAAAAAAAAQEAAAAAAAEAAAAAAAABAQD//wgA/////////////////////////////wgAQAASAP//AAACAAQAFQAaACEAJQApAP//NABFAEcASwBXAP//WQBlAP//aQBtAHkAfQD/////gQCDAIgA/////40AkgD/////lwCcAKEApgCvALEA/////7YAuwDBAMcA////////////////2QDdAP//4QD////////jAP//6AD//////////+wA8QD3APwAAQEGAQsBEQEXAR0BIwEoAf//LQH//zEBNgE7Af///////z8B////////////////////////////////////////QwH//0YBTwFYAWEB//9qAXMBfAH//4UB//////////////////+OAf///////5QBlwGiAaUBpwGqAQEC//8EAv///////////////wYC//////////8KAv//TQL/////UQJXAv////9dAv////////////////////9hAv//////////////////////////////////////////////////ZgL//////////////////////////////////////////////////////////////////////////////////2gCbgJ0AnoCgAKGAowCkgKYAp4C//////////////////////////////////////////////////////////////////////////////////////////////////////////////////+kAv////////////////////////////////////////////////////////////+pArQCuQK/AsMCzALQAv//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////IQP///////8lAy8D////////////////////////////////////////////////OQM/AwcADQAbWyVpJXAxJWQ7JXAyJWRyABtbM2cAG1tIG1tKABtbSwAbW0oAG1slaSVwMSVkRwAbWyVpJXAxJWQ7JXAyJWRIAAoAG1tIABtbPzI1bBtbPzFjAAgAG1s/MjVoG1s/MGMAG1tDABtbQQAbWz8yNWgbWz84YwAbW1AAG1tNAA4AG1s1bQAbWzFtABtbMm0AG1s0aAAbWzdtABtbN20AG1s0bQAbWyVwMSVkWAAPABtbbQ8AG1s0bAAbWzI3bQAbWzI0bQAbWz81aCQ8MjAwLz4bWz81bAAbW0AAG1tMAH8AG1szfgAbW0IAG1tbQQAbWzIxfgAbW1tCABtbW0MAG1tbRAAbW1tFABtbMTd+ABtbMTh+ABtbMTl+ABtbMjB+ABtbMX4AG1syfgAbW0QAG1s2fgAbWzV+ABtbQwAbW0EADQoAG1slcDElZFAAG1slcDElZE0AG1slcDElZEIAG1slcDElZEAAG1slcDElZEwAG1slcDElZEQAG1slcDElZEMAG1slcDElZEEAG2MbXVIAGzgAG1slaSVwMSVkZAAbNwAKABtNABtbMDsxMCU/JXAxJXQ7NyU7JT8lcDIldDs0JTslPyVwMyV0OzclOyU/JXA0JXQ7NSU7JT8lcDUldDsyJTslPyVwNiV0OzElO20lPyVwOSV0DiVlDyU7ABtIAAkAG1tHACsrLCwtLS4uMDBfX2BgYWFmZmdnaGhpaWpqa2tsbG1tbm5vb3BwcXFycnNzdHR1dXZ2d3d4eHl5enp7e3x8fWN+fgAbW1oAG1s/N2gAG1s/N2wAGykwABtbNH4AGgAbWzIzfgAbWzI0fgAbWzI1fgAbWzI2fgAbWzI4fgAbWzI5fgAbWzMxfgAbWzMyfgAbWzMzfgAbWzM0fgAbWzFLABtbJWklZDslZFIAG1s2bgAbWz82YwAbW2MAG1szOTs0OW0AG11SABtdUCVwMSV4JXAyJXsyNTV9JSolezEwMDB9JS8lMDJ4JXAzJXsyNTV9JSolezEwMDB9JS8lMDJ4JXA0JXsyNTV9JSolezEwMDB9JS8lMDJ4ABtbTQAbWzMlcDElZG0AG1s0JXAxJWRtABtbMTFtABtbMTBtAAABAAEAAQAEAA4AAQABAAAAAAADAAYAG1szSgBBWABVOABFMwA="; // /lib/terminfo/l/linux

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
            yield return (new byte[] { 127 }, new ConsoleKeyInfo((char)8, ConsoleKey.Backspace, false, false, false));
            yield return (new byte[] { 27, 127 }, new ConsoleKeyInfo((char)8, ConsoleKey.Backspace, false, true, false));
            yield return (new byte[] { 27, 91, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, false, false));
            yield return (new byte[] { 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, false));
            yield return (new byte[] { 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, true));
            yield return (new byte[] { 27, 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, true, false));
            yield return (new byte[] { 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, true, false, false));
            yield return (new byte[] { 27, 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, true, true, true));
            yield return (new byte[] { 27, 91, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, false));
            yield return (new byte[] { 27, 27, 91, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, true, false));
            yield return (new byte[] { 27, 91, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Insert, false, false, false));
            yield return (new byte[] { 27, 91, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, false));
            yield return (new byte[] { 27, 79, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, true));
            yield return (new byte[] { 27, 27, 91, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, true, false));
            yield return (new byte[] { 13 }, new ConsoleKeyInfo((char)13, ConsoleKey.Enter, false, false, false));
            yield return (new byte[] { 27, 13 }, new ConsoleKeyInfo((char)13, ConsoleKey.Enter, false, true, false));
            yield return (new byte[] { 49 }, new ConsoleKeyInfo('1', ConsoleKey.D1, false, false, false));
            yield return (new byte[] { 43 }, new ConsoleKeyInfo('+', ConsoleKey.Add, false, false, false));
            yield return (new byte[] { 45 }, new ConsoleKeyInfo('-', ConsoleKey.Subtract, false, false, false));
            yield return (new byte[] { 27, 91, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, false));
            yield return (new byte[] { 27, 91, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Insert, false, false, false));
        }
    }
}

// Windows 11 machine connected via PuTTY to Ubuntu 20.04 arm64 machine using "putty" Terminal-type setting
public class PuTTYData_putty : TerminalData
{
    protected override string EncodingCharset => "";
    protected override string Term => "putty";
    internal override byte Verase => 127;
    protected override string EncodedTerminalDb => "GgEeAB0AEAB9AXwEcHV0dHl8UHVUVFkgdGVybWluYWwgZW11bGF0b3IAAQEAAAEAAAAAAQAAAAEBAAAAAAABAAAAAAAAAQEA//8IAP////////////////////////////8IAEAAFgAAAAQABgAIABkAHgAlACkALQD//zgASQBMAFAAVwD//1kAYAD//2QA//9nAGsAbwD//3UAdwB8AIEA/////4gA/////40AkgCXAJwApQCnAKwA//+3ALwAwgDIAP//2gD//9wA/////////gD//wIB////////BAH//wkB//////////8NARMBGQEfASUBKwExATcBPQFDAUkBTgH//1MB//9XAVwBYQFlAWkB//9tAXEBeQH//////////////////////////////////4EB//+EAY0BlgH//58BqAGxAboBwwHMAf/////////////////////VAf/////2AfkBBAIHAgkCDAJUAv//VwJZAv////////////9eAv//////////YgL//5UC/////5kCnwL/////pQL/////////////////////rAL//////////////////////////////////////////////////7EC//////////////////////////////////////////+zAv////////////////////+3Av////////////+7AsECxwLNAtMC2QLfAuUC6wLxAv//////////////////////////////////////////////////////////////////////////////////////////////////////////////////9wL//////////////////////////////////////////////////////////////AIHAwwDEgMWAx8DIwP//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////3QD////////eAOCA////////4wDkgOYA/////////////////////////////+eA3AEdgQbW1oABwANABtbJWklcDElZDslcDIlZHIAG1szZwAbW0gbW0oAG1tLABtbSgAbWyVpJXAxJWRHABtbJWklcDElZDslcDIlZEgAG0QAG1tIABtbPzI1bAAIABtbPzI1aAAbW0MAG00AG1tQABtbTQAbXTA7BwAOABtbNW0AG1sxbQAbWz80N2gAG1s0aAAbWzdtABtbN20AG1s0bQAbWyVwMSVkWAAPABtbbQ8AG1syShtbPzQ3bAAbWzRsABtbMjdtABtbMjRtABtbPzVoJDwxMDAvPhtbPzVsAAcAGzcbW3IbW20bWz83aBtbPzE7NDs2bBtbNGwbOBs+G11SABtbTAB/ABtbM34AG09CABtbMTF+ABtbMjF+ABtbMTJ+ABtbMTN+ABtbMTR+ABtbMTV+ABtbMTd+ABtbMTh+ABtbMTl+ABtbMjB+ABtbMX4AG1syfgAbT0QAG1s2fgAbWzV+ABtPQwAbW0IAG1tBABtPQQAbWz8xbBs+ABtbPzFoGz0ADQoAG1slcDElZFAAG1slcDElZE0AG1slcDElZEIAG1slcDElZFMAG1slcDElZEwAG1slcDElZEQAG1slcDElZEMAG1slcDElZFQAG1slcDElZEEAGzwbWyJwG1s1MDs2InAbYxtbPzNsG11SG1s/MTAwMGwAGzgAG1slaSVwMSVkZAAbNwAKABtNABtbMCU/JXAxJXA2JXwldDsxJTslPyVwMiV0OzQlOyU/JXAxJXAzJXwldDs3JTslPyVwNCV0OzUlO20lPyVwOSV0DiVlDyU7ABtIAAkAG10wOwAbW0cAYGBhYWZmZ2dqamtrbGxtbW5ub29wcHFxcnJzc3R0dXV2dnd3eHh5eXp6e3t8fH19fn4AG1taABtbPzdoABtbPzdsABsoQhspMAAbWzR+ABoAG1tEABtbQwAbWzIzfgAbWzI0fgAbWzI1fgAbWzI2fgAbWzI4fgAbWzI5fgAbWzMxfgAbWzMyfgAbWzMzfgAbWzM0fgAbWzFLABtbJWklZDslZFIAG1s2bgAbWz82YwAbW2MAG1szOTs0OW0AG11SABtdUCVwMSV4JXAyJXsyNTV9JSolezEwMDB9JS8lMDJ4JXAzJXsyNTV9JSolezEwMDB9JS8lMDJ4JXA0JXsyNTV9JSolezEwMDB9JS8lMDJ4ABtbPAAbWzMlcDElZG0AG1s0JXAxJWRtABtbMTBtABtbMTFtABtbMTJtACU/JXAxJXs4fSU9JXQbJSVH4peYGyUlQCVlJXAxJXsxMH0lPSV0GyUlR+KXmRslJUAlZSVwMSV7MTJ9JT0ldBslJUfimYAbJSVAJWUlcDElezEzfSU9JXQbJSVH4pmqGyUlQCVlJXAxJXsxNH0lPSV0GyUlR+KZqxslJUAlZSVwMSV7MTV9JT0ldBslJUfimLwbJSVAJWUlcDElezI3fSU9JXQbJSVH4oaQGyUlQCVlJXAxJXsxNTV9JT0ldBslJUfggqIbJSVAJWUlcDElYyU7ABtbMTFtABtbMTBtAAEAAQAEAAoAYQABAAEAAAAFAAoAKgAAAAMABgAJAAwADwAbWzNKABtdMDsAG1s/MTAwNjsxMDAwJT8lcDElezF9JT0ldGglZWwlOwAbWzwlaSVwMyVkOyVwMSVkOyVwMiVkOyU/JXA0JXRNJWVtJTsAWFQAVTgARTMAVFMAWE0AeG0A"; // /usr/share/terminfo/p/putty

    internal override IEnumerable<(byte[], ConsoleKeyInfo)> RecordedScenarios
    {
        get
        {
            yield return (new byte[] { 90 }, new ConsoleKeyInfo('Z', ConsoleKey.Z, true, false, false));
            yield return (new byte[] { 97 }, new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false));
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
            yield return (new byte[] { 127 }, new ConsoleKeyInfo((char)8, ConsoleKey.Backspace, false, false, false));
            yield return (new byte[] { 27, 127 }, new ConsoleKeyInfo((char)8, ConsoleKey.Backspace, false, true, false));
            yield return (new byte[] { 27, 91, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, false, false));
            yield return (new byte[] { 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, false));
            yield return (new byte[] { 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, true));
            yield return (new byte[] { 27, 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, true, false));
            yield return (new byte[] { 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, true, false, false));
            yield return (new byte[] { 27, 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, true, true, true));
            yield return (new byte[] { 27, 91, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, false));
            yield return (new byte[] { 27, 27, 91, 49, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, true, false));
            yield return (new byte[] { 27, 91, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Insert, false, false, false));
            yield return (new byte[] { 27, 79, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, false));
            yield return (new byte[] { 27, 91, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, true));
            yield return (new byte[] { 27, 27, 79, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, true, false));
            yield return (new byte[] { 13 }, new ConsoleKeyInfo((char)13, ConsoleKey.Enter, false, false, false));
        }
    }
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
            yield return (new byte[] { 127 }, new ConsoleKeyInfo((char)8, ConsoleKey.Backspace, false, false, false));
            yield return (new byte[] { 27, 127 }, new ConsoleKeyInfo((char)8, ConsoleKey.Backspace, false, true, false));
            yield return (new byte[] { 27, 91, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Delete, false, false, false));
            yield return (new byte[] { 27, 91, 50, 52, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, false));
            yield return (new byte[] { 27, 91, 50, 52, 59, 53, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, false, true));
            yield return (new byte[] { 27, 91, 50, 52, 59, 51, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, false, true, false));
            yield return (new byte[] { 27, 91, 50, 52, 59, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, true, false, false));
            yield return (new byte[] { 27, 91, 50, 52, 59, 56, 126 }, new ConsoleKeyInfo(default, ConsoleKey.F12, true, true, true));
            yield return (new byte[] { 27, 79, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, false));
            yield return (new byte[] { 27, 91, 49, 59, 53, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, true));
            yield return (new byte[] { 27, 91, 49, 59, 51, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, true, false));
            yield return (new byte[] { 27, 91, 49, 59, 55, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, true, true));
            yield return (new byte[] { 27, 91, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Insert, false, false, false));
            yield return (new byte[] { 27, 79, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, false));
            yield return (new byte[] { 27, 91, 49, 59, 53, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, false, true));
            yield return (new byte[] { 27, 91, 49, 59, 51, 68 }, new ConsoleKeyInfo(default, ConsoleKey.LeftArrow, false, true, false));
            yield return (new byte[] { 13 }, new ConsoleKeyInfo((char)13, ConsoleKey.Enter, false, false, false));
            yield return (new byte[] { 49 }, new ConsoleKeyInfo('1', ConsoleKey.D1, false, false, false));
            yield return (new byte[] { 43 }, new ConsoleKeyInfo('+', ConsoleKey.Add, false, false, false));
            yield return (new byte[] { 45 }, new ConsoleKeyInfo('-', ConsoleKey.Subtract, false, false, false));
            yield return (new byte[] { 27, 79, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, false));
            yield return (new byte[] { 27, 91, 49, 59, 53, 72 }, new ConsoleKeyInfo(default, ConsoleKey.Home, false, false, true));
            yield return (new byte[] { 27, 91, 50, 126 }, new ConsoleKeyInfo(default, ConsoleKey.Insert, false, false, false));
        }
    }
}


