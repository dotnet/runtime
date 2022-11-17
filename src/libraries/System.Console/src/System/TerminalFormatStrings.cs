// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System;

/// <summary>Provides format strings and related information for use with the current terminal.</summary>
internal sealed class TerminalFormatStrings
{
    /// <summary>The format string to use to change the foreground color.</summary>
    public readonly string? Foreground;
    /// <summary>The format string to use to change the background color.</summary>
    public readonly string? Background;
    /// <summary>The format string to use to reset the foreground and background colors.</summary>
    public readonly string? Reset;
    /// <summary>The maximum number of colors supported by the terminal.</summary>
    public readonly int MaxColors;
    /// <summary>The number of columns in a format.</summary>
    public readonly int Columns;
    /// <summary>The number of lines in a format.</summary>
    public readonly int Lines;
    /// <summary>The format string to use to make cursor visible.</summary>
    public readonly string? CursorVisible;
    /// <summary>The format string to use to make cursor invisible</summary>
    public readonly string? CursorInvisible;
    /// <summary>The format string to use to set the window title.</summary>
    public readonly string? Title;
    /// <summary>The format string to use for an audible bell.</summary>
    public readonly string? Bell;
    /// <summary>The format string to use to clear the terminal.</summary>
    public readonly string? Clear;
    /// <summary>The format string to use to set the position of the cursor.</summary>
    public readonly string? CursorAddress;
    /// <summary>The format string to use to move the cursor to the left.</summary>
    public readonly string? CursorLeft;
    /// <summary>The format string to use to clear to the end of line.</summary>
    public readonly string? ClrEol;
    /// <summary>The ANSI-compatible string for the Cursor Position report request.</summary>
    /// <remarks>
    /// This should really be in user string 7 in the terminfo file, but some terminfo databases
    /// are missing it.  As this is defined to be supported by any ANSI-compatible terminal,
    /// we assume it's available; doing so means CursorTop/Left will work even if the terminfo database
    /// doesn't contain it (as appears to be the case with e.g. screen and tmux on Ubuntu), at the risk
    /// of outputting the sequence on some terminal that's not compatible.
    /// </remarks>
    public const string CursorPositionReport = "\x1B[6n";
    /// <summary>
    /// The dictionary of keystring to ConsoleKeyInfo.
    /// Only some members of the ConsoleKeyInfo are used; in particular, the actual char is ignored.
    /// </summary>
    public readonly Dictionary<ReadOnlyMemory<char>, ConsoleKeyInfo> KeyFormatToConsoleKey =
        new Dictionary<ReadOnlyMemory<char>, ConsoleKeyInfo>(new ReadOnlyMemoryContentComparer());

    /// <summary> Max key length </summary>
    public readonly int MaxKeyFormatLength;
    /// <summary> Min key length </summary>
    public readonly int MinKeyFormatLength;
    /// <summary>The ANSI string used to enter "application" / "keypad transmit" mode.</summary>
    public readonly string? KeypadXmit;
    /// <summary>Indicates that it was created out of rxvt TERM</summary>
    public readonly bool IsRxvtTerm;

