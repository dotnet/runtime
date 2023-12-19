// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Diagnostics
{
    public partial class StackTrace
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void GetStackFramesInternal(StackFrameHelper sfh, int iSkip, bool fNeedFileInfo, Exception? e);

        internal static int CalculateFramesToSkip(StackFrameHelper StackF, int iNumFrames)
        {
            int iRetVal = 0;
            const string PackageName = "System.Diagnostics";

            // Check if this method is part of the System.Diagnostics
            // package. If so, increment counter keeping track of
            // System.Diagnostics functions
            for (int i = 0; i < iNumFrames; i++)
            {
                MethodBase? mb = StackF.GetMethodBase(i);
                if (mb != null)
                {
                    Type? t = mb.DeclaringType;
                    if (t == null)
                        break;
                    string? ns = t.Namespace;
                    if (ns == null)
                        break;
                    if (!string.Equals(ns, PackageName, StringComparison.Ordinal))
                        break;
                }
                iRetVal++;
            }

            return iRetVal;
        }

        private void InitializeForException(Exception? exception, int skipFrames, bool fNeedFileInfo)
        {
            CaptureStackTrace(skipFrames, fNeedFileInfo, exception);
        }

        private void InitializeForCurrentThread(int skipFrames, bool fNeedFileInfo)
        {
            CaptureStackTrace(skipFrames, fNeedFileInfo, null);
        }

        /// <summary>
        /// Retrieves an object with stack trace information encoded.
        /// It leaves out the first "iSkip" lines of the stacktrace.
        /// </summary>
        private void CaptureStackTrace(int skipFrames, bool fNeedFileInfo, Exception? e)
        {
            _methodsToSkip = skipFrames;

            StackFrameHelper StackF = new StackFrameHelper(null);

            StackF.InitializeSourceInfo(0, fNeedFileInfo, e);

            _numOfFrames = StackF.GetNumberOfFrames();

            if (_methodsToSkip > _numOfFrames)
                _methodsToSkip = _numOfFrames;

            if (_numOfFrames != 0)
            {
                _stackFrames = new StackFrame[_numOfFrames];

                for (int i = 0; i < _numOfFrames; i++)
                {
                    _stackFrames[i] = new StackFrame(StackF, i, fNeedFileInfo);
                }

                // CalculateFramesToSkip skips all frames in the System.Diagnostics namespace,
                // but this is not desired if building a stack trace from an exception.
                if (e == null)
                    _methodsToSkip += CalculateFramesToSkip(StackF, _numOfFrames);

                _numOfFrames -= _methodsToSkip;
                if (_numOfFrames < 0)
                {
                    _numOfFrames = 0;
                }
            }
        }
    }
}
