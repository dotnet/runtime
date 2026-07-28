# Properties and indexers on `[GeneratedComInterface]`

The ComInterfaceGenerator allows COM interfaces declared with `[GeneratedComInterface]` to expose ordinary C# properties and indexers in addition to methods. This document describes the supported surface, the ABI shape produced, and the design constraints that shape both. Indexers reuse the property pipeline almost entirely; their incremental rules are collected in the [Indexers](#indexers) section.

## Goals

* Let users declare COM interface members using natural C# property and indexer syntax instead of hand-rolled `get_X` / `set_X` method pairs.
* Match the vtable layout that the built-in COM CCW produces for `[ComVisible(true)]` managed interfaces, so source-generated and built-in COM remain wire-compatible for the common shapes.
* Keep the implementation a layer on top of the existing per-method ABI pipeline; an accessor is, fundamentally, just an `IMethodSymbol` that happens to back a property declaration.
* Provide a mechanism (default-implemented members; see [DefaultImplementedMembers.md](./DefaultImplementedMembers.md)) for users to declare properties or methods that are pure managed sugar and are *not* assigned vtable slots, so that a property can wrap a pair of unrelated or non-adjacent ABI methods.

## Non-goals

* Distinguishing `propput` from `propputref` at the vtable level. The `propputref` concept exists in TLB metadata and in `IDispatch::Invoke`; it has no representation in an `IUnknown`-only vtable. All setters map to a single vtable slot.
* `IDispatch` dispid integration. Properties on a `[GeneratedComInterface]` are exposed through the `IUnknown` vtable only.

## Vtable layout

For a property declared in source order *k* on the interface, the generator emits one or two vtable slots immediately following the slots reserved for any preceding members:

* `T Foo { get; set; }` &rarr; **two consecutive slots**: the getter slot first, then the setter slot.
* `T Foo { get; }` &rarr; one slot for the getter.
* `T Foo { set; }` &rarr; one slot for the setter.

The getter slot has the signature `HRESULT get_Foo(out T value)`. The setter slot has the signature `HRESULT set_Foo(T value)`. The accessor stubs are produced by the same per-method ABI pipeline that produces method stubs, so the same marshalling rules and the same `PreserveSig`-style HRESULT translation apply to both methods and accessors.

The slot ordering (get before set, both in source-declaration order) matches the layout the built-in CLR produces for a `[ComVisible(true)]` managed interface that declares the equivalent property. A read-only or write-only property produces a single slot, again matching the built-in layout.

### Inheritance and vtable layout

Inherited properties follow the same rules as inherited methods (see [DerivedComInterfaces.md](./DerivedComInterfaces.md)): a `[GeneratedComInterface]` that derives from another `[GeneratedComInterface]` inherits all base accessor slots at their original indices and appends its own accessor slots after them.

The derived interface also receives generator-emitted user-facing shadow declarations for every inherited property accessor, so that a `T value = derived.BaseProperty;` call does not require a `QueryInterface` to the base interface type.

## Supported property surface

The supported surface is intentionally narrow. Anything outside this list is rejected with `SYSLIB1091` ("member will not be source generated") so that supported semantics can be expanded over time without breaking existing users.

**Allowed:**

* Auto-property accessors with no body, in any of the three accessor combinations:

  ```csharp
  [GeneratedComInterface, Guid("…")]
  public partial interface IFoo
  {
      int Count { get; set; }      // two vtable slots
      string Name { get; }          // one vtable slot
      bool Verbose { set; }         // one vtable slot
  }
  ```

