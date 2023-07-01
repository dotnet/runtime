// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text;
using Internal.DeveloperExperience;

namespace System
{
    internal unsafe class CrashInfo
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

        /// <summary>
        /// This block is passed to the Watson DOTNET CLRMA provider and must not have any GC references
        /// </summary>
        private struct TriageBlockBuffer
        {
            public const int BufferSize = 8192;
            public fixed byte Buffer[BufferSize];
        }

        private bool _comma;
        private int _currentBufferIndex;
        private int _reservedBuffer;
        private static TriageBlockBuffer _triageBuffer;

        public CrashInfo()
        {
            _comma = false;
            _currentBufferIndex = 0;
            _reservedBuffer = 1;            // Reserve 1 byte for closing }
        }

        /// <summary>
        /// Writes the opening bracket and header to triage buffer
        /// </summary>
        /// <param name="reason"></param>
        /// <param name="message"></param>
        public void Open(RhFailFastReason reason, string message)
        {
            // Write the opening bracket and basic header which should never fail
            bool success = WriteChar('{');
            Debug.Assert(success);
            success = WriteHeader(reason, message);
            Debug.Assert(success);
        }

        /// <summary>
        /// Write the closing bracket. The triage buffer is ready.
        /// </summary>
        public void Close()
        {
            // Write the closing bracket which should never fail since it was reserved
            _reservedBuffer = 0;
            bool success = WriteChar('}');
            Debug.Assert(success);
        }

        /// <summary>
        /// Write the exception information with fallbacks if the info doesn't fit in the fixed size triage buffer.
        /// </summary>
        /// <param name="exception">exception object</param>
        public void WriteExceptionInfo(Exception exception)
        {
            if (!WriteBlock("exception", '{', '}', () => WriteException(exception, int.MaxValue, 500, int.MaxValue)))
            {
                // If the buffer isn't big enough to fit 500 stack frames, try limiting to 10
                if (!WriteBlock("exception", '{', '}', () => WriteException(exception, int.MaxValue, 10, int.MaxValue)))
                {
                    // If that fails, try limiting the size of the stack frame method names to 100 bytes
                    WriteBlock("exception", '{', '}', () => WriteException(exception, int.MaxValue, 10, 100));
                }
            }
        }

        /// <summary>
        /// Get the rendered crash triage buffer address
        /// </summary>
        public IntPtr GetTriageBuffer(out int size)
        {
            size = _currentBufferIndex;
            return new IntPtr(Unsafe.AsPointer<TriageBlockBuffer>(ref _triageBuffer));
        }

        /// <summary>
        /// Writes the basic triage information header. Assumes there is always enough
        /// room in the buffer for this header.
        /// </summary>
        /// <param name="reason">kind of crash</param>
        /// <param name="message">fail fast message, limited to 1024 chars</param>
        /// <returns>true - success, false - out of triage buffer space</returns>
        private bool WriteHeader(RhFailFastReason reason, string message)
        {
            if (!WriteValue("version", "1.0.0"))
                return false;

            if (!WriteValue("runtime", new ReadOnlySpan<byte>(RuntimeImports.RhGetRuntimeVersion(out int cb), cb)))
                return false;

            if (!WriteValue("runtime_type", (int)RuntimeType.NativeAOT))
                return false;

            CrashReason crashReason = reason switch
            {
                RhFailFastReason.EnvironmentFailFast => CrashReason.EnvironmentFailFast,
                RhFailFastReason.InternalError or
                RhFailFastReason.ClassLibDidNotTranslateExceptionID => CrashReason.InternalFailFast,
                RhFailFastReason.UnhandledException or
                RhFailFastReason.UnhandledExceptionFromPInvoke or
                RhFailFastReason.UnhandledException_ExceptionDispatchNotAllowed or
                RhFailFastReason.UnhandledException_CallerDidNotHandle => CrashReason.UnhandledException,
                _ => CrashReason.Unknown,
            };

            if (!WriteValue("reason", (int)crashReason))
                return false;

            if (!WriteValue("message", message, max: 1024))
                return false;

            return true;
        }

