# Known Issues in ECMA-335 CLI Specification 

This is an informal list of notes on issues that have been encountered
with the ECMA-335 CLI specification, primarily during development,
testing, and support of System.Reflection.Metadata.

Some of these are definite spec errors while others could be reasoned
as Microsoft implementation quirks.

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

In II.23.2.12 and II.23.2.14, it is implied that the token in `(CLASS
| VALUETYPE) TypeDefOrRefOrSpecEncoded` can be a `TypeSpec`, when in
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

```
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

```
Type ::=
      ...
      (CLASS | VALUETYPE) TypeDefOrRefEncoded
      GENERICINST (CLASS | VALUETYPE) TypeDefOrRefEncoded  GenArgCount Type+
      ...
```

Note also the correction of ```Type*``` to ```Type+```. A generic type instantiation shall have at least one type argument.

d) In section II.23.2.14 replace

```
TypeSpecBlob ::=
      ...
      GENERICINST (CLASS | VALUETYPE) TypeDefOrRefOrSpecEncoded GenArgCount Type Type*
      ...
```

with

```
TypeSpecBlob ::=
      ...
      GENERICINST (CLASS | VALUETYPE) TypeDefOrRefEncoded GenArgCount Type+
      ...
```

```Type Type*``` is simplified to ```Type+```.

#### Rationale of the proposal

1. The proposal removes the possibility of representing the same type via two different encodings. This approach is consistent with II.23.2.16: "Short form signatures" where a short form of a primitive type is preferred over the corresponding long form.

2. Potential TypeSpec recursion is prevented.

3. PEVerify, the CLR runtime and C# compiler prior to VS 2015 report an error when encountering an encoded TypeSpec in the positions decribed above.

### 2.  `(CMOD_OPT | CMOD_REQ) <TypeSpec>` is permitted in practice

In II.23.2.7, it is noted that CMOD_OPT or CMOD_REQD is followed
by a TypeRef or TypeDef metadata token, but TypeSpec tokens are 
also allowed by ilasm, csc, peverify, and the CLR.

Note, in particular, that TypeSpecs are used there by C++/CLI to
represent strongly-typed  boxing in C++/CLI. e.g. `Nullable<int>^`
in C++/CLI becomes `[mscorlib]System.ValueType 
modopt([mscorlib]System.Nulllable`1<int>) 
modopt([mscorlib]System.Runtime.CompilerServices.IsBoxed)`
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
* https://github.com/dotnet/coreclr/issues/2674

#### Proposed specification change

In section II.23.2.7, replace

> The CMOD_OPT or CMOD_REQD is followed by a metadata token that indexes a row in the TypeDef
 table or the TypeRef table.  However, these tokens are encoded and compressed – see §II.23.2.8
for details

with

> The CMOD_OPT or CMOD_REQD is followed by a metadata token that indexes a row in the TypeDef
table, TypeRef table, or TypeSpec table.  However, these tokens are encoded and compressed – 
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

```
FieldSig ::= FIELD Type
```

b) In section II.23.2.5 PropertySig, replace the diagram with a production rule:

```
PropertySig ::= PROPERTY HASTHIS? ParamCount RetType Param*
```

Note that this change also allows properties to have BYREF type.

c) In section II.23.2.6 LocalVarSig, replace the diagram with production rules:

```
LocalVarSig ::= 
  LOCAL_SIG Count LocalVarType+
  
LocalVarType ::=
  Type
  CustomMod* Constraint BYREF? Type
  CustomMod* BYREF Type
  CustomMod* TYPEDBYREF
  
```

d) In section II.23.2.10 Param, replace the diagram with production rules:

```
Param ::= 
  Type
  CustomMod* BYREF Type
  CustomMod* TYPEDBYREF
```

e) In section II.23.2.11 RetType, replace the diagram with production rules:

```
RetType ::=
  Type
  CustomMod* BYREF Type
  CustomMod* TYPEDBYREF
  CustomMod* VOID
```

f) In section II.23.2.12 Type, add a production rule to the definition of ```Type```:

```
Type ::= CustomMod* Type
  
```

g) In sections II.23.2.12 Type and II.23.2.14 TypeSpec replace production rule

```
PTR CustomMod* Type
```

with 

```
PTR Type
```

and replace production rule

```
SZARRAY CustomMod* Type
```

with 

```
SZARRAY Type
```

### 4. BYREF can come before custom modifiers

Everywhere `BYREF` appears in the spec's box and pointer diagrams, it
comes after any custom modifiers, but the C++/CLI declaration `const
int&` is emitted as `BYREF CMOD_OPT IsConst I4`, and a call-site using
`CMOD_OPT IsConst BYREF I4` will not match.

Under the interpretation that `BYREF` is just a managed pointer type, it
makes sense that there should be parity between `PTR` and `BYREF` with
respect to modifiers. Consider, `const int*` vs. `int* const` in
C++. The former (pointer to constant int) is `PTR CMOD_OPT IsConst I4`
and the latter (constant pointer to int) is `CMOD_OPT IsConst PTR
I4`. The analogy from `const int*` to `const int&` justifies C++'s
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
specified as valid at the root of a `Type`: `modopt(int32
modopt(int32))`, `modopt(void)`, and `modopt(typedref)` all work in
practice. `CMOD_OPT` and `CMOD_REQ` at the root can also be otained by putting
a modifier on the type used with `constrained.`.

## Heap sizes

The ECMA-335-II specification isn't clear on the maximum sizes of #String, #Blob and #GUID heaps.

#### Proposed specification change

We propose  the limit on #String and #Blob heap size is 2^29 (0.5 GB), that is any index to these heaps fits into 29 bits.

#### Rationale of the proposal

1)      2^29 is the maximum value representable by a compressed integer as defined elsewhere in the spec. Currently the metadata don't encode heap indices anywhere using compressed integers. However the Portable PDB specification uses compressed integers for efficient encoding of heap indices. We could extend the definition of compressed integer to cover all 32 bit integers, but it would be simpler if we could leave it as is.

2)      0.5 GB is a very large heap. Having such a big PE file seems unreasonable and very rare scenario (if it exists at all).

3)      Having 3 spare bits available is very beneficial for the implementation. It allows to represent WinRT projected strings, namespaces, etc. in very efficient way. If we had to represent heap indices with all 32 bits it would bloat various structures and increase memory pressure. PE files over 0.5 GB of size are very rare, but the overhead would affect all compilers and tools working with the metadata reader.

