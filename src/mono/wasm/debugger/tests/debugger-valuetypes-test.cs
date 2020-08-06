// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
namespace DebuggerTests
{
    public class ValueTypesTest
    { //Only append content to this class as the test suite depends on line info

        public static void MethodWithLocalStructs()
        {
            var ss_local = new SimpleStruct("set in MethodWithLocalStructs", 1, DateTimeKind.Utc);
            var gs_local = new GenericStruct<ValueTypesTest> { StringField = "gs_local#GenericStruct<ValueTypesTest>#StringField" };

            ValueTypesTest vt_local = new ValueTypesTest
            {
                StringField = "string#0",
                SimpleStructField = new SimpleStruct("SimpleStructField#string#0", 5, DateTimeKind.Local),
                SimpleStructProperty = new SimpleStruct("SimpleStructProperty#string#0", 2, DateTimeKind.Utc), DT = new DateTime(2020, 1, 2, 3, 4, 5), RGB = RGB.Blue
            };
            Console.WriteLine($"Using the struct: {ss_local.gs.StringField}, gs: {gs_local.StringField}, {vt_local.StringField}");
        }

        public static void TestStructsAsMethodArgs()
        {
            var ss_local = new SimpleStruct("ss_local#SimpleStruct#string#0", 5, DateTimeKind.Local);
            var ss_ret = MethodWithStructArgs("TestStructsAsMethodArgs#label", ss_local, 3);
            Console.WriteLine($"got back ss_local: {ss_local.gs.StringField}, ss_ret: {ss_ret.gs.StringField}");
        }

        static SimpleStruct MethodWithStructArgs(string label, SimpleStruct ss_arg, int x)
        {
            Console.WriteLine($"- ss_arg: {ss_arg.str_member}");
            ss_arg.Kind = DateTimeKind.Utc;
            ss_arg.str_member = $"ValueTypesTest#MethodWithStructArgs#updated#ss_arg#str_member";
            ss_arg.gs.StringField = $"ValueTypesTest#MethodWithStructArgs#updated#gs#StringField#{x}";
            return ss_arg;
        }

        public static async Task<bool> MethodWithLocalStructsStaticAsync()
        {
            var ss_local = new SimpleStruct("set in MethodWithLocalStructsStaticAsync", 1, DateTimeKind.Utc);
            var gs_local = new GenericStruct<int>
            {
                StringField = "gs_local#GenericStruct<ValueTypesTest>#StringField",
                List = new System.Collections.Generic.List<int> { 5, 3 },
                Options = Options.Option2

            };

            var result = await ss_local.AsyncMethodWithStructArgs(gs_local);
            Console.WriteLine($"Using the struct: {ss_local.gs.StringField}, result: {result}");

            return result;
        }

        public string StringField;
        public SimpleStruct SimpleStructProperty { get; set; }
        public SimpleStruct SimpleStructField;

        public struct SimpleStruct
        {
            public uint V { get { return 0xDEADBEEF + (uint)dt.Month; } set { } }
            public string str_member;
            public DateTime dt;
            public GenericStruct<DateTime> gs;
            public DateTimeKind Kind;

            public SimpleStruct(string str, int f, DateTimeKind kind)
            {
                str_member = $"{str}#SimpleStruct#str_member";
                dt = new DateTime(2020 + f, 1 + f, 2 + f, 3 + f, 5 + f, 6 + f);
                gs = new GenericStruct<DateTime>
                {
                    StringField = $"{str}#SimpleStruct#gs#StringField",
                    List = new System.Collections.Generic.List<DateTime> { new DateTime(2010 + f, 2 + f, 3 + f, 10 + f, 2 + f, 3 + f) },
                    Options = Options.Option1
                };
                Kind = kind;
            }

            public Task<bool> AsyncMethodWithStructArgs(GenericStruct<int> gs)
            {
                Console.WriteLine($"placeholder line for a breakpoint");
                if (gs.List.Count > 0)
                    return Task.FromResult(true);

                return Task.FromResult(false);
            }
        }

        public struct GenericStruct<T>
        {
            public System.Collections.Generic.List<T> List;
            public string StringField;

            public Options Options { get; set; }
        }

        public DateTime DT { get; set; }
        public RGB RGB;

        public static void MethodWithLocalsForToStringTest(bool call_other)
        {
            var dt0 = new DateTime(2020, 1, 2, 3, 4, 5);
            var dt1 = new DateTime(2010, 5, 4, 3, 2, 1);
            var ts = dt0 - dt1;
            var dto = new DateTimeOffset(dt0, new TimeSpan(4, 5, 0));
            decimal dec = 123987123;
            var guid = new Guid("3d36e07e-ac90-48c6-b7ec-a481e289d014");

            var dts = new DateTime[]
            {
                new DateTime(1983, 6, 7, 5, 6, 10),
                new DateTime(1999, 10, 15, 1, 2, 3)
            };

            var obj = new ClassForToStringTests
            {
                DT = new DateTime(2004, 10, 15, 1, 2, 3),
                DTO = new DateTimeOffset(dt0, new TimeSpan(2, 14, 0)),
                TS = ts,
                Dec = 1239871,
                Guid = guid
            };

            var sst = new StructForToStringTests
            {
                DT = new DateTime(2004, 10, 15, 1, 2, 3),
                DTO = new DateTimeOffset(dt0, new TimeSpan(3, 15, 0)),
                TS = ts,
                Dec = 1239871,
                Guid = guid
            };
            Console.WriteLine($"MethodWithLocalsForToStringTest: {dt0}, {dt1}, {ts}, {dec}, {guid}, {dts[0]}, {obj.DT}, {sst.DT}");
            if (call_other)
                MethodWithArgumentsForToStringTest(call_other, dt0, dt1, ts, dto, dec, guid, dts, obj, sst);
        }

