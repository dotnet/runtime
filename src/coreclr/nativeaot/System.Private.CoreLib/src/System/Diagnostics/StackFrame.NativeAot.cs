// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text;

using Internal.Reflection.Augments;
using Internal.Runtime.Augments;

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
        /// Will be true if we attempted to retrieve the associated MethodBase but couldn't.
        /// </summary>
        private bool _noMethodBaseAvailable;

        private bool _isStackTraceHidden;

        // If stack trace metadata is available, _methodOwningType is the namespace-qualified name of the owning type,
        // _methodName is the name of the method, _methodGenericArgs are generic arguments, and _methodSignature is the list of parameters
        // without braces. StackTrace will format this as `{_methodOwningType}.{_methodName}<{_genericArgs}>({_methodSignature}).
        // We keep this separate because StackFrame.ToString is defined as returning _methodName[{_genericArgs}].
        // If stack trace metadata is unavailable, only _methodName is populated and contains the "{fileNameWithoutExtension}!<BaseAddress>+0x{rva:x}"
        private string _methodOwningType;
        private string _methodName;
        private string _methodGenericArgs;
        private string _methodSignature;

        /// <summary>
        /// Returns the method the frame is executing
        /// </summary>
        [RequiresUnreferencedCode("Metadata for the method might be incomplete or removed. Consider using " + nameof(DiagnosticMethodInfo) + "." + nameof(DiagnosticMethodInfo.Create) + " instead")]
        public virtual MethodBase? GetMethod()
        {
            TryInitializeMethodBase();
            return _method;
        }

        internal bool TryGetMethodStartAddress(out IntPtr startAddress)
        {
            if (_ipAddress == IntPtr.Zero || _ipAddress == Exception.EdiSeparator)
            {
                startAddress = IntPtr.Zero;
                return false;
            }

            startAddress = _ipAddress - _nativeOffset;
            Debug.Assert(RuntimeImports.RhFindMethodStartAddress(_ipAddress) == startAddress);
            return true;
        }

        private bool TryInitializeMethodBase()
        {
            if (_noMethodBaseAvailable || _ipAddress == IntPtr.Zero || _ipAddress == Exception.EdiSeparator)
                return false;

            if (_method != null)
                return true;

            IntPtr methodStartAddress = _ipAddress - _nativeOffset;
            Debug.Assert(RuntimeImports.RhFindMethodStartAddress(_ipAddress) == methodStartAddress);
            _method = ReflectionAugments.GetMethodBaseFromStartAddressIfAvailable(methodStartAddress);
            if (_method == null)
            {
                _noMethodBaseAvailable = true;
                return false;
            }
            return true;
        }

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
                _ilOffset = StackFrame.OFFSET_UNKNOWN;

                StackTraceMetadataCallbacks stackTraceCallbacks = RuntimeAugments.StackTraceCallbacksIfAvailable;
                if (stackTraceCallbacks != null)
                {
                    _methodName = stackTraceCallbacks.TryGetMethodStackFrameInfo(methodStartAddress, _nativeOffset, needFileInfo, out _methodOwningType, out _methodGenericArgs, out _methodSignature, out _isStackTraceHidden, out _fileName, out _lineNumber);
                }

                if (_methodName == null)
                {
                    // If we don't have precise information, try to map it at least back to the right module.
                    string moduleFullFileName = RuntimeAugments.TryGetFullPathToApplicationModule(_ipAddress, out IntPtr moduleBase);

                    // Without any callbacks or the ability to map ip correctly we better admit that we don't know
                    if (string.IsNullOrEmpty(moduleFullFileName))
                    {
                        _methodName = "<unknown>";
                    }
                    else
                    {
                        ReadOnlySpan<char> fileNameWithoutExtension = Path.GetFileNameWithoutExtension(moduleFullFileName.AsSpan());
                        int rva = (int)(_ipAddress - moduleBase);
                        _methodName = $"{fileNameWithoutExtension}!<BaseAddress>+0x{rva:x}";
                    }
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
            return TryInitializeMethodBase();
        }

        /// <summary>
        /// Format stack frame without MethodBase info. Return true if the stack info
        /// is valid and line information should be appended if available.
        /// </summary>
        private bool AppendStackFrameWithoutMethodBase(StringBuilder builder)
        {
            builder.Append(_methodName);
            if (_methodGenericArgs != null)
            {
                builder.Append('<');
                builder.Append(_methodGenericArgs);
                builder.Append('>');
            }
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
                if (!_isStackTraceHidden)
                {
                    // Passing a default string for "at" in case SR.UsingResourceKeys() is true
                    // as this is a special case and we don't want to have "Word_At" on stack traces.
                    string word_At = SR.UsingResourceKeys() ? "at" : SR.Word_At;
                    // We also want to pass in a default for inFileLineNumber.
                    string inFileLineNum = SR.UsingResourceKeys() ? "in {0}:line {1}" : SR.StackTrace_InFileLineNumber;
                    builder.Append("   ").Append(word_At).Append(' ');

                    AppendCommonStringRepresenation(builder, allowFallback: true);

                    if (_fileName != null)
                    {
                        // tack on " in c:\tmp\MyFile.cs:line 5"
                        builder.Append(' ');
                        builder.AppendFormat(CultureInfo.InvariantCulture, inFileLineNum, _fileName, _lineNumber);
                    }

                    builder.AppendLine();
                }
            }
            if (_isLastFrameFromForeignExceptionStackTrace)
            {
                // Passing default for Exception_EndStackTraceFromPreviousThrow in case SR.UsingResourceKeys is set.
                builder.AppendLine(SR.UsingResourceKeys() ?
                    "--- End of stack trace from previous location ---" :
                    SR.Exception_EndStackTraceFromPreviousThrow);
            }
        }

        private void AppendCommonStringRepresenation(StringBuilder builder, bool allowFallback)
        {
            if (_methodOwningType != null)
            {
                builder.Append(_methodOwningType);
                builder.Append('.');
                builder.Append(_methodName);
                if (_methodGenericArgs != null)
                {
                    builder.Append('[');
                    builder.Append(_methodGenericArgs);
                    builder.Append(']');
                }
                builder.Append('(');
                builder.Append(_methodSignature);
                builder.Append(')');
            }
            else if (allowFallback)
            {
                Debug.Assert(_methodSignature == null);
                builder.Append(_methodName);
            }
        }

        internal string GetCrashInfoString()
        {
            StringBuilder sb = new StringBuilder();
            AppendCommonStringRepresenation(sb, allowFallback: false);
            return sb.Length > 0 ? sb.ToString() : null;
        }
    }
}
