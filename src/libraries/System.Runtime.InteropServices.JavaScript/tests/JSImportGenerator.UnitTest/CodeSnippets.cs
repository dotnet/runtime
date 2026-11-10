// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace JSImportGenerator.Unit.Tests
{
    internal static class CodeSnippets
    {
        public static readonly string AllDefault = """
            //AllDefault
            using System;
            using System.Runtime.InteropServices.JavaScript;
            using System.Threading.Tasks;
            partial class Basic
            {
                [JSImport("DoesNotExist")]
                internal static partial void Relaxed(
                    string a1,
                    Exception ex,
                    bool ab, double a6, byte a2, char a3, short a4, float a5, IntPtr a7,
                    bool? nab, double? na6, byte? na2, char? na3, short? na4, float? na5, IntPtr? na7,
                    Task<string> ta1,
                    Task<Exception> tex,
                    Task<bool> tab,
                    Task<double> ta6,
                    Task<byte> ta2,
                    Task<char> ta3,
                    Task<short> ta4,
                    Task<float> ta5,
                    Task<IntPtr> ta7,
                    JSObject jso,
                    string[] aa1, byte[] aab, double[] aad, float[] aaf, int[] aai
                );
            }
            """;

        public static readonly string AllAnnotated = """
            //AllAnnotated
            using System;
            using System.Runtime.InteropServices.JavaScript;
            using System.Threading.Tasks;
            partial class Basic
            {
                [JSImport("DoesNotExist")]
                internal static partial void Annotated(
                    [JSMarshalAs<JSType.Any>] object a1,
                    [JSMarshalAs<JSType.Number>] long a2,
                    [JSMarshalAs<JSType.BigInt>] long a3,
                    [JSMarshalAs<JSType.Function>] Action a4,
                    [JSMarshalAs<JSType.Function<JSType.Number>>] Func<int> a5,
                    [JSMarshalAs<JSType.MemoryView>] Span<byte> a6,
                    [JSMarshalAs<JSType.MemoryView>] ArraySegment<byte> a7,
                    [JSMarshalAs<JSType.Promise<JSType.Any>>] Task<object> a8,
                    [JSMarshalAs<JSType.Array<JSType.Any>>] object[] a9,
                    [JSMarshalAs<JSType.Date>] DateTime a10,
                    [JSMarshalAs<JSType.Date>] DateTimeOffset a11,
                    [JSMarshalAs<JSType.Promise<JSType.Date>>] Task<DateTime> a12,
                    [JSMarshalAs<JSType.Promise<JSType.Date>>] Task<DateTimeOffset> a13,
                    [JSMarshalAs<JSType.Promise<JSType.Number>>] Task<long> a14,
                    [JSMarshalAs<JSType.Promise<JSType.BigInt>>] Task<long> a15,
                    [JSMarshalAs<JSType.MemoryView>] ArraySegment<float> a16
                );
            }
            """;

        public static readonly string AllAnnotatedExport = """
            //AllAnnotated
            using System;
            using System.Runtime.InteropServices.JavaScript;
            using System.Threading.Tasks;
            partial class Basic
            {
                [JSExport]
                internal static void AnnotatedExport(
                    [JSMarshalAs<JSType.Any>] object a1,
                    [JSMarshalAs<JSType.Number>] long a2,
                    [JSMarshalAs<JSType.BigInt>] long a3,
                    [JSMarshalAs<JSType.Function>] Action a4,
                    [JSMarshalAs<JSType.Function<JSType.Number>>] Func<int> a5,
                    [JSMarshalAs<JSType.MemoryView>] Span<byte> a6,
                    [JSMarshalAs<JSType.MemoryView>] ArraySegment<byte> a7,
                    [JSMarshalAs<JSType.Promise<JSType.Any>>] Task<object> a8,
                    [JSMarshalAs<JSType.Array<JSType.Any>>] object[] a9,
                    [JSMarshalAs<JSType.Date>] DateTime a10,
                    [JSMarshalAs<JSType.Date>] DateTimeOffset a11,
                    [JSMarshalAs<JSType.Promise<JSType.Date>>] Task<DateTime> a12,
                    [JSMarshalAs<JSType.Promise<JSType.Date>>] Task<DateTimeOffset> a13,
                    [JSMarshalAs<JSType.Promise<JSType.Number>>] Task<long> a14,
                    [JSMarshalAs<JSType.Promise<JSType.BigInt>>] Task<long> a15,
                    [JSMarshalAs<JSType.MemoryView>] ArraySegment<float> a16
                )
                {}
            }
            """;

        public static readonly string AllMissing = """
            //AllMissing
            using System;
            using System.Runtime.InteropServices.JavaScript;
            using System.Threading.Tasks;
            partial class Basic
            {
                [JSImport("DoesNotExist")]
                internal static partial void Missing(
                    object a1,
                    long a2,
                    long a3,
                    Action a4,
                    Func<int> a5,
                    Span<byte> a6,
                    ArraySegment<byte> a7,
                    Task<object> a8,
                    object[] a9,
                    DateTime a10,
                    DateTimeOffset a11,
                    Task<DateTime> a12,
                    Task<DateTimeOffset> a13,
                    Task<long> a14,
                    Task<long> a15,
                    ArraySegment<float> a16
                );
            }
            """;

        public static readonly string InOutRef = """
            //InOutRef
            using System;
            using System.Runtime.InteropServices.JavaScript;
            using System.Threading.Tasks;
            partial class Basic
            {
                [JSImport("DoesNotExist")]
                internal static partial void InOutRef(
                    out int a1,
                    in int a2,
                    ref int a3
                );
            }
            """;

        public static readonly string AllUnsupported = """
            //AllUnsupported
            using System;
            using System.Runtime.InteropServices.JavaScript;
            using System.Threading.Tasks;
            partial class Basic
            {
                [JSImport("DoesNotExist")]
                internal static partial void Missing(
                    Func<Action> a1,
                    Func<int,int,int,int,int> a2,
                    Span<char> a3,
                    ArraySegment<char> a4,
                    Task<object[]> a5,
                    ulong a6,
                    sbyte a7,
                    ushort a8,
                    uint a9
                );
            }
            """;


        public static readonly string TrivialClassDeclarations = """
            //TrivialClassDeclarations
            using System.Runtime.InteropServices.JavaScript;
            partial class Basic
            {
                [JSImportAttribute("DoesNotExist")]
                public static partial void Import1();

                [JSImport("DoesNotExist")]
                public static partial void Import2();

                [System.Runtime.InteropServices.JavaScript.JSImportAttribute("DoesNotExist")]
                public static partial void Import3();

                [System.Runtime.InteropServices.JavaScript.JSImport("DoesNotExist")]
                public static partial void Import4();

                [JSExportAttribute()]
                public static void Export1(){}

                [JSExport()]
                public static void Export2(){}

                [System.Runtime.InteropServices.JavaScript.JSExportAttribute]
                public static void Export3(){}

                [System.Runtime.InteropServices.JavaScript.JSExport]
                public static void Export4(){}

            }
            """;

        public static readonly string TaskAndDelegateSignatures = """
            using System;
            using System.Runtime.InteropServices.JavaScript;
            using System.Threading.Tasks;

            public partial class Callbacks
            {
                [JSImport("task")]
                public static partial Task ImportTask(Task value);

                [JSExport]
                public static Task ExportTask(Task value) => value;

                [JSImport("taskResult")]
                public static partial Task<IntPtr> ImportTaskResult(Task<IntPtr> value);

                [JSExport]
                public static Task<IntPtr> ExportTaskResult(Task<IntPtr> value) => value;

                [JSImport("action")]
                [return: JSMarshalAs<JSType.Function<JSType.Number, JSType.String, JSType.BigInt>>]
                public static partial Action<int, string, long> ImportAction(
                    [JSMarshalAs<JSType.Function<JSType.Number, JSType.String, JSType.BigInt>>] Action<int, string, long> value);

                [JSExport]
                [return: JSMarshalAs<JSType.Function<JSType.Number, JSType.String, JSType.BigInt>>]
                public static Action<int, string, long> ExportAction(
                    [JSMarshalAs<JSType.Function<JSType.Number, JSType.String, JSType.BigInt>>] Action<int, string, long> value) => value;

                [JSImport("function")]
                [return: JSMarshalAs<JSType.Function<JSType.Number, JSType.String, JSType.BigInt, JSType.Number>>]
                public static partial Func<int, string, long, int> ImportFunction(
                    [JSMarshalAs<JSType.Function<JSType.Number, JSType.String, JSType.BigInt, JSType.Number>>] Func<int, string, long, int> value);

                [JSExport]
                [return: JSMarshalAs<JSType.Function<JSType.Number, JSType.String, JSType.BigInt, JSType.Number>>]
                public static Func<int, string, long, int> ExportFunction(
                    [JSMarshalAs<JSType.Function<JSType.Number, JSType.String, JSType.BigInt, JSType.Number>>] Func<int, string, long, int> value) => value;
            }
            """;

        public static readonly string NestedDeclarations = """
            using System.Runtime.InteropServices.JavaScript;

            namespace Outer.Inner;

            public partial record class Container
            {
                public readonly partial record struct Nested
                {
                    [JSImport("import")]
                    public static partial int Import(int value);

                    [JSExport]
                    public static int Export(int value) => value;
                }
            }
            """;

        public static readonly string EscapedIdentifiersAndLiterals = """
            using System.Runtime.InteropServices.JavaScript;

            namespace @namespace.@event;

            public partial class @class
            {
                [JSImport("function\"\\\r\n\0\u2028", "module\"\\\t")]
                public static partial int @event(int @return);

                [JSImport("", "")]
                public static partial void Empty();

                [JSExport]
                public static int @return(int @class) => @class;

                [JSExport]
                public static int \u0045xport(int @class) => @class;
            }
            """;

        public static readonly string IncrementalGeneration = """
            using System.Runtime.InteropServices.JavaScript;

            public partial class Basic
            {
                [JSImport("import")]
                public static partial int Import(int value);

                [JSExport]
                public static int Export(int value) => value;
            }
            """;

        public static string DefaultReturnMarshaler<T>() => DefaultReturnMarshaler(typeof(T).ToString());

        public static string DefaultReturnMarshaler(string type) => $$"""
            //DefaultReturnMarshaler<{{type}}>
            using System.Runtime.InteropServices.JavaScript;
            partial class Basic
            {
                [JSImport("DoesNotExist")]
                public static partial {{type}} Import1();
            
                [JSExport()]
                public static {{type}} Export1(){ throw null; }
            }
            """;

    }
}
