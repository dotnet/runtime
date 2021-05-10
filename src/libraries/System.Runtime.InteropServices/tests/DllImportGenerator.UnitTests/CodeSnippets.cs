using System.Runtime.InteropServices;

namespace DllImportGenerator.UnitTests
{
    internal static class CodeSnippets
    {
        /// <summary>
        /// Trivial declaration of GeneratedDllImport usage
        /// </summary>
        public static readonly string TrivialClassDeclarations = @"
using System.Runtime.InteropServices;
partial class Basic
{
    [GeneratedDllImportAttribute(""DoesNotExist"")]
    public static partial void Method1();

    [GeneratedDllImport(""DoesNotExist"")]
    public static partial void Method2();

    [System.Runtime.InteropServices.GeneratedDllImportAttribute(""DoesNotExist"")]
    public static partial void Method3();

    [System.Runtime.InteropServices.GeneratedDllImport(""DoesNotExist"")]
    public static partial void Method4();
}
";
        /// <summary>
        /// Trivial declaration of GeneratedDllImport usage
        /// </summary>
        public static readonly string TrivialStructDeclarations = @"
using System.Runtime.InteropServices;
partial struct Basic
{
    [GeneratedDllImportAttribute(""DoesNotExist"")]
    public static partial void Method1();

    [GeneratedDllImport(""DoesNotExist"")]
    public static partial void Method2();

    [System.Runtime.InteropServices.GeneratedDllImportAttribute(""DoesNotExist"")]
    public static partial void Method3();

    [System.Runtime.InteropServices.GeneratedDllImport(""DoesNotExist"")]
    public static partial void Method4();
}
";

        /// <summary>
        /// Declaration with multiple attributes
        /// </summary>
        public static readonly string MultipleAttributes = @"
using System;
using System.Runtime.InteropServices;

sealed class DummyAttribute : Attribute
{
    public DummyAttribute() { }
}

sealed class Dummy2Attribute : Attribute
{
    public Dummy2Attribute(string input) { }
}

partial class Test
{
    [DummyAttribute]
    [GeneratedDllImport(""DoesNotExist""), Dummy2Attribute(""string value"")]
    public static partial void Method();
}
";

        /// <summary>
        /// Validate nested namespaces are handled
        /// </summary>
        public static readonly string NestedNamespace = @"
using System.Runtime.InteropServices;
namespace NS
{
    namespace InnerNS
    {
        partial class Test
        {
            [GeneratedDllImport(""DoesNotExist"")]
            public static partial void Method1();
        }
    }
}
namespace NS.InnerNS
{
    partial class Test
    {
        [GeneratedDllImport(""DoesNotExist"")]
        public static partial void Method2();
    }
}
";

        /// <summary>
        /// Validate nested types are handled.
        /// </summary>
        public static readonly string NestedTypes = @"
using System.Runtime.InteropServices;
namespace NS
{
    partial class OuterClass
    {
        partial class InnerClass
        {
            [GeneratedDllImport(""DoesNotExist"")]
            public static partial void Method();
        }
    }
    partial struct OuterStruct
    {
        partial struct InnerStruct
        {
            [GeneratedDllImport(""DoesNotExist"")]
            public static partial void Method();
        }
    }
    partial class OuterClass
    {
        partial struct InnerStruct
        {
            [GeneratedDllImport(""DoesNotExist"")]
            public static partial void Method();
        }
    }
    partial struct OuterStruct
    {
        partial class InnerClass
        {
            [GeneratedDllImport(""DoesNotExist"")]
            public static partial void Method();
        }
    }
}
";

        /// <summary>
        /// Declaration with user defined EntryPoint.
        /// </summary>
        public static readonly string UserDefinedEntryPoint = @"
using System.Runtime.InteropServices;
partial class Test
{
    [GeneratedDllImport(""DoesNotExist"", EntryPoint=""UserDefinedEntryPoint"")]
    public static partial void NotAnExport();
}
";

