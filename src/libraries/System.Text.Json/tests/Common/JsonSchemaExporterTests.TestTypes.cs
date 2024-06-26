// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json.Serialization.Tests;

namespace System.Text.Json.Schema.Tests
{
    public abstract partial class JsonSchemaExporterTests : SerializerTests
    {
        public static IEnumerable<object[]> GetTestData() =>
            from testCase in GetTestDataCore()
            select new object[] { testCase };

        public static IEnumerable<object[]> GetTestDataUsingAllValues() =>
            from testCase in GetTestDataCore()
            from expandedTestCase in testCase.GetTestDataForAllValues()
            select new object[] { expandedTestCase };

        public static IEnumerable<ITestData> GetTestDataCore()
        {
            // Primitives and built-in types
            yield return new TestData<object>(
                Value: new(),
                AdditionalValues: [null, 42, false, 3.14, 3.14M, new int[] { 1, 2, 3 }, new SimpleRecord(1, "str", false, 3.14)],
                ExpectedJsonSchema: "true");

            yield return new TestData<bool>(true, ExpectedJsonSchema: """{"type":"boolean"}""");
            yield return new TestData<byte>(42, ExpectedJsonSchema: """{"type":"integer"}""");
            yield return new TestData<ushort>(42, ExpectedJsonSchema: """{"type":"integer"}""");
            yield return new TestData<uint>(42, ExpectedJsonSchema: """{"type":"integer"}""");
            yield return new TestData<ulong>(42, ExpectedJsonSchema: """{"type":"integer"}""");
            yield return new TestData<sbyte>(42, ExpectedJsonSchema: """{"type":"integer"}""");
            yield return new TestData<short>(42, ExpectedJsonSchema: """{"type":"integer"}""");
            yield return new TestData<int>(42, ExpectedJsonSchema: """{"type":"integer"}""");
            yield return new TestData<long>(42, ExpectedJsonSchema: """{"type":"integer"}""");
            yield return new TestData<float>(1.2f, ExpectedJsonSchema: """{"type":"number"}""");
            yield return new TestData<double>(3.14159d, ExpectedJsonSchema: """{"type":"number"}""");
            yield return new TestData<decimal>(3.14159M, ExpectedJsonSchema: """{"type":"number"}""");
#if NET
            yield return new TestData<UInt128>(42, ExpectedJsonSchema: """{"type":"integer"}""");
            yield return new TestData<Int128>(42, ExpectedJsonSchema: """{"type":"integer"}""");
            yield return new TestData<Half>((Half)3.141, ExpectedJsonSchema: """{"type":"number"}""");
#endif
            yield return new TestData<string>("I am a string", ExpectedJsonSchema: """{"type":["string","null"]}""");
            yield return new TestData<char>('c', ExpectedJsonSchema: """{"type":"string", "minLength":1, "maxLength":1 }""");
            yield return new TestData<byte[]>(
                Value: [1, 2, 3],
                AdditionalValues: [[]],
                ExpectedJsonSchema: """{"type":["string","null"]}""");

            yield return new TestData<Memory<byte>>(new byte[] { 1, 2, 3 }, ExpectedJsonSchema: """{"type":"string"}""");
            yield return new TestData<ReadOnlyMemory<byte>>(new byte[] { 1, 2, 3 }, ExpectedJsonSchema: """{"type":"string"}""");
            yield return new TestData<DateTime>(
                Value: new(2024, 06, 06, 21, 39, 42, DateTimeKind.Utc),
                ExpectedJsonSchema: """{"type":"string","format":"date-time"}""");

            yield return new TestData<DateTimeOffset>(
                Value: new(new DateTime(2021, 1, 1), TimeSpan.Zero),
                AdditionalValues: [DateTimeOffset.MinValue, DateTimeOffset.MaxValue],
                ExpectedJsonSchema: """{"type":"string","format": "date-time"}""");

            yield return new TestData<TimeSpan>(
                Value: new(hours: 5, minutes: 13, seconds: 3),
                AdditionalValues: [TimeSpan.MinValue, TimeSpan.MaxValue],
                ExpectedJsonSchema: """{"$comment": "Represents a System.TimeSpan value.", "type":"string", "pattern": "^-?(\\d+\\.)?\\d{2}:\\d{2}:\\d{2}(\\.\\d{1,7})?$"}""");

#if NET
            yield return new TestData<DateOnly>(new(2021, 1, 1), ExpectedJsonSchema: """{"type":"string","format": "date"}""");
            yield return new TestData<TimeOnly>(new(hour: 22, minute: 30, second: 33, millisecond: 100), ExpectedJsonSchema: """{"type":"string","format": "time"}""");
#endif
            yield return new TestData<Guid>(Guid.Empty, ExpectedJsonSchema: """{"type":"string","format":"uuid"}""");
            yield return new TestData<Uri>(new("http://example.com"), """{"type":["string","null"],"format":"uri"}""");
            yield return new TestData<Version>(new(1, 2, 3, 4), ExpectedJsonSchema: """{"$comment": "Represents a version string.", "type":["string","null"],"pattern":"^\\d+(\\.\\d+){1,3}$"}""");
            yield return new TestData<JsonDocument>(JsonDocument.Parse("""[{ "x" : 42 }]"""), ExpectedJsonSchema: "true");
            yield return new TestData<JsonElement>(JsonDocument.Parse("""[{ "x" : 42 }]""").RootElement, ExpectedJsonSchema: "true");
            yield return new TestData<JsonNode>(JsonNode.Parse("""[{ "x" : 42 }]"""), ExpectedJsonSchema: "true");
            yield return new TestData<JsonValue>((JsonValue)42, ExpectedJsonSchema: "true");
            yield return new TestData<JsonObject>(new() { ["x"] = 42 }, ExpectedJsonSchema: """{"type":["object","null"]}""");
            yield return new TestData<JsonArray>([(JsonNode)1, (JsonNode)2, (JsonNode)3], ExpectedJsonSchema: """{"type":["array","null"]}""");

            // Enum types
            yield return new TestData<IntEnum>(IntEnum.A, ExpectedJsonSchema: """{"type":"integer"}""");
            yield return new TestData<StringEnum>(StringEnum.A, ExpectedJsonSchema: """{"enum":["A","B","C"]}""");
            yield return new TestData<FlagsStringEnum>(FlagsStringEnum.A, ExpectedJsonSchema: """{"type":"string"}""");

            // Nullable<T> types
            yield return new TestData<bool?>(true, AdditionalValues: [null], ExpectedJsonSchema: """{"type":["boolean","null"]}""");
            yield return new TestData<int?>(42, AdditionalValues: [null], ExpectedJsonSchema: """{"type":["integer","null"]}""");
            yield return new TestData<double?>(3.14, AdditionalValues: [null], ExpectedJsonSchema: """{"type":["number","null"]}""");
            yield return new TestData<Guid?>(Guid.Empty, AdditionalValues: [null], ExpectedJsonSchema: """{"type":["string","null"],"format":"uuid"}""");
            yield return new TestData<JsonElement?>(JsonDocument.Parse("{}").RootElement, AdditionalValues: [null], ExpectedJsonSchema: "true");
            yield return new TestData<IntEnum?>(IntEnum.A, AdditionalValues: [null], ExpectedJsonSchema: """{"type":["integer","null"]}""");
            yield return new TestData<StringEnum?>(StringEnum.A, AdditionalValues: [null], ExpectedJsonSchema: """{"enum":["A","B","C",null]}""");
            yield return new TestData<SimpleRecordStruct?>(
                new(1, "two", true, 3.14),
                AdditionalValues: [null],
                ExpectedJsonSchema: """
                {
                    "type":["object","null"],
                    "properties": {
                        "X": {"type":"integer"},
                        "Y": {"type":"string"},
                        "Z": {"type":"boolean"},
                        "W": {"type":"number"}
                    }
                }
                """);

            // User-defined POCOs
            yield return new TestData<SimplePoco>(
                Value: new() { String = "string", StringNullable = "string", Int = 42, Double = 3.14, Boolean = true },
                ExpectedJsonSchema: """
                {
                    "type": ["object","null"],
                    "properties": {
                        "String": { "type": "string" },
                        "StringNullable": { "type": ["string", "null"] },
                        "Int": { "type": "integer" },
                        "Double": { "type": "number" },
                        "Boolean": { "type": "boolean" }
                    }
                }
                """);

            yield return new TestData<SimpleRecord>(
                Value: new(1, "two", true, 3.14),
                ExpectedJsonSchema: """
                {
                  "type": ["object","null"],
                  "properties": {
                    "X": { "type": "integer" },
                    "Y": { "type": "string" },
                    "Z": { "type": "boolean" },
                    "W": { "type": "number" }
                  },
                  "required": ["X","Y","Z","W"]
                }
                """);

            yield return new TestData<SimpleRecordStruct>(
                Value: new(1, "two", true, 3.14),
                ExpectedJsonSchema: """
                {
                  "type": "object",
                  "properties": {
                    "X": { "type": "integer" },
                    "Y": { "type": "string" },
                    "Z": { "type": "boolean" },
                    "W": { "type": "number" }
                  }
                }
                """);

            yield return new TestData<RecordWithOptionalParameters>(
                Value: new(1, "two", true, 3.14, StringEnum.A),
                ExpectedJsonSchema: """
                {
                  "type": ["object","null"],
                  "properties": {
                    "X1": { "type": "integer" },
                    "X2": { "type": "string" },
                    "X3": { "type": "boolean" },
                    "X4": { "type": "number" },
                    "X5": { "enum": ["A", "B", "C"] },
                    "Y1": { "type": "integer", "default": 42 },
                    "Y2": { "type": "string", "default": "str" },
                    "Y3": { "type": "boolean", "default": true },
                    "Y4": { "type": "number", "default": 0 },
                    "Y5": { "enum": ["A", "B", "C"], "default": "A" }
                  },
                  "required": ["X1", "X2", "X3", "X4", "X5"]
                }
                """);

            yield return new TestData<PocoWithRequiredMembers>(
                new() { X = "str1", Y = "str2", Z = 42 },
                ExpectedJsonSchema: """
                {
                  "type": ["object","null"],
                  "properties": {
                    "Y": { "type": "string" },
                    "Z": { "type": "integer" },
                    "X": { "type": "string" }
                  },
                  "required": [ "Y", "Z", "X" ]
                }
                """);

            yield return new TestData<PocoWithIgnoredMembers>(new() { X = 1, Y = 2 }, ExpectedJsonSchema: """{"type":["object","null"],"properties":{"X":{"type":"integer"}}}""");
            yield return new TestData<PocoWithCustomNaming>(
                Value: new() { IntegerProperty = 1, StringProperty = "str" },
                ExpectedJsonSchema: """
                {
                  "type": ["object","null"],
                  "properties": {
                    "int": { "type": "integer" },
                    "str": { "type": ["string", "null"] }
                  }
                }
                """);

            yield return new TestData<PocoWithCustomNumberHandling>(
                Value: new() { X = 1 },
                ExpectedJsonSchema: """
                {
                    "type": ["object","null"],
                    "properties": { "X": { "type": ["string","integer"], "pattern": "^-?(?:0|[1-9]\\d*)$" } }
                }
                """);

            yield return new TestData<PocoWithCustomNumberHandlingOnProperties>(
                Value: new() {
                    IntegerReadingFromString = 3,
                    DoubleReadingFromString = 3.14,
                    DecimalReadingFromString = 3.14M,
                    IntegerWritingAsString = 3,
                    DoubleWritingAsString = 3.14,
                    DecimalWritingAsString = 3.14M,
                    IntegerAllowingFloatingPointLiterals = 3,
                    DoubleAllowingFloatingPointLiterals = 3.14,
                    DecimalAllowingFloatingPointLiterals = 3.14M,
                    IntegerAllowingFloatingPointLiteralsAndReadingFromString = 3,
                    DoubleAllowingFloatingPointLiteralsAndReadingFromString = 3.14,
                    DecimalAllowingFloatingPointLiteralsAndReadingFromString = 3.14M,
                },
                AdditionalValues: [
                    new() { DoubleAllowingFloatingPointLiterals = double.NaN, },
                    new() { DoubleAllowingFloatingPointLiterals = double.PositiveInfinity },
                    new() { DoubleAllowingFloatingPointLiterals = double.NegativeInfinity },
                ],
                ExpectedJsonSchema: """
                {
                  "type": ["object","null"],
                  "properties": {
                    "IntegerReadingFromString": { "type": ["string","integer"], "pattern": "^-?(?:0|[1-9]\\d*)$" },
                    "DoubleReadingFromString": { "type": ["string","number"], "pattern": "^-?(?:0|[1-9]\\d*)(?:\\.\\d+)?(?:[eE][+-]?\\d+)?$" },
                    "DecimalReadingFromString": { "type": ["string","number"], "pattern": "^-?(?:0|[1-9]\\d*)(?:\\.\\d+)?$" },
                    "IntegerWritingAsString": { "type": ["string","integer"], "pattern": "^-?(?:0|[1-9]\\d*)$" },
                    "DoubleWritingAsString": { "type": ["string","number"], "pattern": "^-?(?:0|[1-9]\\d*)(?:\\.\\d+)?(?:[eE][+-]?\\d+)?$" },
                    "DecimalWritingAsString": { "type": ["string","number"], "pattern": "^-?(?:0|[1-9]\\d*)(?:\\.\\d+)?$" },
                    "IntegerAllowingFloatingPointLiterals": { "type": "integer" },
                    "DoubleAllowingFloatingPointLiterals": {
                        "anyOf": [
                            { "type": "number" },
                            { "enum": ["NaN", "Infinity", "-Infinity"] }
                        ]
                    },
                    "DecimalAllowingFloatingPointLiterals": { "type": "number" },
                    "IntegerAllowingFloatingPointLiteralsAndReadingFromString": { "type": ["string","integer"], "pattern": "^-?(?:0|[1-9]\\d*)$" },
                    "DoubleAllowingFloatingPointLiteralsAndReadingFromString": {
                        "anyOf": [
                            { "type": ["string","number"], "pattern": "^-?(?:0|[1-9]\\d*)(?:\\.\\d+)?(?:[eE][+-]?\\d+)?$" },
                            { "enum": ["NaN", "Infinity", "-Infinity"] }
                        ]
                    },
                    "DecimalAllowingFloatingPointLiteralsAndReadingFromString": { "type": ["string","number"], "pattern": "^-?(?:0|[1-9]\\d*)(?:\\.\\d+)?$" }
                  }
                }
                """);

            yield return new TestData<PocoWithRecursiveMembers>(
                Value: new() { Value = 1, Next = new() { Value = 2, Next = new() { Value = 3 } } },
                AdditionalValues: [new() { Value = 1, Next = null }],
                ExpectedJsonSchema: """
                {
                    "type": ["object","null"],
                    "properties": {
                        "Value": { "type": "integer" },
                        "Next": {
                            "type": ["object", "null"],
                            "properties": {
                                "Value": { "type": "integer" },
                                "Next": { "$ref": "#/properties/Next" }
                            }
                        }
                    }
                }
                """);

            // Same as above with non-nullable reference type handling
            yield return new TestData<PocoWithRecursiveMembers>(
                Value: new() { Value = 1, Next = new() { Value = 2, Next = new() { Value = 3 } } },
                AdditionalValues: [new() { Value = 1, Next = null }],
                ExpectedJsonSchema: """
                {
                    "type": "object",
                    "properties": {
                        "Value": { "type": "integer" },
                        "Next": {
                            "type": ["object", "null"],
                            "properties": {
                                "Value": { "type": "integer" },
                                "Next": { "$ref": "#/properties/Next" }
                            }
                        }
                    }
                }
                """,
                Options: new() { TreatNullObliviousAsNonNullable = true });

            // Same as above but using an anchor-based reference scheme
            yield return new TestData<PocoWithRecursiveMembers>(
                Value: new() { Value = 1, Next = new() { Value = 2, Next = new() { Value = 3 } } },
                AdditionalValues: [new() { Value = 1, Next = null }],
                ExpectedJsonSchema: """
                {
                    "$anchor" : "PocoWithRecursiveMembers",
                    "type": ["object","null"],
                    "properties": {
                        "Value": { "type": "integer" },
                        "Next": {
                            "$anchor" : "PocoWithRecursiveMembers_Next",
                            "type": ["object", "null"],
                            "properties": {
                                "Value": { "type": "integer" },
                                "Next": { "$ref": "#PocoWithRecursiveMembers_Next" }
                            }
                        }
                    }
                }
                """,
                Options: new JsonSchemaExporterOptions
                {
                    TransformSchemaNode = static (ctx, schema) =>
                    {
                        if (ctx.TypeInfo.Kind is JsonTypeInfoKind.None || schema is not JsonObject schemaObj)
                        {
                            return schema;
                        }

                        string anchorName = ctx.PropertyInfo is { } property
                            ? ctx.TypeInfo.Type.Name + "_" + property.Name
                            : ctx.TypeInfo.Type.Name;

                        if (schemaObj.ContainsKey("$ref"))
                        {
                            schemaObj["$ref"] = "#" + anchorName;
                        }
                        else
                        {
                            schemaObj.Insert(0, "$anchor", anchorName);
                        }

                        return schemaObj;
                    }
                });

            // Same as above but using an id-based reference scheme
            yield return new TestData<PocoWithRecursiveMembers>(
                Value: new() { Value = 1, Next = new() { Value = 2, Next = new() { Value = 3 } } },
                AdditionalValues: [new() { Value = 1, Next = null }],
                    ExpectedJsonSchema: """
                    {
                        "$id" : "https://example.com/schema/pocowithrecursivemembers.json",
                        "type": ["object","null"],
                        "properties": {
                            "Value": { "type": "integer" },
                            "Next": {
                                "$id" : "https://example.com/schema/pocowithrecursivemembers/next.json",
                                "type": ["object", "null"],
                                "properties": {
                                    "Value": { "type": "integer" },
                                    "Next": { "$ref": "https://example.com/schema/pocowithrecursivemembers/next.json" }
                                }
                            }
                        }
                    }
                """,
                Options: new JsonSchemaExporterOptions
                {
                    TransformSchemaNode = static (ctx, schema) =>
                    {
                        if (ctx.TypeInfo.Kind is JsonTypeInfoKind.None || schema is not JsonObject schemaObj)
                        {
                            return schema;
                        }

                        string idPath = ctx.PropertyInfo is { } property
                            ? ctx.TypeInfo.Type.Name.ToLower() + "/" + property.Name.ToLower()
                            : ctx.TypeInfo.Type.Name.ToLower();

                        string idUrl = "https://example.com/schema/" + idPath + ".json";

                        if (schemaObj.ContainsKey("$ref"))
                        {
                            schemaObj["$ref"] = idUrl;
                        }
                        else
                        {
                            schemaObj.Insert(0, "$id", idUrl);
                        }

                        return schemaObj;
                    }
                });

            yield return new TestData<PocoWithRecursiveCollectionElement>(
                Value: new() { Children = [new(), new() { Children = [] }] },
                ExpectedJsonSchema: """
                {
                    "type": ["object","null"],
                    "properties": {
                        "Children": {
                            "type": "array",
                            "items": { "$ref" : "#" }
                        }
                    }
                }
                """);

            // Same as above but with non-nullable reference type handling
            yield return new TestData<PocoWithRecursiveCollectionElement>(
                Value: new() { Children = [new(), new() { Children = [] }] },
                ExpectedJsonSchema: """
                {
                    "type": "object",
                    "properties": {
                        "Children": {
                            "type": "array",
                            "items": { "$ref" : "#" }
                        }
                    }
                }
                """,
                Options: new() { TreatNullObliviousAsNonNullable = true });

            yield return new TestData<PocoWithRecursiveDictionaryValue>(
                Value: new() { Children = new() { ["key1"] = new(), ["key2"] = new() { Children = new() { ["key3"] = new() }  } } },
                ExpectedJsonSchema: """
                {
                    "type": ["object","null"],
                    "properties": {
                        "Children": {
                            "type": "object",
                            "additionalProperties": { "$ref" : "#" }
                        }
                    }
                }
                """);

            // Same as above but with non-nullable reference type handling
            yield return new TestData<PocoWithRecursiveDictionaryValue>(
                Value: new() { Children = new() { ["key1"] = new(), ["key2"] = new() { Children = new() { ["key3"] = new() } } } },
                ExpectedJsonSchema: """
                {
                    "type": "object",
                    "properties": {
                        "Children": {
                            "type": "object",
                            "additionalProperties": { "$ref" : "#" }
                        }
                    }
                }
                """,
                Options: new() { TreatNullObliviousAsNonNullable = true });

            yield return new TestData<PocoWithDescription>(
                Value: new() { X = 42 },
                ExpectedJsonSchema: """
                {
                  "description": "The type description",
                  "type": ["object","null"],
                  "properties": {
                    "X": {
                      "description": "The property description",
                      "type": "integer"
                    }
                  }
                }
                """,
                Options: new()
                {
                    TransformSchemaNode = static (ctx, schema) =>
                    {
                        if (schema is not JsonObject schemaObj || schemaObj.ContainsKey("$ref"))
                        {
                            return schema;
                        }

                        DescriptionAttribute? descriptionAttribute =
                            GetCustomAttribute<DescriptionAttribute>(ctx.PropertyInfo?.AttributeProvider) ??
                            GetCustomAttribute<DescriptionAttribute>(ctx.PropertyInfo?.AssociatedParameter.AttributeProvider) ??
                            GetCustomAttribute<DescriptionAttribute>(ctx.TypeInfo.Type);

                        if (descriptionAttribute != null)
                        {
                            schemaObj.Insert(0, "description", (JsonNode)descriptionAttribute.Description);
                        }

                        return schemaObj;
                    }
                });

            yield return new TestData<PocoWithCustomConverter>(new() { Value = 42 }, ExpectedJsonSchema: "true");
            yield return new TestData<PocoWithCustomPropertyConverter>(new() { Value = 42 }, ExpectedJsonSchema: """{"type":["object", "null"],"properties":{"Value":true}}""");
            yield return new TestData<PocoWithEnums>(
                Value: new()
                {
                    IntEnum = IntEnum.A,
                    StringEnum = StringEnum.B,
                    IntEnumUsingStringConverter = IntEnum.A,
                    NullableIntEnumUsingStringConverter = IntEnum.B,
                    StringEnumUsingIntConverter = StringEnum.A,
                    NullableStringEnumUsingIntConverter = StringEnum.B
                },
                AdditionalValues: [
                    new()
                    {
                        IntEnum = (IntEnum)int.MaxValue,
                        StringEnum = StringEnum.A,
                        IntEnumUsingStringConverter = IntEnum.A,
                        NullableIntEnumUsingStringConverter = null,
                        StringEnumUsingIntConverter = (StringEnum)int.MaxValue,
                        NullableStringEnumUsingIntConverter = null
                    },
                ],
                ExpectedJsonSchema: """
                {
                    "type": ["object","null"],
                    "properties": {
                        "IntEnum": { "type": "integer" },
                        "StringEnum": { "enum": [ "A", "B", "C" ] },
                        "IntEnumUsingStringConverter": { "enum": [ "A", "B", "C" ] },
                        "NullableIntEnumUsingStringConverter": { "enum": [ "A", "B", "C", null ] },
                        "StringEnumUsingIntConverter": { "type": "integer" },
                        "NullableStringEnumUsingIntConverter": { "type": [ "integer", "null" ] }
                    }
                }
                """);

            // Same but using a callback to insert a type keyword for string enums.
            yield return new TestData<PocoWithEnums>(
                Value: new()
                {
                    IntEnum = IntEnum.A,
                    StringEnum = StringEnum.B,
                    IntEnumUsingStringConverter = IntEnum.A,
                    NullableIntEnumUsingStringConverter = IntEnum.B,
                    StringEnumUsingIntConverter = StringEnum.A,
                    NullableStringEnumUsingIntConverter = StringEnum.B
                },
                ExpectedJsonSchema: """
                {
                    "type": ["object","null"],
                    "properties": {
                        "IntEnum": { "type": "integer" },
                        "StringEnum": { "type" : "string", "enum": [ "A", "B", "C" ] },
                        "IntEnumUsingStringConverter": { "type" : "string", "enum": [ "A", "B", "C" ] },
                        "NullableIntEnumUsingStringConverter": { "type" : ["string","null"], "enum": [ "A", "B", "C", null ] },
                        "StringEnumUsingIntConverter": { "type": "integer" },
                        "NullableStringEnumUsingIntConverter": { "type": [ "integer", "null" ] }
                    }
                }
                """,
                Options: new()
                {
                    TransformSchemaNode = static (ctx, schema) =>
                    {
                        if (schema is not JsonObject schemaObj)
                        {
                            return schema;
                        }

                        Type type = ctx.TypeInfo.Type;

                        if (schemaObj.ContainsKey("enum"))
                        {
                            if (ctx.TypeInfo.Type.IsEnum)
                            {
                                schemaObj.Add("type", "string");
                            }
                            else if (Nullable.GetUnderlyingType(type)?.IsEnum is true)
                            {
                                schemaObj.Add("type", new JsonArray { (JsonNode)"string", (JsonNode)"null" });
                            }
                        }

                        return schemaObj;
                    }
                });

            var recordStruct = new SimpleRecordStruct(42, "str", true, 3.14);
            yield return new TestData<PocoWithStructFollowedByNullableStruct>(
                Value: new() { Struct = recordStruct, NullableStruct = null },
                AdditionalValues: [new() { Struct = recordStruct, NullableStruct = recordStruct }],
                ExpectedJsonSchema: """
                {
                    "type": ["object","null"],
                    "properties": {
                        "Struct": {
                            "type": "object",
                            "properties": {
                                "X": {"type":"integer"},
                                "Y": {"type":"string"},
                                "Z": {"type":"boolean"},
                                "W": {"type":"number"}
                            }
                        },
                        "NullableStruct": {
                            "type": ["object","null"],
                            "properties": {
                                "X": {"type":"integer"},
                                "Y": {"type":"string"},
                                "Z": {"type":"boolean"},
                                "W": {"type":"number"}
                            }
                        }
                    }
                }
                """);

            yield return new TestData<PocoWithNullableStructFollowedByStruct>(
                Value: new() { NullableStruct = null, Struct = recordStruct },
                AdditionalValues: [new() { NullableStruct = recordStruct, Struct = recordStruct }],
                ExpectedJsonSchema: """
                {
                    "type": ["object","null"],
                    "properties": {
                        "NullableStruct": {
                            "type": ["object","null"],
                            "properties": {
                                "X": {"type":"integer"},
                                "Y": {"type":"string"},
                                "Z": {"type":"boolean"},
                                "W": {"type":"number"}
                            }
                        },
                        "Struct": {
                            "type": "object",
                            "properties": {
                                "X": {"type":"integer"},
                                "Y": {"type":"string"},
                                "Z": {"type":"boolean"},
                                "W": {"type":"number"}
                            }
                        }
                    }
                }
                """);

            yield return new TestData<PocoWithExtensionDataProperty>(
                Value: new() { Name = "name", ExtensionData = new() { ["x"] = 42 } },
                ExpectedJsonSchema: """
                    {
                        "type": ["object","null"],
                        "properties": {
                            "Name": { "type": ["string", "null"] }
                        }
                    }
                    """);

            yield return new TestData<PocoDisallowingUnmappedMembers>(
                Value: new() { Name = "name", Age = 42 },
                ExpectedJsonSchema: """
                {
                    "type": ["object","null"],
                    "properties": {
                        "Name": {"type": ["string", "null"]},
                        "Age": {"type":"integer"}
                    },
                    "additionalProperties": false
                }
                """);

            yield return new TestData<PocoWithNullableAnnotationAttributes>(
                Value: new() { MaybeNull = null!, AllowNull = null, NotNull = null, DisallowNull = null!, NotNullDisallowNull = "str" },
                ExpectedJsonSchema: """
                {
                    "type": ["object","null"],
                    "properties": {
                        "MaybeNull": {"type":["string","null"]},
                        "AllowNull": {"type":["string","null"]},
                        "NotNull": {"type":["string","null"]},
                        "DisallowNull": {"type":["string","null"]},
                        "NotNullDisallowNull": {"type":"string"}
                    }
                }
                """);

            yield return new TestData<PocoWithNullableAnnotationAttributesOnConstructorParams>(
                Value: new(allowNull: null, disallowNull: "str"),
                ExpectedJsonSchema: """
                {
                    "type": ["object","null"],
                    "properties": {
                        "AllowNull": {"type":["string","null"]},
                        "DisallowNull": {"type":"string"}
                    },
                    "required": ["AllowNull", "DisallowNull"]
                }
                """);

            yield return new TestData<PocoWithNullableConstructorParameter>(
                Value: new(null),
                ExpectedJsonSchema: """
                {
                    "type": ["object","null"],
                    "properties": {
                        "Value": {"type":["string","null"]}
                    },
                    "required": ["Value"]
                }
                """);

            yield return new TestData<PocoWithOptionalConstructorParams>(
                Value: new(),
                ExpectedJsonSchema: """
                {
                    "type": ["object","null"],
                    "properties": {
                        "X1": {"type":"string", "default": "str" },
                        "X2": {"type":"integer", "default": 42 },
                        "X3": {"type":"boolean", "default": true },
                        "X4": {"type":"number", "default": 0 },
                        "X5": {"enum":["A","B","C"], "default": "A" },
                        "X6": {"type":["string","null"], "default": "str" },
                        "X7": {"type":["integer","null"], "default": 42 },
                        "X8": {"type":["boolean","null"], "default": true },
                        "X9": {"type":["number","null"], "default": 0 },
                        "X10": {"enum":["A","B","C", null], "default": "A" }
                    }
                }
                """);

            yield return new TestData<GenericPocoWithNullableConstructorParameter<string>>(
                Value: new(null!),
                ExpectedJsonSchema: """
                {
                    "type": ["object","null"],
                    "properties": {
                        "Value": {"type":["string","null"]}
                    },
                    "required": ["Value"]
                }
                """);

            yield return new TestData<PocoWithPolymorphism>(
                Value: new PocoWithPolymorphism.DerivedPocoStringDiscriminator { BaseValue = 42, DerivedValue = "derived" },
                AdditionalValues: [
                    new PocoWithPolymorphism.DerivedPocoNoDiscriminator { BaseValue = 42, DerivedValue = "derived" },
                    new PocoWithPolymorphism.DerivedPocoIntDiscriminator { BaseValue = 42, DerivedValue = "derived" },
                    new PocoWithPolymorphism.DerivedCollectionNoDiscriminator { BaseValue = 42 },
                    new PocoWithPolymorphism.DerivedCollection { BaseValue = 42 },
                    new PocoWithPolymorphism.DerivedDictionaryNoDiscriminator { BaseValue = 42 },
                    new PocoWithPolymorphism.DerivedDictionary { BaseValue = 42 },
                ],

                ExpectedJsonSchema: """
                {
                    "anyOf": [
                        {
                            "type": ["object","null"],
                            "properties": {
                                "BaseValue": {"type":"integer"},
                                "DerivedValue": {"type":["string", "null"]}
                            }
                        },
                        {
                            "type": ["object","null"],
                            "properties": {
                                "$type": {"const":"derivedPoco"},
                                "BaseValue": {"type":"integer"},
                                "DerivedValue": {"type":["string", "null"]}
                            },
                            "required": ["$type"]
                        },
                        {
                            "type": ["object","null"],
                            "properties": {
                                "$type": {"const":42},
                                "BaseValue": {"type":"integer"},
                                "DerivedValue": {"type":["string", "null"]}
                            },
                            "required": ["$type"]
                        },
                        {
                            "type": ["array","null"],
                            "items": {"type":"integer"}
                        },
                        {
                            "type": ["object","null"],
                            "properties": {
                                "$type": {"const":"derivedCollection"},
                                "$values": {
                                    "type": "array",
                                    "items": {"type":"integer"}
                                }
                            },
                            "required": ["$type"]
                        },
                        {
                            "type": ["object","null"],
                            "additionalProperties":{"type": "integer"}
                        },
                        {
                            "type": ["object","null"],
                            "properties": {
                                "$type": {"const":"derivedDictionary"}
                            },
                            "additionalProperties":{"type": "integer"},
                            "required": ["$type"]
                        }
                    ]
                }
                """);

            yield return new TestData<NonAbstractClassWithSingleDerivedType>(
                Value: new NonAbstractClassWithSingleDerivedType(),
                AdditionalValues: [new NonAbstractClassWithSingleDerivedType.Derived()],
                ExpectedJsonSchema: """{"type":["object","null"]}""");

            yield return new TestData<DiscriminatedUnion>(
                Value: new DiscriminatedUnion.Left("value"),
                AdditionalValues: [new DiscriminatedUnion.Right(42)],
                ExpectedJsonSchema: """
                {
                    "type": ["object","null"],
                    "required": ["case"],
                    "anyOf": [
                        {
                            "properties": {
                                "case": {"const":"left"},
                                "value": {"type":"string"}
                            },
                            "required": ["value"]
                        },
                        {
                            "properties": {
                                "case": {"const":"right"},
                                "value": {"type":"integer"}
                            },
                            "required": ["value"]
                        }
                    ]
                }
                """);

            yield return new TestData<PocoCombiningPolymorphicTypeAndDerivedTypes>(
                Value: new(),
                ExpectedJsonSchema: """
                {
                    "type": ["object","null"],
                    "properties": {
                        "PolymorphicValue": {
                            "anyOf": [
                                {
                                    "type": "object",
                                    "properties": {
                                        "BaseValue": {"type":"integer"},
                                        "DerivedValue": {"type":["string", "null"]}
                                    }
                                },
                                {
                                    "type": "object",
                                    "properties": {
                                        "$type": {"const":"derivedPoco"},
                                        "BaseValue": {"type":"integer"},
                                        "DerivedValue": {"type":["string","null"]}
                                    },
                                    "required": ["$type"]
                                },
                                {
                                    "type": "object",
                                    "properties": {
                                        "$type": {"const":42},
                                        "BaseValue": {"type":"integer"},
                                        "DerivedValue": {"type":["string", "null"]}
                                    },
                                    "required": ["$type"]
                                },
                                {
                                    "type": "array",
                                    "items": {"type":"integer"}
                                },
                                {
                                    "type": "object",
                                    "properties": {
                                        "$type": {"const":"derivedCollection"},
                                        "$values": {
                                            "type": "array",
                                            "items": {"type":"integer"}
                                        }
                                    },
                                    "required": ["$type"]
                                },
                                {
                                    "type": "object",
                                    "additionalProperties":{"type": "integer"}
                                },
                                {
                                    "type": "object",
                                    "properties": {
                                        "$type": {"const":"derivedDictionary"}
                                    },
                                    "additionalProperties":{"type": "integer"},
                                    "required": ["$type"]
                                }
                            ]
                        },
                        "DiscriminatedUnion":{
                            "type": "object",
                            "required": ["case"],
                            "anyOf": [
                                {
                                    "properties": {
                                        "case": {"const":"left"},
                                        "value": {"type":"string"}
                                    },
                                    "required": ["value"]
                                },
                                {
                                    "properties": {
                                        "case": {"const":"right"},
                                        "value": {"type":"integer"}
                                    },
                                    "required": ["value"]
                                }
                            ]
                        },
                        "DerivedValue1": {
                            "type": "object",
                            "properties": {
                                "BaseValue": {"type":"integer"},
                                "DerivedValue": {"type":["string", "null"]}
                            }
                        },
                        "DerivedValue2": {
                            "type": "object",
                            "properties": {
                                "BaseValue": {"type":"integer"},
                                "DerivedValue": {"type":["string", "null"]}
                            }
                        }
                    }
                }
                """);

            yield return new TestData<ClassWithComponentModelAttributes>(
                Value: new("string", -1),
                ExpectedJsonSchema: """
                {
                    "type": ["object","null"],
                    "properties": {
                        "StringValue": {"type":"string","pattern":"\\w+"},
                        "IntValue": {"type":"integer","default":42}
                    },
                    "required": ["StringValue","IntValue"]
                }
                """,
                Options: new()
                {
                    TransformSchemaNode = static (ctx, schema) =>
                    {
                        if (ctx.PropertyInfo is null || schema is not JsonObject schemaObj || schemaObj.ContainsKey("$ref"))
                        {
                            return schema;
                        }

                        DefaultValueAttribute? defaultValueAttr =
                            GetCustomAttribute<DefaultValueAttribute>(ctx.PropertyInfo?.AttributeProvider) ??
                            GetCustomAttribute<DefaultValueAttribute>(ctx.PropertyInfo?.AssociatedParameter?.AttributeProvider);

                        if (defaultValueAttr != null)
                        {
                            schemaObj["default"] = JsonSerializer.SerializeToNode(defaultValueAttr.Value, ctx.TypeInfo);
                        }

                        RegularExpressionAttribute? regexAttr =
                            GetCustomAttribute<RegularExpressionAttribute>(ctx.PropertyInfo?.AttributeProvider) ??
                            GetCustomAttribute<RegularExpressionAttribute>(ctx.PropertyInfo?.AssociatedParameter?.AttributeProvider);

                        if (regexAttr != null)
                        {
                            schemaObj["pattern"] = regexAttr.Pattern;
                        }

                        return schemaObj;
                    }
                });

            yield return new TestData<ClassWithJsonPointerEscapablePropertyNames>(
                Value: new ClassWithJsonPointerEscapablePropertyNames { Value = new() },
                ExpectedJsonSchema: """
                {
                    "type": ["object","null"],
                    "properties": {
                        "~/path/to/value": {
                            "type": "object",
                            "properties": {
                                "Value" : {"type":"integer"},
                                "Next": {
                                    "type": ["object","null"],
                                    "properties": {
                                        "Value" : {"type":"integer"},
                                        "Next": {"$ref":"#/properties/~0~1path~1to~1value/properties/Next"}
                                    }
                                }
                            }
                        }
                    }
                }
                """);

            // Collection types
            yield return new TestData<int[]>([1, 2, 3], ExpectedJsonSchema: """{"type":["array","null"],"items":{"type":"integer"}}""");
            yield return new TestData<List<bool>>([false, true, false], ExpectedJsonSchema: """{"type":["array","null"],"items":{"type":"boolean"}}""");
            yield return new TestData<HashSet<string>>(["one", "two", "three"], ExpectedJsonSchema: """{"type":["array","null"],"items":{"type":["string","null"]}}""");
            yield return new TestData<Queue<double>>(new([1.1, 2.2, 3.3]), ExpectedJsonSchema: """{"type":["array","null"],"items":{"type":"number"}}""");
            yield return new TestData<Stack<char>>(new(['x', '2', '+']), ExpectedJsonSchema: """{"type":["array","null"],"items":{"type":"string","minLength":1,"maxLength":1}}""");
            yield return new TestData<ImmutableArray<int>>([1, 2, 3], ExpectedJsonSchema: """{"type":"array","items":{"type":"integer"}}""");
            yield return new TestData<ImmutableList<string>>(["one", "two", "three"], ExpectedJsonSchema: """{"type":["array","null"],"items":{"type":["string","null"]}}""");
            yield return new TestData<ImmutableQueue<bool>>([false, false, true], ExpectedJsonSchema: """{"type":["array","null"],"items":{"type":"boolean"}}""");
            yield return new TestData<object[]>([1, "two", 3.14], ExpectedJsonSchema: """{"type":["array","null"]}""");
            yield return new TestData<System.Collections.ArrayList>([1, "two", 3.14], ExpectedJsonSchema: """{"type":["array","null"]}""");

            // Dictionary types
            yield return new TestData<Dictionary<string, int>>(
                Value: new() { ["one"] = 1, ["two"] = 2, ["three"] = 3 },
                ExpectedJsonSchema: """{"type":["object","null"],"additionalProperties":{"type": "integer"}}""");

            yield return new TestData<StructDictionary<string, int>>(
                Value: new([new("one", 1), new("two", 2), new("three", 3)]),
                ExpectedJsonSchema: """{"type":"object","additionalProperties":{"type": "integer"}}""");

            yield return new TestData<SortedDictionary<int, string>>(
                Value: new() { [1] = "one", [2] = "two", [3] = "three" },
                ExpectedJsonSchema: """{"type":["object","null"],"additionalProperties":{"type": ["string","null"]}}""");

            yield return new TestData<Dictionary<string, SimplePoco>>(
                Value: new()
                {
                    ["one"] = new() { String = "string", StringNullable = "string", Int = 42, Double = 3.14, Boolean = true },
                    ["two"] = new() { String = "string", StringNullable = null, Int = 42, Double = 3.14, Boolean = true },
                    ["three"] = new() { String = "string", StringNullable = null, Int = 42, Double = 3.14, Boolean = true },
                },
                ExpectedJsonSchema: """
                {
                    "type": ["object","null"],
                    "additionalProperties": {
                        "type": ["object","null"],
                        "properties": {
                            "String": { "type": "string" },
                            "StringNullable": { "type": ["string","null"] },
                            "Int": { "type": "integer" },
                            "Double": { "type": "number" },
                            "Boolean": { "type": "boolean" }
                        }
                    }
                }
                """);

            yield return new TestData<Dictionary<string, object>>(
                Value: new() { ["one"] = 1, ["two"] = "two", ["three"] = 3.14 },
                ExpectedJsonSchema: """{"type":["object","null"]}""");

            yield return new TestData<Hashtable>(
                Value: new() { ["one"] = 1, ["two"] = "two", ["three"] = 3.14 },
                ExpectedJsonSchema: """{"type":["object","null"]}""");
        }

