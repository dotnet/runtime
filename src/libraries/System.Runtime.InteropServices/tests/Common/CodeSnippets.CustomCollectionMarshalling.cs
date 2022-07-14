// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Interop.UnitTests
{
    internal static partial class CodeSnippets
    {
        public static class CustomCollectionMarshalling
        {
            public static string TestCollection(bool defineNativeMarshalling = true) => $@"
{(defineNativeMarshalling ? "[NativeMarshalling(typeof(Marshaller<,>))]" : string.Empty)}
class TestCollection<T> {{}}
";

            public static string CollectionOutParameter(string collectionType, string predeclaration = "") => MarshalUsingCollectionOutConstantLength(collectionType, predeclaration);
            public static string CollectionReturnType(string collectionType, string predeclaration = "") => MarshalUsingCollectionReturnConstantLength(collectionType, predeclaration);
            public const string NonBlittableElement = @"
[NativeMarshalling(typeof(ElementMarshaller))]
struct Element
{
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
    public bool b;
#pragma warning restore CS0649
}
";
            public const string ElementMarshaller = @"
[CustomMarshaller(typeof(Element), MarshalMode.ElementIn, typeof(ElementMarshaller))]
[CustomMarshaller(typeof(Element), MarshalMode.ElementRef, typeof(ElementMarshaller))]
[CustomMarshaller(typeof(Element), MarshalMode.ElementOut, typeof(ElementMarshaller))]
static class ElementMarshaller
{
    public struct Native { }
    public static Native ConvertToUnmanaged(Element e) => throw null;
    public static Element ConvertToManaged(Native n) => throw null;
}
";
            public const string ElementIn = @"
[CustomMarshaller(typeof(Element), MarshalMode.ElementIn, typeof(ElementMarshaller))]
static class ElementMarshaller
{
    public struct Native { }
    public static Native ConvertToUnmanaged(Element e) => throw null;
    public static Element ConvertToManaged(Native n) => throw null;
}
";
            public const string ElementOut = @"
[CustomMarshaller(typeof(Element), MarshalMode.ElementOut, typeof(ElementMarshaller))]
static class ElementMarshaller
{
    public struct Native { }
    public static Native ConvertToUnmanaged(Element e) => throw null;
    public static Element ConvertToManaged(Native n) => throw null;
}
";
            public const string CustomIntMarshaller = @"
[CustomMarshaller(typeof(int), MarshalMode.ElementIn, typeof(CustomIntMarshaller))]
[CustomMarshaller(typeof(int), MarshalMode.ElementRef, typeof(CustomIntMarshaller))]
[CustomMarshaller(typeof(int), MarshalMode.ElementOut, typeof(CustomIntMarshaller))]
static class CustomIntMarshaller
{
    public struct Native { }
    public static Native ConvertToUnmanaged(int e) => throw null;
    public static int ConvertToManaged(Native n) => throw null;
}
";
            public static class Stateless
            {
                public const string In = @"
[CustomMarshaller(typeof(TestCollection<>), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller<,>))]
[ContiguousCollectionMarshaller]
static unsafe class Marshaller<T, TUnmanagedElement> where TUnmanagedElement : unmanaged
{
    public static byte* AllocateContainerForUnmanagedElements(TestCollection<T> managed, out int numElements) => throw null;
    public static System.ReadOnlySpan<T> GetManagedValuesSource(TestCollection<T> managed) => throw null;
    public static System.Span<TUnmanagedElement> GetUnmanagedValuesDestination(byte* unmanaged, int numElements) => throw null;
}
";
                public const string InPinnable = @"
[CustomMarshaller(typeof(TestCollection<>), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller<,>))]
[ContiguousCollectionMarshaller]
static unsafe class Marshaller<T, TUnmanagedElement> where TUnmanagedElement : unmanaged
{
    public static byte* AllocateContainerForUnmanagedElements(TestCollection<T> managed, out int numElements) => throw null;
    public static System.ReadOnlySpan<T> GetManagedValuesSource(TestCollection<T> managed) => throw null;
    public static System.Span<TUnmanagedElement> GetUnmanagedValuesDestination(byte* unmanaged, int numElements) => throw null;

    public static ref byte GetPinnableReference(TestCollection<T> managed) => throw null;
}
";
                public const string InBuffer = @"
[CustomMarshaller(typeof(TestCollection<>), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller<,>))]
[ContiguousCollectionMarshaller]
static unsafe class Marshaller<T, TUnmanagedElement> where TUnmanagedElement : unmanaged
{
    public const int BufferSize = 0x100;
    public static byte* AllocateContainerForUnmanagedElements(TestCollection<T> managed, System.Span<byte> buffer, out int numElements) => throw null;
    public static System.ReadOnlySpan<T> GetManagedValuesSource(TestCollection<T> managed) => throw null;
    public static System.Span<TUnmanagedElement> GetUnmanagedValuesDestination(byte* unmanaged, int numElements) => throw null;
}
";
                public const string Ref = @"
[CustomMarshaller(typeof(TestCollection<>), MarshalMode.Default, typeof(Marshaller<,>))]
[ContiguousCollectionMarshaller]
static unsafe class Marshaller<T, TUnmanagedElement> where TUnmanagedElement : unmanaged
{
    public static byte* AllocateContainerForUnmanagedElements(TestCollection<T> managed, out int numElements) => throw null;
    public static System.ReadOnlySpan<T> GetManagedValuesSource(TestCollection<T> managed) => throw null;
    public static System.Span<TUnmanagedElement> GetUnmanagedValuesDestination(byte* unmanaged, int numElements) => throw null;

    public static TestCollection<T> AllocateContainerForManagedElements(byte* unmanaged, int length) => throw null;
    public static System.Span<T> GetManagedValuesDestination(TestCollection<T> managed) => throw null;
    public static System.ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(byte* unmanaged, int numElements) => throw null;
}
";
                public const string RefNested = @"
[CustomMarshaller(typeof(TestCollection<>), MarshalMode.Default, typeof(Marshaller<,>.Nested.Ref))]
[ContiguousCollectionMarshaller]
static unsafe class Marshaller<T, TUnmanagedElement> where TUnmanagedElement : unmanaged
{
    internal static class Nested
    {
        internal static class Ref
        {
            public static byte* AllocateContainerForUnmanagedElements(TestCollection<T> managed, out int numElements) => throw null;
            public static System.ReadOnlySpan<T> GetManagedValuesSource(TestCollection<T> managed) => throw null;
            public static System.Span<TUnmanagedElement> GetUnmanagedValuesDestination(byte* unmanaged, int numElements) => throw null;

            public static TestCollection<T> AllocateContainerForManagedElements(byte* unmanaged, int length) => throw null;
            public static System.Span<T> GetManagedValuesDestination(TestCollection<T> managed) => throw null;
            public static System.ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(byte* unmanaged, int numElements) => throw null;
        }
    }
}
";
                public const string Out = @"
[CustomMarshaller(typeof(TestCollection<>), MarshalMode.ManagedToUnmanagedOut, typeof(Marshaller<,>))]
[ContiguousCollectionMarshaller]
static unsafe class Marshaller<T, TUnmanagedElement> where TUnmanagedElement : unmanaged
{
    public static TestCollection<T> AllocateContainerForManagedElements(byte* unmanaged, int length) => throw null;
    public static System.Span<T> GetManagedValuesDestination(TestCollection<T> managed) => throw null;
    public static System.ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(byte* unmanaged, int numElements) => throw null;
}
";
                public static string ByValue<T>() => ByValue(typeof(T).ToString());
                public static string ByValue(string elementType) => BasicParameterByValue($"TestCollection<{elementType}>", DisableRuntimeMarshalling)
                    + TestCollection()
                    + In;

                public static string ByValueWithPinning<T>() => ByValueWithPinning(typeof(T).ToString());
                public static string ByValueWithPinning(string elementType) => BasicParameterByValue($"TestCollection<{elementType}>", DisableRuntimeMarshalling)
                    + TestCollection()
                    + InPinnable;

                public static string ByValueCallerAllocatedBuffer<T>() => ByValueCallerAllocatedBuffer(typeof(T).ToString());
                public static string ByValueCallerAllocatedBuffer(string elementType) => BasicParameterByValue($"TestCollection<{elementType}>", DisableRuntimeMarshalling)
                    + TestCollection()
                    + InBuffer;

                public static string DefaultMarshallerParametersAndModifiers<T>() => DefaultMarshallerParametersAndModifiers(typeof(T).ToString());
                public static string DefaultMarshallerParametersAndModifiers(string elementType) => MarshalUsingCollectionCountInfoParametersAndModifiers($"TestCollection<{elementType}>")
                    + TestCollection()
                    + Ref;

                public static string CustomMarshallerParametersAndModifiers<T>() => CustomMarshallerParametersAndModifiers(typeof(T).ToString());
                public static string CustomMarshallerParametersAndModifiers(string elementType) => MarshalUsingCollectionParametersAndModifiers($"TestCollection<{elementType}>", $"Marshaller<,>")
                    + TestCollection(defineNativeMarshalling: false)
                    + Ref;

                public static string CustomMarshallerReturnValueLength<T>() => CustomMarshallerReturnValueLength(typeof(T).ToString());
                public static string CustomMarshallerReturnValueLength(string elementType) => MarshalUsingCollectionReturnValueLength($"TestCollection<{elementType}>", $"Marshaller<,>")
                    + TestCollection(defineNativeMarshalling: false)
                    + Ref;

                public static string NativeToManagedOnlyOutParameter<T>() => NativeToManagedOnlyOutParameter(typeof(T).ToString());
                public static string NativeToManagedOnlyOutParameter(string elementType) => CollectionOutParameter($"TestCollection<{elementType}>")
                    + TestCollection()
                    + Out;

                public static string NativeToManagedOnlyReturnValue<T>() => NativeToManagedOnlyReturnValue(typeof(T).ToString());
                public static string NativeToManagedOnlyReturnValue(string elementType) => CollectionReturnType($"TestCollection<{elementType}>")
                    + TestCollection()
                    + Out;

                public static string NestedMarshallerParametersAndModifiers<T>() => NestedMarshallerParametersAndModifiers(typeof(T).ToString());
                public static string NestedMarshallerParametersAndModifiers(string elementType) => MarshalUsingCollectionCountInfoParametersAndModifiers($"TestCollection<{elementType}>")
                    + TestCollection()
                    + RefNested;

                public static string NonBlittableElementParametersAndModifiers => DefaultMarshallerParametersAndModifiers("Element")
                    + NonBlittableElement
                    + ElementMarshaller;

                public static string NonBlittableElementByValue => ByValue("Element")
                    + NonBlittableElement
                    + ElementIn;

                public static string NonBlittableElementNativeToManagedOnlyOutParameter => NativeToManagedOnlyOutParameter("Element")
                    + NonBlittableElement
                    + ElementOut;

                public static string NonBlittableElementNativeToManagedOnlyReturnValue => NativeToManagedOnlyOutParameter("Element")
                    + NonBlittableElement
                    + ElementOut;

                public static string GenericCollectionMarshallingArityMismatch => BasicParameterByValue("TestCollection<int>", DisableRuntimeMarshalling)
                    + @"
[NativeMarshalling(typeof(Marshaller<,,>))]
class TestCollection<T> {}

[CustomMarshaller(typeof(TestCollection<>), MarshalMode.Default, typeof(Marshaller<,,>))]
[ContiguousCollectionMarshaller]
static unsafe class Marshaller<T, U, TUnmanagedElement> where TUnmanagedElement : unmanaged
{
    public static byte* AllocateContainerForUnmanagedElements(TestCollection<T> managed, out int numElements) => throw null;
    public static System.ReadOnlySpan<T> GetManagedValuesSource(TestCollection<T> managed) => throw null;
    public static System.Span<TUnmanagedElement> GetUnmanagedValuesDestination(byte* unmanaged, int numElements) => throw null;

    public static TestCollection<T> AllocateContainerForManagedElements(byte* unmanaged, int length) => throw null;
    public static System.Span<T> GetManagedValuesDestination(TestCollection<T> managed) => throw null;
    public static System.ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(byte* unmanaged, int numElements) => throw null;
}
";

                public static string CustomElementMarshalling => CodeSnippets.CustomElementMarshalling("TestCollection<int>", "CustomIntMarshaller")
                    + TestCollection()
                    + Ref
                    + CustomIntMarshaller;

                public static string CustomElementMarshallingDuplicateElementIndirectionDepth => $@"
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{DisableRuntimeMarshalling}
partial class Test
{{
    [LibraryImport(""DoesNotExist"")]
    public static partial void Method(
        [MarshalUsing(typeof(CustomIntMarshaller), ElementIndirectionDepth = 1)] [MarshalUsing(typeof(CustomIntMarshaller), ElementIndirectionDepth = 1)] TestCollection<int> p);
}}
"
                    + TestCollection()
                    + In
                    + CustomIntMarshaller;

                public static string CustomElementMarshallingUnusedElementIndirectionDepth => $@"
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
{DisableRuntimeMarshalling}
partial class Test
{{
    [LibraryImport(""DoesNotExist"")]
    public static partial void Method(
        [MarshalUsing(typeof(CustomIntMarshaller), ElementIndirectionDepth = 2)] TestCollection<int> p);
}}
"
                    + TestCollection()
                    + In
                    + CustomIntMarshaller;
            }

            public static class Stateful
            {
                public const string In = @"
[ContiguousCollectionMarshaller]
[CustomMarshaller(typeof(TestCollection<>), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller<,>.In))]
static unsafe class Marshaller<T, TUnmanagedElement> where TUnmanagedElement : unmanaged
{
    public ref struct In
    {
        public void FromManaged(TestCollection<T> managed) => throw null;
        public byte* ToUnmanaged() => throw null;
        public System.ReadOnlySpan<T> GetManagedValuesSource() => throw null;
        public System.Span<TUnmanagedElement> GetUnmanagedValuesDestination() => throw null;        
    }
}
";
                public const string InPinnable = @"
