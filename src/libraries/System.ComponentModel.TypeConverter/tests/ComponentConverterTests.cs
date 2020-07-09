// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.ComponentModel.Tests
{
    public class ComponentConverterTests : ReferenceConverterTests
    {
        public override TypeConverter Converter => new ComponentConverter(typeof(int));

        public override bool PropertiesSupported => true;

        [Fact]
        public void GetProperties_Invoke_ReturnsEmpty()
        {
            PropertyDescriptorCollection properties = Converter.GetProperties(1);
            Assert.Empty(properties);
        }
    }
}