        public enum IntEnum { A, B, C };

        [JsonConverter(typeof(JsonStringEnumConverter<StringEnum>))]
        public enum StringEnum { A, B, C };

        [Flags, JsonConverter(typeof(JsonStringEnumConverter<FlagsStringEnum>))]
        public enum FlagsStringEnum { A = 1, B = 2, C = 4 };

        public class SimplePoco
        {
            public string String { get; set; } = "default";
            public string? StringNullable { get; set; }

            public int Int { get; set; }
            public double Double { get; set; }
            public bool Boolean { get; set; }
        }

        public record SimpleRecord(int X, string Y, bool Z, double W);
        public record struct SimpleRecordStruct(int X, string Y, bool Z, double W);

        public record RecordWithOptionalParameters(
            int X1, string X2, bool X3, double X4, StringEnum X5,
            int Y1 = 42, string Y2 = "str", bool Y3 = true, double Y4 = 0, StringEnum Y5 = StringEnum.A);

        public class PocoWithRequiredMembers
        {
            [JsonInclude]
            public required string X;

            public required string Y { get; set; }

            [JsonRequired]
            public int Z { get; set; }
        }

        public class PocoWithIgnoredMembers
        {
            public int X { get; set; }

            [JsonIgnore]
            public int Y { get; set; }
        }

