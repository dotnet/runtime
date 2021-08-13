module System.Text.Json.Tests.FSharp.RecordTests

open System.Text.Json
open System.Text.Json.Serialization
open System.Text.Json.Tests.FSharp.Helpers
open Xunit

type MyRecord =
    {
        Name : string
        MiddleName : string option
        LastName : string
        Age : int
        IsActive : bool
    }
with
    static member Value = { Name = "John" ; MiddleName = None ; LastName = "Doe" ; Age = 34 ; IsActive = true }
    static member ExpectedJson = """{"Name":"John","MiddleName":null,"LastName":"Doe","Age":34,"IsActive":true}"""

[<Fact>]
let ``Support F# record serialization``() =
    let actualJson = JsonSerializer.Serialize(MyRecord.Value)
    Assert.Equal(MyRecord.ExpectedJson, actualJson)

[<Fact>]
let ``Support F# record deserialization``() =
    let result = JsonSerializer.Deserialize<MyRecord>(MyRecord.ExpectedJson)
    Assert.Equal(MyRecord.Value, result)

[<Struct>]
type MyStructRecord =
    {
        Name : string
        MiddleName : string option
        LastName : string
        Age : int
        IsActive : bool
    }
with
    static member Value = { Name = "John" ; MiddleName = None ; LastName = "Doe" ; Age = 34 ; IsActive = true }
    static member ExpectedJson = """{"Name":"John","MiddleName":null,"LastName":"Doe","Age":34,"IsActive":true}"""

[<Fact>]
let ``Support F# struct record serialization``() =
    let actualJson = JsonSerializer.Serialize(MyStructRecord.Value)
    Assert.Equal(MyStructRecord.ExpectedJson, actualJson)

[<Fact>]
let ``Support F# struct record deserialization``() =
    let result = JsonSerializer.Deserialize<MyStructRecord>(MyStructRecord.ExpectedJson)
    Assert.Equal(MyStructRecord.Value, result)
