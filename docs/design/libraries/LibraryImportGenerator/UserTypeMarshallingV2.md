# User Defined Type Marshalling for Source-Generated Interop

For the V1 of our source-generator, we designed support for marshalling collection types and user-defined structure types based on the designs in [StructMarshalling.md](StructMarshalling.md) and [SpanMarshallers.md](SpanMarshallers.md). As the model was adopted throughout dotnet/runtime, ASP.NET Core, WinForms, and early adopters, we received substantial feedback that led us to reconsider some components of the design.

Here are some of the main feedback points on the previous design:

- State diagram was complex
    - The particular order in which methods are called on the marshallers were not simple to define.
    - Handling exception scenarios without causing memory leaks was difficult.
    - Supporting state preservation in "element of a collection" scenarios was going to be extremely difficult.
- Overcomplicated for simple marshallers
    - The support for "Transparent Structures" in the original design added additional overhead on the base design instead of making this scenario cheaper.
- Concept Overload
    - The design with optional features and multiple shapes was getting to the point that introducing people to the design was going to be difficult as there were many small options to pick.
- Limited specialization capabilities
    - The V1 design mapped a single managed type to a single marshaller type. As a result, if the marshaller type required specialized support for a particular scenario such as a stack-allocated buffer optimization, then every scenario had to pay the overhead to support that conditional optimization.
    - A marshaller could only be the marshaller for one managed type. As a result, if two types (such as `string` and `char`) both wanted to use the same marshalling concept, the developer would need to use two different marshaller types.

The new design tries to address many of these concerns.

The new marshallers have a stateless shape and a stateful shape. Stateful shapes are (currently) not allowed in "element of a collection" scenarios as handling them is difficult today, but we may improve this in the future. The stateful shapes are described in the order in which the methods will be called. Additionally, by moving away from using constructors for part of the marshalling, we can simplify the exception-handling guidance as we will only ever have one marshaller instance per parameter and it will always be assigned to the local.

Stateless shapes avoid the problems of maintaining state and will be the primarily used shapes (they cover 90+% of our scenarios).

The new stateless shapes provide simple mechanisms to implement marshalling for "Transparent Structures" without adding additional complexity.

The new design has less "optional" members and each member in a shape is always used when provided.

The new design uses a "marshaller entry-point" type to name a concept, which the user provides attributes on to point to the actual marshaller types per-scenario. This enables a marshaller entry-point type to provide specialized support for particular scenarios and support multiple managed types with one marshaller entry-point type.

## API Diff for Supporting Attributes

