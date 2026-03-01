// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract class UnmappedMemberHandlingTests : SerializerTests
    {
        public UnmappedMemberHandlingTests(JsonSerializerWrapper serializer) : base(serializer)
        { }

        [Theory]
        [MemberData(nameof(SkipHandling_JsonWithoutUnmappedMembers_MemberData))]
        public async Task SkipHandling_JsonWithoutUnmappedMembers_Succeeds(TypeConfiguration typeConfig, JsonInput jsonInput)
        {
            JsonTypeInfo typeInfo = ResolveTypeInfo(typeConfig);

            object result = await Serializer.DeserializeWrapper(jsonInput.json, typeInfo);
            IPoco poco = Assert.IsAssignableFrom<IPoco>(result);
            Assert.Equal(jsonInput.expectedId, poco.Id);
        }

        public static IEnumerable<object[]> SkipHandling_JsonWithoutUnmappedMembers_MemberData()
            => GetAllTestConfigurations()
                .Where(x => x.typeConfig.ExpectedUnmappedMemberHandling is JsonUnmappedMemberHandling.Skip)
                .Where(x => !x.jsonInput.containsUnmappedMember)
                .Select(x => new object[] { x.typeConfig, x.jsonInput });

        [Theory]
        [MemberData(nameof(SkipHandling_JsonWithUnmappedMembers_MemberData))]
        public async Task SkipHandling_JsonWithUnmappedMembers_Succeeds(TypeConfiguration typeConfig, JsonInput jsonInput)
        {
            JsonTypeInfo typeInfo = ResolveTypeInfo(typeConfig);

            object result = await Serializer.DeserializeWrapper(jsonInput.json, typeInfo);

            IPoco poco = Assert.IsAssignableFrom<IPoco>(result);
            Assert.Equal(jsonInput.expectedId, poco.Id);
        }

        public static IEnumerable<object[]> SkipHandling_JsonWithUnmappedMembers_MemberData()
            => GetAllTestConfigurations()
                .Where(x => x.typeConfig.ExpectedUnmappedMemberHandling is JsonUnmappedMemberHandling.Skip)
                .Where(x => x.jsonInput.containsUnmappedMember)
                .Select(x => new object[] { x.typeConfig, x.jsonInput });

        [Theory]
        [MemberData(nameof(DisallowHandling_JsonWithoutUnmappedMembers_MemberData))]
        public async Task DisallowHandling_JsonWithoutUnmappedMembers_Succeeds(TypeConfiguration typeConfig, JsonInput jsonInput)
        {
            JsonTypeInfo typeInfo = ResolveTypeInfo(typeConfig);

            object result = await Serializer.DeserializeWrapper(jsonInput.json, typeInfo);
            IPoco poco = Assert.IsAssignableFrom<IPoco>(result);
            Assert.Equal(jsonInput.expectedId, poco.Id);
        }

        public static IEnumerable<object[]> DisallowHandling_JsonWithoutUnmappedMembers_MemberData()
            => GetAllTestConfigurations()
                .Where(x => x.typeConfig.ExpectedUnmappedMemberHandling is JsonUnmappedMemberHandling.Disallow)
                .Where(x => !x.jsonInput.containsUnmappedMember)
                .Select(x => new object[] { x.typeConfig, x.jsonInput });

        [Theory]
        [MemberData(nameof(DisallowHandling_JsonWithUnmappedMembers_MemberData))]
        public async Task DisallowHandling_JsonWithUnmappedMembers_ThrowsJsonException(TypeConfiguration typeConfig, JsonInput jsonInput)
        {
            JsonTypeInfo typeInfo = ResolveTypeInfo(typeConfig);
            await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper(jsonInput.json, typeInfo));
        }

        public static IEnumerable<object[]> DisallowHandling_JsonWithUnmappedMembers_MemberData()
            => GetAllTestConfigurations()
                .Where(x => x.typeConfig.ExpectedUnmappedMemberHandling is JsonUnmappedMemberHandling.Disallow)
                .Where(x => x.jsonInput.containsUnmappedMember)
                .Select(x => new object[] { x.typeConfig, x.jsonInput });

        [Theory]
        [MemberData(nameof(JsonTypeInfo_ReturnsExpectedUnmappedMemberHandling_MemberData))]
        public void JsonTypeInfo_ReturnsExpectedUnmappedMemberHandling(TypeConfiguration typeConfig)
        {
            JsonTypeInfo typeInfo = ResolveTypeInfo(typeConfig);

            Assert.Equal(typeConfig.contractCustomizationOverride ?? typeConfig.attributeAnnotation, typeInfo.UnmappedMemberHandling);
        }

        public static IEnumerable<object[]> JsonTypeInfo_ReturnsExpectedUnmappedMemberHandling_MemberData()
            => GetTypeConfigurations().Select(x  => new object[] { x });

        private JsonTypeInfo ResolveTypeInfo(TypeConfiguration typeConfig)
        {
            var options = new JsonSerializerOptions(Serializer.DefaultOptions) { UnmappedMemberHandling = typeConfig.globalHandling };
            JsonTypeInfo typeInfo = options.GetTypeInfo(typeConfig.type);

            if (typeConfig.contractCustomizationOverride != null)
            {
                typeInfo.UnmappedMemberHandling = typeConfig.contractCustomizationOverride.Value;
            }

            return typeInfo;
        }

        #region Test Case Generators

        public record struct TypeConfiguration(
            Type type,
            JsonUnmappedMemberHandling globalHandling,
            JsonUnmappedMemberHandling? attributeAnnotation,
            JsonUnmappedMemberHandling? contractCustomizationOverride)
        {
            public JsonUnmappedMemberHandling ExpectedUnmappedMemberHandling => contractCustomizationOverride ?? attributeAnnotation ?? globalHandling;
        }

        public record struct JsonInput(string json, bool containsUnmappedMember = false, int expectedId = 0);

        private static IEnumerable<(TypeConfiguration typeConfig, JsonInput jsonInput)> GetAllTestConfigurations()
            => GetTypeConfigurations().CrossJoin(GetJsonInputs());

        private static IEnumerable<TypeConfiguration> GetTypeConfigurations()
            => GetTypesAndAttributeAnnotations().CrossJoin(
                    GetGlobalUnmappedMemberConfigurations(),
                    GetContractCustomizationOverrides(),
                    static (tc, globalConfig, contractOverride) => new TypeConfiguration(tc.type, globalConfig, tc.attributeAnnotation, contractOverride));

        private static IEnumerable<JsonUnmappedMemberHandling> GetGlobalUnmappedMemberConfigurations()
            => new[] { JsonUnmappedMemberHandling.Skip, JsonUnmappedMemberHandling.Disallow };

        private static IEnumerable<JsonUnmappedMemberHandling?> GetContractCustomizationOverrides()
            => new JsonUnmappedMemberHandling?[] { null, JsonUnmappedMemberHandling.Skip, JsonUnmappedMemberHandling.Disallow };

        private static IEnumerable<(Type type, JsonUnmappedMemberHandling? attributeAnnotation)> GetTypesAndAttributeAnnotations()
        {
            yield return (typeof(PocoWithoutAnnotations), null);
            yield return (typeof(PocoWithSkipAnnotation), JsonUnmappedMemberHandling.Skip);
            yield return (typeof(PocoWithDisallowAnnotation), JsonUnmappedMemberHandling.Disallow);
            yield return (typeof(PocoInheritingDisallowAnnotation), null);
        }

        private static IEnumerable<JsonInput> GetJsonInputs()
        {
            yield return new("""{}""");
            yield return new("""{"Id": 42}""", expectedId: 42);
            yield return new("""{"UnmappedProperty" : null}""", containsUnmappedMember: true);
            yield return new("""{"Id": 42, "UnmappedProperty" : null}""", containsUnmappedMember: true, expectedId: 42);
            yield return new("""{"UnmappedMember" : null, "Id": 42}""", containsUnmappedMember: true, expectedId: 42);
        }

        public interface IPoco
        {
            int Id { get; set; }
        }

        public class PocoWithoutAnnotations : IPoco
        {
            public int Id { get; set; }
        }

        [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Skip)]
        public class PocoWithSkipAnnotation : IPoco
        {
            public int Id { get; set; }
        }

        [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
        public class PocoWithDisallowAnnotation : IPoco
        {
            public int Id { get; set; }
        }

        public class PocoInheritingDisallowAnnotation : PocoWithDisallowAnnotation
        {
        }
        #endregion

        #region JsonExtensionData Interop

        [Fact]
        public async Task ClassWithExtensionData_GlobalDisallowHandling_ClassConfigurationOverridesGlobalSetting()
        {
            var options = new JsonSerializerOptions { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
            ClassWithExtensionData result = await Serializer.DeserializeWrapper<ClassWithExtensionData>("""{"unmappedMember":null}""", options);

            Assert.NotNull(result.ExtensionData);
            Assert.True(result.ExtensionData.ContainsKey("unmappedMember"));
        }

        [Fact]
        public async Task ClassWithExtensionDataAndDisallowHandling_ThrowsInvalidOperationException()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.DeserializeWrapper<ClassWithExtensionDataAndDisallowHandling>("{}"));
        }

        [Fact]
        public async Task ClassWithExtensionDataAndDisallowHandling_DisableUnmappedMemberHandling_Succeeds()
        {
            JsonTypeInfo<ClassWithExtensionDataAndDisallowHandling> typeInfo = Serializer.GetTypeInfo<ClassWithExtensionDataAndDisallowHandling>(mutable: true);

            typeInfo.UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip;
            ClassWithExtensionDataAndDisallowHandling result = await Serializer.DeserializeWrapper("""{"ExtensionData":{}}""", typeInfo);

            Assert.NotNull(result.ExtensionData);
        }

        public class ClassWithExtensionData
        {
            [JsonExtensionData]
            public Dictionary<string, object> ExtensionData { get; set; }
        }

        [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
        public class ClassWithExtensionDataAndDisallowHandling
        {
            [JsonExtensionData]
            public Dictionary<string, object> ExtensionData { get; set; }
        }
        #endregion
    }
}