        public class PocoWithCustomNaming
        {
            [JsonPropertyName("int")]
            public int IntegerProperty { get; set; }

            [JsonPropertyName("str")]
            public string? StringProperty { get; set; }
        }

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public class PocoWithCustomNumberHandling
        {
            public int X { get; set; }
        }

        public class PocoWithCustomNumberHandlingOnProperties
        {
            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
            public int IntegerReadingFromString { get; set; }

            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
            public double DoubleReadingFromString { get; set; }

            [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
            public decimal DecimalReadingFromString { get; set; }

            [JsonNumberHandling(JsonNumberHandling.WriteAsString)]
            public int IntegerWritingAsString { get; set; }

            [JsonNumberHandling(JsonNumberHandling.WriteAsString)]
            public double DoubleWritingAsString { get; set; }

            [JsonNumberHandling(JsonNumberHandling.WriteAsString)]
            public decimal DecimalWritingAsString { get; set; }

            [JsonNumberHandling(JsonNumberHandling.AllowNamedFloatingPointLiterals)]
            public int IntegerAllowingFloatingPointLiterals { get; set; }

            [JsonNumberHandling(JsonNumberHandling.AllowNamedFloatingPointLiterals)]
            public double DoubleAllowingFloatingPointLiterals { get; set; }

            [JsonNumberHandling(JsonNumberHandling.AllowNamedFloatingPointLiterals)]
            public decimal DecimalAllowingFloatingPointLiterals { get; set; }

            [JsonNumberHandling(JsonNumberHandling.AllowNamedFloatingPointLiterals | JsonNumberHandling.AllowReadingFromString)]
            public int IntegerAllowingFloatingPointLiteralsAndReadingFromString { get; set; }

            [JsonNumberHandling(JsonNumberHandling.AllowNamedFloatingPointLiterals | JsonNumberHandling.AllowReadingFromString)]
            public double DoubleAllowingFloatingPointLiteralsAndReadingFromString { get; set; }

            [JsonNumberHandling(JsonNumberHandling.AllowNamedFloatingPointLiterals | JsonNumberHandling.AllowReadingFromString)]
            public decimal DecimalAllowingFloatingPointLiteralsAndReadingFromString { get; set; }
        }

