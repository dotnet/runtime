// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Xunit;

namespace System.Timers.Tests
{
    public class TimersDescriptionAttributeTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("description")]
        public void Ctor_String(string description)
        {
            var attribute = new TimersDescriptionAttribute(description);
            if (PlatformDetection.IsFullFramework)
            {
                Assert.Null(attribute.Description);
            }
            else
            {
                Assert.Equal(description, attribute.Description);
                Assert.Same(attribute.Description, attribute.Description);
            }
        }

        [Fact]
        public void Ctor_String_String()
        {
            PropertyInfo autoResetProperty = typeof(Timer).GetProperty(nameof(Timer.AutoReset));
            TimersDescriptionAttribute attribute = autoResetProperty.GetCustomAttribute<TimersDescriptionAttribute>();
            Assert.NotEmpty(attribute.Description);
            Assert.Same(attribute.Description, attribute.Description);
        }

        [Fact]
        public void Description_GetWithNullDescription_ThrowsArgumentNullException()
        {
            var attribute = new TimersDescriptionAttribute(null);
            AssertExtensions.Throws<ArgumentNullException>("format", "name", () => attribute.Description);

            // Only the first call fails.
            Assert.Null(attribute.Description);
        }
    }
}
