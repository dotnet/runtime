// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

#nullable enable

public partial class ConsoleTest
{
    [JSImport("consoleEventCount", "main.js")]
    private static partial int EventCount();

    [JSImport("consoleClearCount", "main.js")]
    private static partial int ClearCount();

    [JSImport("consoleSetTextEncoder", "main.js")]
    private static partial void SetTextEncoder(bool enabled);

    [JSExport]
    internal static void Run()
    {
        int count = EventCount();
        Console.Write("partial");
        Console.Out.Flush();
        Check(EventCount() == count, "Flushing emitted an incomplete line.");
        Console.WriteLine("-line");
        Console.WriteLine();
        Console.Write("carriage\r\0");
        Console.WriteLine("end");

        Console.Write('\ud83d');
        Console.WriteLine("\ude00");
        Console.Write("x\ud800y");
        Console.WriteLine();
        Console.Write('\ud800');
        Console.Out.Flush();
        Console.WriteLine("flush");
        Console.WriteLine("format:{0}", 42);
        TextWriter initialOutput = Console.Out;
        string originalNewLine = initialOutput.NewLine;
        initialOutput.NewLine = "!\n";
        initialOutput.WriteLine("custom");
        initialOutput.NewLine = originalNewLine;
        initialOutput.WriteLine("span-short-head\n\ud83d".AsSpan());
        const string boundaryPrefix = "span-boundary-head\n";
        initialOutput.WriteLine((boundaryPrefix + new string('x', 255 - boundaryPrefix.Length) + "\ud83d").AsSpan());
        Console.WriteLine("\uFEFFstart");
        Console.Write("prefix");
        Console.WriteLine("\uFEFFmiddle");
        Console.Error.WriteLine("\uFEFFerror");
        Expect<ArgumentNullException>(() => initialOutput.WriteAsync(null!, 0, 0));
        Expect<ArgumentOutOfRangeException>(() => initialOutput.WriteAsync(new char[1], -1, 1));
        Expect<ArgumentException>(() => initialOutput.WriteLineAsync(new char[1], 0, 2));

        using (Stream stdout = Console.OpenStandardOutput())
        using (Stream stderr = Console.OpenStandardError())
        {
            Check(stdout.CanWrite && !stdout.CanRead && !stdout.CanSeek, "Invalid output stream capabilities.");
            Expect<NotSupportedException>(() => stdout.ReadByte());
            Expect<ArgumentOutOfRangeException>(() => stdout.Write(new byte[1], -1, 1));
            Expect<ArgumentException>(() => stdout.Write(new byte[1], 0, 2));
            count = EventCount();
            stdout.Write(new byte[] { (byte)'B', 0, 0xc3 });
            stdout.Flush();
            Check(EventCount() == count, "Flushing a byte stream emitted an incomplete line.");
            stdout.Write(new byte[] { 0xa9, (byte)'\n' });
            stderr.Write(new byte[] { 0xf0, 0x9f });
            stderr.Write(new byte[] { 0x98, 0x80, (byte)'\n' });

            for (int i = 0; i < 1025; i++)
            {
                stdout.WriteByte((byte)'f');
            }
            stdout.WriteByte(0);
            stdout.WriteByte(0xe2);
            stdout.WriteByte(0x82);
            stdout.WriteByte(0xac);
            stdout.WriteByte((byte)'\n');

            stdout.WriteByte((byte)'s');
            stdout.Write("tack\n"u8);
            byte[] reentrantBytes = Encoding.UTF8.GetBytes("_gc trigger\npost-gc\n_");
            stdout.Write(reentrantBytes.AsSpan(1, reentrantBytes.Length - 2));

            Console.Write("text-");
            stdout.Write(Encoding.UTF8.GetBytes("bytes"));
            Console.WriteLine("-tail");
            stdout.Write("no-encoder:"u8);
            SetTextEncoder(false);
            try
            {
                Console.WriteLine("\u20ac\ud83d\ude00-end");
            }
            finally
            {
                SetTextEncoder(true);
            }
            stdout.Write(new byte[] { 0xe2, 0x82 });
            Console.WriteLine("tail");
            stdout.Write(new byte[] { 0xf0, 0x9f });
            using Stream secondStdout = Console.OpenStandardOutput();
            secondStdout.Write(new byte[] { 0x98, 0x80, (byte)'\n' });
            stdout.Write(new byte[] { 0xc3, (byte)'(', (byte)'\n' });
            stderr.Write(new byte[] { 0xe0, 0x80, 0x80, (byte)'\n' });
            stdout.Write(new byte[] { 0xf0, 0x9f });
            stdout.WriteByte((byte)'\n');
        }

        Expect<ArgumentOutOfRangeException>(() => Console.OpenStandardOutput(-1));
        Stream disposedStream = Console.OpenStandardOutput();
        disposedStream.Dispose();
        Check(!disposedStream.CanWrite, "A disposed output stream is still writable.");
        disposedStream.Write(ReadOnlySpan<byte>.Empty);
        Expect<ObjectDisposedException>(() => disposedStream.WriteByte(1));
        Expect<ObjectDisposedException>(() => disposedStream.Write(new byte[1].AsSpan()));
        Expect<NotSupportedException>(() => disposedStream.Write(Array.Empty<byte>(), 0, 0));
        Expect<ObjectDisposedException>(() => disposedStream.Flush());

        Console.Error.WriteLine("error");
        Console.WriteLine("reenter");
        Console.Write("byte-reentry\nremaining\n");
        Console.Write("byte-reentry-incomplete\ntail\n");
        const string SinkError = "console sink failed \u4e16\u754c \U0001F600";
        Check(Expect<IOException>(() => Console.WriteLine("throw sink")).InnerException is JSException { Message: var textError } &&
            textError.Contains(SinkError, StringComparison.Ordinal),
            "The text sink error was not retained.");
        using (Stream stdout = Console.OpenStandardOutput())
        {
            byte[] line = Encoding.UTF8.GetBytes("throw sink\n");
            Check(Expect<IOException>(() => stdout.Write(line)).InnerException is JSException { Message: var byteError } &&
                byteError.Contains(SinkError, StringComparison.Ordinal),
                "The byte sink error was not retained.");
        }
        Console.WriteLine("after-throw");

        TextWriter initialWriter = Console.Out;
        Encoding initialEncoding = Console.OutputEncoding;
        Expect<PlatformNotSupportedException>(() => Console.OutputEncoding = Encoding.Unicode);
        Check(ReferenceEquals(initialWriter, Console.Out), "Rejecting an encoding changed the writer.");
        Check(ReferenceEquals(initialEncoding, Console.OutputEncoding), "Rejecting an encoding changed the encoding.");

        Encoding fallbackEncoding = (Encoding)Encoding.UTF8.Clone();
        fallbackEncoding.EncoderFallback = new EncoderReplacementFallback("?");
        Console.OutputEncoding = fallbackEncoding;
        Console.WriteLine("fallback:\ud800X");
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("after-restore");

        using (SafeFileHandle stdout = Console.OpenStandardOutputHandle())
        using (SafeFileHandle stderr = Console.OpenStandardErrorHandle())
        {
            Check(!stdout.IsInvalid && stdout.DangerousGetHandle() == (IntPtr)1, "Invalid stdout handle.");
            Check(!stderr.IsInvalid && stderr.DangerousGetHandle() == (IntPtr)2, "Invalid stderr handle.");
        }
        Console.WriteLine("after-handle");

        int clearCount = ClearCount();
        Console.Clear();
        Check(ClearCount() == clearCount + 1, "Console.Clear did not reach JavaScript.");

        TextWriter writer = Console.Out;
        using StringWriter redirected = new StringWriter();
        Console.SetOut(redirected);
        Console.WriteLine("redirected");
        Check(redirected.ToString() == "redirected" + Environment.NewLine, "Console.SetOut did not redirect output.");
        Console.SetOut(writer);

        TextWriter errorWriter = Console.Error;
        using StringWriter redirectedError = new StringWriter();
        Console.SetError(redirectedError);
        Console.Error.WriteLine("redirected error");
        Check(redirectedError.ToString() == "redirected error" + Environment.NewLine, "Console.SetError did not redirect output.");
        Console.SetError(errorWriter);

        writer.Dispose();
        writer.WriteLine("after-writer-dispose");
    }

