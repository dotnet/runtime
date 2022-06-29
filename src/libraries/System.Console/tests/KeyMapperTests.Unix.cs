// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace System.Tests;

public class KeyMapperTests
{
    private static readonly ConsolePal.TerminalFormatStrings db_xterm_256color = new(new TermInfo.Database("xterm-256color", Convert.FromBase64String("GgElACYADwCdAQIGeHRlcm0tMjU2Y29sb3J8eHRlcm0gd2l0aCAyNTYgY29sb3JzAAABAAABAAAAAQAAAAABAQAAAAAAAAABAAABAAEBAAAAAAAAAAABAFAACAAYAP//////////////////////////AAH/fwAABAAGAAgAGQAeACYAKgAuAP//OQBKAEwAUABXAP//WQBmAP//agBuAHgAfAD/////gACEAIkAjgD//6AApQCqAP//rwC0ALkAvgDHAMsA0gD//+QA6QDvAPUA////////BwH///////8ZAf//HQH///////8fAf//JAH//////////ygBLAEyATYBOgE+AUQBSgFQAVYBXAFgAf//ZQH//2kBbgFzAXcBfgH//4UBiQGRAf////////////////////////////+ZAaIB/////6sBtAG9AcYBzwHYAeEB6gHzAfwB////////BQIJAg4CEwInAjAC/////0ICRQJQAlMCVQJYArUC//+4Av///////////////7oC//////////++Av//8wL/////9wL9Av////////////////////////////8DAwcD//////////////////////////////////////////////////////////////////8LA/////8SA///////////GQMgAycD/////y4D//81A////////zwD/////////////0MDSQNPA1YDXQNkA2sDcwN7A4MDiwOTA5sDowOrA7IDuQPAA8cDzwPXA98D5wPvA/cD/wMHBA4EFQQcBCMEKwQzBDsEQwRLBFMEWwRjBGoEcQR4BH8EhwSPBJcEnwSnBK8EtwS/BMYEzQTUBP/////////////////////////////////////////////////////////////ZBOQE6QT8BAAFCQUQBf////////////////////////////9uBf///////////////////////3MF////////////////////////////////////////////////////////////////////////////////////////eQX///////99BbwF//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////wF/wUbW1oABwANABtbJWklcDElZDslcDIlZHIAG1szZwAbW0gbWzJKABtbSwAbW0oAG1slaSVwMSVkRwAbWyVpJXAxJWQ7JXAyJWRIAAoAG1tIABtbPzI1bAAIABtbPzEybBtbPzI1aAAbW0MAG1tBABtbPzEyOzI1aAAbW1AAG1tNABsoMAAbWzVtABtbMW0AG1s/MTA0OWgbWzIyOzA7MHQAG1sybQAbWzRoABtbOG0AG1s3bQAbWzdtABtbNG0AG1slcDElZFgAGyhCABsoQhtbbQAbWz8xMDQ5bBtbMjM7MDswdAAbWzRsABtbMjdtABtbMjRtABtbPzVoJDwxMDAvPhtbPzVsABtbIXAbWz8zOzRsG1s0bBs+ABtbTAB/ABtbM34AG09CABtPUAAbWzIxfgAbT1EAG09SABtPUwAbWzE1fgAbWzE3fgAbWzE4fgAbWzE5fgAbWzIwfgAbT0gAG1syfgAbT0QAG1s2fgAbWzV+ABtPQwAbWzE7MkIAG1sxOzJBABtPQQAbWz8xbBs+ABtbPzFoGz0AG1s/MTAzNGwAG1s/MTAzNGgAG1slcDElZFAAG1slcDElZE0AG1slcDElZEIAG1slcDElZEAAG1slcDElZFMAG1slcDElZEwAG1slcDElZEQAG1slcDElZEMAG1slcDElZFQAG1slcDElZEEAG1tpABtbNGkAG1s1aQAlcDElYxtbJXAyJXsxfSUtJWRiABtjG10xMDQHABtbIXAbWz8zOzRsG1s0bBs+ABs4ABtbJWklcDElZGQAGzcACgAbTQAlPyVwOSV0GygwJWUbKEIlOxtbMCU/JXA2JXQ7MSU7JT8lcDUldDsyJTslPyVwMiV0OzQlOyU/JXAxJXAzJXwldDs3JTslPyVwNCV0OzUlOyU/JXA3JXQ7OCU7bQAbSAAJABtPRQBgYGFhZmZnZ2lpampra2xsbW1ubm9vcHBxcXJyc3N0dHV1dnZ3d3h4eXl6ent7fHx9fX5+ABtbWgAbWz83aAAbWz83bAAbT0YAG09NABtbMzsyfgAbWzE7MkYAG1sxOzJIABtbMjsyfgAbWzE7MkQAG1s2OzJ+ABtbNTsyfgAbWzE7MkMAG1syM34AG1syNH4AG1sxOzJQABtb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"))); // /lib/terminfo/x/xterm-256color

    private static IEnumerable<ConsolePal.TerminalFormatStrings> TerminalFormatStrings
    {
        get
        {
            yield return db_xterm_256color;
        }
    }

    private static IEnumerable<Encoding> Encodings
    {
        get
        {
            yield return Encoding.UTF8;
            yield return Encoding.ASCII;
        }
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

            yield return ('\b', ConsoleKey.Backspace);
            yield return ((char)(0x7F), ConsoleKey.Delete);
            yield return ((char)(0x1B), ConsoleKey.Escape);

            for (int i = 0; i <= 9; i++)
            {
                yield return ((char)(48 + i), ConsoleKey.D0 + i);
            }
            for (int i = 0; i <= 'z' - 'a'; i++)
            {
                yield return ((char)(97 + i), ConsoleKey.A + i);
            }
            for (int i = 0; i <= 'Z' - 'A'; i++)
            {
                yield return ((char)(65 + i), ConsoleKey.A + i);
            }
        }
    }

    public static IEnumerable<object[]> AsciiCharactersArguments
    {
        get
        {
            foreach ((char ch, ConsoleKey key) in AsciiKeys)
            {
                yield return new object[] { ch, key };
            }
        }
    }

    [Theory]
    [MemberData(nameof(AsciiCharactersArguments))]
    public void AsciiCharacters(char input, ConsoleKey expectedKey)
    {
        ConsoleKeyInfo consoleKeyInfo = Map(new[] { input }, db_xterm_256color);

        Assert.Equal(input, consoleKeyInfo.KeyChar);
        Assert.Equal(expectedKey, consoleKeyInfo.Key);
        Assert.Equal((ConsoleModifiers)0, consoleKeyInfo.Modifiers);
    }

    private static ConsoleKeyInfo Map(char[] chars, ConsolePal.TerminalFormatStrings terminalFormatStrings)
    {
        int startIndex = 0;

        Assert.True(KeyMapper.MapBufferToConsoleKey(chars, terminalFormatStrings, Byte.MinValue, Byte.MaxValue,
            out ConsoleKey consoleKey, out char ch, out bool isShift, out bool isAlt, out bool isCtrl, ref startIndex, chars.Length));
        Assert.True(startIndex > 0);

        return new ConsoleKeyInfo(ch, consoleKey, isShift, isAlt, isCtrl);
    }
}