        /// <summary>
        /// Declaration with all DllImport named arguments.
        /// </summary>
        public static readonly string AllDllImportNamedArguments = @"
using System.Runtime.InteropServices;
partial class Test
{
    [GeneratedDllImport(""DoesNotExist"",
        BestFitMapping = false,
        CallingConvention = CallingConvention.Cdecl,
        CharSet = CharSet.Unicode,
        EntryPoint = ""UserDefinedEntryPoint"",
        ExactSpelling = true,
        PreserveSig = false,
        SetLastError = true,
        ThrowOnUnmappableChar = true)]
    public static partial void Method();
}
";

        /// <summary>
        /// Declaration with all supported DllImport named arguments.
        /// </summary>
        public static readonly string AllSupportedDllImportNamedArguments = @"
using System.Runtime.InteropServices;
partial class Test
{
    [GeneratedDllImport(""DoesNotExist"",
        CallingConvention = CallingConvention.Cdecl,
        CharSet = CharSet.Unicode,
        EntryPoint = ""UserDefinedEntryPoint"",
        ExactSpelling = true,
        PreserveSig = false,
        SetLastError = true)]
    public static partial void Method();
}
";

        /// <summary>
        /// Declaration using various methods to compute constants in C#.
        /// </summary>
        public static readonly string UseCSharpFeaturesForConstants = @"
using System.Runtime.InteropServices;
partial class Test
{
    private const bool IsTrue = true;
    private const bool IsFalse = false;
    private const string EntryPointName = nameof(Test) + nameof(IsFalse);
    private const int One = 1;
    private const int Two = 2;

    [GeneratedDllImport(nameof(Test),
        CallingConvention = (CallingConvention)1,
        CharSet = (CharSet)2,
        EntryPoint = EntryPointName,
        ExactSpelling = 0 != 1,
        PreserveSig = IsTrue,
        SetLastError = IsFalse)]
    public static partial void Method1();

    [GeneratedDllImport(nameof(Test),
        CallingConvention = (CallingConvention)One,
        CharSet = (CharSet)Two,
        EntryPoint = EntryPointName,
        ExactSpelling = One != Two,
        PreserveSig = !IsFalse,
        SetLastError = !IsTrue)]
    public static partial void Method2();
}
";

        /// <summary>
        /// Declaration with default parameters.
        /// </summary>
        public static readonly string DefaultParameters = @"
using System.Runtime.InteropServices;
partial class Test
{
    [GeneratedDllImport(""DoesNotExist"")]
    public static partial void Method(int t = 0);
}
";

        /// <summary>
        /// Declaration with LCIDConversionAttribute.
        /// </summary>
        public static readonly string LCIDConversionAttribute = @"
using System.Runtime.InteropServices;
partial class Test
{
    [LCIDConversion(0)]
    [GeneratedDllImport(""DoesNotExist"")]
    public static partial void Method();
}
";

        /// <summary>
        /// Define a MarshalAsAttribute with a customer marshaller to parameters and return types.
        /// </summary>
        public static readonly string MarshalAsCustomMarshalerOnTypes = @"
using System;
using System.Runtime.InteropServices;
namespace NS
{
    class MyCustomMarshaler : ICustomMarshaler
    {
        static ICustomMarshaler GetInstance(string pstrCookie)
            => new MyCustomMarshaler();

        public void CleanUpManagedData(object ManagedObj)
            => throw new NotImplementedException();

        public void CleanUpNativeData(IntPtr pNativeData)
            => throw new NotImplementedException();

        public int GetNativeDataSize()
            => throw new NotImplementedException();

        public IntPtr MarshalManagedToNative(object ManagedObj)
            => throw new NotImplementedException();

        public object MarshalNativeToManaged(IntPtr pNativeData)
            => throw new NotImplementedException();
    }
}

partial class Test
{
    [GeneratedDllImport(""DoesNotExist"")]
    [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(NS.MyCustomMarshaler), MarshalCookie=""COOKIE1"")]
    public static partial bool Method1([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(NS.MyCustomMarshaler), MarshalCookie=""COOKIE2"")]bool t);

