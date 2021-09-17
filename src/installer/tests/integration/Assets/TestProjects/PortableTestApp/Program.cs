// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace PortableTestApp
{
    public class Program
    {
        [Fact]
        public void PassingTest()
        {
            Console.WriteLine("Pass!");
            return;
        }

        [Fact]
        public void FailingTest()
        {
            throw new Exception("Fail!");
        }
    }
}