```diff
namespace System.Runtime.InteropServices.Marshalling;

- [AttributeUsage(AttributeTargets.Struct)]
- public sealed class CustomTypeMarshallerAttribute : Attribute
- {
-      public CustomTypeMarshallerAttribute(Type managedType, CustomTypeMarshallerKind marshallerKind = - CustomTypeMarshallerKind.Value)
-      {
-           ManagedType = managedType;
-           MarshallerKind = marshallerKind;
-      }
-
-      public Type ManagedType { get; }
-      public CustomTypeMarshallerKind MarshallerKind { get; }
-      public int BufferSize { get; set; }
-      public CustomTypeMarshallerDirection Direction { get; set; } = CustomTypeMarshallerDirection.Ref;
-      public CustomTypeMarshallerFeatures Features { get; set; }
-      public struct GenericPlaceholder
-      {
-      }
- }
-
- public enum CustomTypeMarshallerKind
- {
-      Value,
-      LinearCollection
- }
-
- [Flags]
- public enum CustomTypeMarshallerFeatures
- {
-      None = 0,
-      /// <summary>
-      /// The marshaller owns unmanaged resources that must be freed
-      /// </summary>
-      UnmanagedResources = 0x1,
-      /// <summary>
-      /// The marshaller can use a caller-allocated buffer instead of allocating in some scenarios
-      /// </summary>
-      CallerAllocatedBuffer = 0x2,
-      /// <summary>
-      /// The marshaller uses the two-stage marshalling design for its <see cref="CustomTypeMarshallerKind"/> instead of the - one-stage design.
-      /// </summary>
-      TwoStageMarshalling = 0x4
- }
- [Flags]
- public enum CustomTypeMarshallerDirection
- {
-      /// <summary>
-      /// No marshalling direction
-      /// </summary>
-      [EditorBrowsable(EditorBrowsableState.Never)]
-      None = 0,
-      /// <summary>
-      /// Marshalling from a managed environment to an unmanaged environment
-      /// </summary>
-      In = 0x1,
-      /// <summary>
-      /// Marshalling from an unmanaged environment to a managed environment
-      /// </summary>
-      Out = 0x2,
-      /// <summary>
-      /// Marshalling to and from managed and unmanaged environments
-      /// </summary>
-      Ref = In | Out,
- }

+
+
+ /// <summary>
+ /// An enumeration representing the different marshalling scenarios in our marshalling model.
+ /// </summary>
+ public enum MarshalMode
+ {
+     /// <summary>
+     /// All scenarios. A marshaller specified with this scenario will be used if there is not a specific
+     /// marshaller specified for a given usage scenario.
+     /// </summary>
+     Default,
+     /// <summary>
+     /// By-value and <c>in</c> parameters in managed-to-unmanaged scenarios, like P/Invoke.
+     /// </summary>
+     ManagedToUnmanagedIn,
+     /// <summary>
+     /// <c>ref</c> parameters in managed-to-unmanaged scenarios, like P/Invoke.
+     /// </summary>
+     ManagedToUnmanagedRef,
+     /// <summary>
+     /// <c>out</c> parameters in managed-to-unmanaged scenarios, like P/Invoke.
+     /// </summary>
+     ManagedToUnmanagedOut,
+     /// <summary>
+     /// By-value and <c>in</c> parameters in unmanaged-to-managed scenarios, like Reverse P/Invoke.
+     /// </summary>
+     UnmanagedToManagedIn,
+     /// <summary>
+     /// <c>ref</c> parameters in unmanaged-to-managed scenarios, like Reverse P/Invoke.
+     /// </summary>
+     UnmanagedToManagedRef,
+     /// <summary>
+     /// <c>out</c> parameters in unmanaged-to-managed scenarios, like Reverse P/Invoke.
+     /// </summary>
+     UnmanagedToManagedOut,
+     /// <summary>
+     /// Elements of arrays passed with <c>in</c> or by-value in interop scenarios.
+     /// </summary>
+     ElementIn,
+     /// <summary>
+     /// Elements of arrays passed with <c>ref</c> or passed by-value with both <see cref="InAttribute"/> and <see cref="OutAttribute" /> in interop scenarios.
+     /// </summary>
+     ElementRef,
+     /// <summary>
+     /// Elements of arrays passed with <c>out</c> or passed by-value with only <see cref="OutAttribute" /> in interop scenarios.
+     /// </summary>
+     ElementOut
+ }
+
+ /// <summary>
+ /// Attribute to indicate an entry point type for defining a marshaller.
+ /// </summary>
+ [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = true)]
+ public sealed class CustomMarshallerAttribute : Attribute
+ {
+     /// <summary>
+     /// Create a <see cref="CustomMarshallerAttribute"/> instance.
+     /// </summary>
+     /// <param name="managedType">Managed type to marshal.</param>
+     /// <param name="marshalMode">Marshalling mode.</param>
+     /// <param name="marshallerType">Type used for marshalling.</param>
+     public CustomMarshallerAttribute(Type managedType, MarshalMode marshalMode, Type marshallerType)
+     {
+         ManagedType = managedType;
+         MarshalMode = marshalMode;
+         MarshallerType = marshallerType;
+     }
+
+     public Type ManagedType { get; }
+
+     public MarshalMode MarshalMode { get; }
+
+     public Type MarshallerType { get; }
+
+     /// <summary>
+     /// Placeholder type for generic parameter
+     /// </summary>
+     public struct GenericPlaceholder
+     {
+     }
+ }
+
+ /// <summary>
+ /// Specifies that this marshaller entry-point type is a contiguous collection marshaller.
+ /// </summary>
+ [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
+ public sealed class ContiguousCollectionMarshallerAttribute : Attribute
+ {
+ }
```

## Design Details

First of all, this new design continues to use the existing policy for defining "blittable" types as described in the V1 design. The rest of this document will describe the custom user-defined marshalling rules.