    [GeneratedDllImport(""DoesNotExist"")]
    [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = ""NS.MyCustomMarshaler"", MarshalCookie=""COOKIE3"")]
    public static partial bool Method2([MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = ""NS.MyCustomMarshaler"", MarshalCookie=""COOKIE4"")]bool t);
}
";

        /// <summary>
        /// Declaration with user defined attributes with prefixed name.
        /// </summary>
        public static readonly string UserDefinedPrefixedAttributes = @"
using System;
using System.Runtime.InteropServices;

namespace System.Runtime.InteropServices
{
    // Prefix with ATTRIBUTE so the lengths will match during check.
    sealed class ATTRIBUTEGeneratedDllImportAttribute : Attribute
    {
        public ATTRIBUTEGeneratedDllImportAttribute(string a) { }
    }
}

partial class Test
{
    [ATTRIBUTEGeneratedDllImportAttribute(""DoesNotExist"")]
    public static partial void Method1();

    [ATTRIBUTEGeneratedDllImport(""DoesNotExist"")]
    public static partial void Method2();

    [System.Runtime.InteropServices.ATTRIBUTEGeneratedDllImport(""DoesNotExist"")]
    public static partial void Method3();
}
";

        /// <summary>
        /// Declaration with parameters with <see cref="CharSet"/> set.
        /// </summary>
        public static string BasicParametersAndModifiersWithCharSet(string typename, CharSet value) => @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"", CharSet = CharSet.{value})]
    public static partial {typename} Method(
        {typename} p,
        in {typename} pIn,
        ref {typename} pRef,
        out {typename} pOut);
}}
";

        public static string BasicParametersAndModifiersWithCharSet<T>(CharSet value) =>
            BasicParametersAndModifiersWithCharSet(typeof(T).ToString(), value);

        /// <summary>
        /// Declaration with parameters.
        /// </summary>
        public static string BasicParametersAndModifiers(string typeName) => @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"")]
    public static partial {typeName} Method(
        {typeName} p,
        in {typeName} pIn,
        ref {typeName} pRef,
        out {typeName} pOut);
}}";

        /// <summary>
        /// Declaration with parameters.
        /// </summary>
        public static string BasicParametersAndModifiersNoRef(string typeName) => @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"")]
    public static partial {typeName} Method(
        {typeName} p,
        in {typeName} pIn,
        out {typeName} pOut);
}}";

        /// <summary>
        /// Declaration with parameters and unsafe.
        /// </summary>
        public static string BasicParametersAndModifiersUnsafe(string typeName) => @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"")]
    public static unsafe partial {typeName} Method(
        {typeName} p,
        in {typeName} pIn,
        ref {typeName} pRef,
        out {typeName} pOut);
}}";

        public static string BasicParametersAndModifiers<T>() => BasicParametersAndModifiers(typeof(T).ToString());

        /// <summary>
        /// Declaration with [In, Out] style attributes on a by-value parameter.
        /// </summary>
        public static string ByValueParameterWithModifier(string typeName, string attributeName) => @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"")]
    public static partial void Method(
        [{attributeName}] {typeName} p);
}}";

        public static string ByValueParameterWithModifier<T>(string attributeName) => ByValueParameterWithModifier(typeof(T).ToString(), attributeName);

        /// <summary>
        /// Declaration with by-value parameter with custom name.
        /// </summary>
        public static string ByValueParameterWithName(string methodName, string paramName) => @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"")]
    public static partial void {methodName}(
        int {paramName});
}}";

        /// <summary>
        /// Declaration with parameters with MarshalAs.
        /// </summary>
        public static string MarshalAsParametersAndModifiers(string typeName, UnmanagedType unmanagedType) => @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"")]
    [return: MarshalAs(UnmanagedType.{unmanagedType})]
    public static partial {typeName} Method(
        [MarshalAs(UnmanagedType.{unmanagedType})] {typeName} p,
        [MarshalAs(UnmanagedType.{unmanagedType})] in {typeName} pIn,
        [MarshalAs(UnmanagedType.{unmanagedType})] ref {typeName} pRef,
        [MarshalAs(UnmanagedType.{unmanagedType})] out {typeName} pOut);
}}
";

        /// <summary>
        /// Declaration with parameters with MarshalAs.
        /// </summary>
        public static string MarshalAsParametersAndModifiersUnsafe(string typeName, UnmanagedType unmanagedType) => @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"")]
    [return: MarshalAs(UnmanagedType.{unmanagedType})]
    public static unsafe partial {typeName} Method(
        [MarshalAs(UnmanagedType.{unmanagedType})] {typeName} p,
        [MarshalAs(UnmanagedType.{unmanagedType})] in {typeName} pIn,
        [MarshalAs(UnmanagedType.{unmanagedType})] ref {typeName} pRef,
        [MarshalAs(UnmanagedType.{unmanagedType})] out {typeName} pOut);
}}
";

        public static string MarshalAsParametersAndModifiers<T>(UnmanagedType unmanagedType) => MarshalAsParametersAndModifiers(typeof(T).ToString(), unmanagedType);

        /// <summary>
        /// Declaration with enum parameters.
        /// </summary>
        public static string EnumParameters => @$"
