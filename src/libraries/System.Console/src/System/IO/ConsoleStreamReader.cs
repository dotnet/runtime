// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace System.IO;

/// <summary>
/// A <see cref="StreamReader"/> over the input of a console, for which a lone '\r' ends a line.
/// </summary>
/// <remarks>
/// The console host ends a line with "\r\n" only while ENABLE_PROCESSED_INPUT is set. With that flag
/// cleared, which is what setting <see cref="Console.TreatControlCAsInput"/> does, Enter arrives as a
/// bare '\r', and <see cref="StreamReader.ReadLine"/> having found the '\r' reads ahead for a '\n' that
/// is never sent. On a console that read waits for the user to enter another line, so each line is
/// returned one Enter late. This reader ends the line at the '\r' and consumes the '\n' that pairs with
/// it, if one does arrive, at the start of the next read instead.
///
/// Only the synchronous members deal with that pending '\n': an instance is reached only through the
/// SyncTextReader wrapped around it, which routes every asynchronous operation to a synchronous
/// member. Reading a character at a time is likewise good enough here, since the input is typed at a
/// keyboard.
/// </remarks>
internal sealed class ConsoleStreamReader : StreamReader
{
    /// <summary>Whether a '\n' pairing with the '\r' that ended the previous line is still to be skipped.</summary>
    private bool _skipLineFeed;

    internal ConsoleStreamReader(Stream stream, Encoding encoding, int bufferSize)
        : base(stream, encoding, detectEncodingFromByteOrderMarks: false, bufferSize, leaveOpen: true)
    {
    }

    public override string? ReadLine()
    {
        SkipPendingLineFeed();

        var line = new ValueStringBuilder(stackalloc char[256]);

        int ch;
        while ((ch = base.Read()) >= 0)
        {
            if (ch == '\n')
            {
                return line.ToString();
            }

            if (ch == '\r')
            {
                // Whether a '\n' follows can only be answered by another read, which on a console
                // waits for the next line, so leave it to the next read to skip it.
                _skipLineFeed = true;
                return line.ToString();
            }

            line.Append((char)ch);
        }

        if (line.Length > 0)
        {
            return line.ToString();
        }

        line.Dispose();
        return null;
    }

    public override int Peek()
    {
        SkipPendingLineFeed();
        return base.Peek();
    }

    public override int Read()
    {
        SkipPendingLineFeed();
        return base.Read();
    }

    // StreamReader sends Read(Span<char>) and both ReadBlock overloads of a derived type through
    // TextReader, which calls this overload, so they need no overrides of their own.
    public override int Read(char[] buffer, int index, int count)
    {
        // Reject invalid arguments before the skip, which can wait for input.
        ArgumentNullException.ThrowIfNull(buffer);

        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (buffer.Length - index < count)
        {
            throw new ArgumentException(SR.Argument_InvalidOffLen);
        }

        if (count > 0)
        {
            SkipPendingLineFeed();
        }

        return base.Read(buffer, index, count);
    }

    public override string ReadToEnd()
    {
        SkipPendingLineFeed();
        return base.ReadToEnd();
    }

    /// <summary>Consumes the '\n' left by the '\r' the previous line ended on, if that is what comes next.</summary>
    /// <remarks>
    /// A read that is not satisfied from the buffer waits for the next line, but so would the read this
    /// one precedes. A zero-length read completes without waiting and so must not come through here.
    /// </remarks>
    private void SkipPendingLineFeed()
    {
        if (_skipLineFeed)
        {
            _skipLineFeed = false;

            if (base.Peek() == '\n')
            {
                base.Read();
            }
        }
    }
}
