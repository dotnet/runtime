module System.Text.Json.Tests.FSharp.ValueOptionTests

open System.Text.Json
open System.Text.Json.Serialization
open System.Text.Json.Tests.FSharp.Helpers
open Xunit

let getOptionalElementInputs() = seq {
    let wrap value = [| box value |]

    wrap 42
    wrap false
    wrap "string"
    wrap [|1..5|]
    wrap (3,2)
    wrap {| Name = "Mary" ; Age = 32 |}
    wrap struct {| Name = "Mary" ; Age = 32 |}
    wrap [false; true; false; false]
    wrap (Set.ofSeq [1 .. 5])
    wrap (Map.ofSeq [("key1", "value1"); ("key2", "value2")])
}

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``Root-level ValueNone should serialize as null``(_ : 'T) =
    let expected = "null"
    let actual = JsonSerializer.Serialize<'T voption>(ValueNone)
    Assert.Equal(expected, actual)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``ValueNone property should serialize as null``(_ : 'T) =
    let expected = """{"value":null}"""
    let actual = JsonSerializer.Serialize {| value = ValueOption<'T>.ValueNone |}
    Assert.Equal(expected, actual)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``ValueNone collection element should serialize as null``(_ : 'T) =
    let expected = """[null]"""
    let actual = JsonSerializer.Serialize [| ValueOption<'T>.ValueNone |]
    Assert.Equal(expected, actual)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``Root-level ValueSome should serialize as the payload`` (value : 'T) =
    let expected = JsonSerializer.Serialize(value)
    let actual = JsonSerializer.Serialize(ValueSome value)
    Assert.Equal(expected, actual)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``ValueSome property should serialize as the payload`` (value : 'T) =
    let expected = JsonSerializer.Serialize {| value = value |}
    let actual = JsonSerializer.Serialize {| value = ValueSome value |}
    Assert.Equal(expected, actual)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``ValueSome collection element should serialize as the payload`` (value : 'T) =
    let expected = JsonSerializer.Serialize [|value|]
    let actual = JsonSerializer.Serialize [|ValueSome value|]
    Assert.Equal(expected, actual)

[<Fact>]
let ``ValueSome of null should serialize as null`` () =
    let expected = "null"
    let actual = JsonSerializer.Serialize<string voption>(ValueSome null)
    Assert.Equal(expected, actual)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``ValueSome of ValueNone should serialize as null`` (_ : 'T) =
    let expected = "null"
    let actual = JsonSerializer.Serialize<'T voption voption>(ValueSome ValueNone)
    Assert.Equal(expected, actual)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``ValueSome of ValueSome of ValueNone should serialize as null`` (_ : 'T) =
    let expected = "null"
    let actual = JsonSerializer.Serialize<'T voption voption voption>(ValueSome (ValueSome ValueNone))
    Assert.Equal(expected, actual)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``ValueSome of ValueSome of value should serialize as value`` (value : 'T) =
    let expected = JsonSerializer.Serialize value
    let actual = JsonSerializer.Serialize(ValueSome (ValueSome value))
    Assert.Equal(expected, actual)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``WhenWritingDefault enabled should skip ValueNone properties``(_ : 'T) =
    let expected = "{}"
    let options = new JsonSerializerOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault)
    let actual = JsonSerializer.Serialize<{| value : 'T voption |}>({| value = ValueNone |}, options)
    Assert.Equal(expected, actual)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``Root-level null should deserialize as ValueNone``(_ : 'T) =
    let actual = JsonSerializer.Deserialize<'T voption>("null")
    Assert.Equal(ValueNone, actual)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``Null property should deserialize as ValueNone``(_ : 'T) =
    let actual = JsonSerializer.Deserialize<{| value : 'T voption |}>("""{"value":null}""")
    Assert.Equal(ValueNone, actual.value)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``Missing property should deserialize as ValueNone``(_ : 'T) =
    let actual = JsonSerializer.Deserialize<{| value : 'T voption |}>("{}")
    Assert.Equal(ValueNone, actual.value)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``Null element should deserialize as ValueNone``(_ : 'T) =
    let expected = [ValueOption<'T>.ValueNone]
    let actual = JsonSerializer.Deserialize<'T voption []>("""[null]""")
    Assert.Equal(expected, actual)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``Root-level value should deserialize as ValueSome``(value : 'T) =
    let json = JsonSerializer.Serialize(value)
    let actual = JsonSerializer.Deserialize<'T voption>(json)
    Assert.Equal(ValueSome value, actual)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``Property value should deserialize as ValueSome``(value : 'T) =
    let json = JsonSerializer.Serialize {| value = value |}
    let actual = JsonSerializer.Deserialize<{| value : 'T voption|}>(json)
    Assert.Equal(ValueSome value, actual.value)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``Collection element should deserialize as ValueSome``(value : 'T) =
    let json = JsonSerializer.Serialize [| value |]
    let actual = JsonSerializer.Deserialize<'T voption []>(json)
    Assert.Equal([ValueSome value], actual)

[<Fact>]
let ``Optional value should support resumable serialization``() = async {
    let valueToSerialize = {| Values = ValueSome [|1 .. 200|] |}
    let expectedJson = JsonSerializer.Serialize valueToSerialize

    let options = new JsonSerializerOptions(DefaultBufferSize = 1)
    let! actualJson = JsonSerializer.SerializeAsync(valueToSerialize, options)

    Assert.Equal(expectedJson, actualJson)
}

[<Fact>]
let ``Optional value should support resumable deserialization``() = async {
    let valueToSerialize = {| Values = ValueSome [|1 .. 200|] |}
    let json = JsonSerializer.Serialize valueToSerialize

    let options = new JsonSerializerOptions(DefaultBufferSize = 1)
    let! result = JsonSerializer.DeserializeAsync<{| Values : int [] voption |}>(json, options)

    Assert.Equal(valueToSerialize, result)
}
