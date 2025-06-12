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
let badEnumJsonStr = $"\"{badEnum}\""

let badEnumWithGoodValue = BadEnum.ThisisagoodEnumValue
let badEnumWithGoodValueJsonStr = $"\"{badEnumWithGoodValue}\""

[<Flags>]
type GoodEnum =
  | Thereisnocommainmyname_1 = 1
  | Thereisnocommaevenhere_2 = 2

let goodEnum = GoodEnum.Thereisnocommainmyname_1 ||| GoodEnum.Thereisnocommaevenhere_2
let goodEnumJsonStr = $"\"{goodEnum}\""

let options = new JsonSerializerOptions()
options.Converters.Add(new JsonStringEnumConverter())

let optionsDisableNumeric = new JsonSerializerOptions()
optionsDisableNumeric.Converters.Add(new JsonStringEnumConverter(null, false))

[<Fact>]
let ``Deserialize With Exception If Enum Contains Special Char`` () =
    let ex = Assert.Throws<InvalidOperationException>(fun () -> JsonSerializer.Deserialize<BadEnum>(badEnumJsonStr, options) |> ignore)
    Assert.Contains("Enum type 'BadEnum' uses unsupported identifier 'There's a comma, in my name'.", ex.Message)


[<Fact>]
let ``Serialize With Exception If Enum Contains Special Char`` () =
    let ex = Assert.Throws<InvalidOperationException>(fun () ->  JsonSerializer.Serialize(badEnum, options) |> ignore)
    Assert.Contains("Enum type 'BadEnum' uses unsupported identifier 'There's a comma, in my name'.", ex.Message)

[<Fact>]
let ``Successful Deserialize Normal Enum`` () =
    let actual = JsonSerializer.Deserialize<GoodEnum>(goodEnumJsonStr, options)
    Assert.Equal(GoodEnum.Thereisnocommainmyname_1 ||| GoodEnum.Thereisnocommaevenhere_2, actual)

[<Fact>]
let ``Fail Deserialize Good Value Of Bad Enum Type`` () =
    let ex = Assert.Throws<InvalidOperationException>(fun () -> JsonSerializer.Deserialize<BadEnum>(badEnumWithGoodValueJsonStr, options) |> ignore)
    Assert.Contains("Enum type 'BadEnum' uses unsupported identifier 'There's a comma, in my name'.", ex.Message)

[<Fact>]
let ``Fail Serialize Good Value Of Bad Enum Type`` () =
    let ex = Assert.Throws<InvalidOperationException>(fun () ->  JsonSerializer.Serialize(badEnumWithGoodValue, options) |> ignore)
    Assert.Contains("Enum type 'BadEnum' uses unsupported identifier 'There's a comma, in my name'.", ex.Message)

type NumericLabelEnum =
  | ``1`` = 1
  | ``2`` = 2
  | ``3`` = 4

[<Theory>]
[<InlineData("\"4\"")>]
[<InlineData("\"5\"")>]
[<InlineData("\"+1\"")>]
[<InlineData("\"-1\"")>]
[<InlineData("\"  +1  \"")>]
[<InlineData("\"  -1  \"")>]
let ``Fail Deserialize Numeric label Of Enum When Disallow Integer Values`` (numericValueJsonStr: string) =
    Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<NumericLabelEnum>(numericValueJsonStr, optionsDisableNumeric) |> ignore)

[<Theory>]
[<InlineData("\"1\"", NumericLabelEnum.``1``)>]
[<InlineData("\"2\"", NumericLabelEnum.``2``)>]
[<InlineData("\"3\"", NumericLabelEnum.``3``)>]
[<InlineData("\"  1  \"", NumericLabelEnum.``1``)>]
let ``Successful Deserialize Numeric label Of Enum When Disallow Integer Values If Matching Integer Label`` (numericValueJsonStr: string, expectedValue: NumericLabelEnum) =
    let actual = JsonSerializer.Deserialize<NumericLabelEnum>(numericValueJsonStr, optionsDisableNumeric)
    Assert.Equal(expectedValue, actual)
    
[<Theory>]
[<InlineData("\"1\"", NumericLabelEnum.``1``)>]
[<InlineData("\"2\"", NumericLabelEnum.``2``)>]
let ``Successful Deserialize Numeric label Of Enum When Allowing Integer Values`` (numericValueJsonStr: string, expectedEnumValue: NumericLabelEnum) =
    let actual = JsonSerializer.Deserialize<NumericLabelEnum>(numericValueJsonStr, options)
    Assert.Equal(expectedEnumValue, actual)
    
[<Theory>]
[<InlineData(-1)>]
[<InlineData(0)>]
[<InlineData(4)>]
[<InlineData(Int32.MaxValue)>]
[<InlineData(Int32.MinValue)>]
let ``Successful Deserialize Numeric label Of Enum But as Underlying value When Allowing Integer Values`` (numericValue: int) =
    let actual = JsonSerializer.Deserialize<NumericLabelEnum>($"\"{numericValue}\"", options)
    Assert.Equal(LanguagePrimitives.EnumOfValue numericValue, actual)

type CharEnum =
  | A = 'A'
  | B = 'B'
  | C = 'C'

[<Fact>]
let ``Serializing char enums throws NotSupportedException`` () =
    Assert.Throws<NotSupportedException>(fun () -> JsonSerializer.Serialize(CharEnum.A) |> ignore) |> ignore
    Assert.Throws<NotSupportedException>(fun () -> JsonSerializer.Serialize(CharEnum.A, options) |> ignore) |> ignore
    Assert.Throws<NotSupportedException>(fun () -> JsonSerializer.Deserialize<CharEnum>("0") |> ignore) |> ignore
    Assert.Throws<NotSupportedException>(fun () -> JsonSerializer.Deserialize<CharEnum>("\"A\"", options) |> ignore) |> ignore