    [JSExport]
    internal static async Task RunAsync()
    {
        using Stream stdout = Console.OpenStandardOutput();
        await stdout.WriteAsync(Encoding.UTF8.GetBytes("async\n"));

        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        try
        {
            await stdout.WriteAsync(Encoding.UTF8.GetBytes("not written\n"), cancellation.Token);
            throw new InvalidOperationException("A cancelled write completed.");
        }
        catch (OperationCanceledException)
        {
        }

        char[] prefix = "async writer ".ToCharArray();
        await Console.Out.WriteAsync(prefix, 0, prefix.Length);
        char[] suffix = "line".ToCharArray();
        await Console.Out.WriteLineAsync(suffix, 0, suffix.Length);
        await Console.Error.WriteLineAsync("async writer error");

        TextWriter writer = Console.Out;
        Task pending = writer.WriteAsync("async overlap\n");
        Check(!pending.IsCompleted, "The asynchronous write completed before overlap could be tested.");
        Expect<InvalidOperationException>(() => writer.Write("not written"));
        Expect<InvalidOperationException>(() => writer.Write((string?)null));
        Expect<InvalidOperationException>(() => writer.Flush());
        Expect<InvalidOperationException>(() => writer.WriteAsync("not written"));
        Expect<InvalidOperationException>(() => writer.WriteLineAsync("not written"));
        Expect<InvalidOperationException>(() => writer.FlushAsync());
        Expect<InvalidOperationException>(() => writer.WriteAsync("not written".AsMemory(), cancellation.Token));
        Check(writer.WriteAsync((string?)null).IsCompletedSuccessfully, "A null asynchronous write did not complete.");
        Console.Error.WriteLine("async independent");
        await pending;

        char[] mutable = "async original\n".ToCharArray();
        pending = writer.WriteAsync(mutable, 0, mutable.Length);
        Array.Fill(mutable, 'x');
        await pending;

        pending = writer.FlushAsync();
        Check(!pending.IsCompleted, "The asynchronous flush completed before overlap could be tested.");
        Expect<InvalidOperationException>(() => writer.Write("not written"));
        await pending;

        try
        {
            await writer.WriteAsync("not written".AsMemory(), cancellation.Token);
            throw new InvalidOperationException("A cancelled writer operation completed.");
        }
        catch (OperationCanceledException)
        {
        }

        using CancellationTokenSource writeCancellation = new CancellationTokenSource();
        pending = writer.WriteAsync("async cancelled write\n".AsMemory(), writeCancellation.Token);
        Check(!pending.IsCompleted, "The cancellable write completed before cancellation.");
        writeCancellation.Cancel();
        await CheckCanceled(pending);

        using CancellationTokenSource lineCancellation = new CancellationTokenSource();
        pending = writer.WriteLineAsync("async cancelled line".AsMemory(), lineCancellation.Token);
        Check(!pending.IsCompleted, "The cancellable line write completed before cancellation.");
        lineCancellation.Cancel();
        await CheckCanceled(pending);

        writer.Write("async flush:");
        writer.Write('\ud800');
        using CancellationTokenSource flushCancellation = new CancellationTokenSource();
        pending = writer.FlushAsync(flushCancellation.Token);
        Check(!pending.IsCompleted, "The cancellable flush completed before cancellation.");
        flushCancellation.Cancel();
        await CheckCanceled(pending);
        writer.WriteLine(":after");

        using CancellationTokenSource longCancellation = new CancellationTokenSource();
        pending = writer.WriteAsync(("cancel-first-line\n" + new string('z', 1024) + "\n").AsMemory(), longCancellation.Token);
        longCancellation.Cancel();
        await CheckCanceled(pending);
        writer.WriteLine("after-cancel");
    }

