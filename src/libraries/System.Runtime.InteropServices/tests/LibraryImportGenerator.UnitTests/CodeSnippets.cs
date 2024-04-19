// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Interop.UnitTests;

namespace LibraryImportGenerator.UnitTests
{
    internal class CodeSnippets : ICustomMarshallingSignatureTestProvider
    {
        public CodeSnippets() { }
        string ICustomMarshallingSignatureTestProvider.BasicParameterByValue(string type, string preDeclaration)
            => BasicParameterByValue(type, preDeclaration);

        string ICustomMarshallingSignatureTestProvider.BasicParameterWithByRefModifier(string byRefModifier, string type, string preDeclaration)
            => BasicParameterWithByRefModifier(byRefModifier, type, preDeclaration);

        string ICustomMarshallingSignatureTestProvider.BasicReturnType(string type, string preDeclaration)
            => BasicReturnType(type, preDeclaration);

        string ICustomMarshallingSignatureTestProvider.BasicParametersAndModifiers(string typeName, string preDeclaration)
            => BasicParametersAndModifiers(typeName, preDeclaration);

        string ICustomMarshallingSignatureTestProvider.BasicParametersAndModifiersNoRef(string typeName, string preDeclaration)
            => BasicParametersAndModifiersNoRef(typeName, preDeclaration);

        string ICustomMarshallingSignatureTestProvider.MarshalUsingParametersAndModifiers(string type, string marshallerType, string preDeclaration)
            => MarshalUsingParametersAndModifiers(type, marshallerType, preDeclaration);

        string ICustomMarshallingSignatureTestProvider.MarshalUsingCollectionCountInfoParametersAndModifiers(string collectionType)
            => MarshalUsingCollectionCountInfoParametersAndModifiers(collectionType);

        string ICustomMarshallingSignatureTestProvider.MarshalUsingCollectionParametersAndModifiers(string type, string marshallerType)
            => MarshalUsingCollectionParametersAndModifiers(type, marshallerType);

        string ICustomMarshallingSignatureTestProvider.MarshalUsingCollectionOutConstantLength(string type, string predeclaration)
            => MarshalUsingCollectionOutConstantLength(type, predeclaration);

        string ICustomMarshallingSignatureTestProvider.MarshalUsingCollectionReturnConstantLength(string type, string predeclaration)
            => MarshalUsingCollectionReturnConstantLength(type, predeclaration);

        string ICustomMarshallingSignatureTestProvider.MarshalUsingCollectionReturnValueLength(string type, string marshallerType)
            => MarshalUsingCollectionReturnValueLength(type, marshallerType);

        string ICustomMarshallingSignatureTestProvider.CustomElementMarshalling(string type, string marshallerType, string preDeclaration)
            => CustomElementMarshalling(type, marshallerType, preDeclaration);

        /// <summary>
        /// Partially define attribute for pre-.NET 7.0
        /// </summary>
        public static readonly string LibraryImportAttributeDeclaration = """
            namespace System.Runtime.InteropServices
            {
                internal enum StringMarshalling
                {
                    Custom = 0,
                    Utf8,
                    Utf16,
                }

                sealed class LibraryImportAttribute : System.Attribute
                {
                    public LibraryImportAttribute(string a) { }
                    public StringMarshalling StringMarshalling { get; set; }
                    public Type StringMarshallingCustomType { get; set; }
                }
            }
            """;

        /// <summary>
        /// Trivial declaration of LibraryImport usage
        /// </summary>
        public static readonly string TrivialClassDeclarations = """
            using System.Runtime.InteropServices;
            partial class {|#0:Basic|}
            {
                [LibraryImportAttribute("DoesNotExist")]
                public static partial void Method1();

                [LibraryImport("DoesNotExist")]
                public static partial void Method2();

                [System.Runtime.InteropServices.LibraryImportAttribute("DoesNotExist")]
                public static partial void Method3();

                [System.Runtime.InteropServices.LibraryImport("DoesNotExist")]
                public static partial void Method4();
            }
            """;
        /// <summary>
        /// Trivial declaration of LibraryImport usage
        /// </summary>
        public static readonly string TrivialStructDeclarations = """
            using System.Runtime.InteropServices;
            partial struct Basic
            {
                [LibraryImportAttribute("DoesNotExist")]
                public static partial void Method1();

                [LibraryImport("DoesNotExist")]
                public static partial void Method2();

                [System.Runtime.InteropServices.LibraryImportAttribute("DoesNotExist")]
                public static partial void Method3();

                [System.Runtime.InteropServices.LibraryImport("DoesNotExist")]
                public static partial void Method4();
            }
            """;

        /// <summary>
        /// Declaration with multiple attributes
        /// </summary>
        public static readonly string MultipleAttributes = """
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
                [LibraryImport("DoesNotExist"), Dummy2Attribute("string value")]
                public static partial void Method();
            }
            """;

        /// <summary>
        /// Validate nested namespaces are handled
        /// </summary>
        public static readonly string NestedNamespace = """
            using System.Runtime.InteropServices;
            namespace NS
            {
                namespace InnerNS
                {
                    partial class Test
                    {
                        [LibraryImport("DoesNotExist")]
                        public static partial void Method1();
                    }
                }
            }
            namespace NS.InnerNS
            {
                partial class Test
                {
                    [LibraryImport("DoesNotExist")]
                    public static partial void Method2();
                }
            }
            """;

        /// <summary>
        /// Validate nested types are handled.
        /// </summary>
        public static readonly string NestedTypes = """
            using System.Runtime.InteropServices;
            namespace NS
            {
                partial class OuterClass
                {
                    partial class InnerClass
                    {
                        [LibraryImport("DoesNotExist")]
                        public static partial void Method();
                    }
                }
                partial struct OuterStruct
                {
                    partial struct InnerStruct
                    {
                        [LibraryImport("DoesNotExist")]
                        public static partial void Method();
                    }
                }
                partial class OuterClass
                {
                    partial struct InnerStruct
                    {
                        [LibraryImport("DoesNotExist")]
                        public static partial void Method();
                    }
                }
                partial struct OuterStruct
                {
                    partial class InnerClass
                    {
                        [LibraryImport("DoesNotExist")]
                        public static partial void Method();
                    }
                }
            }
            """;

