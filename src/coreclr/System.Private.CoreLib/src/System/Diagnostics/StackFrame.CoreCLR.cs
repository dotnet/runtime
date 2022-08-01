// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

#pragma warning disable IDE0060
        private static bool AppendStackFrameWithoutMethodBase(StringBuilder sb) => false;
#pragma warning restore IDE0060

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "StackFrame_GetMethodDescFromNativeIP")]
        private static partial RuntimeMethodHandleInternal GetMethodDescFromNativeIP(IntPtr ip);

        /// <summary>
        /// Returns the MethodBase instance for the managed code IP address.
        ///
        /// Warning: The implementation of this method has race for dynamic and collectible methods.
        /// </summary>
        /// <param name="ip">code address</param>
        /// <returns>MethodBase instance for the method or null if IP not found</returns>
        internal static MethodBase? GetMethodFromNativeIP(IntPtr ip)
        {
            RuntimeMethodHandleInternal method = GetMethodDescFromNativeIP(ip);

            if (method.Value == IntPtr.Zero)
                return null;

            return RuntimeType.GetMethodBase(null, method);
        }
    }
}