        public class PocoWithRecursiveMembers
        {
            public int Value { get; init; }
            public PocoWithRecursiveMembers? Next { get; init; }
        }

        public class PocoWithRecursiveCollectionElement
        {
            public List<PocoWithRecursiveCollectionElement> Children { get; init; } = new();
        }

        public class PocoWithRecursiveDictionaryValue
        {
            public Dictionary<string, PocoWithRecursiveDictionaryValue> Children { get; init; } = new();
        }

        [Description("The type description")]
        public class PocoWithDescription
        {
            [Description("The property description")]
            public int X { get; set; }
        }

        [JsonConverter(typeof(CustomConverter))]
        public class PocoWithCustomConverter
        {
            public int Value { get; set; }

            public class CustomConverter : JsonConverter<PocoWithCustomConverter>
            {
                public override PocoWithCustomConverter Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
                    new PocoWithCustomConverter { Value = reader.GetInt32() };

                public override void Write(Utf8JsonWriter writer, PocoWithCustomConverter value, JsonSerializerOptions options) =>
                    writer.WriteNumberValue(value.Value);
            }
        }

        public class PocoWithCustomPropertyConverter
        {
            [JsonConverter(typeof(CustomConverter))]
            public int Value { get; set; }

            public class CustomConverter : JsonConverter<int>
            {
                public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                    => int.Parse(reader.GetString()!);