        /// <summary>
        /// Containing type with and without unsafe
        /// </summary>
        public static readonly string UnsafeContext = """
            using System.Runtime.InteropServices;
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                public static partial void Method1();
            }
            unsafe partial class Test
            {
                [LibraryImport("DoesNotExist")]
                public static partial int* Method2();
            }
            """;
        /// <summary>
        /// Declaration with user defined EntryPoint.
        /// </summary>
        public static readonly string UserDefinedEntryPoint = """
            using System.Runtime.InteropServices;
            partial class Test
            {
                [LibraryImport("DoesNotExist", EntryPoint="UserDefinedEntryPoint")]
                public static partial void NotAnExport();
            }
            """;

        /// <summary>
        /// Declaration with all LibraryImport named arguments.
        /// </summary>
        public static readonly string AllLibraryImportNamedArguments = """
            using System.Runtime.InteropServices;
            partial class Test
            {
                [LibraryImport("DoesNotExist",
                    StringMarshalling = StringMarshalling.Utf16,
                    EntryPoint = "UserDefinedEntryPoint",
                    SetLastError = true)]
                public static partial void Method();
            }
            """;

        /// <summary>
        /// Declaration using various methods to compute constants in C#.
        /// </summary>
        public static readonly string UseCSharpFeaturesForConstants = """
            using System.Runtime.InteropServices;
            partial class Test
            {
                private const bool IsTrue = true;
                private const bool IsFalse = false;
                private const string EntryPointName = nameof(Test) + nameof(IsFalse);
                private const int One = 1;
                private const int Two = 2;

                [LibraryImport(nameof(Test),
                    StringMarshalling = (StringMarshalling)2,
                    EntryPoint = EntryPointName,
                    SetLastError = IsFalse)]
                public static partial void Method1();

                [LibraryImport(nameof(Test) + "Suffix",
                    StringMarshalling = (StringMarshalling)Two,
                    EntryPoint = EntryPointName + "Suffix",
                    SetLastError = !IsTrue)]
                public static partial void Method2();

                [LibraryImport($"{nameof(Test)}Suffix",
                    StringMarshalling = (StringMarshalling)2,
                    EntryPoint = $"{EntryPointName}Suffix",
                    SetLastError = 0 != 1)]
                public static partial void Method3();
            }
            """;

        /// <summary>
        /// Declaration with default parameters.
        /// </summary>
        public static readonly string DefaultParameters = """
            using System.Runtime.InteropServices;
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                public static partial void Method(int t = 0);
            }
            """;

        /// <summary>
        /// Declaration with LCIDConversionAttribute.
        /// </summary>
        public static readonly string LCIDConversionAttribute = """
            using System.Runtime.InteropServices;
            partial class Test
            {
                [{|#0:LCIDConversion(0)|}]
                [LibraryImport("DoesNotExist")]
                public static partial void Method();
            }
            """;

        /// <summary>
        /// Define a MarshalAsAttribute with a customer marshaller to parameters and return types.
        /// </summary>
        public static readonly string MarshalAsCustomMarshalerOnTypes = """
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
                [LibraryImport("DoesNotExist")]
                [return: {|#0:MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(NS.MyCustomMarshaler), MarshalCookie="COOKIE1")|}]
                public static partial bool {|#1:Method1|}([{|#2:MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(NS.MyCustomMarshaler), MarshalCookie="COOKIE2")|}]bool {|#3:t|});

                [LibraryImport("DoesNotExist")]
                [return: {|#4:MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "NS.MyCustomMarshaler", MarshalCookie="COOKIE3")|}]
                public static partial bool {|#5:Method2|}([{|#6:MarshalAs(UnmanagedType.CustomMarshaler, MarshalType = "NS.MyCustomMarshaler", MarshalCookie="COOKIE4")|}]bool {|#7:t|});
            }
            """;

        /// <summary>
        /// Declaration with user defined attributes with prefixed name.
        /// </summary>
        public static readonly string UserDefinedPrefixedAttributes = """
            using System;
            using System.Runtime.InteropServices;

            namespace System.Runtime.InteropServices
            {
                // Prefix with ATTRIBUTE so the lengths will match during check.
                sealed class ATTRIBUTELibraryImportAttribute : Attribute
                {
                    public ATTRIBUTELibraryImportAttribute(string a) { }
                }
            }

            partial class Test
            {
                [ATTRIBUTELibraryImportAttribute("DoesNotExist")]
                public static partial void {|CS8795:Method1|}();

                [ATTRIBUTELibraryImport("DoesNotExist")]
                public static partial void {|CS8795:Method2|}();

                [System.Runtime.InteropServices.ATTRIBUTELibraryImport("DoesNotExist")]
                public static partial void {|CS8795:Method3|}();
            }
            """;

        public static string LibraryImportInRefStruct = """
            using System;
            using System.Runtime.InteropServices;

            public static partial class MyClass
            {
                public ref partial struct RSPublic
                {
                    [LibraryImport("DoesNotExist")]
                    public static partial int Method();
                }

                internal ref partial struct RSInternal
                {
                    [LibraryImport("DoesNotExist")]
                    public static partial int Method();
                }

                private ref partial struct RSPrivate
                {
                    [LibraryImport("DoesNotExist")]
                    public static partial int Method();
                }
            }

            public ref partial struct RSContainer
            {
                public ref partial struct RSPublic
                {
                    [LibraryImport("DoesNotExist")]
                    public static partial int Method();
                }

                internal ref partial struct RSInternal
                {
                    [LibraryImport("DoesNotExist")]
                    public static partial int Method();
                }

                private ref partial struct RSPrivate
                {
                    [LibraryImport("DoesNotExist")]
                    public static partial int Method();
                }
            }

        """;

        public static readonly string DisableRuntimeMarshalling = "[assembly:System.Runtime.CompilerServices.DisableRuntimeMarshalling]";

        public static readonly string UsingSystemRuntimeInteropServicesMarshalling = "using System.Runtime.InteropServices.Marshalling;";

