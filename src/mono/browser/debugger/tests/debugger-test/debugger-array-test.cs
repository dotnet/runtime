// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
namespace DebuggerTests
{
    public class ArrayTestsClass
    {
        public static void PrimitiveTypeLocals(bool call_other = false)
        {
            var int_arr = new int[] { 4, 70, 1 };
            var int_arr_empty = new int[0];
            int[] int_arr_null = null;

            if (call_other)
                OtherMethod();

            Console.WriteLine($"int_arr: {int_arr.Length}, {int_arr_empty.Length}, {int_arr_null?.Length}");
        }

        public static void ValueTypeLocals(bool call_other = false)
        {
            var point_arr = new Point[]
            {
            new Point { X = 5, Y = -2, Id = "point_arr#Id#0", Color = RGB.Green },
            new Point { X = 123, Y = 0, Id = "point_arr#Id#1", Color = RGB.Blue },
            };

            var point_arr_empty = new Point[0];
            Point[] point_arr_null = null;

            if (call_other)
                OtherMethod();

            Console.WriteLine($"point_arr: {point_arr.Length}, {point_arr_empty.Length}, {point_arr_null?.Length}");
        }

        public static void ObjectTypeLocals(bool call_other = false)
        {
            var class_arr = new SimpleClass[]
            {
            new SimpleClass { X = 5, Y = -2, Id = "class_arr#Id#0", Color = RGB.Green },
            null,
            new SimpleClass { X = 123, Y = 0, Id = "class_arr#Id#2", Color = RGB.Blue },
            };

            var class_arr_empty = new SimpleClass[0];
            SimpleClass[] class_arr_null = null;

            if (call_other)
                OtherMethod();

            Console.WriteLine($"class_arr: {class_arr.Length}, {class_arr_empty.Length}, {class_arr_null?.Length}");
        }

        public static void GenericTypeLocals(bool call_other = false)
        {
            var gclass_arr = new GenericClass<int>[]
            {
            null,
            new GenericClass<int> { Id = "gclass_arr#1#Id", Color = RGB.Red, Value = 5 },
            new GenericClass<int> { Id = "gclass_arr#2#Id", Color = RGB.Blue, Value = -12 },
            };

            var gclass_arr_empty = new GenericClass<int>[0];
            GenericClass<int>[] gclass_arr_null = null;

            if (call_other)
                OtherMethod();

            Console.WriteLine($"gclass_arr: {gclass_arr.Length}, {gclass_arr_empty.Length}, {gclass_arr_null?.Length}");
        }

        public static void GenericValueTypeLocals(bool call_other = false)
        {
            var gvclass_arr = new SimpleGenericStruct<Point>[]
            {
            new SimpleGenericStruct<Point> { Id = "gvclass_arr#1#Id", Color = RGB.Red, Value = new Point { X = 100, Y = 200, Id = "gvclass_arr#1#Value#Id", Color = RGB.Red } },
            new SimpleGenericStruct<Point> { Id = "gvclass_arr#2#Id", Color = RGB.Blue, Value = new Point { X = 10, Y = 20, Id = "gvclass_arr#2#Value#Id", Color = RGB.Green } }
            };

            var gvclass_arr_empty = new SimpleGenericStruct<Point>[0];
            SimpleGenericStruct<Point>[] gvclass_arr_null = null;

            if (call_other)
                OtherMethod();

            Console.WriteLine($"gvclass_arr: {gvclass_arr.Length}, {gvclass_arr_empty.Length}, {gvclass_arr_null?.Length}");
        }

        static void OtherMethod()
        {
            YetAnotherMethod();
            Console.WriteLine($"Just a placeholder for breakpoints");
        }

        static void YetAnotherMethod()
        {
            Console.WriteLine($"Just a placeholder for breakpoints");
        }