    public TerminalFormatStrings(TermInfo.Database? db)
    {
        if (db == null)
            return;

        KeypadXmit = db.GetString(TermInfo.WellKnownStrings.KeypadXmit);
        Foreground = db.GetString(TermInfo.WellKnownStrings.SetAnsiForeground);
        Background = db.GetString(TermInfo.WellKnownStrings.SetAnsiBackground);
        Reset = db.GetString(TermInfo.WellKnownStrings.OrigPairs) ?? db.GetString(TermInfo.WellKnownStrings.OrigColors);
        Bell = db.GetString(TermInfo.WellKnownStrings.Bell);
        Clear = db.GetString(TermInfo.WellKnownStrings.Clear);
        Columns = db.GetNumber(TermInfo.WellKnownNumbers.Columns);
        Lines = db.GetNumber(TermInfo.WellKnownNumbers.Lines);
        CursorVisible = db.GetString(TermInfo.WellKnownStrings.CursorVisible);
        CursorInvisible = db.GetString(TermInfo.WellKnownStrings.CursorInvisible);
        CursorAddress = db.GetString(TermInfo.WellKnownStrings.CursorAddress);
        CursorLeft = db.GetString(TermInfo.WellKnownStrings.CursorLeft);
        ClrEol = db.GetString(TermInfo.WellKnownStrings.ClrEol);

        IsRxvtTerm = !string.IsNullOrEmpty(db.Term) && db.Term.Contains("rxvt", StringComparison.OrdinalIgnoreCase);
        Title = GetTitle(db);

        Debug.WriteLineIf(db.GetString(TermInfo.WellKnownStrings.CursorPositionReport) != CursorPositionReport,
            "Getting the cursor position will only work if the terminal supports the CPR sequence," +
            "but the terminfo database does not contain an entry for it.");

        int maxColors = db.GetNumber(TermInfo.WellKnownNumbers.MaxColors);
        MaxColors = // normalize to either the full range of all ANSI colors, just the dark ones, or none
            maxColors >= 16 ? 16 :
            maxColors >= 8 ? 8 :
            0;

        AddKey(db, TermInfo.WellKnownStrings.KeyF1, ConsoleKey.F1);
        AddKey(db, TermInfo.WellKnownStrings.KeyF2, ConsoleKey.F2);
        AddKey(db, TermInfo.WellKnownStrings.KeyF3, ConsoleKey.F3);
        AddKey(db, TermInfo.WellKnownStrings.KeyF4, ConsoleKey.F4);
        AddKey(db, TermInfo.WellKnownStrings.KeyF5, ConsoleKey.F5);
        AddKey(db, TermInfo.WellKnownStrings.KeyF6, ConsoleKey.F6);
        AddKey(db, TermInfo.WellKnownStrings.KeyF7, ConsoleKey.F7);
        AddKey(db, TermInfo.WellKnownStrings.KeyF8, ConsoleKey.F8);
        AddKey(db, TermInfo.WellKnownStrings.KeyF9, ConsoleKey.F9);
        AddKey(db, TermInfo.WellKnownStrings.KeyF10, ConsoleKey.F10);
        AddKey(db, TermInfo.WellKnownStrings.KeyF11, ConsoleKey.F11);
        AddKey(db, TermInfo.WellKnownStrings.KeyF12, ConsoleKey.F12);
        AddKey(db, TermInfo.WellKnownStrings.KeyF13, ConsoleKey.F13);
        AddKey(db, TermInfo.WellKnownStrings.KeyF14, ConsoleKey.F14);
        AddKey(db, TermInfo.WellKnownStrings.KeyF15, ConsoleKey.F15);
        AddKey(db, TermInfo.WellKnownStrings.KeyF16, ConsoleKey.F16);
        AddKey(db, TermInfo.WellKnownStrings.KeyF17, ConsoleKey.F17);
        AddKey(db, TermInfo.WellKnownStrings.KeyF18, ConsoleKey.F18);
        AddKey(db, TermInfo.WellKnownStrings.KeyF19, ConsoleKey.F19);
        AddKey(db, TermInfo.WellKnownStrings.KeyF20, ConsoleKey.F20);
        AddKey(db, TermInfo.WellKnownStrings.KeyF21, ConsoleKey.F21);
        AddKey(db, TermInfo.WellKnownStrings.KeyF22, ConsoleKey.F22);
        AddKey(db, TermInfo.WellKnownStrings.KeyF23, ConsoleKey.F23);
        AddKey(db, TermInfo.WellKnownStrings.KeyF24, ConsoleKey.F24);
        AddKey(db, TermInfo.WellKnownStrings.KeyBackspace, ConsoleKey.Backspace);
        AddKey(db, TermInfo.WellKnownStrings.KeyBackTab, ConsoleKey.Tab, shift: true, alt: false, control: false);
        AddKey(db, TermInfo.WellKnownStrings.KeyBegin, ConsoleKey.Home);
        AddKey(db, TermInfo.WellKnownStrings.KeyClear, ConsoleKey.Clear);
        AddKey(db, TermInfo.WellKnownStrings.KeyDelete, ConsoleKey.Delete);
        AddKey(db, TermInfo.WellKnownStrings.KeyDown, ConsoleKey.DownArrow);
        AddKey(db, TermInfo.WellKnownStrings.KeyEnd, ConsoleKey.End);
        AddKey(db, TermInfo.WellKnownStrings.KeyEnter, ConsoleKey.Enter);
        AddKey(db, TermInfo.WellKnownStrings.KeyHelp, ConsoleKey.Help);
        AddKey(db, TermInfo.WellKnownStrings.KeyHome, ConsoleKey.Home);
        AddKey(db, TermInfo.WellKnownStrings.KeyInsert, ConsoleKey.Insert);
        AddKey(db, TermInfo.WellKnownStrings.KeyLeft, ConsoleKey.LeftArrow);
        AddKey(db, TermInfo.WellKnownStrings.KeyPageDown, ConsoleKey.PageDown);
        AddKey(db, TermInfo.WellKnownStrings.KeyPageUp, ConsoleKey.PageUp);
        AddKey(db, TermInfo.WellKnownStrings.KeyPrint, ConsoleKey.Print);
        AddKey(db, TermInfo.WellKnownStrings.KeyRight, ConsoleKey.RightArrow);
        AddKey(db, TermInfo.WellKnownStrings.KeyScrollForward, ConsoleKey.PageDown, shift: true, alt: false, control: false);
        AddKey(db, TermInfo.WellKnownStrings.KeyScrollReverse, ConsoleKey.PageUp, shift: true, alt: false, control: false);
        AddKey(db, TermInfo.WellKnownStrings.KeySBegin, ConsoleKey.Home, shift: true, alt: false, control: false);
        AddKey(db, TermInfo.WellKnownStrings.KeySDelete, ConsoleKey.Delete, shift: true, alt: false, control: false);
        AddKey(db, TermInfo.WellKnownStrings.KeySHome, ConsoleKey.Home, shift: true, alt: false, control: false);
        AddKey(db, TermInfo.WellKnownStrings.KeySelect, ConsoleKey.Select);
        AddKey(db, TermInfo.WellKnownStrings.KeySLeft, ConsoleKey.LeftArrow, shift: true, alt: false, control: false);
        AddKey(db, TermInfo.WellKnownStrings.KeySPrint, ConsoleKey.Print, shift: true, alt: false, control: false);
        AddKey(db, TermInfo.WellKnownStrings.KeySRight, ConsoleKey.RightArrow, shift: true, alt: false, control: false);
        AddKey(db, TermInfo.WellKnownStrings.KeyUp, ConsoleKey.UpArrow);
        AddPrefixKey(db, "kLFT", ConsoleKey.LeftArrow);
        AddPrefixKey(db, "kRIT", ConsoleKey.RightArrow);
        AddPrefixKey(db, "kUP", ConsoleKey.UpArrow);
        AddPrefixKey(db, "kDN", ConsoleKey.DownArrow);
        AddPrefixKey(db, "kDC", ConsoleKey.Delete);
        AddPrefixKey(db, "kEND", ConsoleKey.End);
        AddPrefixKey(db, "kHOM", ConsoleKey.Home);
        AddPrefixKey(db, "kNXT", ConsoleKey.PageDown);
        AddPrefixKey(db, "kPRV", ConsoleKey.PageUp);

        if (KeyFormatToConsoleKey.Count > 0)
        {
            MaxKeyFormatLength = int.MinValue;
            MinKeyFormatLength = int.MaxValue;

            foreach (KeyValuePair<ReadOnlyMemory<char>, ConsoleKeyInfo> entry in KeyFormatToConsoleKey)
            {
                if (entry.Key.Length > MaxKeyFormatLength)
                {
                    MaxKeyFormatLength = entry.Key.Length;
                }
                if (entry.Key.Length < MinKeyFormatLength)
                {
                    MinKeyFormatLength = entry.Key.Length;
                }
            }
        }
    }

