# Crossgen2 Driven Compilation structure enhancements

Adding version bubbles and complex compilation rules to compilation in crossgen2 for .NET 5.

This document describes the concept of a version bubble, how to specify them to the ahead of time compiler (crossgen2), and the effect of various options on compilation.

## Behavior of code that shares a version bubble

1. Inlining is only permitted within a version bubble unless the method is marked with `System.Runtime.Versioning.NonVersionableAttribute`. If the inlinee is marked as NonVersionable, it may ALWAYS be inlined into the method being compiled.
2. Generic instantiations may only be ahead of time compiled into the application if the definition of the generic is defined within the version bubble as well as the instantiation arguments to the generic. As an exception to the requirement for the instantiation arguments to be defined within the bubble, the shared generic type `System.__Canon` will always be considered to be part of the version bubble. Also, the list of very well known types (object, string, int, uint, short, ushort, byte, sbyte, long, ulong, float, double, IntPtr, and UIntPtr) are also considered to be part of the version bubble as long as the generic is not constrained on an interface or class. For Example

```csharp
class MyGeneric<T> {}
class ConstrainedGeneric<T> where T : IEquatable<T> {}
...
//MyGeneric<int> would be always be in the version bubble where MyGeneric was defined.
//ConstrainedGeneric<int> would only be in the version bubble of ConstrainedGeneric if ConstrainedGeneric shared a version bubble with System.Private.CoreLib.
//MyGeneric<DateTime> would would only be in the version bubble of MyGeneric if MyGeneric shared a version bubble with System.Private.CoreLib.
//ConstrainedGeneric<DateTime> would would only be in the version bubble of ConstrainedGeneric if ConstrainedGeneric shared a version bubble with System.Private.CoreLib.
```

## Behavior of code that shares a compilation group

A compilation group is a set of assemblies that are compiled together. Typically in the customer case, this is a set of assemblies that is compiled in a similar timescale. In general, all assemblies compiled at once are considered to be part of the same version bubble, but in the case of the core libraries and ASP.NET this isn't actually true. Both of these layers include assemblies which support replacement by either higher layers (in the case of the WinForms/WPF frameworks) or by the application (in the case of ASP.NET).

## Specifying a version bubble in a project file

The end user developer will specify which version bubble an application is using with a notation such as the following in the project file of application.

```xml
<PropertyGroup>
    <CompilationVersionBubble>XXX</CompilationVersionBubble>
</PropertyGroup>
```

If `CompilationVersionBubble` is set to `IncludeFrameworks`, then the application will be compiled with a version bubble that includes the entire set of frameworks, and application. The framework in this scenario is the core-sdk concept of frameworks of which today we have the ASP.NET framework, WinForms, WPF, and the Core-Sdk. If the  property is `Application`, then the version bubble will be that of the application only. (Note that `Application` will require additional development effort in the runtime to support some new token resolution behavior, etc.) If the  property is `Assembly`, then the version bubble will be at the individual assembly level. (Note, naming here is theoretical and unvetted, this is simply the level of control proposed to give to typical developers.)

The default value of the CompilationVersionBubble flag would be dependent on how the application is published. My expectation is that a standalone application will default to `IncludeFrameworks`, and that if we implement sufficient support for `Application` that will be the other default. Otherwise the default shall be `Assembly`. Interaction with Docker build scenarios is also quite interesting, I suspect we would like to enable `IncludeFrameworks` by default for Docker scenarios if possible.

## Specifying the version bubble on the crossgen2 command line

There are 3 sets of files to pass to crossgen2:

