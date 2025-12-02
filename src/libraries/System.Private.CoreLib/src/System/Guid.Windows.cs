// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    public partial struct Guid
    {
        public static unsafe Guid NewGuid()
        {
            // CoCreateGuid should never return Guid.Empty, since it attempts to maintain some uniqueness guarantees.

            Guid g;
            int hr = Interop.Ole32.CoCreateGuid(&g);
            if (hr != 0)
            {
                ThrowForHr(hr);
            }

            return g;
        }

        private static void ThrowForHr(int hr)
        {
            // We don't expect that this will ever throw an error, none are even documented, and so we don't want to pull
            // in the HR to ComException mappings into the core library just for this so we will try a generic exception if
            // we ever hit this condition.
            throw new Exception() { HResult = hr };
        }
    }
}
