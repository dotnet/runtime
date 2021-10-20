module System.Text.Json.Tests.FSharp.CollectionTests

open System.Text.Json
open System.Text.Json.Tests.FSharp.Helpers
open Xunit

let getListsAndSerializations() = seq {
    let wrapArgs (list : 'T list) (json : string) = [| box list ; box json |]

    wrapArgs [] "[]"
    wrapArgs [1] "[1]"
    wrapArgs [1;2;1;2;3;2;2;1;3;3;3] "[1,2,1,2,3,2,2,1,3,3,3]"
    wrapArgs [false;true] "[false,true]"
    wrapArgs [3.14] "[3.14]"
    wrapArgs ["apple";"banana";"cherry"] """["apple","banana","cherry"]"""
    wrapArgs [{| x = 0 ; y = 1 |}] """[{"x":0,"y":1}]"""
    wrapArgs Unchecked.defaultof<int list> "null" // we support null list serialization and deserialization
}

[<Theory>]
[<MemberData(nameof(getListsAndSerializations))>]
let ``Lists should have expected serialization`` (list : 'T list) (expectedJson : string) =
    let actualJson = JsonSerializer.Serialize list
    Assert.Equal(expectedJson, actualJson)

[<Theory>]
[<MemberData(nameof(getListsAndSerializations))>]
let ``Lists should have expected deserialization``(expectedList : 'T list) (json : string) =
    let actualList = JsonSerializer.Deserialize<'T list> json
    Assert.Equal<'T>(expectedList, actualList)

[<Theory>]
[<InlineData("1")>]
[<InlineData("false")>]
[<InlineData("\"value\"")>]
[<InlineData("{}")>]
[<InlineData("[false]")>]
let ``List deserialization should reject invalid inputs``(json : string) =
    Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<int list>(json) |> ignore) |> ignore

[<Fact>]
let ``List async serialization should be supported``() = async {
    let inputs = [1 .. 200]
    let expectedJson = JsonSerializer.Serialize inputs

    let options = new JsonSerializerOptions(DefaultBufferSize = 1)
    let! actualJson = JsonSerializer.SerializeAsync(inputs, options)

    Assert.Equal(expectedJson, actualJson)
}

[<Fact>]
let ``List async deserialization should be supported``() = async {
    let inputs = [1 .. 200]
    let json = JsonSerializer.Serialize inputs

    let options = new JsonSerializerOptions(DefaultBufferSize = 1)
    let! result = JsonSerializer.DeserializeAsync<int list>(json, options)

    Assert.Equal<int>(inputs, result)
}

let getSetsAndSerializations() = seq {
    let wrapArgs (set : Set<'T>) (json : string) = [| box set ; box json |]

    wrapArgs Set.empty<int> "[]"
    wrapArgs (set [1]) "[1]"
    wrapArgs (set [1;2;3]) "[1,2,3]"
    wrapArgs (set [false;true]) "[false,true]"
    wrapArgs (set [3.14]) "[3.14]"
    wrapArgs (set ["apple";"banana";"cherry"]) """["apple","banana","cherry"]"""
    wrapArgs (set [{| x = 0 ; y = 1 |}]) """[{"x":0,"y":1}]"""
    wrapArgs Unchecked.defaultof<Set<int>> "null" // we support null set serialization and deserialization
}

[<Theory>]
[<MemberData(nameof(getSetsAndSerializations))>]
let ``Sets should have expected serialization`` (set : Set<'T>) (expectedJson : string) =
    let actualJson = JsonSerializer.Serialize set
    Assert.Equal(expectedJson, actualJson)

[<Theory>]
[<MemberData(nameof(getSetsAndSerializations))>]
let ``Sets should have expected deserialization`` (expectedSet : Set<'T>) (json : string) =
    let actualSet = JsonSerializer.Deserialize<Set<'T>> json
    Assert.Equal<Set<'T>>(expectedSet, actualSet)

[<Fact>]
let ``Set deserialization should trim duplicate elements`` () =
    let expectedSet = set [1;2;3]
    let actualSet = JsonSerializer.Deserialize<Set<int>> "[1,2,1,2,3,2,2,1,3,3,3]"
    Assert.Equal<Set<int>>(expectedSet, actualSet)

[<Theory>]
[<InlineData("1")>]
[<InlineData("false")>]
[<InlineData("\"value\"")>]
[<InlineData("{}")>]
[<InlineData("[false]")>]
let ``Set deserialization should reject invalid inputs``(json : string) =
    Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<Set<int>>(json) |> ignore) |> ignore

[<Fact>]
let ``Set async serialization should be supported``() = async {
    let inputs = set [1 .. 200]
    let expectedJson = JsonSerializer.Serialize inputs

    let options = new JsonSerializerOptions(DefaultBufferSize = 1)
    let! actualJson = JsonSerializer.SerializeAsync(inputs, options)

    Assert.Equal(expectedJson, actualJson)
}

[<Fact>]
let ``Set async deserialization should be supported``() = async {
    let inputs = set [1 .. 200]
    let json = JsonSerializer.Serialize inputs

    let options = new JsonSerializerOptions(DefaultBufferSize = 1)
    let! result = JsonSerializer.DeserializeAsync<Set<int>>(json, options)

    Assert.Equal<Set<int>>(inputs, result)
}

let getMapsAndSerializations() = seq {
    let wrapArgs (set : Map<'K,'V>) (json : string) = [| box set ; box json |]

    wrapArgs Map.empty<int,string> "{}"
    wrapArgs (Map.ofList [("key", "value")]) """{"key":"value"}"""
    wrapArgs (Map.ofList [(1, 1); (2, 1)]) """{"1":1,"2":1}"""
    wrapArgs (Map.ofList [(false, 1); (true, 1)]) """{"False":1,"True":1}"""
    wrapArgs (Map.ofList [("fruit", ["apple";"banana";"cherry"])]) """{"fruit":["apple","banana","cherry"]}"""
    wrapArgs (Map.ofList [("coordinates", {| x = 0 ; y = 1 |})]) """{"coordinates":{"x":0,"y":1}}"""
    wrapArgs Unchecked.defaultof<Map<int, int>> "null" // we support null set serialization and deserialization
}

[<Theory>]
[<MemberData(nameof(getMapsAndSerializations))>]
let ``Maps should have expected serialization`` (map : Map<'key, 'value>) (expectedJson : string) =
    let actualJson = JsonSerializer.Serialize map
    Assert.Equal(expectedJson, actualJson)

[<Theory>]
[<MemberData(nameof(getMapsAndSerializations))>]
let ``Maps should have expected deserialization`` (expectedMap : Map<'key, 'value>) (json : string) =
    let actualMap = JsonSerializer.Deserialize<Map<'key, 'value>> json
    Assert.Equal<Map<'key, 'value>>(expectedMap, actualMap)

[<Theory>]
[<InlineData("1")>]
[<InlineData("false")>]
[<InlineData("\"value\"")>]
[<InlineData("[]")>]
let ``Map deserialization should reject invalid inputs``(json : string) =
    Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<Map<string, int>>(json) |> ignore) |> ignore

[<Fact>]
let ``Map async serialization should be supported``() = async {
    let inputs = Map.ofList [for i in 1 .. 200 -> (i.ToString(), i)]
    let expectedJson = JsonSerializer.Serialize inputs

    let options = new JsonSerializerOptions(DefaultBufferSize = 1)
    let! actualJson = JsonSerializer.SerializeAsync(inputs, options)

    Assert.Equal(expectedJson, actualJson)
}

[<Fact>]
let ``Map async deserialization should be supported``() = async {
    let inputs = Map.ofList [for i in 1 .. 200 -> (i.ToString(), i)]
    let json = JsonSerializer.Serialize inputs

    let options = new JsonSerializerOptions(DefaultBufferSize = 1)
    let! result = JsonSerializer.DeserializeAsync<Map<string, int>>(json, options)

    Assert.Equal<Map<string, int>>(inputs, result)
}