        /// <summary>
        /// Declaration with parameters with <see cref="StringMarshalling"/> set.
        /// </summary>
        public static string BasicParametersAndModifiersWithStringMarshalling(string typename, StringMarshalling value, string preDeclaration = "") => $$"""
            using System.Runtime.InteropServices;
            {{preDeclaration}}
            partial class Test
            {
                [{|#0:LibraryImport("DoesNotExist", StringMarshalling = StringMarshalling.{{value}})|}]
                public static partial {{typename}} {|#1:Method|}(
                    {{typename}} {|#2:p|},
                    in {{typename}} {|#3:pIn|},
                    ref {{typename}} {|#4:pRef|},
                    out {{typename}} {|#5:pOut|});
            }
            """;

        public static string BasicParametersAndModifiersWithStringMarshalling<T>(StringMarshalling value, string preDeclaration = "") =>
            BasicParametersAndModifiersWithStringMarshalling(typeof(T).ToString(), value, preDeclaration);

        /// <summary>
        /// Declaration with parameters with <see cref="StringMarshallingCustomType"/> set.
        /// </summary>
        public static string BasicParametersAndModifiersWithStringMarshallingCustomType(string typeName, string stringMarshallingCustomTypeName, string preDeclaration = "") => $$"""
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            {{preDeclaration}}
            partial class Test
            {
                [LibraryImport("DoesNotExist", StringMarshallingCustomType = typeof({{stringMarshallingCustomTypeName}}))]
                public static partial {{typeName}} {|#0:Method|}(
                    {{typeName}} {|#1:p|},
                    in {{typeName}} {|#2:pIn|},
                    ref {{typeName}} {|#3:pRef|},
                    out {{typeName}} {|#4:pOut|});
            }
            """;

        public static string BasicParametersAndModifiersWithStringMarshallingCustomType<T>(string stringMarshallingCustomTypeName, string preDeclaration = "") =>
            BasicParametersAndModifiersWithStringMarshallingCustomType(typeof(T).ToString(), stringMarshallingCustomTypeName, preDeclaration);

        public static string CustomStringMarshallingParametersAndModifiers<T>()
        {
            string typeName = typeof(T).ToString();
            return BasicParametersAndModifiersWithStringMarshallingCustomType(typeName, "Marshaller", DisableRuntimeMarshalling) + $$"""
                [CustomMarshaller(typeof({{typeName}}), MarshalMode.Default, typeof(Marshaller))]
                static class Marshaller
                {
                    public static nint ConvertToUnmanaged({{typeName}} s) => default;

                    public static {{typeName}} ConvertToManaged(nint i) => default;
                }
                """;
        }

        /// <summary>
        /// Declaration with parameters.
        /// </summary>
        public static string BasicParametersAndModifiers(string typeName, string preDeclaration = "") => $$"""
            using System.Runtime.InteropServices;
            {{preDeclaration}}
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                public static partial {{typeName}} {|#0:Method|}(
                    {{typeName}} {|#1:p|},
                    in {{typeName}} {|#2:pIn|},
                    ref {{typeName}} {|#3:pRef|},
                    out {{typeName}} {|#4:pOut|});
            }
            """;

        /// <summary>
        /// Declaration with parameters.
        /// </summary>
        public static string BasicParametersAndModifiersNoRef(string typeName, string preDeclaration = "") => $$"""
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            {{preDeclaration}}
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                public static partial {{typeName}} {|#0:Method|}(
                    {{typeName}} {|#1:p|},
                    in {{typeName}} {|#2:pIn|},
                    out {{typeName}} {|#4:pOut|});
            }
            """;

        /// <summary>
        /// Declaration with parameters and unsafe.
        /// </summary>
        public static string BasicParametersAndModifiersUnsafe(string typeName, string preDeclaration = "") => $$"""
            using System.Runtime.InteropServices;
            {{preDeclaration}}
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                public static unsafe partial {{typeName}} {|#0:Method|}(
                    {{typeName}} {|#1:p|},
                    in {{typeName}} {|#2:pIn|},
                    ref {{typeName}} {|#3:pRef|},
                    out {{typeName}} {|#4:pOut|});
            }
            """;

        public static string BasicParametersAndModifiers<T>(string preDeclaration = "") => BasicParametersAndModifiers(typeof(T).ToString(), preDeclaration);

        /// <summary>
        /// Declaration with [In, Out] style attributes on a by-value parameter.
        /// </summary>
        public static string ByValueParameterWithModifier(string typeName, string attributeName, string preDeclaration = "") => $$"""
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            {{preDeclaration}}
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                public static partial void Method(
                    [{{attributeName}}] {{typeName}} {|#0:p|});
            }
            """;

        public static string ByValueParameterWithModifier<T>(string attributeName, string preDeclaration = "") => ByValueParameterWithModifier(typeof(T).ToString(), attributeName, preDeclaration);

        /// <summary>
        /// Declaration with one parameter with custom modifiers.
        /// </summary>
        public static string SingleParameterWithModifier(string typeName, string modifiers, string preDeclaration = "") => $$"""
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            {{preDeclaration}}
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                public static partial void Method(
                    {{modifiers}} {{typeName}} {|#0:p|});
            }
            """;

        /// <summary>
        /// Declaration with by-value parameter with custom name.
        /// </summary>
        public static string ByValueParameterWithName(string methodName, string paramName) => $$"""
            using System.Runtime.InteropServices;
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                public static partial void {{methodName}}(
                    int {{paramName}});
            }
            """;

        /// <summary>
        /// Declaration with parameters with MarshalAs.
        /// </summary>
        public static string MarshalAsParametersAndModifiers(string typeName, UnmanagedType unmanagedType) => $$"""
            using System.Runtime.InteropServices;
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                [return: {|#10:MarshalAs(UnmanagedType.{{unmanagedType}})|}]
                public static partial {{typeName}} {|#0:Method|}(
                    [{|#11:MarshalAs(UnmanagedType.{{unmanagedType}})|}] {{typeName}} {|#1:p|},
                    [{|#12:MarshalAs(UnmanagedType.{{unmanagedType}})|}] in {{typeName}} {|#2:pIn|},
                    [{|#13:MarshalAs(UnmanagedType.{{unmanagedType}})|}] ref {{typeName}} {|#3:pRef|},
                    [{|#14:MarshalAs(UnmanagedType.{{unmanagedType}})|}] out {{typeName}} {|#4:pOut|});
            }
            """;

