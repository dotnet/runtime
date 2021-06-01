# ECMA-335 CLI Specification Addendum

This is a list of additions and edits to be made in ECMA-335 specifications. It includes both documentation of new runtime features and issues encountered during development. Some of the issues are definite spec errors while others could be reasoned as Microsoft implementation quirks.

## Signatures

There is a general philosophical issue whereby the spec defines the
*syntax* of signatures to exclude errors such as:

* using void outside of return types or pointer element types
* instantiating a generic with a byref type
* having a field of byref type
* etc.

Another approach is to syntactically treat `VOID`, `TYPEDBYREF`,
`BYREF Type`, `CMOD_OPT Type`, `CMOD_REQ Type` as the other `Type`s
and then deal with the cases like those above as semantic errors in their use.
That is closer to how many implementations work. It is also how type syntax
is defined in the grammar for IL, with many of the semantic errors
deferred to peverify and/or runtime checking rather than being checked
during assembly.

The spec is also not entirely consistent in its use of the first
approach. Some errors, such as instantiating a generic with an
unmanaged pointer type, are not excluded from the spec's signature
grammars and diagrams.

Many of the specific issues below arise from the tension between these
two approaches.

### 1. `(CLASS | VALUETYPE)` cannot be followed by TypeSpec in practice

In II.23.2.12 and II.23.2.14, it is implied that the token in
`(CLASS | VALUETYPE) TypeDefOrRefOrSpecEncoded` can be a `TypeSpec`, when in
fact it must be a `TypeDef` or `TypeRef`.

peverify gives the following error:

```
[MD]: Error: Signature has token following ELEMENT_TYPE_CLASS
(_VALUETYPE) that is not a TypeDef or TypeRef
```

An insightful comment in CLR source code notes that this rule prevents
cycles in signatures, but see #2 below.

Related issue:

* https://github.com/dotnet/roslyn/issues/7970

#### Proposed specification change

a) Rename section II.23.2.8 from "TypeDefOrRefOrSpecEncoded" to "TypeDefOrRefEncoded and TypeDefOrRefOrSpecEncoded"

b) Replace

> These items are compact ways to store a TypeDef, TypeRef, or TypeSpec token in a Signature (§II.23.2.12).

with

> TypeDefOrRefEncoded is a compact representation of either TypeDef or TypeRef token in a Signature (§II.23.2.12). TypeDefOrRefOrSpecEncoded is a compact representation of either TypeDef, TypeRef or TypeSpec token in a Signature.

Also correct

> The encoded version of this TypeRef token is made up as follows:

to

> The compact representation of a TypeDef, TypeRef or TypeSpec token is made up as follows:

c) In section II.23.2.12 replace

```ebnf
Type ::=
      ...
      CLASS TypeDefOrRefOrSpecEncoded
      ...
      GENERICINST (CLASS | VALUETYPE) TypeDefOrRefOrSpecEncoded GenArgCount Type*
      ...
      VALUETYPE TypeDefOrRefOrSpecEncoded
      ...
```

with

```ebnf
Type ::=
      ...
      (CLASS | VALUETYPE) TypeDefOrRefEncoded
      GENERICINST (CLASS | VALUETYPE) TypeDefOrRefEncoded GenArgCount Type+
      ...
```

Note also the correction of `Type*` to `Type+`. A generic type instantiation shall have at least one type argument.

d) In section II.23.2.14 replace

```ebnf
TypeSpecBlob ::=
      ...
      GENERICINST (CLASS | VALUETYPE) TypeDefOrRefOrSpecEncoded GenArgCount Type Type*
      ...
```

with

```ebnf
TypeSpecBlob ::=
      ...
      GENERICINST (CLASS | VALUETYPE) TypeDefOrRefEncoded GenArgCount Type+
      ...
```

`Type Type*` is simplified to `Type+`.

#### Rationale of the proposal

1. The proposal removes the possibility of representing the same type via two different encodings. This approach is consistent with II.23.2.16: "Short form signatures" where a short form of a primitive type is preferred over the corresponding long form.

2. Potential TypeSpec recursion is prevented.

3. PEVerify, the CLR runtime and C# compiler prior to VS 2015 report an error when encountering an encoded TypeSpec in the positions described above.

### 2. `(CMOD_OPT | CMOD_REQ) <TypeSpec>` is permitted in practice

In II.23.2.7, it is noted that CMOD_OPT or CMOD_REQD is followed
by a TypeRef or TypeDef metadata token, but TypeSpec tokens are
also allowed by ilasm, csc, peverify, and the CLR.

