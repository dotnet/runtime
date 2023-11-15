# .NET Swift Interop

The Swift programming language has a different ABI, runtime environment, and object model, making it challenging to call from the .NET runtime. Existing solutions, like [Binding Tools for Swift](https://github.com/xamarin/binding-tools-for-swift) and [BeyondNet](https://github.com/royalapplications/beyondnet).

This project aims to explore the possibilities and limitations of direct P/Invoke interop with Swift. It should implement runtime mechanisms for handling Swift ABI differences. For a comprehensive .NET Swift interop, the Binding Tools for Swift contains valuable components that could be reused or built upon. 

We want to focus on the runtime support for direct P/Invoke interop with Swift, which Xamarin bindings will consume to support running Maui apps with third-party Swift libraries. Ideally, we want to avoid generating extra wrappers and attempt to directly call all kinds of Swift functions. While external tools can be helpful, they may introduce additional complexity, and we intend to integrate them into the .NET toolchain ecosystem. 

## Swift ABI in a nutshell

The Swift ABI specifies how to call functions and how their data and metadata are represented in memory. Here are important components of the ABI that we need to consider.

### Type layout

The types we want to support are: blittable value types, non-blittable value types, tuples, classes/actors, existential containers, generics types, protocols with associated types, and closures. Objects of types are stored in registers or memory. A data member of an object is any value that requires layout within the object itself. Data members include an object's stored properties and associated values. A layout for static types is determined during the compilation, while the layout for  opaque types is not determined until the runtime. For each type the alignment, size, and offset are calculated.

Enabling library evolution simplifies marshaling rules to some extent. For each entry point, the Swift compiler generates a thunk that is forward compatible. For instance, if there is an enum parameter that would typically fit into registers, the created thunk takes the value by reference rather than by value to account for potential changes in the enum's size. Additionally, there are some edge cases where Swift compiler tries to optimize structs by allocating spare bits from the alignment.

Memory management is handled by [Automatic Reference Counting](https://docs.swift.org/swift-book/documentation/the-swift-programming-language/automaticreferencecounting/).

### Type metadata

The Swift runtime keeps a metadata record for every type used in a program, including every instantiation of generic types. They can be used for class methods, reflection and debugger tools. The metadata layout consists of common properties and type-specific metadata. Common properties are value witness table pointer and kind. The value witness table references a vtable of functions that implement the value semantics of the type (alloc, copy, destroy, move). Additionally, it contains size, alignment, and type. The value witness table pointer is at`offset -1` from the metadata pointer, that is, the pointer-sized word immediately before the pointer's referenced address. This field is at offset 0 from the metadata pointer.

### Calling convention

In Swift programming language, functions parameters can be passed either by reference or value. This is called "pass-by-X" for consistency. Swift allows l-values parameters to be marked as pass-by-reference with"`in/out`. It's assumed the caller ensures validity and ownership of parameters in both cases. 

According to the calling convention, the `self`` context has dedicated registers, and it is always passed through them since it's heavily used. Methods calling other methods on the same object can share the self context. 

Here are cases when `self` context is passed via register:
 - Instance methods on class types: pointer to self
 - Class methods: pointer to type metadata (which may be subclass metadata)
 - Mutating method on value types: pointer to the value (i.e. value is passed indirectly)
 - Non-mutating methods on value types: self may fit in one or more registers, else passed indirectly
 - Thick closures, i.e. closures requiring a context: the closure context

Error handling is also handled through registers, so the caller needs to check for errors and throw if necessary. Similarly, the async context is handled through a register which contains a pointer to an object with information about the current state of the asynchronous operation. More details available at https://github.com/apple/swift/blob/main/docs/ABI/CallConvSummary.rst.

### Name mangling

Swift uses mangling for generating unique names in a binary. This process can change in different major versions (i.e. Swift 4 vs Swift 5). The Swift compiler puts mangled names in binary images to encode type references for runtime instantiation and reflection. In a binary, these mangled names may contain pointers to runtime data structures to represent locally-defined types more efficiently.

Be aware of the edge cases that can't be mapped 1:1 in C#. Consider the following example in Swift:
```swift
public static func getValue() -> Double {
    return 5.0
}

public static func getValue() -> Int {
    return 5
}
```

The Swift compiler generates the following mangled names for the functions:
```
_$s11MathLibraryAAC8getValueSdyFZ ---> static MathLibrary.MathLibrary.getValue() -> Swift.Double
_$s11MathLibraryAAC8getValueSiyFZ ---> static MathLibrary.MathLibrary.getValue() -> Swift.Int
```

In such case, generating entry points automatically from the C# would be problematic because both methods have the same name and parameter list, but different return types. According to the C# documentation, a return type of a method is not part of the signature of the method for the purposes of method overloading.

https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/methods#method-signatures

The Binding Tools for Swift resolves that by reading the native library, extracting the public symbols, and demangling them. On the runtime level the user is supposed to provide the mangled name as the entry point.

## Goals

The goal of this experiment is to explore the possibilities and limitations of direct P/Invoke interop with Swift. It should implement runtime mechanisms for handling Swift ABI differences. In the first iteration of the experiment, our focus will be on the self context and error handling calling conventions with blittable types targeting MacCatalyst. After that, we plan to expand support for non-blittable types using binding wrappers and to include other Apple targets.

We can choose to first support Mono AOT either with or without LLVM according to the initial analysis.

## Tasks

Progress on the implementation can be tracked at https://github.com/dotnet/runtime/issues/93631.

## Local build and testing

Build the runtime:
```sh
./build.sh mono+libs+clr.hosts /p:ApiCompatGenerateSuppressionFile=true /p:KeepNativeSymbols=true
```
Build the coreroot:
```sh
./src/tests/build.sh -mono debug generatelayoutonly /p:LibrariesConfiguration=Debug
```
Build the tests:
```sh
./src/tests/build.sh -mono debug -tree:Interop/Swift /p:LibrariesConfiguration=Debug
```
Build the tests in full AOT mode:
```sh
./src/tests/build.sh -log:MonoAot -mono_fullaot debug -tree:Interop/Swift /p:LibrariesConfiguration=Debug /p:RuntimeVariant=llvmfullaot
```
Run tests in full AOT mode:
- set `MONO_ENV_OPTIONS=--full-aot` and run as usual
