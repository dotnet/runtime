// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System;
using System.IO;
using System.Reflection;

namespace System.Diagnostics
{
    /// <summary>
    /// There is no good reason for the methods of this class to be virtual.
    /// </summary>
    public partial class StackFrame
    {
        /// <summary>
        /// Called from the class "StackTrace"
        /// </summary>
        internal StackFrame(bool DummyFlag1, bool DummyFlag2)
        {
            InitMembers();
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
