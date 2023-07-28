// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
/// <summary>Generic chaos types</summary>
/// <remarks>CommandLine:
///<code>GenericChaos.exe /mtc:5 /mtcc:1 /mic:10 /ol:Cs /ol:Vb /mtpc:1 /mmtpc:1
///</code>
///Data:
///<code>Help:	False
/// MaxGenerationDepth:	2
/// MaxTypeParameterCount:	1
/// MaxMethodTypeParameterCount:	1
/// MaxTypeCount:	5
/// MaxMethodCallDepth:	1000
/// MaxTypeInheranceCount:	1
/// MaxStaticFieldCount:	2
/// MaxInterfaceCount:	10
/// GenerateInterfaces:	True
/// GenerateVirtualMethods:	True
/// GenerateMethods:	True
/// GenerateGenericMethods:	True
/// GenerateNonInlinableMethods:	True
/// GenerateStaticMethods:	True
/// GenerateInstanceMethods:	True
/// GenerateRecursiveMethods:	True
/// GenerateStaticFields:	True
/// GenerateInstanceFields:	True
/// IntermediateTypeRealization:	True
/// GenerateConstructorConstraints:	True
/// GenerateTypeParameterConstraints:	True
/// GenerateMethodParameterConstraints:	True
/// OutputPath:	chaos
/// OutputLanguages:
/// 	Cs
/// 	Vb
/// OutputNamespace:	Chaos
/// ShowOutputInConsole:	False
/// CompileAndRun:	False
/// </code></remarks>
namespace Chaos
{
    using System;


    public class A0A0 : A0, IA1
    {

        private IA1 _fA0A01;

