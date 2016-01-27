// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using Internal.Diagnostics;
using Internal.Runtime.Augments;

namespace System.Diagnostics
{
    public sealed class StackFrame
    {
        private readonly IntPtr _ip;
        private readonly bool _includeFileInfo;
        private bool _fetchedFileInfo;
        private string _fileName;
        private int _lineNumber;
        private int _columnNumber;

        public const int OFFSET_UNKNOWN = -1;

        internal StackFrame(IntPtr ip, bool includeFileInfo)
        {
            _ip = ip;
            _includeFileInfo = includeFileInfo;
        }

        public int GetILOffset()
        {
            return OFFSET_UNKNOWN;
        }

        public int GetFileColumnNumber()
        {
            EnsureSourceFileInfo();
            return _columnNumber;
        }

        public int GetFileLineNumber()
        {
            EnsureSourceFileInfo();
            return _lineNumber;
        }

        public string GetFileName()
        {
            EnsureSourceFileInfo();
            return _fileName;
        }

        public MethodBase GetMethod()
        {
            return null;
        }

        public override string ToString()
        {
            return StackTraceHelper.FormatStackFrame(_ip, _includeFileInfo);
        }

        internal IntPtr GetNativeIP()
        {
            return _ip;
        }

        internal IntPtr GetNativeImageBase()
        {
            return _ip - RuntimeAugments.ConvertIpToRva(_ip);
        }

        private void EnsureSourceFileInfo()
        {
            //this isn't thread-safe and that is OK
            if (!_fetchedFileInfo && _includeFileInfo)
            {
                StackTraceHelper.TryGetSourceLineInfo(_ip, out _fileName, out _lineNumber, out _columnNumber);
                //Even if we didn't get all the data, this is the best we'll get and we shouldn't try again
                _fetchedFileInfo = true;
            }
        }
    }
}