In the new design, the user will first define an "entry-point type" that represents a marshalling concept. For example, if we are marshalling a `string` to a native UTF-8 encoded string, we might call the marshaller `Utf8StringMarshaller`. This entry-point type must be a `static class` or a `struct`. The developer will then use the `CustomMarshallerAttribute` to specify which "marshaller implementation type" will be used to actually provide the marshalling for a `MarshalMode`. If an attribute is missing or a property on the attribute is set to `null` or left unset, this marshaller will not support marshalling in that scenario. If the marshaller implementation type is considered stateless if it is a `static class` and stateful if it is a `struct`. A single type can be specified multiple times if it provides the marshalling support for multiple scenarios.

To avoid confusion around when each marshaller applies, we define when the marshallers apply based on the C# syntax used. This helps reduce the concept load as developers don't need to remember the mapping between the previous design's `CustomTypeMarshallerDirection` enum member and the C# keyword used for a parameter, which do not match in a Reverse P/Invoke-like scenario.

We will recommend that the marshaller types that are supplied are nested types of the "entry-point type" or the "entry-point type" itself, but we will not require it. Each specified marshaller type will have to abide by one of the following shapes depending on the scenario is supports.

The examples below will also show which properties in each attribute support each marshaller shape.

## Value Marshaller Shapes

We'll start with the value marshaller shapes. These marshaller shapes support marshalling a single value.

Each of these shapes will support marshalling the following type:

```csharp
// Any number of generic parameters is allowed, with any constraints
struct TManaged<T, U, V...>
{
    // ...
}
```

The type `TNative` can be any `unmanaged` type. It represents whatever unmanaged type the marshaller marshals the managed type to.

### Stateless Managed->Unmanaged

```csharp
[CustomMarshaller(typeof(TManaged<,,,...>), MarshalMode.ManagedToUnmanagedIn, typeof(ManagedToNative))]
[CustomMarshaller(typeof(TManaged<,,,...>), MarshalMode.UnmanagedToManagedOut, typeof(ManagedToNative))]
static class TMarshaller<T, U, V...>
{
    public static class ManagedToNative
    {
        public static TNative ConvertToUnmanaged(TManaged managed); // Can throw exceptions

        public static ref TOther GetPinnableReference(TManaged managed);  // Optional. Can throw exceptions. Result pinnned and passed to Invoke.

        public static void Free(TNative unmanaged); // Optional. Should not throw exceptions
    }
}

```
### Stateless Managed->Unmanaged with Caller-Allocated Buffer

The element type of the `Span` for the caller-allocated buffer can be any type that guarantees any alignment requirements.

```csharp
[CustomMarshaller(typeof(TManaged<,,,...>), MarshalMode.ManagedToUnmanagedIn, typeof(ManagedToNative))]
[CustomMarshaller(typeof(TManaged<,,,...>), MarshalMode.UnmanagedToManagedOut, typeof(ManagedToNative))]
static class TMarshaller<T, U, V...>
{
    public static class ManagedToNative
    {
        public static int BufferSize { get; }
        public static TNative ConvertToUnmanaged(TManaged managed, Span<byte> callerAllocatedBuffer); // Can throw exceptions

        public static void Free(TNative unmanaged); // Optional. Should not throw exceptions
    }
}

```

### Stateless Unmanaged->Managed

```csharp
[CustomMarshaller(typeof(TManaged<,,,...>), MarshalMode.ManagedToUnmanagedOut, typeof(NativeToManaged))]
[CustomMarshaller(typeof(TManaged<,,,...>), MarshalMode.UnmanagedToManagedIn, typeof(NativeToManaged))]
static class TMarshaller<T, U, V...>
{
    public static class NativeToManaged
    {
        public static TManaged ConvertToManaged(TNative unmanaged); // Can throw exceptions

        public static void Free(TNative unmanaged); // Optional. Should not throw exceptions
    }
}

```

### Stateless Unmanaged->Managed with Guaranteed Unmarshalling

This shape directs the generator to emit the `ConvertToManagedFinally` call in the "GuaranteedUnmarshal" phase of marshalling.

