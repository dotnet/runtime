// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Test.ModuleCore;
using Xunit;

namespace System.Xml.Linq.Tests
{
    public class XAttributeTests
    {
        [Fact]
        public void FormattedDate()
        {
            // Ensure we are compatible with the full framework
            Assert.Equal("CreatedTime=\"2018-01-01T12:13:14Z\"", new XAttribute("CreatedTime", new DateTime(2018, 1, 1, 12, 13, 14, DateTimeKind.Utc)).ToString());
        }
    }
}
