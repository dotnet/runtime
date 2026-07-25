module System.Text.Json.Tests.FSharp.UnionTests

open System
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

[<AllowNullLiteral>]
type RefObj(name: string) =
    member _.Name = name

type UnionWithRefField = WithRef of obj:RefObj | WithoutRef

type WrapperWithSharedRef = { First: UnionWithRefField; Second: UnionWithRefField }

// UseNullAsTrueValue DU (custom option-like type where the None case is null at runtime)
[<CompilationRepresentation(CompilationRepresentationFlags.UseNullAsTrueValue)>]
type UseNullUnion<'T> = UseNullNone | UseNullSome of value:'T

// Struct DU with overlapping fields (cases sharing field name+type)
[<Struct>]
type StructOverlapUnion = OverlapNothing | OverlapIntA of x:int | OverlapIntB of x:int * y:string

// Struct DU with unit-of-measure erased overlapping fields
[<Measure>] type meter

[<Struct>]
type StructMeasureOverlapUnion = MeasureNothing | MeasureScalar of x:int | MeasureRect of x:int<meter> * y:int<meter>

// Mutually recursive DU types
type TreeNode = TreeLeaf of value:int | TreeBranch of children:Forest
and Forest = EmptyForest | NonEmptyForest of head:TreeNode * tail:Forest

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
// AllowOutOfOrderMetadataProperties because it buffers the entire JSON payload
// before invoking Read() (using read-ahead/RequiresReadAhead).

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

// -- AllowDuplicateProperties Tests --

[<Fact>]
let ``Duplicate field throws when AllowDuplicateProperties is false`` () =
    let options = JsonSerializerOptions(AllowDuplicateProperties = false)
    let json = """{"$type":"Circle","radius":1.0,"radius":2.0}"""
    Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<MyMultiCaseUnion>(json, options) |> ignore)

[<Fact>]
let ``Duplicate discriminator always throws`` () =
    let json = """{"$type":"Circle","$type":"Point","radius":3.14}"""
    Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<MyMultiCaseUnion>(json) |> ignore)

[<Fact>]
let ``Duplicate discriminator in fieldless case always throws`` () =
    let json = """{"$type":"Point","$type":"Circle"}"""
    Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<MyMultiCaseUnion>(json) |> ignore)

[<Fact>]
let ``Duplicate field is last-wins when AllowDuplicateProperties is true`` () =
    let json = """{"$type":"Circle","radius":1.0,"radius":2.0}"""
    let result = JsonSerializer.Deserialize<MyMultiCaseUnion>(json)
    Assert.Equal(Circle 2.0, result)

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
let ``Union type info has Kind Object`` (unionType: Type) =
    let options = JsonSerializerOptions.Default
    let typeInfo = options.GetTypeInfo(unionType)
    Assert.Equal(JsonTypeInfoKind.Object, typeInfo.Kind)

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

// -- ReferenceHandler Tests --

[<Fact>]
let ``Shared reference in union fields serializes with $id and $ref`` () =
    let options = JsonSerializerOptions(ReferenceHandler = ReferenceHandler.Preserve)
    let shared = RefObj("shared")
    let value = { First = WithRef shared; Second = WithRef shared }
    let json = JsonSerializer.Serialize(value, options)
    Assert.Equal("""{"$id":"1","First":{"$id":"2","$type":"WithRef","obj":{"$id":"3","Name":"shared"}},"Second":{"$id":"4","$type":"WithRef","obj":{"$ref":"3"}}}""", json)

[<Fact>]
let ``Union with $ref deserializes correctly`` () =
    let options = JsonSerializerOptions(ReferenceHandler = ReferenceHandler.Preserve)
    let json = """{"$id":"1","$type":"Node","left":{"$id":"2","$type":"Leaf"},"right":{"$ref":"2"}}"""
    let result = JsonSerializer.Deserialize<RecursiveUnion>(json, options)
    Assert.Equal(Node(Leaf, Leaf), result)

[<Fact>]
let ``Fieldless union with $id deserializes from object form`` () =
    let options = JsonSerializerOptions(ReferenceHandler = ReferenceHandler.Preserve)
    let json = """{"$id":"1","$type":"Point"}"""
    let result = JsonSerializer.Deserialize<MyMultiCaseUnion>(json, options)
    Assert.Equal(Point, result)

[<Fact>]
let ``Recursive union serializes with $id metadata using ReferenceHandler.Preserve`` () =
    let options = JsonSerializerOptions(ReferenceHandler = ReferenceHandler.Preserve)
    let value = Node(Node(Leaf, Leaf), Leaf)
    let json = JsonSerializer.Serialize(value, options)
    Assert.Equal("""{"$id":"1","$type":"Node","left":{"$id":"2","$type":"Node","left":{"$id":"3","$type":"Leaf"},"right":{"$ref":"3"}},"right":{"$ref":"3"}}""", json)

[<Fact>]
let ``Fieldless union serializes as object with $id using ReferenceHandler.Preserve`` () =
    let options = JsonSerializerOptions(ReferenceHandler = ReferenceHandler.Preserve)
    let json = JsonSerializer.Serialize(Point, options)
    Assert.Equal("""{"$id":"1","$type":"Point"}""", json)

