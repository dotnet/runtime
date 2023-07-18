// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using Xunit;
/// <summary>Generic chaos types</summary>
/// <remarks>CommandLine:
///<code>GenericChaos.exe /mtc:10 /mtcc:10 /mic:5 /ol:Cs /ol:Vb
///</code>
///Data:
///<code>Help:	False
/// MaxGenerationDepth:	2
/// MaxTypeParameterCount:	1
/// MaxMethodTypeParameterCount:	1
/// MaxTypeCount:	10
/// MaxMethodCallDepth:	1000
/// MaxTypeInheranceCount:	10
/// MaxStaticFieldCount:	2
/// MaxInterfaceCount:	5
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


    public class A0A1<T0> : A0, IA2
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
            v1.VerifyInterfaceGenericIA1<A0A0A0<A0A3>>();
            A0 v2 = new A0();
            v2.VerifyInterfaceIA2();
            A0 v3 = new A0();
            v3.VerifyInterfaceGenericIA2<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>();
            A0.VerifyA0NotInlinedGenericStatic<A0A3A4<A0A1A2<A0A0A0A0<A0A0A1>>>>();
            A0.VerifyA0NotInlinedStatic();
            A0.VerifyA0GenericStatic<A0A0A3<A0A0A1>>();
            A0.VerifyA0Static();
            A0 v4 = new A0();
            v4.VerifyA0NotInlinedGeneric<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>();
            A0 v5 = new A0();
            v5.VerifyA0NotInlined();
            A0 v6 = new A0();
            v6.VirtualVerifyGeneric<IA1>();
            A0 v7 = new A0();
            v7.VirtualVerify();
            A0 v8 = new A0();
            v8.DeepRecursion();
            IA1 i9 = ((IA1)(new A0()));
            i9.VerifyInterfaceIA1();
            IA1 i10 = ((IA1)(new A0()));
            i10.VerifyInterfaceGenericIA1<A0A0>();
            IA2 i11 = ((IA2)(new A0()));
            i11.VerifyInterfaceIA2();
            IA2 i12 = ((IA2)(new A0()));
            i12.VerifyInterfaceGenericIA2<A0A0A3<A0A0A1>>();
            A0A0.VerifyA0A0NotInlinedGenericStatic<A0A1A2<A0A0A0A0<A0A0A1>>>();
            A0A0.VerifyA0A0NotInlinedStatic();
            A0A0.VerifyA0A0GenericStatic<A0A0A3<A0A0A1>>();
            A0A0.VerifyA0A0Static();
            A0A0 v13 = new A0A0();
            v13.VerifyA0A0NotInlinedGeneric<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>();
            A0A0 v14 = new A0A0();
            v14.VerifyA0A0NotInlined();
            A0A0 v15 = new A0A0();
            v15.VirtualVerifyGeneric<IA2>();
            A0A0 v16 = new A0A0();
            v16.VirtualVerify();
            IA1 i17 = ((IA1)(new A0A0()));
            i17.VerifyInterfaceIA1();
            IA1 i18 = ((IA1)(new A0A0()));
            i18.VerifyInterfaceGenericIA1<A0A0A3<A0A0A1>>();
            IA2 i19 = ((IA2)(new A0A0()));
            i19.VerifyInterfaceIA2();
            IA2 i20 = ((IA2)(new A0A0()));
            i20.VerifyInterfaceGenericIA2<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>();
            A0A1<A0A0A1>.VerifyA0A1NotInlinedGenericStatic<A0A3A4<A0A1A2<A0A0A0A0<A0A0A1>>>>();
            A0A1<A0A1A2<A0A0A0A0<A0A0A1>>>.VerifyA0A1NotInlinedStatic();
            A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>.VerifyA0A1GenericStatic<A0A0>();
            A0A1<A0A1A2<A0A0A0A0<A0A0A1>>>.VerifyA0A1Static();
            A0A1<A0> v21 = new A0A1<A0>();
            v21.VerifyA0A1NotInlinedGeneric<A0>();
            A0A1<A0A0A3<A0A0A1>> v22 = new A0A1<A0A0A3<A0A0A1>>();
            v22.VerifyA0A1NotInlined();
            IA2 i23 = ((IA2)(new A0A1<A0A4<A0A0A1>>()));
            i23.VerifyInterfaceIA2();
            IA2 i24 = ((IA2)(new A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>()));
            i24.VerifyInterfaceGenericIA2<A0A0A0<A0A3>>();
            A0A0A0<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>.VerifyA0A0A0NotInlinedGenericStatic<A0>();
            A0A0A0<A0A4<A0A0A1>>.VerifyA0A0A0NotInlinedStatic();
            A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>.VerifyA0A0A0GenericStatic<A0A3>();
            A0A0A0<A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>.VerifyA0A0A0Static();
            A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>> v25 = new A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>();
            v25.VerifyA0A0A0NotInlinedGeneric<A0A3A4<A0A1A2<A0A0A0A0<A0A0A1>>>>();
            A0A0A0<A0A3> v26 = new A0A0A0<A0A3>();
            v26.VerifyA0A0A0NotInlined();
            IA2 i27 = ((IA2)(new A0A0A0<A0A0>()));
            i27.VerifyInterfaceIA2();
            IA2 i28 = ((IA2)(new A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>()));
            i28.VerifyInterfaceGenericIA2<A0A3A4<A0A1A2<A0A0A0A0<A0A0A1>>>>();
            A0A3.VerifyA0A3NotInlinedGenericStatic<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>();
            A0A3.VerifyA0A3NotInlinedStatic();
            A0A3.VerifyA0A3GenericStatic<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>();
            A0A3.VerifyA0A3Static();
            A0A3 v29 = new A0A3();
            v29.VerifyA0A3NotInlinedGeneric<A0A4<A0A0A1>>();
            A0A3 v30 = new A0A3();
            v30.VerifyA0A3NotInlined();
            IA2 i31 = ((IA2)(new A0A3()));
            i31.VerifyInterfaceIA2();
            IA2 i32 = ((IA2)(new A0A3()));
            i32.VerifyInterfaceGenericIA2<A0A0A1>();
            A0A0A1.VerifyA0A0A1NotInlinedGenericStatic<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>();
            A0A0A1.VerifyA0A0A1NotInlinedStatic();
            A0A0A1.VerifyA0A0A1GenericStatic<A0A0A3<A0A0A1>>();
            A0A0A1.VerifyA0A0A1Static();
            A0A0A1 v33 = new A0A0A1();
            v33.VerifyA0A0A1NotInlinedGeneric<A0A1A2<A0A0A0A0<A0A0A1>>>();
            A0A0A1 v34 = new A0A0A1();
            v34.VerifyA0A0A1NotInlined();
            IA2 i35 = ((IA2)(new A0A0A1()));
            i35.VerifyInterfaceIA2();
            IA2 i36 = ((IA2)(new A0A0A1()));
            i36.VerifyInterfaceGenericIA2<A0A3A4<A0A1A2<A0A0A0A0<A0A0A1>>>>();
            A0A1A2<A0A0A1>.VerifyA0A1A2NotInlinedGenericStatic<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>();
            A0A1A2<A0A0A1A1<A0A0A1>>.VerifyA0A1A2NotInlinedStatic();
            A0A1A2<A0>.VerifyA0A1A2GenericStatic<A0A3A4<A0A1A2<A0A0A0A0<A0A0A1>>>>();
            A0A1A2<A0A0A1A1<A0A0A1>>.VerifyA0A1A2Static();
            A0A1A2<A0A0A3<A0A0A1>> v37 = new A0A1A2<A0A0A3<A0A0A1>>();
            v37.VerifyA0A1A2NotInlinedGeneric<A0A3A4<A0A1A2<A0A0A0A0<A0A0A1>>>>();
            A0A1A2<A0A0A1> v38 = new A0A1A2<A0A0A1>();
            v38.VerifyA0A1A2NotInlined();
            IA2 i39 = ((IA2)(new A0A1A2<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>()));
            i39.VerifyInterfaceIA2();
            IA2 i40 = ((IA2)(new A0A1A2<A0A0A3<A0A0A1>>()));
            i40.VerifyInterfaceGenericIA2<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>();
            A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>.VerifyA0A0A0A0NotInlinedGenericStatic<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>();
            A0A0A0A0<A0A1A2<A0A0A3<A0A0A1>>>.VerifyA0A0A0A0NotInlinedStatic();
            A0A0A0A0<A0>.VerifyA0A0A0A0GenericStatic<A0A0>();
            A0A0A0A0<A0A3>.VerifyA0A0A0A0Static();
            A0A0A0A0<A0A3> v41 = new A0A0A0A0<A0A3>();
            v41.VerifyA0A0A0A0NotInlinedGeneric<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>();
            A0A0A0A0<A0> v42 = new A0A0A0A0<A0>();
            v42.VerifyA0A0A0A0NotInlined();
            IA2 i43 = ((IA2)(new A0A0A0A0<A0A0A1A1<A0A0A1>>()));
            i43.VerifyInterfaceIA2();
            IA2 i44 = ((IA2)(new A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>()));
            i44.VerifyInterfaceGenericIA2<A0A3>();
            A0A4<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>.VerifyA0A4NotInlinedGenericStatic<A0A4<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>>();
            A0A4<A0>.VerifyA0A4NotInlinedStatic();
            A0A4<A0A3A4<A0A1A2<A0A0A0A0<A0A0A1>>>>.VerifyA0A4GenericStatic<A0A0A3<A0A0A1>>();
            A0A4<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>.VerifyA0A4Static();
            A0A4<A0A3> v45 = new A0A4<A0A3>();
            v45.VerifyA0A4NotInlinedGeneric<A0>();
            A0A4<A0A0> v46 = new A0A4<A0A0>();
            v46.VerifyA0A4NotInlined();
            A0A0A3<A0A4<A0A0>>.VerifyA0A0A3NotInlinedGenericStatic<A0A3A4<A0A1A2<A0A0A0A0<A0A0A1>>>>();
            A0A0A3<A0A3>.VerifyA0A0A3NotInlinedStatic();
            A0A0A3<A0>.VerifyA0A0A3GenericStatic<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>();
            A0A0A3<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>.VerifyA0A0A3Static();
            A0A0A3<A0A0A1> v47 = new A0A0A3<A0A0A1>();
            v47.VerifyA0A0A3NotInlinedGeneric<A0A0A1>();
            A0A0A3<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>> v48 = new A0A0A3<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>();
            v48.VerifyA0A0A3NotInlined();
            A0A3A4<A0A0A1>.VerifyA0A3A4NotInlinedGenericStatic<A0A0>();
            A0A3A4<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>.VerifyA0A3A4NotInlinedStatic();
            A0A3A4<A0A3>.VerifyA0A3A4GenericStatic<A0A4<A0A0>>();
            A0A3A4<A0A3>.VerifyA0A3A4Static();
            A0A3A4<A0A4<A0A0>> v49 = new A0A3A4<A0A4<A0A0>>();
            v49.VerifyA0A3A4NotInlinedGeneric<A0A4<A0A0>>();
            A0A3A4<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>> v50 = new A0A3A4<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>();
            v50.VerifyA0A3A4NotInlined();
            A0A0A1A1<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>.VerifyA0A0A1A1NotInlinedGenericStatic<A0A3>();
            A0A0A1A1<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>.VerifyA0A0A1A1NotInlinedStatic();
            A0A0A1A1<A0A0A3<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>>.VerifyA0A0A1A1GenericStatic<A0A1A2<A0A0A3<A0A0A1>>>();
            A0A0A1A1<A0>.VerifyA0A0A1A1Static();
            A0A0A1A1<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>> v51 = new A0A0A1A1<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>();
            v51.VerifyA0A0A1A1NotInlinedGeneric<A0A4<A0A0>>();
            A0A0A1A1<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>> v52 = new A0A0A1A1<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>();
            v52.VerifyA0A0A1A1NotInlined();
        }
    }

    public class A0 : object, IA1, IA2
    {

        private static A0A0A3<A0A0A1> _sfA00;

        private A0A0 _fA01;

        public void VerifyInterfaceIA1()
        {
            System.Console.WriteLine(typeof(A0));
            A0._sfA00 = new A0A0A3<A0A0A1>();
            this._fA01 = new A0A0();
        }

        public void VerifyInterfaceGenericIA1<K>()
            where K : new()
        {
            System.Console.WriteLine(typeof(A0));
            K t1 = new K();
            A0._sfA00 = new A0A0A3<A0A0A1>();
            this._fA01 = new A0A0();
        }

        public void VerifyInterfaceIA2()
        {
            System.Console.WriteLine(typeof(A0));
            A0._sfA00 = new A0A0A3<A0A0A1>();
            this._fA01 = new A0A0();
        }

        public void VerifyInterfaceGenericIA2<K>()
            where K : new()
        {
            System.Console.WriteLine(typeof(A0));
            K t1 = new K();
            A0._sfA00 = new A0A0A3<A0A0A1>();
            this._fA01 = new A0A0();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void VerifyA0NotInlinedGenericStatic<T>()
            where T : new()
        {
            System.Console.WriteLine(typeof(T));
            System.Console.WriteLine(typeof(A0));
            T t2 = new T();
            A0._sfA00 = new A0A0A3<A0A0A1>();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void VerifyA0NotInlinedStatic()
        {
            System.Console.WriteLine(typeof(A0));
            A0._sfA00 = new A0A0A3<A0A0A1>();
        }

        public static void VerifyA0GenericStatic<T>()
            where T : new()
        {
            System.Console.WriteLine(typeof(T));
            System.Console.WriteLine(typeof(A0));
            T t2 = new T();
            A0._sfA00 = new A0A0A3<A0A0A1>();
        }

        public static void VerifyA0Static()
        {
            System.Console.WriteLine(typeof(A0));
            A0._sfA00 = new A0A0A3<A0A0A1>();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void VerifyA0NotInlinedGeneric<T>()
            where T : new()
        {
            System.Console.WriteLine(this);
            System.Console.WriteLine(typeof(T));
            System.Console.WriteLine(typeof(A0));
            T t3 = new T();
            A0._sfA00 = new A0A0A3<A0A0A1>();
            this._fA01 = new A0A0();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void VerifyA0NotInlined()
        {
            System.Console.WriteLine(this);
            System.Console.WriteLine(typeof(A0));
            A0._sfA00 = new A0A0A3<A0A0A1>();
            this._fA01 = new A0A0();
        }

        public virtual void VirtualVerifyGeneric<T>()
        {
            System.Console.WriteLine(this);
            System.Console.WriteLine(typeof(T));
            System.Console.WriteLine(typeof(A0));
            A0._sfA00 = new A0A0A3<A0A0A1>();
            this._fA01 = new A0A0();
        }

        public virtual void VirtualVerify()
        {
            System.Console.WriteLine(this);
            System.Console.WriteLine(typeof(A0));
            A0._sfA00 = new A0A0A3<A0A0A1>();
            this._fA01 = new A0A0();
        }

        public void RecurseA0(int depth)
        {
            if ((depth < 0))
            {
                return;
            }
            System.Console.Write(".");
            A0A0A1A1<A0A1<A0A0A0<A0>>> next = new A0A0A1A1<A0A1<A0A0A0<A0>>>();
            next.RecurseA0A0A1A1((depth - 1));
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
            v1.VerifyInterfaceGenericIA1<A0A3>();
            A0 v2 = new A0();
            v2.VerifyInterfaceIA2();
            A0 v3 = new A0();
            v3.VerifyInterfaceGenericIA2<A0A3A4<A0A0A3<A0A0A1>>>();
            A0.VerifyA0NotInlinedGenericStatic<A0A1<A0A0A0<A0>>>();
            A0.VerifyA0NotInlinedStatic();
            A0.VerifyA0GenericStatic<A0A0A0A0<A0>>();
            A0.VerifyA0Static();
            A0 v4 = new A0();
            v4.VerifyA0NotInlinedGeneric<A0A0A1>();
            A0 v5 = new A0();
            v5.VerifyA0NotInlined();
            A0 v6 = new A0();
            v6.VirtualVerifyGeneric<A0A0A1A1<A0A1<A0A0A0<A0>>>>();
            A0 v7 = new A0();
            v7.VirtualVerify();
            A0 v8 = new A0();
            v8.DeepRecursion();
            IA1 i9 = ((IA1)(new A0()));
            i9.VerifyInterfaceIA1();
            IA1 i10 = ((IA1)(new A0()));
            i10.VerifyInterfaceGenericIA1<A0A0A1A1<A0A1<A0A0A0<A0>>>>();
            IA2 i11 = ((IA2)(new A0()));
            i11.VerifyInterfaceIA2();
            IA2 i12 = ((IA2)(new A0()));
            i12.VerifyInterfaceGenericIA2<A0A1<A0A0A0<A0>>>();
            A0A0.VerifyA0A0NotInlinedGenericStatic<A0A0A1>();
            A0A0.VerifyA0A0NotInlinedStatic();
            A0A0.VerifyA0A0GenericStatic<A0A0A0A0<A0>>();
            A0A0.VerifyA0A0Static();
            A0A0 v13 = new A0A0();
            v13.VerifyA0A0NotInlinedGeneric<A0A1A2<A0A0A1>>();
            A0A0 v14 = new A0A0();
            v14.VerifyA0A0NotInlined();
            A0A0 v15 = new A0A0();
            v15.VirtualVerifyGeneric<A0A1A2<A0A0A1>>();
            A0A0 v16 = new A0A0();
            v16.VirtualVerify();
            IA1 i17 = ((IA1)(new A0A0()));
            i17.VerifyInterfaceIA1();
            IA1 i18 = ((IA1)(new A0A0()));
            i18.VerifyInterfaceGenericIA1<A0A0A1>();
            IA2 i19 = ((IA2)(new A0A0()));
            i19.VerifyInterfaceIA2();
            IA2 i20 = ((IA2)(new A0A0()));
            i20.VerifyInterfaceGenericIA2<A0A4<A0A0A0<A0A1<A0A0A0<A0>>>>>();
            A0A1<A0A1A2<A0A0A1>>.VerifyA0A1NotInlinedGenericStatic<A0A0A1A1<A0A1<A0A0A0<A0>>>>();
            A0A1<A0>.VerifyA0A1NotInlinedStatic();
            A0A1<A0A0A1A1<A0A1<A0A0A0<A0>>>>.VerifyA0A1GenericStatic<A0A0A3<A0A0A1>>();
            A0A1<A0A0A0<A0A1<A0A0A0<A0>>>>.VerifyA0A1Static();
            A0A1<A0A3> v21 = new A0A1<A0A3>();
            v21.VerifyA0A1NotInlinedGeneric<A0>();
            A0A1<A0> v22 = new A0A1<A0>();
            v22.VerifyA0A1NotInlined();
            IA2 i23 = ((IA2)(new A0A1<A0A0A0A0<A0>>()));
            i23.VerifyInterfaceIA2();
            IA2 i24 = ((IA2)(new A0A1<A0A3A4<A0A0A3<A0A0A1>>>()));
            i24.VerifyInterfaceGenericIA2<A0A0A0<A0A1<A0A0A0<A0>>>>();
            A0A0A0<A0>.VerifyA0A0A0NotInlinedGenericStatic<A0A3A4<A0A0A3<A0A0A1>>>();
            A0A0A0<A0A0>.VerifyA0A0A0NotInlinedStatic();
            A0A0A0<A0A0A3<A0A0A1>>.VerifyA0A0A0GenericStatic<A0A0A0<A0A0A3<A0A0A1>>>();
            A0A0A0<A0A3A4<A0A0A3<A0A0A1>>>.VerifyA0A0A0Static();
            A0A0A0<A0A0A0A0<A0>> v25 = new A0A0A0<A0A0A0A0<A0>>();
            v25.VerifyA0A0A0NotInlinedGeneric<A0A4<A0A0A0<A0A1<A0A0A0<A0>>>>>();
            A0A0A0<A0A1<A0A3A4<A0A0A3<A0A0A1>>>> v26 = new A0A0A0<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>();
            v26.VerifyA0A0A0NotInlined();
            IA2 i27 = ((IA2)(new A0A0A0<A0A0A1>()));
            i27.VerifyInterfaceIA2();
            IA2 i28 = ((IA2)(new A0A0A0<A0A3A4<A0A0A3<A0A0A1>>>()));
            i28.VerifyInterfaceGenericIA2<A0A0A0<A0A3A4<A0A0A3<A0A0A1>>>>();
            A0A3.VerifyA0A3NotInlinedGenericStatic<A0>();
            A0A3.VerifyA0A3NotInlinedStatic();
            A0A3.VerifyA0A3GenericStatic<A0A1A2<A0A0A1>>();
            A0A3.VerifyA0A3Static();
            A0A3 v29 = new A0A3();
            v29.VerifyA0A3NotInlinedGeneric<A0A0A0A0<A0>>();
            A0A3 v30 = new A0A3();
            v30.VerifyA0A3NotInlined();
            IA2 i31 = ((IA2)(new A0A3()));
            i31.VerifyInterfaceIA2();
            IA2 i32 = ((IA2)(new A0A3()));
            i32.VerifyInterfaceGenericIA2<A0A0A3<A0A0A1>>();
            A0A0A1.VerifyA0A0A1NotInlinedGenericStatic<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>();
            A0A0A1.VerifyA0A0A1NotInlinedStatic();
            A0A0A1.VerifyA0A0A1GenericStatic<A0A0A1>();
            A0A0A1.VerifyA0A0A1Static();
            A0A0A1 v33 = new A0A0A1();
            v33.VerifyA0A0A1NotInlinedGeneric<A0A3>();
            A0A0A1 v34 = new A0A0A1();
            v34.VerifyA0A0A1NotInlined();
            IA2 i35 = ((IA2)(new A0A0A1()));
            i35.VerifyInterfaceIA2();
            IA2 i36 = ((IA2)(new A0A0A1()));
            i36.VerifyInterfaceGenericIA2<A0A1A2<A0A0A1>>();
            A0A1A2<A0A0A1>.VerifyA0A1A2NotInlinedGenericStatic<A0A0A0A0<A0>>();
            A0A1A2<A0A0A0A0<A0>>.VerifyA0A1A2NotInlinedStatic();
            A0A1A2<A0A1A2<A0A0A0A0<A0>>>.VerifyA0A1A2GenericStatic<A0A0A1A1<A0A1<A0A0A0<A0>>>>();
            A0A1A2<A0A3>.VerifyA0A1A2Static();
            A0A1A2<A0A0A0A0<A0>> v37 = new A0A1A2<A0A0A0A0<A0>>();
            v37.VerifyA0A1A2NotInlinedGeneric<A0A0A1>();
            A0A1A2<A0A0A3<A0A0A1>> v38 = new A0A1A2<A0A0A3<A0A0A1>>();
            v38.VerifyA0A1A2NotInlined();
            IA2 i39 = ((IA2)(new A0A1A2<A0A0A0<A0A3A4<A0A0A3<A0A0A1>>>>()));
            i39.VerifyInterfaceIA2();
            IA2 i40 = ((IA2)(new A0A1A2<A0A3A4<A0A0A3<A0A0A1>>>()));
            i40.VerifyInterfaceGenericIA2<A0A0A0A0<A0>>();
            A0A0A0A0<A0A0A3<A0A0A1>>.VerifyA0A0A0A0NotInlinedGenericStatic<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>();
            A0A0A0A0<A0A3A4<A0A0A3<A0A0A1>>>.VerifyA0A0A0A0NotInlinedStatic();
            A0A0A0A0<A0>.VerifyA0A0A0A0GenericStatic<A0>();
            A0A0A0A0<A0A4<A0A0A0<A0A1<A0A0A0<A0>>>>>.VerifyA0A0A0A0Static();
            A0A0A0A0<A0A3> v41 = new A0A0A0A0<A0A3>();
            v41.VerifyA0A0A0A0NotInlinedGeneric<A0A0A0A0<A0A3>>();
            A0A0A0A0<A0A0A1A1<A0A1<A0A0A0<A0>>>> v42 = new A0A0A0A0<A0A0A1A1<A0A1<A0A0A0<A0>>>>();
            v42.VerifyA0A0A0A0NotInlined();
            IA2 i43 = ((IA2)(new A0A0A0A0<A0A0A3<A0A0A1>>()));
            i43.VerifyInterfaceIA2();
            IA2 i44 = ((IA2)(new A0A0A0A0<A0A0A1>()));
            i44.VerifyInterfaceGenericIA2<A0A0>();
            A0A4<A0>.VerifyA0A4NotInlinedGenericStatic<A0A0A3<A0A0A1>>();
            A0A4<A0A0A1A1<A0A1<A0A0A0<A0>>>>.VerifyA0A4NotInlinedStatic();
            A0A4<A0A0A0<A0A3A4<A0A0A3<A0A0A1>>>>.VerifyA0A4GenericStatic<A0A4<A0A0A0<A0A3A4<A0A0A3<A0A0A1>>>>>();
            A0A4<A0A1A2<A0A3A4<A0A0A3<A0A0A1>>>>.VerifyA0A4Static();
            A0A4<A0A3A4<A0A0A3<A0A0A1>>> v45 = new A0A4<A0A3A4<A0A0A3<A0A0A1>>>();
            v45.VerifyA0A4NotInlinedGeneric<A0A0A0A0<A0A0A1>>();
            A0A4<A0A1<A0A3A4<A0A0A3<A0A0A1>>>> v46 = new A0A4<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>();
            v46.VerifyA0A4NotInlined();
            A0A0A3<A0A3>.VerifyA0A0A3NotInlinedGenericStatic<A0A0A3<A0A3>>();
            A0A0A3<A0A3>.VerifyA0A0A3NotInlinedStatic();
            A0A0A3<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>.VerifyA0A0A3GenericStatic<A0A0A0A0<A0A0A1>>();
            A0A0A3<A0A0A3<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>.VerifyA0A0A3Static();
            A0A0A3<A0A3A4<A0A0A3<A0A0A1>>> v47 = new A0A0A3<A0A3A4<A0A0A3<A0A0A1>>>();
            v47.VerifyA0A0A3NotInlinedGeneric<A0A0A0<A0A3A4<A0A0A3<A0A0A1>>>>();
            A0A0A3<A0A0> v48 = new A0A0A3<A0A0>();
            v48.VerifyA0A0A3NotInlined();
            A0A3A4<A0A3A4<A0A0A3<A0A0A1>>>.VerifyA0A3A4NotInlinedGenericStatic<A0A3A4<A0A3A4<A0A0A3<A0A0A1>>>>();
            A0A3A4<A0A1A2<A0A3A4<A0A0A3<A0A0A1>>>>.VerifyA0A3A4NotInlinedStatic();
            A0A3A4<A0A0A0<A0A3A4<A0A0A3<A0A0A1>>>>.VerifyA0A3A4GenericStatic<A0A1A2<A0A3A4<A0A0A3<A0A0A1>>>>();
            A0A3A4<A0A3A4<A0A0A0<A0A3A4<A0A0A3<A0A0A1>>>>>.VerifyA0A3A4Static();
            A0A3A4<A0A0A0A0<A0A0A1>> v49 = new A0A3A4<A0A0A0A0<A0A0A1>>();
            v49.VerifyA0A3A4NotInlinedGeneric<A0A1A2<A0A3A4<A0A0A3<A0A0A1>>>>();
            A0A3A4<A0A4<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>> v50 = new A0A3A4<A0A4<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>();
            v50.VerifyA0A3A4NotInlined();
            A0A0A1A1<A0A1A2<A0A3A4<A0A0A3<A0A0A1>>>>.VerifyA0A0A1A1NotInlinedGenericStatic<A0>();
            A0A0A1A1<A0A4<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>.VerifyA0A0A1A1NotInlinedStatic();
            A0A0A1A1<A0A0A0A0<A0A0A1>>.VerifyA0A0A1A1GenericStatic<A0A4<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>();
            A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>.VerifyA0A0A1A1Static();
            A0A0A1A1<A0A1A2<A0A3A4<A0A0A3<A0A0A1>>>> v51 = new A0A0A1A1<A0A1A2<A0A3A4<A0A0A3<A0A0A1>>>>();
            v51.VerifyA0A0A1A1NotInlinedGeneric<A0A0A1>();
            A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>> v52 = new A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>();
            v52.VerifyA0A0A1A1NotInlined();
        }
    }

    public class A0A3A4<T0> : A0A3
        where T0 : new()
    {

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void VerifyA0A3A4NotInlinedGenericStatic<T>()
            where T : new()
        {
            System.Console.WriteLine(typeof(T));
            T0 t1 = new T0();
            T t2 = new T();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void VerifyA0A3A4NotInlinedStatic()
        {
            T0 t0 = new T0();
        }

        public static void VerifyA0A3A4GenericStatic<T>()
            where T : new()
        {
            System.Console.WriteLine(typeof(T));
            T0 t1 = new T0();
            T t2 = new T();
        }

        public static void VerifyA0A3A4Static()
        {
            T0 t0 = new T0();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void VerifyA0A3A4NotInlinedGeneric<T>()
            where T : new()
        {
            System.Console.WriteLine(this);
            System.Console.WriteLine(typeof(T));
            T0 t2 = new T0();
            T t3 = new T();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void VerifyA0A3A4NotInlined()
        {
            System.Console.WriteLine(this);
            T0 t1 = new T0();
        }

        public void RecurseA0A3A4(int depth)
        {
            if ((depth < 0))
            {
                return;
            }
            System.Console.Write(".");
            A0A0A3<A0A0A1> next = new A0A0A3<A0A0A1>();
            next.RecurseA0A0A3((depth - 1));
        }

        public void CreateAllTypesA0A3A4()
        {
            A0 v0 = new A0();
            v0.VerifyInterfaceIA1();
            A0 v1 = new A0();
            v1.VerifyInterfaceGenericIA1<A0A0A0<A0A1A2<A0A0>>>();
            A0 v2 = new A0();
            v2.VerifyInterfaceIA2();
            A0 v3 = new A0();
            v3.VerifyInterfaceGenericIA2<A0A0A0A0<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>();
            A0.VerifyA0NotInlinedGenericStatic<A0A0A1A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>>();
            A0.VerifyA0NotInlinedStatic();
            A0.VerifyA0GenericStatic<A0A0A1A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>>();
            A0.VerifyA0Static();
            A0 v4 = new A0();
            v4.VerifyA0NotInlinedGeneric<A0A3A4<A0>>();
            A0 v5 = new A0();
            v5.VerifyA0NotInlined();
            A0 v6 = new A0();
            v6.VirtualVerifyGeneric<IA2>();
            A0 v7 = new A0();
            v7.VirtualVerify();
            A0 v8 = new A0();
            v8.DeepRecursion();
            IA1 i9 = ((IA1)(new A0()));
            i9.VerifyInterfaceIA1();
            IA1 i10 = ((IA1)(new A0()));
            i10.VerifyInterfaceGenericIA1<A0A3A4<A0>>();
            IA2 i11 = ((IA2)(new A0()));
            i11.VerifyInterfaceIA2();
            IA2 i12 = ((IA2)(new A0()));
            i12.VerifyInterfaceGenericIA2<A0A0A1>();
            A0A0.VerifyA0A0NotInlinedGenericStatic<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>();
            A0A0.VerifyA0A0NotInlinedStatic();
            A0A0.VerifyA0A0GenericStatic<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>();
            A0A0.VerifyA0A0Static();
            A0A0 v13 = new A0A0();
            v13.VerifyA0A0NotInlinedGeneric<A0A0A1A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>>();
            A0A0 v14 = new A0A0();
            v14.VerifyA0A0NotInlined();
            A0A0 v15 = new A0A0();
            v15.VirtualVerifyGeneric<IA2A6<A0A0A0A0<A0A0A1>>>();
            A0A0 v16 = new A0A0();
            v16.VirtualVerify();
            IA1 i17 = ((IA1)(new A0A0()));
            i17.VerifyInterfaceIA1();
            IA1 i18 = ((IA1)(new A0A0()));
            i18.VerifyInterfaceGenericIA1<A0>();
            IA2 i19 = ((IA2)(new A0A0()));
            i19.VerifyInterfaceIA2();
            IA2 i20 = ((IA2)(new A0A0()));
            i20.VerifyInterfaceGenericIA2<A0A3>();
            A0A1<A0A0A1A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>>.VerifyA0A1NotInlinedGenericStatic<A0A0A3<A0A0A0<A0A1A2<A0A0>>>>();
            A0A1<A0A1A2<A0A0A0<A0A1A2<A0A0>>>>.VerifyA0A1NotInlinedStatic();
            A0A1<A0A1A2<A0A0A0<A0A1A2<A0A0>>>>.VerifyA0A1GenericStatic<A0A0A3<A0A0A0<A0A1A2<A0A0>>>>();
            A0A1<A0A0A1>.VerifyA0A1Static();
            A0A1<A0A0A3<A0A0A0<A0A1A2<A0A0>>>> v21 = new A0A1<A0A0A3<A0A0A0<A0A1A2<A0A0>>>>();
            v21.VerifyA0A1NotInlinedGeneric<A0A0A0A0<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>();
            A0A1<A0A0A0<A0A1A2<A0A0>>> v22 = new A0A1<A0A0A0<A0A1A2<A0A0>>>();
            v22.VerifyA0A1NotInlined();
            IA2 i23 = ((IA2)(new A0A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>()));
            i23.VerifyInterfaceIA2();
            IA2 i24 = ((IA2)(new A0A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>()));
            i24.VerifyInterfaceGenericIA2<A0A3A4<A0>>();
            A0A0A0<A0A3>.VerifyA0A0A0NotInlinedGenericStatic<A0A0A0<A0A3>>();
            A0A0A0<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>.VerifyA0A0A0NotInlinedStatic();
            A0A0A0<A0A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>>.VerifyA0A0A0GenericStatic<A0A0A0A0<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>();
            A0A0A0<A0A0>.VerifyA0A0A0Static();
            A0A0A0<A0A1A2<A0A0A0<A0A1A2<A0A0>>>> v25 = new A0A0A0<A0A1A2<A0A0A0<A0A1A2<A0A0>>>>();
            v25.VerifyA0A0A0NotInlinedGeneric<A0A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>>();
            A0A0A0<A0A0A0<A0A1A2<A0A0A0<A0A1A2<A0A0>>>>> v26 = new A0A0A0<A0A0A0<A0A1A2<A0A0A0<A0A1A2<A0A0>>>>>();
            v26.VerifyA0A0A0NotInlined();
            IA2 i27 = ((IA2)(new A0A0A0<A0A0A0A0<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>()));
            i27.VerifyInterfaceIA2();
            IA2 i28 = ((IA2)(new A0A0A0<A0A3>()));
            i28.VerifyInterfaceGenericIA2<A0A3>();
            A0A3.VerifyA0A3NotInlinedGenericStatic<A0A0A1>();
            A0A3.VerifyA0A3NotInlinedStatic();
            A0A3.VerifyA0A3GenericStatic<A0A1A2<A0A0A0<A0A1A2<A0A0>>>>();
            A0A3.VerifyA0A3Static();
            A0A3 v29 = new A0A3();
            v29.VerifyA0A3NotInlinedGeneric<A0A3A4<A0>>();
            A0A3 v30 = new A0A3();
            v30.VerifyA0A3NotInlined();
            IA2 i31 = ((IA2)(new A0A3()));
            i31.VerifyInterfaceIA2();
            IA2 i32 = ((IA2)(new A0A3()));
            i32.VerifyInterfaceGenericIA2<A0A3A4<A0>>();
            A0A0A1.VerifyA0A0A1NotInlinedGenericStatic<A0A0A0<A0A3>>();
            A0A0A1.VerifyA0A0A1NotInlinedStatic();
            A0A0A1.VerifyA0A0A1GenericStatic<A0>();
            A0A0A1.VerifyA0A0A1Static();
            A0A0A1 v33 = new A0A0A1();
            v33.VerifyA0A0A1NotInlinedGeneric<A0A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>>();
            A0A0A1 v34 = new A0A0A1();
            v34.VerifyA0A0A1NotInlined();
            IA2 i35 = ((IA2)(new A0A0A1()));
            i35.VerifyInterfaceIA2();
            IA2 i36 = ((IA2)(new A0A0A1()));
            i36.VerifyInterfaceGenericIA2<A0>();
            A0A1A2<A0A0>.VerifyA0A1A2NotInlinedGenericStatic<A0>();
            A0A1A2<A0A1A2<A0A0>>.VerifyA0A1A2NotInlinedStatic();
            A0A1A2<A0A0A1>.VerifyA0A1A2GenericStatic<A0A0A0A0<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>();
            A0A1A2<A0A0A1>.VerifyA0A1A2Static();
            A0A1A2<A0A0A0A0<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>> v37 = new A0A1A2<A0A0A0A0<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>();
            v37.VerifyA0A1A2NotInlinedGeneric<A0A0A3<A0A0A0<A0A1A2<A0A0>>>>();
            A0A1A2<A0A0A1A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>> v38 = new A0A1A2<A0A0A1A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>>();
            v38.VerifyA0A1A2NotInlined();
            IA2 i39 = ((IA2)(new A0A1A2<A0A0A0A0<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>()));
            i39.VerifyInterfaceIA2();
            IA2 i40 = ((IA2)(new A0A1A2<A0A3>()));
            i40.VerifyInterfaceGenericIA2<A0A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>>();
            A0A0A0A0<A0A0A0<A0A3>>.VerifyA0A0A0A0NotInlinedGenericStatic<A0A0A1A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>>();
            A0A0A0A0<A0A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>>.VerifyA0A0A0A0NotInlinedStatic();
            A0A0A0A0<A0A0A0A0<A0A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>>>.VerifyA0A0A0A0GenericStatic<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>();
            A0A0A0A0<A0A1A2<A0A3>>.VerifyA0A0A0A0Static();
            A0A0A0A0<A0> v41 = new A0A0A0A0<A0>();
            v41.VerifyA0A0A0A0NotInlinedGeneric<A0A3A4<A0>>();
            A0A0A0A0<A0A0A1> v42 = new A0A0A0A0<A0A0A1>();
            v42.VerifyA0A0A0A0NotInlined();
            IA2 i43 = ((IA2)(new A0A0A0A0<A0A0A3<A0A0A0<A0A1A2<A0A0>>>>()));
            i43.VerifyInterfaceIA2();
            IA2 i44 = ((IA2)(new A0A0A0A0<A0A0>()));
            i44.VerifyInterfaceGenericIA2<A0A0A0<A0A3>>();
            A0A4<A0A0A1>.VerifyA0A4NotInlinedGenericStatic<A0A0>();
            A0A4<A0>.VerifyA0A4NotInlinedStatic();
            A0A4<A0A0A0<A0A3>>.VerifyA0A4GenericStatic<A0A0A0A0<A0A0>>();
            A0A4<A0A3A4<A0>>.VerifyA0A4Static();
            A0A4<A0A0A0<A0A3>> v45 = new A0A4<A0A0A0<A0A3>>();
            v45.VerifyA0A4NotInlinedGeneric<A0A0A0<A0A3>>();
            A0A4<A0A0A1> v46 = new A0A4<A0A0A1>();
            v46.VerifyA0A4NotInlined();
            A0A0A3<A0A0A0A0<A0A0>>.VerifyA0A0A3NotInlinedGenericStatic<A0A0A3<A0A0A0A0<A0A0>>>();
            A0A0A3<A0A3>.VerifyA0A0A3NotInlinedStatic();
            A0A0A3<A0A0A3<A0A3>>.VerifyA0A0A3GenericStatic<A0A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>>();
            A0A0A3<A0A1A2<A0A3>>.VerifyA0A0A3Static();
            A0A0A3<A0A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>> v47 = new A0A0A3<A0A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>>();
            v47.VerifyA0A0A3NotInlinedGeneric<A0A0A1>();
            A0A0A3<A0A0A3<A0A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>>> v48 = new A0A0A3<A0A0A3<A0A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>>>();
            v48.VerifyA0A0A3NotInlined();
            A0A3A4<A0A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>>.VerifyA0A3A4NotInlinedGenericStatic<A0A0A0A0<A0A0>>();
            A0A3A4<A0A0A0<A0A3>>.VerifyA0A3A4NotInlinedStatic();
            A0A3A4<A0A0>.VerifyA0A3A4GenericStatic<A0A0>();
            A0A3A4<A0A0A1>.VerifyA0A3A4Static();
            A0A3A4<A0A0A0<A0A3>> v49 = new A0A3A4<A0A0A0<A0A3>>();
            v49.VerifyA0A3A4NotInlinedGeneric<A0A0A0<A0A3>>();
            A0A3A4<A0A0> v50 = new A0A3A4<A0A0>();
            v50.VerifyA0A3A4NotInlined();
            A0A0A1A1<A0A4<A0A0A1>>.VerifyA0A0A1A1NotInlinedGenericStatic<A0A0A1A1<A0A4<A0A0A1>>>();
            A0A0A1A1<A0A3>.VerifyA0A0A1A1NotInlinedStatic();
            A0A0A1A1<A0A0A0A0<A0A0>>.VerifyA0A0A1A1GenericStatic<A0A0A0<A0A3>>();
            A0A0A1A1<A0A3A4<A0A0>>.VerifyA0A0A1A1Static();
            A0A0A1A1<A0A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>> v51 = new A0A0A1A1<A0A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>>();
            v51.VerifyA0A0A1A1NotInlinedGeneric<A0>();
            A0A0A1A1<A0A1A2<A0A3>> v52 = new A0A0A1A1<A0A1A2<A0A3>>();
            v52.VerifyA0A0A1A1NotInlined();
        }
    }

    public class A0A0A1 : A0A0, IA2
    {

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void VerifyA0A0A1NotInlinedGenericStatic<T>()
            where T : new()
        {
            System.Console.WriteLine(typeof(T));
            T t1 = new T();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void VerifyA0A0A1NotInlinedStatic()
        {
        }

        public static void VerifyA0A0A1GenericStatic<T>()
            where T : new()
        {
            System.Console.WriteLine(typeof(T));
            T t1 = new T();
        }

        public static void VerifyA0A0A1Static()
        {
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void VerifyA0A0A1NotInlinedGeneric<T>()
            where T : new()
        {
            System.Console.WriteLine(this);
            System.Console.WriteLine(typeof(T));
            T t2 = new T();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void VerifyA0A0A1NotInlined()
        {
            System.Console.WriteLine(this);
        }

        public void RecurseA0A0A1(int depth)
        {
            if ((depth < 0))
            {
                return;
            }
            System.Console.Write(".");
            A0A3 next = new A0A3();
            next.RecurseA0A3((depth - 1));
        }

        public void CreateAllTypesA0A0A1()
        {
            A0 v0 = new A0();
            v0.VerifyInterfaceIA1();
            A0 v1 = new A0();
            v1.VerifyInterfaceGenericIA1<A0A0A1A1<A0A0A1>>();
            A0 v2 = new A0();
            v2.VerifyInterfaceIA2();
            A0 v3 = new A0();
            v3.VerifyInterfaceGenericIA2<A0A3A4<A0A1<A0>>>();
            A0.VerifyA0NotInlinedGenericStatic<A0A0A0<A0>>();
            A0.VerifyA0NotInlinedStatic();
            A0.VerifyA0GenericStatic<A0A0A1A1<A0A0A1>>();
            A0.VerifyA0Static();
            A0 v4 = new A0();
            v4.VerifyA0NotInlinedGeneric<A0A0A0<A0>>();
            A0 v5 = new A0();
            v5.VerifyA0NotInlined();
            A0 v6 = new A0();
            v6.VirtualVerifyGeneric<IA1A2<A0A1<A0A0>>>();
            A0 v7 = new A0();
            v7.VirtualVerify();
            A0 v8 = new A0();
            v8.DeepRecursion();
            IA1 i9 = ((IA1)(new A0()));
            i9.VerifyInterfaceIA1();
            IA1 i10 = ((IA1)(new A0()));
            i10.VerifyInterfaceGenericIA1<A0A0>();
            IA2 i11 = ((IA2)(new A0()));
            i11.VerifyInterfaceIA2();
            IA2 i12 = ((IA2)(new A0()));
            i12.VerifyInterfaceGenericIA2<A0A0>();
            A0A0.VerifyA0A0NotInlinedGenericStatic<A0A1<A0>>();
            A0A0.VerifyA0A0NotInlinedStatic();
            A0A0.VerifyA0A0GenericStatic<A0>();
            A0A0.VerifyA0A0Static();
            A0A0 v13 = new A0A0();
            v13.VerifyA0A0NotInlinedGeneric<A0A0A3<A0A1A2<A0A3A4<A0A4<A0A0A1>>>>>();
            A0A0 v14 = new A0A0();
            v14.VerifyA0A0NotInlined();
            A0A0 v15 = new A0A0();
            v15.VirtualVerifyGeneric<IA2>();
            A0A0 v16 = new A0A0();
            v16.VirtualVerify();
            IA1 i17 = ((IA1)(new A0A0()));
            i17.VerifyInterfaceIA1();
            IA1 i18 = ((IA1)(new A0A0()));
            i18.VerifyInterfaceGenericIA1<A0>();
            IA2 i19 = ((IA2)(new A0A0()));
            i19.VerifyInterfaceIA2();
            IA2 i20 = ((IA2)(new A0A0()));
            i20.VerifyInterfaceGenericIA2<A0A4<A0A3>>();
            A0A1<A0A3A4<A0A1<A0>>>.VerifyA0A1NotInlinedGenericStatic<A0A0A0A0<A0A0A1>>();
            A0A1<A0A3>.VerifyA0A1NotInlinedStatic();
            A0A1<A0A3>.VerifyA0A1GenericStatic<A0A1A2<A0A3A4<A0A4<A0A0A1>>>>();
            A0A1<A0A0A1A1<A0A0A1>>.VerifyA0A1Static();
            A0A1<A0A3A4<A0A1<A0>>> v21 = new A0A1<A0A3A4<A0A1<A0>>>();
            v21.VerifyA0A1NotInlinedGeneric<A0A0A1>();
            A0A1<A0A0> v22 = new A0A1<A0A0>();
            v22.VerifyA0A1NotInlined();
            IA2 i23 = ((IA2)(new A0A1<A0A0A0<A0>>()));
            i23.VerifyInterfaceIA2();
            IA2 i24 = ((IA2)(new A0A1<A0A0A1>()));
            i24.VerifyInterfaceGenericIA2<A0A1A2<A0A3A4<A0A4<A0A0A1>>>>();
            A0A0A0<A0A3>.VerifyA0A0A0NotInlinedGenericStatic<A0>();
            A0A0A0<A0A3A4<A0A1<A0>>>.VerifyA0A0A0NotInlinedStatic();
            A0A0A0<A0A3>.VerifyA0A0A0GenericStatic<A0A1<A0A0A1>>();
            A0A0A0<A0A0A1>.VerifyA0A0A0Static();
            A0A0A0<A0A3A4<A0A1<A0>>> v25 = new A0A0A0<A0A3A4<A0A1<A0>>>();
            v25.VerifyA0A0A0NotInlinedGeneric<A0A3A4<A0A1<A0>>>();
            A0A0A0<A0A0A3<A0A1A2<A0A3A4<A0A4<A0A0A1>>>>> v26 = new A0A0A0<A0A0A3<A0A1A2<A0A3A4<A0A4<A0A0A1>>>>>();
            v26.VerifyA0A0A0NotInlined();
            IA2 i27 = ((IA2)(new A0A0A0<A0A0>()));
            i27.VerifyInterfaceIA2();
            IA2 i28 = ((IA2)(new A0A0A0<A0A0A0<A0A0>>()));
            i28.VerifyInterfaceGenericIA2<A0A1A2<A0A3A4<A0A4<A0A0A1>>>>();
            A0A3.VerifyA0A3NotInlinedGenericStatic<A0A3A4<A0A1<A0>>>();
            A0A3.VerifyA0A3NotInlinedStatic();
            A0A3.VerifyA0A3GenericStatic<A0A4<A0A3>>();
            A0A3.VerifyA0A3Static();
            A0A3 v29 = new A0A3();
            v29.VerifyA0A3NotInlinedGeneric<A0A3>();
            A0A3 v30 = new A0A3();
            v30.VerifyA0A3NotInlined();
            IA2 i31 = ((IA2)(new A0A3()));
            i31.VerifyInterfaceIA2();
            IA2 i32 = ((IA2)(new A0A3()));
            i32.VerifyInterfaceGenericIA2<A0A0A3<A0A1A2<A0A3A4<A0A4<A0A0A1>>>>>();
            A0A0A1.VerifyA0A0A1NotInlinedGenericStatic<A0A0A1A1<A0A0A1>>();
            A0A0A1.VerifyA0A0A1NotInlinedStatic();
            A0A0A1.VerifyA0A0A1GenericStatic<A0A0>();
            A0A0A1.VerifyA0A0A1Static();
            A0A0A1 v33 = new A0A0A1();
            v33.VerifyA0A0A1NotInlinedGeneric<A0>();
            A0A0A1 v34 = new A0A0A1();
            v34.VerifyA0A0A1NotInlined();
            IA2 i35 = ((IA2)(new A0A0A1()));
            i35.VerifyInterfaceIA2();
            IA2 i36 = ((IA2)(new A0A0A1()));
            i36.VerifyInterfaceGenericIA2<A0A0A0A0<A0A0A1>>();
            A0A1A2<A0>.VerifyA0A1A2NotInlinedGenericStatic<A0A0A1>();
            A0A1A2<A0A0A1>.VerifyA0A1A2NotInlinedStatic();
            A0A1A2<A0A3A4<A0A1<A0>>>.VerifyA0A1A2GenericStatic<A0A0A1>();
            A0A1A2<A0A0A0<A0A0A0<A0A0>>>.VerifyA0A1A2Static();
            A0A1A2<A0A1A2<A0A0A0<A0A0A0<A0A0>>>> v37 = new A0A1A2<A0A1A2<A0A0A0<A0A0A0<A0A0>>>>();
            v37.VerifyA0A1A2NotInlinedGeneric<A0A1A2<A0A1A2<A0A0A0<A0A0A0<A0A0>>>>>();
            A0A1A2<A0A0A0A0<A0A0A1>> v38 = new A0A1A2<A0A0A0A0<A0A0A1>>();
            v38.VerifyA0A1A2NotInlined();
            IA2 i39 = ((IA2)(new A0A1A2<A0A1A2<A0A0A0A0<A0A0A1>>>()));
            i39.VerifyInterfaceIA2();
            IA2 i40 = ((IA2)(new A0A1A2<A0A4<A0A3>>()));
            i40.VerifyInterfaceGenericIA2<A0A0A1A1<A0A0A1>>();
            A0A0A0A0<A0A1A2<A0A4<A0A3>>>.VerifyA0A0A0A0NotInlinedGenericStatic<A0A4<A0A3>>();
            A0A0A0A0<A0A0A1A1<A0A0A1>>.VerifyA0A0A0A0NotInlinedStatic();
            A0A0A0A0<A0>.VerifyA0A0A0A0GenericStatic<A0A1<A0A0A1>>();
            A0A0A0A0<A0A3A4<A0A1<A0>>>.VerifyA0A0A0A0Static();
            A0A0A0A0<A0A0A3<A0A1A2<A0A3A4<A0A4<A0A0A1>>>>> v41 = new A0A0A0A0<A0A0A3<A0A1A2<A0A3A4<A0A4<A0A0A1>>>>>();
            v41.VerifyA0A0A0A0NotInlinedGeneric<A0A0A0<A0A0A0<A0A0>>>();
            A0A0A0A0<A0A4<A0A3>> v42 = new A0A0A0A0<A0A4<A0A3>>();
            v42.VerifyA0A0A0A0NotInlined();
            IA2 i43 = ((IA2)(new A0A0A0A0<A0A1<A0A0A1>>()));
            i43.VerifyInterfaceIA2();
            IA2 i44 = ((IA2)(new A0A0A0A0<A0A3A4<A0A1<A0>>>()));
            i44.VerifyInterfaceGenericIA2<A0A0A3<A0A1A2<A0A3A4<A0A4<A0A0A1>>>>>();
            A0A4<A0A3>.VerifyA0A4NotInlinedGenericStatic<A0A3A4<A0A1<A0>>>();
            A0A4<A0A1A2<A0A4<A0A3>>>.VerifyA0A4NotInlinedStatic();
            A0A4<A0A0A3<A0A1A2<A0A3A4<A0A4<A0A0A1>>>>>.VerifyA0A4GenericStatic<A0A1A2<A0A4<A0A3>>>();
            A0A4<A0>.VerifyA0A4Static();
            A0A4<A0A1A2<A0A4<A0A3>>> v45 = new A0A4<A0A1A2<A0A4<A0A3>>>();
            v45.VerifyA0A4NotInlinedGeneric<A0>();
            A0A4<A0A0A0<A0A0A0<A0A0>>> v46 = new A0A4<A0A0A0<A0A0A0<A0A0>>>();
            v46.VerifyA0A4NotInlined();
            A0A0A3<A0A3A4<A0A1<A0>>>.VerifyA0A0A3NotInlinedGenericStatic<A0A0A1>();
            A0A0A3<A0A0A1A1<A0A0A1>>.VerifyA0A0A3NotInlinedStatic();
            A0A0A3<A0A1A2<A0A4<A0A3>>>.VerifyA0A0A3GenericStatic<A0A0A1A1<A0A0A1>>();
            A0A0A3<A0A3A4<A0A1<A0>>>.VerifyA0A0A3Static();
            A0A0A3<A0A3A4<A0A1<A0>>> v47 = new A0A0A3<A0A3A4<A0A1<A0>>>();
            v47.VerifyA0A0A3NotInlinedGeneric<A0A0A1A1<A0A0A1>>();
            A0A0A3<A0A3> v48 = new A0A0A3<A0A3>();
            v48.VerifyA0A0A3NotInlined();
            A0A3A4<A0A0A0<A0A0A0<A0A0>>>.VerifyA0A3A4NotInlinedGenericStatic<A0A0A0<A0A0A0<A0A0>>>();
            A0A3A4<A0A3>.VerifyA0A3A4NotInlinedStatic();
            A0A3A4<A0A0A0<A0A0A0<A0A0>>>.VerifyA0A3A4GenericStatic<A0>();
            A0A3A4<A0A0>.VerifyA0A3A4Static();
            A0A3A4<A0A0A3<A0A3>> v49 = new A0A3A4<A0A0A3<A0A3>>();
            v49.VerifyA0A3A4NotInlinedGeneric<A0A0A0<A0A0A0<A0A0>>>();
            A0A3A4<A0A1A2<A0A4<A0A3>>> v50 = new A0A3A4<A0A1A2<A0A4<A0A3>>>();
            v50.VerifyA0A3A4NotInlined();
            A0A0A1A1<A0>.VerifyA0A0A1A1NotInlinedGenericStatic<A0A0A0<A0A0A0<A0A0>>>();
            A0A0A1A1<A0A0A3<A0A3>>.VerifyA0A0A1A1NotInlinedStatic();
            A0A0A1A1<A0A3A4<A0A1A2<A0A4<A0A3>>>>.VerifyA0A0A1A1GenericStatic<A0A3A4<A0A1A2<A0A4<A0A3>>>>();
            A0A0A1A1<A0A1A2<A0A4<A0A3>>>.VerifyA0A0A1A1Static();
            A0A0A1A1<A0A4<A0A0A0<A0A0A0<A0A0>>>> v51 = new A0A0A1A1<A0A4<A0A0A0<A0A0A0<A0A0>>>>();
            v51.VerifyA0A0A1A1NotInlinedGeneric<A0A1A2<A0A4<A0A3>>>();
            A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>> v52 = new A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>();
            v52.VerifyA0A0A1A1NotInlined();
        }
    }

    public class A0A0 : A0, IA1, IA2
    {

        private A0A0A1A1<A0> _fA0A01;

        private static A0 _sfA0A00;

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void VerifyA0A0NotInlinedGenericStatic<T>()
            where T : new()
        {
            System.Console.WriteLine(typeof(T));
            System.Console.WriteLine(typeof(A0A0));
            T t2 = new T();
            A0A0._sfA0A00 = new A0();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void VerifyA0A0NotInlinedStatic()
        {
            System.Console.WriteLine(typeof(A0A0));
            A0A0._sfA0A00 = new A0();
        }

        public static void VerifyA0A0GenericStatic<T>()
            where T : new()
        {
            System.Console.WriteLine(typeof(T));
            System.Console.WriteLine(typeof(A0A0));
            T t2 = new T();
            A0A0._sfA0A00 = new A0();
        }

        public static void VerifyA0A0Static()
        {
            System.Console.WriteLine(typeof(A0A0));
            A0A0._sfA0A00 = new A0();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void VerifyA0A0NotInlinedGeneric<T>()
            where T : new()
        {
            System.Console.WriteLine(this);
            System.Console.WriteLine(typeof(T));
            System.Console.WriteLine(typeof(A0A0));
            T t3 = new T();
            A0A0._sfA0A00 = new A0();
            this._fA0A01 = new A0A0A1A1<A0>();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void VerifyA0A0NotInlined()
        {
            System.Console.WriteLine(this);
            System.Console.WriteLine(typeof(A0A0));
            A0A0._sfA0A00 = new A0();
            this._fA0A01 = new A0A0A1A1<A0>();
        }

        public override void VirtualVerifyGeneric<T>()
        {
            base.VirtualVerifyGeneric<T>();
            System.Console.WriteLine(this);
            System.Console.WriteLine(typeof(T));
            System.Console.WriteLine(typeof(A0A0));
            A0A0._sfA0A00 = new A0();
            this._fA0A01 = new A0A0A1A1<A0>();
        }

        public override void VirtualVerify()
        {
            base.VirtualVerify();
            System.Console.WriteLine(this);
            System.Console.WriteLine(typeof(A0A0));
            A0A0._sfA0A00 = new A0();
            this._fA0A01 = new A0A0A1A1<A0>();
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
            v1.VerifyInterfaceGenericIA1<A0A3>();
            A0 v2 = new A0();
            v2.VerifyInterfaceIA2();
            A0 v3 = new A0();
            v3.VerifyInterfaceGenericIA2<A0A0A0<A0A3A4<A0A0A3<A0A0A1>>>>();
            A0.VerifyA0NotInlinedGenericStatic<A0A4<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>();
            A0.VerifyA0NotInlinedStatic();
            A0.VerifyA0GenericStatic<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>();
            A0.VerifyA0Static();
            A0 v4 = new A0();
            v4.VerifyA0NotInlinedGeneric<A0A0A1>();
            A0 v5 = new A0();
            v5.VerifyA0NotInlined();
            A0 v6 = new A0();
            v6.VirtualVerifyGeneric<A0A0A0<A0A3A4<A0A0A3<A0A0A1>>>>();
            A0 v7 = new A0();
            v7.VirtualVerify();
            A0 v8 = new A0();
            v8.DeepRecursion();
            IA1 i9 = ((IA1)(new A0()));
            i9.VerifyInterfaceIA1();
            IA1 i10 = ((IA1)(new A0()));
            i10.VerifyInterfaceGenericIA1<A0A3>();
            IA2 i11 = ((IA2)(new A0()));
            i11.VerifyInterfaceIA2();
            IA2 i12 = ((IA2)(new A0()));
            i12.VerifyInterfaceGenericIA2<A0A3>();
            A0A0.VerifyA0A0NotInlinedGenericStatic<A0A3>();
            A0A0.VerifyA0A0NotInlinedStatic();
            A0A0.VerifyA0A0GenericStatic<A0A0A1>();
            A0A0.VerifyA0A0Static();
            A0A0 v13 = new A0A0();
            v13.VerifyA0A0NotInlinedGeneric<A0A3A4<A0A4<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>();
            A0A0 v14 = new A0A0();
            v14.VerifyA0A0NotInlined();
            A0A0 v15 = new A0A0();
            v15.VirtualVerifyGeneric<A0A1A2<A0A3A4<A0A0A3<A0A0A1>>>>();
            A0A0 v16 = new A0A0();
            v16.VirtualVerify();
            IA1 i17 = ((IA1)(new A0A0()));
            i17.VerifyInterfaceIA1();
            IA1 i18 = ((IA1)(new A0A0()));
            i18.VerifyInterfaceGenericIA1<A0A3A4<A0A4<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>();
            IA2 i19 = ((IA2)(new A0A0()));
            i19.VerifyInterfaceIA2();
            IA2 i20 = ((IA2)(new A0A0()));
            i20.VerifyInterfaceGenericIA2<A0A0A1>();
            A0A1<A0A3>.VerifyA0A1NotInlinedGenericStatic<A0A0A3<A0A0>>();
            A0A1<A0A3A4<A0A4<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>.VerifyA0A1NotInlinedStatic();
            A0A1<A0A0A0A0<A0A0A1>>.VerifyA0A1GenericStatic<A0A0A0A0<A0A0A1>>();
            A0A1<A0A0A3<A0A0>>.VerifyA0A1Static();
            A0A1<A0A0A0<A0A3A4<A0A0A3<A0A0A1>>>> v21 = new A0A1<A0A0A0<A0A3A4<A0A0A3<A0A0A1>>>>();
            v21.VerifyA0A1NotInlinedGeneric<A0A0>();
            A0A1<A0A0A0A0<A0A0A1>> v22 = new A0A1<A0A0A0A0<A0A0A1>>();
            v22.VerifyA0A1NotInlined();
            IA2 i23 = ((IA2)(new A0A1<A0A0>()));
            i23.VerifyInterfaceIA2();
            IA2 i24 = ((IA2)(new A0A1<A0A3A4<A0A4<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>()));
            i24.VerifyInterfaceGenericIA2<A0A0A0<A0A3A4<A0A0A3<A0A0A1>>>>();
            A0A0A0<A0A1A2<A0A3A4<A0A0A3<A0A0A1>>>>.VerifyA0A0A0NotInlinedGenericStatic<A0A1A2<A0A3A4<A0A0A3<A0A0A1>>>>();
            A0A0A0<A0A0A3<A0A0>>.VerifyA0A0A0NotInlinedStatic();
            A0A0A0<A0A0>.VerifyA0A0A0GenericStatic<A0A1<A0A3A4<A0A4<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>();
            A0A0A0<A0A4<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>.VerifyA0A0A0Static();
            A0A0A0<A0A0> v25 = new A0A0A0<A0A0>();
            v25.VerifyA0A0A0NotInlinedGeneric<A0A0A0A0<A0A0A1>>();
            A0A0A0<A0A1A2<A0A3A4<A0A0A3<A0A0A1>>>> v26 = new A0A0A0<A0A1A2<A0A3A4<A0A0A3<A0A0A1>>>>();
            v26.VerifyA0A0A0NotInlined();
            IA2 i27 = ((IA2)(new A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>()));
            i27.VerifyInterfaceIA2();
            IA2 i28 = ((IA2)(new A0A0A0<A0A3>()));
            i28.VerifyInterfaceGenericIA2<A0A0A0A0<A0A0A1>>();
            A0A3.VerifyA0A3NotInlinedGenericStatic<A0A3>();
            A0A3.VerifyA0A3NotInlinedStatic();
            A0A3.VerifyA0A3GenericStatic<A0A0A0<A0A3>>();
            A0A3.VerifyA0A3Static();
            A0A3 v29 = new A0A3();
            v29.VerifyA0A3NotInlinedGeneric<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>();
            A0A3 v30 = new A0A3();
            v30.VerifyA0A3NotInlined();
            IA2 i31 = ((IA2)(new A0A3()));
            i31.VerifyInterfaceIA2();
            IA2 i32 = ((IA2)(new A0A3()));
            i32.VerifyInterfaceGenericIA2<A0A0>();
            A0A0A1.VerifyA0A0A1NotInlinedGenericStatic<A0A1A2<A0A3A4<A0A0A3<A0A0A1>>>>();
            A0A0A1.VerifyA0A0A1NotInlinedStatic();
            A0A0A1.VerifyA0A0A1GenericStatic<A0A0>();
            A0A0A1.VerifyA0A0A1Static();
            A0A0A1 v33 = new A0A0A1();
            v33.VerifyA0A0A1NotInlinedGeneric<A0A0A0A0<A0A0A1>>();
            A0A0A1 v34 = new A0A0A1();
            v34.VerifyA0A0A1NotInlined();
            IA2 i35 = ((IA2)(new A0A0A1()));
            i35.VerifyInterfaceIA2();
            IA2 i36 = ((IA2)(new A0A0A1()));
            i36.VerifyInterfaceGenericIA2<A0A1<A0A3A4<A0A4<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>();
            A0A1A2<A0A4<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>.VerifyA0A1A2NotInlinedGenericStatic<A0A1A2<A0A4<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>();
            A0A1A2<A0A0A0<A0A3>>.VerifyA0A1A2NotInlinedStatic();
            A0A1A2<A0A0A3<A0A0>>.VerifyA0A1A2GenericStatic<A0A0A3<A0A0>>();
            A0A1A2<A0A1A2<A0A0A3<A0A0>>>.VerifyA0A1A2Static();
            A0A1A2<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>> v37 = new A0A1A2<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>();
            v37.VerifyA0A1A2NotInlinedGeneric<A0A4<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>();
            A0A1A2<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>> v38 = new A0A1A2<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>();
            v38.VerifyA0A1A2NotInlined();
            IA2 i39 = ((IA2)(new A0A1A2<A0A1<A0A3A4<A0A4<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>()));
            i39.VerifyInterfaceIA2();
            IA2 i40 = ((IA2)(new A0A1A2<A0A0A0A0<A0A0A1>>()));
            i40.VerifyInterfaceGenericIA2<A0A0A0A0<A0A0A1>>();
            A0A0A0A0<A0A3>.VerifyA0A0A0A0NotInlinedGenericStatic<A0A1<A0A3A4<A0A4<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>();
            A0A0A0A0<A0A4<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>.VerifyA0A0A0A0NotInlinedStatic();
            A0A0A0A0<A0A0A3<A0A0>>.VerifyA0A0A0A0GenericStatic<A0A0A0A0<A0A0A3<A0A0>>>();
            A0A0A0A0<A0A1<A0A3A4<A0A4<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>.VerifyA0A0A0A0Static();
            A0A0A0A0<A0A0A0<A0A3>> v41 = new A0A0A0A0<A0A0A0<A0A3>>();
            v41.VerifyA0A0A0A0NotInlinedGeneric<A0A1<A0A3A4<A0A4<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>();
            A0A0A0A0<A0A0A0<A0A3>> v42 = new A0A0A0A0<A0A0A0<A0A3>>();
            v42.VerifyA0A0A0A0NotInlined();
            IA2 i43 = ((IA2)(new A0A0A0A0<A0A1<A0A3A4<A0A4<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>()));
            i43.VerifyInterfaceIA2();
            IA2 i44 = ((IA2)(new A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>()));
            i44.VerifyInterfaceGenericIA2<A0A3>();
            A0A4<A0A3>.VerifyA0A4NotInlinedGenericStatic<A0A0A0<A0A3>>();
            A0A4<A0A0A0<A0A3>>.VerifyA0A4NotInlinedStatic();
            A0A4<A0A0A0<A0A3>>.VerifyA0A4GenericStatic<A0A0A3<A0A0>>();
            A0A4<A0A0>.VerifyA0A4Static();
            A0A4<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>> v45 = new A0A4<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>();
            v45.VerifyA0A4NotInlinedGeneric<A0A1A2<A0A0A0A0<A0A0A1>>>();
            A0A4<A0A0A1> v46 = new A0A4<A0A0A1>();
            v46.VerifyA0A4NotInlined();
            A0A0A3<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>.VerifyA0A0A3NotInlinedGenericStatic<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>();
            A0A0A3<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>.VerifyA0A0A3NotInlinedStatic();
            A0A0A3<A0A1<A0A3A4<A0A4<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>.VerifyA0A0A3GenericStatic<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>();
            A0A0A3<A0A3>.VerifyA0A0A3Static();
            A0A0A3<A0A4<A0A0A1>> v47 = new A0A0A3<A0A4<A0A0A1>>();
            v47.VerifyA0A0A3NotInlinedGeneric<A0A4<A0A0A1>>();
            A0A0A3<A0A0A1> v48 = new A0A0A3<A0A0A1>();
            v48.VerifyA0A0A3NotInlined();
            A0A3A4<A0A4<A0A0A1>>.VerifyA0A3A4NotInlinedGenericStatic<A0A0A0<A0A3>>();
            A0A3A4<A0A0A0<A0A3>>.VerifyA0A3A4NotInlinedStatic();
            A0A3A4<A0A0A3<A0A0A1>>.VerifyA0A3A4GenericStatic<A0A1<A0A3A4<A0A4<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>();
            A0A3A4<A0A1<A0A3A4<A0A4<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>.VerifyA0A3A4Static();
            A0A3A4<A0A1A2<A0A0A0A0<A0A0A1>>> v49 = new A0A3A4<A0A1A2<A0A0A0A0<A0A0A1>>>();
            v49.VerifyA0A3A4NotInlinedGeneric<A0A0>();
            A0A3A4<A0A1A2<A0A0A0A0<A0A0A1>>> v50 = new A0A3A4<A0A1A2<A0A0A0A0<A0A0A1>>>();
            v50.VerifyA0A3A4NotInlined();
            A0A0A1A1<A0A0A0<A0A3>>.VerifyA0A0A1A1NotInlinedGenericStatic<A0A3A4<A0A1A2<A0A0A0A0<A0A0A1>>>>();
            A0A0A1A1<A0A0A1>.VerifyA0A0A1A1NotInlinedStatic();
            A0A0A1A1<A0A0A1A1<A0A0A1>>.VerifyA0A0A1A1GenericStatic<A0A0A1>();
            A0A0A1A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>.VerifyA0A0A1A1Static();
            A0A0A1A1<A0A3A4<A0A1A2<A0A0A0A0<A0A0A1>>>> v51 = new A0A0A1A1<A0A3A4<A0A1A2<A0A0A0A0<A0A0A1>>>>();
            v51.VerifyA0A0A1A1NotInlinedGeneric<A0A0A1>();
            A0A0A1A1<A0A0A1> v52 = new A0A0A1A1<A0A0A1>();
            v52.VerifyA0A0A1A1NotInlined();
        }
    }

    public interface IA2
    {

        void VerifyInterfaceIA2();

        void VerifyInterfaceGenericIA2<K>()
            where K : new();
    }

    public class A0A1A2<T0> : A0A1<T0>, IA2
        where T0 : new()
    {

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void VerifyA0A1A2NotInlinedGenericStatic<T>()
            where T : new()
        {
            System.Console.WriteLine(typeof(T));
            T0 t1 = new T0();
            T t2 = new T();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void VerifyA0A1A2NotInlinedStatic()
        {
            T0 t0 = new T0();
        }

        public static void VerifyA0A1A2GenericStatic<T>()
            where T : new()
        {
            System.Console.WriteLine(typeof(T));
            T0 t1 = new T0();
            T t2 = new T();
        }

        public static void VerifyA0A1A2Static()
        {
            T0 t0 = new T0();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void VerifyA0A1A2NotInlinedGeneric<T>()
            where T : new()
        {
            System.Console.WriteLine(this);
            System.Console.WriteLine(typeof(T));
            T0 t2 = new T0();
            T t3 = new T();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void VerifyA0A1A2NotInlined()
        {
            System.Console.WriteLine(this);
            T0 t1 = new T0();
        }

        public void RecurseA0A1A2(int depth)
        {
            if ((depth < 0))
            {
                return;
            }
            System.Console.Write(".");
            A0A0A1 next = new A0A0A1();
            next.RecurseA0A0A1((depth - 1));
        }

        public void CreateAllTypesA0A1A2()
        {
            A0 v0 = new A0();
            v0.VerifyInterfaceIA1();
            A0 v1 = new A0();
            v1.VerifyInterfaceGenericIA1<A0A0A3<A0A3>>();
            A0 v2 = new A0();
            v2.VerifyInterfaceIA2();
            A0 v3 = new A0();
            v3.VerifyInterfaceGenericIA2<A0A4<A0A0A0<A0A0A0<A0A0>>>>();
            A0.VerifyA0NotInlinedGenericStatic<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>();
            A0.VerifyA0NotInlinedStatic();
            A0.VerifyA0GenericStatic<A0A1A2<A0A4<A0A3>>>();
            A0.VerifyA0Static();
            A0 v4 = new A0();
            v4.VerifyA0NotInlinedGeneric<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>();
            A0 v5 = new A0();
            v5.VerifyA0NotInlined();
            A0 v6 = new A0();
            v6.VirtualVerifyGeneric<A0A4<A0A0A0<A0A0A0<A0A0>>>>();
            A0 v7 = new A0();
            v7.VirtualVerify();
            A0 v8 = new A0();
            v8.DeepRecursion();
            IA1 i9 = ((IA1)(new A0()));
            i9.VerifyInterfaceIA1();
            IA1 i10 = ((IA1)(new A0()));
            i10.VerifyInterfaceGenericIA1<A0A4<A0A0A0<A0A0A0<A0A0>>>>();
            IA2 i11 = ((IA2)(new A0()));
            i11.VerifyInterfaceIA2();
            IA2 i12 = ((IA2)(new A0()));
            i12.VerifyInterfaceGenericIA2<A0A4<A0A0A0<A0A0A0<A0A0>>>>();
            A0A0.VerifyA0A0NotInlinedGenericStatic<A0A1<A0A0A1>>();
            A0A0.VerifyA0A0NotInlinedStatic();
            A0A0.VerifyA0A0GenericStatic<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>();
            A0A0.VerifyA0A0Static();
            A0A0 v13 = new A0A0();
            v13.VerifyA0A0NotInlinedGeneric<A0A1A2<A0A4<A0A3>>>();
            A0A0 v14 = new A0A0();
            v14.VerifyA0A0NotInlined();
            A0A0 v15 = new A0A0();
            v15.VirtualVerifyGeneric<A0A0A0<A0A0A0<A0A0>>>();
            A0A0 v16 = new A0A0();
            v16.VirtualVerify();
            IA1 i17 = ((IA1)(new A0A0()));
            i17.VerifyInterfaceIA1();
            IA1 i18 = ((IA1)(new A0A0()));
            i18.VerifyInterfaceGenericIA1<A0>();
            IA2 i19 = ((IA2)(new A0A0()));
            i19.VerifyInterfaceIA2();
            IA2 i20 = ((IA2)(new A0A0()));
            i20.VerifyInterfaceGenericIA2<A0A3A4<A0A1A2<A0A4<A0A3>>>>();
            A0A1<A0A0A0<A0A0A0<A0A0>>>.VerifyA0A1NotInlinedGenericStatic<A0A3>();
            A0A1<A0A3A4<A0A1A2<A0A4<A0A3>>>>.VerifyA0A1NotInlinedStatic();
            A0A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>.VerifyA0A1GenericStatic<A0A3>();
            A0A1<A0A0>.VerifyA0A1Static();
            A0A1<A0A0A3<A0A3>> v21 = new A0A1<A0A0A3<A0A3>>();
            v21.VerifyA0A1NotInlinedGeneric<A0A3>();
            A0A1<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>> v22 = new A0A1<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>();
            v22.VerifyA0A1NotInlined();
            IA2 i23 = ((IA2)(new A0A1<A0A0A0<A0A0A0<A0A0>>>()));
            i23.VerifyInterfaceIA2();
            IA2 i24 = ((IA2)(new A0A1<A0A0A1>()));
            i24.VerifyInterfaceGenericIA2<A0A1A2<A0A4<A0A3>>>();
            A0A0A0<A0A4<A0A0A0<A0A0A0<A0A0>>>>.VerifyA0A0A0NotInlinedGenericStatic<A0A0A1>();
            A0A0A0<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>.VerifyA0A0A0NotInlinedStatic();
            A0A0A0<A0A1A2<A0A4<A0A3>>>.VerifyA0A0A0GenericStatic<A0A3>();
            A0A0A0<A0A3A4<A0A1A2<A0A4<A0A3>>>>.VerifyA0A0A0Static();
            A0A0A0<A0A0A1> v25 = new A0A0A0<A0A0A1>();
            v25.VerifyA0A0A0NotInlinedGeneric<A0A1<A0A0A1>>();
            A0A0A0<A0A0> v26 = new A0A0A0<A0A0>();
            v26.VerifyA0A0A0NotInlined();
            IA2 i27 = ((IA2)(new A0A0A0<A0A0A0<A0A0>>()));
            i27.VerifyInterfaceIA2();
            IA2 i28 = ((IA2)(new A0A0A0<A0A0A1>()));
            i28.VerifyInterfaceGenericIA2<A0A4<A0A0A0<A0A0A0<A0A0>>>>();
            A0A3.VerifyA0A3NotInlinedGenericStatic<A0A0A0<A0A0A1>>();
            A0A3.VerifyA0A3NotInlinedStatic();
            A0A3.VerifyA0A3GenericStatic<A0A4<A0A0A0<A0A0A0<A0A0>>>>();
            A0A3.VerifyA0A3Static();
            A0A3 v29 = new A0A3();
            v29.VerifyA0A3NotInlinedGeneric<A0A1A2<A0A4<A0A3>>>();
            A0A3 v30 = new A0A3();
            v30.VerifyA0A3NotInlined();
            IA2 i31 = ((IA2)(new A0A3()));
            i31.VerifyInterfaceIA2();
            IA2 i32 = ((IA2)(new A0A3()));
            i32.VerifyInterfaceGenericIA2<A0A3>();
            A0A0A1.VerifyA0A0A1NotInlinedGenericStatic<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>();
            A0A0A1.VerifyA0A0A1NotInlinedStatic();
            A0A0A1.VerifyA0A0A1GenericStatic<A0A3A4<A0A1A2<A0A4<A0A3>>>>();
            A0A0A1.VerifyA0A0A1Static();
            A0A0A1 v33 = new A0A0A1();
            v33.VerifyA0A0A1NotInlinedGeneric<A0A0A3<A0A3>>();
            A0A0A1 v34 = new A0A0A1();
            v34.VerifyA0A0A1NotInlined();
            IA2 i35 = ((IA2)(new A0A0A1()));
            i35.VerifyInterfaceIA2();
            IA2 i36 = ((IA2)(new A0A0A1()));
            i36.VerifyInterfaceGenericIA2<A0A0A0<A0A0A1>>();
            A0A1A2<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>.VerifyA0A1A2NotInlinedGenericStatic<A0A3>();
            A0A1A2<A0A0A1>.VerifyA0A1A2NotInlinedStatic();
            A0A1A2<A0A1A2<A0A0A1>>.VerifyA0A1A2GenericStatic<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>();
            A0A1A2<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>.VerifyA0A1A2Static();
            A0A1A2<A0A0A0<A0A0A1>> v37 = new A0A1A2<A0A0A0<A0A0A1>>();
            v37.VerifyA0A1A2NotInlinedGeneric<A0A0A1>();
            A0A1A2<A0A0A3<A0A3>> v38 = new A0A1A2<A0A0A3<A0A3>>();
            v38.VerifyA0A1A2NotInlined();
            IA2 i39 = ((IA2)(new A0A1A2<A0A0A1>()));
            i39.VerifyInterfaceIA2();
            IA2 i40 = ((IA2)(new A0A1A2<A0A4<A0A0A0<A0A0A0<A0A0>>>>()));
            i40.VerifyInterfaceGenericIA2<A0A4<A0A0A0<A0A0A0<A0A0>>>>();
            A0A0A0A0<A0A0>.VerifyA0A0A0A0NotInlinedGenericStatic<A0A0A1>();
            A0A0A0A0<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>.VerifyA0A0A0A0NotInlinedStatic();
            A0A0A0A0<A0A1<A0A0A1>>.VerifyA0A0A0A0GenericStatic<A0A4<A0A0A0<A0A0A0<A0A0>>>>();
            A0A0A0A0<A0A0A0A0<A0A1<A0A0A1>>>.VerifyA0A0A0A0Static();
            A0A0A0A0<A0A1A2<A0A4<A0A0A0<A0A0A0<A0A0>>>>> v41 = new A0A0A0A0<A0A1A2<A0A4<A0A0A0<A0A0A0<A0A0>>>>>();
            v41.VerifyA0A0A0A0NotInlinedGeneric<A0A0A0A0<A0A1A2<A0A4<A0A0A0<A0A0A0<A0A0>>>>>>();
            A0A0A0A0<A0A0A3<A0A3>> v42 = new A0A0A0A0<A0A0A3<A0A3>>();
            v42.VerifyA0A0A0A0NotInlined();
            IA2 i43 = ((IA2)(new A0A0A0A0<A0A0A3<A0A3>>()));
            i43.VerifyInterfaceIA2();
            IA2 i44 = ((IA2)(new A0A0A0A0<A0A0A3<A0A3>>()));
            i44.VerifyInterfaceGenericIA2<A0>();
            A0A4<A0A3A4<A0A1A2<A0A4<A0A3>>>>.VerifyA0A4NotInlinedGenericStatic<A0A1<A0A0A1>>();
            A0A4<A0A0A0<A0A0A1>>.VerifyA0A4NotInlinedStatic();
            A0A4<A0A4<A0A0A0<A0A0A1>>>.VerifyA0A4GenericStatic<A0A1A2<A0A4<A0A0A0<A0A0A0<A0A0>>>>>();
            A0A4<A0A0A1>.VerifyA0A4Static();
            A0A4<A0A3> v45 = new A0A4<A0A3>();
            v45.VerifyA0A4NotInlinedGeneric<A0A0>();
            A0A4<A0A0A1> v46 = new A0A4<A0A0A1>();
            v46.VerifyA0A4NotInlined();
            A0A0A3<A0A0>.VerifyA0A0A3NotInlinedGenericStatic<A0A1A2<A0A4<A0A0A0<A0A0A0<A0A0>>>>>();
            A0A0A3<A0A3>.VerifyA0A0A3NotInlinedStatic();
            A0A0A3<A0>.VerifyA0A0A3GenericStatic<A0A0A1>();
            A0A0A3<A0A0A0A0<A0A0A3<A0A3>>>.VerifyA0A0A3Static();
            A0A0A3<A0A0A1> v47 = new A0A0A3<A0A0A1>();
            v47.VerifyA0A0A3NotInlinedGeneric<A0A0A0A0<A0A0A3<A0A3>>>();
            A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>> v48 = new A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>();
            v48.VerifyA0A0A3NotInlined();
            A0A3A4<A0A0A1>.VerifyA0A3A4NotInlinedGenericStatic<A0A0A0A0<A0A0A3<A0A3>>>();
            A0A3A4<A0A1A2<A0A4<A0A0A0<A0A0A0<A0A0>>>>>.VerifyA0A3A4NotInlinedStatic();
            A0A3A4<A0A4<A0A0A1>>.VerifyA0A3A4GenericStatic<A0A0A0A0<A0A0A3<A0A3>>>();
            A0A3A4<A0A3A4<A0A4<A0A0A1>>>.VerifyA0A3A4Static();
            A0A3A4<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>> v49 = new A0A3A4<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>();
            v49.VerifyA0A3A4NotInlinedGeneric<A0A0A0<A0A0A1>>();
            A0A3A4<A0A4<A0A0A1>> v50 = new A0A3A4<A0A4<A0A0A1>>();
            v50.VerifyA0A3A4NotInlined();
            A0A0A1A1<A0A0A0<A0A0A1>>.VerifyA0A0A1A1NotInlinedGenericStatic<A0A0A0<A0A0A1>>();
            A0A0A1A1<A0A1<A0A0A1>>.VerifyA0A0A1A1NotInlinedStatic();
            A0A0A1A1<A0A1A2<A0A4<A0A0A0<A0A0A0<A0A0>>>>>.VerifyA0A0A1A1GenericStatic<A0A1<A0A0A1>>();
            A0A0A1A1<A0A0A0A0<A0A0A3<A0A3>>>.VerifyA0A0A1A1Static();
            A0A0A1A1<A0A0A1> v51 = new A0A0A1A1<A0A0A1>();
            v51.VerifyA0A0A1A1NotInlinedGeneric<A0A0A0A0<A0A0A3<A0A3>>>();
            A0A0A1A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>> v52 = new A0A0A1A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>();
            v52.VerifyA0A0A1A1NotInlined();
        }
    }

    public interface IA1 : IA2
    {

        void VerifyInterfaceIA1();

        void VerifyInterfaceGenericIA1<K>()
            where K : new();
    }

    public interface IA1A2<T0> : IA2
        where T0 : new()
    {
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
            A0A1<A0A1<A0A4<A0A0A1>>> v2 = new A0A1<A0A1<A0A4<A0A0A1>>>();
            v2.CreateAllTypesA0A1();
            A0A0A0<A0A1A2<A0A3>> v3 = new A0A0A0<A0A1A2<A0A3>>();
            v3.CreateAllTypesA0A0A0();
            A0A3 v4 = new A0A3();
            v4.CreateAllTypesA0A3();
            A0A0A1 v5 = new A0A0A1();
            v5.CreateAllTypesA0A0A1();
            A0A1A2<A0A4<A0A0A3<A0A0A3<A0A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>>>>> v6 = new A0A1A2<A0A4<A0A0A3<A0A0A3<A0A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>>>>>();
            v6.CreateAllTypesA0A1A2();
            A0A0A0A0<A0A0> v7 = new A0A0A0A0<A0A0>();
            v7.CreateAllTypesA0A0A0A0();
            A0A4<A0A0A1> v8 = new A0A4<A0A0A1>();
            v8.CreateAllTypesA0A4();
            A0A0A3<A0A0A3<A0A0A1>> v9 = new A0A0A3<A0A0A3<A0A0A1>>();
            v9.CreateAllTypesA0A0A3();
            A0A3A4<A0> v10 = new A0A3A4<A0>();
            v10.CreateAllTypesA0A3A4();
            A0A0A1A1<A0A0A0<A0A1A2<A0A3>>> v11 = new A0A0A1A1<A0A0A0<A0A1A2<A0A3>>>();
            v11.CreateAllTypesA0A0A1A1();
            System.Console.WriteLine("Test SUCCESS");
            return 100;
        }
    }

    public interface IA2A6<T0>
        where T0 : new()
    {
    }

    public interface IA1A5<T0> : IA2
        where T0 : new()
    {
    }

    public class A0A4<T0> : A0
        where T0 : new()
    {

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void VerifyA0A4NotInlinedGenericStatic<T>()
            where T : new()
        {
            System.Console.WriteLine(typeof(T));
            T0 t1 = new T0();
            T t2 = new T();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void VerifyA0A4NotInlinedStatic()
        {
            T0 t0 = new T0();
        }

        public static void VerifyA0A4GenericStatic<T>()
            where T : new()
        {
            System.Console.WriteLine(typeof(T));
            T0 t1 = new T0();
            T t2 = new T();
        }

        public static void VerifyA0A4Static()
        {
            T0 t0 = new T0();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void VerifyA0A4NotInlinedGeneric<T>()
            where T : new()
        {
            System.Console.WriteLine(this);
            System.Console.WriteLine(typeof(T));
            T0 t2 = new T0();
            T t3 = new T();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void VerifyA0A4NotInlined()
        {
            System.Console.WriteLine(this);
            T0 t1 = new T0();
        }

        public void RecurseA0A4(int depth)
        {
            if ((depth < 0))
            {
                return;
            }
            System.Console.Write(".");
            A0A0A0A0<A0> next = new A0A0A0A0<A0>();
            next.RecurseA0A0A0A0((depth - 1));
        }

        public void CreateAllTypesA0A4()
        {
            A0 v0 = new A0();
            v0.VerifyInterfaceIA1();
            A0 v1 = new A0();
            v1.VerifyInterfaceGenericIA1<A0A3A4<A0A3>>();
            A0 v2 = new A0();
            v2.VerifyInterfaceIA2();
            A0 v3 = new A0();
            v3.VerifyInterfaceGenericIA2<A0A0A1>();
            A0.VerifyA0NotInlinedGenericStatic<A0A0A3<A0A3>>();
            A0.VerifyA0NotInlinedStatic();
            A0.VerifyA0GenericStatic<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>();
            A0.VerifyA0Static();
            A0 v4 = new A0();
            v4.VerifyA0NotInlinedGeneric<A0A1A2<A0A1A2<A0A0>>>();
            A0 v5 = new A0();
            v5.VerifyA0NotInlined();
            A0 v6 = new A0();
            v6.VirtualVerifyGeneric<A0A0A3<A0A3>>();
            A0 v7 = new A0();
            v7.VirtualVerify();
            A0 v8 = new A0();
            v8.DeepRecursion();
            IA1 i9 = ((IA1)(new A0()));
            i9.VerifyInterfaceIA1();
            IA1 i10 = ((IA1)(new A0()));
            i10.VerifyInterfaceGenericIA1<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>();
            IA2 i11 = ((IA2)(new A0()));
            i11.VerifyInterfaceIA2();
            IA2 i12 = ((IA2)(new A0()));
            i12.VerifyInterfaceGenericIA2<A0A0>();
            A0A0.VerifyA0A0NotInlinedGenericStatic<A0A0>();
            A0A0.VerifyA0A0NotInlinedStatic();
            A0A0.VerifyA0A0GenericStatic<A0>();
            A0A0.VerifyA0A0Static();
            A0A0 v13 = new A0A0();
            v13.VerifyA0A0NotInlinedGeneric<A0A3A4<A0A3>>();
            A0A0 v14 = new A0A0();
            v14.VerifyA0A0NotInlined();
            A0A0 v15 = new A0A0();
            v15.VirtualVerifyGeneric<A0A0A1>();
            A0A0 v16 = new A0A0();
            v16.VirtualVerify();
            IA1 i17 = ((IA1)(new A0A0()));
            i17.VerifyInterfaceIA1();
            IA1 i18 = ((IA1)(new A0A0()));
            i18.VerifyInterfaceGenericIA1<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>();
            IA2 i19 = ((IA2)(new A0A0()));
            i19.VerifyInterfaceIA2();
            IA2 i20 = ((IA2)(new A0A0()));
            i20.VerifyInterfaceGenericIA2<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>();
            A0A1<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>.VerifyA0A1NotInlinedGenericStatic<A0>();
            A0A1<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>.VerifyA0A1NotInlinedStatic();
            A0A1<A0A1A2<A0A1A2<A0A0>>>.VerifyA0A1GenericStatic<A0A3>();
            A0A1<A0>.VerifyA0A1Static();
            A0A1<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>> v21 = new A0A1<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>();
            v21.VerifyA0A1NotInlinedGeneric<A0A0A1A1<A0A4<A0A0A0<A0A0>>>>();
            A0A1<A0A3> v22 = new A0A1<A0A3>();
            v22.VerifyA0A1NotInlined();
            IA2 i23 = ((IA2)(new A0A1<A0A0>()));
            i23.VerifyInterfaceIA2();
            IA2 i24 = ((IA2)(new A0A1<A0A1A2<A0A1A2<A0A0>>>()));
            i24.VerifyInterfaceGenericIA2<A0A3>();
            A0A0A0<A0A1A2<A0A1A2<A0A0>>>.VerifyA0A0A0NotInlinedGenericStatic<A0>();
            A0A0A0<A0A3>.VerifyA0A0A0NotInlinedStatic();
            A0A0A0<A0A0A3<A0A3>>.VerifyA0A0A0GenericStatic<A0A4<A0A0A0<A0A0>>>();
            A0A0A0<A0A0A3<A0A3>>.VerifyA0A0A0Static();
            A0A0A0<A0A0A3<A0A3>> v25 = new A0A0A0<A0A0A3<A0A3>>();
            v25.VerifyA0A0A0NotInlinedGeneric<A0A0A3<A0A3>>();
            A0A0A0<A0A0A1> v26 = new A0A0A0<A0A0A1>();
            v26.VerifyA0A0A0NotInlined();
            IA2 i27 = ((IA2)(new A0A0A0<A0>()));
            i27.VerifyInterfaceIA2();
            IA2 i28 = ((IA2)(new A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>()));
            i28.VerifyInterfaceGenericIA2<A0A3>();
            A0A3.VerifyA0A3NotInlinedGenericStatic<A0A1<A0A1A2<A0A1A2<A0A0>>>>();
            A0A3.VerifyA0A3NotInlinedStatic();
            A0A3.VerifyA0A3GenericStatic<A0A0A3<A0A3>>();
            A0A3.VerifyA0A3Static();
            A0A3 v29 = new A0A3();
            v29.VerifyA0A3NotInlinedGeneric<A0A3>();
            A0A3 v30 = new A0A3();
            v30.VerifyA0A3NotInlined();
            IA2 i31 = ((IA2)(new A0A3()));
            i31.VerifyInterfaceIA2();
            IA2 i32 = ((IA2)(new A0A3()));
            i32.VerifyInterfaceGenericIA2<A0A0A1>();
            A0A0A1.VerifyA0A0A1NotInlinedGenericStatic<A0A0A1>();
            A0A0A1.VerifyA0A0A1NotInlinedStatic();
            A0A0A1.VerifyA0A0A1GenericStatic<A0A0>();
            A0A0A1.VerifyA0A0A1Static();
            A0A0A1 v33 = new A0A0A1();
            v33.VerifyA0A0A1NotInlinedGeneric<A0A4<A0A0A0<A0A0>>>();
            A0A0A1 v34 = new A0A0A1();
            v34.VerifyA0A0A1NotInlined();
            IA2 i35 = ((IA2)(new A0A0A1()));
            i35.VerifyInterfaceIA2();
            IA2 i36 = ((IA2)(new A0A0A1()));
            i36.VerifyInterfaceGenericIA2<A0A0A1A1<A0A4<A0A0A0<A0A0>>>>();
            A0A1A2<A0>.VerifyA0A1A2NotInlinedGenericStatic<A0A4<A0A0A0<A0A0>>>();
            A0A1A2<A0>.VerifyA0A1A2NotInlinedStatic();
            A0A1A2<A0A3A4<A0A3>>.VerifyA0A1A2GenericStatic<A0A0A1>();
            A0A1A2<A0A0A1>.VerifyA0A1A2Static();
            A0A1A2<A0A0A1A1<A0A4<A0A0A0<A0A0>>>> v37 = new A0A1A2<A0A0A1A1<A0A4<A0A0A0<A0A0>>>>();
            v37.VerifyA0A1A2NotInlinedGeneric<A0>();
            A0A1A2<A0A0> v38 = new A0A1A2<A0A0>();
            v38.VerifyA0A1A2NotInlined();
            IA2 i39 = ((IA2)(new A0A1A2<A0A0A1A1<A0A4<A0A0A0<A0A0>>>>()));
            i39.VerifyInterfaceIA2();
            IA2 i40 = ((IA2)(new A0A1A2<A0A0>()));
            i40.VerifyInterfaceGenericIA2<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>();
            A0A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>.VerifyA0A0A0A0NotInlinedGenericStatic<A0A0>();
            A0A0A0A0<A0A1<A0A1A2<A0A1A2<A0A0>>>>.VerifyA0A0A0A0NotInlinedStatic();
            A0A0A0A0<A0A1<A0A1A2<A0A1A2<A0A0>>>>.VerifyA0A0A0A0GenericStatic<A0A0A3<A0A3>>();
            A0A0A0A0<A0A3>.VerifyA0A0A0A0Static();
            A0A0A0A0<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>> v41 = new A0A0A0A0<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>();
            v41.VerifyA0A0A0A0NotInlinedGeneric<A0A0>();
            A0A0A0A0<A0A0A0A0<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>> v42 = new A0A0A0A0<A0A0A0A0<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>();
            v42.VerifyA0A0A0A0NotInlined();
            IA2 i43 = ((IA2)(new A0A0A0A0<A0A0A1>()));
            i43.VerifyInterfaceIA2();
            IA2 i44 = ((IA2)(new A0A0A0A0<A0A0>()));
            i44.VerifyInterfaceGenericIA2<A0A0>();
            A0A4<A0A0A1>.VerifyA0A4NotInlinedGenericStatic<A0A1<A0A1A2<A0A1A2<A0A0>>>>();
            A0A4<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>.VerifyA0A4NotInlinedStatic();
            A0A4<A0A0A0A0<A0A0>>.VerifyA0A4GenericStatic<A0A4<A0A0A0A0<A0A0>>>();
            A0A4<A0A0>.VerifyA0A4Static();
            A0A4<A0A0A1> v45 = new A0A4<A0A0A1>();
            v45.VerifyA0A4NotInlinedGeneric<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>();
            A0A4<A0A3> v46 = new A0A4<A0A3>();
            v46.VerifyA0A4NotInlined();
            A0A0A3<A0A0A0A0<A0A0>>.VerifyA0A0A3NotInlinedGenericStatic<A0A3>();
            A0A0A3<A0A3A4<A0A3>>.VerifyA0A0A3NotInlinedStatic();
            A0A0A3<A0A0A1A1<A0A4<A0A0A0<A0A0>>>>.VerifyA0A0A3GenericStatic<A0A1A2<A0A0>>();
            A0A0A3<A0A3>.VerifyA0A0A3Static();
            A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>> v47 = new A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>();
            v47.VerifyA0A0A3NotInlinedGeneric<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>();
            A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>> v48 = new A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>();
            v48.VerifyA0A0A3NotInlined();
            A0A3A4<A0A4<A0A3>>.VerifyA0A3A4NotInlinedGenericStatic<A0A1A2<A0A0>>();
            A0A3A4<A0A0A1A1<A0A4<A0A0A0<A0A0>>>>.VerifyA0A3A4NotInlinedStatic();
            A0A3A4<A0A0A1A1<A0A4<A0A0A0<A0A0>>>>.VerifyA0A3A4GenericStatic<A0A4<A0A3>>();
            A0A3A4<A0>.VerifyA0A3A4Static();
            A0A3A4<A0A1A2<A0A0>> v49 = new A0A3A4<A0A1A2<A0A0>>();
            v49.VerifyA0A3A4NotInlinedGeneric<A0A0>();
            A0A3A4<A0A4<A0A3>> v50 = new A0A3A4<A0A4<A0A3>>();
            v50.VerifyA0A3A4NotInlined();
            A0A0A1A1<A0A0>.VerifyA0A0A1A1NotInlinedGenericStatic<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>();
            A0A0A1A1<A0A0A0A0<A0A0>>.VerifyA0A0A1A1NotInlinedStatic();
            A0A0A1A1<A0A3>.VerifyA0A0A1A1GenericStatic<A0A1A2<A0A0>>();
            A0A0A1A1<A0A1A2<A0A0>>.VerifyA0A0A1A1Static();
            A0A0A1A1<A0A3A4<A0A4<A0A3>>> v51 = new A0A0A1A1<A0A3A4<A0A4<A0A3>>>();
            v51.VerifyA0A0A1A1NotInlinedGeneric<A0A3>();
            A0A0A1A1<A0A1<A0A1A2<A0A1A2<A0A0>>>> v52 = new A0A0A1A1<A0A1<A0A1A2<A0A1A2<A0A0>>>>();
            v52.VerifyA0A0A1A1NotInlined();
        }
    }

    public class A0A0A0A0<T0> : A0A0A0<T0>, IA2
        where T0 : new()
    {

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void VerifyA0A0A0A0NotInlinedGenericStatic<T>()
            where T : new()
        {
            System.Console.WriteLine(typeof(T));
            T0 t1 = new T0();
            T t2 = new T();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void VerifyA0A0A0A0NotInlinedStatic()
        {
            T0 t0 = new T0();
        }

        public static void VerifyA0A0A0A0GenericStatic<T>()
            where T : new()
        {
            System.Console.WriteLine(typeof(T));
            T0 t1 = new T0();
            T t2 = new T();
        }

        public static void VerifyA0A0A0A0Static()
        {
            T0 t0 = new T0();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void VerifyA0A0A0A0NotInlinedGeneric<T>()
            where T : new()
        {
            System.Console.WriteLine(this);
            System.Console.WriteLine(typeof(T));
            T0 t2 = new T0();
            T t3 = new T();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void VerifyA0A0A0A0NotInlined()
        {
            System.Console.WriteLine(this);
            T0 t1 = new T0();
        }

        public void RecurseA0A0A0A0(int depth)
        {
            if ((depth < 0))
            {
                return;
            }
            System.Console.Write(".");
            A0A1A2<A0A0A1> next = new A0A1A2<A0A0A1>();
            next.RecurseA0A1A2((depth - 1));
        }

        public void CreateAllTypesA0A0A0A0()
        {
            A0 v0 = new A0();
            v0.VerifyInterfaceIA1();
            A0 v1 = new A0();
            v1.VerifyInterfaceGenericIA1<A0A0A0A0<A0A0A3<A0A3>>>();
            A0 v2 = new A0();
            v2.VerifyInterfaceIA2();
            A0 v3 = new A0();
            v3.VerifyInterfaceGenericIA2<A0A1A2<A0A4<A0A0A0<A0A0A0<A0A0>>>>>();
            A0.VerifyA0NotInlinedGenericStatic<A0A3A4<A0A4<A0A0A1>>>();
            A0.VerifyA0NotInlinedStatic();
            A0.VerifyA0GenericStatic<A0A0A0<A0A0A1>>();
            A0.VerifyA0Static();
            A0 v4 = new A0();
            v4.VerifyA0NotInlinedGeneric<A0>();
            A0 v5 = new A0();
            v5.VerifyA0NotInlined();
            A0 v6 = new A0();
            v6.VirtualVerifyGeneric<IA2>();
            A0 v7 = new A0();
            v7.VirtualVerify();
            A0 v8 = new A0();
            v8.DeepRecursion();
            IA1 i9 = ((IA1)(new A0()));
            i9.VerifyInterfaceIA1();
            IA1 i10 = ((IA1)(new A0()));
            i10.VerifyInterfaceGenericIA1<A0A1<A0A0A1>>();
            IA2 i11 = ((IA2)(new A0()));
            i11.VerifyInterfaceIA2();
            IA2 i12 = ((IA2)(new A0()));
            i12.VerifyInterfaceGenericIA2<A0A0A1A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>();
            A0A0.VerifyA0A0NotInlinedGenericStatic<A0A0A1A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>();
            A0A0.VerifyA0A0NotInlinedStatic();
            A0A0.VerifyA0A0GenericStatic<A0A0A0A0<A0A0A3<A0A3>>>();
            A0A0.VerifyA0A0Static();
            A0A0 v13 = new A0A0();
            v13.VerifyA0A0NotInlinedGeneric<A0A1<A0A0A1>>();
            A0A0 v14 = new A0A0();
            v14.VerifyA0A0NotInlined();
            A0A0 v15 = new A0A0();
            v15.VirtualVerifyGeneric<A0A0A0<A0A0A1>>();
            A0A0 v16 = new A0A0();
            v16.VirtualVerify();
            IA1 i17 = ((IA1)(new A0A0()));
            i17.VerifyInterfaceIA1();
            IA1 i18 = ((IA1)(new A0A0()));
            i18.VerifyInterfaceGenericIA1<A0>();
            IA2 i19 = ((IA2)(new A0A0()));
            i19.VerifyInterfaceIA2();
            IA2 i20 = ((IA2)(new A0A0()));
            i20.VerifyInterfaceGenericIA2<A0A0>();
            A0A1<A0A0A1A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>.VerifyA0A1NotInlinedGenericStatic<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>();
            A0A1<A0A0A0<A0A0A1>>.VerifyA0A1NotInlinedStatic();
            A0A1<A0A0>.VerifyA0A1GenericStatic<A0A0A1>();
            A0A1<A0A1<A0A0>>.VerifyA0A1Static();
            A0A1<A0A0A0A0<A0A0A3<A0A3>>> v21 = new A0A1<A0A0A0A0<A0A0A3<A0A3>>>();
            v21.VerifyA0A1NotInlinedGeneric<A0A0A1>();
            A0A1<A0A0A1A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>> v22 = new A0A1<A0A0A1A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>();
            v22.VerifyA0A1NotInlined();
            IA2 i23 = ((IA2)(new A0A1<A0A0A0<A0A0A1>>()));
            i23.VerifyInterfaceIA2();
            IA2 i24 = ((IA2)(new A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>()));
            i24.VerifyInterfaceGenericIA2<A0A0>();
            A0A0A0<A0A3A4<A0A4<A0A0A1>>>.VerifyA0A0A0NotInlinedGenericStatic<A0A0A0<A0A3A4<A0A4<A0A0A1>>>>();
            A0A0A0<A0A0A0A0<A0A0A3<A0A3>>>.VerifyA0A0A0NotInlinedStatic();
            A0A0A0<A0A0A0<A0A0A0A0<A0A0A3<A0A3>>>>.VerifyA0A0A0GenericStatic<A0A4<A0A0A1>>();
            A0A0A0<A0A1A2<A0A4<A0A0A0<A0A0A0<A0A0>>>>>.VerifyA0A0A0Static();
            A0A0A0<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>> v25 = new A0A0A0<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>();
            v25.VerifyA0A0A0NotInlinedGeneric<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>();
            A0A0A0<A0A3A4<A0A4<A0A0A1>>> v26 = new A0A0A0<A0A3A4<A0A4<A0A0A1>>>();
            v26.VerifyA0A0A0NotInlined();
            IA2 i27 = ((IA2)(new A0A0A0<A0A0>()));
            i27.VerifyInterfaceIA2();
            IA2 i28 = ((IA2)(new A0A0A0<A0A0>()));
            i28.VerifyInterfaceGenericIA2<A0A0>();
            A0A3.VerifyA0A3NotInlinedGenericStatic<A0A0A0A0<A0A0A3<A0A3>>>();
            A0A3.VerifyA0A3NotInlinedStatic();
            A0A3.VerifyA0A3GenericStatic<A0A0A1A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>();
            A0A3.VerifyA0A3Static();
            A0A3 v29 = new A0A3();
            v29.VerifyA0A3NotInlinedGeneric<A0A1A2<A0A4<A0A0A0<A0A0A0<A0A0>>>>>();
            A0A3 v30 = new A0A3();
            v30.VerifyA0A3NotInlined();
            IA2 i31 = ((IA2)(new A0A3()));
            i31.VerifyInterfaceIA2();
            IA2 i32 = ((IA2)(new A0A3()));
            i32.VerifyInterfaceGenericIA2<A0A4<A0A0A1>>();
            A0A0A1.VerifyA0A0A1NotInlinedGenericStatic<A0A3A4<A0A4<A0A0A1>>>();
            A0A0A1.VerifyA0A0A1NotInlinedStatic();
            A0A0A1.VerifyA0A0A1GenericStatic<A0A0>();
            A0A0A1.VerifyA0A0A1Static();
            A0A0A1 v33 = new A0A0A1();
            v33.VerifyA0A0A1NotInlinedGeneric<A0>();
            A0A0A1 v34 = new A0A0A1();
            v34.VerifyA0A0A1NotInlined();
            IA2 i35 = ((IA2)(new A0A0A1()));
            i35.VerifyInterfaceIA2();
            IA2 i36 = ((IA2)(new A0A0A1()));
            i36.VerifyInterfaceGenericIA2<A0A3>();
            A0A1A2<A0A0A0<A0A0>>.VerifyA0A1A2NotInlinedGenericStatic<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>();
            A0A1A2<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>.VerifyA0A1A2NotInlinedStatic();
            A0A1A2<A0A4<A0A0A1>>.VerifyA0A1A2GenericStatic<A0A3A4<A0A4<A0A0A1>>>();
            A0A1A2<A0A0A1>.VerifyA0A1A2Static();
            A0A1A2<A0> v37 = new A0A1A2<A0>();
            v37.VerifyA0A1A2NotInlinedGeneric<A0A0A1>();
            A0A1A2<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>> v38 = new A0A1A2<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>();
            v38.VerifyA0A1A2NotInlined();
            IA2 i39 = ((IA2)(new A0A1A2<A0A0>()));
            i39.VerifyInterfaceIA2();
            IA2 i40 = ((IA2)(new A0A1A2<A0A1A2<A0A0>>()));
            i40.VerifyInterfaceGenericIA2<A0A1A2<A0A1A2<A0A0>>>();
            A0A0A0A0<A0A1A2<A0A1A2<A0A0>>>.VerifyA0A0A0A0NotInlinedGenericStatic<A0A0A1A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>();
            A0A0A0A0<A0A4<A0A0A1>>.VerifyA0A0A0A0NotInlinedStatic();
            A0A0A0A0<A0A0A1A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>.VerifyA0A0A0A0GenericStatic<A0A0A0<A0A0>>();
            A0A0A0A0<A0A0A0A0<A0A0A1A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>.VerifyA0A0A0A0Static();
            A0A0A0A0<A0A3A4<A0A4<A0A0A1>>> v41 = new A0A0A0A0<A0A3A4<A0A4<A0A0A1>>>();
            v41.VerifyA0A0A0A0NotInlinedGeneric<A0A4<A0A0A1>>();
            A0A0A0A0<A0A0A1> v42 = new A0A0A0A0<A0A0A1>();
            v42.VerifyA0A0A0A0NotInlined();
            IA2 i43 = ((IA2)(new A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>()));
            i43.VerifyInterfaceIA2();
            IA2 i44 = ((IA2)(new A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>()));
            i44.VerifyInterfaceGenericIA2<A0A3A4<A0A4<A0A0A1>>>();
            A0A4<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>.VerifyA0A4NotInlinedGenericStatic<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>();
            A0A4<A0A1A2<A0A1A2<A0A0>>>.VerifyA0A4NotInlinedStatic();
            A0A4<A0A1A2<A0A1A2<A0A0>>>.VerifyA0A4GenericStatic<A0A4<A0A1A2<A0A1A2<A0A0>>>>();
            A0A4<A0A0A1A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>.VerifyA0A4Static();
            A0A4<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>> v45 = new A0A4<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>();
            v45.VerifyA0A4NotInlinedGeneric<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>();
            A0A4<A0A0A0<A0A0>> v46 = new A0A4<A0A0A0<A0A0>>();
            v46.VerifyA0A4NotInlined();
            A0A0A3<A0A3>.VerifyA0A0A3NotInlinedGenericStatic<A0A0A3<A0A3>>();
            A0A0A3<A0A4<A0A0A0<A0A0>>>.VerifyA0A0A3NotInlinedStatic();
            A0A0A3<A0>.VerifyA0A0A3GenericStatic<A0A3A4<A0A4<A0A0A1>>>();
            A0A0A3<A0A0A1>.VerifyA0A0A3Static();
            A0A0A3<A0A0A0<A0A0>> v47 = new A0A0A3<A0A0A0<A0A0>>();
            v47.VerifyA0A0A3NotInlinedGeneric<A0A1A2<A0A1A2<A0A0>>>();
            A0A0A3<A0A3> v48 = new A0A0A3<A0A3>();
            v48.VerifyA0A0A3NotInlined();
            A0A3A4<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>.VerifyA0A3A4NotInlinedGenericStatic<A0A4<A0A0A0<A0A0>>>();
            A0A3A4<A0A1A2<A0A1A2<A0A0>>>.VerifyA0A3A4NotInlinedStatic();
            A0A3A4<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>.VerifyA0A3A4GenericStatic<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>();
            A0A3A4<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>.VerifyA0A3A4Static();
            A0A3A4<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>> v49 = new A0A3A4<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>();
            v49.VerifyA0A3A4NotInlinedGeneric<A0A1A2<A0A1A2<A0A0>>>();
            A0A3A4<A0A3> v50 = new A0A3A4<A0A3>();
            v50.VerifyA0A3A4NotInlined();
            A0A0A1A1<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>.VerifyA0A0A1A1NotInlinedGenericStatic<A0A0A0<A0A0>>();
            A0A0A1A1<A0A0A0<A0A0>>.VerifyA0A0A1A1NotInlinedStatic();
            A0A0A1A1<A0A0A1A1<A0A0A0<A0A0>>>.VerifyA0A0A1A1GenericStatic<A0>();
            A0A0A1A1<A0A0A1A1<A0A0A1A1<A0A0A0<A0A0>>>>.VerifyA0A0A1A1Static();
            A0A0A1A1<A0A0A0<A0A0>> v51 = new A0A0A1A1<A0A0A0<A0A0>>();
            v51.VerifyA0A0A1A1NotInlinedGeneric<A0>();
            A0A0A1A1<A0A4<A0A0A0<A0A0>>> v52 = new A0A0A1A1<A0A4<A0A0A0<A0A0>>>();
            v52.VerifyA0A0A1A1NotInlined();
        }
    }

    public class A0A0A1A1<T0> : A0A0A1
        where T0 : new()
    {

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void VerifyA0A0A1A1NotInlinedGenericStatic<T>()
            where T : new()
        {
            System.Console.WriteLine(typeof(T));
            T0 t1 = new T0();
            T t2 = new T();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void VerifyA0A0A1A1NotInlinedStatic()
        {
            T0 t0 = new T0();
        }

        public static void VerifyA0A0A1A1GenericStatic<T>()
            where T : new()
        {
            System.Console.WriteLine(typeof(T));
            T0 t1 = new T0();
            T t2 = new T();
        }

        public static void VerifyA0A0A1A1Static()
        {
            T0 t0 = new T0();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void VerifyA0A0A1A1NotInlinedGeneric<T>()
            where T : new()
        {
            System.Console.WriteLine(this);
            System.Console.WriteLine(typeof(T));
            T0 t2 = new T0();
            T t3 = new T();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void VerifyA0A0A1A1NotInlined()
        {
            System.Console.WriteLine(this);
            T0 t1 = new T0();
        }

        public void RecurseA0A0A1A1(int depth)
        {
            if ((depth < 0))
            {
                return;
            }
            System.Console.Write(".");
            A0A3A4<A0A0A3<A0A0A1>> next = new A0A3A4<A0A0A3<A0A0A1>>();
            next.RecurseA0A3A4((depth - 1));
        }

        public void CreateAllTypesA0A0A1A1()
        {
            A0 v0 = new A0();
            v0.VerifyInterfaceIA1();
            A0 v1 = new A0();
            v1.VerifyInterfaceGenericIA1<A0A3>();
            A0 v2 = new A0();
            v2.VerifyInterfaceIA2();
            A0 v3 = new A0();
            v3.VerifyInterfaceGenericIA2<A0A3>();
            A0.VerifyA0NotInlinedGenericStatic<A0A0>();
            A0.VerifyA0NotInlinedStatic();
            A0.VerifyA0GenericStatic<A0A0>();
            A0.VerifyA0Static();
            A0 v4 = new A0();
            v4.VerifyA0NotInlinedGeneric<A0A3>();
            A0 v5 = new A0();
            v5.VerifyA0NotInlined();
            A0 v6 = new A0();
            v6.VirtualVerifyGeneric<A0>();
            A0 v7 = new A0();
            v7.VirtualVerify();
            A0 v8 = new A0();
            v8.DeepRecursion();
            IA1 i9 = ((IA1)(new A0()));
            i9.VerifyInterfaceIA1();
            IA1 i10 = ((IA1)(new A0()));
            i10.VerifyInterfaceGenericIA1<A0A0A3<A0A0A3<A0A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>>>>();
            IA2 i11 = ((IA2)(new A0()));
            i11.VerifyInterfaceIA2();
            IA2 i12 = ((IA2)(new A0()));
            i12.VerifyInterfaceGenericIA2<A0A0A0A0<A0A0>>();
            A0A0.VerifyA0A0NotInlinedGenericStatic<A0A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>>();
            A0A0.VerifyA0A0NotInlinedStatic();
            A0A0.VerifyA0A0GenericStatic<A0A3A4<A0A0>>();
            A0A0.VerifyA0A0Static();
            A0A0 v13 = new A0A0();
            v13.VerifyA0A0NotInlinedGeneric<A0A0A3<A0A0A3<A0A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>>>>();
            A0A0 v14 = new A0A0();
            v14.VerifyA0A0NotInlined();
            A0A0 v15 = new A0A0();
            v15.VirtualVerifyGeneric<A0A1A2<A0A3>>();
            A0A0 v16 = new A0A0();
            v16.VirtualVerify();
            IA1 i17 = ((IA1)(new A0A0()));
            i17.VerifyInterfaceIA1();
            IA1 i18 = ((IA1)(new A0A0()));
            i18.VerifyInterfaceGenericIA1<A0>();
            IA2 i19 = ((IA2)(new A0A0()));
            i19.VerifyInterfaceIA2();
            IA2 i20 = ((IA2)(new A0A0()));
            i20.VerifyInterfaceGenericIA2<A0A3A4<A0A0>>();
            A0A1<A0A3>.VerifyA0A1NotInlinedGenericStatic<A0A4<A0A0A1>>();
            A0A1<A0A0A0A0<A0A0>>.VerifyA0A1NotInlinedStatic();
            A0A1<A0A0A1>.VerifyA0A1GenericStatic<A0A0A1A1<A0A1A2<A0A3>>>();
            A0A1<A0A3>.VerifyA0A1Static();
            A0A1<A0A0A1A1<A0A1A2<A0A3>>> v21 = new A0A1<A0A0A1A1<A0A1A2<A0A3>>>();
            v21.VerifyA0A1NotInlinedGeneric<A0A0A0A0<A0A0>>();
            A0A1<A0A0A1A1<A0A1A2<A0A3>>> v22 = new A0A1<A0A0A1A1<A0A1A2<A0A3>>>();
            v22.VerifyA0A1NotInlined();
            IA2 i23 = ((IA2)(new A0A1<A0A0A0<A0A3>>()));
            i23.VerifyInterfaceIA2();
            IA2 i24 = ((IA2)(new A0A1<A0A4<A0A0A1>>()));
            i24.VerifyInterfaceGenericIA2<A0A0A0A0<A0A0>>();
            A0A0A0<A0A3>.VerifyA0A0A0NotInlinedGenericStatic<A0>();
            A0A0A0<A0>.VerifyA0A0A0NotInlinedStatic();
            A0A0A0<A0A0A1>.VerifyA0A0A0GenericStatic<A0A0A1>();
            A0A0A0<A0A1A2<A0A3>>.VerifyA0A0A0Static();
            A0A0A0<A0A1<A0A4<A0A0A1>>> v25 = new A0A0A0<A0A1<A0A4<A0A0A1>>>();
            v25.VerifyA0A0A0NotInlinedGeneric<A0A0A1A1<A0A1A2<A0A3>>>();
            A0A0A0<A0A0A3<A0A0A3<A0A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>>>> v26 = new A0A0A0<A0A0A3<A0A0A3<A0A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>>>>();
            v26.VerifyA0A0A0NotInlined();
            IA2 i27 = ((IA2)(new A0A0A0<A0A0>()));
            i27.VerifyInterfaceIA2();
            IA2 i28 = ((IA2)(new A0A0A0<A0A0A0A0<A0A0>>()));
            i28.VerifyInterfaceGenericIA2<A0A0>();
            A0A3.VerifyA0A3NotInlinedGenericStatic<A0A0A0<A0A0A0A0<A0A0>>>();
            A0A3.VerifyA0A3NotInlinedStatic();
            A0A3.VerifyA0A3GenericStatic<A0A0A0<A0A0A0A0<A0A0>>>();
            A0A3.VerifyA0A3Static();
            A0A3 v29 = new A0A3();
            v29.VerifyA0A3NotInlinedGeneric<A0A3>();
            A0A3 v30 = new A0A3();
            v30.VerifyA0A3NotInlined();
            IA2 i31 = ((IA2)(new A0A3()));
            i31.VerifyInterfaceIA2();
            IA2 i32 = ((IA2)(new A0A3()));
            i32.VerifyInterfaceGenericIA2<A0A1A2<A0A3>>();
            A0A0A1.VerifyA0A0A1NotInlinedGenericStatic<A0A1<A0A4<A0A0A1>>>();
            A0A0A1.VerifyA0A0A1NotInlinedStatic();
            A0A0A1.VerifyA0A0A1GenericStatic<A0A3>();
            A0A0A1.VerifyA0A0A1Static();
            A0A0A1 v33 = new A0A0A1();
            v33.VerifyA0A0A1NotInlinedGeneric<A0A0A1>();
            A0A0A1 v34 = new A0A0A1();
            v34.VerifyA0A0A1NotInlined();
            IA2 i35 = ((IA2)(new A0A0A1()));
            i35.VerifyInterfaceIA2();
            IA2 i36 = ((IA2)(new A0A0A1()));
            i36.VerifyInterfaceGenericIA2<A0A4<A0A0A1>>();
            A0A1A2<A0A4<A0A0A1>>.VerifyA0A1A2NotInlinedGenericStatic<A0A0>();
            A0A1A2<A0A0A1A1<A0A1A2<A0A3>>>.VerifyA0A1A2NotInlinedStatic();
            A0A1A2<A0A0A1A1<A0A1A2<A0A3>>>.VerifyA0A1A2GenericStatic<A0A4<A0A0A1>>();
            A0A1A2<A0A0A1>.VerifyA0A1A2Static();
            A0A1A2<A0A1<A0A4<A0A0A1>>> v37 = new A0A1A2<A0A1<A0A4<A0A0A1>>>();
            v37.VerifyA0A1A2NotInlinedGeneric<A0A4<A0A0A1>>();
            A0A1A2<A0A1A2<A0A1<A0A4<A0A0A1>>>> v38 = new A0A1A2<A0A1A2<A0A1<A0A4<A0A0A1>>>>();
            v38.VerifyA0A1A2NotInlined();
            IA2 i39 = ((IA2)(new A0A1A2<A0A0A3<A0A0A3<A0A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>>>>()));
            i39.VerifyInterfaceIA2();
            IA2 i40 = ((IA2)(new A0A1A2<A0A3>()));
            i40.VerifyInterfaceGenericIA2<A0A3A4<A0A0>>();
            A0A0A0A0<A0A3>.VerifyA0A0A0A0NotInlinedGenericStatic<A0A4<A0A0A1>>();
            A0A0A0A0<A0A4<A0A0A1>>.VerifyA0A0A0A0NotInlinedStatic();
            A0A0A0A0<A0A4<A0A0A1>>.VerifyA0A0A0A0GenericStatic<A0A1A2<A0A3>>();
            A0A0A0A0<A0A4<A0A0A1>>.VerifyA0A0A0A0Static();
            A0A0A0A0<A0A3> v41 = new A0A0A0A0<A0A3>();
            v41.VerifyA0A0A0A0NotInlinedGeneric<A0A1<A0A4<A0A0A1>>>();
            A0A0A0A0<A0A0A0<A0A0A0A0<A0A0>>> v42 = new A0A0A0A0<A0A0A0<A0A0A0A0<A0A0>>>();
            v42.VerifyA0A0A0A0NotInlined();
            IA2 i43 = ((IA2)(new A0A0A0A0<A0A0>()));
            i43.VerifyInterfaceIA2();
            IA2 i44 = ((IA2)(new A0A0A0A0<A0A0>()));
            i44.VerifyInterfaceGenericIA2<A0A0A0<A0A0A0A0<A0A0>>>();
            A0A4<A0>.VerifyA0A4NotInlinedGenericStatic<A0A0A1>();
            A0A4<A0A1<A0A4<A0A0A1>>>.VerifyA0A4NotInlinedStatic();
            A0A4<A0A4<A0A1<A0A4<A0A0A1>>>>.VerifyA0A4GenericStatic<A0A0A1A1<A0A1A2<A0A3>>>();
            A0A4<A0A0A1>.VerifyA0A4Static();
            A0A4<A0A3> v45 = new A0A4<A0A3>();
            v45.VerifyA0A4NotInlinedGeneric<A0A0A0A0<A0A0>>();
            A0A4<A0A0A3<A0A0A3<A0A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>>>> v46 = new A0A4<A0A0A3<A0A0A3<A0A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>>>>();
            v46.VerifyA0A4NotInlined();
            A0A0A3<A0>.VerifyA0A0A3NotInlinedGenericStatic<A0A0A1A1<A0A1A2<A0A3>>>();
            A0A0A3<A0A1<A0A4<A0A0A1>>>.VerifyA0A0A3NotInlinedStatic();
            A0A0A3<A0A0>.VerifyA0A0A3GenericStatic<A0>();
            A0A0A3<A0A0A3<A0A0>>.VerifyA0A0A3Static();
            A0A0A3<A0> v47 = new A0A0A3<A0>();
            v47.VerifyA0A0A3NotInlinedGeneric<A0A3>();
            A0A0A3<A0A0A1> v48 = new A0A0A3<A0A0A1>();
            v48.VerifyA0A0A3NotInlined();
            A0A3A4<A0A3>.VerifyA0A3A4NotInlinedGenericStatic<A0A0A1A1<A0A1A2<A0A3>>>();
            A0A3A4<A0A0A3<A0A0A1>>.VerifyA0A3A4NotInlinedStatic();
            A0A3A4<A0A4<A0A0A3<A0A0A3<A0A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>>>>>.VerifyA0A3A4GenericStatic<A0A0A3<A0A0A1>>();
            A0A3A4<A0A1A2<A0A3>>.VerifyA0A3A4Static();
            A0A3A4<A0A1A2<A0A3>> v49 = new A0A3A4<A0A1A2<A0A3>>();
            v49.VerifyA0A3A4NotInlinedGeneric<A0A0A0A0<A0A0>>();
            A0A3A4<A0A0A1A1<A0A1A2<A0A3>>> v50 = new A0A3A4<A0A0A1A1<A0A1A2<A0A3>>>();
            v50.VerifyA0A3A4NotInlined();
            A0A0A1A1<A0A1A2<A0A3>>.VerifyA0A0A1A1NotInlinedGenericStatic<A0A0A1A1<A0A1A2<A0A3>>>();
            A0A0A1A1<A0A1<A0A4<A0A0A1>>>.VerifyA0A0A1A1NotInlinedStatic();
            A0A0A1A1<A0A3>.VerifyA0A0A1A1GenericStatic<A0A0A0A0<A0A0>>();
            A0A0A1A1<A0A0A1A1<A0A3>>.VerifyA0A0A1A1Static();
            A0A0A1A1<A0A4<A0A0A3<A0A0A3<A0A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>>>>> v51 = new A0A0A1A1<A0A4<A0A0A3<A0A0A3<A0A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>>>>>();
            v51.VerifyA0A0A1A1NotInlinedGeneric<A0A3A4<A0A0A1A1<A0A1A2<A0A3>>>>();
            A0A0A1A1<A0A0A0<A0A0A0A0<A0A0>>> v52 = new A0A0A1A1<A0A0A0<A0A0A0A0<A0A0>>>();
            v52.VerifyA0A0A1A1NotInlined();
        }
    }

    public class A0A0A0<T0> : A0A0, IA2
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
            v1.VerifyInterfaceGenericIA1<A0A0A1A1<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>();
            A0 v2 = new A0();
            v2.VerifyInterfaceIA2();
            A0 v3 = new A0();
            v3.VerifyInterfaceGenericIA2<A0A3A4<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>>();
            A0.VerifyA0NotInlinedGenericStatic<A0>();
            A0.VerifyA0NotInlinedStatic();
            A0.VerifyA0GenericStatic<A0A0A3<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>>();
            A0.VerifyA0Static();
            A0 v4 = new A0();
            v4.VerifyA0NotInlinedGeneric<A0A3A4<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>>();
            A0 v5 = new A0();
            v5.VerifyA0NotInlined();
            A0 v6 = new A0();
            v6.VirtualVerifyGeneric<IA1A5<A0A0A1>>();
            A0 v7 = new A0();
            v7.VirtualVerify();
            A0 v8 = new A0();
            v8.DeepRecursion();
            IA1 i9 = ((IA1)(new A0()));
            i9.VerifyInterfaceIA1();
            IA1 i10 = ((IA1)(new A0()));
            i10.VerifyInterfaceGenericIA1<A0>();
            IA2 i11 = ((IA2)(new A0()));
            i11.VerifyInterfaceIA2();
            IA2 i12 = ((IA2)(new A0()));
            i12.VerifyInterfaceGenericIA2<A0A0A1>();
            A0A0.VerifyA0A0NotInlinedGenericStatic<A0A0A1>();
            A0A0.VerifyA0A0NotInlinedStatic();
            A0A0.VerifyA0A0GenericStatic<A0A0>();
            A0A0.VerifyA0A0Static();
            A0A0 v13 = new A0A0();
            v13.VerifyA0A0NotInlinedGeneric<A0A0A1A1<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>();
            A0A0 v14 = new A0A0();
            v14.VerifyA0A0NotInlined();
            A0A0 v15 = new A0A0();
            v15.VirtualVerifyGeneric<A0A1A2<A0A0A3<A0A0A1>>>();
            A0A0 v16 = new A0A0();
            v16.VirtualVerify();
            IA1 i17 = ((IA1)(new A0A0()));
            i17.VerifyInterfaceIA1();
            IA1 i18 = ((IA1)(new A0A0()));
            i18.VerifyInterfaceGenericIA1<A0>();
            IA2 i19 = ((IA2)(new A0A0()));
            i19.VerifyInterfaceIA2();
            IA2 i20 = ((IA2)(new A0A0()));
            i20.VerifyInterfaceGenericIA2<A0A0A1A1<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>();
            A0A1<A0A0A3<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>>.VerifyA0A1NotInlinedGenericStatic<A0A3>();
            A0A1<A0A0A1>.VerifyA0A1NotInlinedStatic();
            A0A1<A0>.VerifyA0A1GenericStatic<A0>();
            A0A1<A0A0A1A1<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>.VerifyA0A1Static();
            A0A1<A0A0A1A1<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>> v21 = new A0A1<A0A0A1A1<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>();
            v21.VerifyA0A1NotInlinedGeneric<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>();
            A0A1<A0A1<A0A0A1A1<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>> v22 = new A0A1<A0A1<A0A0A1A1<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>>();
            v22.VerifyA0A1NotInlined();
            IA2 i23 = ((IA2)(new A0A1<A0A0>()));
            i23.VerifyInterfaceIA2();
            IA2 i24 = ((IA2)(new A0A1<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>()));
            i24.VerifyInterfaceGenericIA2<A0>();
            A0A0A0<A0A4<A0A0>>.VerifyA0A0A0NotInlinedGenericStatic<A0A3A4<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>>();
            A0A0A0<A0A3A4<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>>.VerifyA0A0A0NotInlinedStatic();
            A0A0A0<A0A0A0<A0A3A4<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>>>.VerifyA0A0A0GenericStatic<A0A0A0<A0A0A0<A0A3A4<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>>>>();
            A0A0A0<A0A0A1>.VerifyA0A0A0Static();
            A0A0A0<A0A3A4<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>> v25 = new A0A0A0<A0A3A4<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>>();
            v25.VerifyA0A0A0NotInlinedGeneric<A0A0>();
            A0A0A0<A0A0A1> v26 = new A0A0A0<A0A0A1>();
            v26.VerifyA0A0A0NotInlined();
            IA2 i27 = ((IA2)(new A0A0A0<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>()));
            i27.VerifyInterfaceIA2();
            IA2 i28 = ((IA2)(new A0A0A0<A0A1A2<A0A0A3<A0A0A1>>>()));
            i28.VerifyInterfaceGenericIA2<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>();
            A0A3.VerifyA0A3NotInlinedGenericStatic<A0A0A0<A0A1A2<A0A0A3<A0A0A1>>>>();
            A0A3.VerifyA0A3NotInlinedStatic();
            A0A3.VerifyA0A3GenericStatic<A0A1A2<A0A0A3<A0A0A1>>>();
            A0A3.VerifyA0A3Static();
            A0A3 v29 = new A0A3();
            v29.VerifyA0A3NotInlinedGeneric<A0A0A0<A0A1A2<A0A0A3<A0A0A1>>>>();
            A0A3 v30 = new A0A3();
            v30.VerifyA0A3NotInlined();
            IA2 i31 = ((IA2)(new A0A3()));
            i31.VerifyInterfaceIA2();
            IA2 i32 = ((IA2)(new A0A3()));
            i32.VerifyInterfaceGenericIA2<A0A0A0<A0A1A2<A0A0A3<A0A0A1>>>>();
            A0A0A1.VerifyA0A0A1NotInlinedGenericStatic<A0A3A4<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>>();
            A0A0A1.VerifyA0A0A1NotInlinedStatic();
            A0A0A1.VerifyA0A0A1GenericStatic<A0A1A2<A0A0A3<A0A0A1>>>();
            A0A0A1.VerifyA0A0A1Static();
            A0A0A1 v33 = new A0A0A1();
            v33.VerifyA0A0A1NotInlinedGeneric<A0A4<A0A0>>();
            A0A0A1 v34 = new A0A0A1();
            v34.VerifyA0A0A1NotInlined();
            IA2 i35 = ((IA2)(new A0A0A1()));
            i35.VerifyInterfaceIA2();
            IA2 i36 = ((IA2)(new A0A0A1()));
            i36.VerifyInterfaceGenericIA2<A0A3A4<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>>();
            A0A1A2<A0A0A0<A0A1A2<A0A0A3<A0A0A1>>>>.VerifyA0A1A2NotInlinedGenericStatic<A0A3>();
            A0A1A2<A0A1A2<A0A0A0<A0A1A2<A0A0A3<A0A0A1>>>>>.VerifyA0A1A2NotInlinedStatic();
            A0A1A2<A0A3>.VerifyA0A1A2GenericStatic<A0>();
            A0A1A2<A0>.VerifyA0A1A2Static();
            A0A1A2<A0A1A2<A0>> v37 = new A0A1A2<A0A1A2<A0>>();
            v37.VerifyA0A1A2NotInlinedGeneric<A0A0A3<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>>();
            A0A1A2<A0A1<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>> v38 = new A0A1A2<A0A1<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>>();
            v38.VerifyA0A1A2NotInlined();
            IA2 i39 = ((IA2)(new A0A1A2<A0A0A0<A0A1A2<A0A0A3<A0A0A1>>>>()));
            i39.VerifyInterfaceIA2();
            IA2 i40 = ((IA2)(new A0A1A2<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>()));
            i40.VerifyInterfaceGenericIA2<A0A0A1A1<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>();
            A0A0A0A0<A0A1A2<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>>.VerifyA0A0A0A0NotInlinedGenericStatic<A0A0A3<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>>();
            A0A0A0A0<A0A0>.VerifyA0A0A0A0NotInlinedStatic();
            A0A0A0A0<A0A1<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>>.VerifyA0A0A0A0GenericStatic<A0A1A2<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>>();
            A0A0A0A0<A0A1<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>>.VerifyA0A0A0A0Static();
            A0A0A0A0<A0A3A4<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>> v41 = new A0A0A0A0<A0A3A4<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>>();
            v41.VerifyA0A0A0A0NotInlinedGeneric<A0A0A1>();
            A0A0A0A0<A0A1A2<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>> v42 = new A0A0A0A0<A0A1A2<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>>();
            v42.VerifyA0A0A0A0NotInlined();
            IA2 i43 = ((IA2)(new A0A0A0A0<A0A0A1A1<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>()));
            i43.VerifyInterfaceIA2();
            IA2 i44 = ((IA2)(new A0A0A0A0<A0A0A1>()));
            i44.VerifyInterfaceGenericIA2<A0A1<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>>();
            A0A4<A0A3A4<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>>.VerifyA0A4NotInlinedGenericStatic<A0A0A3<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>>();
            A0A4<A0A0A3<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>>.VerifyA0A4NotInlinedStatic();
            A0A4<A0A1A2<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>>.VerifyA0A4GenericStatic<A0A0A0A0<A0A0A1>>();
            A0A4<A0A4<A0A1A2<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>>>.VerifyA0A4Static();
            A0A4<A0A0A3<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>> v45 = new A0A4<A0A0A3<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>>();
            v45.VerifyA0A4NotInlinedGeneric<A0A4<A0A0A3<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>>>();
            A0A4<A0A0A1> v46 = new A0A4<A0A0A1>();
            v46.VerifyA0A4NotInlined();
            A0A0A3<A0A0A1>.VerifyA0A0A3NotInlinedGenericStatic<A0A0A3<A0A0A1>>();
            A0A0A3<A0A0A0<A0A1A2<A0A0A3<A0A0A1>>>>.VerifyA0A0A3NotInlinedStatic();
            A0A0A3<A0A3>.VerifyA0A0A3GenericStatic<A0A0A1>();
            A0A0A3<A0A1<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>>.VerifyA0A0A3Static();
            A0A0A3<A0A0A0<A0A1A2<A0A0A3<A0A0A1>>>> v47 = new A0A0A3<A0A0A0<A0A1A2<A0A0A3<A0A0A1>>>>();
            v47.VerifyA0A0A3NotInlinedGeneric<A0A0A1A1<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>();
            A0A0A3<A0A0> v48 = new A0A0A3<A0A0>();
            v48.VerifyA0A0A3NotInlined();
            A0A3A4<A0A3A4<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>>.VerifyA0A3A4NotInlinedGenericStatic<A0A1<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>>();
            A0A3A4<A0A0A1A1<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>.VerifyA0A3A4NotInlinedStatic();
            A0A3A4<A0A0A0A0<A0A0A1>>.VerifyA0A3A4GenericStatic<A0A0>();
            A0A3A4<A0A1A2<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>>.VerifyA0A3A4Static();
            A0A3A4<A0A3> v49 = new A0A3A4<A0A3>();
            v49.VerifyA0A3A4NotInlinedGeneric<A0>();
            A0A3A4<A0A4<A0A0A1>> v50 = new A0A3A4<A0A4<A0A0A1>>();
            v50.VerifyA0A3A4NotInlined();
            A0A0A1A1<A0A4<A0A0A1>>.VerifyA0A0A1A1NotInlinedGenericStatic<A0A4<A0A0A1>>();
            A0A0A1A1<A0A1<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>>.VerifyA0A0A1A1NotInlinedStatic();
            A0A0A1A1<A0A0A0<A0A1A2<A0A0A3<A0A0A1>>>>.VerifyA0A0A1A1GenericStatic<A0A0A0<A0A1A2<A0A0A3<A0A0A1>>>>();
            A0A0A1A1<A0A3A4<A0A4<A0A0A1>>>.VerifyA0A0A1A1Static();
            A0A0A1A1<A0A0A0<A0A1A2<A0A0A3<A0A0A1>>>> v51 = new A0A0A1A1<A0A0A0<A0A1A2<A0A0A3<A0A0A1>>>>();
            v51.VerifyA0A0A1A1NotInlinedGeneric<A0A1A2<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>>();
            A0A0A1A1<A0A1<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>> v52 = new A0A0A1A1<A0A1<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>>();
            v52.VerifyA0A0A1A1NotInlined();
        }
    }

    public class A0A3 : A0, IA2
    {

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void VerifyA0A3NotInlinedGenericStatic<T>()
            where T : new()
        {
            System.Console.WriteLine(typeof(T));
            T t1 = new T();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void VerifyA0A3NotInlinedStatic()
        {
        }

        public static void VerifyA0A3GenericStatic<T>()
            where T : new()
        {
            System.Console.WriteLine(typeof(T));
            T t1 = new T();
        }

        public static void VerifyA0A3Static()
        {
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void VerifyA0A3NotInlinedGeneric<T>()
            where T : new()
        {
            System.Console.WriteLine(this);
            System.Console.WriteLine(typeof(T));
            T t2 = new T();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void VerifyA0A3NotInlined()
        {
            System.Console.WriteLine(this);
        }

        public void RecurseA0A3(int depth)
        {
            if ((depth < 0))
            {
                return;
            }
            System.Console.Write(".");
            A0A0A0<A0A1<A0A0A0<A0>>> next = new A0A0A0<A0A1<A0A0A0<A0>>>();
            next.RecurseA0A0A0((depth - 1));
        }

        public void CreateAllTypesA0A3()
        {
            A0 v0 = new A0();
            v0.VerifyInterfaceIA1();
            A0 v1 = new A0();
            v1.VerifyInterfaceGenericIA1<A0A1<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>>();
            A0 v2 = new A0();
            v2.VerifyInterfaceIA2();
            A0 v3 = new A0();
            v3.VerifyInterfaceGenericIA2<A0A0A1A1<A0A1<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>>>();
            A0.VerifyA0NotInlinedGenericStatic<A0A0>();
            A0.VerifyA0NotInlinedStatic();
            A0.VerifyA0GenericStatic<A0A0A1>();
            A0.VerifyA0Static();
            A0 v4 = new A0();
            v4.VerifyA0NotInlinedGeneric<A0A1A2<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>>();
            A0 v5 = new A0();
            v5.VerifyA0NotInlined();
            A0 v6 = new A0();
            v6.VirtualVerifyGeneric<A0A0A1A1<A0A1<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>>>();
            A0 v7 = new A0();
            v7.VirtualVerify();
            A0 v8 = new A0();
            v8.DeepRecursion();
            IA1 i9 = ((IA1)(new A0()));
            i9.VerifyInterfaceIA1();
            IA1 i10 = ((IA1)(new A0()));
            i10.VerifyInterfaceGenericIA1<A0A1A2<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>>();
            IA2 i11 = ((IA2)(new A0()));
            i11.VerifyInterfaceIA2();
            IA2 i12 = ((IA2)(new A0()));
            i12.VerifyInterfaceGenericIA2<A0>();
            A0A0.VerifyA0A0NotInlinedGenericStatic<A0A0A1A1<A0A1<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>>>();
            A0A0.VerifyA0A0NotInlinedStatic();
            A0A0.VerifyA0A0GenericStatic<A0A0A3<A0A0>>();
            A0A0.VerifyA0A0Static();
            A0A0 v13 = new A0A0();
            v13.VerifyA0A0NotInlinedGeneric<A0A3A4<A0A4<A0A0A1>>>();
            A0A0 v14 = new A0A0();
            v14.VerifyA0A0NotInlined();
            A0A0 v15 = new A0A0();
            v15.VirtualVerifyGeneric<A0A0>();
            A0A0 v16 = new A0A0();
            v16.VirtualVerify();
            IA1 i17 = ((IA1)(new A0A0()));
            i17.VerifyInterfaceIA1();
            IA1 i18 = ((IA1)(new A0A0()));
            i18.VerifyInterfaceGenericIA1<A0A0A1A1<A0A1<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>>>();
            IA2 i19 = ((IA2)(new A0A0()));
            i19.VerifyInterfaceIA2();
            IA2 i20 = ((IA2)(new A0A0()));
            i20.VerifyInterfaceGenericIA2<A0A1A2<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>>();
            A0A1<A0A1A2<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>>.VerifyA0A1NotInlinedGenericStatic<A0A0A1>();
            A0A1<A0A0>.VerifyA0A1NotInlinedStatic();
            A0A1<A0A0A0<A0A1A2<A0A0A3<A0A0A1>>>>.VerifyA0A1GenericStatic<A0A1<A0A0A0<A0A1A2<A0A0A3<A0A0A1>>>>>();
            A0A1<A0A1<A0A0A0<A0A1A2<A0A0A3<A0A0A1>>>>>.VerifyA0A1Static();
            A0A1<A0A4<A0A0A1>> v21 = new A0A1<A0A4<A0A0A1>>();
            v21.VerifyA0A1NotInlinedGeneric<A0A0A0<A0A1A2<A0A0A3<A0A0A1>>>>();
            A0A1<A0A0> v22 = new A0A1<A0A0>();
            v22.VerifyA0A1NotInlined();
            IA2 i23 = ((IA2)(new A0A1<A0A0A0A0<A0A0A1>>()));
            i23.VerifyInterfaceIA2();
            IA2 i24 = ((IA2)(new A0A1<A0>()));
            i24.VerifyInterfaceGenericIA2<A0>();
            A0A0A0<A0>.VerifyA0A0A0NotInlinedGenericStatic<A0A1<A0>>();
            A0A0A0<A0A0>.VerifyA0A0A0NotInlinedStatic();
            A0A0A0<A0A1A2<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>>.VerifyA0A0A0GenericStatic<A0A3>();
            A0A0A0<A0>.VerifyA0A0A0Static();
            A0A0A0<A0A3A4<A0A4<A0A0A1>>> v25 = new A0A0A0<A0A3A4<A0A4<A0A0A1>>>();
            v25.VerifyA0A0A0NotInlinedGeneric<A0>();
            A0A0A0<A0> v26 = new A0A0A0<A0>();
            v26.VerifyA0A0A0NotInlined();
            IA2 i27 = ((IA2)(new A0A0A0<A0A0A1A1<A0A1<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>>>()));
            i27.VerifyInterfaceIA2();
            IA2 i28 = ((IA2)(new A0A0A0<A0>()));
            i28.VerifyInterfaceGenericIA2<A0A3>();
            A0A3.VerifyA0A3NotInlinedGenericStatic<A0A0A0A0<A0A0A1>>();
            A0A3.VerifyA0A3NotInlinedStatic();
            A0A3.VerifyA0A3GenericStatic<A0>();
            A0A3.VerifyA0A3Static();
            A0A3 v29 = new A0A3();
            v29.VerifyA0A3NotInlinedGeneric<A0A0>();
            A0A3 v30 = new A0A3();
            v30.VerifyA0A3NotInlined();
            IA2 i31 = ((IA2)(new A0A3()));
            i31.VerifyInterfaceIA2();
            IA2 i32 = ((IA2)(new A0A3()));
            i32.VerifyInterfaceGenericIA2<A0>();
            A0A0A1.VerifyA0A0A1NotInlinedGenericStatic<A0A1A2<A0A0A0A0<A0A1<A0A0A0A0<A0A0A1A1<A0A1<A0A3A4<A0A0A3<A0A0A1>>>>>>>>>();
            A0A0A1.VerifyA0A0A1NotInlinedStatic();
            A0A0A1.VerifyA0A0A1GenericStatic<A0A0A0A0<A0A0A1>>();
            A0A0A1.VerifyA0A0A1Static();
            A0A0A1 v33 = new A0A0A1();
            v33.VerifyA0A0A1NotInlinedGeneric<A0A1<A0>>();
            A0A0A1 v34 = new A0A0A1();
            v34.VerifyA0A0A1NotInlined();
            IA2 i35 = ((IA2)(new A0A0A1()));
            i35.VerifyInterfaceIA2();
            IA2 i36 = ((IA2)(new A0A0A1()));
            i36.VerifyInterfaceGenericIA2<A0A0A0A0<A0A0A1>>();
            A0A1A2<A0A0A3<A0A0>>.VerifyA0A1A2NotInlinedGenericStatic<A0A0>();
            A0A1A2<A0A0A0A0<A0A0A1>>.VerifyA0A1A2NotInlinedStatic();
            A0A1A2<A0A3A4<A0A4<A0A0A1>>>.VerifyA0A1A2GenericStatic<A0A1<A0>>();
            A0A1A2<A0A0A0A0<A0A0A1>>.VerifyA0A1A2Static();
            A0A1A2<A0> v37 = new A0A1A2<A0>();
            v37.VerifyA0A1A2NotInlinedGeneric<A0A3>();
            A0A1A2<A0A1A2<A0>> v38 = new A0A1A2<A0A1A2<A0>>();
            v38.VerifyA0A1A2NotInlined();
            IA2 i39 = ((IA2)(new A0A1A2<A0A0A1A1<A0A1<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>>>()));
            i39.VerifyInterfaceIA2();
            IA2 i40 = ((IA2)(new A0A1A2<A0A3A4<A0A4<A0A0A1>>>()));
            i40.VerifyInterfaceGenericIA2<A0A1A2<A0A3A4<A0A4<A0A0A1>>>>();
            A0A0A0A0<A0A0A1>.VerifyA0A0A0A0NotInlinedGenericStatic<A0A0A1>();
            A0A0A0A0<A0A3A4<A0A4<A0A0A1>>>.VerifyA0A0A0A0NotInlinedStatic();
            A0A0A0A0<A0A0A0<A0>>.VerifyA0A0A0A0GenericStatic<A0A3A4<A0A4<A0A0A1>>>();
            A0A0A0A0<A0A3>.VerifyA0A0A0A0Static();
            A0A0A0A0<A0A0A1> v41 = new A0A0A0A0<A0A0A1>();
            v41.VerifyA0A0A0A0NotInlinedGeneric<A0A3A4<A0A4<A0A0A1>>>();
            A0A0A0A0<A0A0> v42 = new A0A0A0A0<A0A0>();
            v42.VerifyA0A0A0A0NotInlined();
            IA2 i43 = ((IA2)(new A0A0A0A0<A0A3A4<A0A4<A0A0A1>>>()));
            i43.VerifyInterfaceIA2();
            IA2 i44 = ((IA2)(new A0A0A0A0<A0A0A1>()));
            i44.VerifyInterfaceGenericIA2<A0A0A0A0<A0A0A1>>();
            A0A4<A0A0A1>.VerifyA0A4NotInlinedGenericStatic<A0A4<A0A0A1>>();
            A0A4<A0A0A0A0<A0A0A1>>.VerifyA0A4NotInlinedStatic();
            A0A4<A0A1<A0>>.VerifyA0A4GenericStatic<A0A0>();
            A0A4<A0A0A0A0<A0A0A1>>.VerifyA0A4Static();
            A0A4<A0A4<A0A0A0A0<A0A0A1>>> v45 = new A0A4<A0A4<A0A0A0A0<A0A0A1>>>();
            v45.VerifyA0A4NotInlinedGeneric<A0A1A2<A0A3A4<A0A4<A0A0A1>>>>();
            A0A4<A0A3> v46 = new A0A4<A0A3>();
            v46.VerifyA0A4NotInlined();
            A0A0A3<A0A3>.VerifyA0A0A3NotInlinedGenericStatic<A0A1<A0>>();
            A0A0A3<A0A3A4<A0A4<A0A0A1>>>.VerifyA0A0A3NotInlinedStatic();
            A0A0A3<A0A1A2<A0A3A4<A0A4<A0A0A1>>>>.VerifyA0A0A3GenericStatic<A0A0A0<A0>>();
            A0A0A3<A0A3>.VerifyA0A0A3Static();
            A0A0A3<A0A0A3<A0A3>> v47 = new A0A0A3<A0A0A3<A0A3>>();
            v47.VerifyA0A0A3NotInlinedGeneric<A0A0A1A1<A0A1<A0A0A0<A0A1A2<A0A0A0A0<A0A0A1>>>>>>();
            A0A0A3<A0A1A2<A0A3A4<A0A4<A0A0A1>>>> v48 = new A0A0A3<A0A1A2<A0A3A4<A0A4<A0A0A1>>>>();
            v48.VerifyA0A0A3NotInlined();
            A0A3A4<A0A0A1>.VerifyA0A3A4NotInlinedGenericStatic<A0A0>();
            A0A3A4<A0>.VerifyA0A3A4NotInlinedStatic();
            A0A3A4<A0A0A1>.VerifyA0A3A4GenericStatic<A0A3A4<A0A0A1>>();
            A0A3A4<A0A1<A0>>.VerifyA0A3A4Static();
            A0A3A4<A0A0> v49 = new A0A3A4<A0A0>();
            v49.VerifyA0A3A4NotInlinedGeneric<A0A0A3<A0A1A2<A0A3A4<A0A4<A0A0A1>>>>>();
            A0A3A4<A0A1<A0>> v50 = new A0A3A4<A0A1<A0>>();
            v50.VerifyA0A3A4NotInlined();
            A0A0A1A1<A0A0>.VerifyA0A0A1A1NotInlinedGenericStatic<A0A0>();
            A0A0A1A1<A0A3>.VerifyA0A0A1A1NotInlinedStatic();
            A0A0A1A1<A0A1A2<A0A3A4<A0A4<A0A0A1>>>>.VerifyA0A0A1A1GenericStatic<A0A3A4<A0A1<A0>>>();
            A0A0A1A1<A0A4<A0A3>>.VerifyA0A0A1A1Static();
            A0A0A1A1<A0> v51 = new A0A0A1A1<A0>();
            v51.VerifyA0A0A1A1NotInlinedGeneric<A0A0A1>();
            A0A0A1A1<A0A0A1> v52 = new A0A0A1A1<A0A0A1>();
            v52.VerifyA0A0A1A1NotInlined();
        }
    }

    public class A0A0A3<T0> : A0A0
        where T0 : new()
    {

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void VerifyA0A0A3NotInlinedGenericStatic<T>()
            where T : new()
        {
            System.Console.WriteLine(typeof(T));
            T0 t1 = new T0();
            T t2 = new T();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public static void VerifyA0A0A3NotInlinedStatic()
        {
            T0 t0 = new T0();
        }

        public static void VerifyA0A0A3GenericStatic<T>()
            where T : new()
        {
            System.Console.WriteLine(typeof(T));
            T0 t1 = new T0();
            T t2 = new T();
        }

        public static void VerifyA0A0A3Static()
        {
            T0 t0 = new T0();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void VerifyA0A0A3NotInlinedGeneric<T>()
            where T : new()
        {
            System.Console.WriteLine(this);
            System.Console.WriteLine(typeof(T));
            T0 t2 = new T0();
            T t3 = new T();
        }

        [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void VerifyA0A0A3NotInlined()
        {
            System.Console.WriteLine(this);
            T0 t1 = new T0();
        }

        public void RecurseA0A0A3(int depth)
        {
            if ((depth < 0))
            {
                return;
            }
            System.Console.Write(".");
            A0A4<A0A0A0<A0A1<A0A0A0<A0>>>> next = new A0A4<A0A0A0<A0A1<A0A0A0<A0>>>>();
            next.RecurseA0A4((depth - 1));
        }

        public void CreateAllTypesA0A0A3()
        {
            A0 v0 = new A0();
            v0.VerifyInterfaceIA1();
            A0 v1 = new A0();
            v1.VerifyInterfaceGenericIA1<A0A0>();
            A0 v2 = new A0();
            v2.VerifyInterfaceIA2();
            A0 v3 = new A0();
            v3.VerifyInterfaceGenericIA2<A0A3>();
            A0.VerifyA0NotInlinedGenericStatic<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>();
            A0.VerifyA0NotInlinedStatic();
            A0.VerifyA0GenericStatic<A0A0A1A1<A0A1<A0A1A2<A0A1A2<A0A0>>>>>();
            A0.VerifyA0Static();
            A0 v4 = new A0();
            v4.VerifyA0NotInlinedGeneric<A0A0A1>();
            A0 v5 = new A0();
            v5.VerifyA0NotInlined();
            A0 v6 = new A0();
            v6.VirtualVerifyGeneric<A0A3A4<A0A4<A0A3>>>();
            A0 v7 = new A0();
            v7.VirtualVerify();
            A0 v8 = new A0();
            v8.DeepRecursion();
            IA1 i9 = ((IA1)(new A0()));
            i9.VerifyInterfaceIA1();
            IA1 i10 = ((IA1)(new A0()));
            i10.VerifyInterfaceGenericIA1<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>();
            IA2 i11 = ((IA2)(new A0()));
            i11.VerifyInterfaceIA2();
            IA2 i12 = ((IA2)(new A0()));
            i12.VerifyInterfaceGenericIA2<A0>();
            A0A0.VerifyA0A0NotInlinedGenericStatic<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>();
            A0A0.VerifyA0A0NotInlinedStatic();
            A0A0.VerifyA0A0GenericStatic<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>();
            A0A0.VerifyA0A0Static();
            A0A0 v13 = new A0A0();
            v13.VerifyA0A0NotInlinedGeneric<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>();
            A0A0 v14 = new A0A0();
            v14.VerifyA0A0NotInlined();
            A0A0 v15 = new A0A0();
            v15.VirtualVerifyGeneric<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>();
            A0A0 v16 = new A0A0();
            v16.VirtualVerify();
            IA1 i17 = ((IA1)(new A0A0()));
            i17.VerifyInterfaceIA1();
            IA1 i18 = ((IA1)(new A0A0()));
            i18.VerifyInterfaceGenericIA1<A0>();
            IA2 i19 = ((IA2)(new A0A0()));
            i19.VerifyInterfaceIA2();
            IA2 i20 = ((IA2)(new A0A0()));
            i20.VerifyInterfaceGenericIA2<A0A0A1A1<A0A1<A0A1A2<A0A1A2<A0A0>>>>>();
            A0A1<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>.VerifyA0A1NotInlinedGenericStatic<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>();
            A0A1<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>.VerifyA0A1NotInlinedStatic();
            A0A1<A0A0A0A0<A0A0>>.VerifyA0A1GenericStatic<A0A1<A0A0A0A0<A0A0>>>();
            A0A1<A0A1A2<A0A0>>.VerifyA0A1Static();
            A0A1<A0A0A1> v21 = new A0A1<A0A0A1>();
            v21.VerifyA0A1NotInlinedGeneric<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>();
            A0A1<A0> v22 = new A0A1<A0>();
            v22.VerifyA0A1NotInlined();
            IA2 i23 = ((IA2)(new A0A1<A0A0A0A0<A0A0>>()));
            i23.VerifyInterfaceIA2();
            IA2 i24 = ((IA2)(new A0A1<A0A0A1>()));
            i24.VerifyInterfaceGenericIA2<A0A3>();
            A0A0A0<A0A0A1>.VerifyA0A0A0NotInlinedGenericStatic<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>();
            A0A0A0<A0A0A0<A0A0A1>>.VerifyA0A0A0NotInlinedStatic();
            A0A0A0<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>.VerifyA0A0A0GenericStatic<A0A3>();
            A0A0A0<A0>.VerifyA0A0A0Static();
            A0A0A0<A0A0> v25 = new A0A0A0<A0A0>();
            v25.VerifyA0A0A0NotInlinedGeneric<A0A0>();
            A0A0A0<A0A1<A0A0A1>> v26 = new A0A0A0<A0A1<A0A0A1>>();
            v26.VerifyA0A0A0NotInlined();
            IA2 i27 = ((IA2)(new A0A0A0<A0A0A1>()));
            i27.VerifyInterfaceIA2();
            IA2 i28 = ((IA2)(new A0A0A0<A0A1A2<A0A0>>()));
            i28.VerifyInterfaceGenericIA2<A0A4<A0A3>>();
            A0A3.VerifyA0A3NotInlinedGenericStatic<A0>();
            A0A3.VerifyA0A3NotInlinedStatic();
            A0A3.VerifyA0A3GenericStatic<A0A0>();
            A0A3.VerifyA0A3Static();
            A0A3 v29 = new A0A3();
            v29.VerifyA0A3NotInlinedGeneric<A0A4<A0A3>>();
            A0A3 v30 = new A0A3();
            v30.VerifyA0A3NotInlined();
            IA2 i31 = ((IA2)(new A0A3()));
            i31.VerifyInterfaceIA2();
            IA2 i32 = ((IA2)(new A0A3()));
            i32.VerifyInterfaceGenericIA2<A0A4<A0A3>>();
            A0A0A1.VerifyA0A0A1NotInlinedGenericStatic<A0A1<A0A0A1>>();
            A0A0A1.VerifyA0A0A1NotInlinedStatic();
            A0A0A1.VerifyA0A0A1GenericStatic<A0A0A0<A0A1A2<A0A0>>>();
            A0A0A1.VerifyA0A0A1Static();
            A0A0A1 v33 = new A0A0A1();
            v33.VerifyA0A0A1NotInlinedGeneric<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>();
            A0A0A1 v34 = new A0A0A1();
            v34.VerifyA0A0A1NotInlined();
            IA2 i35 = ((IA2)(new A0A0A1()));
            i35.VerifyInterfaceIA2();
            IA2 i36 = ((IA2)(new A0A0A1()));
            i36.VerifyInterfaceGenericIA2<A0A0A1>();
            A0A1A2<A0>.VerifyA0A1A2NotInlinedGenericStatic<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>();
            A0A1A2<A0A0A0<A0A1A2<A0A0>>>.VerifyA0A1A2NotInlinedStatic();
            A0A1A2<A0A1A2<A0A0A0<A0A1A2<A0A0>>>>.VerifyA0A1A2GenericStatic<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>();
            A0A1A2<A0A3A4<A0A4<A0A3>>>.VerifyA0A1A2Static();
            A0A1A2<A0A4<A0A3>> v37 = new A0A1A2<A0A4<A0A3>>();
            v37.VerifyA0A1A2NotInlinedGeneric<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>();
            A0A1A2<A0A4<A0A3>> v38 = new A0A1A2<A0A4<A0A3>>();
            v38.VerifyA0A1A2NotInlined();
            IA2 i39 = ((IA2)(new A0A1A2<A0A3A4<A0A4<A0A3>>>()));
            i39.VerifyInterfaceIA2();
            IA2 i40 = ((IA2)(new A0A1A2<A0A0A0<A0A1A2<A0A0>>>()));
            i40.VerifyInterfaceGenericIA2<A0A4<A0A3>>();
            A0A0A0A0<A0A0A1>.VerifyA0A0A0A0NotInlinedGenericStatic<A0>();
            A0A0A0A0<A0A3>.VerifyA0A0A0A0NotInlinedStatic();
            A0A0A0A0<A0A0A1>.VerifyA0A0A0A0GenericStatic<A0A4<A0A3>>();
            A0A0A0A0<A0>.VerifyA0A0A0A0Static();
            A0A0A0A0<A0A3A4<A0A4<A0A3>>> v41 = new A0A0A0A0<A0A3A4<A0A4<A0A3>>>();
            v41.VerifyA0A0A0A0NotInlinedGeneric<A0A0A0<A0A1A2<A0A0>>>();
            A0A0A0A0<A0A0> v42 = new A0A0A0A0<A0A0>();
            v42.VerifyA0A0A0A0NotInlined();
            IA2 i43 = ((IA2)(new A0A0A0A0<A0A3A4<A0A4<A0A3>>>()));
            i43.VerifyInterfaceIA2();
            IA2 i44 = ((IA2)(new A0A0A0A0<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>()));
            i44.VerifyInterfaceGenericIA2<A0A3A4<A0A4<A0A3>>>();
            A0A4<A0A0A0<A0A1A2<A0A0>>>.VerifyA0A4NotInlinedGenericStatic<A0>();
            A0A4<A0A0A0A0<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>.VerifyA0A4NotInlinedStatic();
            A0A4<A0>.VerifyA0A4GenericStatic<A0A3A4<A0A4<A0A3>>>();
            A0A4<A0A4<A0>>.VerifyA0A4Static();
            A0A4<A0> v45 = new A0A4<A0>();
            v45.VerifyA0A4NotInlinedGeneric<A0A4<A0>>();
            A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>> v46 = new A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>();
            v46.VerifyA0A4NotInlined();
            A0A0A3<A0>.VerifyA0A0A3NotInlinedGenericStatic<A0A0A0<A0A1A2<A0A0>>>();
            A0A0A3<A0A0A0A0<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>.VerifyA0A0A3NotInlinedStatic();
            A0A0A3<A0A1A2<A0A0A0<A0A1A2<A0A0>>>>.VerifyA0A0A3GenericStatic<A0A0A3<A0A1A2<A0A0A0<A0A1A2<A0A0>>>>>();
            A0A0A3<A0A0A1A1<A0A1<A0A1A2<A0A1A2<A0A0>>>>>.VerifyA0A0A3Static();
            A0A0A3<A0A1A2<A0A0A0<A0A1A2<A0A0>>>> v47 = new A0A0A3<A0A1A2<A0A0A0<A0A1A2<A0A0>>>>();
            v47.VerifyA0A0A3NotInlinedGeneric<A0A0A0<A0A1A2<A0A0>>>();
            A0A0A3<A0A0A0<A0A1A2<A0A0>>> v48 = new A0A0A3<A0A0A0<A0A1A2<A0A0>>>();
            v48.VerifyA0A0A3NotInlined();
            A0A3A4<A0A3>.VerifyA0A3A4NotInlinedGenericStatic<A0A0A0<A0A1A2<A0A0>>>();
            A0A3A4<A0A0>.VerifyA0A3A4NotInlinedStatic();
            A0A3A4<A0A0A3<A0A0A0<A0A1A2<A0A0>>>>.VerifyA0A3A4GenericStatic<A0A0A0<A0A1A2<A0A0>>>();
            A0A3A4<A0>.VerifyA0A3A4Static();
            A0A3A4<A0A0A3<A0A0A0<A0A1A2<A0A0>>>> v49 = new A0A3A4<A0A0A3<A0A0A0<A0A1A2<A0A0>>>>();
            v49.VerifyA0A3A4NotInlinedGeneric<A0A1<A0A0A1>>();
            A0A3A4<A0> v50 = new A0A3A4<A0>();
            v50.VerifyA0A3A4NotInlined();
            A0A0A1A1<A0A1<A0A0A1>>.VerifyA0A0A1A1NotInlinedGenericStatic<A0A3>();
            A0A0A1A1<A0A1A2<A0A0A0<A0A1A2<A0A0>>>>.VerifyA0A0A1A1NotInlinedStatic();
            A0A0A1A1<A0A0A0<A0A1A2<A0A0>>>.VerifyA0A0A1A1GenericStatic<A0A0>();
            A0A0A1A1<A0A0A1A1<A0A0A0<A0A1A2<A0A0>>>>.VerifyA0A0A1A1Static();
            A0A0A1A1<A0A3> v51 = new A0A0A1A1<A0A3>();
            v51.VerifyA0A0A1A1NotInlinedGeneric<A0A1<A0A0A1>>();
            A0A0A1A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>> v52 = new A0A0A1A1<A0A4<A0A0A3<A0A0A3<A0A0A0<A0A0A0A0<A0A1<A0A0A3<A0A0A1A1<A0A0A0A0<A0A3A4<A0A1<A0>>>>>>>>>>>>();
            v52.VerifyA0A0A1A1NotInlined();
        }
    }
}
