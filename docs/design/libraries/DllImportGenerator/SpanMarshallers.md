# Support for Marshalling `(ReadOnly)Span<T>`

As part of the exit criteria for the DllImportGenerator experiment, we have decided to introduce support for marshalling `System.Span<T>` and `System.ReadOnlySpan<T>` into the DllImportGenerator-generated stubs. This document describes design decisions made during the implementation of these marshallers.

## Design 1: "Intrinsic" support for `(ReadOnly)Span<T>`

In this design, the default support for `(ReadOnly)Span<T>` is emitted into the marshalling stub directly and builds on the pattern we enabled for arrays.

### Default behavior

This section describes the behavior of the default emitted-in-stub marshallers.

By default, we will marshal `Span<T>` and `ReadOnlySpan<T>` similarly to array types. When possible, we will pin the `(ReadOnly)Span<T>`'s data and pass that down. When it is not possible to do so, we will try stack allocating scratch space for efficiency or allocate native memory with `Marshal.AllocCoTaskMem` for compatibility with arrays.

To support marshalling from native to managed, we will support the same `MarshalAsAttribute` properties that arrays support today.

When a `(ReadOnly)Span<T>` is marshalled from native to managed, we will allocate a managed array and copy the data from the native memory into the array.

### Empty spans

We have decided to match the managed semantics of `(ReadOnly)Span<T>` to provide the smoothest default experience for span users in interop. To assist developers who wish to transition from array parameters to span parameters, we will also provide an in-source marshaller that enables marshalling a span that wraps an empty array as a non-null pointer as an opt-in experience.

### Additional proposed in-source marshallers

As part of this design, we would also want to include some in-box marshallers that follow the design laid out in the [Struct Marshalling design doc](./StructMarshalling.md) to support some additional scenarios:

- A marshaler that marshals an empty span as a non-null pointer.
  - This marshaller would only support empty spans as it cannot correctly represent non-empty spans of non-blittable types.
- A marshaler that marshals out a pointer to the native memory as a Span instead of copying the data into a managed array.
  - This marshaller would only support blittable spans by design.
  - This marshaler will require the user to manually release the memory. Since this will be an opt-in marshaler, this scenario is already advanced and that additional requirement should be understandable to users who use this marshaler.
  - Since there is no mechansim to provide a collection length, the question of how to provide the span's length in this case is still unresolved. One option would be to always provide a length 1 span and require the user to create a new span with the correct size, but that feels like a bad design.

### Pros/Cons of Design 1

Pros:

- This design builds on the array support that already exists, providing implementation experience and a slightly easier implementation.
- As we use the same MarshalAs attributes that already support arrays, developers can easily migrate their usage of array parameters in source-generated P/Invokes to use the span types with minimal hassle.

Cons:

- Defining custom marshalers for non-empty spans of non-blittable types generically is impossible since the marshalling rules of the element's type cannot be known.
- Custom non-default marshalling of the span element types is impossible for non-built-in types.
- Inlining the span marshalling fully into the stub increases on-disk IL size.
- This design does not enable developers to easily define custom marshalling support for their own collection types, which may be desireable.
- The MarshalAs attributes will continue to fail to work on spans used in non-source-generated DllImports, so this would be the first instance of enabling the "old" MarshalAs model on a new type in the generated DllImports, which may or may not be undesirable.
  - The existing "native type marshalling" support cannot support marshalling collections of an unknown (at marshaller authoring time) non-blittable element type and cannot specify an element count for collections during unmarshalling.

## Design 2: "Out-line" default support with extensions to Native Type Marshalling for Contiguous Collections

An alternative option to fully inlining the stub would be to extend the model described in the [Struct Marshalling design doc](./StructMarshalling.md) to have custom support for collection-like types. By extending the model to be built with generic collection types in mind, many of the cons of the first approach would be resolved.

Span marshalling would still be implemented with similar semantics as mentioned above in the Empty Spans section. Additional marshallers would still be provided as mentioned in the Additional proposed in-source marshallers section, but the non-`null` span marshaller and the no-alloc span marshaller would be able to be used in all cases, not just for empty spans.

### Proposed extension to the custom type marshalling design

Introduce a new attribute named `GenericContiguousCollectionMarshallerAttribute`. This attribute would have the following shape:

