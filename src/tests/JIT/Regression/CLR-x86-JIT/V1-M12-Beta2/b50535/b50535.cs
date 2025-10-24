// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
namespace b50535
{
    using System;
    using System.Collections;

    public class App
    {
        [OuterLoop]
        [Fact]
        public static void TestEntryPoint()
        {
            "hello".CompareTo(null);
        }
    }
}