    [JSExport]
    internal static void Reenter() => Console.WriteLine("nested");

    [JSExport]
    internal static void RunInWorker()
    {
        Console.WriteLine("worker output");
        Console.Error.WriteLine("worker error");
    }

    [JSExport]
    internal static void ReenterSurrogate() => Console.Write("\ude00");

    [JSExport]
    internal static void ReenterBytes()
    {
        using Stream stdout = Console.OpenStandardOutput();
        stdout.Write("nested byte:"u8);
    }

    [JSExport]
    internal static void ReenterIncompleteBytes()
    {
        using Stream stdout = Console.OpenStandardOutput();
        stdout.Write(new byte[] { 0xe2, 0x82 });
    }

    [JSExport]
    internal static void Collect() => GC.Collect();

    [JSExport]
    internal static void PrepareExit()
    {
        int count = EventCount();
        Console.Write("pending stdout\ud800");
        Console.Error.Write("pending stderr\ud800");
        Console.Error.Flush();
        Check(EventCount() == count, "An incomplete line was emitted before exit.");
    }

    [JSExport]
    internal static void WriteFromExitListener() => Console.Write("exit listener");

    [JSExport]
    internal static void WriteExitBytes(string text)
    {
        using Stream stdout = Console.OpenStandardOutput();
        stdout.Write(Encoding.UTF8.GetBytes(text));
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static async Task CheckCanceled(Task task)
    {
        try
        {
            await task;
            throw new InvalidOperationException("The asynchronous operation completed without cancellation.");
        }
        catch (OperationCanceledException)
        {
            Check(task.IsCanceled, "The asynchronous operation did not complete as canceled.");
        }
    }

    private static T Expect<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T exception)
        {
            return exception;
        }

        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