        public static void ObjectArrayMembers()
        {
            var c = new Container
            {
                id = "c#id",
                ClassArrayProperty = new SimpleClass[]
                {
                new SimpleClass { X = 5, Y = -2, Id = "ClassArrayProperty#Id#0", Color = RGB.Green },
                new SimpleClass { X = 30, Y = 1293, Id = "ClassArrayProperty#Id#1", Color = RGB.Green },
                null
                },
                ClassArrayField = new SimpleClass[]
                {
                null,
                new SimpleClass { X = 5, Y = -2, Id = "ClassArrayField#Id#1", Color = RGB.Blue },
                new SimpleClass { X = 30, Y = 1293, Id = "ClassArrayField#Id#2", Color = RGB.Green },
                },
                PointsProperty = new Point[]
                {
                new Point { X = 5, Y = -2, Id = "PointsProperty#Id#0", Color = RGB.Green },
                new Point { X = 123, Y = 0, Id = "PointsProperty#Id#1", Color = RGB.Blue },
                },
                PointsField = new Point[]
                {
                new Point { X = 5, Y = -2, Id = "PointsField#Id#0", Color = RGB.Green },
                new Point { X = 123, Y = 0, Id = "PointsField#Id#1", Color = RGB.Blue },
                }
            };

            Console.WriteLine($"Back from PlaceholderMethod, {c.ClassArrayProperty?.Length}");
            c.PlaceholderMethod();
            Console.WriteLine($"Back from PlaceholderMethod, {c.id}");
        }

        public static async Task<bool> ValueTypeLocalsAsync(bool call_other = false)
        {
            var gvclass_arr = new SimpleGenericStruct<Point>[]
            {
            new SimpleGenericStruct<Point> { Id = "gvclass_arr#1#Id", Color = RGB.Red, Value = new Point { X = 100, Y = 200, Id = "gvclass_arr#1#Value#Id", Color = RGB.Red } },
            new SimpleGenericStruct<Point> { Id = "gvclass_arr#2#Id", Color = RGB.Blue, Value = new Point { X = 10, Y = 20, Id = "gvclass_arr#2#Value#Id", Color = RGB.Green } }
            };

            var gvclass_arr_empty = new SimpleGenericStruct<Point>[0];
            SimpleGenericStruct<Point>[] gvclass_arr_null = null;
            Console.WriteLine($"ValueTypeLocalsAsync: call_other: {call_other}");
            SimpleGenericStruct<Point> gvclass;
            Point[] points = null;

            if (call_other)
            {
                (gvclass, points) = await new ArrayTestsClass().InstanceMethodValueTypeLocalsAsync<SimpleGenericStruct<Point>>(gvclass_arr[0]);
                Console.WriteLine($"* gvclass: {gvclass}, points: {points.Length}");
            }

            Console.WriteLine($"gvclass_arr: {gvclass_arr.Length}, {gvclass_arr_empty.Length}, {gvclass_arr_null?.Length}");
            return true;
        }

        public async Task<(T, Point[])> InstanceMethodValueTypeLocalsAsync<T>(T t1)
        {
            var point_arr = new Point[]
            {
                new Point { X = 5, Y = -2, Id = "point_arr#Id#0", Color = RGB.Red },
                new Point { X = 123, Y = 0, Id = "point_arr#Id#1", Color = RGB.Blue }
            };
            var point = new Point { X = 45, Y = 51, Id = "point#Id", Color = RGB.Green };

            Console.WriteLine($"point_arr: {point_arr.Length}, T: {t1}, point: {point}");
            return await Task.FromResult((t1, new Point[] { point_arr[0], point_arr[1], point }));
        }

        // A workaround for method invocations on structs not working right now
        public static async Task EntryPointForStructMethod(bool call_other = false)
        {
            await Point.AsyncMethod(call_other);
        }

        public static void GenericValueTypeLocals2(bool call_other = false)
        {
            var gvclass_arr = new SimpleGenericStruct<Point[]>[]
            {
            new SimpleGenericStruct<Point[]>
            {
            Id = "gvclass_arr#0#Id",
            Color = RGB.Red,
            Value = new Point[]
            {
            new Point { X = 100, Y = 200, Id = "gvclass_arr#0#0#Value#Id", Color = RGB.Red },
            new Point { X = 100, Y = 200, Id = "gvclass_arr#0#1#Value#Id", Color = RGB.Green }
            }
            },

            new SimpleGenericStruct<Point[]>
            {
            Id = "gvclass_arr#1#Id",
            Color = RGB.Blue,
            Value = new Point[]
            {
            new Point { X = 100, Y = 200, Id = "gvclass_arr#1#0#Value#Id", Color = RGB.Green },
            new Point { X = 100, Y = 200, Id = "gvclass_arr#1#1#Value#Id", Color = RGB.Blue }
            }
            },
            };

            var gvclass_arr_empty = new SimpleGenericStruct<Point[]>[0];
            SimpleGenericStruct<Point[]>[] gvclass_arr_null = null;

            if (call_other)
                OtherMethod();

            Console.WriteLine($"gvclass_arr: {gvclass_arr.Length}, {gvclass_arr_empty.Length}, {gvclass_arr_null?.Length}");
        }
    }

