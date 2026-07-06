## Proposal: generic witness variable for generic bridge and extraction

We already have a lot of discussions about generic bridge in https://github.com/dotnet/csharplang/discussions/6308, which is a feature that allows users to bridge a generic parameter to a more constrained generic parameter and pass it down when the constraint is fulfilled. There was also a bridging constraint exploration in #97084.

Additionally, there're also needs for extracting a type argument from a given type parameter at a specific arity and pass the extracted type argument down to instantiate another generic type or generic method.

The concept is like:

```cs
void Foo<T>()
{
    if (T is List<var U> where U : INumber<U>)
    {
        Bar<U>();
    }
}

void Bar<T>() where T : INumber<T>
{
    Console.WriteLine(typeof(T));
}
```

When you call `Foo<List<int>>`, it will print `System.Int32`; when you call `Foo<List<string>>`, nothing will be printed.

## Design

### Type witness variable

I'm using type witness variable for the matched type argument that is being extracted. The pseudo IL representation I will use for denoting a witness variable below is `^`. A witness variable is not a normal type generic parameter (!0) or method generic parameter (!!0). It is a type variable introduced by a successful type match. Each method or type can come with a witness table that describes the witness variable, the constraint and where the witness variable coming from.

#### Simple case

Here I'm using `.witness` to create a witness definition with a generic constraint `where U : INumber<U>`:

```il
.method static void Foo<T>() cil managed
{
    .witness W0<(class [System.Runtime]System.Numerics.INumber`1<^0>) U>
           !!0 : class [System.Collections]System.Collections.Generic.List`1<^0>
}
```

Then the defined witness `W0` can be used for matching a type `T` and extracting the first type argument into `U`.

Inside the .witness declaration: `^0` means the first witness variable owned by `W0`, which is `U`; outside the .witness declaration, the witness should be referenced with its owner: `^W0.U` or by index: `^W0.0`.

So with the above stuff, ``!!0 : class [System.Collections]System.Collections.Generic.List`1<^0>`` means "try to match the actual type of `!!0` against `List<^0>` and if the match succeeds, bind `^0` to the first generic argument of `List<>`".

If `T` is `List<int>`, it will bind `W0.U = int`.

#### Multiple nested type arguments

For a type parameter that has more than one nested type argument, for example extracting the second type argument from `Dictionary<,>`, we still declare all witness positions:

```il
.method static void Foo<T>() cil managed
{
    .witness W0<U, (class [System.Runtime]System.Numerics.INumber`1<^1>) V>
           !!0 : class [System.Collections]System.Collections.Generic.Dictionary`2<^0, ^1>
}
```

here both `U` and `^0` are unused, but we still assign an identifier for them anyway.

You can also have multiple generic constraints like

```il
.witness W0<
    (class [System.Runtime]System.Numerics.INumber`1<^0>)
    (class [System]System.IComparable`1<^0>)
    U
>
       !!0 : class [System.Collections]System.Collections.Generic.List`1<^0>
```

#### Repeated witness variables

If the same witness variable appears more than once in the pattern, then all positions must match the same type.

```il
.method static void Foo<T>() cil managed
{
    .witness W0<U> !!0 : class ValueTuple`2<^0, ^0>
}
```

This matches `(int, int)`, but not `(int, long)`. The equality here should be exact runtime type identity, not assignability.

#### Nested extraction

Witness variables can also appear inside nested generic instantiations:

```il
.method static void Foo<T>() cil managed
{
    .witness W0<(class [System.Runtime]System.Numerics.INumber`1<^0>) U>
           !!0 : class [System.Collections]System.Collections.Generic.Dictionary`2<
                    string,
                    class [System.Collections]System.Collections.Generic.List`1<^0>
                 >
}
```

This means `Dictionary<string, List<U>>`, so if `T` is `Dictionary<string, List<int>>`, the match binds `W0.U = int`.

### Type match intrinsic method

Introduce a type match intrinsic method to match a type against a witness rule:

```cs
namespace System.Runtime.CompilerServices
{
    public static class TypeWitness
    {
        public static bool TryMatchType(
            MethodDesc* methodDesc,
            void* genericContext,
            mdToken witnessToken,
            TypeMatchKind matchKind,
            TypeHandle** result);
    }

    public enum TypeMatchKind { Exact, BaseClass, Interface };
}
```

```il
call bool [System.Runtime]System.Runtime.CompilerServices.TypeWitness::TryMatchType(currentMethod, genericContext, W0, Exact, &b)
brfalse.s NO_MATCH

...

NO_MATCH:
    ret
```

It performs a type match with the given witness rule `W0`. When it succeeds, it binds the extraction result into `W0.*`; otherwise, no witness variable from `W0` is considered available. Here the `exact` stands for exact type match.

Even though this is written as a method call, `W0` is a witness rule token passed into the intrinsic.

For example:

```il
.method static void Foo<T>() cil managed
{
    .witness W0<(class [System.Runtime]System.Numerics.INumber`1<^0>) U>
           !!0 : class [System.Collections]System.Collections.Generic.List`1<^0>

    call bool [System.Runtime]System.Runtime.CompilerServices.TypeWitness::TryMatchType(currentMethod, genericContext, W0, Exact, &b)
    brfalse.s NO_MATCH

    call void Program::Bar<^W0.U>()
    ret

NO_MATCH:
    ret
}
```

In the `List<>` case, `W0.U` will be `int` if `T` is `List<int>`.

In the `Dictionary<,>` case:

```il
.method static void Foo<T>() cil managed
{
    .witness W0<U, (class [System.Runtime]System.Numerics.INumber`1<^1>) V>
           !!0 : class [System.Collections]System.Collections.Generic.Dictionary`2<^0, ^1>

    call bool [System.Runtime]System.Runtime.CompilerServices.TypeWitness::TryMatchType(currentMethod, genericContext, W0, Exact, &b)
    brfalse.s NO_MATCH

    call void Program::Bar<^W0.V>()
    ret

NO_MATCH:
    ret
}
```

