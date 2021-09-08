// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace System
{
    internal sealed class NSLogStream : ConsoleStream
    {
        private readonly StringBuilder _buffer = new StringBuilder();
        private readonly Encoding _encoding;
        private readonly Decoder _decoder;

        public NSLogStream(Encoding encoding) : base(FileAccess.Write)
        {
            _encoding = encoding;
            _decoder = _encoding.GetDecoder();
        }

        public override int Read(Span<byte> buffer) => throw Error.GetReadNotSupported();

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            int maxCharCount = _encoding.GetMaxCharCount(buffer.Length);
            char[]? pooledBuffer = null;
            Span<char> charSpan = maxCharCount <= 512 ? stackalloc char[512] : (pooledBuffer = ArrayPool<char>.Shared.Rent(maxCharCount));
            try
            {
                int count = _decoder.GetChars(buffer, charSpan, false);
                if (count > 0)
                {
                    WriteOrCache(_buffer, charSpan.Slice(0, count));
                }
            }
            finally
            {
                if (pooledBuffer != null)
                {
                    ArrayPool<char>.Shared.Return(pooledBuffer);
                }
            }
        }

        private static void WriteOrCache(StringBuilder cache, Span<char> charBuffer)
        {
            int lastNewLine = charBuffer.LastIndexOf('\n');
            if (lastNewLine != -1)
            {
                Span<char> lineSpan = charBuffer.Slice(0, lastNewLine);
                if (cache.Length > 0)
                {
                    Print(cache.Append(lineSpan).ToString());
                    cache.Clear();
                }
                else
                {
                    Print(lineSpan);
                }

                if (lastNewLine + 1 < charBuffer.Length)
                {
                    cache.Append(charBuffer.Slice(lastNewLine + 1));
                }

                return;
            }

            // no newlines found, add the entire buffer to the cache
            cache.Append(charBuffer);

            static unsafe void Print(ReadOnlySpan<char> line)
            {
                fixed (char* ptr = line)
                {
                    Interop.Sys.Log((byte*)ptr, line.Length * 2);
                }
            }
        }
    }

    internal static class ConsolePal
    {
        internal static void EnsureConsoleInitialized()
        { }

        public static Stream OpenStandardInput() => throw new PlatformNotSupportedException();

        public static Stream OpenStandardOutput() => new NSLogStream(OutputEncoding);

        public static Stream OpenStandardError() => new NSLogStream(OutputEncoding);

        public static Encoding InputEncoding => throw new PlatformNotSupportedException();

        public static void SetConsoleInputEncoding(Encoding enc) => throw new PlatformNotSupportedException();

        public static Encoding OutputEncoding => Encoding.Unicode;

        // underlying API expects only utf-16
        public static void SetConsoleOutputEncoding(Encoding enc) => throw new PlatformNotSupportedException();

        public static bool IsInputRedirectedCore() => false;

        public static bool IsOutputRedirectedCore() => false;

        public static bool IsErrorRedirectedCore() => false;

        internal static TextReader GetOrCreateReader() => throw new PlatformNotSupportedException();

        public static bool NumberLock => throw new PlatformNotSupportedException();

        public static bool CapsLock => throw new PlatformNotSupportedException();

        public static bool KeyAvailable => false;

        public static ConsoleKeyInfo ReadKey(bool intercept) => throw new PlatformNotSupportedException();

        public static bool TreatControlCAsInput
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public static ConsoleColor BackgroundColor
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public static ConsoleColor ForegroundColor
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public static void ResetColor() => throw new PlatformNotSupportedException();

        public static int CursorSize
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public static bool CursorVisible
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public static (int Left, int Top) GetCursorPosition() => throw new PlatformNotSupportedException();

        public static string Title
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public static void Beep() => throw new PlatformNotSupportedException();

        public static void Beep(int frequency, int duration) => throw new PlatformNotSupportedException();

        public static void MoveBufferArea(int sourceLeft, int sourceTop,
            int sourceWidth, int sourceHeight, int targetLeft, int targetTop,
            char sourceChar, ConsoleColor sourceForeColor,
            ConsoleColor sourceBackColor) => throw new PlatformNotSupportedException();

        public static void Clear() => throw new PlatformNotSupportedException();

        public static void SetCursorPosition(int left, int top) => throw new PlatformNotSupportedException();

        public static int BufferWidth
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public static int BufferHeight
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public static void SetBufferSize(int width, int height) => throw new PlatformNotSupportedException();

        public static int LargestWindowWidth => throw new PlatformNotSupportedException();

        public static int LargestWindowHeight => throw new PlatformNotSupportedException();

        public static int WindowLeft
        {
            get => 0;
            set => throw new PlatformNotSupportedException();
        }

        public static int WindowTop
        {
            get => 0;
            set => throw new PlatformNotSupportedException();
        }

        public static int WindowWidth
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public static int WindowHeight
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public static void SetWindowPosition(int left, int top) => throw new PlatformNotSupportedException();

        public static void SetWindowSize(int width, int height) => throw new PlatformNotSupportedException();
    }
}
