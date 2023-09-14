# .NET Swift Interop

Swift has a different ABI, runtime environment, and object model, making it non-trivial to call from the .NET runtime. Existing solutions like [Binding Tools for Swift](https://github.com/xamarin/binding-tools-for-swift) and [BeyondNet](https://github.com/royalapplications/beyondnet) rely on Swift /C# /C wrappers.

This project aims to explore the possibilities and limitations of direct P/Invoke interop with Swift. For a comprehensive .NET-Swift Interop, the Binding Tools for Swift contains valuable components that should either be reused or built upon.

## Calling convention

In Swift, functions parameters can be passed differently, either by reference or value. This is called "pass-by-X" for consistency. Swift allows l-values parameters to be marked as pass-by-reference with"`in/out`. It's assumed the caller ensures validity and ownership of parameters in both cases. On a physical level, it's valuable to return common value types in registers instead of indirectly. The `self` parameter for both static and instance methods is always passed through a register since it's heavily used. Also, many methods call other methods on the same object, so it's best if the register storing `self` remains stable across different method signatures. Error handling is also handled through registers, so the caller needs to check for errors and throw if necessary. Registers allocation: https://github.com/apple/swift/blob/main/docs/ABI/CallConvSummary.rst. 

Swift's default function types are `thick` meaning they have an optional context object implicitly passed when calling the function. It would be ideal to create a thick function from a thin one without introducing a thunk just to move parameters with the missing context parameter.

## Name mangling

Swift uses mangling for generating unique names. This process can change in different major versions (i.e. Swift 4 vs Swift 5). The Swift compiler puts mangled names in binary images to encode type references for runtime instantiation and reflection. In a binary, these mangled names may contain pointers to runtime data structures to represent locally-defined types more efficiently. When calling in from .NET, it is necessary to mangle the name during the runtime or AOT compilation.

In order to simplify the testing we can use mangled name as the entry point. This provides a number of advantages, specifically for functions that have name overlap (i.e. functions with the same name that return different types) for which we cannot disambiguate from C#. The `Binding Tools for Swift` reads the dylib, pulls the public symbols and demangles them.

## Memory management

In Swift, memory management is handled by [Automatic Reference Counting](https://docs.swift.org/swift-book/documentation/the-swift-programming-language/automaticreferencecounting/).

## Types and marshalling

Interop should handle various types when calling from .NET, including blittable value types, non-blittable value types, tuples, classes/actors, existential containers, generic types, protocols with associated types, and closures. Enabling library evolution simplifies marshaling rules to some extent. For each entry point, the Swift compiler generates a thunk that is forward compatible. For instance, if you have an enum parameter that would typically fit into registers, the created thunk takes the value by reference rather than by value to account for potential changes in the enum's size.

Reference: https://github.com/xamarin/binding-tools-for-swift/blob/main/docs/ValueTypeModeling.md

Initially, we should focus on blittable types. Later, we may introduce support for other non-blittable types.

## Flow for invoking a Swift function from .NET

Here's the planned flow for invoking a Swift function from .NET if a developer uses a direct P/Invoke and demangled name:
1. A function name should be mangled using the `Binding Tools for Swift` due to limitations for functions that have name overlap. Alternative option is to mangle entry points during the runtime or AOT compilation. In this case, it may slow down the compiler for non-Swift interops and have limitations for functions that have name overlap.
2. Function parameters should be automatically marshalled without any wrappers.
3. A thunk should be emitted to handle the different calling convention, especially for instance functions and functions with error handling.
    - Implement calling convention using thunks to handle self, errors, etc.
    - Is it possible to test instance functions with P/Invoke or COM Interop is required?
4. The result should be retrieved from the register or stack or indirectly.
5. Error registers should be checked and if set, an error should be thrown.
6. Cleanup may be required?


## Template

Template is used to set the definition of done (DoD) and contains of a set of unit tests that must be implemented. Each unit test is designed to cover a specific invocation type using different input types. The tests should be expanded with all Swift types.

### Global/Static functions

This is the simplest case that should be implemented.

```swift
public static func add(_ a: Double, _ b: Double) -> Double {
    return a + b
}

public static func subtract(_ a: Double, _ b: Double) -> Double {
    return a - b
}
```

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

### Instance functions

This case is important for handling self and errors via registers.

```swift
public func factorial() -> Int {
    guard _internalValue >= 0 else {
        fatalError("Factorial is undefined for negative numbers")
    }
    return _internalValue == 0 ? 1 : _internalValue * MathLibrary(_internalValue: _internalValue - 1).factorial()
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