                public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
                    => writer.WriteStringValue(value.ToString());
            }
        }

        public class PocoWithEnums
        {
            public IntEnum IntEnum { get; init; }
            public StringEnum StringEnum { get; init; }

            [JsonConverter(typeof(JsonStringEnumConverter<IntEnum>))]
            public IntEnum IntEnumUsingStringConverter { get; set; }

            [JsonConverter(typeof(JsonStringEnumConverter<IntEnum>))]
            public IntEnum? NullableIntEnumUsingStringConverter { get; set; }

            [JsonConverter(typeof(JsonNumberEnumConverter<StringEnum>))]
            public StringEnum StringEnumUsingIntConverter { get; set; }

            [JsonConverter(typeof(JsonNumberEnumConverter<StringEnum>))]
            public StringEnum? NullableStringEnumUsingIntConverter { get; set; }
        }

        public class PocoWithStructFollowedByNullableStruct
        {
            public SimpleRecordStruct? NullableStruct { get; set; }
            public SimpleRecordStruct Struct { get; set; }
        }

        public class PocoWithNullableStructFollowedByStruct
        {
            public SimpleRecordStruct? NullableStruct { get; set; }
            public SimpleRecordStruct Struct { get; set; }
        }

        public class PocoWithExtensionDataProperty
        {
            public string? Name { get; set; }