`W0.U` will be `string` and `W0.V` will be `int` if `T` is `Dictionary<string, int>`.

The match also validates witness variable constraints. So if `U` has the constraint `where U : INumber<U>`, then `Foo<List<string>>()` fails during `TryMatchType`, not when calling `Bar<U>`.

#### Lookup on base class or interfaces

For base classes match:

```il
.witness W0<U>
       !!0 : class [System.Collections]System.Collections.Generic.List`1<^0>
...
    call bool [System.Runtime]System.Runtime.CompilerServices.TypeWitness::TryMatchType(currentMethod, genericContext, W0, BaseClass, &b)
```

which finds the first base type of `T` that matches `List<U>`. So if we have `class MyList<T> : List<T> {}`, `Foo<MyList<int>>()` binds `W0.U = int`.

And for interfaces:

```il
.witness W0<U>
       !!0 : class [System.Collections]System.Collections.Generic.IEnumerable`1<^0>...
...
    call bool [System.Runtime]System.Runtime.CompilerServices.TypeWitness::TryMatchType(currentMethod, genericContext, W0, Interface, &b)
```

which find an implemented interface of `T` that matches `IEnumerable<U>`. So if `T` is `List<int>`, it can bind `W0.U = int` through `IEnumerable<int>`.

### Instantiation

With the type match and type witness variable, we can finally instantiate a generic type or method:

```il
call void Program::Bar<^W0.U>()
```

This is a witness-dependent generic method instantiation. It is not an ordinary eagerly valid `MethodSpec`, because one of the generic arguments comes from a witness binding instead of a normal type / method generic parameter.

For example:

```il
.method static void Foo<T>() cil managed
{
    .witness W0<(class [System.Runtime]System.Numerics.INumber`1<^0>) U>
           !!0 : class [System.Collections]System.Collections.Generic.List`1<^0>

    call bool [System.Runtime]System.Runtime.CompilerServices.TypeWitness::TryMatchType(currentMethod, genericContext, W0, Exact, &b)
    brfalse.s NO_MATCH

    call void Program::Bar<^W0.U>()
    ret

NO_MATCH:
    ret
}
```

If this is instantiated as `Foo<List<int>>()`, then the call becomes `Bar<int>()`.

If this is instantiated as `Foo<List<string>>()`, then the `TryMatchType` fails, so the call is skipped.

A witness variable can also be used to instantiate a generic type:

```il
newobj instance void class Box`1<^W0.U>::.ctor()
```

or inside another generic instantiation:

```il
call void Program::Baz<class [System.Collections]System.Collections.Generic.List`1<^W0.U>>()
```

There can be cases where multiple type match when matching against interfaces, in which case it can take the first eligible interface in the type declaration. Users can adjust the order of interface to match their expectations. 

For example,

```cs
if (T is IEnumerable<var U>) {}
```

when T implements both `IEnumerable<int>` and `IEnumerable<string>`.

## Metadata

### Witness table

Introduce a new metadata table:

```text
WitnessDef
    Owner      MethodDef or TypeDef
    Number     uint16
    Name       string
    Source     TypeSpec
    Pattern    TypeSpec
```

`Owner` describes where this witness rule is declared. For a method-local witness rule, the owner is the `MethodDef`. For a type-level witness rule, the owner is the `TypeDef`.

`Number` is the owner-local ordinal of the witness rule.

`Name` is the textual name of the witness rule, for example `W0`.

`Source` is a `TypeSpec` describing the type being matched, eg. `!!0`.

`Pattern` is a `TypeSpec` describing the type pattern. The pattern may contain witness variables, for example:

```text
class [System.Collections]System.Collections.Generic.List`1<^0>
class [System.Collections]System.Collections.Generic.Dictionary`2<^0, ^1>
```

This requires extending `TypeSpec` signatures so that they can reference witness-owned parameters.

### Witness variables

Witness variables should be represented like generic parameters, simliar to `GenericParam` rows:

