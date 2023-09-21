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

Error handling is also handled through registers, so the caller needs to check for errors and throw if necessary. Similarly, the calling convention allows a function to return value types that are not opaque through a combination of registers if those values fit within the size and alignment constraints of the register. More details available at https://github.com/apple/swift/blob/main/docs/ABI/CallConvSummary.rst.

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

Templates are used to set the definition of done (DoD) and contains of a set of unit tests that will be be implemented. Each unit test is designed to cover a specific invocation type using different input types. In the first iteration of the experiment, we want to focus on blittable types targeting MacCatalyst only. Later, we plan to include support for non-blittable types and other Apple targets.

Here's the flow for invoking a Swift function from .NET if a developer uses a direct P/Invoke and mangled names:
1. Function parameters should be automatically marshalled without any wrappers
2. A thunk should be emitted to handle the `self` and `error` registers
3. The result should be retrieved from the register, stack, or indirectly
4. An error should be thrown if the `error` register is set
5. Cleanup may be required

### Type marshalling and metadata conversions

Type marshalling should ideally be automated, with an initial focus on supporting blittable types. Later, we can extend support to include non-blittable types. Here are some tasks (not a full list):
 - Create a mapping between C# and Swift types
 - Investigate and determine which types lack marshalling support
 - Investigate and determine how to generate types metadata


This is the simplest case that should be implemented. It should be expanded with other types.

```swift
public static func add(_ a: Double, _ b: Double) -> Double {
    return a + b
}

public static func subtract(_ a: Double, _ b: Double) -> Double {
    return a - b
}
```

### Self context

In order to support Swift calling convention, it is necessary to implement register handling by emitting thunks in the above-mentioned cases.

```swift
public func factorial() -> Int {
    guard _internalValue >= 0 else {
        fatalError("Factorial is undefined for negative numbers")
    }
    return _internalValue == 0 ? 1 : _internalValue * MathLibrary(_internalValue: _internalValue - 1).factorial()
}
```

### Error handling

In order to support Swift calling convention, it is necessary to implement error handling by emitting thunks in the above-mentioned cases.

### Update Binding Tools for Swift to work with latest version of Mono runtime

We should update the Binding Tools for Swift to be compatible with the latest version of the Mono runtime and Xcode.

## Other examples

### Function with default params

Nice to have. When Swift compiles them it leaves the initialization expression behind for the compiler to consume later. It's effectively a weak macro that gets expanded later so that the compiler can potentially optimize it.

```swift
public static func multiply(_ a: Double, _ b: Double = 1.0) -> Double {
    return a * b
}
```

### Function with varargs

Nice to have. The semantics for parameters being `f(name0: type0, name1: type1, name2: type2)`, Swift lets you do `f(a: Int..., b: Float..., c: String...)` which gets turned into `f(a: Int[], b: Float[], c: String[])`

```swift
public static func average(numbers: Double...) -> Double {
    let sum = numbers.reduce(0, +)
    return sum / Double(numbers.count)
}
```

### Function with closures
```swift
public static func applyOperation(a: Double, b: Double, operation: (Double, Double) -> Double) -> Double {
    return operation(a, b)
}
```

### Computed properties
```swift
public static var circleArea: (Double) -> Double = { radius in
    return Double.pi * radius * radius
}
```

### Initializers/Deinitializers
```swift
public init(_internalValue: Int) {
    self._internalValue = _internalValue
    print("MathLibrary initialized.")
}

deinit {
    print("MathLibrary deinitialized.")
}
```

### Getters/Setters
```swift
private var _internalValue: Int = 0

public var value: Int {
    get {
        return _internalValue
    }
    set {
        _internalValue = newValue
    }
}
```

### Delegates/Protocols
```swift
public protocol MathLibraryDelegate: AnyObject {
    func mathLibraryDidInitialize()
    func mathLibraryDidDeinitialize()
}
...
public weak var delegate: MathLibraryDelegate?

public init(_internalValue: Int) {
    self._internalValue = _internalValue
    print("MathLibrary initialized.")
    
    // Notify the delegate that the library was initialized
    delegate?.mathLibraryDidInitialize()
}

deinit {
    print("MathLibrary deinitialized.")
    
    // Notify the delegate that the library was deinitialized
    delegate?.mathLibraryDidDeinitialize()
}
```

## Build steps

Build the runtime:
```sh
./build.sh mono+libs+clr.hosts
```
Build the coreroot:
```sh
./src/tests/build.sh -mono debug generatelayoutonly /p:LibrariesConfiguration=Debug
```
Build the tests:
```sh
./src/tests/build.sh -mono debug -test:Interop/Swift/SwiftInterop.csproj /p:LibrariesConfiguration=Debug
```
Build the native library:
```sh
swiftc -emit-library ./src/tests/Interop/Swift/MathLibrary.swift -o $PWD/artifacts/tests/coreclr/osx.arm64.Debug/Interop/Swift/SwiftInterop/libMathLibrary.dylib
```
Run the tests:
```
bash $PWD/artifacts/tests/coreclr/osx.arm64.Debug/Interop/Swift/SwiftInterop/SwiftInterop.sh -coreroot=$PWD/artifacts/tests/coreclr/osx.arm64.Debug/Tests/Core_Root/
```
