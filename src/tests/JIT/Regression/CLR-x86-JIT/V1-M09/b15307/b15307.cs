// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;

namespace DefaultNamespace
{
    internal class bug
    {
        public static int Main(String[] args)
        {
            CultureInfo ci = new CultureInfo("en-us");
            return 100;
        }
    }
}