```text
WitnessParam
    Owner    WitnessDef
    Number   uint16
    Flags    GenericParamAttributes-like
    Name     string

WitnessParamConstraint
    Owner      WitnessParam
    Constraint TypeSpec
```

The witness variables are stored as `WitnessParam` rows.

For example:

```il
.witness W0<U, V>
       !!0 : class Dictionary`2<^0, ^1>
```

uses:

```text
WitnessDef #1
    Name     W0

WitnessParam #1
    Owner    WitnessDef #1
    Number   0
    Name     U

WitnessParam #2
    Owner    WitnessDef #1
    Number   1
    Name     V
```

So `U` and `V` are stored as the names of `WitnessParam` rows. The numbers `0` and `1` are what signature blobs use to reference them.

### Signature encoding

Existing signatures can already reference normal type and method generic parameters:

```text
ELEMENT_TYPE_VAR     !0
ELEMENT_TYPE_MVAR    !!0
```

This feature needs a new signature encoding for witness-owned parameters. Let's say `ELEMENT_TYPE_WVAR`. A witness variable needs two pieces of information:

1. Which `WitnessDef` owns the variable
2. Which witness parameter under that `WitnessDef` is being referenced

So the encoding can be:

```text
ELEMENT_TYPE_WVAR <WitnessDef>, <Number>
```

For example:

```il
.witness W0<U>
       !!0 : class List`1<^0>
```

Inside the `.witness` declaration, the `^0` means `ELEMENT_TYPE_WVAR WitnessDef(W0), 0`.

For the earlier `Dictionary` example, the pattern signature would contain:

```text
TypeSpec
    Signature class Dictionary`2<
        ELEMENT_TYPE_WVAR WitnessDef #1, 0,
        ELEMENT_TYPE_WVAR WitnessDef #1, 1
    >
```

Outside the `.witness` declaration, `ELEMENT_TYPE_WVAR` should only appear in a witness-dependent signature that is dominated by a successful type match intrinsic for that witness.

For example:

```il
call void Program::Bar<^W0.U>()
```

is represented as a normal `MethodSpec`, but its instantiation contains:

```text
ELEMENT_TYPE_WVAR WitnessDef(W0), 0
```

### Full metadata example

For:

```il
.method static void Foo<T>() cil managed
{
    .witness W0<(class [System.Runtime]System.Numerics.INumber`1<^0>) U>
           !!0 : class [System.Collections]System.Collections.Generic.List`1<^0>

    call bool [System.Runtime]System.Runtime.CompilerServices.TypeWitness::TryMatchType(currentMethod, genericContext, W0, Exact, &b)
    brfalse.s NO_MATCH

    call void Program::Bar<^W0.U>()
    ret

NO_MATCH:
    ret
}

.method static void Bar<TBar> cil managed
{
    ...
}
```

the metadata can be something like:

```text
MethodDef #10
    Name Foo

GenericParam #20
    Owner    MethodDef #10
    Number   0
    Flags    none
    Name     T

WitnessDef #1
    Owner    MethodDef #10
    Number   0
    Name     W0
    Source   TypeSpec #30
    Pattern  TypeSpec #31

TypeSpec #30
    Signature  !!0

TypeSpec #31
    Signature  class List`1<ELEMENT_TYPE_WVAR WitnessDef #1, 0>

WitnessParam #21
    Owner    WitnessDef #1
    Number   0
    Flags    none
    Name     U

WitnessParamConstraint
    Owner       WitnessParam #21
    Constraint  TypeSpec #32

TypeSpec #32
    Signature  class INumber`1<ELEMENT_TYPE_WVAR WitnessDef #1, 0>

MethodSpec
    Method         Program::Bar<TBar>
    Instantiation  <ELEMENT_TYPE_WVAR WitnessDef #1, 0>
```

## Implementation strategy

I haven't been thinking too much about this, but the JIT will need to support importing signatures like `call void Bar<^W0.U>()`. This is a witness-dependent instantiation, and it should only be materialized on a path dominated by a successful type match intrinsic for `W0`.

After a successful `TryMatchType(currentMethod, genericContext, W0, Exact, &b)`, the following witness-dependent call can consume the matched TypeHandle list directly. For `call void Program::Bar<^W0.U>()`, the JIT knows `U` is entry 0 from `W0`, then builds or looks up the runtime generic method instantiation for `Program::Bar<u>` and emits the normal indirect call / helper / dictionary lookup shape for that instantiation.

For example,

```cs
TypeHandle* b;
bool matched = TryMatchType(currentMethod, genericContext, W0, Exact, &b);

if (!matched)
    goto NO_MATCH;

TypeHandle u = b[0];
DispatchGenericWitnessCall(methodof(Program::Bar<>), null, [u]);
```

For fully instantiated code, the JIT can also try to fold `TryMatchType`. For example `Foo<List<int>>()` can fold the match to true, bind `U = int`, and import the call like a normal `Bar<int>()` call.

There're also areas like using `WitnessParam` as the type of local variables (just like how we are using `GenericParam` as the type of loval variables), which are not explored yet. 
