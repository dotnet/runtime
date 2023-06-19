// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

public class P {
        public static void Main () {
                bool isMono = typeof(object).Assembly.GetType("Mono.RuntimeStructs") != null;
                Console.WriteLine(isMono ? "from Mono!" : "from CoreCLR!");
                Console.WriteLine(System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);

                var l = new object();
                //var s = new Semaphore (0, 1);

                var t = new Thread(() => {
                        try {
                                Console.WriteLine ("other thread trying to Enter");
                                //s.WaitOne();
                                lock (l) {
                                        Console.WriteLine ("BAD: acquired lock");
                                        Console.WriteLine ($"IsEntered by other thread: {Monitor.IsEntered(l)}");
                                }
                        } catch (ThreadInterruptedException) {
                                Console.WriteLine ("interrupted");
                        }
                });

                t.IsBackground = true;

                lock (l) {
                        t.Start();
                        Console.WriteLine ("main thread sleeping");
                        Thread.Sleep (2000);
                        Console.WriteLine ("main thread interrupting");
                        t.Interrupt();
                        Console.WriteLine ("main thread interrupted; sleeping");
                        Thread.Sleep (2000);
                        Console.WriteLine ("main thread unlocking");
                        //s.Release();
                }
                t.Join();
        }
}
