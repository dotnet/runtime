// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHosting
{
    public partial class HostContext : IClassFixture<HostContext.SharedTestState>
    {
        public class PropertyTestData : IXunitSerializable
        {
            public string Name;
            public string NewValue;
            public string ExistingValue;

            void IXunitSerializable.Deserialize(IXunitSerializationInfo info)
            {
                Name = info.GetValue<string>("Name");
                NewValue = info.GetValue<string>("NewValue");
                ExistingValue = info.GetValue<string>("ExistingValue");
            }

            void IXunitSerializable.Serialize(IXunitSerializationInfo info)
            {
                info.AddValue("Name", Name);
                info.AddValue("NewValue", NewValue);
                info.AddValue("ExistingValue", ExistingValue);
            }

            public override string ToString()
            {
                return $"Name: {Name}, NewValue: {NewValue}, ExistingValue: {ExistingValue}";
            }
        }

        private static List<PropertyTestData[]> GetPropertiesTestData(
            string propertyName1,
            string propertyValue1,
            string propertyName2,
            string propertyValue2)
        {
            var list = new List<PropertyTestData[]>()
            {
                // No additional properties
                new PropertyTestData[] { },
                // Match
                new PropertyTestData[]
                {
                    new PropertyTestData { Name = propertyName1, NewValue = propertyValue1, ExistingValue = propertyValue1 }
                },
                // Substring
                new  PropertyTestData[]
                {
                    new PropertyTestData { Name = propertyName1, NewValue = propertyValue1.Remove(propertyValue1.Length - 1), ExistingValue = propertyValue1 }
                },
                // Different in case only
                new PropertyTestData[]
                {
                    new PropertyTestData { Name = propertyName1, NewValue = propertyValue1.ToLower(), ExistingValue = propertyValue1 }
                },
                // Different value
                new PropertyTestData[]
                {
                    new PropertyTestData { Name = propertyName1, NewValue = "NEW_PROPERTY_VALUE", ExistingValue = propertyValue1 }
                },
                // Different value (empty)
                new PropertyTestData[]
                {
                    new PropertyTestData { Name = propertyName1, NewValue = string.Empty, ExistingValue = propertyValue1 }
                },
                // New property
                new PropertyTestData[]
                {
                    new PropertyTestData { Name = "NEW_PROPERTY_NAME", NewValue = "NEW_PROPERTY_VALUE", ExistingValue = null }
                },
                // Match, new property
                new PropertyTestData[]
                {
                    new PropertyTestData { Name = propertyName1, NewValue = propertyValue1, ExistingValue = propertyValue1 },
                    new PropertyTestData { Name = "NEW_PROPERTY_NAME", NewValue = "NEW_PROPERTY_VALUE", ExistingValue = null }
                },
                // One match, one different
                new PropertyTestData[]
                {
                    new PropertyTestData { Name = propertyName1, NewValue = propertyValue1, ExistingValue = propertyValue1 },
                    new PropertyTestData { Name = propertyName2, NewValue = "NEW_PROPERTY_VALUE", ExistingValue = propertyValue2 }
                },
                // Both different
                new PropertyTestData[]
                {
                    new PropertyTestData { Name = propertyName1, NewValue = "NEW_PROPERTY_VALUE", ExistingValue = propertyValue1 },
                    new PropertyTestData { Name = propertyName2, NewValue = "NEW_PROPERTY_VALUE", ExistingValue = propertyValue2 }
                },
            };

            if (propertyValue2 != null)
            {
                list.Add(
                    // Both match
                    new PropertyTestData[]
                    {
                        new PropertyTestData { Name = propertyName1, NewValue = propertyValue1, ExistingValue = propertyValue1 },
                        new PropertyTestData { Name = propertyName2, NewValue = propertyValue2, ExistingValue = propertyValue2 }
                    });
                list.Add(
                    // Both match, new property
                    new PropertyTestData[]
                    {
                        new PropertyTestData { Name = propertyName1, NewValue = propertyValue1, ExistingValue = propertyValue1 },
                        new PropertyTestData { Name = propertyName2, NewValue = propertyValue2, ExistingValue = propertyValue2 },
                        new PropertyTestData { Name = "NEW_PROPERTY_NAME", NewValue = "NEW_PROPERTY_VALUE", ExistingValue = null }
                    });
            }

            return list;
        }

        public static IEnumerable<object[]> GetPropertyCompatibilityTestData(string scenario, bool hasSecondProperty)
        {
            List<PropertyTestData[]> properties;
            switch (scenario)
            {
                case Scenario.ConfigMultiple:
                    properties = GetPropertiesTestData(
                        SharedTestState.ConfigPropertyName,
                        SharedTestState.ConfigPropertyValue,
                        SharedTestState.ConfigMultiPropertyName,
                        hasSecondProperty ? SharedTestState.ConfigMultiPropertyValue : null);
                    break;
                case Scenario.Mixed:
                case Scenario.NonContextMixed:
                    properties = GetPropertiesTestData(
                        SharedTestState.AppPropertyName,
                        SharedTestState.AppPropertyValue,
                        SharedTestState.AppMultiPropertyName,
                        hasSecondProperty ? SharedTestState.AppMultiPropertyValue : null);
                    break;
                default:
                    throw new Exception($"Unexpected scenario: {scenario}");
            }

            var list = new List<object[]> ();
            foreach (var p in properties)
            {
                list.Add(new object[] { scenario, hasSecondProperty, p });
            }

            return list;
        }
    }
}
