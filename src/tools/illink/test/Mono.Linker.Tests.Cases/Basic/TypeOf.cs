// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
    public class TypeOf
    {
        public static void Main()
        {
            var t = typeof(TestType);
        }

        [Kept]
        class TestType
        {
        }
    }
}
