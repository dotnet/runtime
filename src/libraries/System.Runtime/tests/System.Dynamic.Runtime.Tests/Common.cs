// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Dynamic.Tests
{
    public static class Helpers
    {
        public static T Cast<T>(dynamic d) => (T)d;
    }
}
