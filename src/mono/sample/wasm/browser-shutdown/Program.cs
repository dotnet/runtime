// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.InteropServices;

namespace Sample
{
    public partial class Test
    {
        public static int Main(string[] args)
        {
            return 0;
        }

        [JSExport()]
        public static void DoNothing ()
        {
            Console.WriteLine("You got it, boss! Doing nothing!");
        }

        [JSExport()]
        public static void ThrowManagedException ()
        {
            throw new Exception("I'll make an exception to the rules just this once... and throw one.");
        }

        [JSExport()]
        public static void CallFailFast ()
        {
            System.Environment.FailFast("User requested FailFast");
        }

        [JSImport("timerTick", "main.js")]
        public static partial void TimerTick (int i);

        [JSExport()]
        public static void StartTimer ()
        {
            int i = 0;
            var timer = new System.Timers.Timer(1000);
            timer.Elapsed += (s, e) => {
                TimerTick(i);
                i += 1;
            };
            timer.AutoReset = true;
            timer.Enabled = true;
        }
    }
}