            [JsonExtensionData]
            public Dictionary<string, object>? ExtensionData { get; set; }
        }

        [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
        public class PocoDisallowingUnmappedMembers
        {
            public string? Name { get; set; }
            public int Age { get; set; }
        }

        public class PocoWithNullableAnnotationAttributes
        {
            [MaybeNull]
            public string MaybeNull { get; set; }

            [AllowNull]
            public string AllowNull { get; set; }

            [NotNull]
            public string? NotNull { get; set; }

            [DisallowNull]
            public string? DisallowNull { get; set; }

            [NotNull, DisallowNull]
            public string? NotNullDisallowNull { get; set; } = "";
        }

        public class PocoWithNullableAnnotationAttributesOnConstructorParams([AllowNull] string allowNull, [DisallowNull] string? disallowNull)
        {
            public string AllowNull { get; } = allowNull!;
            public string DisallowNull { get; } = disallowNull;
        }

        public class PocoWithNullableConstructorParameter(string? value)
        {
            public string Value { get; } = value!;
        }

        public class PocoWithOptionalConstructorParams(
            string x1 = "str", int x2 = 42, bool x3 = true, double x4 = 0, StringEnum x5 = StringEnum.A,
            string? x6 = "str", int? x7 = 42, bool? x8 = true, double? x9 = 0, StringEnum? x10 = StringEnum.A)
        {
            public string X1 { get; } = x1;
            public int X2 { get; } = x2;
            public bool X3 { get; } = x3;
            public double X4 { get; } = x4;
            public StringEnum X5 { get; } = x5;

            public string? X6 { get; } = x6;
            public int? X7 { get; } = x7;
            public bool? X8 { get; } = x8;
            public double? X9 { get; } = x9;
            public StringEnum? X10 { get; } = x10;
        }

        // Regression test for https://github.com/dotnet/runtime/issues/92487
        public class GenericPocoWithNullableConstructorParameter<T>(T value)
        {
            [NotNull]
            public T Value { get; } = value!;
        }

        [JsonDerivedType(typeof(DerivedPocoNoDiscriminator))]
        [JsonDerivedType(typeof(DerivedPocoStringDiscriminator), "derivedPoco")]
        [JsonDerivedType(typeof(DerivedPocoIntDiscriminator), 42)]
        [JsonDerivedType(typeof(DerivedCollectionNoDiscriminator))]
        [JsonDerivedType(typeof(DerivedCollection), "derivedCollection")]
        [JsonDerivedType(typeof(DerivedDictionaryNoDiscriminator))]
        [JsonDerivedType(typeof(DerivedDictionary), "derivedDictionary")]
        public abstract class PocoWithPolymorphism
        {
            public int BaseValue { get; set; }

            public class DerivedPocoNoDiscriminator : PocoWithPolymorphism
            {
                public string? DerivedValue { get; set; }
            }

            public class DerivedPocoStringDiscriminator : PocoWithPolymorphism
            {
                public string? DerivedValue { get; set; }
            }

            public class DerivedPocoIntDiscriminator : PocoWithPolymorphism
            {
                public string? DerivedValue { get; set; }
            }

            public class DerivedCollection : PocoWithPolymorphism, IEnumerable<int>
            {
                public IEnumerator<int> GetEnumerator() => Enumerable.Repeat(BaseValue, 1).GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }

            public class DerivedCollectionNoDiscriminator : DerivedCollection;

            public class DerivedDictionary : PocoWithPolymorphism, IReadOnlyDictionary<string, int>
            {
                public int this[string key] => key == nameof(BaseValue) ? BaseValue : throw new KeyNotFoundException();
                public IEnumerable<string> Keys => [nameof(BaseValue)];
                public IEnumerable<int> Values => [BaseValue];
                public int Count => 1;
                public bool ContainsKey(string key) => key == nameof(BaseValue);
                public bool TryGetValue(string key, out int value) => key == nameof(BaseValue) ? (value = BaseValue) == BaseValue : (value = 0) == 0;
                public IEnumerator<KeyValuePair<string, int>> GetEnumerator() => Enumerable.Repeat(new KeyValuePair<string, int>(nameof(BaseValue), BaseValue), 1).GetEnumerator();
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }

            public class DerivedDictionaryNoDiscriminator : DerivedDictionary;
        }

        [JsonDerivedType(typeof(Derived))]
        public class NonAbstractClassWithSingleDerivedType
        {
            public class Derived : NonAbstractClassWithSingleDerivedType;
        }

        [JsonPolymorphic(TypeDiscriminatorPropertyName = "case")]
        [JsonDerivedType(typeof(Left), "left")]
        [JsonDerivedType(typeof(Right), "right")]
        public abstract record DiscriminatedUnion
        {
            public record Left(string value) : DiscriminatedUnion;
            public record Right(int value) : DiscriminatedUnion;
        }

        public class PocoCombiningPolymorphicTypeAndDerivedTypes
        {
            public PocoWithPolymorphism PolymorphicValue { get; set; } = new PocoWithPolymorphism.DerivedPocoNoDiscriminator { DerivedValue = "derived" };
            public DiscriminatedUnion DiscriminatedUnion { get; set; } = new DiscriminatedUnion.Left("value");
            public PocoWithPolymorphism.DerivedPocoNoDiscriminator DerivedValue1 { get; set; } = new() { DerivedValue = "derived" };
            public PocoWithPolymorphism.DerivedPocoStringDiscriminator DerivedValue2 { get; set; } = new() { DerivedValue = "derived" };
        }

        public class ClassWithComponentModelAttributes
        {
            public ClassWithComponentModelAttributes(string stringValue, [DefaultValue(42)] int intValue)
            {
                StringValue = stringValue;
                IntValue = intValue;
            }

            [RegularExpression(@"\w+")]
            public string StringValue { get; }

            public int IntValue { get; }
        }

        public class ClassWithJsonPointerEscapablePropertyNames
        {
            [JsonPropertyName("~/path/to/value")]
            public PocoWithRecursiveMembers Value { get; set; }
        }

        public readonly struct StructDictionary<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> values)
            : IReadOnlyDictionary<TKey, TValue>
            where TKey : notnull
        {
            private readonly IReadOnlyDictionary<TKey, TValue> _dictionary = values.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            public TValue this[TKey key] => _dictionary[key];
            public IEnumerable<TKey> Keys => _dictionary.Keys;
            public IEnumerable<TValue> Values => _dictionary.Values;
            public int Count => _dictionary.Count;
            public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);
            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();
#if NET
            public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => _dictionary.TryGetValue(key, out value);
#else
            public bool TryGetValue(TKey key, out TValue value) => _dictionary.TryGetValue(key, out value);
#endif
            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_dictionary).GetEnumerator();
        }

