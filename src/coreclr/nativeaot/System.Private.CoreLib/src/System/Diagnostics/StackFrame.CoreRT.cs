// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text;

using Internal.DeveloperExperience;

namespace System.Diagnostics
{
    /// <summary>
    /// Stack frame represents a single frame in a stack trace; frames
    /// corresponding to methods with available symbolic information
    /// provide source file / line information. Some frames may provide IL
    /// offset information and / or MethodBase reflection information.
    /// There is no good reason for the methods of this class to be virtual.
    /// </summary>
    public partial class StackFrame
    {
        /// <summary>
        /// IP address representing this stack frame.
        /// </summary>
        private IntPtr _ipAddress;

        /// <summary>
        /// File info flag to use for stack trace-style formatting.
        /// </summary>
        private bool _needFileInfo;

        /// <summary>
        /// Constructs a StackFrame corresponding to a given IP address.
        /// </summary>
        internal StackFrame(IntPtr ipAddress, bool needFileInfo)
        {
            InitializeForIpAddress(ipAddress, needFileInfo);
        }

        /// <summary>
        /// Internal stack frame initialization based on IP address.
        /// </summary>
        private void InitializeForIpAddress(IntPtr ipAddress, bool needFileInfo)
        {
            _ipAddress = ipAddress;
            _needFileInfo = needFileInfo;

            if (_ipAddress == Exception.EdiSeparator)
            {
                _isLastFrameFromForeignExceptionStackTrace = true;
            }
            else if (_ipAddress != IntPtr.Zero)
            {
                IntPtr methodStartAddress = RuntimeImports.RhFindMethodStartAddress(ipAddress);

                _nativeOffset = (int)((nint)_ipAddress - (nint)methodStartAddress);

                DeveloperExperience.Default.TryGetILOffsetWithinMethod(_ipAddress, out _ilOffset);
                DeveloperExperience.Default.TryGetMethodBase(methodStartAddress, out _method);

                if (needFileInfo)
                {
                    DeveloperExperience.Default.TryGetSourceLineInfo(
                        _ipAddress,
                        out _fileName,
                        out _lineNumber,
                        out _columnNumber);
                }
            }
        }

        /// <summary>
        /// Internal stack frame initialization based on frame index within the stack of the current thread.
        /// </summary>
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private void BuildStackFrame(int frameIndex, bool needFileInfo)
        {
            const int SystemDiagnosticsStackDepth = 2;

            frameIndex += SystemDiagnosticsStackDepth;
            IntPtr[] frameArray = new IntPtr[frameIndex + 1];
            int returnedFrameCount = RuntimeImports.RhGetCurrentThreadStackTrace(frameArray);
            int realFrameCount = (returnedFrameCount >= 0 ? returnedFrameCount : frameArray.Length);

            IntPtr ipAddress = (frameIndex < realFrameCount) ? frameArray[frameIndex] : IntPtr.Zero;
            InitializeForIpAddress(ipAddress, needFileInfo);
        }

        /// <summary>
        /// Return native IP address for this stack frame.
        /// </summary>
        internal IntPtr GetNativeIPAddress()
        {
            return _ipAddress;
        }

        /// <summary>
        /// Check whether method info is available.
        /// </summary>
        internal bool HasMethod()
        {
            return _method != null;
        }

        /// <summary>
        /// Format stack frame without MethodBase info. Return true if the stack info
        /// is valid and line information should be appended if available.
        /// </summary>
        private bool AppendStackFrameWithoutMethodBase(StringBuilder builder)
        {
            builder.Append(DeveloperExperience.Default.CreateStackTraceString(_ipAddress, includeFileInfo: false));
            return true;
        }

        /// <summary>
        /// Set rethrow marker.
        /// </summary>
        internal void SetIsLastFrameFromForeignExceptionStackTrace()
        {
            _isLastFrameFromForeignExceptionStackTrace = true;
        }

        /// <summary>
        /// Builds a representation of the stack frame for use in the stack trace.
        /// </summary>
        internal void AppendToStackTrace(StringBuilder builder)
        {
            if (_ipAddress != Exception.EdiSeparator)
            {
                // Passing a default string for "at" in case SR.UsingResourceKeys() is true
                // as this is a special case and we don't want to have "Word_At" on stack traces.
                string word_At = SR.GetResourceString(nameof(SR.Word_At), defaultString: "at");
                builder.AppendFormat("   {0} ", word_At);
                builder.AppendLine(DeveloperExperience.Default.CreateStackTraceString(_ipAddress, _needFileInfo));
            }
            if (_isLastFrameFromForeignExceptionStackTrace)
            {
                builder.AppendLine(SR.StackTrace_EndStackTraceFromPreviousThrow);
            }
        }
    }
}
