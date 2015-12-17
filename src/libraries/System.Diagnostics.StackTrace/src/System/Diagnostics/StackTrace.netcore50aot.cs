// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Internal.Diagnostics;

namespace System.Diagnostics
{
    public sealed class StackTrace
    {
        private readonly IntPtr[] _ips;
        private readonly bool _fNeedFileInfo;

        public StackTrace(Exception exception, bool needFileInfo)
        {
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }
            _ips = ExceptionExtensions.GetStackIPs(exception);
            _fNeedFileInfo = needFileInfo;
        }

        public StackFrame[] GetFrames()
        {
            // There are special IPs mixed in to the list that aren't intended to be exposed as StackFrames
            // Deliberately doing this without Linq to keep the dependency set down.
            StackFrame[] tempFrames = new StackFrame[_ips.Length];
            int countNonSpecialIPs = 0;
            for (int i = 0; i < _ips.Length; i++)
            {
                if (_ips[i] != StackTraceHelper.SpecialIP.EdiSeparator)
                {
                    tempFrames[countNonSpecialIPs++] = new StackFrame(_ips[i], _fNeedFileInfo);
                }
            }
            //for compat with CoreCLR, we always return null instead of 0 length array
            if (countNonSpecialIPs == 0)
            {
                return null;
            }
            StackFrame[] frames = new StackFrame[countNonSpecialIPs];
            Array.Copy(tempFrames, frames, countNonSpecialIPs);
            return frames;
        }

        public override string ToString()
        {
            return StackTraceHelper.FormatStackTrace(_ips, _fNeedFileInfo);
        }
    }
}