    private static string GetTitle(TermInfo.Database db)
    {
        // Try to get the format string from tsl/fsl and use it if they're available
        string? tsl = db.GetString(TermInfo.WellKnownStrings.ToStatusLine);
        string? fsl = db.GetString(TermInfo.WellKnownStrings.FromStatusLine);
        if (tsl != null && fsl != null)
        {
            return tsl + "%p1%s" + fsl;
        }

        string term = db.Term;
        if (term == null)
        {
            return string.Empty;
        }

        if (term.StartsWith("xterm", StringComparison.Ordinal)) // normalize all xterms to enable easier matching
        {
            term = "xterm";
        }
        else if (term.StartsWith("screen", StringComparison.Ordinal)) // normalize all tmux configs
        {
            term = "screen";
        }

        switch (term)
        {
            case "aixterm":
            case "dtterm":
            case "linux":
            case "rxvt":
            case "xterm":
                return "\x1B]0;%p1%s\x07";
            case "cygwin":
                return "\x1B];%p1%s\x07";
            case "konsole":
                return "\x1B]30;%p1%s\x07";
            case "screen":
                return "\x1Bk%p1%s\x1B";
            default:
                return string.Empty;
        }
    }

    private void AddKey(TermInfo.Database db, TermInfo.WellKnownStrings keyId, ConsoleKey key)
    {
        AddKey(db, keyId, key, shift: false, alt: false, control: false);
    }

