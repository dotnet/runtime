module System.Text.Json.Tests.FSharp.UnionTests

open System
open System.Reflection
open System.Text.Json
open System.Text.Json.Serialization
open System.Text.Json.Serialization.Metadata
open System.Text.Json.Tests.FSharp.Helpers
open Xunit

// -- Test Types --

type MySingleCaseUnion = MySingleCaseUnion of string
type MyTypeSafeEnum = Label1 | Label2 | Label3
type MyMultiCaseUnion = Point | Circle of radius:float | Rectangle of height:float * length:float

[<Struct>]
type MyStructSingleCaseUnion = MyStructSingleCaseUnion of string
[<Struct>]
type MyStructTypeSafeEnum = StructLabel1 | StructLabel2 | StructLabel3
[<Struct>]
type MyStructMultiCaseUnion = StructPoint | StructCircle of radius:float | StructRectangle of height:float * length:float

type RecursiveUnion = Leaf | Node of left:RecursiveUnion * right:RecursiveUnion

type UnionWithOption = NoValue | SomeValue of value: int option

type UnionWithJsonPropertyName =
    | [<JsonPropertyName("pt")>] CustomPoint
    | [<JsonPropertyName("cir")>] CustomCircle of radius:float

[<JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")>]
type UnionWithCustomDiscriminator =
    | Alpha
    | Beta of x:int * y:string

[<JsonPolymorphic>]
type UnionWithDefaultPolymorphic =
    | Gamma
    | Delta of v:int

type UnionWithMultipleFields = | Multi of x:int * y:string * z:float

[<JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)>]
type UnionWithDisallowUnmapped = Foo | Bar of x:int

[<JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")>]
type UnionWithFieldConflictingDiscriminator = Conflict of Kind:int

// -- Serialization Tests --

[<Fact>]
let ``Fieldless class union case serializes as string`` () =
    let json = JsonSerializer.Serialize(Point)
    Assert.Equal("\"Point\"", json)

[<Fact>]
let ``Fieldless struct union case serializes as string`` () =
    let json = JsonSerializer.Serialize(StructLabel2)
    Assert.Equal("\"StructLabel2\"", json)

[<Fact>]
let ``Single-field class union case serializes as object`` () =
    let json = JsonSerializer.Serialize(Circle 3.14)
    Assert.Equal("""{"$type":"Circle","radius":3.14}""", json)

[<Fact>]
let ``Multi-field class union case serializes as object`` () =
    let json = JsonSerializer.Serialize(Rectangle(10.0, 20.0))
    Assert.Equal("""{"$type":"Rectangle","height":10,"length":20}""", json)

[<Fact>]
let ``Single-case union serializes as object`` () =
    let json = JsonSerializer.Serialize(MySingleCaseUnion "hello")
    Assert.Equal("""{"$type":"MySingleCaseUnion","Item":"hello"}""", json)

[<Fact>]
let ``Struct single-field case serializes as object`` () =
    let json = JsonSerializer.Serialize(StructCircle 2.0)
    Assert.Equal("""{"$type":"StructCircle","radius":2}""", json)

[<Fact>]
let ``Struct multi-field case serializes as object`` () =
    let json = JsonSerializer.Serialize(StructRectangle(5.0, 10.0))
    Assert.Equal("""{"$type":"StructRectangle","height":5,"length":10}""", json)

// -- Deserialization Tests --

[<Fact>]
let ``Fieldless case deserializes from string`` () =
    let result = JsonSerializer.Deserialize<MyMultiCaseUnion>("\"Point\"")
    Assert.Equal(Point, result)

[<Fact>]
let ``Fieldless case deserializes from object with $type`` () =
    let result = JsonSerializer.Deserialize<MyMultiCaseUnion>("""{"$type":"Point"}""")
    Assert.Equal(Point, result)

[<Fact>]
let ``Fieldless struct case deserializes from string`` () =
    let result = JsonSerializer.Deserialize<MyStructTypeSafeEnum>("\"StructLabel3\"")
    Assert.Equal(StructLabel3, result)

[<Fact>]
let ``Single-field case deserializes from object`` () =
    let result = JsonSerializer.Deserialize<MyMultiCaseUnion>("""{"$type":"Circle","radius":3.14}""")
    Assert.Equal(Circle 3.14, result)

[<Fact>]
let ``Multi-field case deserializes from object`` () =
    let result = JsonSerializer.Deserialize<MyMultiCaseUnion>("""{"$type":"Rectangle","height":10.0,"length":20.0}""")
    Assert.Equal(Rectangle(10.0, 20.0), result)

// -- Roundtrip Tests --

let roundtrip<'T> (value: 'T) =
    let json = JsonSerializer.Serialize<'T>(value)
    let result = JsonSerializer.Deserialize<'T>(json)
    Assert.Equal(value, result)

