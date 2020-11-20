// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Xunit;

namespace DebuggerTests
{
    static class AssertHelpers
    {
        public static async Task ThrowsAsync<T>(Func<Task> testCode, Action<Exception> testException)
            where T: Exception
        {
            try
            {
                await testCode();
                Assert.True(false, $"Expected an exception of type {typeof(T)} to be thrown");
            }
            catch (Exception exception)
            {
                Assert.Equal(typeof(T), exception.GetType());
                testException(exception);
            }
        }
    }
}
