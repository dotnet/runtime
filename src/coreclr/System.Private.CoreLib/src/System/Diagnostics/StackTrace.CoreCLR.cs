// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Diagnostics
{
    public partial class StackTrace
    {
        private StackFrameHelper? _stackFrameHelper;
        private bool _fNeedFileInfo;

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "StackTrace_GetStackFramesInternal")]
        private static partial void GetStackFramesInternal(ObjectHandleOnStack sfh, [MarshalAs(UnmanagedType.Bool)] bool fNeedFileInfo, ObjectHandleOnStack e);

        internal static void GetStackFramesInternal(StackFrameHelper sfh, bool fNeedFileInfo, Exception? e)
            => GetStackFramesInternal(ObjectHandleOnStack.Create(ref sfh), fNeedFileInfo, ObjectHandleOnStack.Create(ref e));

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

            StackFrameHelper StackF = new StackFrameHelper();

            StackF.InitializeSourceInfo(fNeedFileInfo, e);

            _numOfFrames = StackF.GetNumberOfFrames();
            _stackFrameHelper = StackF;

            if (_methodsToSkip > _numOfFrames)
                _methodsToSkip = _numOfFrames;

            _fNeedFileInfo = fNeedFileInfo;

            // CalculateFramesToSkip skips all frames in the System.Diagnostics namespace,
            // but this is not desired if building a stack trace from an exception.
            if (e == null)
                _methodsToSkip += CalculateFramesToSkip(_stackFrameHelper, _numOfFrames);

            _numOfFrames -= _methodsToSkip;
            if (_numOfFrames < 0)
            {
                _numOfFrames = 0;
            }
        }

        /// <summary>
        /// Gets the <see cref="StackFrame"/> array for this StackTrace, possibly creating if needed.
        /// </summary>
        /// <returns></returns>
        private StackFrame[] GetFramesCore()
        {
            if (_numOfFrames != 0 && _stackFrames == null)
            {
                Debug.Assert(_stackFrameHelper != null);

                // the frame array should contain all frames (even skipped ones!)
                StackFrame[] stackFrames = new StackFrame[_stackFrameHelper.GetNumberOfFrames()];

                for (int i = 0; i < stackFrames.Length; i++)
                {
                    stackFrames[i] = new StackFrame(_stackFrameHelper, i, _fNeedFileInfo);
                }

                Interlocked.CompareExchange(ref _stackFrames, stackFrames, null);
            }

            return _stackFrames ?? [];
        }
    }
}