```csharp
[CustomMarshaller(typeof(TManaged<,,,...>), MarshalMode.ManagedToUnmanagedOut, typeof(NativeToManaged))]
[CustomMarshaller(typeof(TManaged<,,,...>), MarshalMode.UnmanagedToManagedIn, typeof(NativeToManaged))]
static class TMarshaller<T, U, V...>
{
    public static class NativeToManaged
    {
        public static TManaged ConvertToManagedFinally(TNative unmanaged); // Should not throw exceptions

        public static void Free(TNative unmanaged); // Optional. Should not throw exceptions
    }
}

```

### Stateless Bidirectional
```csharp
[CustomMarshaller(typeof(TManaged<,,,...>), MarshalMode.ManagedToUnmanagedRef, typeof(Bidirectional))]
[CustomMarshaller(typeof(TManaged<,,,...>), MarshalMode.UnmanagedToManagedRef, typeof(Bidirectional))]
[CustomMarshaller(typeof(TManaged<,,,...>), MarshalMode.ElementRef, typeof(Bidirectional))]
static class TMarshaller<T, U, V...>
{
    public static class Bidirectional
    {
        // Include members from each of the following:
        // - One Stateless Managed->Unmanaged Value shape
        // - One Stateless Unmanaged->Managed Value shape
    }
}

```

### Stateful Managed->Unmanaged

```csharp
[CustomMarshaller(typeof(TManaged<,,,...>), MarshalMode.ManagedToUnmanagedIn, typeof(ManagedToNative))]
[CustomMarshaller(typeof(TManaged<,,,...>), MarshalMode.UnmanagedToManagedOut, typeof(ManagedToNative))]
static class TMarshaller<T, U, V...>
{
    public struct ManagedToNative // Can be ref struct
    {
        public ManagedToNative(); // Optional, can throw exceptions.

        public void FromManaged(TManaged managed); // Can throw exceptions.

        public ref TIgnored GetPinnableReference(); // Result pinned for ToUnmanaged call and Invoke, but not used otherwise.

        public static ref TOther GetPinnableReference(TManaged managed); // Optional. Can throw exceptions. Result pinnned and passed to Invoke.

        public TNative ToUnmanaged(); // Can throw exceptions.

        public void OnInvoked(); // Optional. Should not throw exceptions.

        public void Free(); // Should not throw exceptions.
    }
}

```
### Stateful Managed->Unmanaged with Caller Allocated Buffer

The element type of the `Span` for the caller-allocated buffer can be any type that guarantees any alignment requirements.

```csharp
[CustomMarshaller(typeof(TManaged<,,,...>), MarshalMode.ManagedToUnmanagedIn, typeof(ManagedToNative))]
[CustomMarshaller(typeof(TManaged<,,,...>), MarshalMode.UnmanagedToManagedOut, typeof(ManagedToNative))]
static class TMarshaller<T, U, V...>
{
    public struct ManagedToNative // Can be ref struct
    {
        public static int BufferSize { get; }
        public ManagedToNative(); // Optional, can throw exceptions.

        public void FromManaged(TManaged managed, Span<byte> buffer); // Can throw exceptions.

        public ref TIgnored GetPinnableReference(); // Result pinned for ToUnmanaged call and Invoke, but not used otherwise.

        public static ref TOther GetPinnableReference(TManaged managed); // Optional. Can throw exceptions. Result pinnned and passed to Invoke.

        public TNative ToUnmanaged(); // Can throw exceptions.

        public void OnInvoked(); // Optional. Should not throw exceptions.

        public void Free(); // Should not throw exceptions.
    }
}

```

### Stateful Unmanaged->Managed

```csharp
[CustomMarshaller(typeof(TManaged<,,,...>), MarshalMode.ManagedToUnmanagedOut, typeof(NativeToManaged))]
[CustomMarshaller(typeof(TManaged<,,,...>), MarshalMode.UnmanagedToManagedIn, typeof(NativeToManaged))]
static class TMarshaller<T, U, V...>
{
    public struct NativeToManaged // Can be ref struct
    {
        public NativeToManaged(); // Optional, can throw exceptions.

        public void FromUnmanaged(TNative unmanaged); // Should not throw exceptions.

        public TManaged ToManaged(); // Can throw exceptions.

        public void Free(); // Should not throw exceptions.
    }
}

```

### Stateful Unmanaged->Managed with Guaranteed Unmarshalling

