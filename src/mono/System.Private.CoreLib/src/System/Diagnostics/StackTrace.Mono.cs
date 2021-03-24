// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Diagnostics
{
    // Need our own stackframe class since the shared version has its own fields
    [StructLayout(LayoutKind.Sequential)]
    internal sealed class MonoStackFrame
    {
        #region Keep in sync with object-internals.h
        internal int ilOffset;
        internal int nativeOffset;
        // Unused
        internal long methodAddress;
        // Unused
        internal uint methodIndex;
        internal MethodBase? methodBase;
        internal string? fileName;
        internal int lineNumber;
        internal int columnNumber;
        // Unused
        internal string? internalMethodName;
        #endregion

        internal bool isLastFrameFromForeignException;
    }

    public partial class StackTrace
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern MonoStackFrame[] get_trace(Exception e, int skipFrames, bool needFileInfo);

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        private void InitializeForCurrentThread(int skipFrames, bool needFileInfo)
        {
            skipFrames += 2; // Current method + parent ctor

            StackFrame sf;
            var frames = new List<StackFrame>();
            while (skipFrames >= 0)
            {
                sf = new StackFrame(skipFrames, needFileInfo);
                if (sf.GetMethod() == null)
                {
                    break;
                }
                frames.Add(sf);
                skipFrames++;
            }

            _stackFrames = frames.ToArray();
            _numOfFrames = _stackFrames.Length;
        }

        private void InitializeForException(Exception e, int skipFrames, bool needFileInfo)
        {
            MonoStackFrame[] frames = get_trace(e, skipFrames, needFileInfo);
            _numOfFrames = frames.Length;

            int foreignFrames;
            MonoStackFrame[]? foreignExceptions = e.foreignExceptionsFrames;

            if (foreignExceptions != null)
            {
                foreignFrames = foreignExceptions.Length;
                _numOfFrames += foreignFrames;

                _stackFrames = new StackFrame[_numOfFrames];

                for (int i = 0; i < foreignExceptions.Length; ++i)
                {
                    _stackFrames[i] = new StackFrame(foreignExceptions[i], needFileInfo);
                }
            }
            else
            {
                _stackFrames = new StackFrame[_numOfFrames];
                foreignFrames = 0;
            }

            for (int i = 0; i < frames.Length; ++i)
            {
                _stackFrames[foreignFrames + i] = new StackFrame(frames[i], needFileInfo);
            }
        }
    }
}