using System.Runtime.InteropServices;
using NS;

namespace NS
{{
    enum MyEnum {{ A, B, C }}
}}

partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"")]
    public static partial MyEnum Method(
        MyEnum p,
        in MyEnum pIn,
        ref MyEnum pRef,
        out MyEnum pOut);
}}";

        /// <summary>
        /// Declaration with pointer parameters.
        /// </summary>
        public static string PointerParameters<T>() => BasicParametersAndModifiersUnsafe($"{typeof(T)}*");

        /// <summary>
        /// Declaration with PreserveSig = false.
        /// </summary>
        public static string PreserveSigFalse(string typeName) => @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"", PreserveSig = false)]
    public static partial {typeName} Method1();

    [GeneratedDllImport(""DoesNotExist"", PreserveSig = false)]
    public static partial {typeName} Method2({typeName} p);
}}";

        public static string PreserveSigFalse<T>() => PreserveSigFalse(typeof(T).ToString());

        /// <summary>
        /// Declaration with PreserveSig = false and void return.
        /// </summary>
        public static readonly string PreserveSigFalseVoidReturn = @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"", PreserveSig = false)]
    public static partial void Method();
}}";

        public static string DelegateParametersAndModifiers = BasicParametersAndModifiers("MyDelegate") + @"
delegate int MyDelegate(int a);";
        public static string DelegateMarshalAsParametersAndModifiers = MarshalAsParametersAndModifiers("MyDelegate", UnmanagedType.FunctionPtr) + @"
delegate int MyDelegate(int a);";

        public static string BlittableStructParametersAndModifiers = BasicParametersAndModifiers("MyStruct") + @"
#pragma warning disable CS0169
[BlittableType]
struct MyStruct
{
    private int i;
    private short s;
}";
        public static string GenericBlittableStructParametersAndModifiers = BasicParametersAndModifiers("MyStruct<int>") + @"