[<Fact>]
let ``Fieldless class case roundtrips`` () = roundtrip Point

[<Fact>]
let ``Single-field class case roundtrips`` () = roundtrip (Circle 3.14)

[<Fact>]
let ``Multi-field class case roundtrips`` () = roundtrip (Rectangle(10.0, 20.0))

[<Fact>]
let ``Type-safe enum roundtrips`` () =
    roundtrip Label1
    roundtrip Label2
    roundtrip Label3

[<Fact>]
let ``Single-case union roundtrips`` () = roundtrip (MySingleCaseUnion "test")

[<Fact>]
let ``Fieldless struct case roundtrips`` () = roundtrip StructPoint

[<Fact>]
let ``Single-field struct case roundtrips`` () = roundtrip (StructCircle 2.0)

[<Fact>]
let ``Multi-field struct case roundtrips`` () = roundtrip (StructRectangle(5.0, 10.0))

[<Fact>]
let ``Struct type-safe enum roundtrips`` () =
    roundtrip StructLabel1
    roundtrip StructLabel2
    roundtrip StructLabel3

[<Fact>]
let ``Struct single-case union roundtrips`` () = roundtrip (MyStructSingleCaseUnion "test")

// -- Recursive Union Tests --

[<Fact>]
let ``Recursive union roundtrips`` () =
    let value = Node(Node(Leaf, Leaf), Leaf)
    let json = JsonSerializer.Serialize(value)
    let result = JsonSerializer.Deserialize<RecursiveUnion>(json)
    Assert.Equal(value, result)

// -- Union with Option Field --

[<Fact>]
let ``Union with option field roundtrips`` () =
    let v1 = SomeValue(Some 42)
    let v2 = SomeValue(None)
    let v3 = NoValue
    for value in [v1; v2; v3] do
        let json = JsonSerializer.Serialize(value)
        let result = JsonSerializer.Deserialize<UnionWithOption>(json)
        Assert.Equal(value, result)

// -- Naming Policy Tests --

[<Fact>]
let ``PropertyNamingPolicy applies to case discriminator names`` () =
    let options = JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase)
    let json = JsonSerializer.Serialize(Circle 1.0, options)
    Assert.Contains("\"$type\":\"circle\"", json)

[<Fact>]
let ``PropertyNamingPolicy applies to field names`` () =
    let options = JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase)
    let json = JsonSerializer.Serialize(MySingleCaseUnion "hello", options)
    Assert.Contains("\"item\":", json)

[<Fact>]
let ``Roundtrip with CamelCase naming policy`` () =
    let options = JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase)
    let value = Rectangle(10.0, 20.0)
    let json = JsonSerializer.Serialize(value, options)
    let result = JsonSerializer.Deserialize<MyMultiCaseUnion>(json, options)
    Assert.Equal(value, result)

// -- JsonPropertyNameAttribute Tests --

[<Fact>]
let ``JsonPropertyName on case overrides discriminator name`` () =
    let json = JsonSerializer.Serialize(CustomPoint)
    Assert.Equal("\"pt\"", json)

[<Fact>]
let ``JsonPropertyName on case with fields overrides discriminator name`` () =
    let json = JsonSerializer.Serialize(CustomCircle 5.0)
    Assert.Contains("\"$type\":\"cir\"", json)

[<Fact>]
let ``JsonPropertyName takes precedence over naming policy`` () =
    let options = JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase)
    let json = JsonSerializer.Serialize(CustomPoint, options)
    Assert.Equal("\"pt\"", json)

[<Fact>]
let ``JsonPropertyName roundtrips`` () =
    let value = CustomCircle 5.0
    let json = JsonSerializer.Serialize(value)
    let result = JsonSerializer.Deserialize<UnionWithJsonPropertyName>(json)
    Assert.Equal(value, result)

// -- Case-Insensitive Tests --

[<Fact>]
let ``Case-insensitive deserialization of case name`` () =
    let options = JsonSerializerOptions(PropertyNameCaseInsensitive = true)
    let result = JsonSerializer.Deserialize<MyMultiCaseUnion>("\"point\"", options)
    Assert.Equal(Point, result)