    public class Container
    {
        public string id;
        public SimpleClass[] ClassArrayProperty { get; set; }
        public SimpleClass[] ClassArrayField;

        public Point[] PointsProperty { get; set; }
        public Point[] PointsField;

        public void PlaceholderMethod()
        {
            Console.WriteLine($"Container.PlaceholderMethod");
        }
    }

    public struct Point
    {
        public int X, Y;
        public string Id { get; set; }
        public RGB Color { get; set; }

        /* instance too */
        public static async Task AsyncMethod(bool call_other)
        {
            int local_i = 5;
            var sc = new SimpleClass { X = 10, Y = 45, Id = "sc#Id", Color = RGB.Blue };
            if (call_other)
                await new Point { X = 90, Y = -4, Id = "point#Id", Color = RGB.Green }.AsyncInstanceMethod(sc);
            Console.WriteLine($"AsyncMethod local_i: {local_i}, sc: {sc.Id}");
        }

        public async Task AsyncInstanceMethod(SimpleClass sc_arg)
        {
            var local_gs = new SimpleGenericStruct<int> { Id = "local_gs#Id", Color = RGB.Green, Value = 4 };
            sc_arg.Id = "sc_arg#Id";
            Console.WriteLine($"AsyncInstanceMethod sc_arg: {sc_arg.Id}, local_gs: {local_gs.Id}"); await Task.CompletedTask;
        }

        public void GenericInstanceMethod<T>(T sc_arg) where T : SimpleClass
        {
            var local_gs = new SimpleGenericStruct<int> { Id = "local_gs#Id", Color = RGB.Green, Value = 4 };
            sc_arg.Id = "sc_arg#Id";
            Console.WriteLine($"AsyncInstanceMethod sc_arg: {sc_arg.Id}, local_gs: {local_gs.Id}");
        }
    }

    public class SimpleClass
    {
        public int X, Y;
        public string Id { get; set; }
        public RGB Color { get; set; }

        public Point PointWithCustomGetter { get { return new Point { X = 100, Y = 400, Id = "SimpleClass#Point#gen#Id", Color = RGB.Green }; } }
    }

    public class GenericClass<T>
    {
        public string Id { get; set; }
        public RGB Color { get; set; }
        public T Value { get; set; }
    }

    public struct SimpleGenericStruct<T>
    {
        public string Id { get; set; }
        public RGB Color { get; set; }
        public T Value { get; set; }
    }

    public class EntryClass
    {
        public static void run()
        {
            ArrayTestsClass.PrimitiveTypeLocals(true);
            ArrayTestsClass.ValueTypeLocals(true);
            ArrayTestsClass.ObjectTypeLocals(true);

            ArrayTestsClass.GenericTypeLocals(true);
            ArrayTestsClass.GenericValueTypeLocals(true);
            ArrayTestsClass.GenericValueTypeLocals2(true);

            ArrayTestsClass.ObjectArrayMembers();

            ArrayTestsClass.ValueTypeLocalsAsync(true).Wait();

            ArrayTestsClass.EntryPointForStructMethod(true).Wait();

            var sc = new SimpleClass { X = 10, Y = 45, Id = "sc#Id", Color = RGB.Blue };
            new Point { X = 90, Y = -4, Id = "point#Id", Color = RGB.Green }.GenericInstanceMethod(sc);
        }
    }
    public class MultiDimensionalArray
    {
        public static void run()
        {
            var int_arr_1 = new int[2];
            int_arr_1[0] = 0;
            int_arr_1[1] = 1;

            var int_arr_2 = new int[2, 3];
            int_arr_2[0, 0] = 0;
            int_arr_2[0, 1] = 1;
            int_arr_2[0, 2] = 2;
            int_arr_2[1, 0] = 10;
            int_arr_2[1, 1] = 11;
            int_arr_2[1, 2] = 12;

            var int_arr_3 = new int[2, 3, 3];
            int_arr_3[0, 0, 0] = 0;
            int_arr_3[0, 0, 1] = 1;
            int_arr_3[0, 0, 2] = 2;
            int_arr_3[0, 1, 0] = 10;
            int_arr_3[0, 1, 1] = 11;
            int_arr_3[0, 1, 2] = 12;
            int_arr_3[0, 2, 0] = 20;
            int_arr_3[0, 2, 1] = 21;
            int_arr_3[0, 2, 2] = 22;
            int_arr_3[1, 0, 0] = 100;
            int_arr_3[1, 0, 1] = 101;
            int_arr_3[1, 0, 2] = 102;
            int_arr_3[1, 1, 0] = 110;
            int_arr_3[1, 1, 1] = 111;
            int_arr_3[1, 1, 2] = 112;
            int_arr_3[1, 2, 0] = 120;
            int_arr_3[1, 2, 1] = 121;
            int_arr_3[1, 2, 2] = 122;

            System.Diagnostics.Debugger.Break();
            Console.WriteLine($"int_arr: {int_arr_3.Length}");
        }
    }