```csharp
namespace System.Runtime.InteropServices
{ 
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public sealed class GenericContiguousCollectionMarshallerAttribute : Attribute
    {
        public GenericContiguousCollectionMarshallerAttribute();
    }
}
```

The attribute would be used with a collection type like `Span<T>` as follows:

```csharp
[NativeTypeMarshalling(typeof(DefaultSpanMarshaler<>))]
public ref struct Span<T>
{
  ...
}

[GenericContiguousCollectionMarshaller]
public ref struct DefaultSpanMarshaler<T>
{
  ...
}
```

The `GenericContiguousCollectionMarshallerAttribute` attribute is applied to a generic marshaler type with the "collection marshaller" shape described below. Since generic parameters cannot be used in attributes, open generic types will be permitted in the `NativeTypeMarshallingAttribute` constructor as long as they have the same arity as the type the attribute is applied to and generic parameters provided to the applied-to type can also be used to construct the type passed as a parameter.

#### Generic collection marshaller shape

A generic collection marshaller would be required to have the following shape, in addition to the requirements for marshaler types used with the `NativeTypeMarshallingAttribute`, excluding the constructors.

```csharp
[GenericContiguousCollectionMarshaller]
public struct GenericContiguousCollectionMarshallerImpl<T, U, V,...>
{
    // these constructors are required if marshalling from managed to native is supported.
    public GenericContiguousCollectionMarshallerImpl(GenericCollection<T, U, V, ...> collection, int nativeSizeOfElement);
    public GenericContiguousCollectionMarshallerImpl(GenericCollection<T, U, V, ...> collection, Span<byte> stackSpace, int nativeSizeOfElement); // optional
    
    public const int StackBufferSize = /* */; // required if the span-based constructor is supplied.

    /// <summary>
    /// A span that points to the memory where the managed values of the collection are stored (in the marshalling case) or should be stored (in the unmarshalling case).
    /// </summary>
    public Span<TCollectionElement> ManagedValues { get; }

    /// <summary>
    /// Set the expected length of the managed collection based on the parameter/return value/field marshalling information.
    /// Required only when unmarshalling is supported.
    /// </summary>
    public void SetUnmarshalledCollectionLength(int length);

    public IntPtr Value { get; set; }

    /// <summary>
    /// A span that points to the memory where the native values of the collection should be stored.
    /// </summary>
    public unsafe Span<byte> NativeValueStorage { get; }

    // The requirements on the Value property are the same as when used with `NativeTypeMarshallingAttribute`.
    // The property is required with the generic collection marshalling.
    public TNative Value { get; set; }
}
```

The constructors now require an additional `int` parameter specifying the native size of a collection element. The collection element type is represented as `TCollectionElement` above, and can be any type the marshaller defines. As the elements may be marshalled to types with different native sizes than managed, this enables the author of the generic collection marshaller to not need to know how to marshal the elements of the collection, just the collection structure itself.

When the elements of the collection are blittable, the marshaller will emit a block copy of the span `ManagedValues` to the destination `NativeValueStorage`. When the elements are not blittable, the marshaller will emit a loop that will marshal the elements of the managed span one at a time and store them in the `NativeValueStorage` span.

This would enable similar performance metrics as the current support for arrays as well as Design 1's support for the span types when the element type is blittable.

#### Providing additional data for collection marshalling

As part of collection marshalling, there needs to be a mechanism for the user to tell the stub code generator how many elements are in the native collection when unmarshalling. For parity with the previous system, there also needs to be a mechanism to describe how to marshal the elements of the collection. This proposal adds the following members to the `MarshalUsingAttribute` attribute to enable this and other features:

```diff

- [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Field)]
+ [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Field, AllowMultiple=true)]
public class MarshalUsingAttribute : Attribute
{
+    public MarshalUsingAttribute() {}
     public MarshalUsingAttribute(Type nativeType) {}
+    public string CountElementName { get; set; }
+    public int ConstantElementCount { get; set; }
+    public int ElementIndirectionLevel { get; set; }
+    public const string ReturnsCountValue = "return-value";
}
```

The `MarshalUsingAttribute` will now provide a `CountElementName` property that will point to a parameter (or a field in a struct-marshalling context) whose value will hold the number of native collection elements, or to the return value if the value of `CountElementName` is `ReturnsCountValue`. The `ConstantElementCount` property allows users to provide a constant collection length.

