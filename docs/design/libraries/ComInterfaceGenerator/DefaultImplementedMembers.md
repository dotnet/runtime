# Default-implemented members (DIM) on `[GeneratedComInterface]`

A method, property accessor, or indexer accessor on a `[GeneratedComInterface]` interface that carries a user-supplied body is treated as a **default-implemented member** (DIM): it is pure managed sugar that ships on the interface, **and it is not assigned a vtable slot**.

```csharp
[GeneratedComInterface, Guid("…")]
public partial interface IFoo
{
    // Two vtable slots — ABI methods.
    double ReadValue();
    void WriteValue(double value);

    // Zero vtable slots — managed sugar wrapping the two ABI methods above.
    double Value
    {
        get => ReadValue();
        set => WriteValue(value);
    }

    // Also zero vtable slots — a managed-only helper.
    double DoubleIt() => ReadValue() * 2;
}
```

DIM is the user-facing escape hatch for scenarios the canonical "method &rarr; one vtable slot" / "property &rarr; one or two adjacent vtable slots" rules cannot express, including:

* Wrapping ABI methods whose names do not match the canonical `get_X` / `set_X` accessor naming.
* Wrapping ABI methods that live on different vtable slots than two adjacent slots would imply.
* Adding helper methods that the wire ABI does not need to know about.

## Rules and diagnostics

* **All accessors must agree.** A property must have either all-abstract accessors (`int X { get; set; }`) or all-bodied accessors (`int X { get => …; set => …; }`). Mixing the two emits the error `SYSLIB1091` `PropertyAccessorsMustBeAllOrNothing`. (The C# compiler also rejects bare `get;` paired with a bodied `set { … }` with `CS0525`, because it interprets `get;` as an auto-property accessor and disallows auto-properties on interfaces.)

* **`[MarshalUsing]` and `[MarshalAs]` on a DIM are a warning.** A DIM never participates in marshalling, so any marshal attribute on the DIM (the property itself, an accessor return, a method parameter, etc.) emits a `SYSLIB1091` warning `MarshalAttributeOnDefaultImplementedComInterfaceMember`. The code generator does not generate code for the DIM, so the presence of a marshal attribute is misleading.

* **Inherited DIMs are skipped from base-class CCW dispatch.** When the generator emits CCW dispatch for an inherited interface, accessor methods that are not `IsAbstract` (i.e. inherited DIMs) are excluded — the runtime resolves them through ordinary virtual dispatch on the managed object.

## The `get_X` / `set_X` name reservation constraint

A natural-looking DIM pattern is to wrap a pair of ABI methods named `get_X` / `set_X` with a property `X`:

```csharp
[GeneratedComInterface, Guid("…")]
public partial interface IFoo
{
    double get_Value();                                                    // ABI method
    void set_Value(double value);                                          // ABI method
    double Value { get => get_Value(); set => set_Value(value); }          // DIM wrapping the ABI methods
}
```

**This does not compile.** Whenever a C# interface declares a property `Value`, the language reserves the IL names `get_Value` and `set_Value` for the property's accessors. Declaring an explicit `double get_Value()` method on the same interface fails with `CS0082` ("type already reserves a member called 'get_Value'") and related diagnostics.

The workaround is to give the ABI methods names that do not collide with the property's accessor names, and have the DIM wrap them by call rather than by name:

```csharp
[GeneratedComInterface, Guid("…")]
public partial interface IFoo
{
    double ReadValue();                                                   // ABI method
    void WriteValue(double value);                                        // ABI method
    double Value { get => ReadValue(); set => WriteValue(value); }        // DIM
}
```

The same constraint applies to derived-interface shadowing: a derived interface cannot declare a DIM property `X` that shadows an inherited pair of ABI methods named `get_X` / `set_X`. ABI methods and properties with matching names cannot coexist on the same C# interface chain.

## References

* [Properties.md](./Properties.md) — Property and indexer surface that consumes the DIM contract for non-ABI accessors.
* [DerivedComInterfaces.md](./DerivedComInterfaces.md) — Inheritance and shadowing rules; explains why inherited DIMs are skipped from base-class CCW dispatch.
* [Compatibility.md](./Compatibility.md) — Rolling per-release semantic compatibility notes.
