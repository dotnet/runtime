// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Tests;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class MultilineStringConverterTests : ConverterTestBase
    {
        [Fact]
        public static void ConvertTo_WithContext()
        {
            using (new ThreadCultureChange(null, CultureInfo.InvariantCulture))
            {
                ConvertTo_WithContext(new object[1, 3]
                    {
                        { "any string", "(Text)", null }
                    },
                    new MultilineStringConverter());
            }
        }
    }
}