        /// <summary>
        /// Declaration with parameters with MarshalAs.
        /// </summary>
        public static string MarshalAsParametersAndModifiersUnsafe(string typeName, UnmanagedType unmanagedType) => $$"""
            using System.Runtime.InteropServices;
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                [return: MarshalAs(UnmanagedType.{{unmanagedType}})]
                public static unsafe partial {{typeName}} {|#0:Method|}(
                    [MarshalAs(UnmanagedType.{{unmanagedType}})] {{typeName}} {|#1:p|},
                    [MarshalAs(UnmanagedType.{{unmanagedType}})] in {{typeName}} {|#2:pIn|},
                    [MarshalAs(UnmanagedType.{{unmanagedType}})] ref {{typeName}} {|#3:pRef|},
                    [MarshalAs(UnmanagedType.{{unmanagedType}})] out {{typeName}} {|#4:pOut|});
            }
            """;

        public static string MarshalAsParametersAndModifiers<T>(UnmanagedType unmanagedType) => MarshalAsParametersAndModifiers(typeof(T).ToString(), unmanagedType);

        /// <summary>
        /// Declaration with enum parameters.
        /// </summary>
        public static string EnumParameters => """
            using System.Runtime.InteropServices;
            using NS;

            namespace NS
            {
                enum MyEnum { A, B, C }
            }

            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                public static partial MyEnum Method(
                    MyEnum p,
                    in MyEnum pIn,
                    ref MyEnum pRef,
                    out MyEnum pOut);
            }
            """;

        /// <summary>
        /// Declaration with pointer parameters.
        /// </summary>
        public static string PointerParameters<T>() => BasicParametersAndModifiersUnsafe($"{typeof(T)}*");

        /// <summary>
        /// Declaration with PreserveSig = false.
        /// </summary>
        public static string SetLastErrorTrue(string typeName) => $$"""
            using System.Runtime.InteropServices;
            partial class Test
            {
                [LibraryImport("DoesNotExist", SetLastError = true)]
                public static partial {{typeName}} Method({{typeName}} p);
            }
            """;

        public static string SetLastErrorTrue<T>() => SetLastErrorTrue(typeof(T).ToString());

        public static string DelegateParametersAndModifiers = BasicParametersAndModifiers("MyDelegate") + """
            delegate int MyDelegate(int a);
            """;
        public static string DelegateMarshalAsParametersAndModifiers = MarshalAsParametersAndModifiers("MyDelegate", UnmanagedType.FunctionPtr) + """
            delegate int MyDelegate(int a);
            """;

        private static string BlittableMyStruct(string modifier = "") => $$$"""
            #pragma warning disable CS0169
            {{{modifier}}} unsafe struct MyStruct
            {
                private int i;
                private short s;
                private long l;
                private double d;
                private int* iptr;
                private short* sptr;
                private long* lptr;
                private double* dptr;
                private void* vptr;
            }
            """;

        public static string BlittableStructParametersAndModifiers(string attr) => $$"""
            {{BasicParametersAndModifiers("MyStruct", attr)}}
            {{BlittableMyStruct()}}
            """;

        public static string MarshalAsArrayParametersAndModifiers(string elementType, string preDeclaration = "") => $$"""
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            {{preDeclaration}}
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                [return:MarshalAs(UnmanagedType.LPArray, SizeConst=10)]
                public static partial {{elementType}}[] {|#0:Method|}(
                    {{elementType}}[] {|#1:p|},
                    in {{elementType}}[] {|#2:pIn|},
                    int {|#3:pRefSize|},
                    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=2)] ref {{elementType}}[] {|#4:pRef|},
                    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=5, SizeConst=4)] out {{elementType}}[] {|#5:pOut|},
                    out int {|#6:pOutSize|}
                    );
            }
            """;

        public static string MarshalAsArrayParametersAndModifiers<T>(string preDeclaration = "") => MarshalAsArrayParametersAndModifiers(typeof(T).ToString(), preDeclaration);

        public static string MarshalAsArrayParameterWithSizeParam(string sizeParamType, bool isByRef) => $$"""
            using System.Runtime.InteropServices;
            {{DisableRuntimeMarshalling}}
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                public static partial void Method(
                    {{(isByRef ? "ref" : "")}} {{sizeParamType}} {|#0:pRefSize|},
                    [MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)] ref int[] {|#1:pRef|}
                    );
            }
            """;

        public static string MarshalAsArrayParameterWithSizeParam<T>(bool isByRef) => MarshalAsArrayParameterWithSizeParam(typeof(T).ToString(), isByRef);


        public static string MarshalAsArrayParameterWithNestedMarshalInfo(string elementType, UnmanagedType nestedMarshalInfo, string preDeclaration = "") => $$"""
            using System.Runtime.InteropServices;
            {{preDeclaration}}
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                public static partial void Method(
                    [MarshalAs(UnmanagedType.LPArray, ArraySubType=UnmanagedType.{{nestedMarshalInfo}})] {{elementType}}[] pRef
                    );
            }
            """;

        public static string MarshalAsArrayParameterWithNestedMarshalInfo<T>(UnmanagedType nestedMarshalType, string preDeclaration = "") => MarshalAsArrayParameterWithNestedMarshalInfo(typeof(T).ToString(), nestedMarshalType, preDeclaration);

        /// <summary>
        /// Declaration with parameters with MarshalAs.
        /// </summary>
        //public static string MarshalUsingParametersAndModifiers(string typeName, string nativeTypeName, string preDeclaration = "") => $$"""
        //    using System.Runtime.InteropServices;
        //    using System.Runtime.InteropServices.Marshalling;
        //    {{preDeclaration}}
        //    partial class Test
        //    {
        //        [LibraryImport("DoesNotExist")]
        //        [return: MarshalUsing(typeof({{nativeTypeName}}))]
        //        public static partial {{typeName}} Method(
        //            [MarshalUsing(typeof({{nativeTypeName}}))] {{typeName}} p,
        //            [MarshalUsing(typeof({{nativeTypeName}}))] in {{typeName}} pIn,
        //            [MarshalUsing(typeof({{nativeTypeName}}))] ref {{typeName}} pRef,
        //            [MarshalUsing(typeof({{nativeTypeName}}))] out {{typeName}} pOut);
        //    }
        //    """;
        public static string MarshalUsingParametersAndModifiers(string typeName, string nativeTypeName, string preDeclaration = "") => $$"""
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            {{preDeclaration}}
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                [return: MarshalUsing(typeof({{nativeTypeName}}))]
                public static partial {{typeName}} {|#0:Method|}(
                    [MarshalUsing(typeof({{nativeTypeName}}))] {{typeName}} {|#1:p|},
                    [MarshalUsing(typeof({{nativeTypeName}}))] in {{typeName}} {|#2:pIn|},
                    [MarshalUsing(typeof({{nativeTypeName}}))] ref {{typeName}} {|#3:pRef|},
                    [MarshalUsing(typeof({{nativeTypeName}}))] out {{typeName}} {|#4:pOut|});
            }
            """;
        public static string BasicParameterWithByRefModifier(string byRefKind, string typeName, string preDeclaration = "") => $$"""
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            {{preDeclaration}}
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                public static partial void Method(
                    {{byRefKind}} {{typeName}} {|#0:p|});
            }
            """;

