// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System
{
    public static partial class Environment
    {
        public static unsafe long WorkingSet
        {
            get
            {
                nint cookie = 0;
                long workingSet = 0;

                Interop.OS.area_info areaInfo;

                while (Interop.OS.GetNextAreaInfo(ProcessId, ref cookie, out areaInfo) == 0)
                {
                    workingSet += areaInfo.ram_size;
                }

                return workingSet;
            }
        }
    }
}
