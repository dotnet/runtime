// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.Diagnostics
{
    public partial class StackFrame
    {
        internal StackFrame(MonoStackFrame monoStackFrame, bool needFileInfo)
        {
            _method = monoStackFrame.methodBase;
            _nativeOffset = monoStackFrame.nativeOffset;
            _ilOffset = monoStackFrame.ilOffset;

            if (needFileInfo)
            {
                _fileName = monoStackFrame.fileName;
                _lineNumber = monoStackFrame.lineNumber;
                _columnNumber = monoStackFrame.columnNumber;
            }

            _isLastFrameFromForeignExceptionStackTrace = monoStackFrame.isLastFrameFromForeignException;
        }

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private void BuildStackFrame(int skipFrames, bool needFileInfo)
        {
            const int SystemDiagnosticsStackDepth = 3;

            if (skipFrames + SystemDiagnosticsStackDepth < 0)
                return;

            MethodBase? method = null;
            string? fileName = null;
            bool success = GetFrameInfo(skipFrames + SystemDiagnosticsStackDepth, needFileInfo,
                                        ObjectHandleOnStack.Create(ref method), ObjectHandleOnStack.Create(ref fileName),
                                        out int ilOffset, out int nativeOffset, out int line, out int column);
            if (!success)
                return;

            _method = method;
            _ilOffset = ilOffset;
            _nativeOffset = nativeOffset;

            if (needFileInfo)
            {
                _fileName = fileName;
                _lineNumber = line;
                _columnNumber = column;
            }
        }

#pragma warning disable IDE0060
        private static bool AppendStackFrameWithoutMethodBase(StringBuilder sb) => false;
#pragma warning restore IDE0060

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool GetFrameInfo(int skipFrames, bool needFileInfo,
                                                ObjectHandleOnStack out_method, ObjectHandleOnStack out_file,
                                                out int ilOffset, out int nativeOffset, out int line, out int column);
    }
}