Note, in particular, that TypeSpecs are used there by C++/CLI to
represent strongly-typed boxing in C++/CLI. e.g. `Nullable<int>^`
in C++/CLI becomes
``[mscorlib]System.ValueType modopt([mscorlib]System.Nullable`1<int>) modopt([mscorlib]System.Runtime.CompilerServices.IsBoxed)``
in IL.

This tolerance adds a loophole to the rule above whereby cyclical
signatures are in fact possible, e.g.:

* `TypeSpec #1: PTR CMOD_OPT <TypeSpec #1> I4`

Such signatures can currently cause crashes in the runtime and various
tools, so if the spec is amended to permit TypeSpecs as modifiers,
then there should be a clarification that cycles are nonetheless not
permitted, and ideally readers would detect such cycles and handle the
error with a suitable message rather than a stack overflow.

Related issues:

* https://github.com/dotnet/roslyn/issues/7971
* https://github.com/dotnet/runtime/issues/4945

#### Proposed specification change

In section II.23.2.7, replace

> The CMOD_OPT or CMOD_REQD is followed by a metadata token that indexes a row in the TypeDef
 table or the TypeRef table. However, these tokens are encoded and compressed – see §II.23.2.8
for details

with

> The CMOD_OPT or CMOD_REQD is followed by a metadata token that indexes a row in the TypeDef
table, TypeRef table, or TypeSpec table. However, these tokens are encoded and compressed –
see §II.23.2.8 for details. Furthermore, if a row in the TypeSpec table is indicated,
it must not create cycle.

### 3. Custom modifiers can go in more places than specified

Most notably, II.23.2.14 and II.23.21.12 (`Type` and `TypeSpec` grammars)
are missing custom modifiers for the element type of `ARRAY` and the
type arguments of `GENERICINST`.

Also, `LocalVarSig` as specified does not allow modifiers on
`TYPEDBYREF`, and that seems arbitrary since it is allowed on parameter
and return types.

#### Proposed specification change

a) In section II.23.2.4 FieldSig, replace the diagram with a production rule:

```ebnf
FieldSig ::= FIELD Type
```

b) In section II.23.2.5 PropertySig, replace the diagram with a production rule:

```ebnf
PropertySig ::= PROPERTY HASTHIS? ParamCount RetType Param*
```

Note that this change also allows properties to have BYREF type.

c) In section II.23.2.6 LocalVarSig, replace the diagram with production rules:

```ebnf
LocalVarSig ::=
  LOCAL_SIG Count LocalVarType+

LocalVarType ::=
  Type
  CustomMod* Constraint BYREF? Type
  CustomMod* BYREF Type
  CustomMod* TYPEDBYREF

```

d) In section II.23.2.10 Param, replace the diagram with production rules:

```ebnf
Param ::=
  Type
  CustomMod* BYREF Type
  CustomMod* TYPEDBYREF
```

e) In section II.23.2.11 RetType, replace the diagram with production rules:

```ebnf
RetType ::=
  Type
  CustomMod* BYREF Type
  CustomMod* TYPEDBYREF
  CustomMod* VOID
```

f) In section II.23.2.12 Type, add a production rule to the definition of `Type`:

```ebnf
Type ::= CustomMod* Type

```

g) In sections II.23.2.12 Type and II.23.2.14 TypeSpec replace production rule

```ebnf
PTR CustomMod* Type
```

with

```ebnf
PTR Type
```

and replace production rule

```ebnf
SZARRAY CustomMod* Type
```

with

```ebnf
SZARRAY Type
```

### 4. BYREF can come before custom modifiers

Everywhere `BYREF` appears in the spec's box and pointer diagrams, it
comes after any custom modifiers, but the C++/CLI declaration `const int&`
is emitted as `BYREF CMOD_OPT IsConst I4`, and a call-site using
`CMOD_OPT IsConst BYREF I4` will not match.

Under the interpretation that `BYREF` is just a managed pointer type, it
makes sense that there should be parity between `PTR` and `BYREF` with
respect to modifiers. Consider, `const int*` vs. `int* const` in
C++. The former (pointer to constant int) is `PTR CMOD_OPT IsConst I4`
and the latter (constant pointer to int) is `CMOD_OPT IsConst PTR I4`.
The analogy from `const int*` to `const int&` justifies C++'s
encoding of `BYREF` before `CMOD_OPT` in defiance of the spec.

#### Proposed specification change

Already addressed by changes in proposal #3 above.

### 5. TypeSpecs can encode more than specified

In II.23.2.14, the grammar for a `TypeSpec` blob is a subset of the
`Type` grammar defined in II.23.21.12. However, in practice, it is
possible to have other types than what is listed.

Most notably, the important use of the `constrained.` IL prefix with
type parameters is not representable as specified since `MVAR` and `VAR`
are excluded from II.23.2.14.

More obscurely, the constrained. prefix also works with primitives,
e.g:

```
constrained. int32
callvirt instance string [mscorlib]System.Object::ToString()
```