[<Fact>]
let ``Case-insensitive deserialization of field names`` () =
    let options = JsonSerializerOptions(PropertyNameCaseInsensitive = true)
    let result = JsonSerializer.Deserialize<MyMultiCaseUnion>("""{"$type":"Circle","RADIUS":3.14}""", options)
    Assert.Equal(Circle 3.14, result)

// -- Error Cases --

[<Fact>]
let ``Unknown case name throws JsonException`` () =
    Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<MyMultiCaseUnion>("\"Unknown\"") |> ignore)

[<Fact>]
let ``String form for case with fields uses defaults when RespectRequired is off`` () =
    let result = JsonSerializer.Deserialize<MyMultiCaseUnion>("\"Circle\"")
    Assert.Equal(Circle 0.0, result)

[<Fact>]
let ``String form for case with fields throws when RespectRequired is on`` () =
    let options = JsonSerializerOptions(RespectRequiredConstructorParameters = true)
    Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<MyMultiCaseUnion>("\"Circle\"", options) |> ignore)

[<Fact>]
let ``Missing $type in object throws JsonException with discriminator name`` () =
    let ex = Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<MyMultiCaseUnion>("""{"radius":3.14}""") |> ignore)
    Assert.Contains("$type", ex.Message)

[<Fact>]
let ``Missing custom discriminator in object includes property name in error`` () =
    let ex = Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<UnionWithCustomDiscriminator>("""{"x":1}""") |> ignore)
    Assert.Contains("kind", ex.Message)

[<Fact>]
let ``Invalid token type throws JsonException`` () =
    Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<MyMultiCaseUnion>("42") |> ignore)

// -- Metadata Resolution --

let getUnionTypes () = seq {
    let wrap (t: Type) = [| t :> obj |]
    typeof<MyMultiCaseUnion> |> wrap
    typeof<MyTypeSafeEnum> |> wrap
    typeof<MySingleCaseUnion> |> wrap
    typeof<MyStructMultiCaseUnion> |> wrap
    typeof<MyStructTypeSafeEnum> |> wrap
    typeof<MyStructSingleCaseUnion> |> wrap
}

[<Theory>]
[<MemberData(nameof(getUnionTypes))>]
let ``Union types resolve metadata and converters`` (unionType: Type) =
    let options = JsonSerializerOptions.Default
    Assert.NotNull(options.GetTypeInfo(unionType))
    Assert.NotNull(options.GetConverter(unionType))

// -- Async Serialization --

[<Fact>]
let ``Async serialization roundtrip`` () = async {
    let options = JsonSerializerOptions(DefaultBufferSize = 1)
    let value = Rectangle(10.0, 20.0)
    let! json = JsonSerializer.SerializeAsync(value, options)
    let! result = JsonSerializer.DeserializeAsync<MyMultiCaseUnion>(json, options)
    Assert.Equal(value, result)
}

// -- Custom Discriminator Property Name Tests --

[<Fact>]
let ``Custom discriminator via JsonPolymorphic attribute on fieldless case`` () =
    let json = JsonSerializer.Serialize(Alpha)
    Assert.Equal("\"Alpha\"", json)

[<Fact>]
let ``Custom discriminator via JsonPolymorphic attribute on case with fields`` () =
    let json = JsonSerializer.Serialize(Beta(42, "hello"))
    Assert.Equal("""{"kind":"Beta","x":42,"y":"hello"}""", json)

[<Fact>]
let ``Custom discriminator roundtrip with fields`` () =
    let value = Beta(42, "hello")
    let json = JsonSerializer.Serialize(value)
    let result = JsonSerializer.Deserialize<UnionWithCustomDiscriminator>(json)
    Assert.Equal(value, result)

[<Fact>]
let ``Custom discriminator deserialization from object form`` () =
    let json = """{"kind":"Alpha"}"""
    let result = JsonSerializer.Deserialize<UnionWithCustomDiscriminator>(json)
    Assert.Equal(Alpha, result)

[<Fact>]
let ``Default discriminator still uses $type when no attribute`` () =
    let json = JsonSerializer.Serialize(Circle 5.0)
    Assert.Contains("\"$type\"", json)

[<Fact>]
let ``JsonPolymorphic without TypeDiscriminatorPropertyName uses default $type`` () =
    let json = JsonSerializer.Serialize(Delta 7)
    Assert.Contains("\"$type\"", json)
    Assert.Contains("\"Delta\"", json)
    let result = JsonSerializer.Deserialize<UnionWithDefaultPolymorphic>(json)
    Assert.Equal(Delta 7, result)

