// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: TypeMap<ReflectionBlockedTests.Universe>("Blocked", typeof(ReflectionBlockedTests.Holder.BlockedClass), typeof(ReflectionBlockedTests.Holder.BlockedClass))]

class ReflectionBlockedTests
{
    // Containing type with and without blocked members. Nested types are only found by name if they have metadata.
    internal class Holder
    {
        [ReflectionBlocked] internal static class BlockedStatic
        {
            internal static int Counter;
            internal static void Hit() => Counter++;
        }

        [ReflectionBlocked] internal class BlockedClass : IBlockedInterface
        {
            public int Value = 42;
            public override int GetHashCode() => Value;
        }

        [ReflectionBlocked] internal struct BlockedStruct
        {
            public int Value;
        }

        [ReflectionBlocked] internal interface IBlockedInterface;

        [ReflectionBlocked] internal class BlockedImpl : IRunner
        {
            public static void Run() => BlockedStatic.Hit();
        }

        [ReflectionBlocked] internal class BlockedWithNested
        {
            internal class Nested { public int Value = 7; }
        }

        [ReflectionBlocked] internal class BlockedBase { }
        internal class DerivedFromBlocked : BlockedBase { }

        // Closures and iterators of a blocked type are generic types nested in it, they are blocked as well.
        [ReflectionBlocked] internal class BlockedWithGenericMethod
        {
            public static T Invoke<T>(T value)
            {
                Func<T> closure = () => value;
                return closure();
            }

            public static Type GetClosureType<T>(T value)
            {
                Func<T> closure = () => value;
                return closure.Target.GetType();
            }
        }

        internal interface IDynamicTarget { int Get(); }

        // The runtime only needs the MethodTable of the implementation type that IDynamicInterfaceCastable returns.
        [ReflectionBlocked, DynamicInterfaceCastableImplementation]
        internal interface IBlockedDynamicImpl : IDynamicTarget
        {
            int IDynamicTarget.Get() => 42;
        }

        internal sealed class DynamicCastable : IDynamicInterfaceCastable
        {
            public bool IsInterfaceImplemented(RuntimeTypeHandle interfaceType, bool throwIfNotImplemented) => interfaceType.Equals(typeof(IDynamicTarget).TypeHandle);
            public RuntimeTypeHandle GetInterfaceImplementation(RuntimeTypeHandle interfaceType) => typeof(IBlockedDynamicImpl).TypeHandle;
        }

        internal class VisibleClass { }

        internal interface IRunner
        {
            static abstract void Run();
        }

        internal class Host<T> where T : class { }
        internal class DerivedHost : Host<BlockedClass> { }
        internal class VisibleHost : Host<VisibleClass> { }

        internal class Generic2<T1, T2> where T1 : class where T2 : class { }
        internal sealed class DerivedGeneric2 : Generic2<VisibleClass, BlockedClass> { }

        [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
        internal sealed class TypeAttribute : Attribute
        {
            public TypeAttribute(Type type) => Type = type;
            public Type Type { get; }
        }

        internal sealed class GenericAttribute<T> : Attribute { }
        internal sealed class OtherAttribute : Attribute { }

        // The attributes that mention a blocked type are dropped, the others on the same member are kept.
        [Type(typeof(BlockedClass)), Other, Type(typeof(List<BlockedClass>)), Type(typeof(BlockedClass[]))]
        internal class AnnotatedWithBlockedTypeof { }

        [Other, Generic<BlockedClass>]
        internal class AnnotatedWithBlockedGenericAttribute { }

        internal class OverloadsWithBlockedParameter
        {
            public OverloadsWithBlockedParameter() { }
            public OverloadsWithBlockedParameter(BlockedClass c) { }
            public static int Visible(int x) => x;
            public static int Visible(BlockedClass c) => 1;
        }

        internal static class UsesStaticAbstract
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static void Call<T>() where T : IRunner => T.Run();
        }
    }

    internal sealed class Universe;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static object Make(object o) => o;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Type Hide(Type t) => t;

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern", Justification = "That's the point")]
    static Type GetNested(string name) => typeof(Holder).GetNestedType(name, BindingFlags.NonPublic | BindingFlags.Public);

    static void Check(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }

    static void ExpectNotSupported(Action action, string message)
    {
        try
        {
            action();
        }
        catch (NotSupportedException)
        {
            return;
        }

        throw new Exception(message + " did not throw NotSupportedException");
    }