        public record TestData<T>(
            T? Value,
            string ExpectedJsonSchema,
            IEnumerable<T?>? AdditionalValues = null,
            JsonSchemaExporterOptions? Options = null)
            : ITestData
        {
            public Type Type => typeof(T);
            object? ITestData.Value => Value;

            IEnumerable<ITestData> ITestData.GetTestDataForAllValues()
            {
                yield return this;

                if (default(T) is null && Options?.TreatNullObliviousAsNonNullable != true)
                {
                    yield return this with { Value = default, AdditionalValues = null };
                }

                if (AdditionalValues != null)
                {
                    foreach (T? value in AdditionalValues)
                    {
                        yield return this with { Value = value, AdditionalValues = null };
                    }
                }
            }
        }

        public interface ITestData
        {
            Type Type { get; }

            object? Value { get; }

            string ExpectedJsonSchema { get; }

            JsonSchemaExporterOptions? Options { get; }

            IEnumerable<ITestData> GetTestDataForAllValues();
        }

        private static TAttribute? GetCustomAttribute<TAttribute>(ICustomAttributeProvider? provider, bool inherit = false) where TAttribute : Attribute
            => provider?.GetCustomAttributes(typeof(TAttribute), inherit).FirstOrDefault() as TAttribute;
    }
}
