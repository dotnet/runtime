// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class BodyFoldingTest
{
    [UnmanagedCallersOnly(EntryPoint = "FoldableMethod1")]
    static void FoldableMethod1() { }

    [UnmanagedCallersOnly(EntryPoint = "FoldableMethod2")]
    static void FoldableMethod2() { }

    class SimpleDelegateTargets
    {
        public static object Return1DelStatic() => new object();
        public static object Return2DelStatic() => new object();

        public object Return1DelInstance() => new object();
        public object Return2DelInstance() => new object();
    }

    class BaseDelegateTargets
    {
        public virtual object Return1Del() => new object();
        public virtual object Return2Del() => new object();
    }

    class DerivedDelegateTargets : BaseDelegateTargets
    {
        public override object Return1Del() => new object();
        public override object Return2Del() => new object();

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static BaseDelegateTargets GetInstance() => new DerivedDelegateTargets();
    }

    class PreinitializedDelegateTargets
    {
        public static readonly PreinitializedDelegateTargets s_o = new PreinitializedDelegateTargets();
        public static readonly Func<object> s_f1 = s_o.Return1Del;
        public static readonly Func<object> s_f2 = s_o.Return2Del;

        public object Return1Del() => new object();
        public object Return2Del() => new object();
    }

    interface IInterfaceTarget
    {
        object Return1Del();
        object Return2Del();
    }

    class InterfaceImplementedTargets : IInterfaceTarget
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static IInterfaceTarget GetInstance() => new InterfaceImplementedTargets();
        public object Return1Del() => new object();
        public object Return2Del() => new object();
    }

    interface IDefaultInterfaceTarget
    {
        object Return1Del() => new object();
        object Return2Del() => new object();
    }

    class DefaultInterfaceImplementedTargets : IDefaultInterfaceTarget
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static IDefaultInterfaceTarget GetInstance() => new DefaultInterfaceImplementedTargets();
    }

    interface IGenericDefaultInterfaceTarget<T>
    {
        object Return1Del() => new object();
        object Return2Del() => new object();
    }

    class GenericDefaultInterfaceImplementedTargets : IGenericDefaultInterfaceTarget<object>
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static IGenericDefaultInterfaceTarget<object> GetInstance() => new GenericDefaultInterfaceImplementedTargets();
    }

    interface IInterfaceToBeDefaultImplemented
    {
        object Return1Del();
        object Return2Del();
    }

    interface IDefaultImplementingInterface : IInterfaceToBeDefaultImplemented
    {
        object IInterfaceToBeDefaultImplemented.Return1Del() => new object();
        object IInterfaceToBeDefaultImplemented.Return2Del() => new object();
    }

    class DefaultInterfaceImplementedTargetsFromOther : IDefaultImplementingInterface
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static IInterfaceToBeDefaultImplemented GetInstance() => new DefaultInterfaceImplementedTargetsFromOther();
    }

    abstract class AbstractBaseClass
    {
        public virtual object Return1Del() => new object();
        public virtual object Return2Del() => new object();
    }

    class ClassDerivedFromAbstract : AbstractBaseClass
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static AbstractBaseClass GetInstance() => new ClassDerivedFromAbstract();
    }

    interface IStaticInterfaceTargets
    {
        static abstract object Return1Del();
        static abstract object Return2Del();

        static virtual object Return1DelDefaultImpl() => new object();
        static virtual object Return2DelDefaultImpl() => new object();

        public static void Test<T>() where T : IStaticInterfaceTargets
        {
            {
                Func<object> f1 = T.Return1Del;
                Func<object> f2 = T.Return2Del;
                if (f1.Equals(f2))
                    throw new Exception();
            }

            // Sanity check equality of delegates pointing to exact same thing
            {
                Func<object> f1 = T.Return1Del;
                Func<object> f2 = T.Return1Del;
                if (!f1.Equals(f2))
                    throw new Exception();
            }

            {
                Func<object> f1 = T.Return1DelDefaultImpl;
                Func<object> f2 = T.Return2DelDefaultImpl;
                if (f1.Equals(f2))
                    throw new Exception();
            }

            // Sanity check equality of delegates pointing to exact same thing
            {
                Func<object> f1 = T.Return1DelDefaultImpl;
                Func<object> f2 = T.Return1DelDefaultImpl;
                if (!f1.Equals(f2))
                    throw new Exception();
            }
        }
    }

    class StaticInterfaceImplementedTargetsClass : IStaticInterfaceTargets
    {
        public static object Return1Del() => new object();
        public static object Return2Del() => new object();
    }

    class StaticInterfaceImplementedTargetsClass2 : IStaticInterfaceTargets
    {
        public static object Return1Del() => new object();
        public static object Return2Del() => new object();
    }


    struct StaticInterfaceImplementedTargetsStruct : IStaticInterfaceTargets
    {
        public static object Return1Del() => new object();
        public static object Return2Del() => new object();
    }

    class ReflectedOnType
    {
        public static object Return1Del() => new object();
        public static object Return2Del() => new object();
    }

    static object Return1() => new object();
    static object Return2() => new object();

    class ConstructedInGenericContextToGenerics<T>
    {
        static object Return1Del() => new object();
        static object Return2Del() => new object();

        public static void Test()
        {
            {
                Func<object> f1 = ConstructedInGenericContextToGenerics<T>.Return1Del;
                Func<object> f2 = ConstructedInGenericContextToGenerics<T>.Return2Del;
                if (f1.Equals(f2))
                    throw new Exception();
            }

            // Sanity check equality of delegates pointing to exact same thing
            {
                Func<object> f1 = ConstructedInGenericContextToGenerics<T>.Return1Del;
                Func<object> f2 = ConstructedInGenericContextToGenerics<T>.Return1Del;
                if (!f1.Equals(f2))
                    throw new Exception();
            }
        }
    }

    interface IInterfaceWithGvms
    {
        object Return1Del<T>();
        object Return2Del<T>();
    }

    class GvmImplementingClass : IInterfaceWithGvms
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static IInterfaceWithGvms GetInstance() => new GvmImplementingClass();

        public virtual object Return1Del<T>() => new object();
        public virtual object Return2Del<T>() => new object();
    }

    struct UnboxingThunkStruct<T>
    {
        public object Return1Del() => new object();
        public object Return2Del() => new object();
    }

    struct ReflectedUnboxingThunkStruct<T>
    {
        public object Return1Del() => new object();
        public object Return2Del() => new object();
    }

    public static void Run()
    {
        Return1();
        Return2();

        // Static method
        {
            Func<object> f1 = SimpleDelegateTargets.Return1DelStatic;
            Func<object> f2 = SimpleDelegateTargets.Return2DelStatic;
            if (f1.Equals(f2))
                throw new Exception();
        }

        // Sanity check equality of delegates pointing to exact same thing
        {
            Func<object> f1 = SimpleDelegateTargets.Return1DelStatic;
            Func<object> f2 = SimpleDelegateTargets.Return1DelStatic;
            if (!f1.Equals(f2))
                throw new Exception();
        }

        // Instance method
        {
            var o = new SimpleDelegateTargets();
            Func<object> f1 = o.Return1DelInstance;
            Func<object> f2 = o.Return2DelInstance;
            if (f1.Equals(f2))
                throw new Exception();
        }

        // Sanity check equality of delegates pointing to exact same thing
        {
            var o = new SimpleDelegateTargets();
            Func<object> f1 = o.Return1DelInstance;
            Func<object> f2 = o.Return1DelInstance;
            if (!f1.Equals(f2))
                throw new Exception();
        }

        // Virtual method
        {
            var o = DerivedDelegateTargets.GetInstance();
            Func<object> f1 = o.Return1Del;
            Func<object> f2 = o.Return2Del;
            if (f1.Equals(f2))
                throw new Exception();
        }

        // Sanity check equality of delegates pointing to exact same thing
        {
            var o = DerivedDelegateTargets.GetInstance();
            Func<object> f1 = o.Return1Del;
            Func<object> f2 = o.Return1Del;
            if (!f1.Equals(f2))
                throw new Exception();
        }

        // Method from a frozen delegate
        {
            if (PreinitializedDelegateTargets.s_f1.Equals(PreinitializedDelegateTargets.s_f2))
                throw new Exception();
        }

        // Interface method
        {
            var o = InterfaceImplementedTargets.GetInstance();
            Func<object> f1 = o.Return1Del;
            Func<object> f2 = o.Return2Del;
            if (f1.Equals(f2))
                throw new Exception();
        }

        // Sanity check equality of delegates pointing to exact same thing
        {
            var o = InterfaceImplementedTargets.GetInstance();
            Func<object> f1 = o.Return1Del;
            Func<object> f2 = o.Return1Del;
            if (!f1.Equals(f2))
                throw new Exception();
        }

        // Default interface method
        {
            var o = DefaultInterfaceImplementedTargets.GetInstance();
            Func<object> f1 = o.Return1Del;
            Func<object> f2 = o.Return2Del;
            if (f1.Equals(f2))
                throw new Exception();
        }

        // Sanity check equality of delegates pointing to exact same thing
        {
            var o = DefaultInterfaceImplementedTargets.GetInstance();
            Func<object> f1 = o.Return1Del;
            Func<object> f2 = o.Return1Del;
            if (!f1.Equals(f2))
                throw new Exception();
        }

        // Default generic interface method
        {
            var o = GenericDefaultInterfaceImplementedTargets.GetInstance();
            Func<object> f1 = o.Return1Del;
            Func<object> f2 = o.Return2Del;
            if (f1.Equals(f2))
                throw new Exception();
        }

        // Sanity check equality of delegates pointing to exact same thing
        {
            var o = GenericDefaultInterfaceImplementedTargets.GetInstance();
            Func<object> f1 = o.Return1Del;
            Func<object> f2 = o.Return1Del;
            if (!f1.Equals(f2))
                throw new Exception();
        }

        // Default interface method
        {
            var o = DefaultInterfaceImplementedTargetsFromOther.GetInstance();
            Func<object> f1 = o.Return1Del;
            Func<object> f2 = o.Return2Del;
            if (f1.Equals(f2))
                throw new Exception();
        }

        // Sanity check equality of delegates pointing to exact same thing
        {
            var o = DefaultInterfaceImplementedTargetsFromOther.GetInstance();
            Func<object> f1 = o.Return1Del;
            Func<object> f2 = o.Return1Del;
            if (!f1.Equals(f2))
                throw new Exception();
        }

        // Interaction with virtuals on abstract classes optimization
        {
            var o = ClassDerivedFromAbstract.GetInstance();
            Func<object> f1 = o.Return1Del;
            Func<object> f2 = o.Return2Del;
            if (f1.Equals(f2))
                throw new Exception();
        }

        // Sanity check equality of delegates pointing to exact same thing
        {
            var o = ClassDerivedFromAbstract.GetInstance();
            Func<object> f1 = o.Return1Del;
            Func<object> f2 = o.Return1Del;
            if (!f1.Equals(f2))
                throw new Exception();
        }

        IStaticInterfaceTargets.Test<StaticInterfaceImplementedTargetsClass>();
        IStaticInterfaceTargets.Test<StaticInterfaceImplementedTargetsStruct>();
        typeof(IStaticInterfaceTargets).GetMethod(nameof(IStaticInterfaceTargets.Test))
            .MakeGenericMethod(GetStaticInterfaceImplementedTargetsClass2())
            .Invoke(null, []);

        static Type GetStaticInterfaceImplementedTargetsClass2() => typeof(StaticInterfaceImplementedTargetsClass2);

        {
            var f1 = typeof(ReflectedOnType).GetMethod(nameof(ReflectedOnType.Return1Del)).CreateDelegate<Func<object>>();
            var f2 = typeof(ReflectedOnType).GetMethod(nameof(ReflectedOnType.Return2Del)).CreateDelegate<Func<object>>();
            if (f1.Equals(f2))
                throw new Exception();
        }

        // Sanity check equality of delegates pointing to exact same thing
        {
            var f1 = typeof(ReflectedOnType).GetMethod(nameof(ReflectedOnType.Return1Del)).CreateDelegate<Func<object>>();
            var f2 = typeof(ReflectedOnType).GetMethod(nameof(ReflectedOnType.Return1Del)).CreateDelegate<Func<object>>();
            if (!f1.Equals(f2))
                throw new Exception();
        }

        ConstructedInGenericContextToGenerics<int>.Test();
        ConstructedInGenericContextToGenerics<object>.Test();

        // Interface generic virtual methods (shared)
        {
            var o = GvmImplementingClass.GetInstance();
            Func<object> f1 = o.Return1Del<object>;
            Func<object> f2 = o.Return2Del<object>;
            if (f1.Equals(f2))
                throw new Exception();
        }

        // Sanity check equality of delegates pointing to exact same thing
        {
            var o = GvmImplementingClass.GetInstance();
            Func<object> f1 = o.Return1Del<object>;
            Func<object> f2 = o.Return1Del<object>;
            if (!f1.Equals(f2))
                throw new Exception();
        }

        // Interface generic virtual methods (unshared)
        {
            var o = GvmImplementingClass.GetInstance();
            Func<object> f1 = o.Return1Del<int>;
            Func<object> f2 = o.Return2Del<int>;
            if (f1.Equals(f2))
                throw new Exception();
        }

        // Sanity check equality of delegates pointing to exact same thing
        {
            var o = GvmImplementingClass.GetInstance();
            Func<object> f1 = o.Return1Del<int>;
            Func<object> f2 = o.Return1Del<int>;
            if (!f1.Equals(f2))
                throw new Exception();
        }

        // Instance method on struct (unshared)
        {
            var o = new UnboxingThunkStruct<int>();
            Func<object> f1 = o.Return1Del;
            Func<object> f2 = o.Return2Del;
            if (f1.Equals(f2))
                throw new Exception();
        }

        // Instance method on struct (shared)
        {
            var o = new UnboxingThunkStruct<object>();
            Func<object> f1 = o.Return1Del;
            Func<object> f2 = o.Return2Del;
            if (f1.Equals(f2))
                throw new Exception();
        }

        // Reflection-created open delegates to valuetypes (unshared)
        {
            var f1 = typeof(ReflectedUnboxingThunkStruct<int>).GetMethod(nameof(ReflectedUnboxingThunkStruct<int>.Return1Del)).CreateDelegate<RefParamDelegate<ReflectedUnboxingThunkStruct<int>>>();
            var f2 = typeof(ReflectedUnboxingThunkStruct<int>).GetMethod(nameof(ReflectedUnboxingThunkStruct<int>.Return2Del)).CreateDelegate<RefParamDelegate<ReflectedUnboxingThunkStruct<int>>>();
            if (f1.Equals(f2))
                throw new Exception();
        }

        // Reflection-created open delegates to valuetypes (shared)
        {
            var f1 = typeof(ReflectedUnboxingThunkStruct<object>).GetMethod(nameof(ReflectedUnboxingThunkStruct<object>.Return1Del)).CreateDelegate<RefParamDelegate<ReflectedUnboxingThunkStruct<object>>>();
            var f2 = typeof(ReflectedUnboxingThunkStruct<object>).GetMethod(nameof(ReflectedUnboxingThunkStruct<object>.Return2Del)).CreateDelegate<RefParamDelegate<ReflectedUnboxingThunkStruct<object>>>();
            if (f1.Equals(f2))
                throw new Exception();
        }
    }

    delegate object RefParamDelegate<T>(ref T inst);
}
