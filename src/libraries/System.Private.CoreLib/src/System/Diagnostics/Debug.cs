// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Do not remove this, it is needed to retain calls to these conditional methods in release builds
#define DEBUG

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace System.Diagnostics
{
    /// <summary>
    /// Provides a set of properties and methods for debugging code.
    /// </summary>
    public static partial class Debug
    {
        private static volatile DebugProvider s_provider = new DebugProvider();

        public static DebugProvider SetProvider(DebugProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            return Interlocked.Exchange(ref s_provider, provider);
        }

        public static bool AutoFlush
        {
            get => true;
            set { }
        }

        [ThreadStatic]
        private static int t_indentLevel;
        public static int IndentLevel
        {
            get => t_indentLevel;
            set
            {
                t_indentLevel = value < 0 ? 0 : value;
                s_provider.OnIndentLevelChanged(t_indentLevel);
            }
        }

        private static volatile int s_indentSize = 4;
        public static int IndentSize
        {
            get => s_indentSize;
            set
            {
                s_indentSize = value < 0 ? 0 : value;
                s_provider.OnIndentSizeChanged(s_indentSize);
            }
        }

        [Conditional("DEBUG")]
        public static void Close() { }

        [Conditional("DEBUG")]
        public static void Flush() { }

        [Conditional("DEBUG")]
        public static void Indent() =>
            IndentLevel++;

        [Conditional("DEBUG")]
        public static void Unindent() =>
            IndentLevel--;

        [Conditional("DEBUG")]
        public static void Print(string? message) =>
            WriteLine(message);

        [Conditional("DEBUG")]
        public static void Print(string format, params object?[] args) =>
            WriteLine(string.Format(null, format, args));

        [Conditional("DEBUG")]
        public static void Assert([DoesNotReturnIf(false)] bool condition) =>
            Assert(condition, string.Empty, string.Empty);

        [Conditional("DEBUG")]
        public static void Assert([DoesNotReturnIf(false)] bool condition, string? message) =>
            Assert(condition, message, string.Empty);

        [Conditional("DEBUG")]
        public static void Assert([DoesNotReturnIf(false)] bool condition, [InterpolatedStringHandlerArgument("condition")] ref AssertInterpolatedStringHandler message) =>
            Assert(condition, message.ToStringAndClear());

        [Conditional("DEBUG")]
        public static void Assert([DoesNotReturnIf(false)] bool condition, string? message, string? detailMessage)
        {
            if (!condition)
            {
                Fail(message, detailMessage);
            }
        }

        [Conditional("DEBUG")]
        public static void Assert([DoesNotReturnIf(false)] bool condition, [InterpolatedStringHandlerArgument("condition")] ref AssertInterpolatedStringHandler message, [InterpolatedStringHandlerArgument("condition")] ref AssertInterpolatedStringHandler detailMessage) =>
            Assert(condition, message.ToStringAndClear(), detailMessage.ToStringAndClear());

        [Conditional("DEBUG")]
        public static void Assert([DoesNotReturnIf(false)] bool condition, string? message, string detailMessageFormat, params object?[] args) =>
            Assert(condition, message, string.Format(detailMessageFormat, args));

        internal static void ContractFailure(string message, string detailMessage, string failureKindMessage)
        {
            string stackTrace;
            try
            {
                stackTrace = new StackTrace(2, true).ToString(StackTrace.TraceFormat.Normal);
            }
            catch
            {
                stackTrace = "";
            }
            s_provider.WriteAssert(stackTrace, message, detailMessage);
            DebugProvider.FailCore(stackTrace, message, detailMessage, failureKindMessage);
        }

        [Conditional("DEBUG")]
        [DoesNotReturn]
        public static void Fail(string? message) =>
            Fail(message, string.Empty);

        [Conditional("DEBUG")]
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)] // Preserve the frame for debugger
        public static void Fail(string? message, string? detailMessage) =>
            s_provider.Fail(message, detailMessage);

        [Conditional("DEBUG")]
        public static void WriteLine(string? message) =>
            s_provider.WriteLine(message);

        [Conditional("DEBUG")]
        public static void Write(string? message) =>
            s_provider.Write(message);

        [Conditional("DEBUG")]
        public static void WriteLine(object? value) =>
            WriteLine(value?.ToString());

        [Conditional("DEBUG")]
        public static void WriteLine(object? value, string? category) =>
            WriteLine(value?.ToString(), category);

        [Conditional("DEBUG")]
        public static void WriteLine(string format, params object?[] args) =>
            WriteLine(string.Format(null, format, args));

        [Conditional("DEBUG")]
        public static void WriteLine(string? message, string? category)
        {
            if (category == null)
            {
                WriteLine(message);
            }
            else
            {
                WriteLine(category + ": " + message);
            }
        }

        [Conditional("DEBUG")]
        public static void Write(object? value) =>
            Write(value?.ToString());

        [Conditional("DEBUG")]
        public static void Write(string? message, string? category)
        {
            if (category == null)
            {
                Write(message);
            }
            else
            {
                Write(category + ": " + message);
            }
        }

        [Conditional("DEBUG")]
        public static void Write(object? value, string? category) =>
            Write(value?.ToString(), category);

        [Conditional("DEBUG")]
        public static void WriteIf(bool condition, string? message)
        {
            if (condition)
            {
                Write(message);
            }
        }

        [Conditional("DEBUG")]
        public static void WriteIf(bool condition, [InterpolatedStringHandlerArgument("condition")] ref WriteIfInterpolatedStringHandler message) =>
            WriteIf(condition, message.ToStringAndClear());

        [Conditional("DEBUG")]
        public static void WriteIf(bool condition, object? value)
        {
            if (condition)
            {
                Write(value);
            }
        }

        [Conditional("DEBUG")]
        public static void WriteIf(bool condition, string? message, string? category)
        {
            if (condition)
            {
                Write(message, category);
            }
        }

        [Conditional("DEBUG")]
        public static void WriteIf(bool condition, [InterpolatedStringHandlerArgument("condition")] ref WriteIfInterpolatedStringHandler message, string? category) =>
            WriteIf(condition, message.ToStringAndClear(), category);

        [Conditional("DEBUG")]
        public static void WriteIf(bool condition, object? value, string? category)
        {
            if (condition)
            {
                Write(value, category);
            }
        }

        [Conditional("DEBUG")]
        public static void WriteLineIf(bool condition, object? value)
        {
            if (condition)
            {
                WriteLine(value);
            }
        }

        [Conditional("DEBUG")]
        public static void WriteLineIf(bool condition, object? value, string? category)
        {
            if (condition)
            {
                WriteLine(value, category);
            }
        }

        [Conditional("DEBUG")]
        public static void WriteLineIf(bool condition, string? message)
        {
            if (condition)
            {
                WriteLine(message);
            }
        }

        [Conditional("DEBUG")]
        public static void WriteLineIf(bool condition, [InterpolatedStringHandlerArgument("condition")] ref WriteIfInterpolatedStringHandler message) =>
            WriteLineIf(condition, message.ToStringAndClear());

        [Conditional("DEBUG")]
        public static void WriteLineIf(bool condition, string? message, string? category)
        {
            if (condition)
            {
                WriteLine(message, category);
            }
        }

        [Conditional("DEBUG")]
        public static void WriteLineIf(bool condition, [InterpolatedStringHandlerArgument("condition")] ref WriteIfInterpolatedStringHandler message, string? category) =>
            WriteLineIf(condition, message.ToStringAndClear(), category);

        /// <summary>Provides an interpolated string handler for <see cref="Debug.Assert"/> that only performs formatting if the assert fails.</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [InterpolatedStringHandler]
        public struct AssertInterpolatedStringHandler
        {
            /// <summary>The handler we use to perform the formatting.</summary>
            private StringBuilder.AppendInterpolatedStringHandler _stringBuilderHandler;

            /// <summary>Creates an instance of the handler..</summary>
            /// <param name="literalLength">The number of constant characters outside of interpolation expressions in the interpolated string.</param>
            /// <param name="formattedCount">The number of interpolation expressions in the interpolated string.</param>
            /// <param name="condition">The condition Boolean passed to the <see cref="Debug"/> method.</param>
            /// <param name="shouldAppend">A value indicating whether formatting should proceed.</param>
            /// <remarks>This is intended to be called only by compiler-generated code. Arguments are not validated as they'd otherwise be for members intended to be used directly.</remarks>
            public AssertInterpolatedStringHandler(int literalLength, int formattedCount, bool condition, out bool shouldAppend)
            {
                if (condition)
                {
                    _stringBuilderHandler = default;
                    shouldAppend = false;
                }
                else
                {
                    // Only used when failing an assert.  Additional allocation here doesn't matter; just create a new StringBuilder.
                    _stringBuilderHandler = new StringBuilder.AppendInterpolatedStringHandler(literalLength, formattedCount, new StringBuilder());
                    shouldAppend = true;
                }
            }

            /// <summary>Extracts the built string from the handler.</summary>
            internal string ToStringAndClear()
            {
                string s = _stringBuilderHandler._stringBuilder is StringBuilder sb ?
                    sb.ToString() :
                    string.Empty;
                _stringBuilderHandler = default;
                return s;
            }

            /// <summary>Writes the specified string to the handler.</summary>
            /// <param name="value">The string to write.</param>
            public void AppendLiteral(string value) => _stringBuilderHandler.AppendLiteral(value);

            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            /// <typeparam name="T">The type of the value to write.</typeparam>
            public void AppendFormatted<T>(T value) => _stringBuilderHandler.AppendFormatted(value);

            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            /// <param name="format">The format string.</param>
            /// <typeparam name="T">The type of the value to write.</typeparam>
            public void AppendFormatted<T>(T value, string? format) => _stringBuilderHandler.AppendFormatted(value, format);

            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
            /// <typeparam name="T">The type of the value to write.</typeparam>
            public void AppendFormatted<T>(T value, int alignment) => _stringBuilderHandler.AppendFormatted(value, alignment);

            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            /// <param name="format">The format string.</param>
            /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
            /// <typeparam name="T">The type of the value to write.</typeparam>
            public void AppendFormatted<T>(T value, int alignment, string? format) => _stringBuilderHandler.AppendFormatted(value, alignment, format);

            /// <summary>Writes the specified character span to the handler.</summary>
            /// <param name="value">The span to write.</param>
            public void AppendFormatted(ReadOnlySpan<char> value) => _stringBuilderHandler.AppendFormatted(value);

            /// <summary>Writes the specified string of chars to the handler.</summary>
            /// <param name="value">The span to write.</param>
            /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
            /// <param name="format">The format string.</param>
            public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? format = null) => _stringBuilderHandler.AppendFormatted(value, alignment, format);

            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            public void AppendFormatted(string? value) => _stringBuilderHandler.AppendFormatted(value);

            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
            /// <param name="format">The format string.</param>
            public void AppendFormatted(string? value, int alignment = 0, string? format = null) => _stringBuilderHandler.AppendFormatted(value, alignment, format);

            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
            /// <param name="format">The format string.</param>
            public void AppendFormatted(object? value, int alignment = 0, string? format = null) => _stringBuilderHandler.AppendFormatted(value, alignment, format);
        }

        /// <summary>Provides an interpolated string handler for <see cref="Debug.WriteIf"/> and <see cref="Debug.WriteLineIf"/> that only performs formatting if the condition applies.</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [InterpolatedStringHandler]
        public struct WriteIfInterpolatedStringHandler
        {
            /// <summary>The handler we use to perform the formatting.</summary>
            private StringBuilder.AppendInterpolatedStringHandler _stringBuilderHandler;

            /// <summary>Creates an instance of the handler..</summary>
            /// <param name="literalLength">The number of constant characters outside of interpolation expressions in the interpolated string.</param>
            /// <param name="formattedCount">The number of interpolation expressions in the interpolated string.</param>
            /// <param name="condition">The condition Boolean passed to the <see cref="Debug"/> method.</param>
            /// <param name="shouldAppend">A value indicating whether formatting should proceed.</param>
            /// <remarks>This is intended to be called only by compiler-generated code. Arguments are not validated as they'd otherwise be for members intended to be used directly.</remarks>
            public WriteIfInterpolatedStringHandler(int literalLength, int formattedCount, bool condition, out bool shouldAppend)
            {
                if (condition)
                {
                    // Only used in debug, but could be used on non-failure code paths, so use a cached builder.
                    _stringBuilderHandler = new StringBuilder.AppendInterpolatedStringHandler(literalLength, formattedCount,
                        StringBuilderCache.Acquire(DefaultInterpolatedStringHandler.GetDefaultLength(literalLength, formattedCount)));
                    shouldAppend = true;
                }
                else
                {
                    _stringBuilderHandler = default;
                    shouldAppend = false;
                }
            }

            /// <summary>Extracts the built string from the handler.</summary>
            internal string ToStringAndClear()
            {
                string s = _stringBuilderHandler._stringBuilder is StringBuilder sb ?
                    StringBuilderCache.GetStringAndRelease(sb) :
                    string.Empty;
                _stringBuilderHandler = default;
                return s;
            }

            /// <summary>Writes the specified string to the handler.</summary>
            /// <param name="value">The string to write.</param>
            public void AppendLiteral(string value) => _stringBuilderHandler.AppendLiteral(value);

            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            /// <typeparam name="T">The type of the value to write.</typeparam>
            public void AppendFormatted<T>(T value) => _stringBuilderHandler.AppendFormatted(value);

            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            /// <param name="format">The format string.</param>
            /// <typeparam name="T">The type of the value to write.</typeparam>
            public void AppendFormatted<T>(T value, string? format) => _stringBuilderHandler.AppendFormatted(value, format);

            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
            /// <typeparam name="T">The type of the value to write.</typeparam>
            public void AppendFormatted<T>(T value, int alignment) => _stringBuilderHandler.AppendFormatted(value, alignment);

            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            /// <param name="format">The format string.</param>
            /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
            /// <typeparam name="T">The type of the value to write.</typeparam>
            public void AppendFormatted<T>(T value, int alignment, string? format) => _stringBuilderHandler.AppendFormatted(value, alignment, format);

            /// <summary>Writes the specified character span to the handler.</summary>
            /// <param name="value">The span to write.</param>
            public void AppendFormatted(ReadOnlySpan<char> value) => _stringBuilderHandler.AppendFormatted(value);

            /// <summary>Writes the specified string of chars to the handler.</summary>
            /// <param name="value">The span to write.</param>
            /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
            /// <param name="format">The format string.</param>
            public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? format = null) => _stringBuilderHandler.AppendFormatted(value, alignment, format);

            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            public void AppendFormatted(string? value) => _stringBuilderHandler.AppendFormatted(value);

            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
            /// <param name="format">The format string.</param>
            public void AppendFormatted(string? value, int alignment = 0, string? format = null) => _stringBuilderHandler.AppendFormatted(value, alignment, format);

            /// <summary>Writes the specified value to the handler.</summary>
            /// <param name="value">The value to write.</param>
            /// <param name="alignment">Minimum number of characters that should be written for this value.  If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
            /// <param name="format">The format string.</param>
            public void AppendFormatted(object? value, int alignment = 0, string? format = null) => _stringBuilderHandler.AppendFormatted(value, alignment, format);
        }
    }
}
