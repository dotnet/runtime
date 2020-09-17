// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace System.Text.Json.Serialization.Tests
{
   public class EnumConverterWithNamingPolicyTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public EnumConverterWithNamingPolicyTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        public class SnakeCaseNamingPolicy : JsonNamingPolicy
        {
            public override string ConvertName(string name)
            {
                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name));
                }
                var result = new StringBuilder();
                for (var i = 0; i < name.Length; i++)
                {
                    var c = name[i];
                    if (i == 0)
                    {
                        result.Append(char.ToLower(c));
                    }
                    else
                    {
                        if (char.IsUpper(c))
                        {
                            result.Append('_');
                            result.Append(char.ToLower(c));
                        }
                        else
                        {
                            result.Append(c);
                        }
                    }
                }
                return result.ToString();
            }

        }

        [Flags]
        public enum TestType
        {
            None,
            ValueOne,
            ValueTwo,
        }

        public class ObjectWithEnumProperty
        {
            public TestType TestType { get; set; }
        }

        [Fact]
        public void TestEnumCase()
        {
            var namingPolicy = new SnakeCaseNamingPolicy();

            var opts = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = namingPolicy,
                DictionaryKeyPolicy = namingPolicy,
                Converters =
                {
                    new JsonStringEnumConverter(namingPolicy)
                }
            };

            var enumValues = Enum.GetValues(typeof(TestType)).Cast<TestType>();
            foreach (var v in enumValues)
            {
                var sourceObject = new ObjectWithEnumProperty()
                {
                    TestType = v
                };

                var json = JsonSerializer.Serialize(sourceObject, opts);
                _outputHelper.WriteLine(json);
                var deserializedObject = JsonSerializer.Deserialize<ObjectWithEnumProperty>(json, opts);
                Assert.Equal(sourceObject.TestType, deserializedObject.TestType);
            }
        }


        [Fact]
        public void TestFlagsEnumValuesWithNamingPolicy()
        {
            var namingPolicy = new SnakeCaseNamingPolicy();

            var opts = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = namingPolicy,
                DictionaryKeyPolicy = namingPolicy,
                Converters =
                {
                    new JsonStringEnumConverter(namingPolicy)
                }
            };

            var sourceObject = new ObjectWithEnumProperty()
            {
                TestType = TestType.ValueOne | TestType.ValueTwo
            };

            var json = JsonSerializer.Serialize(sourceObject, opts);
            _outputHelper.WriteLine(json);

            Assert.Equal(@"{""test_type"":""value_one, value_two""}", json);

            var restoredObject = JsonSerializer.Deserialize<ObjectWithEnumProperty>(json, opts);

            Assert.Equal(sourceObject.TestType, restoredObject.TestType);
        }
    }
}