    public static int Run()
    {
        // Blocked types have no metadata, types that are not blocked do.
        Check(GetNested(nameof(Holder.VisibleClass)) != null, "control type should have metadata");
        Check(GetNested(nameof(Holder.Host<object>) + "`1") != null, "control generic type should have metadata");
        foreach (string name in new[] { nameof(Holder.BlockedStatic), nameof(Holder.BlockedClass), nameof(Holder.BlockedStruct), nameof(Holder.IBlockedInterface),
                                        nameof(Holder.BlockedImpl), nameof(Holder.BlockedWithNested), nameof(Holder.BlockedBase) })
        {
            Check(GetNested(name) == null, name + " should not have metadata");
        }

        // The code of blocked types works, including static abstract members used through a blocked type argument.
        Holder.BlockedStatic.Hit();
        Holder.UsesStaticAbstract.Call<Holder.BlockedImpl>();
        Check(Holder.BlockedStatic.Counter == 2, "static class and static abstract call");

        // Instances: allocation, casts, GetType, type identity all work. The names are not available.
        object blocked = Make(new Holder.BlockedClass());
        Check(blocked is Holder.IBlockedInterface, "interface cast of a blocked class");
        Check(((Holder.BlockedClass)blocked).Value == 42, "field of a blocked class");
        Check(blocked.GetHashCode() == 42, "virtual call on a blocked class");
        Check(blocked.GetType() == typeof(Holder.BlockedClass), "type identity");
        Check(blocked.GetType().BaseType == typeof(object), "base type of a blocked class");
        Check(blocked.GetType().IsClass && !blocked.GetType().IsValueType, "type flags of a blocked class");
        Check(Array.IndexOf(blocked.GetType().GetInterfaces(), typeof(Holder.IBlockedInterface)) >= 0, "interfaces of a blocked type");
        Check(blocked.GetType().Namespace == null, "Namespace of a blocked type");
        Check(blocked.GetType().DeclaringType == null, "DeclaringType of a blocked type");
        Check(blocked.GetType().Name.StartsWith("<ReflectionBlocked", StringComparison.Ordinal), "Name of a blocked type is a placeholder");
        Check(blocked.GetType().FullName == blocked.GetType().Name && blocked.GetType().ToString() == blocked.GetType().Name, "FullName and ToString of a blocked type");
        Check(blocked.ToString() == blocked.GetType().Name, "ToString of an instance of a blocked type");
        Check(blocked.GetType().Name != Make(new Holder.BlockedWithNested()).GetType().Name, "placeholder names of different blocked types differ");
        ExpectNotSupported(() => _ = blocked.GetType().GUID, "GUID of a blocked type");
        Check(new Dictionary<Type, int>().TryGetValue(blocked.GetType(), out _) == false, "blocked type as a dictionary key");
        try
        {
            _ = new Dictionary<Type, int>()[blocked.GetType()];
            throw new Exception("missing dictionary key did not throw");
        }
        catch (KeyNotFoundException)
        {
        }

        object boxed = Make(new Holder.BlockedStruct { Value = 5 });
        Check(((Holder.BlockedStruct)boxed).Value == 5, "boxed blocked struct");
        Check(boxed.GetType().IsValueType, "type flags of a blocked struct");


        // Nested types of a blocked type are blocked.
        Check(Make(new Holder.BlockedWithNested.Nested()) is Holder.BlockedWithNested.Nested nested && nested.Value == 7, "nested type of a blocked type");
        Check(Make(new Holder.BlockedWithNested.Nested()).GetType().Name.StartsWith("<ReflectionBlocked", StringComparison.Ordinal), "Name of a type nested in a blocked type");

        // Closures of a generic method of a blocked type.
        Check(Holder.BlockedWithGenericMethod.Invoke("a") == "a" && Holder.BlockedWithGenericMethod.Invoke(3) == 3, "generic method of a blocked type");
        Type closure = Holder.BlockedWithGenericMethod.GetClosureType(3);
        Check(closure.IsGenericType, "closure of a generic method of a blocked type");
        ExpectNotSupported(() => closure.GetFields(), "members of a closure of a blocked type");

        // Blocking a type does not block the types that use it: derived types, generic instantiations, arrays.
        Check(Make(new Holder.DerivedFromBlocked()).GetType().Name == nameof(Holder.DerivedFromBlocked), "type derived from a blocked type keeps its metadata");
        Check(typeof(Holder.DerivedFromBlocked).BaseType == typeof(Holder.BlockedBase), "base type is the blocked type");
        Check(typeof(Holder.DerivedFromBlocked).BaseType.Name.StartsWith("<ReflectionBlocked", StringComparison.Ordinal), "Name of a blocked base type");

        Type derivedHost = Make(new Holder.DerivedHost()).GetType();
        Check(derivedHost.Name == nameof(Holder.DerivedHost), "derived from an instantiation over a blocked type: Name");
        Type baseHost = derivedHost.BaseType;
        Check(baseHost.IsGenericType && !baseHost.IsGenericTypeDefinition, "instantiation over a blocked type is a generic type");
        Check(baseHost.Name == "Host`1", "instantiation over a blocked type keeps its metadata: Name");
        Check(baseHost.GetGenericTypeDefinition() == typeof(Holder.Host<>), "generic type definition of an instantiation over a blocked type");
        Check(baseHost.GetGenericArguments().Length == 1 && baseHost.GetGenericArguments()[0] == typeof(Holder.BlockedClass), "generic argument is the blocked type");
        Check(baseHost == typeof(Holder.Host<Holder.BlockedClass>), "type identity of the instantiation");
        ExpectNotSupported(() => _ = baseHost.FullName, "FullName of an instantiation over a blocked type");
        Check(baseHost.GetGenericArguments()[0].Name.StartsWith("<ReflectionBlocked", StringComparison.Ordinal), "Name of the blocked generic argument");

        Type derivedGeneric2 = Make(new Holder.DerivedGeneric2()).GetType();
        Check(derivedGeneric2.BaseType.Name == "Generic2`2" && derivedGeneric2.BaseType.GetGenericArguments()[0] == typeof(Holder.VisibleClass), "visible argument of a mixed instantiation");

        Check(Make(new Holder.VisibleHost()).GetType().BaseType.FullName.Contains(nameof(Holder.VisibleClass)), "instantiation over visible types is unaffected");

        Type blockedArray = Make(new Holder.BlockedClass[1]).GetType();
        Check(blockedArray.IsArray && blockedArray.GetElementType() == typeof(Holder.BlockedClass), "array of a blocked type");
        Check(blockedArray.Name.EndsWith("[]", StringComparison.Ordinal), "Name of an array of a blocked type");

        // Custom attributes that mention a blocked type are dropped, others on the same member survive.
        object[] attributes = typeof(Holder.AnnotatedWithBlockedTypeof).GetCustomAttributes(inherit: false);
        Check(attributes.Length == 1 && attributes[0] is Holder.OtherAttribute, "typeof of a blocked type in a custom attribute");
        attributes = typeof(Holder.AnnotatedWithBlockedGenericAttribute).GetCustomAttributes(inherit: false);
        Check(attributes.Length == 1 && attributes[0] is Holder.OtherAttribute, "generic attribute over a blocked type");

        // A visible method whose signature mentions a blocked type: its parameter type is not available, and binding by parameter
        // types over an overload set that contains it fails (all references to blocked types share one placeholder). By name only works.
        Check(typeof(Holder.OverloadsWithBlockedParameter).GetMethods().Length > 0, "methods of a type with a blocked parameter type");
        ExpectNotSupported(() => _ = typeof(Holder.OverloadsWithBlockedParameter).GetMethod("Visible", new[] { typeof(int) }), "binding over a blocked parameter type");

        // The runtime type loader accepts a blocked type argument.
        Type made = Hide(typeof(Holder.Host<>)).MakeGenericType(Hide(typeof(Holder.BlockedClass)));
        Check(made == typeof(Holder.Host<Holder.BlockedClass>), "MakeGenericType over a blocked type");

        // IDynamicInterfaceCastable implementation types can be blocked.
        Check(((Holder.IDynamicTarget)Make(new Holder.DynamicCastable())).Get() == 42, "IDynamicInterfaceCastable implementation type that is blocked");

        // A blocked type can be the key and the value of a type map entry.
        Check(TypeMapping.GetOrCreateExternalTypeMapping<Universe>()["Blocked"] == typeof(Holder.BlockedClass), "blocked type as a type map value");

        // A delegate to a method of a blocked type works, but its MethodInfo is not available.
        Action hit = Holder.BlockedStatic.Hit;
        hit();
        ExpectNotSupported(() => _ = hit.Method, "Delegate.Method of a method of a blocked type");

        List<Holder.BlockedClass> list = new() { new Holder.BlockedClass() };
        Check(Make(list).GetType().GetGenericTypeDefinition() == typeof(List<>), "BCL generic over a blocked type");

        return 100;
    }
}
