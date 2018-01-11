// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace STAMain
{
    internal static class STAMain
    {
        [STAThread]
        static int Main(string[] args)
        {
            const int Success = 0;
            const int SuccessOnUnix = 2;
            const int Failure = 1;

            string mode = args[0];
            int retValue = Failure;
            Thread curThread = Thread.CurrentThread;
            
            if (mode == "GetApartmentState")
            {
                if (curThread.GetApartmentState() == ApartmentState.STA)
                {
                    curThread.SetApartmentState(ApartmentState.STA);
                    retValue = Success;
                }
                else
                {
                    retValue = SuccessOnUnix;
                }
            }
            else
            {
                try
                {
                    curThread.SetApartmentState(ApartmentState.MTA);
                }
                catch (InvalidOperationException)
                {
                    retValue = Success;
                }
                catch (PlatformNotSupportedException)
                {
                    retValue = SuccessOnUnix;
                }
            }

            return retValue;
        }
    }
}
