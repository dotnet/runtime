// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;


namespace UninitializedHighWord
{
    public struct StackFiller
    {
        public Int32 Field00;
        public Int32 Field01;
        public Int32 Field02;
        public Int32 Field03;
        public Int32 Field04;
        public Int32 Field05;
        public Int32 Field06;
        public Int32 Field07;
        public Int32 Field08;
        public Int32 Field09;
        public Int32 Field10;
        public Int32 Field11;
        public Int32 Field12;
        public Int32 Field13;
        public Int32 Field14;
        public Int32 Field15;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void FillWithFFPattern(ref StackFiller target)
        {
            target.Field00 = -1;
            target.Field01 = -1;
            target.Field02 = -1;
            target.Field03 = -1;
            target.Field04 = -1;
            target.Field05 = -1;
            target.Field06 = -1;
            target.Field07 = -1;
            target.Field08 = -1;
            target.Field09 = -1;
            target.Field10 = -1;
            target.Field11 = -1;
            target.Field12 = -1;
            target.Field13 = -1;
            target.Field14 = -1;
            target.Field15 = -1;

            return;
        }
    }


    public struct SystemTime
    {
        public short Year;
        public short Month;
        public short DayOfWeek;
        public short Day;
        public short Hour;
        public short Minute;
        public short Second;
        public short Milliseconds;
    }


    public struct RegistryTimeZoneInformation
    {
        public Int32 Bias;
        public Int32 StandardBias;
        public Int32 DaylightBias;
        public SystemTime StandardDate;
        public SystemTime DaylightDate;
    }


    public static class App
    {


        private static bool s_fArgumentCheckPassed = false;
        private static bool s_fPreparingMethods = false;


        [MethodImpl(MethodImplOptions.NoInlining)]
        private static
        void CheckArguments(
            Int32 fill,
            Int32 year,
            Int32 month,
            Int32 day,
            Int32 hour,
            Int32 minute,
            Int32 second,
            Int32 milliseconds
            )
        {
            if (App.s_fPreparingMethods)
            {
                return;
            }
            else
            {
                if ((hour == 0) &&
                    (minute == 0) &&
                    (second == 0) &&
                    (milliseconds == 0))
                {
                    App.s_fArgumentCheckPassed = true;
                    Console.WriteLine("Argument check passed.  All trailing arguments are zero.");
                }
                else
                {
                    App.s_fArgumentCheckPassed = false;

                    Console.WriteLine(
                        "Argument check failed.  Trailing argument values are:\r\n" +
                        "    Hour           = {0:x8}\r\n" +
                        "    Minute         = {1:x8}\r\n" +
                        "    Second         = {2:x8}\r\n" +
                        "    Milliseconds   = {3:x8}\r\n",
                        hour,
                        minute,
                        second,
                        milliseconds
                    );
                }

                return;
            }
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private static
        void GenerateHalfInitializedArgSlots(
            RegistryTimeZoneInformation timeZoneInformation
            )
        {
            if (timeZoneInformation.DaylightDate.Year == 0)
            {
                App.CheckArguments(
                    1,
                    1,
                    1,
                    1,
                    timeZoneInformation.DaylightDate.Hour,
                    timeZoneInformation.DaylightDate.Minute,
                    timeZoneInformation.DaylightDate.Second,
                    timeZoneInformation.DaylightDate.Milliseconds
                );
            }

            return;
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private static
        void InitializeStack(
            Int32 arg1,
            Int32 arg2,
            Int32 arg3,
            Int32 arg4,
            StackFiller fill1,
            StackFiller fill2,
            StackFiller fill3,
            StackFiller fill4
            )
        {
            return;
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private static
        void StompStackBelowCallerSP(
            )
        {
            var filler = new StackFiller();

            StackFiller.FillWithFFPattern(ref filler);

            App.InitializeStack(
                1, 1, 1, 1,
                filler, filler, filler, filler
            );

            return;
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private static
        void PrepareMethods(
            )
        {
            var timeZoneInformation = new RegistryTimeZoneInformation();

            App.s_fPreparingMethods = true;
            {
                App.GenerateHalfInitializedArgSlots(timeZoneInformation);
            }
            App.s_fPreparingMethods = false;

            return;
        }


        [MethodImpl(MethodImplOptions.NoInlining)]
        private static
        int RunTest(
            )
        {
            var timeZoneInformation = new RegistryTimeZoneInformation();



            App.StompStackBelowCallerSP();




            App.GenerateHalfInitializedArgSlots(timeZoneInformation);



            if (App.s_fArgumentCheckPassed)
            {
                Console.WriteLine("Passed.");
                return 100;
            }
            else
            {
                Console.WriteLine("Failed.");
                return 101;
            }
        }


        [Fact]
        public static int TestEntryPoint()
        {
            App.PrepareMethods();

            return App.RunTest();
        }
    }
}