```csharp
[CustomMarshaller(typeof(TManaged<,,,...>), MarshalMode.ManagedToUnmanagedOut, typeof(NativeToManaged))]
[CustomMarshaller(typeof(TManaged<,,,...>), MarshalMode.UnmanagedToManagedIn, typeof(NativeToManaged))]
static class TMarshaller<T, U, V...>
{
    public struct NativeToManaged // Can be ref struct
    {
        public NativeToManaged(); // Optional, can throw exceptions.

        public void FromUnmanaged(TNative unmanaged); // Should not throw exceptions.

        public TManaged ToManagedFinally(); // Should not throw exceptions.

        public void Free(); // Should not throw exceptions.
    }
}

```

### Stateful Bidirectional
```csharp
[CustomMarshaller(typeof(TManaged<,,,...>), MarshalMode.ManagedToUnmanagedRef, typeof(Bidirectional))]
[CustomMarshaller(typeof(TManaged<,,,...>), MarshalMode.UnmanagedToManagedRef, typeof(Bidirectional))]
static class TMarshaller<T, U, V...>
{
    public struct Bidirectional // Can be ref struct
    {
        // Include members from each of the following:
        // - One Stateful Managed->Unmanaged Value shape
        // - One Stateful Unmanaged->Managed Value shape
    }
}
```

## Linear (Array-like) Collection Marshaller Shapes

We'll continue with the collection marshaller shapes. These marshaller shapes support marshalling the structure of a collection of values, where the values themselves are marshalled with marshallers of their own (using the marshaller provided for `MarshalMode.Element*`). This construction allows us to compose our marshallers and to easily support arrays of custom types without needing to implement a separate marshaller for each element type.

Each of these shapes will support marshalling the following type:

```csharp
// Any number of generic parameters is allowed, with any constraints
struct TCollection<T, U, V...>
{
    // ...
}
```

A collection marshaller must have the `ContiguousCollectionMarshallerAttribute` applied to the entry-point type. A collection marshaller for a managed type will have similar generics handling as the value marshaller case; however, there is one difference. A collection marshaller must have an additional generic parameter at the end of the generic parameter list. This parameter can optionally be constrained to `: unmanaged` (but the system will not require this). The additional parameter will be filled in with a generics-compatible representation of the unmanaged type for the collection's element type (`nint` will be used when the native type is a pointer type).

The type `TNative` can be any `unmanaged` type. It represents whatever unmanaged type the marshaller marshals the managed type to.


### Stateless Managed->Unmanaged

```csharp
[CustomMarshaller(typeof(TCollection<,,,...>), MarshalMode.ManagedToUnmanagedIn, typeof(ManagedToNative))]
[CustomMarshaller(typeof(TCollection<,,,...>), MarshalMode.UnmanagedToManagedOut, typeof(ManagedToNative))]
[CustomMarshaller(typeof(TCollection<,,,...>), MarshalMode.ElementIn, typeof(ManagedToNative))]
[ContiguousCollectionMarshaller]
static class TMarshaller<T, U, V..., TUnmanagedElement> where TUnmanagedElement : unmanaged
{
    public static class ManagedToNative
    {
        public static TNative AllocateContainerForUnmanagedElements(TCollection managed, out int numElements); // Can throw exceptions

        public static ReadOnlySpan<TManagedElement> GetManagedValuesSource(TCollection managed); // Can throw exceptions

        public static Span<TUnmanagedElement> GetUnmanagedValuesDestination(TNative unmanaged, int numElements); // Can throw exceptions

        public static ref TOther GetPinnableReference(TManaged managed);  // Optional. Can throw exceptions. Result pinnned and passed to Invoke.

        public static void Free(TNative unmanaged); // Optional. Should not throw exceptions
    }
}

```
### Stateless Managed->Unmanaged with Caller-Allocated Buffer

The element type of the `Span` for the caller-allocated buffer can be any type that guarantees any alignment requirements, including `TUnmanagedElement`.