which opens the door to `TypeSpec`s with I4, I8, etc. signatures.

It then follows that the only productions in `Type` that do not make
sense in `TypeSpec` are `(CLASS | VALUETYPE) TypeDefOrRef` since
`TypeDefOrRef` tokens can be used directly and the indirection through
a `TypeSpec` would serve no purpose.

In the same way as `constrained.`, (assuming #2 is a spec bug and not
an ilasm/peverify/CLR quirk), custom modifiers can beget `TypeSpec`s
beyond what is allowed by II.23.2.14, e.g. `modopt(int32)` creates a
typespec with signature I4.

Even more obscurely, this gives us a way to use `VOID`, `TYPEDBYREF`,
`CMOD_OPT`, and `CMOD_REQ` at the root of a `TypeSpec`, which are not even
specified as valid at the root of a `Type`: `modopt(int32 modopt(int32))`,
`modopt(void)`, and `modopt(typedref)` all work in
practice. `CMOD_OPT` and `CMOD_REQ` at the root can also be obtained by putting
a modifier on the type used with `constrained.`.

## Heap sizes

The ECMA-335-II specification isn't clear on the maximum sizes of #String, #Blob and #GUID heaps.

#### Proposed specification change

We propose the limit on #String and #Blob heap size is 2^29 (0.5 GB), that is any index to these heaps fits into 29 bits.

#### Rationale of the proposal

1) 2^29 is the maximum value representable by a compressed integer as defined elsewhere in the spec. Currently the metadata don't encode heap indices anywhere using compressed integers. However the Portable PDB specification uses compressed integers for efficient encoding of heap indices. We could extend the definition of compressed integer to cover all 32 bit integers, but it would be simpler if we could leave it as is.

2) 0.5 GB is a very large heap. Having such a big PE file seems unreasonable and very rare scenario (if it exists at all).

3) Having 3 spare bits available is very beneficial for the implementation. It allows to represent WinRT projected strings, namespaces, etc. in very efficient way. If we had to represent heap indices with all 32 bits it would bloat various structures and increase memory pressure. PE files over 0.5 GB of size are very rare, but the overhead would affect all compilers and tools working with the metadata reader.

## Metadata merging

The mention of metadata merging in § II.10.8 _Global fields and methods_ is a spec bug. The CLI does not merge metadata. Policies of static linkers that merge metadata may vary and do not concern the CLI.

This text should be deleted, and the _metadata merging_ entry should be removed from the index:

> ~~The only noticeable difference is in how definitions of this special class
> are treated when multiple modules are combined together, as is done by a class loader. This process is
> known as _metadata merging_.~~
>
> ~~For an ordinary type, if the metadata merges two definitions of the same type, it simply discards one
> definition on the assumption they are equivalent, and that any anomaly will be discovered when the
> type is used. For the special class that holds global members, however, members are unioned across all
> modules at merge time. If the same name appears to be defined for cross-module use in multiple
> modules then there is an error. In detail:~~
>
> * ~~If no member of the same kind (field or method), name, and signature exists, then
>   add this member to the output class.~~
>
> * ~~If there are duplicates and no more than one has an accessibility other than
>   **compilercontrolled**, then add them all to the output class.~~
>
> * ~~If there are duplicates and two or more have an accessibility other than
>   **compilercontrolled**, an error has occurred.~~

## Module Initializer

All modules may have a module initializer. A module initializer is defined as the type initializer (§ II.10.5.3) of the `<Module>` type (§ II.10.8).

There are no limitations on what code is permitted in a module initializer. Module initializers are permitted to run and call both managed and unmanaged code.

### Module Initialization Guarantees

In addition to the guarantees that apply to all type initializers, the CLI shall provide the following guarantees for module initializers:

1. A module initializer is executed at, or sometime before, first access to any static field or first invocation of any method defined in the module.

2. A module initializer shall run exactly once for any given module unless explicitly called by user code.

3. No method other than those called directly or indirectly from the module initializer will be able to access the types, methods, or data in a module before its initializer completes execution.

## Default Interface Methods

We propose to allow default implementations of interface methods.