[<Fact>]
let ``Fieldless union without Preserve still serializes as string`` () =
    let json = JsonSerializer.Serialize(Point)
    Assert.Equal("\"Point\"", json)

[<Fact>]
let ``Struct union does not emit $id with ReferenceHandler.Preserve`` () =
    let options = JsonSerializerOptions(ReferenceHandler = ReferenceHandler.Preserve)
    let json = JsonSerializer.Serialize(StructCircle 3.14, options)
    Assert.Equal("""{"$type":"StructCircle","radius":3.14}""", json)

[<Fact>]
let ``Fieldless struct union serializes as string with ReferenceHandler.Preserve`` () =
    let options = JsonSerializerOptions(ReferenceHandler = ReferenceHandler.Preserve)
    let json = JsonSerializer.Serialize(StructPoint, options)
    Assert.Equal("\"StructPoint\"", json)

// -- UseNullAsTrueValue Tests --

[<Fact>]
let ``UseNullAsTrueValue None case serializes as null`` () =
    let json = JsonSerializer.Serialize<UseNullUnion<int>>(UseNullNone)
    Assert.Equal("null", json)

[<Fact>]
let ``UseNullAsTrueValue Some case roundtrips`` () =
    let value = UseNullSome 42
    let json = JsonSerializer.Serialize(value)
    Assert.Equal("""{"$type":"UseNullSome","value":42}""", json)
    let result = JsonSerializer.Deserialize<UseNullUnion<int>>(json)
    Assert.Equal(value, result)

[<Fact>]
let ``UseNullAsTrueValue null JSON deserializes as None case`` () =
    let result = JsonSerializer.Deserialize<UseNullUnion<int>>("null")
    Assert.True(obj.ReferenceEquals(result :> obj, null))

[<Fact>]
let ``UseNullAsTrueValue string form deserializes as None case`` () =
    let result = JsonSerializer.Deserialize<UseNullUnion<int>>(""" "UseNullNone" """)
    Assert.True(obj.ReferenceEquals(result :> obj, null))

// -- Struct DU with Overlapping Fields Tests --

[<Fact>]
let ``Struct DU with overlapping fields roundtrips`` () =
    let a = OverlapIntA 42
    let jsonA = JsonSerializer.Serialize(a)
    Assert.Equal("""{"$type":"OverlapIntA","x":42}""", jsonA)
    let resultA = JsonSerializer.Deserialize<StructOverlapUnion>(jsonA)
    Assert.Equal(a, resultA)

    let b = OverlapIntB(99, "hello")
    let jsonB = JsonSerializer.Serialize(b)
    Assert.Equal("""{"$type":"OverlapIntB","x":99,"y":"hello"}""", jsonB)
    let resultB = JsonSerializer.Deserialize<StructOverlapUnion>(jsonB)
    Assert.Equal(b, resultB)

    let n = OverlapNothing
    let jsonN = JsonSerializer.Serialize(n)
    Assert.Equal("\"OverlapNothing\"", jsonN)
    let resultN = JsonSerializer.Deserialize<StructOverlapUnion>(jsonN)
    Assert.Equal(n, resultN)

[<Fact>]
let ``Struct DU with measure-erased overlapping fields roundtrips`` () =
    let a = MeasureScalar 42
    let jsonA = JsonSerializer.Serialize(a)
    let resultA = JsonSerializer.Deserialize<StructMeasureOverlapUnion>(jsonA)
    Assert.Equal(a, resultA)

    let b = MeasureRect(LanguagePrimitives.Int32WithMeasure<meter> 10, LanguagePrimitives.Int32WithMeasure<meter> 20)
    let jsonB = JsonSerializer.Serialize(b)
    let resultB = JsonSerializer.Deserialize<StructMeasureOverlapUnion>(jsonB)
    Assert.Equal(b, resultB)

// -- Mutually Recursive DU Types Tests --

[<Fact>]
let ``Mutually recursive DU types roundtrip`` () =
    let tree = TreeBranch(NonEmptyForest(TreeLeaf 1, NonEmptyForest(TreeBranch(NonEmptyForest(TreeLeaf 2, EmptyForest)), EmptyForest)))
    let json = JsonSerializer.Serialize(tree)
    let result = JsonSerializer.Deserialize<TreeNode>(json)
    Assert.Equal(tree, result)

    let forest = NonEmptyForest(TreeLeaf 3, EmptyForest)
    let jsonF = JsonSerializer.Serialize(forest)
    let resultF = JsonSerializer.Deserialize<Forest>(jsonF)
    Assert.Equal(forest, resultF)

// -- F# Collection Regression Test --

[<Fact>]
let ``F# list serializes as JSON array not as DU`` () =
    let values = [1; 2; 3]
    let json = JsonSerializer.Serialize(values)
    Assert.Equal("[1,2,3]", json)
    let result = JsonSerializer.Deserialize<int list>(json)
    Assert.True((values = result))