#pragma warning disable CS0169
[BlittableType]
struct MyStruct<T>
{
    private T t;
    private short s;
}";

        public static string ArrayParametersAndModifiers(string elementType) => $@"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"")]
    [return:MarshalAs(UnmanagedType.LPArray, SizeConst=10)]
    public static partial {elementType}[] Method(
        {elementType}[] p,
        in {elementType}[] pIn,
        int pRefSize,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=2)] ref {elementType}[] pRef,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=5, SizeConst=4)] out {elementType}[] pOut,
        out int pOutSize
        );
}}";

        public static string ArrayParametersAndModifiers<T>() => ArrayParametersAndModifiers(typeof(T).ToString());

        public static string ArrayParameterWithSizeParam(string sizeParamType, bool isByRef) => $@"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"")]
    public static partial void Method(
        {(isByRef ? "ref" : "")} {sizeParamType} pRefSize,
        [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)] ref int[] pRef
        );
}}";

        public static string ArrayParameterWithSizeParam<T>(bool isByRef) => ArrayParameterWithSizeParam(typeof(T).ToString(), isByRef);


        public static string ArrayParameterWithNestedMarshalInfo(string elementType, UnmanagedType nestedMarshalInfo) => $@"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"")]
    public static partial void Method(
        [MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.{nestedMarshalInfo})] {elementType}[] pRef
        );
}}";

        public static string ArrayParameterWithNestedMarshalInfo<T>(UnmanagedType nestedMarshalType) => ArrayParameterWithNestedMarshalInfo(typeof(T).ToString(), nestedMarshalType);
        
        public static string ArrayPreserveSigFalse(string elementType) => $@"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"", PreserveSig = false)]
    [return:MarshalAs(UnmanagedType.LPArray, SizeConst=10)]
    public static partial {elementType}[] Method1();

    [GeneratedDllImport(""DoesNotExist"", PreserveSig = false)]
    [return:MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)]
    public static partial {elementType}[] Method2(int i);
}}";

        public static string ArrayPreserveSigFalse<T>() => ArrayPreserveSigFalse(typeof(T).ToString());

        /// <summary>
        /// Declaration with parameters with MarshalAs.
        /// </summary>
        public static string MarshalUsingParametersAndModifiers(string typeName, string nativeTypeName) => @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"")]
    [return: MarshalUsing(typeof({nativeTypeName}))]
    public static partial {typeName} Method(
        [MarshalUsing(typeof({nativeTypeName}))] {typeName} p,
        [MarshalUsing(typeof({nativeTypeName}))] in {typeName} pIn,
        [MarshalUsing(typeof({nativeTypeName}))] ref {typeName} pRef,
        [MarshalUsing(typeof({nativeTypeName}))] out {typeName} pOut);
}}
";

        public static string CustomStructMarshallingParametersAndModifiers = BasicParametersAndModifiers("S") + @"
[NativeMarshalling(typeof(Native))]
struct S
{
    public bool b;
}

struct Native
{
    private int i;
    public Native(S s)
    {
        i = s.b ? 1 : 0;
    }

    public S ToManaged() => new S { b = i != 0 };
}
";

        public static string CustomStructMarshallingMarshalUsingParametersAndModifiers = MarshalUsingParametersAndModifiers("S", "Native") + @"
struct S
{
    public bool b;
}

struct Native
{
    private int i;
    public Native(S s)
    {
        i = s.b ? 1 : 0;
    }

    public S ToManaged() => new S { b = i != 0 };
}
";

        public static string CustomStructMarshallingStackallocParametersAndModifiersNoRef = BasicParametersAndModifiersNoRef("S") + @"
[NativeMarshalling(typeof(Native))]
struct S
{
    public bool b;
}

struct Native
{
    private int i;
    public Native(S s, System.Span<byte> b)
    {
        i = s.b ? 1 : 0;
    }

    public S ToManaged() => new S { b = i != 0 };

    public const int StackBufferSize = 1;
}
";
        public static string CustomStructMarshallingStackallocOnlyRefParameter = BasicParameterWithByRefModifier("ref", "S") + @"
[NativeMarshalling(typeof(Native))]
struct S
{
    public bool b;
}

struct Native
{
    private int i;
    public Native(S s, System.Span<byte> b)
    {
        i = s.b ? 1 : 0;
    }

    public S ToManaged() => new S { b = i != 0 };

    public const int StackBufferSize = 1;
}
";
        public static string CustomStructMarshallingOptionalStackallocParametersAndModifiers = BasicParametersAndModifiers("S") + @"
[NativeMarshalling(typeof(Native))]
struct S
{
    public bool b;
}

struct Native
{
    private int i;
    public Native(S s, System.Span<byte> b)
    {
        i = s.b ? 1 : 0;
    }
    public Native(S s)
    {
        i = s.b ? 1 : 0;
    }

