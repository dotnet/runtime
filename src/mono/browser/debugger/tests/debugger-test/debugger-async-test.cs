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
                Console.WriteLine("done with this continueWith");
                await Task.Delay(300).ContinueWith(t2 => Console.WriteLine ($"t2: {t2.Status}, str: {str}, {dt0}"));
            });
            Console.WriteLine ($"done with this method");
        }

        public static async Task RunAsyncWithLineHidden()
        {
            await HiddenLinesInAnAsyncBlock("foobar");
            await HiddenLinesJustBeforeANestedAsyncBlock("foobar");
            await HiddenLinesAtTheEndOfANestedAsyncBlockWithNoLinesAtEndOfTheMethod("foobar"); 
            await HiddenLinesAtTheEndOfANestedAsyncBlockWithBreakableLineAtEndOfTheMethod("foobar"); 
            await HiddenLinesContainingStartOfAnAsyncBlock("foobar");
            await HiddenLinesAtTheEndOfANestedAsyncBlockWithWithLineDefaultOutsideTheMethod("foobar");
            await HiddenLinesAtTheEndOfANestedAsyncBlockWithWithLineDefaultOutsideTheMethod2("foobar");
            System.Diagnostics.Debugger.Break();
        }
        public static async Task HiddenLinesInAnAsyncBlock(string str)
        {
            await Task.Delay(500).ContinueWith(async t =>
            {
#line hidden
                var code = t.Status;
#line default
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
        static async Task HiddenLinesJustBeforeANestedAsyncBlock(string str)
        {
            await Task.Delay(500).ContinueWith(async t =>
            {
                Console.WriteLine($"First continueWith");
        #line hidden
                var code = t.Status; // Next line will be in the next async block? hidden line just before async block
                Console.WriteLine("another line of code");
        #line default
                await Task.Delay(300).ContinueWith(t2 =>
                {
                    var ncs_dt1 = new DateTime(4513, 4, 5, 6, 7, 8);
                    Console.WriteLine($"t2: {t2.Status}, str: {str}, {ncs_dt1}");//t2, dt1, str, dt0
                });
            });
            Console.WriteLine($"done with this method");
        }

        static async Task HiddenLinesAtTheEndOfANestedAsyncBlockWithNoLinesAtEndOfTheMethod(string str)
        {
            await Task.Delay(500).ContinueWith(async t =>
            {
                var code = t.Status;
                Console.WriteLine($"First continueWith");
                await Task.Delay(300).ContinueWith(t2 =>
                {
                    var ncs_dt1 = new DateTime(4513, 4, 5, 6, 7, 8);
                    Console.WriteLine($"t2: {t2.Status}, str: {str}, {ncs_dt1}");//t2, dt1, str, dt0
        #line hidden
                    Console.WriteLine("something else"); // Next line will be in the next async block? hidden line at end of async block
        #line default
                });
            });
        }

        static async Task HiddenLinesAtTheEndOfANestedAsyncBlockWithBreakableLineAtEndOfTheMethod(string str)
        {
            await Task.Delay(500).ContinueWith(async t =>
            {
                var code = t.Status;
                Console.WriteLine($"First continueWith");
                await Task.Delay(300).ContinueWith(t2 =>
                {
                    var ncs_dt1 = new DateTime(4513, 4, 5, 6, 7, 8);
                    Console.WriteLine($"t2: {t2.Status}, str: {str}, {ncs_dt1}");//t2, dt1, str, dt0
        #line hidden
                    Console.WriteLine("something else"); // Next line will be in the next async block? hidden line at end of async block
        #line default
                });
            });
            Console.WriteLine ($"Last line..");
        }

        static async Task HiddenLinesContainingStartOfAnAsyncBlock(string str)
        {
            await Task.Delay(500).ContinueWith(async t =>
            {
                var code = t.Status;
                Console.WriteLine($"First continueWith");
        #line hidden
                await Task.Delay(300).ContinueWith(t2 =>
        #line default
                {
                    var ncs_dt1 = new DateTime(4513, 4, 5, 6, 7, 8);
                    Console.WriteLine($"t2: {t2.Status}, str: {str}, {ncs_dt1}");//t2, dt1, str, dt0
                    Console.WriteLine("something else"); // Next line will be in the next async block? hidden line at end of async block
                });
            });
            Console.WriteLine($"done with this method");
        }

        static async Task HiddenLinesAtTheEndOfANestedAsyncBlockWithWithLineDefaultOutsideTheMethod(string str)
        {
            await Task.Delay(500).ContinueWith(async t =>
            {
                var code = t.Status;
                Console.WriteLine($"First continueWith");
                await Task.Delay(300).ContinueWith(t2 =>
                {
                    var ncs_dt1 = new DateTime(4513, 4, 5, 6, 7, 8);
                    Console.WriteLine($"t2: {t2.Status}, str: {str}, {ncs_dt1}");//t2, dt1, str, dt0
        #line hidden
                    Console.WriteLine("somethind else"); // Next line will be in the next async block? hidden line at end of async block
                });
            });
        #line default
            Console.WriteLine($"done with this method");
        }

        static async Task HiddenLinesAtTheEndOfANestedAsyncBlockWithWithLineDefaultOutsideTheMethod2(string str)
        {
            await Task.Delay(500).ContinueWith(async t =>
            {
                var code = t.Status;
                Console.WriteLine($"First continueWith");
                await Task.Delay(300).ContinueWith(t2 =>
                {
                    var ncs_dt1 = new DateTime(4513, 4, 5, 6, 7, 8);
                    Console.WriteLine($"t2: {t2.Status}, str: {str}, {ncs_dt1}");//t2, dt1, str, dt0
        #line hidden
                    Console.WriteLine("somethind else"); // Next line will be in the next async block? hidden line at end of async block
                });
        #line default
            });
            Console.WriteLine($"done with this method");
        }
    }

    public class VariablesWithSameNameDifferentScopes
    {
        public static async Task Run()
        {
            await RunCSharpScope(10);
            await RunCSharpScope(1000);
        }

        public static async Task<string> RunCSharpScope(int number)
        {
            await Task.Delay(1);
            if (number < 999)
            {
                string testCSharpScope = "hello"; string onlyInFirstScope = "only-in-first-scope";
                System.Diagnostics.Debugger.Break();
                return testCSharpScope;
            }
            else
            {
                string testCSharpScope = "hi"; string onlyInSecondScope = "only-in-second-scope";
                System.Diagnostics.Debugger.Break();
                return testCSharpScope;
            }
        }

        public static async Task RunContinueWith()
        {
            await RunContinueWithSameVariableName(10);
            await RunContinueWithSameVariableName(1000);
        }

        public static async Task RunNestedContinueWith()
        {
            await RunNestedContinueWithSameVariableName(10);
            await RunNestedContinueWithSameVariableName(1000);
        }

        public static async Task RunContinueWithSameVariableName(int number)
        {
            await Task.Delay(500).ContinueWith(async t =>
            {
                await Task.Delay(1);
                if (number < 999)
                {
                    var testCSharpScope = new String("hello"); string onlyInFirstScope = "only-in-first-scope";
                    System.Diagnostics.Debugger.Break();
                    return testCSharpScope;
                }
                else
                {
                    var testCSharpScope = new String("hi"); string onlyInSecondScope = "only-in-second-scope";
                    System.Diagnostics.Debugger.Break();
                    return testCSharpScope;
                }
            });
            Console.WriteLine ($"done with this method");
        }

        public static async Task RunNestedContinueWithSameVariableName(int number)
        {
            await Task.Delay(500).ContinueWith(async t =>
            {
                if (number < 999)
                {
                    var testCSharpScope = new String("hello_out"); string onlyInFirstScope = "only-in-first-scope_out";
                    Console.WriteLine(testCSharpScope);
                }
                else
                {
                    var testCSharpScope = new String("hi_out"); string onlyInSecondScope = "only-in-second-scope_out";
                    Console.WriteLine(testCSharpScope);
                }
                await Task.Delay(300).ContinueWith(t2 =>
                {
                    if (number < 999)
                    {
                        var testCSharpScope = new String("hello"); string onlyInFirstScope = "only-in-first-scope";
                        System.Diagnostics.Debugger.Break();
                        return testCSharpScope;
                    }
                    else
                    {
                        var testCSharpScope = new String("hi"); string onlyInSecondScope = "only-in-second-scope";
                        System.Diagnostics.Debugger.Break();
                        return testCSharpScope;
                    }
                });
            });
            Console.WriteLine ($"done with this method");
        }

        public static void RunNonAsyncMethod()
        {
            RunNonAsyncMethodSameVariableName(10);
            RunNonAsyncMethodSameVariableName(1000);
        }

        public static string RunNonAsyncMethodSameVariableName(int number)
        {
            if (number < 999)
            {
                var testCSharpScope = new String("hello"); string onlyInFirstScope = "only-in-first-scope";
                System.Diagnostics.Debugger.Break();
                return testCSharpScope;
            }
            else
            {
                var testCSharpScope = new String("hi"); string onlyInSecondScope = "only-in-second-scope";
                System.Diagnostics.Debugger.Break();
                return testCSharpScope;
            }
        }
    }

}