```csharp
[CustomMarshaller(typeof(TCollection<,,,...>), MarshalMode.ManagedToUnmanagedIn, typeof(ManagedToNative))]
[CustomMarshaller(typeof(TCollection<,,,...>), MarshalMode.ElementIn, typeof(ManagedToNative))]
[ContiguousCollectionMarshaller]
static class TMarshaller<T, U, V..., TUnmanagedElement> where TUnmanagedElement : unmanaged
{
    public static class ManagedToNative
    {
        public static int BufferSize { get; }
        public static TNative AllocateContainerForUnmanagedElements(TCollection managed, Span<TOther> buffer, out int numElements); // Can throw exceptions

        public static ReadOnlySpan<TManagedElement> GetManagedValuesSource(TCollection managed); // Can throw exceptions

        public static Span<TUnmanagedElement> GetUnmanagedValuesDestination(TNative unmanaged, int numElements); // Can throw exceptions

        public static ref TOther GetPinnableReference(TManaged managed);  // Optional. Can throw exceptions. Result pinnned and passed to Invoke.

        public static void Free(TNative unmanaged); // Optional. Should not throw exceptions
    }
}

```

### Stateless Unmanaged->Managed

```csharp
[CustomMarshaller(typeof(TCollection<,,,...>), MarshalMode.ManagedToUnmanagedOut, typeof(NativeToManaged))]
[CustomMarshaller(typeof(TCollection<,,,...>), MarshalMode.UnmanagedToManagedIn, typeof(NativeToManaged))]
[CustomMarshaller(typeof(TCollection<,,,...>), MarshalMode.ElementOut, typeof(NativeToManaged))]
[ContiguousCollectionMarshaller]
static class TMarshaller<T, U, V..., TUnmanagedElement> where TUnmanagedElement : unmanaged
{
    public static class NativeToManaged
    {
        public static TCollection AllocateContainerForManagedElements(TNative unmanaged, int numElements); // Can throw exceptions

        public static Span<TManagedElement> GetManagedValuesDestination(TCollection managed) => managed;  // Can throw exceptions

        public static ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(TNative unmanaged, int numElements);  // Can throw exceptions

        public static void Free(TNative unmanaged); // Optional. Should not throw exceptions.
    }
}

```

### Stateless Unmanaged->Managed with Guaranteed Unmarshalling

This shape directs the generator to emit the `ConvertToManagedFinally` call in the "GuaranteedUnmarshal" phase of marshalling.

```csharp
[CustomMarshaller(typeof(TCollection<,,,...>), MarshalMode.ManagedToUnmanagedOut, typeof(NativeToManaged))]
[ContiguousCollectionMarshaller]
static class TMarshaller<T, U, V..., TUnmanagedElement> where TUnmanagedElement : unmanaged
{
    public static class NativeToManaged
    {
        public static TCollection AllocateContainerForManagedElementsFinally(TNative unmanaged, int numElements); // Should not throw exceptions other than OutOfMemoryException.

        public static Span<TManagedElement> GetManagedValuesDestination(TCollection managed) => managed;  // Can throw exceptions

        public static ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(TNative unmanaged, int numElements);  // Can throw exceptions

        public static void Free(TNative unmanaged); // Optional. Should not throw exceptions.
    }
}

```

### Stateless Bidirectional
```csharp
[CustomMarshaller(typeof(TCollection<,,,...>), MarshalMode.ManagedToUnmanagedRef, typeof(Bidirectional))]
[CustomMarshaller(typeof(TCollection<,,,...>), MarshalMode.UnmanagedToManagedRef, typeof(Bidirectional))]
[CustomMarshaller(typeof(TCollection<,,,...>), MarshalMode.ElementRef, typeof(Bidirectional))]
[ContiguousCollectionMarshaller]
static class TMarshaller<T, U, V..., TUnmanagedElement> where TUnmanagedElement : unmanaged
{
    public static class Bidirectional
    {
        // Include members from each of the following:
        // - One Stateless Managed->Unmanaged Linear Collection shape
        // - One Stateless Unmanaged->Managed Linear Collection shape
    }
}

```

### Stateful Managed->Unmanaged

