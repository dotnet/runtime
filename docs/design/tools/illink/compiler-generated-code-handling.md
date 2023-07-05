# Handling of compiler generated code

Modern compilers provide language features which require lot of fancy code generation by the compiler. Not just pure IL generation, but producing new types, methods and fields. An example is `async`/`await` in C# which turns the body of the method into a separate class which implements a state machine.

Lot of the trimming logic relies on attributes authored by the developer. These provide hints to the trimmer especially around areas which are otherwise problematic, like reflection. For example see [reflection-flow](reflection-flow.md) for an example of such attribute.

## Problem

User authored attributes are not propagated to the compiler generated code. For example (using C# async feature):

```csharp
[RequiresUnreferencedCode ("--MethodRequiresUnreferencedCode--")]
static void MethodRequiresUnreferencedCode () { }

[UnconditionalSuppressMessage ("IL2026", "")]
static async void TestBeforeAwait ()
{
    MethodRequiresUnreferencedCode ();
    await AsyncMethod ();
}
```

This code should not produce any warning, because the `IL2026` is suppressed via an attribute. But currently this will produce the `IL2026` warning from a different method:

```console
ILLink: Trim analysis warning IL2026: SuppressWarningsInAsyncCode.<TestBeforeAwait>d__1.MoveNext(): Using method 'MethodRequiresUnreferencedCode()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code.
```

Note that the warning comes from a compiler generated method `MoveNext` on class `<TestBeforeAwait>d__1`. The `UnconditionalSuppressMessage` attribute is not propagated and so from a trimmer perspective this is completely unrelated code and thus the warning is not suppressed.

### Method body attributes

The trimmer currently recognizes two attributes which effectively apply to entire method body:

#### `RequiresUnreferencedCodeAttribute`

The `RequiresUnreferencedCodeAttribute` marks the method as incompatible with trimming and at the same time it suppressed trim analysis warnings from the entire method's body. So for example (using C# iterator feature):

```csharp
[RequiresUnreferencedCode ("Incompatible with trimming")]
static IEnumerable<int> TestBeforeIterator ()
{
    MethodRequiresUnreferencedCode ();
    yield return 1;
}
```

Should not produce a warning.

#### `UnconditionalSuppressMessageAttribute`

The `UnconditionalSuppressMessageAttribute` can target lot of scopes, but the smallest one is a method. It can't target specific statements within a method. It is supposed to suppress a specific warning from the method's body, for example:

```csharp
[UnconditionalSuppressMessage("IL2026", "")]
static async void TestAfterAwait ()
{
    await AsyncMethod ();
    MethodRequiresUnreferencedCode ();
}
```

Should not produce a warning.

### Data flow analysis

The trimmer performs data flow analysis within a single method's body, mostly around track the flow of `System.Type` and related instances to be able to detect recursion usage.

#### `DynamicallyAccessedMembersAttribute`

The `DynamicallyAccessedMembersAttribute` annotates values (local variables, method parameters, ...) of type `System.Type` to hint the trimmer that the type will have its methods accessed dynamically (through reflection). Such annotation doesn't propagate currently. For example:

```csharp
static IEnumerable<int> TestParameter ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
{
    type.GetMethod ("BeforeIteratorMethod");
    yield return 1;
    type.GetMethod ("AfterIteratorMethod");
}
```

Should not produce any warnings, because the `type` variable is properly annotated.

#### Intrinsic data flow

The trimmer also intrinsically recognizes certain patterns and perform data flow analysis around them. This allows full analysis of certain reflection usage even without annotations. For example, intrinsic handling of the `typeof` keyword:

```csharp
static IEnumerable<int> TestLocalVariable ()
{
    Type type = typeof (TestType);
    type.GetMethod ("BeforeIteratorMethod");
    yield return 1;
    type.GetMethod ("AfterIteratorMethod");
}
```

### Compiler dependent behavior

Since the problems are all caused by compiler generated code, the behaviors depend on the specific compiler in use. The main focus of this document is the Roslyn C# compiler right now. Mainly since it's by far the most used compiler for .NET code. That said, we would like to design the solution in such a way that other compilers using similar patterns could also benefit from it.

It is expected that other compilers (for example the F# compiler) might have other patterns which are also problematic for trimmers. These should be eventually added to the document as well, but may require new solutions not discussed in here yet.

## A - Roslyn closure rewrite expected behavior

In order to create a lambda method with captured variables, the Roslyn compiler will generate a closure class which stores the captured values and the lambda method is then generated as a method on that class. Currently compiler doesn't propagate attributes to the generated methods.

### A1 - `RequiresUnreferencedCode` with lambda

```csharp
[RequiresUnreferencedCode ("--TestLambdaWithCapture--")]
static void TestLambdaWithCapture (int p)
{
    Action a = () => MethodRequiresUnreferencedCode (p);
}
```

Trimmer should suppress trim analysis warnings due to `RequiresUnreferencedCode` even inside the lambda. In C# 10 it will be possible to add an attribute onto the lambda directly. The attribute should be propagated only if it's not already there.
**Open question Q1a**: Should method body attributes propagate to lambdas? Maybe we should rely on C# 10 and explicit attributes only.

### A2 - `UnconditionalSuppressMessage` with lambda

```csharp
[UnconditionalSuppressMessage ("IL2026", "")]
static void TestLambdaWithCapture (int p)
{
    Action a = () => MethodRequiresUnreferencedCode (p);
}
```

Trimmer should suppress `IL2026` due to the suppression attribute. In C# 10 it will be possible to add an attribute onto the lambda directly. The attribute should be propagated only if it's not already there.
**Open question Q1a**: Should method body attributes propagate to lambdas? Maybe we should rely on C# 10 and explicit attributes only.

### A3 - Data flow annotations with lambda

```csharp
static void TestParameterInLambda ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
{
    Action a = () => {
        type.GetMethod ("InLambdaMethod");
    };
}
```

Trimmer should be able to flow the annotation from the parameter into the closure for the lambda and thus avoid warning in this case.

### A4 - Intrinsic data flow with lambda

```csharp
static void TestLocalVariableInLambda ()
{
    Type type = typeof (TestType);
    Action a = () => {
        type.GetMethod ("InLambdaMethod");
    };
}
```

Internal data flow tracking should propagate into lambdas.

### A5 - Generic parameter data flow with lambda

```csharp
static void TestGenericParameterInLambda<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
{
    Action a = () => {
        typeof (T).GetMethod ("InLocalMethod");
    };
}
```

Annotations should flow with the generic parameter into lambdas.

### A6 - `RequiresUnreferencedCode` with local function

```csharp
[RequiresUnreferencedCode ("--TestLocalFunctionWithNoCapture--")]
static void TestLocalFunctionWithNoCapture ()
{
    LocalFunction ();

    void LocalFunction()
    {
        MethodRequiresUnreferencedCode ();
    }
}
```

The trimmer could propagate the `RequiresUnreferencedCode` to the local function. Unless the function already has that attribute present.
**Question Q1b**: Should method body attributes propagate to local functions? It's possible to add the attribute manually to the local function, so maybe we should simply rely on that.

### A7 - `UnconditionalSuppressMessage` with local function

```csharp
[UnconditionalSuppressMessage ("IL2026", "")]
static void TestLocalFunctionWithNoCapture ()
{
    LocalFunction ();

    void LocalFunction ()
    {
        MethodRequiresUnreferencedCode ();
    }
}
```

Similarly to the A5 case, the trimmer could propagate the warning suppression to the local function. Unless the function already has suppressions.
**Question Q1b**: Should method body attributes propagate to local functions? It's possible to add the attribute manually to the local function, so maybe we should simply rely on that.

### A8 - Data flow annotations with local function

```csharp
static void TestParameterInLocalFunction ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
{
    LocalFunction ();

    void LocalFunction ()
    {
        type.GetMethod ("InLocalMethod");
    }
}
```

Identical to A3, annotations should propagate into local functions.

### A9 - Intrinsic data flow with local function

```csharp
static void TestLocalVariableInLocalFunction ()
{
    Type type = typeof (TestType);
    LocalFunction ();

    void LocalFunction ()
    {
        type.GetMethod ("InLocalMethod");
    }
}
```

Identical to A4 - Internal data flow tracking should propagate into local functions.

### A10 - Generic parameter data flow with local functions

```csharp
static void TestGenericParameterInLocalFunction<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
{
    LocalFunction ();

    void LocalFunction ()
    {
        typeof (T).GetMethod ("InLocalMethod");
    }
}
```

Generic parameter annotations should flow into local methods.

## B Roslyn iterator rewrites expected behavior

Specifically the C# compiler will rewrite entire method bodies. Iterators which return enumeration and use `yield return` will rewrite entire method body and move it into a separate class. This has similar problems as closures since it effectively behaves a lot like closure, but has additional challenges due to different syntax.

### B1 - `RequiresUnreferencedCode` with iterator body

```csharp
[RequiresUnreferencedCode ("--TestAfterIterator--")]
static IEnumerable<int> TestAfterIterator ()
{
    yield return 1;
    MethodRequiresUnreferencedCode ();
}
```

The attribute should apply to the entire method body and thus suppress trim analysis warnings. Even if the body is spread by the compiler into different methods.

### B2 - `UnconditionalSuppressMessage` with iterator body

```csharp
[UnconditionalSuppressMessage ("IL2026", "")]
static IEnumerable<int> TestBeforeIterator ()
{
    MethodRequiresUnreferencedCode ();
    yield return 1;
}
```

The attribute should apply to the entire method body and thus suppress trim analysis warnings. Even if the body is spread by the compiler into different methods.

### B3 - Data flow annotations in iterator body

```csharp
static IEnumerable<int> TestParameter ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
{
    type.GetMethod ("BeforeIteratorMethod");
    yield return 1;
    type.GetMethod ("AfterIteratorMethod");
}
```

The data flow annotation from method parameter should flow through the entire body.

### B4 - Intrinsic data flow in iterator body

```csharp
static IEnumerable<int> TestLocalVariable ()
{
    Type type = typeof (TestType);
    type.GetMethod ("BeforeIteratorMethod");
    yield return 1;
    type.GetMethod ("AfterIteratorMethod");
}
```

The intrinsic annotations should flow through the entire body.

### B5 - Generic parameter data flow in iterator body

```csharp
static IEnumerable<int> TestGenericParameter<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
{
    typeof (T).GetMethod ("BeforeIteratorMethod");
    yield return 1;
    typeof (T).GetMethod ("AfterIteratorMethod");
}
```

Generic parameter annotations should flow through the entire body.

## C Roslyn async rewrites expected behavior

Similarly to iterators, C# compiler also rewrites method bodies which use `async`/`await`. This has similar problems as closures since it effectively behaves a lot like closure, but has additional challenges due to different syntax.

### C1 - `RequiresUnreferencedCode` with async body

```csharp
[RequiresUnreferencedCode ("--TestAfterAwait--")]
static async void TestAfterAwait ()
{
    await AsyncMethod ();
    MethodRequiresUnreferencedCode ();
}
```

The attribute should apply to the entire method body and thus suppress trim analysis warnings. Even if the body is spread by the compiler into different methods. Very similar to B1.

### C2 - `UnconditionalSuppressMessage` with iterator body

```csharp
[UnconditionalSuppressMessage("IL2026", "")]
static async void TestBeforeAwait()
{
    MethodRequiresUnreferencedCode ();
    await AsyncMethod ();
}
```

The attribute should apply to the entire method body and thus suppress trim analysis warnings. Even if the body is spread by the compiler into different methods. Very similar to B2.

### C3 = Data flow annotations in async body

```csharp
static async void TestParameter ([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
{
    type.GetMethod ("BeforeAsyncMethod");
    await AsyncMethod ();
    type.GetMethod ("AfterAsyncMethod");
}
```

The data flow annotation from method parameter should flow through the entire body. Very similar to B3.

### C4 - Intrinsic data flow in async body

```csharp
static async void TestLocalVariable ()
{
    Type type = typeof  (TestClass);
    type.GetMethod ("BeforeAsyncMethod");
    await AsyncMethod ();
    type.GetMethod ("AfterAsyncMethod");
}
```

The intrinsic annotations should flow through the entire body. Very similar to B4.

### C5 - Generic parameter data flow in async body

```csharp
static async void TestGenericParameter<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
{
    typeof (T).GetMethod ("BeforeIteratorMethod");
    await AsyncMethod ();
    typeof (T).GetMethod ("AfterIteratorMethod");
}
```

Generic parameter annotations should flow through the entire body. Very similar to B5.

## D Roslyn closure class and method naming behavior

When the Roslyn compiler generates a closure and moves the code in the method into a method on the closure trimming tools should have enough information to provide correct information about the source of a warning (if there's any).

### D1 - Lambda methods

```csharp
static void TestInLambda ()
{
    Action a = () => MethodRequiresUnreferencedCode (); // Warning should point to this line
}
```

Given symbols this should produce a warning pointing to the source file, but should not include the method name which looks like `<>c.<TestInLambda>b__1_0()`. This should produce a warning which looks like:

```console
Source.cs(3,22): Trim analysis warning IL2026: Using method 'MethodRequiresUnreferencedCode()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code.
```

Similarly for all other warnings originating from trim analysis.

**Open question Q2a:** Should we try to display a better name for lambdas if there are no symbols available for the assembly?

### D2 - Local functions

```csharp
static void TestInLocalFunction ()
{
    LocalFunction ();

    void LocalFunction()
    {
        MethodRequiresUnreferencedCode (); // Warning should point to this line
    }
}
```

Just like with lambdas, if there are symbols the warning should point to the source location and not include the compiler generated method name which in this case looks something like `Type.<TestInLocalFunction>g__LocalFunction|2_0()`. The produced warning should look like this:

```console
Source.cs(7,9): Using method 'MethodRequiresUnreferencedCode()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code.
```

**Open question Q2b:** Should we try to display a better name for local functions if there are no symbols available for the assembly?

### D3 - Iterators

```csharp
static IEnumerable<int> TestIterator ()
{
    MethodRequiresUnreferencedCode (); // Warning should point to this line
    yield return 1;
}
```

In this case as well, the trimmer should report the warning pointing to the source and avoid including the compiler generated name which looks like `<TestIterator>d__3.MoveNext()`. The warning should look like this:

```console
Source.cs(3,5): Using method 'MethodRequiresUnreferencedCode()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code.
```

**Open question Q2c:** Should we try to display a better name for iterator methods if there are no symbols available for the assembly?

### D4 - Async

```csharp
static async void TestAsync ()
{
    MethodRequiresUnreferencedCode ();
    await AsyncMethod ();
}
```

Just like in the above cases the warning should point to source and not include the compiler generated name, which looks like `<TestAsync>d__4.MoveNext()`. The warning should look like:

```console
Source.cs(3,5): Using method 'MethodRequiresUnreferencedCode()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code.
```

**Open question Q2d:** Should we try to display a better name for async methods if there are no symbols available for the assembly?

## E Reporting user visible names from data flow analysis

Data flow analysis in the trimmer reports warnings which point to sources of data values, for example method parameters, fields and so on. With closure classes generated by the compiler the warning might be in a spot where the immediate value is read from a field on the closure class, but that field is compiler generated. Ideally the warning would point to the actual source of the value for that field which is visible in user code.

### E1 - Lambda methods

```csharp
static void TestWarningInLambda (Type typeParameter)
{
    Action a = () => typeParameter.GetMethod ("InLambdaMethod");
}
```

The above should produce warning blaming `typeParameter` as the source of the unannotated value.

### E2 - Local functions

```csharp
static void TestWarningInLocalFunction<TInput> ()
{
    Action a = () => typeof(TInput).GetMethod ("InLambdaMethod");
}
```

In this case the warning should blame `TInput` as the unannotated value.

### E3 - Iterators

```csharp
static IEnumerable<int> TestWarning(Type typeParameter)
{
    typeParameter.GetMethod ("InIteratorMethod");
    yield return 1;
}
```

This should produce warning blaming `typeParameter`.

### E4 - Async

```csharp
static async void TestWarning<TInput>()
{
    typeof (TInput).GetMethod ("InAsyncMethod");
    await AsyncMethod ();
}
```

And this one should blame `TInput`.

## Recommended solution

The solution needs to solve two problems:

* How to propagate warning suppression and `RequiresUnreferencedCode` suppression from
the user method to all of the compiler generated methods/types which are called by that method.
This is called "suppression propagation" below.
* How to implement data flow analysis across the user method's body and all of the compiler generated
methods, types and fields. This is called "data flow analysis" below.

In both cases the first thing the trimmer needs to figure out is for a given user method
what are all of the compiler generated items which were used to implement that method
in the IL. This can include:

* New types - for example closure types
* New methods - for example methods on the closure types, or methods on the parent type for local functions
* New fields - fields on the closure types
* New generic parameters - closure types and methods may be generic if the user method is generic

To correctly handle warning suppression, the trimmer needs to apply the same warning suppressions
on warnings generated due to all of the compiler generated items as if those were coming
directly from the user method's body.

To correctly handle data flow analysis, the trimmer needs to be able to track values across all of the
compiler generated items as if they implement a single unit. This almost inevitable leads to
cross method data flow analysis. Since that is a very expensive thing to do, being able to confine
it to only the compiler generated code for a given user method is almost necessary. On top of that
the trimmer needs to be able to track values as they traverse local variables, method parameters
and fields on the closure types.

### Long term solution

Detecting which compiler generated items are used by any given user method is currently relatively tricky.
There's no definitive marker in the IL which would let the trimmer confidently determine this information.
Good long term solution will need the compilers to produce some kind of marker in the IL so that
static analysis tools can reliably detect all of the compiler generated items.

This ask can be described as:
For a given user method, ability to determine all of the items (methods, fields, types, IL code) which were
used by the compiler to generate the functionality of the method into an IL assembly.

This should be things which are directly generated from the user's code. It should NOT include compiler
helpers and other infrastructure which may be needed but is not directly attributable to a user code.

This should be enough to implement solutions for both suppression propagation and data flow analysis.

### Possible short term solution

#### Heuristic based solution

Without compiler provided markers, there's no existing way to 100% reliably implement the required
functionality in the trimmer. That said, it should be possible to implement a reasonably good
approximation. This approximation will necessarily make assumptions about the compilers used
to produce the analyzed code. So for the purposes of a short term solution, Roslyn CSharp compiler
should be treated as first priority (since it's by far the most common compiler to produce analyzed IL).
Second in row should be the FSharp compiler.

It is also much simpler to implement suppression propagation, so that should be done first.

The general idea how to detect compiler generated code for a given user method:

* Ability to detect compiler generated code. Can be done by detecting identifiers which are invalid
in a given language. For CSharp, the compiler uses `<` and `>` characters in the identifiers
of a compiler generated items. In FSharp this role is served by the `@` character.
* Any compiler generated item (as detected per above) referenced from a user method
will be considered to belong to that user method.

Pros:

* Can handle all cases for warning suppressions - async, iterator, lambdas and local functions
* Can work on not just Roslyn generated IL

Cons:

* Heuristic - it may get it wrong in which case the trimmer may report less warnings than it should
* Complex implementation mainly due to issues with reflection accessed code. For example if the closure class
is marked only due to reflection access (for example `DynamicallyAccessedMemberType.All`) and the actual
user method is not marked at all then there's no good way to determine the user method to figure out suppression
context. Again can lead to reporting less warnings.

#### Deterministic partial solution based on attributes

For state machines the Roslyn compiler currently generates `IteratorStateMachineAttribute`, `AsyncStateMachineAttribute` and `AsyncIteratorStateMachineAttribute` attributes onto the "user method" and the attribute points to the generated state machine type.

This can be used to completely deterministically figure out the mapping between state machine code and user methods.

Pros:

* Deterministic and reliable - as long as Roslyn generates these attributes (which is considered internal behavior)
* Relatively simple implementation in the trimmer for suppressions

Cons:

* Partial solution - only works on async and iterator methods, doesn't work on lambdas and local functions

The recommended solution is to use the deterministic approach via attributes. This solution doesn't have the reflection access problem (which is very problematic).
The fact that it doesn't solve lambdas and local functions has a reasonable workaround. Both can be annotated
manually by the developer by adding attributes to them. This introduces a discrepancy between analyzer
and trimmer (as analyzer would not have this problem), but it's acceptable behavior for .NET 6.