module System.Text.Json.Tests.FSharp.EnumTests

open System
open System.Text.Json
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

[<Fact>]
let ``Throw Exception If Enum Contains Special Char`` () =
    Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<BadEnum>(badEnum) |> ignore)

[<Fact>]
let ``Successful deserialize normal enum`` () =
    Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<BadEnum>(goodEnum) |> ignore)
