// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Reflection;

namespace System.Diagnostics
{
    /// <summary>
    /// There is no good reason for the methods of this class to be virtual.
    /// </summary>
    public partial class StackFrame
    {
        /// <summary>
        /// Reflection information for the method if available, null otherwise.
        /// </summary>
        private MethodBase? _method;

        /// <summary>
        /// Native offset of the current instruction within the current method if available,
        /// OFFSET_UNKNOWN otherwise.
        /// </summary>
        private int _nativeOffset;

        /// <summary>
        /// IL offset of the current instruction within the current method if available,
        /// OFFSET_UNKNOWN otherwise.
        /// </summary>
        private int _ilOffset;

        /// <summary>
        /// Source file name representing the current code location if available, null otherwise.
        /// </summary>
        private string? _fileName;

        /// <summary>
        /// Line number representing the current code location if available, 0 otherwise.
        /// </summary>
        private int _lineNumber;

        /// <summary>
        /// Column number representing the current code location if available, 0 otherwise.
        /// </summary>
        private int _columnNumber;

        /// <summary>
        /// This flag is set to true when the frame represents a rethrow marker.
        /// </summary>
        private bool _isLastFrameFromForeignExceptionStackTrace;

        private void InitMembers()
        {
            _nativeOffset = OFFSET_UNKNOWN;
            _ilOffset = OFFSET_UNKNOWN;
        }

        /// <summary>
        /// Constructs a StackFrame corresponding to the active stack frame.
        /// </summary>
        public StackFrame()
        {
            InitMembers();
            BuildStackFrame(StackTrace.METHODS_TO_SKIP, false);
        }

        /// <summary>
        /// Constructs a StackFrame corresponding to the active stack frame.
        /// </summary>
        public StackFrame(bool needFileInfo)
        {
            InitMembers();
            BuildStackFrame(StackTrace.METHODS_TO_SKIP, needFileInfo);
        }

        /// <summary>
        /// Constructs a StackFrame corresponding to a calling stack frame.
        /// </summary>
        public StackFrame(int skipFrames)
        {
            InitMembers();
            BuildStackFrame(skipFrames + StackTrace.METHODS_TO_SKIP, false);
        }

        /// <summary>
        /// Constructs a StackFrame corresponding to a calling stack frame.
        /// </summary>
        public StackFrame(int skipFrames, bool needFileInfo)
        {
            InitMembers();
            BuildStackFrame(skipFrames + StackTrace.METHODS_TO_SKIP, needFileInfo);
        }

        /// <summary>
        /// Constructs a "fake" stack frame, just containing the given file
        /// name and line number.  Use when you don't want to use the
        /// debugger's line mapping logic.
        /// </summary>
        public StackFrame(string? fileName, int lineNumber)
        {
            InitMembers();

            BuildStackFrame(StackTrace.METHODS_TO_SKIP, false);
            _fileName = fileName;
            _lineNumber = lineNumber;
        }

        /// <summary>
        /// Constructs a "fake" stack frame, just containing the given file
        /// name, line number and column number.  Use when you don't want to
        /// use the debugger's line mapping logic.
        /// </summary>
        public StackFrame(string? fileName, int lineNumber, int colNumber)
            : this (fileName, lineNumber)
        {
            _columnNumber = colNumber;
        }

        /// <summary>
        /// Constant returned when the native or IL offset is unknown
        /// </summary>
        public const int OFFSET_UNKNOWN = -1;

        internal bool IsLastFrameFromForeignExceptionStackTrace => _isLastFrameFromForeignExceptionStackTrace;

        /// <summary>
        /// Returns the method the frame is executing
        /// </summary>
        public virtual MethodBase? GetMethod()
        {
            return _method;
        }

        /// <summary>
        /// Returns the offset from the start of the native (jitted) code for the
        /// method being executed
        /// </summary>
        public virtual int GetNativeOffset()
        {
            return _nativeOffset;
        }


        /// <summary>
        /// Returns the offset from the start of the IL code for the
        /// method being executed.  This offset may be approximate depending
        /// on whether the jitter is generating debuggable code or not.
        /// </summary>
        public virtual int GetILOffset()
        {
            return _ilOffset;
        }

        /// <summary>
        /// Returns the file name containing the code being executed.  This
        /// information is normally extracted from the debugging symbols
        /// for the executable.
        /// </summary>
        public virtual string? GetFileName()
        {
            return _fileName;
        }

        /// <summary>
        /// Returns the line number in the file containing the code being executed.
        /// This information is normally extracted from the debugging symbols
        /// for the executable.
        /// </summary>
        public virtual int GetFileLineNumber()
        {
            return _lineNumber;
        }

        /// <summary>
        /// Returns the column number in the line containing the code being executed.
        /// This information is normally extracted from the debugging symbols
        /// for the executable.
        /// </summary>
        public virtual int GetFileColumnNumber()
        {
            return _columnNumber;
        }

        /// <summary>
        /// Builds a readable representation of the stack frame
        /// </summary>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(255);
            bool includeFileInfoIfAvailable;

            if (_method != null)
            {
                sb.Append(_method.Name);

                // deal with the generic portion of the method
                if (_method is MethodInfo methodInfo && methodInfo.IsGenericMethod)
                {
                    Type[] typars = methodInfo.GetGenericArguments();

                    sb.Append('<');
                    int k = 0;
                    bool fFirstTyParam = true;
                    while (k < typars.Length)
                    {
                        if (fFirstTyParam == false)
                            sb.Append(',');
                        else
                            fFirstTyParam = false;

                        sb.Append(typars[k].Name);
                        k++;
                    }

                    sb.Append('>');
                }
                includeFileInfoIfAvailable = true;
            }
            else
            {
                includeFileInfoIfAvailable = AppendStackFrameWithoutMethodBase(sb);
            }

            if (includeFileInfoIfAvailable)
            {
                sb.Append(" at offset ");
                if (_nativeOffset == OFFSET_UNKNOWN)
                    sb.Append("<offset unknown>");
                else
                    sb.Append(_nativeOffset);

                sb.Append(" in file:line:column ");
                sb.Append(_fileName ?? "<filename unknown>");
                sb.Append(':');
                sb.Append(_lineNumber);
                sb.Append(':');
                sb.Append(_columnNumber);
            }
            else
            {
                sb.Append("<null>");
            }
            sb.Append(Environment.NewLine);

            return sb.ToString();
        }
    }
}
