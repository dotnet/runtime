// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Internal.Diagnostics;

namespace System.Diagnostics
{
    public sealed class StackTrace
    {
        private readonly IntPtr[] _ips;
        private readonly bool _needFileInfo;

        public StackTrace(Exception e, bool needFileInfo)
        {
            if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }
            _ips = ExceptionExtensions.GetStackIPs(e);
            _needFileInfo = needFileInfo;
        }

        public StackFrame[] GetFrames()
        {
            // There are special IPs mixed in to the list that aren't intended to be exposed as StackFrames
            // Deliberately doing this without Linq to keep the dependency set down.
            StackFrame[] frames = new StackFrame[_ips.Length];
            int countNonSpecialIPs = 0;
            for (int i = 0; i < _ips.Length; i++)
            {
                if (_ips[i] != StackTraceHelper.SpecialIP.EdiSeparator)
                {
                    frames[countNonSpecialIPs++] = new StackFrame(_ips[i], _needFileInfo);
                }
            }
            //for compat with CoreCLR, we always return null instead of 0 length array
            if (countNonSpecialIPs == 0)
            {
                return null;
            }
            Array.Resize(ref frames, countNonSpecialIPs);
            return frames;
        }

        public override string ToString()
        {
            return StackTraceHelper.FormatStackTrace(_ips, _needFileInfo);
        }
    }
}
