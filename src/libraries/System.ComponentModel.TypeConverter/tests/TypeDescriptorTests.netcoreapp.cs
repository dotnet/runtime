// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.ComponentModel.Tests
{
    public partial class TypeDescriptorTests
    {
        [Theory]
        [InlineData(typeof(Version), typeof(VersionConverter))]
        public static void GetConverter_NetCoreApp(Type targetType, Type resultConverterType) =>
            GetConverter(targetType, resultConverterType);
    }
}
