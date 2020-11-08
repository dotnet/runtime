// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading
{
    public sealed partial class Thread
    {
        private static Exception GetApartmentStateChangeFailedException()
        {
            string msg = SR.Format(SR.Thread_ApartmentState_ChangeFailed, Thread.CurrentThread.GetApartmentState());
            return new InvalidOperationException(msg);
        }
    }
}
