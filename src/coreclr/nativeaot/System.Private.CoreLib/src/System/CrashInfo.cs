// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text;

using Internal.DeveloperExperience;

namespace System
{
    internal unsafe struct CrashInfo
    {
        /// <summary>
        /// The kind or reason of crash for the triage JSON
        /// </summary>
        public enum CrashReason
        {
            Unknown = 0,
            UnhandledException = 1,
            EnvironmentFailFast = 2,
            InternalFailFast = 3,
        }

        /// <summary>
        /// Type of runtime (currently only NativeAOT is used)
        /// </summary>
        public enum RuntimeType
        {
            Unknown = 0,
            Desktop = 1,
            NetCore = 2,
            SingleFile = 3,
            NativeAOT = 4,
        }

        private int _currentBufferIndex;
        private int _reservedBuffer;
        private bool _isCommaNeeded;
        private readonly int _maxBufferSize;
        private readonly byte* _bufferAddress;

        public CrashInfo()
        {
            _currentBufferIndex = 0;
            _reservedBuffer = 0;
            _isCommaNeeded = false;
            _bufferAddress = RuntimeImports.RhGetCrashInfoBuffer(out _maxBufferSize);
        }

        public IntPtr TriageBufferAddress => new IntPtr(_bufferAddress);

        public int TriageBufferSize => _currentBufferIndex;

        /// <summary>
        /// Writes the opening bracket and header to triage buffer
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="crashingThreadId">the thread id of this crashing thread</param>
        /// <param name="message"></param>
        public void Open(RhFailFastReason reason, ulong crashingThreadId, string message)
        {
            // Write the opening bracket and basic header which should never fail
            bool success = OpenValue(default, '{');
            Debug.Assert(success);
            success = WriteHeader(reason, crashingThreadId, message);
            Debug.Assert(success);
        }

        /// <summary>
        /// Write the closing bracket. The triage buffer is ready.
        /// </summary>
        public void Close() => CloseValue('}');

        /// <summary>
        /// Write the exception information with fallbacks if the info doesn't fit in the fixed size triage buffer.
        /// </summary>
        /// <param name="exception">exception object</param>
        public void WriteException(Exception exception)
        {
            ReadOnlySpan<byte> key = "exception"u8;
            if (!WriteExceptionWithFallback(key, exception, int.MaxValue, 500, int.MaxValue))
            {
                // If the buffer isn't big enough to fit 500 stack frames, try limiting to 10
                if (!WriteExceptionWithFallback(key, exception, int.MaxValue, 10, int.MaxValue))
                {
                    // If that fails, try limiting the size of the stack frame method names to 100 chars
                    WriteExceptionWithFallback(key, exception, int.MaxValue, 10, 100);
                }
            }
        }

        /// <summary>
        /// Writes the basic triage information header. Assumes there is always enough
        /// room in the buffer for this header.
        /// </summary>
        /// <param name="reason">kind of crash</param>
        /// <param name="crashingThreadId">the thread id of this crashing thread</param>
        /// <param name="message">fail fast message, limited to 1024 chars</param>
        /// <returns>true - success, false - out of triage buffer space</returns>
        private bool WriteHeader(RhFailFastReason reason, ulong crashingThreadId, string message)
        {
            if (!WriteValue("version"u8, "1.0.0"u8))
                return false;

            static void Dummy() { }

            if (!WriteHexValue("runtime_base"u8, (ulong)RuntimeImports.RhGetOSModuleFromPointer((nint)(void*)(delegate*<void>)&Dummy)))
                return false;

            if (!WriteIntValue("runtime_type"u8, (int)RuntimeType.NativeAOT))
                return false;

            if (!WriteValue("runtime_version"u8, new ReadOnlySpan<byte>(RuntimeImports.RhGetRuntimeVersion(out int cbLength), cbLength)))
                return false;

            CrashReason crashReason = reason switch
            {
                RhFailFastReason.AssertionFailure or
                RhFailFastReason.EnvironmentFailFast => CrashReason.EnvironmentFailFast,
                RhFailFastReason.InternalError => CrashReason.InternalFailFast,
                RhFailFastReason.UnhandledException or
                RhFailFastReason.UnhandledExceptionFromPInvoke => CrashReason.UnhandledException,
                _ => CrashReason.Unknown,
            };

            if (!WriteIntValue("reason"u8, (int)crashReason))
                return false;

            if (!WriteHexValue("thread"u8, crashingThreadId))
                return false;

            if (!WriteStringValue("message"u8, message, maxChars: 1024))
                return false;

            return true;
        }