```csharp
[CustomMarshaller(typeof(TCollection<,,,...>), MarshalMode.ManagedToUnmanagedIn, typeof(ManagedToNative))]
[CustomMarshaller(typeof(TCollection<,,,...>), MarshalMode.UnmanagedToManagedOut, typeof(ManagedToNative))]
[ContiguousCollectionMarshaller]
static class TMarshaller<T, U, V..., TUnmanagedElement> where TUnmanagedElement : unmanaged
{
    public struct ManagedToNative // Can be ref struct
    {
        public ManagedToNative(); // Optional, can throw exceptions.

        public void FromManaged(TCollection collection); // Can throw exceptions.

        public ReadOnlySpan<TManagedElement> GetManagedValuesSource(); // Can throw exceptions.

        public Span<TUnmanagedElement> GetUnmanagedValuesDestination(); // Can throw exceptions.

        public ref TIgnored GetPinnableReference(); // Optional. Can throw exceptions.

        public TNative ToUnmanaged(); // Can throw exceptions.

        public static ref TOther GetPinnableReference(TCollection collection); // Optional. Can throw exceptions. Result pinnned and passed to Invoke.

        public void OnInvoked(); // Optional. Should not throw exceptions.
    }
}

```
### Stateful Managed->Unmanaged with Caller Allocated Buffer

The element type of the `Span` for the caller-allocated buffer can be any type that guarantees any alignment requirements.

```csharp
[CustomMarshaller(typeof(TCollection<,,,...>), MarshalMode.ManagedToUnmanagedIn, typeof(ManagedToNative))]
[ContiguousCollectionMarshaller]
static class TMarshaller<T, U, V..., TUnmanagedElement> where TUnmanagedElement : unmanaged
{
    public struct ManagedToNative // Can be ref struct
    {
        public static int BufferSize { get; }

        public ManagedToNative(); // Optional, can throw exceptions.

        public void FromManaged(TCollection collection, Span<TBuffer> buffer); // Can throw exceptions.

        public ReadOnlySpan<TManagedElement> GetManagedValuesSource(); // Can throw exceptions.

        public Span<TUnmanagedElement> GetUnmanagedValuesDestination(); // Can throw exceptions.

        public ref TIgnored GetPinnableReference(); // Optional. Can throw exceptions.

        public TNative ToUnmanaged(); // Can throw exceptions.

        public static ref TOther GetPinnableReference(TCollection collection); // Optional. Can throw exceptions. Result pinnned and passed to Invoke.

        public void OnInvoked(); // Optional. Should not throw exceptions.
    }
}

```

### Stateful Unmanaged->Managed

```csharp
[CustomMarshaller(typeof(TCollection<,,,...>), MarshalMode.ManagedToUnmanagedOut, typeof(NativeToManaged))]
[CustomMarshaller(typeof(TCollection<,,,...>), MarshalMode.UnmanagedToManagedIn, typeof(NativeToManaged))]
[ContiguousCollectionMarshaller]
static class TMarshaller<T, U, V..., TUnmanagedElement> where TUnmanagedElement : unmanaged
{
    public struct NativeToManaged // Can be ref struct
    {
        public NativeToManaged(); // Optional, can throw exceptions.

        public void FromUnmanaged(TNative value); // Should not throw exceptions.

        public ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(int numElements); // Can throw exceptions.

        public Span<TManagedElement> GetManagedValuesDestination(int numElements); // Can throw exceptions.

        public TCollection ToManaged(); // Can throw exceptions

        public void Free(); // Optional. Should not throw exceptions.
    }
}

```

### Stateful Unmanaged->Managed with Guaranteed Unmarshalling

```csharp
[CustomMarshaller(typeof(TCollection<,,,...>), MarshalMode.ManagedToUnmanagedOut, typeof(NativeToManaged))]
[ContiguousCollectionMarshaller]
static class TMarshaller<T, U, V..., TUnmanagedElement> where TUnmanagedElement : unmanaged
{
    public struct NativeToManaged // Can be ref struct
    {
        public NativeToManaged(); // Optional, can throw exceptions.

        public void FromUnmanaged(TNative value); // Should not throw exceptions.

        public ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(int numElements); // Can throw exceptions.

        public Span<TManagedElement> GetManagedValuesDestination(int numElements); // Can throw exceptions.

        public TCollection ToManagedFinally(); // Can throw exceptions

        public void Free(); // Optional. Should not throw exceptions.
    }
}

```

### Stateful Bidirectional
```csharp
[CustomMarshaller(typeof(TCollection<,,,...>), MarshalMode.ManagedToUnmanagedRef, typeof(Bidirectional))]
[CustomMarshaller(typeof(TCollection<,,,...>), MarshalMode.UnmanagedToManagedRef, typeof(Bidirectional))]
[ContiguousCollectionMarshaller]
static class TMarshaller<T, U, V..., TUnmanagedElement> where TUnmanagedElement : unmanaged
{
    public struct Bidirectional // Can be ref struct
    {
        // Include members from each of the following:
        // - One Stateful Managed->Unmanaged Linear Collection shape
        // - One Stateful Unmanaged->Managed Linear Collection shape
    }
}
```

