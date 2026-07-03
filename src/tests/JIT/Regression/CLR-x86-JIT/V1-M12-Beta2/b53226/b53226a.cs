// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
namespace b53226a
{
    using System;

    public class App
    {
        private static void Func(TypedReference tr) { }

        [OuterLoop]
        [Fact]
        public static void TestEntryPoint()
        {
            bool b = false;
            TypedReference tr = __makeref(b);
            Func(b ? tr : __makeref(b));
        }
    }
}
