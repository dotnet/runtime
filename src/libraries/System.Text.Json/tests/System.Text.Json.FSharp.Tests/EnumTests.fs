module System.Text.Json.Tests.FSharp.EnumTests

open System
open System.Reflection
open System.Text.Json
open System.Text.Json.Serialization
open Xunit

[<Flags>]
type BadEnum =
  | ``There's a comma, in my name`` = 1
  | ``There's a comma, even here`` = 2
  | ``ThisisagoodEnumValue`` = 4

let badEnum = BadEnum.``There's a comma, in my name`` ||| BadEnum.``There's a comma, even here``
let badEnumJsonStr = @$"""{badEnum.ToString()}"""

let badEnumWithGoodValue = BadEnum.ThisisagoodEnumValue
let badEnumWithGoodValueJsonStr = @$"""{badEnumWithGoodValue.ToString()}"""

[<Flags>]
type GoodEnum =
  | Thereisnocommainmyname_1 = 1
  | Thereisnocommaevenhere_2 = 2

let goodEnum = GoodEnum.Thereisnocommainmyname_1 ||| GoodEnum.Thereisnocommaevenhere_2
let goodEnumJsonStr = @$"""{goodEnum.ToString()}"""

let options = new JsonSerializerOptions()
options.Converters.Add(new JsonStringEnumConverter())

[<Fact>]
let ``Deserialize With Exception If Enum Contains Special Char`` () =
    Assert.Throws<TargetInvocationException>(fun () -> JsonSerializer.Deserialize<BadEnum>(badEnumJsonStr, options) |> ignore)

[<Fact>]
let ``Serialize With Exception If Enum Contains Special Char`` () =
    Assert.Throws<TargetInvocationException>(fun () ->  JsonSerializer.Serialize(badEnum, options) |> ignore)

[<Fact>]
let ``Successful Deserialize Normal Enum`` () =
    let actual = JsonSerializer.Deserialize<GoodEnum>(goodEnumJsonStr, options)
    Assert.Equal(GoodEnum.Thereisnocommainmyname_1 ||| GoodEnum.Thereisnocommaevenhere_2, actual)

[<Fact>]
let ``Fail Deserialize Good Value Of Bad Enum Type`` () =
    Assert.Throws<TargetInvocationException>(fun () -> JsonSerializer.Deserialize<BadEnum>(badEnumWithGoodValueJsonStr, options) |> ignore)

[<Fact>]
let ``Fail Serialize Good Value Of Bad Enum Type`` () =
    Assert.Throws<TargetInvocationException>(fun () ->  JsonSerializer.Serialize(badEnumWithGoodValue, options) |> ignore)
