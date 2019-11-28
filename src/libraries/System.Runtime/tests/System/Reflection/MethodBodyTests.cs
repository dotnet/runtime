// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using Xunit;

#pragma warning disable 0219  // field is never used

namespace System.Reflection.Tests
{
    public static class MethodBodyTests
    {
        [Fact]
        public static void Test_MethodBody_ExceptionHandlingClause()
        {
            MethodInfo mi = typeof(MethodBodyTests).GetMethod("MethodBodyExample", BindingFlags.NonPublic | BindingFlags.Static);
            MethodBody mb = mi.GetMethodBody();

            Assert.True(mb.InitLocals);  // local variables are initialized

            Assert.Equal(2, mb.MaxStackSize);
            // Release expects 3, Debug 3.
            AssertExtensions.AtLeastOneEquals(3, 5, mb.LocalVariables.Count);

            foreach (LocalVariableInfo lvi in mb.LocalVariables)
            {
                if (lvi.LocalIndex == 0) { Assert.Equal(typeof(int), lvi.LocalType); }
                if (lvi.LocalIndex == 1) { Assert.Equal(typeof(string), lvi.LocalType); }
                // Release expects Exception, Debug bool.
                if (lvi.LocalIndex == 2) { AssertExtensions.AtLeastOneEquals(typeof(Exception), typeof(bool), lvi.LocalType); }
                if (lvi.LocalIndex == 3) { Assert.Equal(typeof(bool), lvi.LocalType); }
                if (lvi.LocalIndex == 4) { Assert.Equal(typeof(Exception), lvi.LocalType); }
            }

            foreach (ExceptionHandlingClause ehc in mb.ExceptionHandlingClauses)
            {
                if (ehc.Flags != ExceptionHandlingClauseOptions.Finally && ehc.Flags != ExceptionHandlingClauseOptions.Filter)
                {
                    Assert.Equal(typeof(Exception), ehc.CatchType);

                    // First arg is for Release, second for Debug.
                    AssertExtensions.AtLeastOneEquals(14, 19, ehc.HandlerLength);
                    AssertExtensions.AtLeastOneEquals(58, 70, ehc.HandlerOffset);
                    AssertExtensions.AtLeastOneEquals(50, 61, ehc.TryLength);
                    AssertExtensions.AtLeastOneEquals(8, 9, ehc.TryOffset);
                    return;
                }
            }

            Assert.True(false, "Expected to find CatchType clause.");
        }

        private static void MethodBodyExample(object arg)
        {
            int var1 = 2;
            string var2 = "I am a string";

            try
            {
                if (arg == null)
                {
                    throw new ArgumentNullException("Input argument cannot be null.");
                }
                if (arg.GetType() == typeof(string))
                {
                    throw new ArgumentException("Input argument cannot be a string.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                var1 = 3;
                var2 = "I am a new string!";
            }
        }
    }
}
