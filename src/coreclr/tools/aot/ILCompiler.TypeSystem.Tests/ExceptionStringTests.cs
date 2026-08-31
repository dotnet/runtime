// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.TypeSystem;

using Xunit;

namespace TypeSystemTests
{
    public class ExceptionStringTests
    {
        public ExceptionStringTests()
        {
        }

        [Fact]
        public void TestAllExceptionIdsHaveMessages()
        {
            foreach(var exceptionId in (ExceptionStringID[])Enum.GetValues(typeof(ExceptionStringID)))
            {
                Assert.NotNull(TypeSystemException.GetFormatString(exceptionId));
            }
        }
    }
}
