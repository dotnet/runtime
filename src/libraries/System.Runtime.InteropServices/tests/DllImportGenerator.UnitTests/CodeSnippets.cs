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
        /// Declaration using various methods to compute constants in C#.
        /// </summary>
        public static readonly string UseCSharpFeaturesForConstants = @"
using System.Runtime.InteropServices;
partial class Test
{
    private const bool IsTrue = true;
    private const bool IsFalse = false;
    private const string EntryPointName = nameof(Test) + nameof(IsFalse);

    [GeneratedDllImport(nameof(Test),
        BestFitMapping = 0 != 1,
        CallingConvention = (CallingConvention)1,
        CharSet = (CharSet)2,
        EntryPoint = EntryPointName,
        ExactSpelling = IsTrue,
        PreserveSig = IsFalse,
        SetLastError = !IsFalse,
        ThrowOnUnmappableChar = !IsTrue)]
    public static partial void Method();
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
        /// Apply MarshalAsAttribute to parameters and return types.
        /// </summary>
        public static readonly string MarshalAsAttributeOnTypes = @"
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
    [return: MarshalAs(UnmanagedType.LPWStr)]
    public static partial string Method1([MarshalAs(UnmanagedType.LPStr)]string t);

    [GeneratedDllImport(""DoesNotExist"")]
    [return: MarshalAs(UnmanagedType.LPWStr)]
    public static partial string Method2([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(NS.MyCustomMarshaler), MarshalCookie=""COOKIE1"")]string t);

    [GeneratedDllImport(""DoesNotExist"")]
    [return: MarshalAs(UnmanagedType.LPWStr)]
    public static partial string Method3([MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = ""NS.MyCustomMarshaler"", MarshalCookie=""COOKIE2"")]string t);
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

        public static string BasicParametersAndModifiers<T>() => BasicParametersAndModifiers(typeof(T).ToString());

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
    }
}