        public static string BasicParameterByValue(string typeName, string preDeclaration = "") => $$"""
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            {{preDeclaration}}
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                public static partial void Method(
                    {{typeName}} {|#0:p|});
            }
            """;

        public static string BasicReturnType(string typeName, string preDeclaration = "") => $$"""
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            {{preDeclaration}}
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                public static partial {{typeName}} {|#0:Method|}();
            }
            """;

        public static string BasicReturnAndParameterByValue(string returnType, string parameterType, string preDeclaration = "") => $$"""
            using System.Runtime.InteropServices;
            {{preDeclaration}}
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                public static partial {{returnType}} Method({{parameterType}} p);
            }
            """;

        public static string SafeHandleWithCustomDefaultConstructorAccessibility(bool privateCtor) => BasicParametersAndModifiers("MySafeHandle") + $$"""
            class MySafeHandle : SafeHandle
            {
                {{(privateCtor ? "private" : "public")}} MySafeHandle() : base(System.IntPtr.Zero, true) { }

                public override bool IsInvalid => handle == System.IntPtr.Zero;

                protected override bool ReleaseHandle() => true;
            }
            """;

        public static string GeneratedComInterface => BasicParametersAndModifiers("MyInterfaceType", "using System.Runtime.InteropServices.Marshalling;") + """
            [GeneratedComInterface]
            interface MyInterfaceType
            {
                void Method();
            }
            """;

        public static string PreprocessorIfAroundFullFunctionDefinition(string define) =>
            $$"""
            partial class Test
            {
            #if {{define}}
                [System.Runtime.InteropServices.LibraryImport("DoesNotExist")]
                public static partial int Method(
                    int p,
                    in int pIn,
                    out int pOut);
            #endif
            }
            """;

        public static string PreprocessorIfAroundFullFunctionDefinitionWithFollowingFunction(string define) =>
            $$"""
            using System.Runtime.InteropServices;
            partial class Test
            {
            #if {{define}}
                [LibraryImport("DoesNotExist")]
                public static partial int Method(
                    int p,
                    in int pIn,
                    out int pOut);
            #endif
                public static int Method2(
                    SafeHandle p) => throw null;
            }
            """;

        public static string PreprocessorIfAfterAttributeAroundFunction(string define) =>
            $$"""
            using System.Runtime.InteropServices;
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
            #if {{define}}
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
            }
            """;

        public static string PreprocessorIfAfterAttributeAroundFunctionAdditionalFunctionAfter(string define) =>
            $$"""
            using System.Runtime.InteropServices;
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
            #if {{define}}
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
            }
            """;

        public static string MaybeBlittableGenericTypeParametersAndModifiers(string typeArgument) => BasicParametersAndModifiers($"Generic<{typeArgument}>", DisableRuntimeMarshalling) + """
            struct Generic<T>
            {
            #pragma warning disable CS0649
                public T field;
            }
            """;

        public static string MaybeBlittableGenericTypeParametersAndModifiers<T>() =>
            MaybeBlittableGenericTypeParametersAndModifiers(typeof(T).ToString());

        public static string RecursiveImplicitlyBlittableStruct => BasicParametersAndModifiers("RecursiveStruct", DisableRuntimeMarshalling) + """
            struct RecursiveStruct
            {
                RecursiveStruct {|CS0523:s|};
                int i;
            }
            """;
        public static string MutuallyRecursiveImplicitlyBlittableStruct => BasicParametersAndModifiers("RecursiveStruct1", DisableRuntimeMarshalling) + """
            struct RecursiveStruct1
            {
                RecursiveStruct2 {|CS0523:s|};
                int i;
            }

            struct RecursiveStruct2
            {
                RecursiveStruct1 {|CS0523:s|};
                int i;
            }
            """;

        public static string MarshalUsingCollectionCountInfoParametersAndModifiers(string collectionType) => $$"""
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            {{DisableRuntimeMarshalling}}
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                [return:MarshalUsing(ConstantElementCount=10)]
                public static partial {{collectionType}} Method(
                    {{collectionType}} p,
                    in {{collectionType}} pIn,
                    int pRefSize,
                    [MarshalUsing(CountElementName = "pRefSize")] ref {{collectionType}} pRef,
                    [MarshalUsing(CountElementName = "pOutSize")] out {{collectionType}} pOut,
                    out int pOutSize
                    );
            }
            """;

        public static string MarshalUsingCollectionCountInfoParametersAndModifiers<T>() => MarshalUsingCollectionCountInfoParametersAndModifiers(typeof(T).ToString());

        public static string MarshalUsingCollectionParametersAndModifiers(string collectionType, string marshallerType) => $$"""
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            {{DisableRuntimeMarshalling}}
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                [return:MarshalUsing(typeof({{marshallerType}}), ConstantElementCount=10)]
                public static partial {{collectionType}} Method(
                    [MarshalUsing(typeof({{marshallerType}}))] {{collectionType}} p,
                    [MarshalUsing(typeof({{marshallerType}}))] in {{collectionType}} pIn,
                    int pRefSize,
                    [MarshalUsing(typeof({{marshallerType}}), CountElementName = "pRefSize")] ref {{collectionType}} pRef,
                    [MarshalUsing(typeof({{marshallerType}}), CountElementName = "pOutSize")] out {{collectionType}} pOut,
                    out int pOutSize
                    );
            }
            """;

