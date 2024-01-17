# Bridging generic constraints

Since introduction of generics in .NET there hasn't been a way to go from less contrained type parameter T to a more constrained type parameter U. If U has any constraints that T doesn't have, a T cannot be substituted for U. We've seen many instances where it would be useful to allow this for concrete substitution of T that match U's constraints. There's no way to do this substitution at runtime besides using reflection right now.

One way to go about this would be to relax constraint checks within method bodies so that constraints are validated at runtime instead of at compile time:

```csharp
//
// This is pseudo-C# just so that I don't have to write IL.
// Not expected to compile as C# in this shape, but imagine the straightforward lowering to IL.
//
static void DoConstrained<T>() where T : struct { }

static void Do<T>
{
    if (typeof(T).IsValueType)
    {
        // The T in Do is unconstrainted, but DoConstrained requires it to be a struct
        // The run time check above ensures this.
        // This will not compile as C# today because the constraints don't match.
        // Today, it will also result in a verification exception at runtime when compiling the Do method.
        DoConstrained<T>();
    }
}
```

The purpose of this document is to sketch out a possible implementation on the runtime side that would allow above code to work.

This feature doesn't assume any kind of relaxation of constraint checks outside method bodies. E.g. allowing to derive from `class Base<T> where T : IFoo { }` with `class Derived<T> : Base<T> { }` to load for `Derived<IFoo>` is not in scope.

## Runtime impact

There are two ways how constraints are validated within the runtime right now:

* Validating they match on uninstantiated types: done by comparing constraints on generic type parameters - if type parameter T is used to substitute U, the constraints need to be satisfied.
* Validating they match on concrete instantiation: for example `class Foo<T> where T: struct { }`: `Foo<int>` satisfies constraints, `Foo<object>` doesn't.

Validating on uninstantiated forms is done as part of the old verification logic. The runtime doesn't do this consistently - for example, it is done in calls in IL, but not for ldtoken in IL.

Validating instantiated forms is done always so that a `MethodTable` for an invalid type cannot even be created.

From the runtime perspective, it would be desirable to maintain the invariant that `MethodTable` for a generic type instantiation that doesn't meet its own constraints can never be loaded - an attempt to do so would continue throwing an exception. This has implications on how method bodies that may be violating constraints in certain branches can be compiled. Consider:

```csharp
//
// This is pseudo-C# just so that I don't have to write IL.
// Not expected to compile as C# in this shape, but imagine the straightforward lowering to IL.
//
interface IInterface { }

struct Constrained<T> where T : IInterface { }

static void Do<T>()
{
    if (typeof(T).IsAssignableFrom(typeof(IInterface))
    {
        Constrained<T> someLocal = default;
        try
        {
            someLocal.ToString();
        }
        catch (GenericException<Constrained<T>>) { }
    }
}
```

The requirement of never having an invalid type instantiation within the runtime type system means that if we're compiling `Do<int>` (where we cannot allow `Constrained<int>` to exist), the code generator cannot ask questions about all locals (IL locals are scoped to the method, not to a basic block), exception handling regions, or any IL within the `IsAssignableFrom` check. While it may be reasonable to expect the `IsAssignableFrom` check gets optimized out, it would likely not happen for unoptimized code. Locals and EH regions pose additional programs.

It would greatly simplify the codegen impact of this if we were to limit the amount of places within a method body that are allowed to break the constraints. The least impactful and potentially sufficiently powerful change would be allowing to break constraints on `call` instruction.

The proposal is to:

* Stop doing the verification-level constraint validation when compiling `call` (i.e. the check on uninstantiated forms).
* Keep constraint validation of the instantiated form.
* If the instantiated form of the call target doesn't satisfy constraint, replace the call with a call to a throw helper (we can likely reuse infrastructure around `CORINFO_ACCESS_ILLEGAL`).
* If the constraint validation requires a runtime check due to shared code, delay validation to run time (this can potentially be optimized out if the potential constraint violation is already in a guarded block).

### Alternative option: allow loading constraint-failing MethodTables

Limiting the constraint-violating code to `call` instruction is required to simplify code generation. Allowing `MethodTable`s that don't satisfy their own constraints to load sounds like trouble. We could potentially allow them to load and then try to prevent them from leaking out. Calling this out for completeness, I don't know if it's a good idea.

### Potential shortcut: Stop validating constraints

One more avenue could be to drop all constraint checks within the runtime and make constraints source compiler's concern only. We already have a constraint (`unmanaged` in C#) that is only known to the source compiler and not enforced at run time. This would likely require some thoughts around failure modes when invalid IL within a method body IL is encountered. This document is not going to explore this direction further since the assumption is that we do not want to drop constraint checks from the runtime, we just want to delay them.

## API proposal

Runtime support for this will be indicated by following property:

```diff
namespace System.Runtime.CompilerServices
{
    public static partial class RuntimeFeature
    {
+        /// <summary>
+        /// Represents a runtime feature where constraint validation can be delayed until run time.
+        /// </summary>
+        public const string DelayedConstraintCheck = nameof(DelayedConstraintCheck);
    }
}
```

C# compiler will also need APIs to check for constraints at runtime. Some constraints could be checked using existing reflection APIs; it's questionable if we're okay with the perf characteristics or whether we want new APIs.

* `new()` constraint: add `bool RuntimeHelpers.SatisfiesNewConstraint<T>()` (no existing reflection API for this, we need to check if it's a valuetype or a non-abstract type with public parameterless constructor)
* `struct` constraint: add `bool RuntimeHelpers.IsValueType<T>()`, or reuse `typeof(T).IsValueType`
* `class` constraint: can be `!struct` as long as we don't allow using pointers as generic parameters, but probably better to add API
* `unmanaged` constraint: does `RuntimeHelpers.IsReferenceOrContainsReferences` fit the bill?
* class/interface constraint: add new RuntimeHelpers API or reuse `Type.IsAssignableFrom`.

