// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using Xunit;

namespace DefaultNamespace
{
    public class bug
    {
        [Fact]
        public static int TestEntryPoint()
        {
            CultureInfo ci = new CultureInfo("en-us");
            return 100;
        }
    }
}
