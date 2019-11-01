// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace System.Diagnostics
{
    public partial class StackFrame
    {
        /// <summary>
        /// Called from the class "StackTrace"
        /// </summary>
        internal StackFrame(StackFrameHelper stackFrameHelper, int skipFrames, bool needFileInfo)
        {
            _method = stackFrameHelper.GetMethodBase(skipFrames);
            _nativeOffset = stackFrameHelper.GetOffset(skipFrames);
            _ilOffset = stackFrameHelper.GetILOffset(skipFrames);
            _isLastFrameFromForeignExceptionStackTrace = stackFrameHelper.IsLastFrameFromForeignExceptionStackTrace(skipFrames);

            if (needFileInfo)
            {
                _fileName = stackFrameHelper.GetFilename(skipFrames);
                _lineNumber = stackFrameHelper.GetLineNumber(skipFrames);
                _columnNumber = stackFrameHelper.GetColumnNumber(skipFrames);
            }
        }

        private void BuildStackFrame(int skipFrames, bool needFileInfo)
        {
            StackFrameHelper StackF = new StackFrameHelper(null);

            StackF.InitializeSourceInfo(0, needFileInfo, null);

            int iNumOfFrames = StackF.GetNumberOfFrames();

            skipFrames += StackTrace.CalculateFramesToSkip(StackF, iNumOfFrames);

            if ((iNumOfFrames - skipFrames) > 0)
            {
                _method = StackF.GetMethodBase(skipFrames);
                _nativeOffset = StackF.GetOffset(skipFrames);
                _ilOffset = StackF.GetILOffset(skipFrames);
                if (needFileInfo)
                {
                    _fileName = StackF.GetFilename(skipFrames);
                    _lineNumber = StackF.GetLineNumber(skipFrames);
                    _columnNumber = StackF.GetColumnNumber(skipFrames);
                }
            }
        }

        private bool AppendStackFrameWithoutMethodBase(StringBuilder sb) => false;
    }
}
