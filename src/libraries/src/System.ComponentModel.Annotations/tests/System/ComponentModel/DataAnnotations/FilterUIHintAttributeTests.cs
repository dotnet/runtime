// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class FilterUIHintAttributeTests
    {
#pragma warning disable 618 // FilterUIHintAttribute is marked as Obsolete
        public static IEnumerable<object[]> Ctor_TestData()
        {
            yield return new object[] { "abc", null, new object[0], new Dictionary<string, object>() };
            yield return new object[] { null, "abc", new object[0], new Dictionary<string, object>() };
            yield return new object[] { "abc", "def", new object[0], new Dictionary<string, object>() };
            yield return new object[] { "abc", "def", new object[] { "abc", 5 }, new Dictionary<string, object>() { { "abc", 5 } } };
            yield return new object[] { "abc", "def", new object[] { "abc", 5, "", 6 }, new Dictionary<string, object>() { { "abc", 5 }, { "", 6 } } };
            yield return new object[] { "abc", "def", null, new Dictionary<string, object>() };
        }

        [Theory]
        [MemberData(nameof(Ctor_TestData))]
        public void Ctor(string filterUIHint, string presentationLayer, object[] controlParameters, IDictionary<string, object> expectedControlParameters)
        {
            if (controlParameters == null || controlParameters.Length == 0)
            {
                if (presentationLayer == null)
                {
                    // Use FilterUIHintAttribute(string)
                    FilterUIHintAttribute attribute1 = new FilterUIHintAttribute(filterUIHint);
                    VerifyAttribute(attribute1, filterUIHint, presentationLayer, expectedControlParameters);
                }
                // Use FilterUIHintAttribute(string, string)
                FilterUIHintAttribute attribute2 = new FilterUIHintAttribute(filterUIHint, presentationLayer);
                VerifyAttribute(attribute2, filterUIHint, presentationLayer, expectedControlParameters);
            }
            // Use FilterUIHintAttribute(string, string, object[])
            FilterUIHintAttribute attribute3 = new FilterUIHintAttribute(filterUIHint, presentationLayer, controlParameters);
            VerifyAttribute(attribute3, filterUIHint, presentationLayer, expectedControlParameters);
        }

        private static void VerifyAttribute(FilterUIHintAttribute attribute, string filterUIHint, string presentationLayer, IDictionary<string, object> controlParameters)
        {
            Assert.Equal(filterUIHint, attribute.FilterUIHint);
            Assert.Equal(presentationLayer, attribute.PresentationLayer);
            Assert.Equal(controlParameters, attribute.ControlParameters);

            // ControlParameters is cached
            Assert.Same(attribute.ControlParameters, attribute.ControlParameters);
        }

        [Theory]
        [InlineData(new object[] { new object[] { 1 } })]
        [InlineData(new object[] { new object[] { null, 1 } })]
        [InlineData(new object[] { new object[] { 1, 1 } })]
        [InlineData(new object[] { new object[] { "abc", 1, "abc", 2 } })]
        public void InvalidControlParameters_Get_ThrowsInvalidOperationException(object[] controlParameters)
        {
            FilterUIHintAttribute attribute = new FilterUIHintAttribute("FilterUIHint", "PresentationLayer", controlParameters);
            Assert.Throws<InvalidOperationException>(() => attribute.ControlParameters);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { new FilterUIHintAttribute("abc", "def", new object[] { "1", 2 }), new FilterUIHintAttribute("abc", "def", new object[] { "1", 2 }), true };
            yield return new object[] { new FilterUIHintAttribute(null, null, null), new FilterUIHintAttribute(null, null, null), true };
            yield return new object[] { new FilterUIHintAttribute("abc", "def", new object[0]), new FilterUIHintAttribute("xyz", "def", new object[0]), false };
            yield return new object[] { new FilterUIHintAttribute("abc", "def", new object[0]), new FilterUIHintAttribute("abc", "xyz", new object[0]), false };
            yield return new object[] { new FilterUIHintAttribute("abc", "def", new object[0]), new FilterUIHintAttribute("abc", "def", new object[] { "1", 2 }), false };
            yield return new object[] { new FilterUIHintAttribute("abc", "def", new object[] { "1", 2 }), new FilterUIHintAttribute("abc", "def", new object[] { "1" }), false };
            yield return new object[] { new FilterUIHintAttribute("abc", "def", new object[] { "1", 2 }), new FilterUIHintAttribute("abc", "def", new object[] { "1", 2, "3", 4 }), false };

            yield return new object[] { new FilterUIHintAttribute("abc", "def", new object[0]), new object(), false };
            yield return new object[] { new FilterUIHintAttribute("abc", "def", new object[0]), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals(FilterUIHintAttribute attribute, object obj, bool expected)
        {
            Assert.Equal(expected, attribute.Equals(obj));
            Assert.Equal(attribute.GetHashCode(), attribute.GetHashCode());
        }
#pragma warning restore 618
    }
}