        private static A0A1<A0A0> _sfA0A00;

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void VerifyA0A0NotInlinedGenericStatic<T>()
            where T : new()
        {
            System.Console.WriteLine(typeof(T));
            System.Console.WriteLine(typeof(A0A0));
            T t2 = new T();
            A0A0._sfA0A00 = new A0A1<A0A0>();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void VerifyA0A0NotInlinedStatic()
        {
            System.Console.WriteLine(typeof(A0A0));
            A0A0._sfA0A00 = new A0A1<A0A0>();
        }

        public static void VerifyA0A0GenericStatic<T>()
            where T : new()
        {
            System.Console.WriteLine(typeof(T));
            System.Console.WriteLine(typeof(A0A0));
            T t2 = new T();
            A0A0._sfA0A00 = new A0A1<A0A0>();
        }

        public static void VerifyA0A0Static()
        {
            System.Console.WriteLine(typeof(A0A0));
            A0A0._sfA0A00 = new A0A1<A0A0>();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void VerifyA0A0NotInlinedGeneric<T>()
            where T : new()
        {
            System.Console.WriteLine(this);
            System.Console.WriteLine(typeof(T));
            System.Console.WriteLine(typeof(A0A0));
            T t3 = new T();
            A0A0._sfA0A00 = new A0A1<A0A0>();
            this._fA0A01 = new A0A0();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void VerifyA0A0NotInlined()
        {
            System.Console.WriteLine(this);
            System.Console.WriteLine(typeof(A0A0));
            A0A0._sfA0A00 = new A0A1<A0A0>();
            this._fA0A01 = new A0A0();
        }

        public override void VirtualVerifyGeneric<T>()
        {
            base.VirtualVerifyGeneric<T>();
            System.Console.WriteLine(this);
            System.Console.WriteLine(typeof(T));
            System.Console.WriteLine(typeof(A0A0));
            A0A0._sfA0A00 = new A0A1<A0A0>();
            this._fA0A01 = new A0A0();
        }

        public override void VirtualVerify()
        {
            base.VirtualVerify();
            System.Console.WriteLine(this);
            System.Console.WriteLine(typeof(A0A0));
            A0A0._sfA0A00 = new A0A1<A0A0>();
            this._fA0A01 = new A0A0();
        }

        public void RecurseA0A0(int depth)
        {
            if ((depth < 0))
            {
                return;
            }
            System.Console.Write(".");
            A0 next = new A0();
            next.RecurseA0((depth - 1));
        }

        public void CreateAllTypesA0A0()
        {
            A0 v0 = new A0();
            v0.VerifyInterfaceIA1();
            A0 v1 = new A0();
            v1.VerifyInterfaceGenericIA1<A0A1<A0>>();
            A0.VerifyA0NotInlinedGenericStatic<A0>();
            A0.VerifyA0NotInlinedStatic();
            A0.VerifyA0GenericStatic<A0A0>();
            A0.VerifyA0Static();
            A0 v2 = new A0();
            v2.VerifyA0NotInlinedGeneric<A0>();
            A0 v3 = new A0();
            v3.VerifyA0NotInlined();
            A0 v4 = new A0();
            v4.VirtualVerifyGeneric<IA1>();
            A0 v5 = new A0();
            v5.VirtualVerify();
            A0 v6 = new A0();
            v6.DeepRecursion();
            IA1 i7 = ((IA1)(new A0()));
            i7.VerifyInterfaceIA1();
            IA1 i8 = ((IA1)(new A0()));
            i8.VerifyInterfaceGenericIA1<A0A0A0<A0A0>>();
            A0A0.VerifyA0A0NotInlinedGenericStatic<A0A0A0<A0A0>>();
            A0A0.VerifyA0A0NotInlinedStatic();
            A0A0.VerifyA0A0GenericStatic<A0A0>();
            A0A0.VerifyA0A0Static();
            A0A0 v9 = new A0A0();
            v9.VerifyA0A0NotInlinedGeneric<A0A0A0<A0A0>>();
            A0A0 v10 = new A0A0();
            v10.VerifyA0A0NotInlined();
            A0A0 v11 = new A0A0();
            v11.VirtualVerifyGeneric<A0A1<A0>>();
            A0A0 v12 = new A0A0();
            v12.VirtualVerify();
            IA1 i13 = ((IA1)(new A0A0()));
            i13.VerifyInterfaceIA1();
            IA1 i14 = ((IA1)(new A0A0()));
            i14.VerifyInterfaceGenericIA1<A0>();
            A0A1<A0>.VerifyA0A1NotInlinedGenericStatic<A0>();
            A0A1<A0>.VerifyA0A1NotInlinedStatic();
            A0A1<A0>.VerifyA0A1GenericStatic<A0A0>();
            A0A1<A0>.VerifyA0A1Static();
            A0A1<A0A0> v15 = new A0A1<A0A0>();
            v15.VerifyA0A1NotInlinedGeneric<A0A1<A0A0>>();
            A0A1<A0A0> v16 = new A0A1<A0A0>();
            v16.VerifyA0A1NotInlined();
            A0A0A0<A0>.VerifyA0A0A0NotInlinedGenericStatic<A0A0A0<A0>>();
            A0A0A0<A0A0A0<A0>>.VerifyA0A0A0NotInlinedStatic();
            A0A0A0<A0A0A0<A0A0A0<A0>>>.VerifyA0A0A0GenericStatic<A0A0A0<A0A0A0<A0A0A0<A0>>>>();
            A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>.VerifyA0A0A0Static();
            A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>> v17 = new A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>();
            v17.VerifyA0A0A0NotInlinedGeneric<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>();
            A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>> v18 = new A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>();
            v18.VerifyA0A0A0NotInlined();
        }
    }

    public class A0A1<T0> : A0
        where T0 : new()
    {

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void VerifyA0A1NotInlinedGenericStatic<T>()
            where T : new()
        {
            System.Console.WriteLine(typeof(T));
            T0 t1 = new T0();
            T t2 = new T();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void VerifyA0A1NotInlinedStatic()
        {
            T0 t0 = new T0();
        }

        public static void VerifyA0A1GenericStatic<T>()
            where T : new()
        {
            System.Console.WriteLine(typeof(T));
            T0 t1 = new T0();
            T t2 = new T();
        }

        public static void VerifyA0A1Static()
        {
            T0 t0 = new T0();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void VerifyA0A1NotInlinedGeneric<T>()
            where T : new()
        {
            System.Console.WriteLine(this);
            System.Console.WriteLine(typeof(T));
            T0 t2 = new T0();
            T t3 = new T();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void VerifyA0A1NotInlined()
        {
            System.Console.WriteLine(this);
            T0 t1 = new T0();
        }

        public void RecurseA0A1(int depth)
        {
            if ((depth < 0))
            {
                return;
            }
            System.Console.Write(".");
            A0A0 next = new A0A0();
            next.RecurseA0A0((depth - 1));
        }

        public void CreateAllTypesA0A1()
        {
            A0 v0 = new A0();
            v0.VerifyInterfaceIA1();
            A0 v1 = new A0();
            v1.VerifyInterfaceGenericIA1<A0A1<A0A0>>();
            A0.VerifyA0NotInlinedGenericStatic<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>();
            A0.VerifyA0NotInlinedStatic();
            A0.VerifyA0GenericStatic<A0A1<A0A0>>();
            A0.VerifyA0Static();
            A0 v2 = new A0();
            v2.VerifyA0NotInlinedGeneric<A0>();
            A0 v3 = new A0();
            v3.VerifyA0NotInlined();
            A0 v4 = new A0();
            v4.VirtualVerifyGeneric<A0A0>();
            A0 v5 = new A0();
            v5.VirtualVerify();
            A0 v6 = new A0();
            v6.DeepRecursion();
            IA1 i7 = ((IA1)(new A0()));
            i7.VerifyInterfaceIA1();
            IA1 i8 = ((IA1)(new A0()));
            i8.VerifyInterfaceGenericIA1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>();
            A0A0.VerifyA0A0NotInlinedGenericStatic<A0A1<A0A0>>();
            A0A0.VerifyA0A0NotInlinedStatic();
            A0A0.VerifyA0A0GenericStatic<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>();
            A0A0.VerifyA0A0Static();
            A0A0 v9 = new A0A0();
            v9.VerifyA0A0NotInlinedGeneric<A0A0>();
            A0A0 v10 = new A0A0();
            v10.VerifyA0A0NotInlined();
            A0A0 v11 = new A0A0();
            v11.VirtualVerifyGeneric<A0>();
            A0A0 v12 = new A0A0();
            v12.VirtualVerify();
            IA1 i13 = ((IA1)(new A0A0()));
            i13.VerifyInterfaceIA1();
            IA1 i14 = ((IA1)(new A0A0()));
            i14.VerifyInterfaceGenericIA1<A0A1<A0A0>>();
            A0A1<A0>.VerifyA0A1NotInlinedGenericStatic<A0>();
            A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>.VerifyA0A1NotInlinedStatic();
            A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>.VerifyA0A1GenericStatic<A0A0>();
            A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>.VerifyA0A1Static();
            A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>> v15 = new A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>();
            v15.VerifyA0A1NotInlinedGeneric<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>();
            A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>> v16 = new A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>();
            v16.VerifyA0A1NotInlined();
            A0A0A0<A0A0>.VerifyA0A0A0NotInlinedGenericStatic<A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>>();
            A0A0A0<A0>.VerifyA0A0A0NotInlinedStatic();
            A0A0A0<A0A0A0<A0>>.VerifyA0A0A0GenericStatic<A0>();
            A0A0A0<A0A0>.VerifyA0A0A0Static();
            A0A0A0<A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>> v17 = new A0A0A0<A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>>();
            v17.VerifyA0A0A0NotInlinedGeneric<A0>();
            A0A0A0<A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>> v18 = new A0A0A0<A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>>();
            v18.VerifyA0A0A0NotInlined();
        }
    }

    public class Program
    {

        [Fact]
        public static int TestEntryPoint()
        {
            A0 v0 = new A0();
            v0.CreateAllTypesA0();
            A0A0 v1 = new A0A0();
            v1.CreateAllTypesA0A0();
            A0A1<A0A0A0<A0A0A0<A0A0A0<A0A1<A0A0A0<A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>>>>>>> v2 = new A0A1<A0A0A0<A0A0A0<A0A0A0<A0A1<A0A0A0<A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>>>>>>>();
            v2.CreateAllTypesA0A1();
            A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A1<A0A0A0<A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>>>>>>> v3 = new A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A1<A0A0A0<A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>>>>>>>();
            v3.CreateAllTypesA0A0A0();
            System.Console.WriteLine("Test SUCCESS");
            return 100;
        }
    }

    public interface IA1A2<T0>
        where T0 : new()
    {
    }

    public class A0A0A0<T0> : A0A0
        where T0 : new()
    {

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void VerifyA0A0A0NotInlinedGenericStatic<T>()
            where T : new()
        {
            System.Console.WriteLine(typeof(T));
            T0 t1 = new T0();
            T t2 = new T();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void VerifyA0A0A0NotInlinedStatic()
        {
            T0 t0 = new T0();
        }

        public static void VerifyA0A0A0GenericStatic<T>()
            where T : new()
        {
            System.Console.WriteLine(typeof(T));
            T0 t1 = new T0();
            T t2 = new T();
        }

        public static void VerifyA0A0A0Static()
        {
            T0 t0 = new T0();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void VerifyA0A0A0NotInlinedGeneric<T>()
            where T : new()
        {
            System.Console.WriteLine(this);
            System.Console.WriteLine(typeof(T));
            T0 t2 = new T0();
            T t3 = new T();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void VerifyA0A0A0NotInlined()
        {
            System.Console.WriteLine(this);
            T0 t1 = new T0();
        }

        public void RecurseA0A0A0(int depth)
        {
            if ((depth < 0))
            {
                return;
            }
            System.Console.Write(".");
            A0A1<A0A0A0<A0>> next = new A0A1<A0A0A0<A0>>();
            next.RecurseA0A1((depth - 1));
        }

        public void CreateAllTypesA0A0A0()
        {
            A0 v0 = new A0();
            v0.VerifyInterfaceIA1();
            A0 v1 = new A0();
            v1.VerifyInterfaceGenericIA1<A0>();
            A0.VerifyA0NotInlinedGenericStatic<A0A0>();
            A0.VerifyA0NotInlinedStatic();
            A0.VerifyA0GenericStatic<A0A0A0<A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>>>();
            A0.VerifyA0Static();
            A0 v2 = new A0();
            v2.VerifyA0NotInlinedGeneric<A0>();
            A0 v3 = new A0();
            v3.VerifyA0NotInlined();
            A0 v4 = new A0();
            v4.VirtualVerifyGeneric<A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>>();
            A0 v5 = new A0();
            v5.VirtualVerify();
            A0 v6 = new A0();
            v6.DeepRecursion();
            IA1 i7 = ((IA1)(new A0()));
            i7.VerifyInterfaceIA1();
            IA1 i8 = ((IA1)(new A0()));
            i8.VerifyInterfaceGenericIA1<A0A0>();
            A0A0.VerifyA0A0NotInlinedGenericStatic<A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>>();
            A0A0.VerifyA0A0NotInlinedStatic();
            A0A0.VerifyA0A0GenericStatic<A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>>();
            A0A0.VerifyA0A0Static();
            A0A0 v9 = new A0A0();
            v9.VerifyA0A0NotInlinedGeneric<A0>();
            A0A0 v10 = new A0A0();
            v10.VerifyA0A0NotInlined();
            A0A0 v11 = new A0A0();
            v11.VirtualVerifyGeneric<IA1A2<A0A0A0<A0>>>();
            A0A0 v12 = new A0A0();
            v12.VirtualVerify();
            IA1 i13 = ((IA1)(new A0A0()));
            i13.VerifyInterfaceIA1();
            IA1 i14 = ((IA1)(new A0A0()));
            i14.VerifyInterfaceGenericIA1<A0>();
            A0A1<A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>>.VerifyA0A1NotInlinedGenericStatic<A0A1<A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>>>();
            A0A1<A0A0A0<A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>>>.VerifyA0A1NotInlinedStatic();
            A0A1<A0A0>.VerifyA0A1GenericStatic<A0A0A0<A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>>>();
            A0A1<A0>.VerifyA0A1Static();
            A0A1<A0A0A0<A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>>> v15 = new A0A1<A0A0A0<A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>>>();
            v15.VerifyA0A1NotInlinedGeneric<A0>();
            A0A1<A0A0A0<A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>>> v16 = new A0A1<A0A0A0<A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>>>();
            v16.VerifyA0A1NotInlined();
            A0A0A0<A0A0>.VerifyA0A0A0NotInlinedGenericStatic<A0>();
            A0A0A0<A0>.VerifyA0A0A0NotInlinedStatic();
            A0A0A0<A0>.VerifyA0A0A0GenericStatic<A0A0A0<A0>>();
            A0A0A0<A0A1<A0A0A0<A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>>>>.VerifyA0A0A0Static();
            A0A0A0<A0A0A0<A0A1<A0A0A0<A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>>>>> v17 = new A0A0A0<A0A0A0<A0A1<A0A0A0<A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>>>>>();
            v17.VerifyA0A0A0NotInlinedGeneric<A0A1<A0A0A0<A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>>>>();
            A0A0A0<A0A0A0<A0A0A0<A0A1<A0A0A0<A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>>>>>> v18 = new A0A0A0<A0A0A0<A0A0A0<A0A1<A0A0A0<A0A1<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0A0A0<A0>>>>>>>>>>>>();
            v18.VerifyA0A0A0NotInlined();
        }
    }

    public interface IA1
    {

        void VerifyInterfaceIA1();

        void VerifyInterfaceGenericIA1<K>()
            where K : new();
    }

    public class A0 : object, IA1
    {

        public void VerifyInterfaceIA1()
        {
            System.Console.WriteLine(typeof(A0));
        }

        public void VerifyInterfaceGenericIA1<K>()
            where K : new()
        {
            System.Console.WriteLine(typeof(A0));
            K t1 = new K();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void VerifyA0NotInlinedGenericStatic<T>()
            where T : new()
        {
            System.Console.WriteLine(typeof(T));
            System.Console.WriteLine(typeof(A0));
            T t2 = new T();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void VerifyA0NotInlinedStatic()
        {
            System.Console.WriteLine(typeof(A0));
        }

        public static void VerifyA0GenericStatic<T>()
            where T : new()
        {
            System.Console.WriteLine(typeof(T));
            System.Console.WriteLine(typeof(A0));
            T t2 = new T();
        }

        public static void VerifyA0Static()
        {
            System.Console.WriteLine(typeof(A0));
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void VerifyA0NotInlinedGeneric<T>()
            where T : new()
        {
            System.Console.WriteLine(this);
            System.Console.WriteLine(typeof(T));
            System.Console.WriteLine(typeof(A0));
            T t3 = new T();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void VerifyA0NotInlined()
        {
            System.Console.WriteLine(this);
            System.Console.WriteLine(typeof(A0));
        }

        public virtual void VirtualVerifyGeneric<T>()
        {
            System.Console.WriteLine(this);
            System.Console.WriteLine(typeof(T));
            System.Console.WriteLine(typeof(A0));
        }

        public virtual void VirtualVerify()
        {
            System.Console.WriteLine(this);
            System.Console.WriteLine(typeof(A0));
        }

        public void RecurseA0(int depth)
        {
            if ((depth < 0))
            {
                return;
            }
            System.Console.Write(".");
            A0A0A0<A0A0A0<A0>> next = new A0A0A0<A0A0A0<A0>>();
            next.RecurseA0A0A0((depth - 1));
        }

        public void DeepRecursion()
        {
            this.RecurseA0(1000);
            System.Console.WriteLine();
        }

        public void CreateAllTypesA0()
        {
            A0 v0 = new A0();
            v0.VerifyInterfaceIA1();
            A0 v1 = new A0();
            v1.VerifyInterfaceGenericIA1<A0A0A0<A0A0A0<A0>>>();
            A0.VerifyA0NotInlinedGenericStatic<A0A0A0<A0A0A0<A0>>>();
            A0.VerifyA0NotInlinedStatic();
            A0.VerifyA0GenericStatic<A0A1<A0A0A0<A0>>>();
            A0.VerifyA0Static();
            A0 v2 = new A0();
            v2.VerifyA0NotInlinedGeneric<A0A0>();
            A0 v3 = new A0();
            v3.VerifyA0NotInlined();
            A0 v4 = new A0();
            v4.VirtualVerifyGeneric<A0A1<A0A0A0<A0>>>();
            A0 v5 = new A0();
            v5.VirtualVerify();
            A0 v6 = new A0();
            v6.DeepRecursion();
            IA1 i7 = ((IA1)(new A0()));
            i7.VerifyInterfaceIA1();
            IA1 i8 = ((IA1)(new A0()));
            i8.VerifyInterfaceGenericIA1<A0>();
            A0A0.VerifyA0A0NotInlinedGenericStatic<A0A1<A0A0A0<A0>>>();
            A0A0.VerifyA0A0NotInlinedStatic();
            A0A0.VerifyA0A0GenericStatic<A0A1<A0A0A0<A0>>>();
            A0A0.VerifyA0A0Static();
            A0A0 v9 = new A0A0();
            v9.VerifyA0A0NotInlinedGeneric<A0A1<A0A0A0<A0>>>();
            A0A0 v10 = new A0A0();
            v10.VerifyA0A0NotInlined();
            A0A0 v11 = new A0A0();
            v11.VirtualVerifyGeneric<A0A0A0<A0A0A0<A0>>>();
            A0A0 v12 = new A0A0();
            v12.VirtualVerify();
            IA1 i13 = ((IA1)(new A0A0()));
            i13.VerifyInterfaceIA1();
            IA1 i14 = ((IA1)(new A0A0()));
            i14.VerifyInterfaceGenericIA1<A0A0>();
            A0A1<A0A0>.VerifyA0A1NotInlinedGenericStatic<A0A1<A0A0>>();
            A0A1<A0A1<A0A0>>.VerifyA0A1NotInlinedStatic();
            A0A1<A0>.VerifyA0A1GenericStatic<A0A1<A0>>();
            A0A1<A0A1<A0>>.VerifyA0A1Static();
            A0A1<A0A1<A0A1<A0>>> v15 = new A0A1<A0A1<A0A1<A0>>>();
            v15.VerifyA0A1NotInlinedGeneric<A0A1<A0A1<A0A1<A0>>>>();
            A0A1<A0> v16 = new A0A1<A0>();
            v16.VerifyA0A1NotInlined();
            A0A0A0<A0A0A0<A0A0A0<A0>>>.VerifyA0A0A0NotInlinedGenericStatic<A0A0A0<A0A0A0<A0A0A0<A0>>>>();
            A0A0A0<A0>.VerifyA0A0A0NotInlinedStatic();
            A0A0A0<A0A0A0<A0>>.VerifyA0A0A0GenericStatic<A0A0A0<A0A0A0<A0>>>();
            A0A0A0<A0A1<A0>>.VerifyA0A0A0Static();
            A0A0A0<A0A0> v17 = new A0A0A0<A0A0>();
            v17.VerifyA0A0A0NotInlinedGeneric<A0A1<A0>>();
            A0A0A0<A0A0> v18 = new A0A0A0<A0A0>();
            v18.VerifyA0A0A0NotInlined();
        }
    }
}