Default interface methods is a runtime feature designed to support the [default interface methods](https://github.com/dotnet/csharplang/blob/main/proposals/csharp-8.0/default-interface-methods.md) C# 8.0 language feature.

The major changes are:

* Interfaces are now allowed to have instance methods (both virtual and non-virtual). Previously we only allowed abstract virtual methods.
  * Interfaces obviously still can't have instance fields.
* Interface methods are allowed to MethodImpl other interface methods the interface _requires_ (but we require the `MethodImpl`s to be final to keep things simple) - i.e. an interface is allowed to provide (or override) an implementation of another interface's method

This list of changes to the specification doesn't attempt to be an exhaustive list - there are many places within the spec that mention interfaces being just contracts that don't define implementation. This list should be complete enough to list places where interesting _implementation differences_ happen.

#### Proposed specification change

**Section** "I.8.5.3.2 Accessibility of members and nested types" is extended so that the definition of "referents that support the same type" includes "an exact type and all of the types that inherit from it, or implement it as an interface (either explicitly or implicitly)".

Examples:
`class Base : IFoo {}` / `class Derived : Base {}`: `Base` can access protected members of `IFoo`. `Derived` can also access protected members of `IFoo` because it inherits the interface.
`class Outer : IFoo { class Nested { } }`: `Nested` can access protected members of `IFoo`.
`interface IBar : IFoo { }`: `IBar` can access protected members of `IFoo` (same rules as for classes)

TODO: since we now allow protected/internal members on interfaces, do we need to adjust the existing interface method resolution algorithm to do accessibility checks (can a method in a class that can't access the interface method implement the method)? CoreCLR seems to let us do things like override internal methods from a different assembly so this doesn't seem to be enforced for classes either.

**Section** "I.8.9.5 Class type definition" [Note: the section on type initializers within the spec only seems to apply to object types and value types, not to interfaces, but the CLR has historically supported running .cctors when accessing static members of interfaces and the spec does mention interface type initializers as well. We might want to move the part about type initializers out of the section. End note.] The semantics of when and what triggers the execution of type initialization methods will be updated so that we support the strict semantic of type initializers when executing instance methods on interfaces (strict semantic currently only covers accessing static methods on interfaces):
Bullet 4 "If not marked BeforeFieldInit", item "c" is amended to include instance methods on interfaces, in addition to the existing value types.

**Section** "II.12 Semantics of interfaces" is extended to allow instance methods on interfaces.

**Section** "II.12.1 Implementing interfaces" is extended to say all virtual instance methods defined on an interface must be abstract, be marked with newslot and not have an associated MethodImpl which uses the method as its Impl, or final without newslot and with a MethodImpl that uses the method as its Impl entry.

**Section** "II.12.2 Implementing virtual methods on interfaces" is extended by an additional mechanism to provide interface method implementation - through inheritance of an existing implementation from an implemented interface.

[The general gist of the implementation is that default interface methods (either the slot defining method, or a MethodImpl for the interface method on another interface type) is always used as a fallback - only if the "old rules" didn't find an implementation, we apply the new rules and try to find an implementation on one of the interfaces.]
The algorithm is amended as follows:
* The existing algorithm to build interface table on the open type is left intact up to the last step "If the current class is not abstract and there are any interface methods that still have empty slots (i.e. slots with empty lists) for this class and all classes in its inheritance chain, then the program is invalid.". This is amended to become "If the current class is not abstract and there are any interface methods that still have empty slots (i.e. slots with empty lists) and the slot defining method is not abstract and there is no MethodImpl for the slot within the interfaces of the type's implicit or explicit interfaces, then the program is invalid." [Note: the default interface method resolution is disconnected from the interface table on the open type, as defined by the spec. The purpose of this change is not to fail loading at this stage.]
* The runtime resolution algorithm "When an interface method is invoked" is amended:
  * The original step 4 is moved after the following steps:
    * Create an empty list of candidate implementations of the interface method.
    * If the interface method itself is not abstract, add it to the list.
    * Apply all MethodImpls specified in the list of interfaces implicitly implemented by the runtime class of the instance through which the interface method is invoked and add the methods to the list.
    * Go over the owning types of each of the candidate methods in the list. If the owning type is less concrete than some other type in the list (there is another method in the list whose owning type requires the less concrete type), remove it from the list.
    * If there's more than one method in the list, throw AmbiguousImplementationException
    * If there's exactly one method in the list and the method is not abstract, call that method
    * If there's exactly one method in the list but the method is abstract, throw `EntryPointNotFoundException`.
    * If there's no method in the list and the interface is variant, repeat the above algorithm, looking for a variant match. Return the first variant match provided by a most specific interface.

**Section** "III.2.1 constrained. prefix" the paragraph starting with "This last case can only occur when method was defined on `System.Object`, `System.ValueType`, or `System.Enum`" is extended to also cover default interface method implementation. In the case the interface method implementation is provided by an interface, the implicit boxing becomes _observable_ to the program.

**Section** "III.4.2 callvirt" is extended to allow throwing `AmbiguousImplementationException` if the implementation of the interface method resolves at runtime to more than one default interface method. It's also extended to specify throwing `EntryPointNotFoundException` if the default interface implementation is abstract.

**Section** "III.4.18 ldvirtftn" is extended to allow throwing `AmbiguousImplementationException` if the implementation of the interface method resolves at runtime to more than one default interface method. It's also extended to specify throwing `EntryPointNotFoundException` if the default interface implementation is abstract.