[ContiguousCollectionMarshaller]
[CustomMarshaller(typeof(TestCollection<>), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller<,>.In))]
static unsafe class Marshaller<T, TUnmanagedElement> where TUnmanagedElement : unmanaged
{
    public ref struct In
    {
        public void FromManaged(TestCollection<T> managed) => throw null;
        public byte* ToUnmanaged() => throw null;
        public System.ReadOnlySpan<T> GetManagedValuesSource() => throw null;
        public System.Span<TUnmanagedElement> GetUnmanagedValuesDestination() => throw null;        
        public ref byte GetPinnableReference() => throw null;
    }
}
";
                public const string InStaticPinnable = @"
[ContiguousCollectionMarshaller]
[CustomMarshaller(typeof(TestCollection<>), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller<,>.In))]
static unsafe class Marshaller<T, TUnmanagedElement> where TUnmanagedElement : unmanaged
{
    public ref struct In
    {
        public void FromManaged(TestCollection<T> managed) => throw null;
        public byte* ToUnmanaged() => throw null;
        public System.ReadOnlySpan<T> GetManagedValuesSource() => throw null;
        public System.Span<TUnmanagedElement> GetUnmanagedValuesDestination() => throw null;        
        public static ref byte GetPinnableReference(TestCollection<T> managed) => throw null;
    }
}
";
                public const string InBuffer = @"