## Optional Members In Shapes

There's a few optional members in the above shapes. This section explains what these members do and why they're optional.

### Free method

The `Free` method on each shape supports releasing any unmanaged (or managed in the stateful shapes) resources. This method is optional as the `Free` method is required to be called in a `finally` clause and emitting a `try-finally` block with only method calls to empty methods puts a lot of stress on the JIT to inline all of the methods and realize that they are no-ops to remove the `finally` clause. Additionally, just having the `try-finally` block wrapping the main code can cause some de-optimizations.

### OnInvoked method

This method is called after a stub successfully invokes the target code (unmanaged code in a P/Invoke scenario, managed code in a Reverse P/Invoke scenario). As this method would be called in a very large majority of cases in P/Invoke-style scenarios and has only limited utility (its main use is to provide a good place to call `GC.KeepAlive` that does not require a `try-finally` block), we decided to make it optional.

### Instance GetPinnableReference method on stateful shapes

The non-static `GetPinnableReference` method on stateful shapes is provided to enable pinning a managed value as part of the marshalling process. As some types don't have values that need to be pinned to help with marshalling and pinning has some overhead, this member is optional to make the overhead pay-for-play.

### Static GetPinnableReference method

The static GetPinnableReference method provides a mechanism to pin a managed value and pass down the pinned value directly to native code. This allows us to provide massive performance benefits and to match built-in interop semantics. Unlike the previous design that used the `GetPinnableReference` method on the managed type in some scenarios, this design allows the "interop" pinning rules to not match the easier-to-use `GetPinnableReference` instance method, which may have differing semantics (`Span<T>` and arrays being a prime example here). As many types aren't marshallable via only pinning, the generator does not require this method on every marshaller.

### `-Generated` method variants

These method variants provide a mechanism for a marshaller to state that it needs to be called during the "Generated Unmarshal" phase in the `finally` block to ensure that resources are not leaked. This feature is required only by the SafeHandle marshaller, so it is an optional extension to the model instead of being a required feature.

## Blittability

To determine which types are blittable and which are not, we will be following [Design 2 in StructMarshalling.md](StructMarshalling.md#determining-if-a-type-doesnt-need-marshalling).

## Using the marshallers

To use these marshallers the user would apply either the `NativeMarshallingAttribute` attribute to their type or a `MarshalUsingAttribute` at the marshalling location (field, parameter, or return value) with a marshalling type matching the same requirements as `NativeMarshallingAttribute`'s marshalling type.

The marshaller type must be an entry-point marshaller type as defined above and meet the following additional requirements:

- The type must either be:
  - Non-generic
  - A closed generic
  - An open generic with as many generic parameters with compatible constraints as the managed type (excluding one generic parameter if the marshaller has the `ContiguousCollectionMarshallerAttribute` attribute)
- If used in `NativeMarshallingAttribute`, the type should be at least as visible as the managed type.

Passing size info for parameters will be based to the [V1 design](SpanMarshallers.md#providing-additional-data-for-collection-marshalling) and the properties/fields on `MarshalUsingAttribute` will remain unchanged.

Here are some examples of using these new marshaller shapes with the `NativeMarshallingAttribute` and the `MarshalUsingAttribute`.

```csharp
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

[NativeMarshalling(typeof(HResultMarshaller))]
struct HResult
{
    private int hr;
}

[CustomMarshaller(typeof(HResult), MarshalMode.Default, typeof(HResultMarshaller))]
public static class HResultMarshaller
{
    public static int ConvertToUnmanaged(HResult hr);
    public static HResult ConvertToManaged(int hr);
}

public static class NativeLib
{
    [LibraryImport(nameof(NativeLib))]
    public static partial HResult CountArrayElements(
        [MarshalUsing(typeof(ArrayMarshaller<,>))] int[] array, // Unlike the V1 system, we'll allow open generics in the V2 system in MarshalUsing since there's an extra generic parameter that the user does not provide.
        out int numElements);
}

```