// -- Missing Field Default Value Tests --

[<Fact>]
let ``Missing value-type field defaults to zero`` () =
    let json = """{"$type":"Circle"}"""
    let result = JsonSerializer.Deserialize<MyMultiCaseUnion>(json)
    Assert.Equal(Circle 0.0, result)

[<Fact>]
let ``Missing reference-type field defaults to null`` () =
    let json = """{"$type":"Multi","x":42,"z":1.5}"""
    let result = JsonSerializer.Deserialize<UnionWithMultipleFields>(json)
    Assert.Equal(Multi(42, null, 1.5), result)

[<Fact>]
let ``Missing option field defaults to None`` () =
    let json = """{"$type":"SomeValue"}"""
    let result = JsonSerializer.Deserialize<UnionWithOption>(json)
    Assert.Equal(SomeValue None, result)

[<Fact>]
let ``Partial fields present uses defaults for missing`` () =
    let json = """{"$type":"Rectangle","height":10.0}"""
    let result = JsonSerializer.Deserialize<MyMultiCaseUnion>(json)
    Assert.Equal(Rectangle(10.0, 0.0), result)

// -- RespectRequiredConstructorParameters Tests --

[<Fact>]
let ``RespectRequired throws for missing fields in object form`` () =
    let options = JsonSerializerOptions(RespectRequiredConstructorParameters = true)
    let json = """{"$type":"Circle"}"""
    let ex = Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<MyMultiCaseUnion>(json, options) |> ignore)
    Assert.Contains("radius", ex.Message)

[<Fact>]
let ``RespectRequired succeeds when all fields present`` () =
    let options = JsonSerializerOptions(RespectRequiredConstructorParameters = true)
    let json = """{"$type":"Rectangle","height":10.0,"length":20.0}"""
    let result = JsonSerializer.Deserialize<MyMultiCaseUnion>(json, options)
    Assert.Equal(Rectangle(10.0, 20.0), result)

// -- Out-of-Order Discriminator Tests --
// The union converter always accepts out-of-order discriminators regardless of
// AllowOutOfOrderMetadataProperties because it uses ConverterStrategy.Value,
// meaning the JSON is always fully buffered before Read() is called.

[<Fact>]
let ``Out-of-order discriminator succeeds`` () =
    let json = """{"radius":3.14,"$type":"Circle"}"""
    let result = JsonSerializer.Deserialize<MyMultiCaseUnion>(json)
    Assert.Equal(Circle 3.14, result)

[<Fact>]
let ``Out-of-order discriminator in middle succeeds`` () =
    let json = """{"height":10.0,"$type":"Rectangle","length":20.0}"""
    let result = JsonSerializer.Deserialize<MyMultiCaseUnion>(json)
    Assert.Equal(Rectangle(10.0, 20.0), result)

[<Fact>]
let ``Out-of-order fieldless case from object form`` () =
    let json = """{"extra":"ignored","$type":"Point"}"""
    let result = JsonSerializer.Deserialize<MyMultiCaseUnion>(json)
    Assert.Equal(Point, result)

[<Fact>]
let ``Out-of-order with nested object field value`` () =
    let json = """{"value":42,"$type":"SomeValue"}"""
    let result = JsonSerializer.Deserialize<UnionWithOption>(json)
    Assert.Equal(SomeValue(Some 42), result)

[<Fact>]
let ``Out-of-order with RespectRequired succeeds when all fields present`` () =
    let options = JsonSerializerOptions(RespectRequiredConstructorParameters = true)
    let json = """{"radius":3.14,"$type":"Circle"}"""
    let result = JsonSerializer.Deserialize<MyMultiCaseUnion>(json, options)
    Assert.Equal(Circle 3.14, result)

[<Fact>]
let ``Out-of-order with RespectRequired throws for missing fields`` () =
    let options = JsonSerializerOptions(RespectRequiredConstructorParameters = true)
    let json = """{"height":10.0,"$type":"Rectangle"}"""
    let ex = Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<MyMultiCaseUnion>(json, options) |> ignore)
    Assert.Contains("length", ex.Message)

[<Fact>]
let ``String form for multi-field case with RespectRequired throws`` () =
    let options = JsonSerializerOptions(RespectRequiredConstructorParameters = true)
    Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<MyMultiCaseUnion>("\"Rectangle\"", options) |> ignore)