    public S ToManaged() => new S { b = i != 0 };

    public const int StackBufferSize = 1;
}
";

        public static string CustomStructMarshallingStackallocValuePropertyParametersAndModifiersNoRef = BasicParametersAndModifiersNoRef("S") + @"
[NativeMarshalling(typeof(Native))]
struct S
{
    public bool b;
}

struct Native
{
    public Native(S s, System.Span<byte> b)
    {
        Value = s.b ? 1 : 0;
    }

    public S ToManaged() => new S { b = Value != 0 };

    public int Value { get; set; }

    public const int StackBufferSize = 1;
}
";
        public static string CustomStructMarshallingValuePropertyParametersAndModifiers = BasicParametersAndModifiers("S") + @"
[NativeMarshalling(typeof(Native))]
struct S
{
    public bool b;
}

struct Native
{
    public Native(S s)
    {
        Value = s.b ? 1 : 0;
    }

    public S ToManaged() => new S { b = Value != 0 };

    public int Value { get; set; }
}
";
        public static string CustomStructMarshallingPinnableParametersAndModifiers = BasicParametersAndModifiers("S") + @"
[NativeMarshalling(typeof(Native))]
class S
{
    public int i;

    public ref int GetPinnableReference() => ref i;
}

unsafe struct Native
{
    private int* ptr;
    public Native(S s)
    {
        ptr = (int*)Marshal.AllocHGlobal(sizeof(int));
        *ptr = s.i;
    }

    public S ToManaged() => new S { i = *ptr };

    public nint Value
    {
        get => (nint)ptr;
        set => ptr = (int*)value;
    }
}
";

        public static string CustomStructMarshallingNativeTypePinnable = @"
using System.Runtime.InteropServices;
using System;

[NativeMarshalling(typeof(Native))]
class S
{
    public byte c;
}

unsafe ref struct Native
{
    private byte* ptr;
    private Span<byte> stackBuffer;

    public Native(S s) : this()
    {
        ptr = (byte*)Marshal.AllocCoTaskMem(sizeof(byte));
        *ptr = s.c;
    }

    public Native(S s, Span<byte> buffer) : this()
    {
        stackBuffer = buffer;
        stackBuffer[0] = s.c;
    }

    public ref byte GetPinnableReference() => ref (ptr != null ? ref *ptr : ref stackBuffer.GetPinnableReference());

    public S ToManaged()
    {
        return new S { c = *ptr };
    }

    public byte* Value
    {
        get => ptr != null ? ptr : throw new InvalidOperationException();
        set => ptr = value;
    }

    public void FreeNative()
    {
        if (ptr != null)
        {
            Marshal.FreeCoTaskMem((IntPtr)ptr);
        }
    }

    public const int StackBufferSize = 1;
}

partial class Test
{
    [GeneratedDllImport(""DoesNotExist"")]
    public static partial void Method(
        S s,
        in S sIn);
}
";

        public static string CustomStructMarshallingByRefValueProperty = BasicParametersAndModifiers("S") + @"
[NativeMarshalling(typeof(Native))]
class S
{
    public byte c = 0;
}

unsafe struct Native
{
    private S value;

    public Native(S s) : this()
    {
        value = s;
    }

    public ref byte Value { get => ref value.c; }
}
";

        public static string BasicParameterWithByRefModifier(string byRefKind, string typeName) => @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"")]
    public static partial void Method(
        {byRefKind} {typeName} p);
}}";

        public static string BasicParameterByValue(string typeName) => @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"")]
    public static partial void Method(
        {typeName} p);
}}";

        public static string BasicReturnType(string typeName) => @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"")]
    public static partial {typeName} Method();
}}";

        public static string CustomStructMarshallingManagedToNativeOnlyOutParameter => BasicParameterWithByRefModifier("out", "S")  + @"
[NativeMarshalling(typeof(Native))]
[StructLayout(LayoutKind.Sequential)]
struct S
{
    public bool b;
}