        public static string MarshalUsingCollectionReturnValueLength(string collectionType, string marshallerType) => $$"""
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            {{DisableRuntimeMarshalling}}
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                public static partial int Method(
                    [MarshalUsing(typeof({{marshallerType}}), CountElementName = MarshalUsingAttribute.ReturnsCountValue)] out {{collectionType}} pOut
                    );
            }
            """;

        public static string MarshalUsingCollectionOutConstantLength(string collectionType, string predeclaration = "") => $$"""
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            {{predeclaration}}
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                public static partial int Method(
                    [MarshalUsing(ConstantElementCount = 10)] out {{collectionType}} pOut);
            }
            """;

        public static string MarshalUsingCollectionReturnConstantLength(string collectionType, string predeclaration = "") => $$"""
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            {{predeclaration}}
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                [return:MarshalUsing(ConstantElementCount = 10)]
                public static partial {{collectionType}} Method();
            }
            """;
        public static string CustomElementMarshalling(string collectionType, string elementMarshaller, string predeclaration = "") => $$"""
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            {{predeclaration}}
            {{DisableRuntimeMarshalling}}
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                [return:MarshalUsing(ConstantElementCount=10)]
                [return:MarshalUsing(typeof({{elementMarshaller}}), ElementIndirectionDepth = 1)]
                public static partial {{collectionType}} Method(
                    [MarshalUsing(typeof({{elementMarshaller}}), ElementIndirectionDepth = 1)] {{collectionType}} p,
                    [MarshalUsing(typeof({{elementMarshaller}}), ElementIndirectionDepth = 1)] in {{collectionType}} pIn,
                    int pRefSize,
                    [MarshalUsing(CountElementName = "pRefSize"), MarshalUsing(typeof({{elementMarshaller}}), ElementIndirectionDepth = 1)] ref {{collectionType}} pRef,
                    [MarshalUsing(CountElementName = "pOutSize")][MarshalUsing(typeof({{elementMarshaller}}), ElementIndirectionDepth = 1)] out {{collectionType}} pOut,
                    out int pOutSize
                    );
            }
            """;

        public const string IntClassAndMarshaller = IntClassDefinition + IntClassMarshallerDefinition;
        public const string IntClassDefinition = """
            internal struct IntClass
            {
                public int Field;
            }
            """;
        public const string IntClassMarshallerDefinition = """
            [CustomMarshaller(typeof(IntClass), MarshalMode.Default, typeof(IntClassMarshaller))]
            internal static class IntClassMarshaller
            {
                public static nint ConvertToUnmanaged(IntClass managed) => (nint)0;
                public static IntClass ConvertToManaged(nint unmanaged) => default;
            }
            """;

        public const string IntStructAndMarshaller = IntStructDefinition + IntStructMarshallerDefinition;
        public const string IntStructDefinition = """
            internal struct IntStruct
            {
                public int Field;
            }
            """;
        public const string IntStructMarshallerDefinition = """
            [CustomMarshaller(typeof(IntStruct), MarshalMode.Default, typeof(IntStructMarshaller))]
            internal static class IntStructMarshaller
            {
                public static nint ConvertToUnmanaged(IntStruct managed) => (nint)0;
                public static IntStruct ConvertToManaged(nint unmanaged) => default;
            }
            """;

        public string CollectionMarshallingWithCountRefKinds(
            (string parameterType, string parameterModifiers, string[] countNames) returnType,
            params (string parameterType, string parameterModifiers, string parameterName, string[] countNames)[] parameters)
        {
            List<string> parameterSources = new();
            int i = 1;
            foreach (var (parameterType, parameterModifiers, parameterName, countNames) in parameters)
            {
                List<string> marshalUsings = new();
                int j = 0;
                foreach (var countName in countNames)
                {
                    marshalUsings.Add($"[MarshalUsing(CountElementName = {countName}, ElementIndirectionDepth = {j})]");
                    j++;
                }
                parameterSources.Add($$"""
                    {{string.Join(' ', marshalUsings)}} {{parameterModifiers}} {{parameterType}} {|#{{i}}:{{parameterName}}|}
                    """);
                i++;
            }
            string returnTypeSource;
            string returnAttributes;
            {
                List<string> marshalUsings = new();
                var (parameterType, parameterModifiers, countNames) = returnType;
                foreach (var countName in countNames)
                {
                    marshalUsings.Add($"[return: MarshalUsing(CountElementName = nameof({countName}))]");
                }
                returnAttributes = string.Join(' ', marshalUsings);
                returnTypeSource = $"{parameterModifiers} {parameterType}";
            }
            var parametersSource = string.Join(',', parameterSources);
            return $$"""
                using System.Runtime.CompilerServices;
                using System.Runtime.InteropServices;
                using System.Runtime.InteropServices.Marshalling;
                {{DisableRuntimeMarshalling}}

                partial class Test
                {
                    [LibraryImport("DoesNotExist")]
                    {{returnAttributes}}
                    public static partial {{returnTypeSource}} {|#0:Method|}({{parametersSource}});
                }
                """;
        }

        public static string MarshalUsingArrayParameterWithSizeParam(string sizeParamType, bool isByRef) => $$"""
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            {{DisableRuntimeMarshalling}}
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                public static partial void Method(
                    {{(isByRef ? "ref" : "")}} {{sizeParamType}} pRefSize,
                    [MarshalUsing(CountElementName = "pRefSize")] ref int[] pRef
                    );
            }
            """;

        public static string MarshalUsingArrayParameterWithSizeParam<T>(bool isByRef) => MarshalUsingArrayParameterWithSizeParam(typeof(T).ToString(), isByRef);

        public static string MarshalUsingCollectionWithConstantAndElementCount => $$"""
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            {{DisableRuntimeMarshalling}}
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                public static partial void Method(
                    int pRefSize,
                    [{|#0:MarshalUsing(ConstantElementCount = 10, CountElementName = "pRefSize")|}] ref int[] {|#1:pRef|}
                    );
            }
            """;

        public static string MarshalUsingCollectionWithNullElementName => $$"""
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            {{DisableRuntimeMarshalling}}
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                public static partial void Method(
                    int pRefSize,
                    [{|#0:MarshalUsing(CountElementName = null)|}] ref int[] {|#1:pRef|}
                    );
            }
            """;

