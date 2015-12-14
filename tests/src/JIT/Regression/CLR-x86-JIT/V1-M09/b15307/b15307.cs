// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