struct Native
{
    private int i;
    public Native(S s)
    {
        i = s.b ? 1 : 0;
    }
}
";

        public static string CustomStructMarshallingManagedToNativeOnlyReturnValue => BasicReturnType("S")  + @"
[NativeMarshalling(typeof(Native))]
[StructLayout(LayoutKind.Sequential)]
struct S
{
    public bool b;
}

struct Native
{
    private int i;
    public Native(S s)
    {
        i = s.b ? 1 : 0;
    }
}
";

        public static string CustomStructMarshallingNativeToManagedOnlyInParameter => BasicParameterWithByRefModifier("in", "S")  + @"
[NativeMarshalling(typeof(Native))]
struct S
{
    public bool b;
}

[StructLayout(LayoutKind.Sequential)]
struct Native
{
    private int i;
    public S ToManaged() => new S { b = i != 0 };
}
";

        public static string ArrayMarshallingWithCustomStructElementWithValueProperty => ArrayParametersAndModifiers("IntStructWrapper") + @"
[NativeMarshalling(typeof(IntStructWrapperNative))]
public struct IntStructWrapper
{
    public int Value;
}

public struct IntStructWrapperNative
{
    public IntStructWrapperNative(IntStructWrapper managed)
    {
        Value = managed.Value;
    }

    public int Value { get; set; }

    public IntStructWrapper ToManaged() => new IntStructWrapper { Value = Value };
}
";

        public static string ArrayMarshallingWithCustomStructElement => ArrayParametersAndModifiers("IntStructWrapper") + @"
[NativeMarshalling(typeof(IntStructWrapperNative))]
public struct IntStructWrapper
{
    public int Value;
}

public struct IntStructWrapperNative
{
    private int value;

    public IntStructWrapperNative(IntStructWrapper managed)
    {
        value = managed.Value;
    }

    public IntStructWrapper ToManaged() => new IntStructWrapper { Value = value };
}
";

        public static string SafeHandleWithCustomDefaultConstructorAccessibility(bool privateCtor) => BasicParametersAndModifiers("MySafeHandle") + $@"
class MySafeHandle : SafeHandle
{{
    {(privateCtor ? "private" : "public")} MySafeHandle() : base(System.IntPtr.Zero, true) {{ }}

    public override bool IsInvalid => handle == System.IntPtr.Zero;

    protected override bool ReleaseHandle() => true;
}}";

        public static string PreprocessorIfAroundFullFunctionDefinition(string define) =>
            @$"
partial class Test
{{
#if {define}
    [System.Runtime.InteropServices.GeneratedDllImport(""DoesNotExist"")]
    public static partial int Method(
        int p,
        in int pIn,
        out int pOut);
#endif
}}";

        public static string PreprocessorIfAroundFullFunctionDefinitionWithFollowingFunction(string define) =>
            @$"
using System.Runtime.InteropServices;
partial class Test
{{
#if {define}
    [GeneratedDllImport(""DoesNotExist"")]
    public static partial int Method(
        int p,
        in int pIn,
        out int pOut);
#endif
    public static int Method2(
        SafeHandle p) => throw null;
}}";

        public static string PreprocessorIfAfterAttributeAroundFunction(string define) =>
            @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"")]
#if {define}
    public static partial int Method(
        int p,
        in int pIn,
        out int pOut);
#else
    public static partial int Method2(
        int p,
        in int pIn,
        out int pOut);
#endif
}}";

        public static string PreprocessorIfAfterAttributeAroundFunctionAdditionalFunctionAfter(string define) =>
            @$"
using System.Runtime.InteropServices;
partial class Test
{{
    [GeneratedDllImport(""DoesNotExist"")]
#if {define}
    public static partial int Method(
        int p,
        in int pIn,
        out int pOut);
#else
    public static partial int Method2(
        int p,
        in int pIn,
        out int pOut);
#endif
    public static int Foo() => throw null;
}}";
    }
}