1. The set of files that are referenceable by the compilation. These are specified via the --reference switch.
2. The set of files that comprise the set of assemblies being compiled at once. These are specified via the --input-file-path switch. (The first assembly specified in this list describes module actually to be compiled and output. When we add support for composite R2R images, we will produce a single module file from all of these input assemblies at once.)
3. The set of modules that comprise the version bubble tied to the assembly being compiled. The specification of this is complicated and unclear at this time. My current idea is to have two modes. Input bubble mode (where the set of input files matches the version bubble), and all mode (where all assemblies are considered to be part of the bubble. For framework ahead of time compilation scenarios such as containers and such where we may be compiling the framework and supporting scenarios where not all files in the framework are necessarily unified in the bubble, we may need an exclude function to exclude specific binaries.

## Choice of what code to compile

### Principles

1. Ahead of time generated code exists to improve startup, and for some scenarios will be retained for the lifetime of the process.
2. Our default scenario relies on tiered compilation rejit for best performance.
3. Too much pregenerated code will negatively affect applications. Startup is the critical detail for most pregeneration scenarios, and for those, there is a blend of time to pull the file to the CPU (from disk, over the network, etc.) and to compile functions. Striking the right blend has been discovered to be critical.

### Proposed approach

Note, this approach is probably more complete than we will finish in one release, but encompasses a large set of future vision in this space.

For non-generic code this is straightforward. Either compile all the non-generic code in the binary, or compile only that which is specified via a profile guided optimization step. This choice shall be driven by a per "input assembly" switch as in the presence of a composite R2R image we likely will want to have different policy for different assemblies, as has proven valuable in the past. Until proven otherwise, per assembly specification of this behavior shall be considered to be sufficient.

We shall set a guideline for how much generic code to generate, and the amount of generic code to generate shall be gated as a multiplier of the amount of non-generic code generated.

For generic code we also need a per assembly switch to adjust between various behaviors, but the proposal is as follows:

1. We compile the non-generic code aggressively and we compile generic code specified via a profile guide aggressively. These compilations will ignore any generics size limitations.
2. We compile the generic code directly referenced by the above logic. These compilations will be gated on fitting within the generics compilation budget.
3. We compile generic code driven by heuristics. Intentionally we shall choose a small set of heuristics driven by customer data. A set of proposed heuristics is below, but we should determine which heuristics through experimentation, not decide them before the technology is developed.
  - Precompilation of generic task infrastructure related to async state machines. (Only if CoreLib is part of the version bubble)
  - Precompilation of MoveNext and Current methods for C# generated iterators
  - Precompilation of interface methods used by LINQ expressions. (Don't force generation of all virtual functions on a type, just pick a subset related to common uses)
  - Precompilation of statically discoverable cross-module instantiations for self-contained and docker scenarios if CompilationVersionBubble >= ‘Application’. Here, statically discoverable instantiations can be loaded from the typespecs and methodspecs in the metadata of the input files that comprise the version bubble tied to the assembly being compiled

## Reducing generics duplication

With the advent of a version bubble larger than a single binary and the ability to generate generics, comes the problem of managing the multiple copies of generic code that might be generated.

The traditional NGEN model was to greedily generate generic code everywhere and assume it wouldn't get out of hand. As generics have become more used and applications have become more broken down into many assemblies, this model has become less workable, and thus this model attempts to prevent such overgeneration.

Application construction consists of 1 or more frameworks, which may be built and distributed independently from the application (docker container scenario) and the application.

For instance,

Application
ASP.NET
Runtime Layer

Each layer in this stack will be compiled as a consistent set of crossgen2 compilations.

I propose to reduce the generics duplication problem to allow duplication between layers, but not within a layer. There are two ways to do this. The first of which is to produce composite R2R images for a layer. Within a single composite R2R image generation, running heuristics and generating generics eagerly should be straightforward. This composite R2R image would have all instantiations statically computed that are local to that particular layer of compilation, and also any instantiations from other layers. The duplication problem would be reduced in that a single analysis would trigger these multi-layer dependent compilations, and so which there may be duplication between layers, there wouldn't be duplication within a layer. And given that the count of layers is not expected to exceed 3 or 4, that duplication will not be a major concern.

The second approach is to split compilation up into assembly level units, run the heuristics per assembly, generate the completely local generics in the individual assemblies, and then nominate a final mop up assembly that consumes a series of data files produced by the individual assembly compilations and holds all of the stuff that didn't make sense in the individual assemblies. In my opinion this second approach would be better for debug builds, but the first approach is strictly better for release builds, and really shouldn't be terribly slow.

# Proposal

Address the reducing generics duplication concern by implementing composite R2R files instead of attempting to build a multifile approach, where each layer is expected to know the exact layer above, or be fully R2R with respect to all layers above. Loading of R2R images built with version bubble dependence will lazily verify that version rules are not violated, and produce a FailFast if version bubble usage rules are used incorrectly. For customers which produce customized versions of the underlying frameworks, if any differences are present they will likely be forced to provide their own targeting packs to use the fully AOT layered scenarios.

## Expected benefits of this design

1. Allow for best performance standalone applications. (Primarily targeting the hyperscale scenario. Benefits for regular UI standalone applications would also likely be present, but not significant enough to drive this effort.)
2. Support a layered Docker image model for high performance smaller applications. This is driven by the presence of a generics budget that is application wide (to allow customers to control distribution size of applications, as well as the ability to capture a really good set of precompiled generics to optimize startup time. The biggest precompiled wins here are the ability to AOT compile portions of CoreLib into these applications such as appropriate parts of the task infrastructure, as well as collections.)
3.  Allows use of proven technology (profile guided optimization) to drive generics compilation for high value customers, while also provided an opportunity for innovation in heuristics.

## Issues to resolve with this proposal

1. This proposal needs to be squared with our servicing policies and ensure that we aren't affecting preventing promised servicing capabilities.
  - In particular, there has been a concept in the past about making self contained applications serviceable through a machine wide policy key. We would need to design what our plans are here.
