// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

#pragma warning disable 0219  // field is never used

namespace System.Reflection.Tests
{
    public static class MethodBodyTests
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsMethodBodySupported))]
        public static void Test_MethodBody_ExceptionHandlingClause()
        {
            MethodInfo mi = typeof(MethodBodyTests).GetMethod("MethodBodyExample", BindingFlags.NonPublic | BindingFlags.Static);
            MethodBody mb = mi.GetMethodBody();

            var il = mb.GetILAsByteArray();
            if (il?.Length == 1 && il[0] == 0x2a) // ILStrip replaces method bodies with the 'ret' IL opcode i.e. 0x2a
                throw new SkipTestException("The method body was processed using ILStrip.");

            Assert.True(mb.InitLocals);  // local variables are initialized
#if DEBUG
            Assert.Equal(2, mb.MaxStackSize);
            Assert.Equal(5, mb.LocalVariables.Count);

            foreach (LocalVariableInfo lvi in mb.LocalVariables)
            {
                if (lvi.LocalIndex == 0) { Assert.Equal(typeof(int), lvi.LocalType); }
                if (lvi.LocalIndex == 1) { Assert.Equal(typeof(string), lvi.LocalType); }
                if (lvi.LocalIndex == 2) { Assert.Equal(typeof(bool), lvi.LocalType); }
                if (lvi.LocalIndex == 3) { Assert.Equal(typeof(bool), lvi.LocalType); }
                if (lvi.LocalIndex == 4) { Assert.Equal(typeof(Exception), lvi.LocalType); }
            }

            foreach (ExceptionHandlingClause ehc in mb.ExceptionHandlingClauses)
            {
                if (ehc.Flags != ExceptionHandlingClauseOptions.Finally && ehc.Flags != ExceptionHandlingClauseOptions.Filter)
                {
                    Assert.Equal(typeof(Exception), ehc.CatchType);
                    Assert.Equal(19, ehc.HandlerLength);
                    Assert.Equal(70, ehc.HandlerOffset);
                    Assert.Equal(61, ehc.TryLength);
                    Assert.Equal(9, ehc.TryOffset);
                    return;
                }
            }
#else
            Assert.Equal(2, mb.MaxStackSize);
            Assert.Equal(3, mb.LocalVariables.Count);

            foreach (LocalVariableInfo lvi in mb.LocalVariables)
            {
                if (lvi.LocalIndex == 0) { Assert.Equal(typeof(int), lvi.LocalType); }
                if (lvi.LocalIndex == 1) { Assert.Equal(typeof(string), lvi.LocalType); }
                if (lvi.LocalIndex == 2) { Assert.Equal(typeof(Exception), lvi.LocalType); }
            }

            foreach (ExceptionHandlingClause ehc in mb.ExceptionHandlingClauses)
            {
                if (ehc.Flags != ExceptionHandlingClauseOptions.Finally && ehc.Flags != ExceptionHandlingClauseOptions.Filter)
                {
                    Assert.Equal(typeof(Exception), ehc.CatchType);
                    Assert.Equal(14, ehc.HandlerLength);
                    Assert.Equal(58, ehc.HandlerOffset);
                    Assert.Equal(50, ehc.TryLength);
                    Assert.Equal(8, ehc.TryOffset);
                    return;
                }
            }
#endif

            Assert.Fail("Expected to find CatchType clause.");
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