        /// <summary>
        /// Adds the exception info to the JSON buffer
        /// </summary>
        /// <param name="exception">exception build triage block from</param>
        /// <param name="maxMessageSize">limits the size of the exception message strings</param>
        /// <param name="maxNumberStackFrames">limits the number of stack frames written to the triage buffer</param>
        /// <param name="maxMethodNameSize">limits the size of the stack frame method name strings</param>
        /// <returns>true - success, false - out of triage buffer space</returns>
        private bool WriteException(Exception exception, int maxMessageSize, int maxNumberStackFrames, int maxMethodNameSize)
        {
            if (!WriteHexValue("address", (ulong)Unsafe.AsPointer(ref exception)))
                return false;

            if (!WriteHexValue("hr", exception.HResult))
                return false;

            if (!WriteValue("message", exception.Message, maxMessageSize))
                return false;

            // Exception type names are not truncated because the full name is important to bucketing and usually not that long
            if (!WriteValue("type", exception.GetType().ToString()))
                return false;

            bool success = WriteBlock("stack", '[', ']', () =>
            {
                StackTrace stackTrace = new(exception);
                int count = 0;
                foreach (StackFrame frame in stackTrace.GetFrames())
                {
                    // Check if the stack frame limit has been hit
                    if (++count > maxNumberStackFrames)
                        break;

                    // Write as many stack frames that will fit
                    if (!WriteStackFrame(frame, maxMethodNameSize))
                        break;
                }
                return true;
            });

            if (!success)
                return false;

            AggregateException? aggregate = exception as AggregateException;
            if (aggregate is not null || exception.InnerException is not null)
            {
                success = WriteBlock("inner", '[', ']', () =>
                {
                    // Write as many inner exceptions that will fit
                    if (aggregate is not null)
                    {
                        foreach (Exception ex in aggregate.InnerExceptions)
                        {
                            if (!WriteBlock(null, '{', '}', () => WriteException(ex, maxMessageSize, maxNumberStackFrames, maxMethodNameSize)))
                                break;
                        }
                    }
                    else
                    {
                        WriteBlock(null, '{', '}', () => WriteException(exception.InnerException, maxMessageSize, maxNumberStackFrames, maxMethodNameSize));
                    }
                    return true;
                });

                if (!success)
                    return false;
            }
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
            return WriteBlock(null, '{', '}', () =>
            {
                nint ip = frame.GetNativeIPAddress();
                if (!WriteHexValue("ip", (ulong)ip))
                    return false;

                if (!WriteHexValue("offset", frame.GetNativeOffset()))
                    return false;

                string method = DeveloperExperience.GetMethodName(ip, out IntPtr _);
                if (method != null)
                {
                    if (!WriteValue("name", method, maxNameSize))
                        return false;
                }
                return true;
            });
        }

        private bool WriteHexValue(ReadOnlySpan<char> key, ulong value) => WriteValue(key, $"0x{value:X}");

        private bool WriteHexValue(ReadOnlySpan<char> key, int value) => WriteValue(key, $"0x{value:X}");

        private bool WriteValue(ReadOnlySpan<char> key, int value) => WriteValue(key, value.ToString());

        private bool WriteValue(ReadOnlySpan<char> key, string value, int max = int.MaxValue) => WriteBlock(key, '"', '"', () => WriteChars(value, max));

        /// <summary>
        /// Opens and closes an object or array. If the block can not fit in the triage buffer
        /// the allocations are backed out and the block is skipped.
        /// </summary>
        /// <param name="key">the name of the block</param>
        /// <param name="opening">opening char of the block { or [</param>
        /// <param name="closing">closing char of the block } or ]</param>
        /// <param name="callback">called to fill in the object or array values</param>
        /// <returns>true - success, false - out of triage buffer space</returns>
        private bool WriteBlock(ReadOnlySpan<char> key, char opening, char closing, Func<bool> callback)
        {
            int savedIndex = _currentBufferIndex;
            if (!OpenValue(key, opening))
                goto error;
            if (!callback())
                goto error;
            if (!CloseValue(closing))
                goto error;
            return true;
        error:
            _currentBufferIndex = savedIndex;
            return false;
        }

        /// <summary>
        /// Write raw bytes or already converted to UTF8 string to the triage buffer. If the block can not
        /// fit in the triage buffer the allocations are backed out and the value is skipped.
        /// </summary>
        /// <param name="key">the name of the value</param>
        /// <param name="bytes">value</param>
        /// <returns>true - success, false - out of triage buffer space</returns>
        private bool WriteValue(ReadOnlySpan<char> key, ReadOnlySpan<byte> bytes)
        {
            int savedIndex = _currentBufferIndex;
            if (!OpenValue(key, '"'))
                goto error;
            if (!WriteSpan(bytes))
                goto error;
            if (!CloseValue('"'))
                goto error;
            return true;
        error:
            _currentBufferIndex = savedIndex;
            return false;
        }

        private bool OpenValue(ReadOnlySpan<char> key, char marker)
        {
            if (!WriteSeparator())
                return false;
            if (!key.IsEmpty)
            {
                if (!WriteChar('"'))
                    return false;
                if (!WriteChars(key))
                    return false;
                if (!WriteChar('"'))
                    return false;
                if (!WriteChar(':'))
                    return false;
            }
            if (!WriteChar(marker))
                return false;
            _comma = false;
            return true;
        }

        private bool CloseValue(char marker)
        {
            if (!WriteChar(marker))
                return false;
            _comma = true;
            return true;
        }

        private bool WriteSeparator() => _comma ? WriteChar(',') : true;

        private bool WriteChar(char source) => WriteChars(new ReadOnlySpan<char>(source));

        private bool WriteChars(ReadOnlySpan<char> chars, int max = int.MaxValue)
        {
            int size = Encoding.UTF8.GetByteCount(chars);
            size = Math.Min(size, max);
            Span<byte> destination = AllocBuffer(size);
            if (destination.IsEmpty)
            {
                return false;
            }
            Encoding.UTF8.GetBytes(chars.Slice(0, size), destination);
            return true;
        }

        private bool WriteSpan(ReadOnlySpan<byte> bytes)
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

            fixed (byte* ptr = &_triageBuffer.Buffer[_currentBufferIndex])
            {
                // Check if there is any space left in the triage buffer
                if ((_currentBufferIndex + size) >= (TriageBlockBuffer.BufferSize - _reservedBuffer))
                {
                    return default;
                }
                _currentBufferIndex += size;
                return new Span<byte>(ptr, size);
            }
        }
    }
}
