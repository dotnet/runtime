// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace DefaultApartmentStateMain
{
    internal static class DefaultApartmentStateMain
    {
        private const int Success = 0;
        private const int SuccessOnUnix = 2;
        private const int Failure = 1;

        private static Thread s_mainThread;

        static int Main(string[] args)
        {
            string testName = args[0];
            s_mainThread = Thread.CurrentThread;

            switch (testName)
            {
                case "GetApartmentStateTest":
                    return GetApartmentStateTest();
                case "SetApartmentStateTest":
                    return SetApartmentStateTest();
                default:
                    return Failure;
            }
        }

        private static int GetApartmentStateTest()
        {
            if (s_mainThread.GetApartmentState() == ApartmentState.MTA)
            {
                s_mainThread.SetApartmentState(ApartmentState.MTA);
                return Success;
            }
            return SuccessOnUnix;
        }

        private static int SetApartmentStateTest()
        {
            try
            {
                s_mainThread.SetApartmentState(ApartmentState.STA);
            }
            catch (InvalidOperationException)
            {
                return Success;
            }
            catch (PlatformNotSupportedException)
            {
                return SuccessOnUnix;
            }
            return Failure;
        }
    }
}