        static void MethodWithArgumentsForToStringTest(
            bool call_other, // not really used, just to help with using common code in the tests
            DateTime dt0, DateTime dt1, TimeSpan ts, DateTimeOffset dto, decimal dec,
            Guid guid, DateTime[] dts, ClassForToStringTests obj, StructForToStringTests sst)
        {
            Console.WriteLine($"MethodWithArgumentsForToStringTest: {dt0}, {dt1}, {ts}, {dec}, {guid}, {dts[0]}, {obj.DT}, {sst.DT}");
        }

        public static async Task MethodWithLocalsForToStringTestAsync(bool call_other)
        {
            var dt0 = new DateTime(2020, 1, 2, 3, 4, 5);
            var dt1 = new DateTime(2010, 5, 4, 3, 2, 1);
            var ts = dt0 - dt1;
            var dto = new DateTimeOffset(dt0, new TimeSpan(4, 5, 0));
            decimal dec = 123987123;
            var guid = new Guid("3d36e07e-ac90-48c6-b7ec-a481e289d014");

            var dts = new DateTime[]
            {
                new DateTime(1983, 6, 7, 5, 6, 10),
                new DateTime(1999, 10, 15, 1, 2, 3)
            };

            var obj = new ClassForToStringTests
            {
                DT = new DateTime(2004, 10, 15, 1, 2, 3),
                DTO = new DateTimeOffset(dt0, new TimeSpan(2, 14, 0)),
                TS = ts,
                Dec = 1239871,
                Guid = guid
            };

            var sst = new StructForToStringTests
            {
                DT = new DateTime(2004, 10, 15, 1, 2, 3),
                DTO = new DateTimeOffset(dt0, new TimeSpan(3, 15, 0)),
                TS = ts,
                Dec = 1239871,
                Guid = guid
            };
            Console.WriteLine($"MethodWithLocalsForToStringTest: {dt0}, {dt1}, {ts}, {dec}, {guid}, {dts[0]}, {obj.DT}, {sst.DT}");
            if (call_other)
                await MethodWithArgumentsForToStringTestAsync(call_other, dt0, dt1, ts, dto, dec, guid, dts, obj, sst);
        }

        static async Task MethodWithArgumentsForToStringTestAsync(
            bool call_other, // not really used, just to help with using common code in the tests
            DateTime dt0, DateTime dt1, TimeSpan ts, DateTimeOffset dto, decimal dec,
            Guid guid, DateTime[] dts, ClassForToStringTests obj, StructForToStringTests sst)
        {
            Console.WriteLine($"MethodWithArgumentsForToStringTest: {dt0}, {dt1}, {ts}, {dec}, {guid}, {dts[0]}, {obj.DT}, {sst.DT}");
        }

        public static void MethodUpdatingValueTypeMembers()
        {
            var obj = new ClassForToStringTests
            {
                DT = new DateTime(1, 2, 3, 4, 5, 6)
            };
            var vt = new StructForToStringTests
            {
                DT = new DateTime(4, 5, 6, 7, 8, 9)
            };
            Console.WriteLine($"#1");
            obj.DT = new DateTime(9, 8, 7, 6, 5, 4);
            vt.DT = new DateTime(5, 1, 3, 7, 9, 10);
            Console.WriteLine($"#2");
        }

        public static async Task MethodUpdatingValueTypeLocalsAsync()
        {
            var dt = new DateTime(1, 2, 3, 4, 5, 6);
            Console.WriteLine($"#1");
            dt = new DateTime(9, 8, 7, 6, 5, 4);
            Console.WriteLine($"#2");
        }

        public static void MethodUpdatingVTArrayMembers()
        {
            var ssta = new []
            {
                new StructForToStringTests { DT = new DateTime(1, 2, 3, 4, 5, 6) }
            };
            Console.WriteLine($"#1");
            ssta[0].DT = new DateTime(9, 8, 7, 6, 5, 4);
            Console.WriteLine($"#2");
        }
    }

    class ClassForToStringTests
    {
        public DateTime DT;
        public DateTimeOffset DTO;
        public TimeSpan TS;
        public decimal Dec;
        public Guid Guid;
    }

    struct StructForToStringTests
    {
        public DateTime DT;
        public DateTimeOffset DTO;
        public TimeSpan TS;
        public decimal Dec;
        public Guid Guid;
    }

    public enum RGB
    {
        Red,
        Green,
        Blue
    }

    [Flags]
    public enum Options
    {
        None = 0,
        Option1 = 1,
        Option2 = 2,
        Option3 = 4,

        All = Option1 | Option3
    }
}