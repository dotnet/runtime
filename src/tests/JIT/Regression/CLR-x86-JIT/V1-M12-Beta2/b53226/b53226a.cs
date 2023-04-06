// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
namespace Test
{
    using System;

    public class App
    {
        private static void Func(TypedReference tr) { }

        [Fact]
        public static int TestEntryPoint()
        {
            bool b = false;
            TypedReference tr = __makeref(b);
            Func(b ? tr : __makeref(b));
            return 100;
        }
    }
}
