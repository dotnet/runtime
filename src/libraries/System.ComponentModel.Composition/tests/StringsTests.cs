// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Reflection;
using Microsoft.VisualBasic;
using Xunit;

namespace System
{
    public class StringsTests
    {
        [Fact]
        [ActiveIssue(25498)]
        public void PropertiesAreInsyncWithResources()
        {
            var properties = GetStringProperties();

            Assert.True(properties.Length > 0, "Expected to find at least one string property in Strings.cs.");

            Assert.All(properties, property =>
            {
                object value = property.GetValue(null, (object[])null);
                Assert.NotNull(value);
            });
        }

        private static PropertyInfo[] GetStringProperties()
        {
            PropertyInfo[] properties = typeof(Strings).GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

            return properties.Where(property => 
            {
                return !CanIgnore(property);

            }).ToArray();
        }

        private static bool CanIgnore(PropertyInfo property)
        {
            switch (property.Name)
            {
                case "Culture":
                case "ResourceManager":
                    return true;
            }

            return false;
        }
    }
}