    private void AddKey(TermInfo.Database db, TermInfo.WellKnownStrings keyId, ConsoleKey key, bool shift, bool alt, bool control)
    {
        ReadOnlyMemory<char> keyFormat = db.GetString(keyId).AsMemory();
        if (!keyFormat.IsEmpty)
            KeyFormatToConsoleKey[keyFormat] = new ConsoleKeyInfo(key == ConsoleKey.Enter ? '\r' : '\0', key, shift, alt, control);
    }

    private void AddPrefixKey(TermInfo.Database db, string extendedNamePrefix, ConsoleKey key)
    {
        if (db.HasExtendedStrings) // avoid string concatenation in case when there are no Extended Strings (typical scenario)
        {
            AddKey(db, extendedNamePrefix + "3", key, shift: false, alt: true,  control: false);
            AddKey(db, extendedNamePrefix + "4", key, shift: true,  alt: true,  control: false);
            AddKey(db, extendedNamePrefix + "5", key, shift: false, alt: false, control: true);
            AddKey(db, extendedNamePrefix + "6", key, shift: true,  alt: false, control: true);
            AddKey(db, extendedNamePrefix + "7", key, shift: false, alt: false, control: true);
        }
    }

    private void AddKey(TermInfo.Database db, string extendedName, ConsoleKey key, bool shift, bool alt, bool control)
    {
        ReadOnlyMemory<char> keyFormat = db.GetExtendedString(extendedName).AsMemory();
        if (!keyFormat.IsEmpty)
            KeyFormatToConsoleKey[keyFormat] = new ConsoleKeyInfo('\0', key, shift, alt, control);
    }

    private sealed class ReadOnlyMemoryContentComparer : IEqualityComparer<ReadOnlyMemory<char>>
    {
        public bool Equals(ReadOnlyMemory<char> x, ReadOnlyMemory<char> y) =>
            x.Span.SequenceEqual(y.Span);

        public int GetHashCode(ReadOnlyMemory<char> obj) =>
            string.GetHashCode(obj.Span);
    }
}