        public static string MarshalAsAndMarshalUsingOnReturnValue => $$"""
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            {{DisableRuntimeMarshalling}}
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                [return:MarshalUsing(ConstantElementCount=10)]
                [return:{|#0:MarshalAs(UnmanagedType.LPArray, SizeConst=10)|}]
                public static partial int[] Method();
            }
            """;
        public static string CustomElementMarshallingDuplicateElementIndirectionDepth => $$"""
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            {{DisableRuntimeMarshalling}}
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                public static partial void Method(
                    [MarshalUsing(typeof(CustomIntMarshaller), ElementIndirectionDepth = 1)] [{|#0:MarshalUsing(typeof(CustomIntMarshaller), ElementIndirectionDepth = 1)|}] TestCollection<int> p);
            }
            """
                    + CustomCollectionMarshallingCodeSnippets.TestCollection()
                    + CustomCollectionMarshallingCodeSnippets.StatelessSnippets.In
                    + CustomCollectionMarshallingCodeSnippets.CustomIntMarshaller;

        public static string CustomElementMarshallingUnusedElementIndirectionDepth => $$"""
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            {{DisableRuntimeMarshalling}}
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                public static partial void Method(
                    [{|#0:MarshalUsing(typeof(CustomIntMarshaller), ElementIndirectionDepth = 2)|}] TestCollection<int> p);
            }
            """
            + CustomCollectionMarshallingCodeSnippets.TestCollection()
            + CustomCollectionMarshallingCodeSnippets.StatelessSnippets.In
            + CustomCollectionMarshallingCodeSnippets.CustomIntMarshaller;

        public static string RecursiveCountElementNameOnReturnValue => $$"""
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            {{DisableRuntimeMarshalling}}
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                [return:{|#0:MarshalUsing(CountElementName=MarshalUsingAttribute.ReturnsCountValue)|}]
                public static partial int[] {|#1:Method|}();
            }
            """;

        public static string RecursiveCountElementNameOnParameter => $$"""
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            {{DisableRuntimeMarshalling}}
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                public static partial void Method(
                    [{|#0:MarshalUsing(CountElementName="arr")|}] ref int[] {|#1:arr|}
                );
            }
            """;
        public static string MutuallyRecursiveCountElementNameOnParameter => $$"""
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            {{DisableRuntimeMarshalling}}
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                public static partial void Method(
                    [{|#0:MarshalUsing(CountElementName="arr2")|}] ref int[] {|#1:arr|},
                    [{|#2:MarshalUsing(CountElementName="arr")|}] ref int[] {|#3:arr2|}
                );
            }
            """;
        public static string MutuallyRecursiveSizeParamIndexOnParameter => $$"""
            using System.Runtime.InteropServices;
            {{DisableRuntimeMarshalling}}
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                public static partial void Method(
                    [{|#0:MarshalAs(UnmanagedType.LPArray, SizeParamIndex=1)|}] ref int[] {|#1:arr|},
                    [{|#2:MarshalAs(UnmanagedType.LPArray, SizeParamIndex=0)|}] ref int[] {|#3:arr2|}
                );
            }
            """;