        /// <summary>
        /// Adds the exception info to the JSON buffer under the "exception" key. If the exception can not fit in
        /// the triage buffer the allocations are backed out.
        /// </summary>
        /// <param name="key">the UTF8 name of the block</param>
        /// <param name="exception">exception build triage block from</param>
        /// <param name="maxMessageSize">limits the size of the exception message strings</param>
        /// <param name="maxNumberStackFrames">limits the number of stack frames written to the triage buffer</param>
        /// <param name="maxMethodNameSize">limits the size of the stack frame method name strings</param>
        /// <returns>true - success, false - out of triage buffer space</returns>
        private bool WriteExceptionWithFallback(ReadOnlySpan<byte> key, Exception exception, int maxMessageSize, int maxNumberStackFrames, int maxMethodNameSize)
        {
            int savedIndex = _currentBufferIndex;
            if (!WriteExceptionHelper(key, exception, maxMessageSize, maxNumberStackFrames, maxMethodNameSize))
            {
                _currentBufferIndex = savedIndex;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Adds the exception info to the JSON buffer
        /// </summary>
        /// <param name="key">the UTF8 name of the block</param>
        /// <param name="exception">exception build triage block from</param>
        /// <param name="maxMessageSize">limits the size of the exception message strings</param>
        /// <param name="maxNumberStackFrames">limits the number of stack frames written to the triage buffer</param>
        /// <param name="maxMethodNameSize">limits the size of the stack frame method name strings</param>
        /// <returns>true - success, false - out of triage buffer space</returns>
        private bool WriteExceptionHelper(ReadOnlySpan<byte> key, Exception exception, int maxMessageSize, int maxNumberStackFrames, int maxMethodNameSize)
        {
            if (!OpenValue(key, '{'))
                return false;

            ulong address = Unsafe.As<Exception, nuint>(ref exception);
            if (!WriteHexValue("address"u8, address))
                return false;

            if (!WriteHexValue("hr"u8, exception.HResult))
                return false;

            if (!WriteStringValue("message"u8, exception.Message, maxMessageSize))
                return false;

            // Exception type names are not truncated because the full name is important to bucketing and usually not that long
            if (!WriteStringValue("type"u8, exception.GetType().ToString()))
                return false;

            StackFrame[] stackFrames = new StackTrace(exception).GetFrames();
            if (stackFrames.Length > 0)
            {
                if (!OpenValue("stack"u8, '['))
                    return false;

                int count = 0;
                foreach (StackFrame frame in stackFrames)
                {
                    // Check if the stack frame limit has been hit
                    if (++count > maxNumberStackFrames)
                        break;

                    if (!WriteStackFrame(frame, maxMethodNameSize))
                        return false;
                }

                CloseValue(']');
            }

            AggregateException? aggregate = exception as AggregateException;
            if (aggregate is not null || exception.InnerException is not null)
            {
                if (!OpenValue("inner"u8, '['))
                    return false;

                // Write as many inner exceptions that will fit
                if (aggregate is not null)
                {
                    foreach (Exception ex in aggregate.InnerExceptions)
                    {
                        if (!WriteExceptionWithFallback(default, ex, maxMessageSize, maxNumberStackFrames, maxMethodNameSize))
                            break;
                    }
                }
                else
                {
                    WriteExceptionWithFallback(default, exception.InnerException, maxMessageSize, maxNumberStackFrames, maxMethodNameSize);
                }

                CloseValue(']');
            }
            CloseValue('}');
            return true;
        }

        /// <summary>
        /// Write an exception stack frame to the triage buffer
        /// </summary>
        /// <param name="frame">the stack frame instance to write</param>
        /// <param name="maxNameSize">limits the size of the frame type name</param>
        /// <returns>true - success, false - out of triage buffer space</returns>
        private bool WriteStackFrame(StackFrame frame, int maxNameSize)
        {
            if (!OpenValue(default, '{'))
                return false;

            nint ip = frame.GetNativeIPAddress();
            if (!WriteHexValue("ip"u8, (nuint)ip))
                return false;

            nint moduleBase = RuntimeImports.RhGetOSModuleFromPointer(ip);
            if (moduleBase != nint.Zero)
            {
                if (!WriteHexValue("module"u8, (nuint)moduleBase))
                    return false;
            }

            if (!WriteHexValue("offset"u8, frame.GetNativeOffset()))
                return false;

            string method = DeveloperExperience.GetMethodName(ip, out _, out _);
            if (method != null)
            {
                if (!WriteStringValue("name"u8, method, maxNameSize))
                    return false;
            }
            CloseValue('}');
            return true;
        }

        private bool WriteHexValue(ReadOnlySpan<byte> key, ulong value) => WriteValue(key, $"0x{value:X}".AsSpan());

        private bool WriteHexValue(ReadOnlySpan<byte> key, int value) => WriteValue(key, $"0x{value:X}".AsSpan());

        private bool WriteIntValue(ReadOnlySpan<byte> key, int value) => WriteValue(key, $"{value}".AsSpan());

        private bool WriteStringValue(ReadOnlySpan<byte> key, string value, int maxChars = int.MaxValue, bool truncateLeft = false)
        {
            if (!OpenValue(key, '"'))
                return false;

            int length = value.Length;
            int count = Math.Min(length, maxChars);
            int start = truncateLeft ? Math.Max(0, length - maxChars) : 0;

            for (int i = 0; i < count; i++)
            {
                // Escape the special JSON characters.
                char c = value[start + i];
                if (c < 0x20)
                {
                    if (!WriteChars($"\\u{((ushort)c):X4}".AsSpan()))
                        return false;
                }
                else if (c == '"')
                {
                    if (!WriteBytes("\\\""u8))
                        return false;
                }
                else if (c == '\\')
                {
                    if (!WriteBytes("\\\\"u8))
                        return false;
                }
                else
                {
                    if (!WriteChar(c))
                        return false;
                }
            }

            CloseValue('"');
            return true;
        }

        private bool WriteValue(ReadOnlySpan<byte> key, ReadOnlySpan<char> value)
        {
            if (!OpenValue(key, '"'))
                return false;
            if (!WriteChars(value))
                return false;
            CloseValue('"');
            return true;
        }

        /// <summary>
        /// Write raw bytes or already converted to UTF8 string to the triage buffer.
        /// </summary>
        /// <param name="key">the UF8 name of the value</param>
        /// <param name="bytes">value</param>
        /// <returns>true - success, false - out of triage buffer space</returns>
        private bool WriteValue(ReadOnlySpan<byte> key, ReadOnlySpan<byte> bytes)
        {
            if (!OpenValue(key, '"'))
                return false;
            if (!WriteBytes(bytes))
                return false;
            CloseValue('"');
            return true;
        }

        private bool OpenValue(ReadOnlySpan<byte> key, char marker)
        {
            if (!WriteSeparator())
                return false;
            if (!key.IsEmpty)
            {
                if (!WriteChar('"'))
                    return false;
                if (!WriteBytes(key))
                    return false;
                if (!WriteChar('"'))
                    return false;
                if (!WriteChar(':'))
                    return false;
            }
            _reservedBuffer += 1;           // Reserve 1 byte for closing marker
            if (!WriteChar(marker))
            {
                _reservedBuffer -= 1;
                return false;
            }
            _isCommaNeeded = false;
            return true;
        }

        private void CloseValue(char marker)
        {
            _reservedBuffer -= 1;           // Make the reserved byte available for the closing marker
            bool success = WriteChar(marker);
            Debug.Assert(success);          // Should never fail because of the reservation
            _isCommaNeeded = true;
        }

        private bool WriteSeparator() => _isCommaNeeded ? WriteChar(',') : true;

        private bool WriteChar(char source) => WriteChars(new ReadOnlySpan<char>(in source));

        private bool WriteChars(ReadOnlySpan<char> chars)
        {
            int size = Encoding.UTF8.GetByteCount(chars);
            Span<byte> destination = AllocBuffer(size);
            if (destination.IsEmpty)
            {
                return false;
            }
            Encoding.UTF8.GetBytes(chars, destination);
            return true;
        }

        private bool WriteBytes(ReadOnlySpan<byte> bytes)
        {
            Span<byte> destination = AllocBuffer(bytes.Length);
            if (destination.IsEmpty)
            {
                return false;
            }
            bytes.CopyTo(destination);
            return true;
        }

        private Span<byte> AllocBuffer(int size)
        {
            Debug.Assert(size > 0);

            // Check if there is any space left in the triage buffer
            if ((_currentBufferIndex + size) >= (_maxBufferSize - _reservedBuffer))
            {
                return default;
            }
            byte* ptr = _bufferAddress + _currentBufferIndex;
            _currentBufferIndex += size;
            return new Span<byte>(ptr, size);
        }
    }
}
