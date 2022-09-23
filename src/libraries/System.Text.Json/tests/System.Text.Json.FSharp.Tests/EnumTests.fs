module System.Text.Json.Tests.FSharp.EnumTests

open System
open System.Text.Json
open Xunit

[<Flags>]
type MyEnum =
  | ``There's a comma, in my name`` = 1
  | ``There's a comma, even here`` = 2

let enum = (MyEnum.``There's a comma, in my name`` ||| MyEnum.``There's a comma, even here``).ToString()

[<Fact>]
let ``Throw Exception If Enum Contains Comma`` () =
    Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<MyEnum>(enum) |> ignore)