        public static string CollectionsOfCollectionsStress => $$"""
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            {{DisableRuntimeMarshalling}}
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                public static partial void Method(
                    [MarshalUsing(CountElementName="arr0", ElementIndirectionDepth = 0)]
                    [MarshalUsing(CountElementName="arr1", ElementIndirectionDepth = 1)]
                    [MarshalUsing(CountElementName="arr2", ElementIndirectionDepth = 2)]
                    [MarshalUsing(CountElementName="arr3", ElementIndirectionDepth = 3)]
                    [MarshalUsing(CountElementName="arr4", ElementIndirectionDepth = 4)]
                    [MarshalUsing(CountElementName="arr5", ElementIndirectionDepth = 5)]
                    [MarshalUsing(CountElementName="arr6", ElementIndirectionDepth = 6)]
                    [MarshalUsing(CountElementName="arr7", ElementIndirectionDepth = 7)]
                    [MarshalUsing(CountElementName="arr8", ElementIndirectionDepth = 8)]
                    [MarshalUsing(CountElementName="arr9", ElementIndirectionDepth = 9)]
                    [MarshalUsing(CountElementName="arr10", ElementIndirectionDepth = 10)]ref int[][][][][][][][][][][] arr11,
                    [MarshalUsing(CountElementName="arr0", ElementIndirectionDepth = 0)]
                    [MarshalUsing(CountElementName="arr1", ElementIndirectionDepth = 1)]
                    [MarshalUsing(CountElementName="arr2", ElementIndirectionDepth = 2)]
                    [MarshalUsing(CountElementName="arr3", ElementIndirectionDepth = 3)]
                    [MarshalUsing(CountElementName="arr4", ElementIndirectionDepth = 4)]
                    [MarshalUsing(CountElementName="arr5", ElementIndirectionDepth = 5)]
                    [MarshalUsing(CountElementName="arr6", ElementIndirectionDepth = 6)]
                    [MarshalUsing(CountElementName="arr7", ElementIndirectionDepth = 7)]
                    [MarshalUsing(CountElementName="arr8", ElementIndirectionDepth = 8)]
                    [MarshalUsing(CountElementName="arr9", ElementIndirectionDepth = 9)]ref int[][][][][][][][][][] arr10,
                    [MarshalUsing(CountElementName="arr0", ElementIndirectionDepth = 0)]
                    [MarshalUsing(CountElementName="arr1", ElementIndirectionDepth = 1)]
                    [MarshalUsing(CountElementName="arr2", ElementIndirectionDepth = 2)]
                    [MarshalUsing(CountElementName="arr3", ElementIndirectionDepth = 3)]
                    [MarshalUsing(CountElementName="arr4", ElementIndirectionDepth = 4)]
                    [MarshalUsing(CountElementName="arr5", ElementIndirectionDepth = 5)]
                    [MarshalUsing(CountElementName="arr6", ElementIndirectionDepth = 6)]
                    [MarshalUsing(CountElementName="arr7", ElementIndirectionDepth = 7)]
                    [MarshalUsing(CountElementName="arr8", ElementIndirectionDepth = 8)]ref int[][][][][][][][][] arr9,
                    [MarshalUsing(CountElementName="arr0", ElementIndirectionDepth = 0)]
                    [MarshalUsing(CountElementName="arr1", ElementIndirectionDepth = 1)]
                    [MarshalUsing(CountElementName="arr2", ElementIndirectionDepth = 2)]
                    [MarshalUsing(CountElementName="arr3", ElementIndirectionDepth = 3)]
                    [MarshalUsing(CountElementName="arr4", ElementIndirectionDepth = 4)]
                    [MarshalUsing(CountElementName="arr5", ElementIndirectionDepth = 5)]
                    [MarshalUsing(CountElementName="arr6", ElementIndirectionDepth = 6)]
                    [MarshalUsing(CountElementName="arr7", ElementIndirectionDepth = 7)]ref int[][][][][][][][] arr8,
                    [MarshalUsing(CountElementName="arr0", ElementIndirectionDepth = 0)]
                    [MarshalUsing(CountElementName="arr1", ElementIndirectionDepth = 1)]
                    [MarshalUsing(CountElementName="arr2", ElementIndirectionDepth = 2)]
                    [MarshalUsing(CountElementName="arr3", ElementIndirectionDepth = 3)]
                    [MarshalUsing(CountElementName="arr4", ElementIndirectionDepth = 4)]
                    [MarshalUsing(CountElementName="arr5", ElementIndirectionDepth = 5)]
                    [MarshalUsing(CountElementName="arr6", ElementIndirectionDepth = 6)]ref int[][][][][][][] arr7,
                    [MarshalUsing(CountElementName="arr0", ElementIndirectionDepth = 0)]
                    [MarshalUsing(CountElementName="arr1", ElementIndirectionDepth = 1)]
                    [MarshalUsing(CountElementName="arr2", ElementIndirectionDepth = 2)]
                    [MarshalUsing(CountElementName="arr3", ElementIndirectionDepth = 3)]
                    [MarshalUsing(CountElementName="arr4", ElementIndirectionDepth = 4)]
                    [MarshalUsing(CountElementName="arr5", ElementIndirectionDepth = 5)]ref int[][][][][][] arr6,
                    [MarshalUsing(CountElementName="arr0", ElementIndirectionDepth = 0)]
                    [MarshalUsing(CountElementName="arr1", ElementIndirectionDepth = 1)]
                    [MarshalUsing(CountElementName="arr2", ElementIndirectionDepth = 2)]
                    [MarshalUsing(CountElementName="arr3", ElementIndirectionDepth = 3)]
                    [MarshalUsing(CountElementName="arr4", ElementIndirectionDepth = 4)]ref int[][][][][] arr5,
                    [MarshalUsing(CountElementName="arr0", ElementIndirectionDepth = 0)]
                    [MarshalUsing(CountElementName="arr1", ElementIndirectionDepth = 1)]
                    [MarshalUsing(CountElementName="arr2", ElementIndirectionDepth = 2)]
                    [MarshalUsing(CountElementName="arr3", ElementIndirectionDepth = 3)]ref int[][][][] arr4,
                    [MarshalUsing(CountElementName="arr0", ElementIndirectionDepth = 0)]
                    [MarshalUsing(CountElementName="arr1", ElementIndirectionDepth = 1)]
                    [MarshalUsing(CountElementName="arr2", ElementIndirectionDepth = 2)]ref int[][][] arr3,
                    [MarshalUsing(CountElementName="arr0", ElementIndirectionDepth = 0)]
                    [MarshalUsing(CountElementName="arr1", ElementIndirectionDepth = 1)]ref int[][] arr2,
                    [MarshalUsing(CountElementName="arr0", ElementIndirectionDepth = 0)]ref int[] arr1,
                    ref int arr0
                );
            }
            """;

        public static string GenericsStress => $$"""
            using System.Runtime.InteropServices;
            using System.Runtime.InteropServices.Marshalling;
            {{DisableRuntimeMarshalling}}
            partial class Test
            {
                [LibraryImport("DoesNotExist")]
                public static partial void Method(S<int>.N<bool> v);
            }

            class S<T>
            {
                [NativeMarshalling(typeof(Container<,>.NestedMarshallerType))]
                public struct N<U>
                {
                }
            }

            class Container<T, U>
            {
                [CustomMarshaller(typeof(S<>.N<>), MarshalMode.ManagedToUnmanagedIn, typeof(Container<,>.NestedMarshallerType))]
                public static class NestedMarshallerType
                {
                    public static int ConvertToUnmanaged(S<T>.N<U> managed) => 0;
                }
            }
            """;

        public static string RefReturn(string typeName) => $$"""
            using System.Runtime.InteropServices;
            partial struct Basic
            {
                [LibraryImport("DoesNotExist")]
                public static partial ref {{typeName}} {|#0:RefReturn|}();
                [LibraryImport("DoesNotExist")]
                public static partial ref readonly {{typeName}} {|#1:RefReadonlyReturn|}();
            }
            """;

        public static string PartialPropertyName => """
            using System.Runtime.InteropServices;

            partial struct Basic
            {
                [{|CS1729:LibraryImport("DoesNotExist", {|CS0103:SetLa|})|}]
                public static partial void Method();
            }
            """;
        public static string InvalidConstantForModuleName => """
            using System.Runtime.InteropServices;

            partial struct Basic
            {
                [LibraryImport({|CS0103:DoesNotExist|})]
                public static partial void Method();
            }
            """;
        public static string IncorrectAttributeFieldType => """
            using System.Runtime.InteropServices;

            partial struct Basic
            {
                [LibraryImport("DoesNotExist", SetLastError = {|CS0029:"Foo"|})]
                public static partial void Method();
            }
            """;

        public static class ValidateDisableRuntimeMarshalling
        {
            public static string NonBlittableUserDefinedTypeWithNativeType = """
                public struct S
                {
                    public string s;
                }

                [CustomMarshaller(typeof(S), MarshalMode.Default, typeof(Marshaller))]
                public static class Marshaller
                {
                    public static Native ConvertToUnmanaged(S s) => new(s);

                    public static S ConvertToManaged(Native unmanaged) => unmanaged.ToManaged();

                    public struct Native
                    {
                        private System.IntPtr p;
                        public Native(S s)
                        {
                            p = System.IntPtr.Zero;
                        }

                        public S ToManaged() => new S { s = string.Empty };
                    }
                }
                """;

            public static string TypeUsage(string attr) => MarshalUsingParametersAndModifiers("S", "Marshaller", attr);
        }
    }
}
