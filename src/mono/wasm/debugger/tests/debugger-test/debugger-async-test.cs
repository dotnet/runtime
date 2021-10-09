// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace DebuggerTests.AsyncTests
{
    public class ContinueWithTests
    {
        public DateTime Date => new DateTime(2510, 1, 2, 3, 4, 5);

        public static async Task RunAsync()
        {
            await ContinueWithStaticAsync("foobar");
            await new ContinueWithTests().ContinueWithInstanceAsync("foobar");

            await NestedContinueWithStaticAsync("foobar");
            await new ContinueWithTests().NestedContinueWithInstanceAsync("foobar");
            await new ContinueWithTests().ContinueWithInstanceUsingThisAsync("foobar");

        }

        public static async Task ContinueWithStaticAsync(string str)
        {
            await Task.Delay(1000).ContinueWith(t =>
            {
                var code = t.Status;
                var dt = new DateTime(4513, 4, 5, 6, 7, 8);
                Console.WriteLine ($"First continueWith: {code}, {dt}"); //t, code, dt
            });
            Console.WriteLine ($"done with this method");
        }

        public static async Task NestedContinueWithStaticAsync(string str)
        {
            await Task.Delay(500).ContinueWith(async t =>
            {
                var code = t.Status;
                var ncs_dt0 = new DateTime(3412, 4, 6, 8, 0, 2);
                Console.WriteLine ($"First continueWith: {code}, {ncs_dt0}"); // t, code, str, dt0
                await Task.Delay(300).ContinueWith(t2 =>
                {
                    var ncs_dt1 = new DateTime(4513, 4, 5, 6, 7, 8);
                    Console.WriteLine ($"t2: {t2.Status}, str: {str}, {ncs_dt1}, {ncs_dt0}");//t2, dt1, str, dt0
                });
            });
            Console.WriteLine ($"done with this method");
        }

        public async Task ContinueWithInstanceAsync(string str)
        {
            await Task.Delay(1000).ContinueWith(t =>
            {
                var code = t.Status;
                var dt = new DateTime(4513, 4, 5, 6, 7, 8);
                Console.WriteLine ($"First continueWith: {code}, {dt}");// t, code, dt
            });
            Console.WriteLine ($"done with this method");
        }

        public async Task ContinueWithInstanceUsingThisAsync(string str)
        {
            await Task.Delay(1000).ContinueWith(t =>
            {
                var code = t.Status;
                var dt = new DateTime(4513, 4, 5, 6, 7, 8);
                Console.WriteLine ($"First continueWith: {code}, {dt}, {this.Date}");
            });
            Console.WriteLine ($"done with this method");
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public async Task NestedContinueWithInstanceAsync(string str)
        {
            await Task.Delay(500).ContinueWith(async t =>
            {
                var code = t.Status;
                var dt0 = new DateTime(3412, 4, 6, 8, 0, 2);
                if (str == "oi")
                {
                    dt0 = new DateTime(3415, 4, 6, 8, 0, 2);
                }
                Console.WriteLine ($"First continueWith: {code}, {dt0}, {Date}");//this, t, code, str, dt0
                await Task.Delay(300).ContinueWith(t2 =>
                {
                    var dt1 = new DateTime(4513, 4, 5, 6, 7, 8);
                    Console.WriteLine ($"t2: {t2.Status}, str: {str}, {dt1}, {dt0}");//this, t2, dt1, str, dt0
                });
            });
            Console.WriteLine ($"done with this method");
        }

    }

}