// -- UnmappedMemberHandling Tests --

[<Fact>]
let ``Unknown property is skipped by default`` () =
    let json = """{"$type":"Circle","radius":3.14,"extra":"ignored"}"""
    let result = JsonSerializer.Deserialize<MyMultiCaseUnion>(json)
    Assert.Equal(Circle 3.14, result)

[<Fact>]
let ``Unknown property throws when UnmappedMemberHandling is Disallow via options`` () =
    let options = JsonSerializerOptions(UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)
    let json = """{"$type":"Circle","radius":3.14,"extra":"ignored"}"""
    let ex = Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<MyMultiCaseUnion>(json, options) |> ignore)
    Assert.Contains("extra", ex.Message)

[<Fact>]
let ``Unknown property throws when UnmappedMemberHandling is Disallow via attribute`` () =
    let json = """{"$type":"Bar","x":1,"extra":"ignored"}"""
    let ex = Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<UnionWithDisallowUnmapped>(json) |> ignore)
    Assert.Contains("extra", ex.Message)

[<Fact>]
let ``Fieldless case in object form with extra property throws when Disallow`` () =
    let options = JsonSerializerOptions(UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)
    let json = """{"$type":"Point","extra":"ignored"}"""
    Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<MyMultiCaseUnion>(json, options) |> ignore)

[<Fact>]
let ``Fieldless case in object form with extra property is skipped by default`` () =
    let json = """{"$type":"Point","extra":"ignored"}"""
    let result = JsonSerializer.Deserialize<MyMultiCaseUnion>(json)
    Assert.Equal(Point, result)

[<Fact>]
let ``Discriminator property alone does not trigger unmapped error on fieldless case`` () =
    let options = JsonSerializerOptions(UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)
    let json = """{"$type":"Point"}"""
    let result = JsonSerializer.Deserialize<MyMultiCaseUnion>(json, options)
    Assert.Equal(Point, result)

[<Fact>]
let ``Disallow unmapped does not affect known fields`` () =
    let options = JsonSerializerOptions(UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)
    let json = """{"$type":"Rectangle","height":10,"length":20}"""
    let result = JsonSerializer.Deserialize<MyMultiCaseUnion>(json, options)
    Assert.Equal(Rectangle(10.0, 20.0), result)

// -- Field/Discriminator Conflict Tests --

[<Fact>]
let ``Field name matching discriminator case-insensitively throws when case-insensitive`` () =
    let options = JsonSerializerOptions(PropertyNameCaseInsensitive = true)
    let ex = Assert.ThrowsAny<Exception>(fun () -> JsonSerializer.Deserialize<UnionWithFieldConflictingDiscriminator>("""{"kind":"Conflict","Kind":1}""", options) |> ignore)
    let innerEx =
        match ex with
        | :? System.Reflection.TargetInvocationException as tie -> tie.InnerException
        | _ -> ex
    Assert.IsType<InvalidOperationException>(innerEx) |> ignore
    Assert.Contains("Kind", innerEx.Message)
    Assert.Contains("kind", innerEx.Message)

// -- JsonTypeInfo Verification Tests --

[<Theory>]
[<MemberData(nameof(getUnionTypes))>]
let ``Union type info has no PolymorphismOptions`` (unionType: Type) =
    let options = JsonSerializerOptions.Default
    let typeInfo = options.GetTypeInfo(unionType)
    Assert.Null(typeInfo.PolymorphismOptions)

[<Theory>]
[<MemberData(nameof(getUnionTypes))>]
let ``Union type info has Kind None`` (unionType: Type) =
    let options = JsonSerializerOptions.Default
    let typeInfo = options.GetTypeInfo(unionType)
    Assert.Equal(JsonTypeInfoKind.None, typeInfo.Kind)

[<Fact>]
let ``Union with JsonPolymorphic attribute has no PolymorphismOptions`` () =
    let options = JsonSerializerOptions.Default
    let typeInfo = options.GetTypeInfo(typeof<UnionWithCustomDiscriminator>)
    Assert.Null(typeInfo.PolymorphismOptions)

[<Fact>]
let ``Union with default JsonPolymorphic attribute has no PolymorphismOptions`` () =
    let options = JsonSerializerOptions.Default
    let typeInfo = options.GetTypeInfo(typeof<UnionWithDefaultPolymorphic>)
    Assert.Null(typeInfo.PolymorphismOptions)
