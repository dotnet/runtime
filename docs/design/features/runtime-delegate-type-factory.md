# Runtime delegate-type factory proposal

This is an unapproved API proposal and cross-runtime prototype, not a shipping API.
The prototype is on the `api-proposal/runtime-delegate-factory` branch,
based on upstream main commit `2eb7113245f7c536ab876dca5e3533fb96c81bbf`.
This is exploratory evidence for discussion, not the likely direction we will
take for this problem, and is not intended to merge as-is.

## Background and motivation

`Expression.GetDelegateType` uses `Func` and `Action` where possible, but otherwise
depends on managed Reflection.Emit to create a delegate with a runtime-implemented
constructor and `Invoke`. Callers needing byref, pointer, byref-like, or high-arity
signatures should not need the expression-tree library or the general-purpose
managed assembly/module/type/method builder implementation.

Concrete consumers include Expressions' dynamic call-site machinery,
Microsoft.CSharp COM invocation, and CsWinRT ABI delegates with pointer and byref
parameters. [dotnet/runtime#74067](https://github.com/dotnet/runtime/issues/74067)
illustrates the latter scenario; this prototype does **not** solve its NativeAOT
code-generation limitation.

## API proposal

```csharp
namespace System.Runtime.CompilerServices;

public static partial class RuntimeHelpers
{
    [System.Diagnostics.CodeAnalysis.RequiresDynamicCode(
        "Creating a delegate type may require generating code at runtime.")]
    public static Type GetDelegateType(params Type[] typeArgs);
}
```

The parameter types precede the return type, just as in
`Expression.GetDelegateType`; `typeof(void)` denotes no return value.
Null arrays/elements, empty arrays, and `void` parameters are rejected.
Compatible signatures use the existing `Func` or `Action` type, including their
normal support for open generic arguments. Other signatures produce public,
sealed `MulticastDelegate` subclasses with a runtime constructor and `Invoke`.
Repeated requests with the same runtime type identities return the same type.

```csharp
Type delegateType = RuntimeHelpers.GetDelegateType(
    typeof(int).MakeByRefType(), typeof(int));
MethodInfo method = typeof(Example).GetMethod(nameof(Example.Increment))!;
Delegate increment = method.CreateDelegate(delegateType);
object?[] arguments = { 41 };
int result = (int)increment.DynamicInvoke(arguments)!; // 42; arguments[0] is also 42.

public static class Example
{
    public static int Increment(ref int value) => ++value;
}
```

This example requires `using System.Reflection;` and
`using System.Runtime.CompilerServices;` in addition to `using System;`.

## CoreCLR implementation

Managed code performs validation, predefined-delegate selection, and signature
caching. Two narrow QCalls identify the owning loader allocator and create a
custom delegate. No managed Reflection.Emit builders or IL generation are used
by the factory.

There is one generated assembly/module per allocator cache, not one per delegate
type. Noncollectible signatures share a process-lifetime cache and assembly.
Collectible caches are values in a `ConditionalWeakTable` keyed by the allocator's
managed object. Their signature snapshots, generated types, and assembly may
refer back to the key without making that lifetime permanent.

The native factory reuses the loader-module selection rule for function-pointer
types: select the newest collectible allocator among signature types, or the
CoreLib allocator for a noncollectible signature. The generated assembly is
created **in that existing allocator**. Allocator references retain other
collectible signature dependencies. Retaining a generated type or delegate
therefore retains its signature dependencies; releasing those roots permits
the participating allocators and generated assembly to be collected.

Only minimal in-memory metadata is generated: the assembly/module, a
`MulticastDelegate` parent reference, a type, and two methods. Native
`Assembly::CreateDynamic` and `COMDynamicWrite` loading machinery are reused,
but no assembly save, metadata delta, Hot Reload, or managed builder surface is
required.

Method signatures use primitives and structural byref, pointer, array, and
generic-instantiation encodings, with runtime-owned `ELEMENT_TYPE_INTERNAL`
handles replacing metadata type-token leaves. This preserves exact identity
even when different collectible assemblies have identical assembly/type names.
Array shapes match ordinary Emit's lower-bound encoding so reconstructed
MemberRefs can resolve correctly.

A native-only module flag permits internal handles during validation of factory
method definitions. Ordinary PE/byte-array assembly signatures remain
untrusted and still reject internal handles. Importing factory methods into
ordinary Reflection.Emit copies their module-independent signatures rather than
feeding handle bytes into the metadata-token translator. Existing collectible
dependency checks still apply.

## Prototype boundaries and risks

Custom creation on CoreCLR and Mono currently requires closed runtime types. Unbaked `TypeBuilder`
inputs and open custom signatures are not implemented; standard `Func`/`Action`
construction retains its existing generic behavior.
NativeAOT uses predefined delegates where possible but custom creation throws
`PlatformNotSupportedException`, preserving Expressions' existing restriction.
A runtime supporting dynamic code is required for custom creation.

Mono's CoreLib implementation reuses its existing Reflection.Emit delegate
creation machinery behind the factory API, with one lazily created assembly
and a synchronized signature cache. This relocates the former Expressions
implementation; it is not a new metadata-free Mono implementation. Mono's
existing dynamic-assembly implementation places assemblies in the default
load context, so its generated delegates remain process-lifetime types.
CoreCLR's allocator-scoped collectible implementation is not ported to Mono
by this change.

Expressions uses `RuntimeHelpers.GetDelegateType` unconditionally for custom
delegate types on all runtime flavors. `AssemblyGen` and the feature gate
are removed.
There is no fallback to managed assembly/module/type/method builders, including
for inputs the factory rejects. `DynamicMethod`, `ILGenerator`, and their
lightweight helpers remain available for compiling expression bodies, and the
existing single `AssemblyBuilder.ForceAllowDynamicCode` scope is retained.
Runtime-specific custom generation or rejection is implemented in CoreLib
rather than Expressions.

Expressions is an archived library; this adoption is prototype evidence, not a
proposed standalone feature contribution.

Generated assembly names, type names, and grouping differ from the previous
Expressions implementation. Collection improves for collectible signatures,
and supported custom signatures are canonicalized across factory and
Expressions callers. These observable changes require review before production
adoption. Noncollectible metadata grows with the number of distinct custom
signatures, as it does for other process-lifetime generated types.

## Prototype validation

Initial CoreCLR validation was performed on Windows x64 against the upstream-main baseline
identified above. Checked runtime/CoreLib builds and the final Release build
completed without warnings or errors.

Commands below are relative to the repository root:

| Command | Result |
|---|---|
| `.\build.cmd clr+libs+host -arch x64 -rc checked` | Clean baseline build succeeded. |
| `.\build.cmd clr+libs -arch x64 -rc Release -lc Release` | Clean baseline and prototype builds succeeded. |
| `.\build.cmd clr.corelib+clr.nativecorelib+libs.pretest -arch x64 -rc Release -lc Release` | Final prototype CoreLib and testhost refresh succeeded. |
| `.\.dotnet\dotnet.exe build .\src\libraries\System.Linq.Expressions\tests\System.Linq.Expressions.Tests.csproj /t:Test /p:RuntimeConfiguration=Release /p:Configuration=Release` | 35,132 tests passed; no failures or skips. |
| `.\.dotnet\dotnet.exe build .\src\libraries\System.Runtime\tests\System.Runtime.Tests\System.Runtime.Tests.csproj /t:Test /p:RuntimeConfiguration=Release /p:Configuration=Release /p:CustomAfterMicrosoftCommonTargets=<isolation-targets>` | 78,568 tests total; 78,481 passed, 87 skipped, no failures, with the discovery workaround described below. |

The unmodified System.Runtime suite fails during discovery with
`TypeLoadException` for
`System.Tests.ValueTypeTests+StructWithMutualGenericFieldA`. This was reproduced
on the clean upstream-main Release baseline as well as the prototype.
`<isolation-targets>` denotes a temporary, session-only MSBuild targets file that
removes `System\ValueTypeTests.cs` before compilation. No repository tests were
disabled or modified for this workaround; the result is not a passing run of
the full, unmodified System.Runtime suite.

The new tests cover custom signature reflection, delegate binding, multicast
invocation, expression compilation, method-signature import and reconstruction,
concurrent caching, shared assemblies, and collectible dependency retention and
collection. Expressions tests inspect the built assembly's metadata to verify
that `AssemblyGen` and classic Emit builder dependencies are absent, apart from
the explicitly retained `AssemblyBuilder` dynamic-code scope. They also verify
that unsupported open custom signatures throw rather than falling back.

The subsequent unconditional `RuntimeHelpers` implementation was also built
for Windows x64 Mono in Release, with API compatibility validation enabled and
no warnings or errors:

| Command | Result |
|---|---|
| `.\build.cmd mono+libs+libs.pretest -arch x64 -rc Release -lc Release` | Succeeded. |
| `.\.dotnet\dotnet.exe build .\src\libraries\System.Linq.Expressions\tests\System.Linq.Expressions.Tests.csproj /t:Test /p:RuntimeFlavor=Mono /p:RuntimeConfiguration=Release /p:Configuration=Release` | 30,774 tests passed; no failures or skips. |
| `.\.dotnet\dotnet.exe build .\src\libraries\System.Runtime\tests\System.Runtime.Tests\System.Runtime.Tests.csproj /t:Test /p:RuntimeFlavor=Mono /p:RuntimeConfiguration=Release /p:Configuration=Release "/p:XUnitOptions=-class System.Tests.DelegateTests"` | 67 tests total; 60 passed, seven skipped, no failures. |

Mono's `DynamicInvoke` does not write byref argument updates back into the
argument array when the delegate targets an expression-compiled dynamic method.
A separate probe reproduced this with a statically declared delegate and a
manually Emit-generated delegate, as well as the factory-generated delegate.
That writeback assertion remains CoreCLR-specific; binding, multicast,
expression return values, and ordinary Emit wrapper invocation are exercised
on both runtimes.

Final validation of the unconditional `RuntimeHelpers` version also covered
CoreCLR and NativeAOT:

| Command | Result |
|---|---|
| `.\build.cmd clr+libs -arch x64 -rc Release -lc Release` | Succeeded with API compatibility validation enabled, no warnings or errors. |
| `.\.dotnet\dotnet.exe build .\src\libraries\System.Linq.Expressions\tests\System.Linq.Expressions.Tests.csproj /t:Test /p:RuntimeConfiguration=Release /p:Configuration=Release` | 35,132 tests passed; no failures or skips. |
| `.\.dotnet\dotnet.exe build .\src\libraries\System.Runtime\tests\System.Runtime.Tests\System.Runtime.Tests.csproj /t:Rebuild,Test /p:BuildProjectReferences=false /p:RuntimeConfiguration=Release /p:Configuration=Release /p:CustomAfterMicrosoftCommonTargets=<isolation-targets>` | 78,570 tests total; 78,482 passed, 88 skipped, no failures, with the same baseline discovery isolation. |
| `.\build.cmd clr.aot+libs -arch x64 -rc Release -lc Release` | Succeeded with API compatibility validation enabled, no warnings or errors. |
| `.\.dotnet\dotnet.exe build .\src\libraries\System.Runtime\tests\System.Runtime.Tests\System.Runtime.Tests.csproj /t:Test /p:RuntimeConfiguration=Release /p:Configuration=Release /p:TestNativeAot=true "/p:XUnitOptions=-class System.Tests.DelegateTests"` | Native compilation succeeded, but the test script could not launch its unqualified executable name. |
| `.\artifacts\bin\System.Runtime.Tests\Release\net11.0-windows\publish\System.Runtime.Tests.exe -notrait category=AdditionalTimezoneChecks -notrait category=OuterLoop -notrait category=failing -class System.Tests.DelegateTests -xml delegate-nativeaot-results.xml` | Invoked by absolute path from the publish directory: 71 tests total; 63 passed, eight skipped, no failures. |

The NativeAOT run explicitly passed predefined delegate cases with 0, 1, and
16 parameters, argument validation, and both byref and high-arity custom
generation rejection through CoreLib and Expressions.

Other operating systems were not validated.

## Performance evidence

An ad hoc BenchmarkDotNet 0.16.0-preview.1 harness used
`Expression.GetDelegateType` on both the preserved clean baseline and prototype
Release CoreRun testhosts. This avoids requiring the new API on the baseline.
Warm cases reuse their input arrays and previously cached types. Cold creation
uses a distinct 18-parameter signature per iteration and one measured call to
avoid creating an unbounded number of process-lifetime types.

The initial CoreCLR comparison, before moving the API to `RuntimeHelpers`,
used three warmup iterations, eight measurement iterations, and one process
launch per case:

| Workload | Baseline mean | Prototype mean | Baseline managed allocation | Prototype managed allocation |
|---|---|---|---|---|
| Cached Action | 39.22 ns | 35.76 ns | 0 B | 0 B |
| Cached Func | 36.11 ns | 38.82 ns | 0 B | 0 B |
| Cached byref delegate | 35.81 ns | 35.98 ns | 0 B | 0 B |
| Cached high-arity delegate | 258.03 ns | 252.26 ns | 0 B | 0 B |
| Uncached high-arity delegate | 65.46 us | 83.62 us | 6.05 KB | 4.18 KB |

The apparent cached Func difference was investigated with three process
launches per runtime, keeping the same warmup and measurement counts.
Baseline measured 38.58 ns (99.9% confidence interval: 36.18-40.98 ns);
the prototype measured 38.92 ns (36.42-41.42 ns), with no managed allocations.
The repeated result does not establish a regression in this unchanged warm
lookup path.

Cold creation had broad overlapping confidence intervals and minimum-iteration
time warnings. Its medians were 62.20 us for the baseline and 58.05 us for the
prototype. Neither a cold speedup nor a cold regression is established by this
run. Measured managed allocations decreased by approximately 31%; this excludes
native metadata and loader allocations. The prototype's purpose is reducing
the managed Emit dependency and providing allocator-scoped ownership, not
promising a throughput improvement.

After making Expressions unconditional and relocating the API to
`RuntimeHelpers`, the same ten-case comparison was repeated with three warmup
iterations, eight measurement iterations, and one process launch:

| Workload | Baseline mean | Final prototype mean | Baseline managed allocation | Final prototype managed allocation |
|---|---|---|---|---|
| Cached Action | 42.78 ns | 38.53 ns | 0 B | 0 B |
| Cached Func | 37.50 ns | 36.90 ns | 0 B | 0 B |
| Cached byref delegate | 40.82 ns | 37.99 ns | 0 B | 0 B |
| Cached high-arity delegate | 276.96 ns | 253.47 ns | 0 B | 0 B |
| Uncached high-arity delegate | 59.76 us | 53.57 us | 6.05 KB | 4.18 KB |

All benchmark processes succeeded. These short runs do not establish a
throughput improvement. Cold creation still has overlapping confidence
intervals and minimum-iteration time warnings; the managed allocation reduction
remains approximately 31%. Mono and NativeAOT were functionally validated but
not benchmarked.

## Alternatives

| Alternative | Tradeoff |
|---|---|
| Keep `Expression.GetDelegateType` | Requires Expressions and its general managed Emit implementation; does not establish the requested allocator-scoped factory. |
| A small managed Emit wrapper | Hides the builders but retains their implementation dependency. |
| A byte-array assembly per signature | Requires normal portable metadata and binding, adds assembly overhead, and does not provide shared allocator-scoped storage. |
| MetadataUpdater | Adds an unrelated Hot Reload dependency and assembly eligibility/configuration restrictions; it cannot mutate an existing delegate signature. |
| Fully metadata-free synthesized types | Requires a separate type-loading/reflection/debugger integration path; minimal native metadata reuses existing delegate loading and reflection. |

## Adoption catalog

| Status | Site | Scope |
|---|---|---|
| Updated | `System.Linq.Expressions/.../Compiler/DelegateHelpers.cs` | All custom delegate requests use the runtime factory unconditionally, without a classic Emit fallback. |
| Updated | `System.Linq.Expressions/src/System.Linq.Expressions.csproj` | Removes `AssemblyGen` and the runtime-delegate-factory feature gate. |
| Updated | `System.Linq.Expressions/tests/DelegateType/GetDelegateTypeTests.cs` | Exercises shared type identity and verifies the binary does not reference classic Emit builders, except the accepted dynamic-code scope. |
| Updated | `System.Runtime/tests/System.Runtime.Tests/System/DelegateTests.cs` | Binding, multicast, expression compilation, signature import, caching, and collectible lifetimes. |
| Updated | Mono CoreLib `RuntimeHelpers.DelegateTypeFactory.Mono.cs` | Existing Emit-based creation is owned by CoreLib and shares a process-lifetime assembly and signature cache. |
| Updated | NativeAOT CoreLib `RuntimeHelpers.NativeAot.cs` | Explicitly rejects custom generation; predefined delegates use the shared implementation. |
| Candidate | `Microsoft.CSharp/.../RuntimeBinder/ComInterop/ComInvokeAction.cs` | Direct use instead of requesting the delegate through Expressions; deferred pending runtime parity and API approval. |
| Candidate | `Microsoft.CSharp/.../RuntimeBinder/DynamicDebuggerProxy.cs` | Existing `Expression.GetDelegateType` consumer; deferred to the area owner. |
| Candidate | `System.ComponentModel.Composition/.../Primitives/ExportedDelegate.cs` | Existing `Expression.GetDelegateType` consumer; requires framework-target and compatibility review. |
| Inapplicable | Ordinary statically declared delegates and predefined `Func`/`Action` consumers | Already have their required type; no dynamic factory needed. |