    public class InlineArray
    {
        struct StructWithInlineArray
        {
            public Arr1 myInlineArray;
        }
        class ClassWithInlineArrayField
        {
            public Arr1 myInlineArray;
            public Arr1 InlineArrayProp => myInlineArray;
            public StructWithInlineArray myStructWithInlineArray;
        }
        class One {}
        class Two {}
        class Three {}
        class Four {}
        public struct E
        {
            public int x;
            public int y;
            public object o;
        }

        [System.Runtime.CompilerServices.InlineArray(Length)]
        public struct Arr1
        {
            public const int Length = 42;
            public E e;
        }

        [System.Runtime.CompilerServices.InlineArray(Length)]
        struct Arr2
        {
            public const int Length = 42;
            public int e;
            public int InlineMethod(int n) => n + 100;
        }
        
        [System.Runtime.CompilerServices.InlineArray(1)]
        struct Arr3
        {
            public int e;
        }

        [System.Runtime.CompilerServices.InlineArray(Length)]
        struct Arr4
        {
            public static int myStaticField = 50;
            public const int Length = 42;
            public E e;
        }

        private static Arr1 Initialize(Arr1 s)
        {
            s[0].o = new One();
            s[0].x = 1;
            s[0].y = 2;
            s[1].o = new Two();
            s[1].x = 3;
            s[1].y = 4;
            s[2].o = new Three();
            s[2].x = 5;
            s[2].y = 6;
            s[3].o = new Four();
            s[3].x = 7;
            s[3].y = 8;
            return s;
        }
        public static void run()
        {
            int a = 0;
            int b = 1;
            Arr1 s = default;
            s = Initialize(s);
            ClassWithInlineArrayField classWithInlineArrayField = new ();
            s = Initialize(classWithInlineArrayField.myInlineArray);
            classWithInlineArrayField.myInlineArray[0].o = new One();
            classWithInlineArrayField.myInlineArray[0].x = 1;
            classWithInlineArrayField.myInlineArray[0].y = 2;
            classWithInlineArrayField.myInlineArray[1].o = new Two();
            classWithInlineArrayField.myInlineArray[1].x = 3;
            classWithInlineArrayField.myInlineArray[1].y = 4;            
            //classWithInlineArrayField.InlineArrayProp[0].o = new One();
            //classWithInlineArrayField.InlineArrayProp[0].x = 1;
            //classWithInlineArrayField.InlineArrayProp[0].y = 2;
            //classWithInlineArrayField.InlineArrayProp[1].o = new Two();
            //classWithInlineArrayField.InlineArrayProp[1].x = 3;
            //classWithInlineArrayField.InlineArrayProp[1].y = 4;
            classWithInlineArrayField.myStructWithInlineArray.myInlineArray[0].o = new One();
            classWithInlineArrayField.myStructWithInlineArray.myInlineArray[0].x = 1;
            classWithInlineArrayField.myStructWithInlineArray.myInlineArray[0].y = 2;
            classWithInlineArrayField.myStructWithInlineArray.myInlineArray[1].o = new Two();
            classWithInlineArrayField.myStructWithInlineArray.myInlineArray[1].x = 3;
            classWithInlineArrayField.myStructWithInlineArray.myInlineArray[1].y = 4;
            System.Diagnostics.Debugger.Break();
            Console.WriteLine(s[0].o.GetType().Name);
        }

        public static void run2()
        {
            Arr2 a2 = default; //test with primitive type
            Arr3 a3 = default; //test with length==1
            Arr4 a4 = default; //test with static field
            a2[0] = 1; 
            a3[0] = 2;
            a4[0].o = new One();
            a4[0].x = 1;
            a4[0].y = 2;
            a4[1].o = new Two();
            a4[1].x = 3;
            a4[1].y = 4;
            Console.WriteLine($"olha thays - {Arr4.myStaticField}");
            System.Diagnostics.Debugger.Break();
        }
    }
}
