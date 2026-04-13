// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Diagnostics
{
    public partial class StackTrace
    {
        private const string PerfMapSymbolReaderTypeName = "System.Diagnostics.PerfMapSymbolReader, System.Diagnostics.StackTrace, Version=4.0.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct PerfMapSequencePointInfoNative
        {
            public int lineNumber;
            public int ilOffset;
            public char* fileName;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct PerfMapLocalVarInfoNative
        {
            public int startOffset;
            public int endOffset;
            public char* name;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct PerfMapMethodDebugInfoNative
        {
            public PerfMapSequencePointInfoNative* points;
            public int size;
            public PerfMapLocalVarInfoNative* locals;
            public int localsSize;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "StackTrace_GetStackFramesInternal")]
        private static partial void GetStackFramesInternal(ObjectHandleOnStack sfh, [MarshalAs(UnmanagedType.Bool)] bool fNeedFileInfo, ObjectHandleOnStack e);

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "GetInfoForMethod")]
        private static extern unsafe bool GetPerfMapInfoForMethodFromReader(
            [UnsafeAccessorType(PerfMapSymbolReaderTypeName)] object? perfMapSymbolReader,
            [MarshalAs(UnmanagedType.LPUTF8Str)] string assemblyPath,
            int methodToken,
            IntPtr points,
            int size);

        [DynamicDependency(nameof(GetInfoForMethod))]
        internal static void GetStackFramesInternal(StackFrameHelper sfh, bool fNeedFileInfo, Exception? e)
            => GetStackFramesInternal(ObjectHandleOnStack.Create(ref sfh), fNeedFileInfo, ObjectHandleOnStack.Create(ref e));

        [UnmanagedCallersOnly]
        internal static unsafe int GetInfoForMethod(byte* assemblyPath, uint methodToken, PerfMapMethodDebugInfoNative* methodDebugInfo)
        {
            if (assemblyPath == null || methodDebugInfo == null || methodDebugInfo->points == null || methodDebugInfo->size <= 0)
            {
                return 0;
            }

            methodDebugInfo->locals = null;
            methodDebugInfo->localsSize = 0;

            try
            {
                string? assemblyPathString = Marshal.PtrToStringUTF8((IntPtr)assemblyPath);
                if (string.IsNullOrEmpty(assemblyPathString))
                {
                    return 0;
                }

                return GetPerfMapInfoForMethodFromReader(null, assemblyPathString, unchecked((int)methodToken), (IntPtr)methodDebugInfo->points, methodDebugInfo->size) ? 1 : 0;
            }
            catch
            {
                return 0;
            }
        }

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
