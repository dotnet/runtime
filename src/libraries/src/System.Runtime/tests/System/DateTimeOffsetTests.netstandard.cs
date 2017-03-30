// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Xunit;

namespace System.Tests
{
    public static partial class DateTimeOffsetTests
    {
        [Fact]
        public static void Ctor_Calendar_TimeSpan()
        {
            var dateTimeOffset = new DateTimeOffset(1, 1, 1, 0, 0, 0, 0, new GregorianCalendar(),TimeSpan.Zero);
            VerifyDateTimeOffset(dateTimeOffset, 1, 1, 1, 0, 0, 0, 0, TimeSpan.Zero);
        }
    }
}
