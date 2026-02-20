// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public sealed partial class PropertyNameTestsDynamic : PropertyNameTests
    {
        public PropertyNameTestsDynamic() : base(JsonSerializerWrapper.StringSerializer) { }

        [Fact]
        public async Task JsonNullNameAttribute()
        {
            var options = new JsonSerializerOptions();
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.PropertyNameCaseInsensitive = true;

            // A null name in JsonPropertyNameAttribute is not allowed.
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.SerializeWrapper(new NullPropertyName_TestClass(), options));
        }

        [Fact]
        public async Task JsonNameConflictOnCaseInsensitiveFail()
        {
            string json = @"{""myInt"":1,""MyInt"":2}";

            {
                var options = new JsonSerializerOptions();
                options.PropertyNameCaseInsensitive = true;

                await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper<IntPropertyNamesDifferentByCaseOnly_TestClass>(json, options));
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.SerializeWrapper(new IntPropertyNamesDifferentByCaseOnly_TestClass(), options));
            }
        }

        [Fact]
        public void CamelCasePolicyToleratesNullOrEmpty()
        {
            Assert.Null(JsonNamingPolicy.CamelCase.ConvertName(null));
            Assert.Equal(string.Empty, JsonNamingPolicy.CamelCase.ConvertName(string.Empty));
        }

        [Theory]
        [MemberData(nameof(JsonSeparatorNamingPolicyInstances))]
        public void InboxSeparatorNamingPolicies_ThrowsOnNullInput(JsonNamingPolicy policy)
        {
            Assert.Throws<ArgumentNullException>(() => policy.ConvertName(null));
        }

        [Theory]
        [MemberData(nameof(JsonSeparatorNamingPolicyInstances))]
        public void InboxSeparatorNamingPolicies_EmptyInput(JsonNamingPolicy policy)
        {
            Assert.Equal(string.Empty, policy.ConvertName(string.Empty));
        }

        public static IEnumerable<object[]> JsonSeparatorNamingPolicyInstances()
        {
            yield return new object[] { JsonNamingPolicy.SnakeCaseLower };
            yield return new object[] { JsonNamingPolicy.SnakeCaseUpper };
            yield return new object[] { JsonNamingPolicy.KebabCaseLower };
            yield return new object[] { JsonNamingPolicy.KebabCaseUpper };
        }

        [Fact]
        public async Task JsonNamingPolicyAttribute_CustomDerived_TypeLevel_AppliesCustomPolicy()
        {
            string json = await Serializer.SerializeWrapper(new ClassWithCustomDerivedNamingPolicyAttribute { MyValue = "test" });
            Assert.Contains(@"""MYVALUE"":""test""", json);
        }

        [Fact]
        public async Task JsonNamingPolicyAttribute_CustomDerived_TypeLevel_Deserialize()
        {
            var obj = await Serializer.DeserializeWrapper<ClassWithCustomDerivedNamingPolicyAttribute>(@"{""MYVALUE"":""test""}");
            Assert.Equal("test", obj.MyValue);
        }

        [Fact]
        public async Task JsonNamingPolicyAttribute_CustomDerived_MemberLevel_AppliesCustomPolicy()
        {
            string json = await Serializer.SerializeWrapper(new ClassWithCustomDerivedMemberNamingPolicyAttribute { MyValue = "test" });
            Assert.Contains(@"""MYVALUE"":""test""", json);
        }

        [Fact]
        public async Task JsonNamingPolicyAttribute_CustomDerived_MemberLevel_Deserialize()
        {
            var obj = await Serializer.DeserializeWrapper<ClassWithCustomDerivedMemberNamingPolicyAttribute>(@"{""MYVALUE"":""test""}");
            Assert.Equal("test", obj.MyValue);
        }

        [Fact]
        public async Task JsonNamingPolicyAttribute_CustomDerived_TypeLevel_OverridesGlobalPolicy()
        {
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            string json = await Serializer.SerializeWrapper(new ClassWithCustomDerivedNamingPolicyAttribute { MyValue = "test" }, options);
            Assert.Contains(@"""MYVALUE"":""test""", json);
        }
    }
}
