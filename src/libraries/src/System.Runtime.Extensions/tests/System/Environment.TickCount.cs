// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;

namespace System.Runtime.Extensions.Tests
{
    public class EnvironmentTickCount
    {
        [Fact]
        public void TickCountTest()
        {
            //arrange
            const int milliSeconds = 1000;
            int start = Environment.TickCount;

            //act
            Task.Delay(milliSeconds).Wait();
            int end = Environment.TickCount;

            //assert
            Console.WriteLine("Start - " + start);
            Console.WriteLine("End - " + end);
            Assert.True(end - start >= milliSeconds);
        }
    }
}
