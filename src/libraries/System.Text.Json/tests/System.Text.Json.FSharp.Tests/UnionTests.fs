module System.Text.Json.Tests.FSharp.UnionTests

open System
open System.Text.Json
open System.Text.Json.Serialization
open Xunit

type MySingleCaseUnion = MySingleCaseUnion of string
type MyTypeSafeEnum = Label1 | Label2 | Label3
type MyMultiCaseUnion = Point | Circle of radius:float | Rectangle of height:float * length:float

[<Struct>]
type MyStructSingleCaseUnion = MyStructSingleCaseUnion of string
[<Struct>]
type MyStructTypeSafeEnum = StructLabel1 | StructLabel2 | StructLabel3
[<Struct>]
type MyStructMultiCaseUnion = StructPoint | StructCircle of radius:float | StructRectangle of height:float * length:float

let getUnionValues() = seq {
    let wrap value = [| value :> obj |]

    MySingleCaseUnion "value" |> wrap
    Label1 |> wrap
    Circle 1. |> wrap

    MyStructSingleCaseUnion "value" |> wrap
    StructLabel2 |> wrap
    StructCircle 1. |> wrap        
}

[<Theory>]
[<MemberData(nameof(getUnionValues))>]
let ``Union serialization should throw NotSupportedException`` (value : 'T) =
    Assert.Throws<NotSupportedException>(fun () -> JsonSerializer.Serialize(value) |> ignore)

[<Theory>]
[<MemberData(nameof(getUnionValues))>]
let ``Union deserialization should throw NotSupportedException`` (value : 'T) =
    Assert.Throws<NotSupportedException>(fun () -> JsonSerializer.Deserialize<'T>("{}") |> ignore)
