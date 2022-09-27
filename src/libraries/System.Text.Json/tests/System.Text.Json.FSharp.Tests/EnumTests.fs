module System.Text.Json.Tests.FSharp.EnumTests

open System
open System.Text.Json
open System.Text.Json.Serialization
open Xunit

[<Flags>]
type BadEnum =
  | ``There's a comma, in my name`` = 1
  | ``There's a comma, even here`` = 2

let badEnum = (BadEnum.``There's a comma, in my name`` ||| BadEnum.``There's a comma, even here``).ToString()

[<Flags>]
type GoodEnum =
  | ``Thereisnocommainmyname_1`` = 1
  | ``Thereisnocommaevenhere_2`` = 2

let goodEnum = (GoodEnum.``Thereisnocommainmyname_1`` ||| GoodEnum.``Thereisnocommaevenhere_2``).ToString()


let options = new JsonSerializerOptions()
options.Converters.Add(new JsonStringEnumConverter())

[<Fact>]
let ``Throw Exception If Enum Contains Special Char`` () =
    Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<BadEnum>(@$"""{badEnum}""", options) |> ignore)

[<Fact>]
let ``Successful deserialize normal enum`` () =
    let actual = JsonSerializer.Deserialize<GoodEnum>(@$"""{goodEnum}""", options)
    Assert.Equal(GoodEnum.Thereisnocommainmyname_1 ||| GoodEnum.Thereisnocommaevenhere_2, actual)