> Open Question:
> Should combining `CountElementName` and `ConstantElementCount` in the same attribute be allowed?
> With the `MarshalAs` marshalling, `SizeParamIndex` and `SizeConst` can be combined and the resulting size will be `paramValue(SizeParamIndex) + SizeConst`.

To support supplying information about collection element counts, a parameterless constructor is added to the `MarshalUsingAttribute` type. The default constructor specifies that the code generator should use the information in the attribute but use the default marshalling rules for the type.

The `ElementIndirectionLevel` property is added to support supplying marshalling info for element types in a collection. For example, if the user is passing a `List<List<Foo>>` from managed to native code, they could provide the following attributes to specify marshalling rules for the outer and inner lists and `Foo` separately:

```csharp
private static partial void Bar([MarshalUsing(typeof(ListAsArrayMarshaller<List<Foo>>), CountElementName = nameof(count)), MarshalUsing(ConstantElementCount = 10, ElementIndirectionLevel = 1), MarshalUsing(typeof(FooMarshaler), ElementIndirectionLevel = 2)] List<List<Foo>> foos, int count);
```

Multiple `MarshalUsing` attributes can only be supplied on the same parameter or return value if the `ElementIndirectionLevel` property is set to distinct values. One `MarshalUsing` attribute per parameter or return value can leave the `ElementIndirectionLevel` property unset. This attribute controls the marshalling of the collection object passed in as the parameter. The sequence of managed types for `ElementIndirectionLevel` is based on the elements of the `ManagedValues` span on the collection marshaller of the previous indirection level. For example, for the marshalling info for `ElementIndirectionLevel = 1` above, the managed type is the type of the following C# expression: `ListAsArrayMarshaller<List<Foo>>.ManagedValues[0]`.

Alternatively, the `MarshalUsingAttribute` could provide a `Type ElementNativeType { get; set; }` property instead of an `ElementIndirectionLevel` property and support specifying the native type of the element of the collection this way. However, this design would block support for marshalling collections of collections.

#### Example: Using generic collection marshalling for spans

This design could be used to provide a default marshaller for spans and arrays. Below is an example simple marshaller for `Span<T>`. This design does not include all possible optimizations, such as stack allocation, for simpilicity of the example.

```csharp
[GenericContiguousCollectionMarshaller]
public ref struct SpanMarshaler<T>
{
    private Span<T> managedCollection;

    private int nativeElementSize;

    public SpanMarshaler(Span<T> collection, int nativeSizeOfElement)
    {
       managedCollection = collection;
       Value = Marshal.AllocCoTaskMem(collection.Length * nativeSizeOfElement);
       nativeElementSize = nativeSizeOfElement;
       nativeElementSize = nativeSizeOfElement;
    }

    public Span<T> ManagedValues => managedCollection;

    public void SetUnmarshalledCollectionLength(int length)
    {
       managedCollection = new T[value];
    }

    public IntPtr Value { get; set; }

    public unsafe Span<byte> NativeValueStorage => MemoryMarshal.CreateSpan(ref *(byte*)(Value), Length);

    public Span<T> ToManaged() => managedCollection;

    public void FreeNative()
    {
      if (Value != IntPtr.Zero)
      {
          Marshal.FreeCoTaskMem(Value);
      }
    }
}
```

The following example would show the expected stub for the provided signature (assuming that `Span<T>` has a `[NativeMarshalling(typeof(SpanMarshaller<>))]` attribute):

```csharp
struct WrappedInt
{
  private int value;

  public WrappedInt(int w)
  {
     value = w.i;
  }

  public int ToManaged() => value;
}

[GeneratedDllImport("Native")]
[return:MarshalUsing(CountElementName = nameof(length))]
public static partial Span<int> DuplicateValues([MarshalUsing(typeof(WrappedInt), ElementIndirectionLevel = 1)] Span<int> values, int length);

// Generated stub:
public static partial unsafe Span<int> DuplicateValues(Span<int> values, int length)
{
     SpanMarshaller<int> __values_marshaller = new SpanMarshaller<int>(values, sizeof(WrappedInt));
     for (int i = 0; i < __values_marshaller.ManagedValues.Length; ++i)
     {
        WrappedInt native = new WrappedInt(__values_marshaller.ManagedValues[i]);
        MemoryMarshal.Write(__values_marshaller.NativeValueStorage.Slice(sizeof(WrappedInt) * i), ref native);
     }

     IntPtr __retVal_native = __PInvoke__(__values_marshaller.Value, length);
     SpanMarshaller<int> __retVal_marshaller = new
     {
        Value = __retVal_native
     };
     __retVal_marshaller.SetUnmarshalledCollectionLength(length);
     MemoryMarshal.Cast<byte, int>(__retVal_marshaller.NativeValueStorage).CopyTo(__retVal_marshaller.ManagedValues);
     return __retVal_marshaller.ToManaged();

     [DllImport("Native", EntryPoint="DuplicateValues")]
     static extern IntPtr __PInvoke__(IntPtr values, int length);
}
```