* Property-level accessibility modifiers (`public`, `internal`, `private`, `protected`). These exist on the C# surface only; the ABI shape is unchanged:

  ```csharp
  internal int Count { get; set; }   // still two vtable slots
  ```

  (Accessor-level accessibility modifiers such as `int Count { get; private set; }` are rejected by the C# language itself with **CS0442** when the accessors are abstract, so they never reach the generator. To narrow an accessor's visibility you must give it a body, which puts the property on the default-implementation path below.)

* The `unsafe` modifier on the property declaration. Generated accessor stubs are already emitted inside an `unsafe` partial interface, so this is essentially free; it lets the property's value type be a pointer.

* The `new` modifier on the property declaration for explicit shadowing of an inherited COM property. The base accessor slots remain at their original indices and the derived interface appends fresh slots for the shadowing accessors, matching the behavior of `new`-keyword method shadowing.

  ```csharp
  [GeneratedComInterface, Guid("…")] public partial interface IBase  { int Value { get; set; } }
  [GeneratedComInterface, Guid("…")] public partial interface IFoo : IBase
  {
      new int Value { get; set; }   // appends two fresh slots; IBase.Value slots remain
  }
  ```

* Default-implemented properties (properties with accessor bodies). See [DefaultImplementedMembers.md](./DefaultImplementedMembers.md).

**Disallowed (reported as `SYSLIB1091`):**

* `extern` and `required` modifiers on the property.
* The `init` accessor. `init`-vs-`set` is a C#-call-site distinction with no representation in the COM vtable; declaring an `init` accessor on a `[GeneratedComInterface]` property is rejected regardless of whether the accessor has a body. Use `set` if the accessor should participate in the vtable, or use a default-implemented `init`-style helper through a separate managed-only abstraction.
* Mixed accessor shapes where one accessor has a body and the other does not (e.g. `int Mixed { get; set { … } }`).
* Property-level marshalling attributes other than `[MarshalUsing]`. `[MarshalAs]` is not allowed on a property.

Property modifiers `virtual`, `abstract`, and `sealed` are not currently supported on `[GeneratedComInterface]` properties; the modifier story for properties is stricter than for methods. (`abstract` is the interface default and need not be written; `virtual` on an interface property requires a body, which would put it on the [DIM](./DefaultImplementedMembers.md) path but isn't currently accepted; `sealed` is rejected outright.)

## Marshalling attributes on properties

`[MarshalUsing]` may be applied directly to a property:

```csharp
[GeneratedComInterface, Guid("…")]
public partial interface IFoo
{
    [MarshalUsing(typeof(MyMarshaller))]
    MyType Item { get; set; }
}
```

Property-level marshalling info is propagated to **both** the getter (as return-value info) and the setter (as parameter info) when the generator builds the per-accessor stub. The property-level form is the recommended style because it expresses "marshal this property" in one place.

The legacy per-accessor form using `[return: MarshalUsing(...)]` on the getter and `[param: MarshalUsing(...)]` on the setter remains supported. When both forms are present, the per-accessor form wins.

`[MarshalAs]` is **not** valid on a property declaration; the BCL's `[AttributeUsage]` on `MarshalAsAttribute` does not include `AttributeTargets.Property`, so C# itself rejects the placement with **CS0592** before the generator runs. Apply it to individual accessor return values / parameters if needed.

The shadow members the generator emits in derived interfaces strip `[MarshalUsing]` and `[MarshalAs]` from their copy of the property attributes; the shadow forwards the call to the base accessor by managed invocation, so re-running marshalling info on it would be redundant.

## Indexers

C# indexers (`T this[I0 i0, …, In in] { get; set; }`) are supported using the same per-accessor pipeline as ordinary properties. Each accessor becomes its own vtable slot, the indexer's getter slot precedes its setter slot, and indexer slots are appended in source-declaration order alongside any other members.

**Slot signatures.** With a default `[IndexerName("Item")]`, the getter slot has the signature `HRESULT get_Item(I0 i0, …, In in, out T value)` and the setter slot has the signature `HRESULT set_Item(I0 i0, …, In in, T value)`. The index parameters precede the trailing value parameter on the setter, matching the order the C# language and the built-in CLR use when surfacing an indexer through COM.

**Read-only and write-only.** `this[I i] { get; }` produces one getter slot; `this[I i] { set; }` produces one setter slot — the same as properties.

**Overloading.** A `[GeneratedComInterface]` may declare multiple indexer overloads distinguished by index-parameter type; each overload's accessors get their own consecutive slot pair, in source order. The C# language requires that all indexers on the same type share a single effective `[IndexerName]` (the compiler enforces this with **CS0668**), so the IL method names on the slots are identical across overloads and overload disambiguation is left to the parameter list:

```csharp
[GeneratedComInterface, Guid("…")]
public partial interface IFoo
{
    int this[int i]            { get; set; }   // slots 3, 4: get_Item(int, out int) / set_Item(int, int)
    int this[int i, int j]     { get; set; }   // slots 5, 6: get_Item(int, int, out int) / set_Item(int, int, int)
    int this[long l]           { get;      }   // slot  7:   get_Item(long, out int)
    int this[short s]          {      set; }   // slot  8:   set_Item(short, int)
}
```

**`[IndexerName]`.** The `[IndexerName(...)]` attribute on an indexer renames the IL accessor methods (e.g. `get_Element` / `set_Element` instead of `get_Item` / `set_Item`). The generator honors this on both ABI slot generation and the auto-emitted forwarding shadows it places on derived interfaces. A derived `[GeneratedComInterface]` that re-declares an inherited indexer with the `new` modifier **must** repeat the base interface's `[IndexerName]` value (or, if the base uses the default, omit the attribute on the derived); the two values cannot diverge.

The reason is that the generator emits the inherited indexer onto the derived interface as an explicit-interface implementation (`int IBase.this[…] => throw new UnreachableException();`), which counts as an indexer member on the derived type for CS0668's "all indexers on one type share one `[IndexerName]`" rule. If the user-declared `new` shadow's `[IndexerName]` disagrees with the base's, the C# compiler rejects the derived interface with **CS0668**, **CS0111**, and related diagnostics. Splitting a single interface into multiple `[IndexerName]`-distinguished shapes therefore requires declaring them on separate, unrelated interfaces.

**Supported modifiers and marshalling.** The lists of allowed and disallowed modifiers under [Supported property surface](#supported-property-surface) apply unchanged to indexers, replacing `int Foo { … }` with `int this[I i] { … }`. `[MarshalUsing]` on the indexer is propagated to both accessor stubs as parameter info on the value parameter and as return-value info on the getter, in the same way it is for properties.

**Default-implemented indexers.** As with properties, an indexer whose accessors all carry bodies is treated as a [default-implemented member](./DefaultImplementedMembers.md) and is not assigned a vtable slot. Mixed accessor bodies (`int this[int i] { get; set { … } }`) are rejected by the C# language itself with **CS0501** rather than by the generator — the equivalent property shape triggers **CS0525** instead, but either way the user-facing diagnostic surfaces before generator analysis runs.

## Default-implemented members (DIM)

Property accessors, indexer accessors, and methods on `[GeneratedComInterface]` may carry user-supplied bodies; the generator treats those as managed-only sugar that does **not** receive a vtable slot. The full contract — rules, diagnostics, the `get_X` / `set_X` name reservation pitfall — lives in [DefaultImplementedMembers.md](./DefaultImplementedMembers.md).

## References

* [Compatibility.md](./Compatibility.md) — Rolling per-release semantic compatibility notes.
* [DefaultImplementedMembers.md](./DefaultImplementedMembers.md) — DIM contract for properties, indexers, and methods on a `[GeneratedComInterface]`.
* [DerivedComInterfaces.md](./DerivedComInterfaces.md) — Inheritance and shadowing rules that property declarations inherit from.
* [VTableStubs.md](./VTableStubs.md) — The underlying `VirtualMethodIndexAttribute` building block that ABI accessor stubs ultimately consume.
