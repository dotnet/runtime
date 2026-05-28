// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.CompilerServices;
using Xunit;
using TestLibrary;

public class Test121578
{
    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void TestEntryPoint()
    {
        try
        {
            bool internalFinally, externalFinally, internalCatch;

            try
            {
                throw new Exception("foo");
            }
            catch (Exception)
            {
                try
                {
                    try
                    {
                    }
                    finally
                    {
                        // Throw in non-exceptionally called finally
                        throw new Exception();
                    }
                }
                catch(Exception)
                {
                    // Swallow
                }
                finally
                {
                    internalFinally = true;
                }
            }
            finally
            {
                externalFinally = true;
            }

            Assert.True(internalFinally);
            Assert.True(externalFinally);
        }
        catch (Exception ex)
        {
            Assert.Fail("The exception should have been handled. Exception: " + ex.ToString());
        }
    }
}
