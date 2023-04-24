module System.Text.Json.Tests.FSharp.OptionTests

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
let ``Root-level None should serialize as null``(_ : 'T) =
    let expected = "null"
    let actual = JsonSerializer.Serialize<'T option>(None)
    Assert.Equal(expected, actual)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``None property should serialize as null``(_ : 'T) =
    let expected = """{"value":null}"""
    let actual = JsonSerializer.Serialize {| value = Option<'T>.None |}
    Assert.Equal(expected, actual)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``None collection element should serialize as null``(_ : 'T) =
    let expected = """[null]"""
    let actual = JsonSerializer.Serialize [| Option<'T>.None |]
    Assert.Equal(expected, actual)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``Root-level Some should serialize as the payload`` (value : 'T) =
    let expected = JsonSerializer.Serialize(value)
    let actual = JsonSerializer.Serialize(Some value)
    Assert.Equal(expected, actual)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``Some property should serialize as the payload`` (value : 'T) =
    let expected = JsonSerializer.Serialize {| value = value |}
    let actual = JsonSerializer.Serialize {| value = Some value |}
    Assert.Equal(expected, actual)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``Some collection element should serialize as the payload`` (value : 'T) =
    let expected = JsonSerializer.Serialize [|value|]
    let actual = JsonSerializer.Serialize [|Some value|]
    Assert.Equal(expected, actual)

[<Fact>]
let ``Some of null should serialize as null`` () =
    let expected = "null"
    let actual = JsonSerializer.Serialize<string option>(Some null)
    Assert.Equal(expected, actual)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``Some of None should serialize as null`` (_ : 'T) =
    let expected = "null"
    let actual = JsonSerializer.Serialize<'T option option>(Some None)
    Assert.Equal(expected, actual)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``Some of Some of None should serialize as null`` (_ : 'T) =
    let expected = "null"
    let actual = JsonSerializer.Serialize<'T option option option>(Some (Some None))
    Assert.Equal(expected, actual)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``Some of Some of value should serialize as value`` (value : 'T) =
    let expected = JsonSerializer.Serialize value
    let actual = JsonSerializer.Serialize(Some (Some value))
    Assert.Equal(expected, actual)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``WhenWritingNull enabled should skip None properties``(_ : 'T) =
    let expected = "{}"
    let options = new JsonSerializerOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)
    let actual = JsonSerializer.Serialize<{| value : 'T option |}>({| value = None |}, options)
    Assert.Equal(expected, actual)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``Root-level null should deserialize as None``(_ : 'T) =
    let actual = JsonSerializer.Deserialize<'T option>("null")
    Assert.Equal(None, actual)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``Null property should deserialize as None``(_ : 'T) =
    let actual = JsonSerializer.Deserialize<{| value : 'T option |}>("""{"value":null}""")
    Assert.Equal(None, actual.value)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``Missing property should deserialize as None``(_ : 'T) =
    let actual = JsonSerializer.Deserialize<{| value : 'T option |}>("{}")
    Assert.Equal(None, actual.value)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``Null element should deserialize as None``(_ : 'T) =
    let expected = [Option<'T>.None]
    let actual = JsonSerializer.Deserialize<'T option []>("""[null]""")
    Assert.Equal(expected, actual)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``Root-level value should deserialize as Some``(value : 'T) =
    let json = JsonSerializer.Serialize(value)
    let actual = JsonSerializer.Deserialize<'T option>(json)
    Assert.Equal(Some value, actual)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``Property value should deserialize as Some``(value : 'T) =
    let json = JsonSerializer.Serialize {| value = value |}
    let actual = JsonSerializer.Deserialize<{| value : 'T option|}>(json)
    Assert.Equal(Some value, actual.value)

[<Theory>]
[<MemberData(nameof(getOptionalElementInputs))>]
let ``Collection element should deserialize as Some``(value : 'T) =
    let json = JsonSerializer.Serialize [| value |]
    let actual = JsonSerializer.Deserialize<'T option []>(json)
    Assert.Equal([Some value], actual)

[<Fact>]
let ``Optional value should support resumable serialization``() = async {
    let valueToSerialize = {| Values = Some [|1 .. 200|] |}
    let expectedJson = JsonSerializer.Serialize valueToSerialize

    let options = new JsonSerializerOptions(DefaultBufferSize = 1)
    let! actualJson = JsonSerializer.SerializeAsync(valueToSerialize, options)

    Assert.Equal(expectedJson, actualJson)
}

[<Fact>]
let ``Optional value should support resumable deserialization``() = async {
    let valueToSerialize = {| Values = Some [|1 .. 200|] |}
    let json = JsonSerializer.Serialize valueToSerialize

    let options = new JsonSerializerOptions(DefaultBufferSize = 1)
    let! result = JsonSerializer.DeserializeAsync<{| Values : int [] option |}>(json, options)

    Assert.Equal(valueToSerialize, result)
}