[ContiguousCollectionMarshaller]
[CustomMarshaller(typeof(TestCollection<>), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller<,>.In))]
static unsafe class Marshaller<T, TUnmanagedElement> where TUnmanagedElement : unmanaged
{
    public ref struct In
    {
        public static int BufferSize { get; }
        public void FromManaged(TestCollection<T> managed, System.Span<TUnmanagedElement> buffer) => throw null;
        public byte* ToUnmanaged() => throw null;
        public System.ReadOnlySpan<T> GetManagedValuesSource() => throw null;
        public System.Span<TUnmanagedElement> GetUnmanagedValuesDestination() => throw null;        
    }
}
";
                public const string Ref = @"
[ContiguousCollectionMarshaller]
[CustomMarshaller(typeof(TestCollection<>), MarshalMode.Default, typeof(Marshaller<,>.Ref))]
static unsafe class Marshaller<T, TUnmanagedElement> where TUnmanagedElement : unmanaged
{
    public ref struct Ref
    {
        public void FromManaged(TestCollection<T> managed) => throw null;
        public byte* ToUnmanaged() => throw null;
        public System.ReadOnlySpan<T> GetManagedValuesSource() => throw null;
        public System.Span<TUnmanagedElement> GetUnmanagedValuesDestination() => throw null;

        public void FromUnmanaged(byte* value) => throw null;
        public TestCollection<T> ToManaged() => throw null;
        public System.Span<T> GetManagedValuesDestination(int numElements) => throw null;
        public System.ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(int numElements) => throw null;
    }
}
";
                public const string Out = @"
