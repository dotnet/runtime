# Bridging generic constraints

Since introduction of generics in .NET there hasn't been a way to go from less contrained type parameter T to a more constrained type parameter U. If U has any constraints that T doesn't have, a T cannot be substituted for U. We've seen many instances where it would be useful to allow this for concrete substitution of T that match U's constraints. There's no way to do this substitution at runtime besides using reflection right now.

One way to go about this would be to relax constraint checks within method bodies so that constraints are validated at runtime instead of at compile time. For example:

```csharp
//
// This is pseudo-C# just so that I don't have to write IL.
// Not expected to compile as C# in this shape, but imagine the straightforward lowering to IL.
//
static void DoConstrained<T>() where T : struct { }

static void Do<T>
{
    if (default(T) != null)
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

The requirement of never having an invalid type instantiation within the runtime type system means that if we're compiling `Do<int>` (where we cannot allow `Constrained<int>` to exist), the code generator or debugger cannot ask questions about all locals (IL locals are scoped to the method, not to a basic block), exception handling regions, or any IL within the `IsAssignableFrom` check. While it may be reasonable to expect the `IsAssignableFrom` check gets optimized out, it would likely not happen for unoptimized code. Locals and EH regions pose additional programs.

It would greatly simplify impact of this if we were to limit the amount of places within a method body that are allowed to break the constraints. The least impactful and potentially sufficiently powerful change would be allowing to break constraints on `call` instruction only.

Special attention is needed to the `if` checks that guard constraint-breaking calls. While it would be possible to take the approach in the above examples (have language compilers generate `if` blocks guarded with e.g. `IsAssignableFrom`, or other APIs depending on the form of the constraint), it seems preferable to delegate this responsibility to the runtime itself.

Instead of the language compiler generating `if (typeof(T).IsAssignableFrom(typeof(IInterface))` checks or similar, we'll define an IL sequence that would be intrinsically recognized as a _constraints bridge_. The bridge would take the form of:

```
call RuntimeHelpers.CheckConstraintsForMethodCallThatFollows
br.false ....
push arg0
push arg1
...
push argN
call CodeWithMoreStringentConstraints
```

The sequence must be within the same basic block and there must not be any other call/callvirt between `CheckConstraintsForMethodCallThatFollows` and the more constrained method call.

NOTE: This would only check constraints that the runtime recognizes. It would still be responsibility of the language compiler to add checks for constraints the runtime doesn't recognize, such as `unmanaged` in C#.

The proposal is to:

* Stop doing the verification-level constraint validation when compiling `call` prefixed with `CheckConstraintsForMethodCallThatFollows` (i.e. the check on uninstantiated forms).
* Keep constraint validation of the instantiated form. Components operating on instantiated method bodies (such as code generators or debuggers) would do the necessary macro expansion of the _constraints bridge_ IL sequence:
    * If the constraints are not met, treat the sequence as an unconditional jump to the branch that handles false return from `CheckConstraintsForMethodCallThatFollows`.
    * If the constraints are met, treat the sequence as a normal call.
    * If the constraints may be met (e.g. due to shared code), generate a runtime check.

### Alternative option: allow loading constraint-failing MethodTables

Limiting the constraint-violating code to `call` instruction is required to simplify code generation. Allowing `MethodTable`s that don't satisfy their own constraints to load sounds like trouble. We could potentially allow them to load and then try to prevent them from leaking out. Calling this out for completeness, I don't know if it's a good idea.

### Potential shortcut: Stop validating constraints

One more avenue could be to drop all constraint checks within the runtime and make constraints source compiler's concern only. We already have a constraint (`unmanaged` in C#) that is only known to the source compiler and not enforced at run time. This would likely require some thoughts around failure modes when invalid IL within a method body IL is encountered. This document is not going to explore this direction further since the assumption is that we do not want to drop constraint checks from the runtime, we just want to delay them.

## API proposal

Runtime support for this will be indicated by following property:

```diff
namespace System.Runtime.CompilerServices
{
    public static partial class RuntimeHelpers
    {
+        public static bool CheckConstraintsForMethodCallThatFollows();
    }
}

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