This design could also be applied to support the built-in array marshalling if it is desired to move that marshalling out of the stub and into shared code.

#### Future extension to the above model: Non-contiguous collection support

If a managed or native representation of a collection has a non-contiguous element layout, then developers currently will need to convert to or from array/span types at the interop boundary. This section proposes an API that would enable developers to convert directly between a managed and native non-contiguous collection layout as part of marshalling.

A new attribute named `GenericCollectionMarshaller` attribute could be added that would specify that the collection is noncontiguous in either managed or native representations. Then additional methods should be added to the generic collection model, and some methods would be removed:

```diff
- [GenericContiguousCollectionMarshaller]
+ [GenericCollectionMarshaller]
public struct GenericContiguousCollectionMarshallerImpl<T, U, V,...>
{
    // these constructors are required if marshalling from managed to native is supported.
    public GenericContiguousCollectionMarshallerImpl(GenericCollection<T, U, V, ...> collection, int nativeSizeOfElements);
    public GenericContiguousCollectionMarshallerImpl(GenericCollection<T, U, V, ...> collection, Span<byte> stackSpace, int nativeSizeOfElements); // optional
    
    public const int StackBufferSize = /* */; // required if the span-based constructor is supplied.

-    public Span<TCollectionElement> ManagedValues { get; }

-    public void SetUnmarshalledCollectionLength(int length);

    public IntPtr Value { get; set; }

-    public unsafe Span<byte> NativeValueStorage { get; }

    // The requirements on the Value property are the same as when used with `NativeTypeMarshallingAttribute`.
    // The property is required with the generic collection marshalling.
    public TNative Value { get; set; }

+    public ref byte GetOffsetForNativeValueAtIndex(int index);
+    public TCollectionElement GetManagedValueAtIndex(int index);
+    public TCollectionElement SetManagedValueAtIndex(int index);
+    public int Count { get; set; }
}
```

The `GetManagedValueAtIndex` method and `Count` getter are used in the process of marshalling from managed to native. The generated code will iterate through `Count` elements (retrieved through `GetManagedValueAtIndex`) and assign their marshalled result to the address represented by `GetOffsetForNativeValueAtIndex` called with the same index. Then either the `Value` property getter will be called or the marshaller's `GetPinnableReference` method will be called, depending on if pinning is supported in the current scenario.

The `SetManagedValueAtIndex` method and the `Count` setter are used in the process of marshalling from native to managed. The `Count` property will be set to the number of elements that the native collection contains, and the `Value` property will be assigned the result value from native code. Then the stub will iterate through the native collection `Count` times, calling `GetOffsetForNativeValueAtIndex` to get the offset of the native value and calling `SetManagedValueAtIndex` to set the unmarshalled managed value at that index.

### Pros/Cons of Design 2

Pros:

- Collection type owners do not need to know how to marshal the elements of the collection.
- Custom non-default marshalling of collections of non-blittable types supported with the same code as blittable types.
- Sharing code for marshalling a given collection type reduces IL size on disk.
- Developers can easily enable marshalling their own collection types without needing to modify the source generator.
- Makes no assumptions about native collection layout, so collections like linked lists can be easily supported.

Cons:

- Introduces more attribute types into the BCL.
- Introduces more complexity in the marshalling type model.
  - It may be worth describing the required members (other than constructors) in interfaces just to simplify the mental load of which members are required for which scenarios.
    - A set of interfaces (one for managed-to-native members, one for native-to-managed members, and one for the sequential-specific members) could replace the `GenericContiguousCollectionMarshaller` attribute.
- The base proposal only supports contiguous collections.
  - The feeling at time of writing is that we are okay asking developers to convert to/from arrays or spans at the interop boundary.