[ContiguousCollectionMarshaller]
[CustomMarshaller(typeof(TestCollection<>), MarshalMode.ManagedToUnmanagedOut, typeof(Marshaller<,>.Out))]
static unsafe class Marshaller<T, TUnmanagedElement> where TUnmanagedElement : unmanaged
{
    public ref struct Out
    {
        public void FromUnmanaged(byte* value) => throw null;
        public TestCollection<T> ToManaged() => throw null;
        public System.Span<T> GetManagedValuesDestination(int numElements) => throw null;
        public System.ReadOnlySpan<TUnmanagedElement> GetUnmanagedValuesSource(int numElements) => throw null;
    }
}
";
                public static string ByValue<T>() => ByValue(typeof(T).ToString());
                public static string ByValue(string elementType) => BasicParameterByValue($"TestCollection<{elementType}>", DisableRuntimeMarshalling)
                    + TestCollection()
                    + In;

                public static string ByValueWithPinning<T>() => ByValueWithPinning(typeof(T).ToString());
                public static string ByValueWithPinning(string elementType) => BasicParameterByValue($"TestCollection<{elementType}>", DisableRuntimeMarshalling)
                    + TestCollection()
                    + InPinnable;

                public static string ByValueWithStaticPinning<T>() => ByValueWithStaticPinning(typeof(T).ToString());
                public static string ByValueWithStaticPinning(string elementType) => BasicParameterByValue($"TestCollection<{elementType}>", DisableRuntimeMarshalling)
                    + TestCollection()
                    + InStaticPinnable;

                public static string ByValueCallerAllocatedBuffer<T>() => ByValueCallerAllocatedBuffer(typeof(T).ToString());
                public static string ByValueCallerAllocatedBuffer(string elementType) => BasicParameterByValue($"TestCollection<{elementType}>", DisableRuntimeMarshalling)
                    + TestCollection()
                    + InBuffer;

                public static string DefaultMarshallerParametersAndModifiers<T>() => DefaultMarshallerParametersAndModifiers(typeof(T).ToString());
                public static string DefaultMarshallerParametersAndModifiers(string elementType) => MarshalUsingCollectionCountInfoParametersAndModifiers($"TestCollection<{elementType}>")
                    + TestCollection()
                    + Ref;

                public static string CustomMarshallerParametersAndModifiers<T>() => CustomMarshallerParametersAndModifiers(typeof(T).ToString());
                public static string CustomMarshallerParametersAndModifiers(string elementType) => MarshalUsingCollectionParametersAndModifiers($"TestCollection<{elementType}>", $"Marshaller<,>")
                    + TestCollection(defineNativeMarshalling: false)
                    + Ref;

                public static string CustomMarshallerReturnValueLength<T>() => CustomMarshallerReturnValueLength(typeof(T).ToString());
                public static string CustomMarshallerReturnValueLength(string elementType) => MarshalUsingCollectionReturnValueLength($"TestCollection<{elementType}>", $"Marshaller<,>")
                    + TestCollection(defineNativeMarshalling: false)
                    + Ref;

                public static string NativeToManagedOnlyOutParameter<T>() => NativeToManagedOnlyOutParameter(typeof(T).ToString());
                public static string NativeToManagedOnlyOutParameter(string elementType) => CollectionOutParameter($"TestCollection<{elementType}>")
                    + TestCollection()
                    + Out;

                public static string NativeToManagedOnlyReturnValue<T>() => NativeToManagedOnlyReturnValue(typeof(T).ToString());
                public static string NativeToManagedOnlyReturnValue(string elementType) => CollectionReturnType($"TestCollection<{elementType}>")
                    + TestCollection()
                    + Out;

                public static string NonBlittableElementParametersAndModifiers => DefaultMarshallerParametersAndModifiers("Element")
                    + NonBlittableElement
                    + ElementMarshaller;

                public static string NonBlittableElementByValue => ByValue("Element")
                    + NonBlittableElement
                    + ElementIn;

                public static string NonBlittableElementNativeToManagedOnlyOutParameter => NativeToManagedOnlyOutParameter("Element")
                    + NonBlittableElement
                    + ElementOut;

                public static string NonBlittableElementNativeToManagedOnlyReturnValue => NativeToManagedOnlyOutParameter("Element")
                    + NonBlittableElement
                    + ElementOut;

                public static string CustomElementMarshalling => CodeSnippets.CustomElementMarshalling("TestCollection<int>", "CustomIntMarshaller")
                    + TestCollection()
                    + Ref
                    + CustomIntMarshaller;
            }
        }
    }
}
