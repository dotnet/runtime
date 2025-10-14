// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Mono.Linker;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Reflection;

[assembly: KeptAttributeAttribute(typeof(TypeMapAttribute<UsedTypeMap>), By = Tool.Trimmer)]
[assembly: KeptAttributeAttribute(typeof(TypeMapAssociationAttribute<UsedTypeMap>), By = Tool.Trimmer)]
[assembly: TypeMap<UsedTypeMap>("TrimTargetIsTarget", typeof(TargetAndTrimTarget), typeof(TargetAndTrimTarget))]
[assembly: TypeMap<UsedTypeMap>("TrimTargetIsUnrelated", typeof(TargetType), typeof(TrimTarget))]
[assembly: TypeMap<UsedTypeMap>(nameof(AllocatedNoTypeCheckClassTarget), typeof(AllocatedNoTypeCheckClassTarget), typeof(AllocatedNoTypeCheckClass))]
[assembly: TypeMap<UsedTypeMap>(nameof(AlloctedNoTypeCheckStructTarget), typeof(AlloctedNoTypeCheckStructTarget), typeof(AllocatedNoTypeCheckStruct))]
[assembly: TypeMap<UsedTypeMap>(nameof(UnreferencedTargetType), typeof(UnreferencedTargetType), typeof(UnreferencedTrimTarget))]
[assembly: TypeMap<UsedTypeMap>("TypeMapEntryOnly", typeof(TypeMapEntryOnly))]
[assembly: TypeMap<UsedTypeMap>(nameof(UnboxedOnlyTarget), typeof(UnboxedOnlyTarget), typeof(UnboxedOnly))]
[assembly: TypeMap<UsedTypeMap>("TypedRefSource", typeof(MakeRefTargetType), typeof(MakeRef))]
[assembly: TypeMap<UsedTypeMap>("TypedRefTarget", typeof(RefValueTargetType), typeof(RefValue))]
[assembly: TypeMap<UsedTypeMap>("Constrained", typeof(ConstrainedTarget), typeof(Constrained))]
[assembly: TypeMap<UsedTypeMap>("ConstrainedStatic", typeof(ConstraintedStaticTarget), typeof(ConstrainedStatic))]
[assembly: TypeMap<UsedTypeMap>("Ldobj", typeof(LdobjTarget), typeof(LdobjType))]
[assembly: TypeMap<UsedTypeMap>("ArrayElement", typeof(ArrayElementTarget), typeof(ArrayElement))]
[assembly: TypeMap<UsedTypeMap>("TrimTargetIsAllocatedNoTypeCheckNoBoxStruct", typeof(ConstructedNoTypeCheckOrBoxTarget), typeof(ConstructedNoTypeCheckNoBoxStruct))]
[assembly: TypeMapAssociation<UsedTypeMap>(typeof(SourceClass), typeof(ProxyType))]
[assembly: TypeMapAssociation<UsedTypeMap>(typeof(TypeCheckOnlyClass), typeof(TypeCheckOnlyProxy))]
[assembly: TypeMapAssociation<UsedTypeMap>(typeof(AllocatedNoBoxStructType), typeof(AllocatedNoBoxProxy))]
[assembly: TypeMapAssociation<UsedTypeMap>(typeof(I), typeof(IImpl))]
[assembly: TypeMapAssociation<UsedTypeMap>(typeof(IInterfaceWithDynamicImpl), typeof(IDynamicImpl))]
[assembly: TypeMapAssociation<UsedTypeMap>(typeof(ArrayElement), typeof(ArrayElementProxy))]

[assembly: TypeMap<UnusedTypeMap>("UnusedName", typeof(UnusedTargetType), typeof(TrimTarget))]
[assembly: TypeMapAssociation<UsedTypeMap>(typeof(UnusedSourceClass), typeof(UnusedProxyType))]
[assembly: TypeMap<UsedTypeMap>("ClassWithStaticMethod", typeof(TargetType4), typeof(ClassWithStaticMethod))]
[assembly: TypeMap<UsedTypeMap>("ClassWithStaticMethodAndField", typeof(TargetType5), typeof(ClassWithStaticMethodAndField))]

namespace Mono.Linker.Tests.Cases.Reflection
{
    [Kept]
    [SetupCompileArgument("/unsafe")]
    class TypeMap
    {
        [Kept]
        [ExpectedWarning("IL2057", "Unrecognized value passed to the parameter 'typeName' of method 'System.Type.GetType(String)'")]
        public static void Main(string[] args)
        {
            object t = Activator.CreateInstance(Type.GetType(args[1]));
            CheckTargetAndTrimTarget(t);
            CheckTrimTarget(t);
            CheckTypeCheckOnlyClass(t);
            Unbox(t);
            if (t is IInterfaceWithDynamicImpl d)
            {
                d.Method();
            }

            Console.WriteLine("Hash code of SourceClass instance: " + new SourceClass().GetHashCode());
            Console.WriteLine("Hash code of UsedClass instance: " + new UsedClass().GetHashCode());
            Console.WriteLine("Hash code of AllocatedNoTypeCheckClass instance: " + new AllocatedNoTypeCheckClass().GetHashCode());
            Console.WriteLine("Hash code of AllocatedNoTypeCheckStruct instance: " + new AllocatedNoTypeCheckStruct().GetHashCode());

            Console.WriteLine(TypeMapping.GetOrCreateExternalTypeMapping<UsedTypeMap>());
            Console.WriteLine(GetExternalTypeMap<UnusedTypeMap>());

            var proxyMap = TypeMapping.GetOrCreateProxyTypeMapping<UsedTypeMap>();

            AllocatedNoBoxStructType allocatedNoBoxStructType = new AllocatedNoBoxStructType(Random.Shared.Next());
            Console.WriteLine("AllocatedNoBoxStructType value: " + allocatedNoBoxStructType.Value);
            Console.WriteLine(proxyMap[typeof(AllocatedNoBoxStructType)]);
            ClassWithStaticMethod.StaticMethod();

            Console.WriteLine(ClassWithStaticMethodAndField.StaticMethod());

            unsafe
            {
                delegate*<void> staticMethodPtr = &ClassWithStaticMethod.StaticMethod;
                staticMethodPtr();
            }

            MakeRef s = default;

            TypedReference r = __makeref(s);

            RefValue t2 = __refvalue(r, RefValue);

            ConstrainedCall<Constrained>(default);

            static void ConstrainedCall<[KeptGenericParamAttributes(GenericParameterAttributes.DefaultConstructorConstraint | GenericParameterAttributes.NotNullableValueTypeConstraint)] T>(T t) where T : struct, IInterface
            {
                t.Method();
            }

            ConstrainedStaticCall<ConstrainedStatic>(default);

            static void ConstrainedStaticCall<T>(T t) where T : IStaticInterface
            {
                T.Method();
            }

            unsafe
            {
                LdobjType* ptr = (LdobjType*)NativeMemory.AllocZeroed((nuint)sizeof(LdobjType));
                LdobjType val = *ptr;
                Console.WriteLine(val.Value);
                NativeMemory.Free(ptr);
            }

            Console.WriteLine(new ArrayElement[1]);

            Console.WriteLine(new ConstructedNoTypeCheckNoBoxStruct(42).Value);
        }

        [Kept]
        private static void CheckTargetAndTrimTarget(object o)
        {
            if (o is TargetAndTrimTarget)
            {
                Console.WriteLine("Type deriving from TargetAndTrimTarget instantiated.");
            }
        }

        [Kept]
        [ExpectedInstructionSequence([
            "nop",
            "ldarg.0",
            "pop",
            "ldnull",
            "ldnull",
            "cgt.un",
            "stloc.0",
            "ldloc.0",
            "brfalse.s il_18",
            "nop",
            "ldstr 'Type deriving from TypeCheckOnlyClass instantiated.'",
            "call System.Void System.Console::WriteLine(System.String)",
            "nop",
            "nop",
            "ret",
        ])]
        private static void CheckTypeCheckOnlyClass(object o)
        {
            if (o is TypeCheckOnlyClass)
            {
                Console.WriteLine("Type deriving from TypeCheckOnlyClass instantiated.");
            }
        }

        [Kept]
        [ExpectedInstructionSequence([
            "nop",
            "ldarg.0",
            "pop",
            "ldnull",
            "ldnull",
            "cgt.un",
            "stloc.0",
            "ldloc.0",
            "brfalse.s il_18",
            "nop",
            "ldstr 'Type deriving from TrimTarget instantiated.'",
            "call System.Void System.Console::WriteLine(System.String)",
            "nop",
            "nop",
            "ret"
            ])]
        private static void CheckTrimTarget(object o)
        {
            if (o is TrimTarget)
            {
                Console.WriteLine("Type deriving from TrimTarget instantiated.");
            }
        }

        [Kept]
        private static UnboxedOnly Unbox(object o)
        {
            return (UnboxedOnly) o;
        }

        [Kept]
        [ExpectedWarning("IL2124", "Type 'T' must not contain signature variables to be used as a type map group.")]
        private static IReadOnlyDictionary<string, Type> GetExternalTypeMap<T>()
        {
            return TypeMapping.GetOrCreateExternalTypeMapping<T>();
        }
    }

    [Kept]
    class UsedTypeMap;

    [Kept]
    class TargetAndTrimTarget;

    [Kept]
    class TargetType;

    [Kept] // This is kept by NativeAot by the scanner. It is not kept during codegen.
    class TrimTarget;

    class UnreferencedTargetType;

    class UnreferencedTrimTarget;

    [Kept]
    [KeptMember(".ctor()")]
    class SourceClass;

    [Kept]
    class ProxyType;

    [Kept]
    class UnusedTypeMap;
    class UnusedTargetType;
    class UnusedSourceClass;
    class UnusedProxyType;

    interface I;

    interface IImpl : I;

    [Kept]
    [KeptMember(".ctor()")]
    class UsedClass : IImpl;

    [Kept]
    interface IInterfaceWithDynamicImpl
    {
        [Kept(By = Tool.Trimmer)]
        void Method();
    }

    [Kept]
    [KeptInterface(typeof(IInterfaceWithDynamicImpl))]
    [KeptAttributeAttribute(typeof(DynamicInterfaceCastableImplementationAttribute), By = Tool.Trimmer)]
    [DynamicInterfaceCastableImplementation]
    interface IDynamicImpl : IInterfaceWithDynamicImpl
    {
        [Kept]
        void IInterfaceWithDynamicImpl.Method()
        {
        }
    }

    [Kept]
    [KeptMember(".ctor()")]
    class AllocatedNoTypeCheckClass;

    [Kept]
    struct AllocatedNoTypeCheckStruct;

    [Kept]
    class TypeMapEntryOnly;

    [Kept]
    class AllocatedNoTypeCheckClassTarget;

    [Kept]
    class AlloctedNoTypeCheckStructTarget;

    [Kept(By = Tool.NativeAot)] // Kept by NativeAot by the scanner. It is not kept during codegen.
    class TypeCheckOnlyClass;

    class TargetType4;

    [Kept]
    class ClassWithStaticMethod
    {
        [Kept]
        public static void StaticMethod() { }
    }

    class TargetType5;

    [Kept]
    class ClassWithStaticMethodAndField
    {
        [Kept]
        private static int i;
        [Kept]
        public static int StaticMethod() => i;
    }

    [Kept]
    class UnboxedOnlyTarget;

    [Kept]
    struct UnboxedOnly;

    [Kept]
    class MakeRefTargetType;

    [Kept]
    class RefValueTargetType;

    [Kept]
    struct MakeRef;

    [Kept]
    struct RefValue;

    [Kept(By = Tool.Trimmer)] // NativeAOT can devirtualize the constrained call, so it can remove the interface entirely.
    class ConstrainedTarget;

    [Kept(By = Tool.Trimmer)]
    interface IInterface
    {
        [Kept(By = Tool.Trimmer)]
        void Method();
    }

    [Kept]
    [KeptInterface(typeof(IInterface), By = Tool.Trimmer)]
    struct Constrained : IInterface
    {
        [Kept]
        void IInterface.Method()
        {
            Console.WriteLine("Constrained.Method called");
        }
    }

    [Kept(By = Tool.Trimmer)] // NativeAot can devirtualize the constrained call, so it can remove the interface entirely.
    class ConstraintedStaticTarget;

    [Kept(By = Tool.Trimmer)]
    interface IStaticInterface
    {
        [Kept(By = Tool.Trimmer)]
        static abstract void Method();
    }

    [Kept]
    [KeptInterface(typeof(IStaticInterface), By = Tool.Trimmer)]
    struct ConstrainedStatic : IStaticInterface
    {
        [Kept]
        static void IStaticInterface.Method()
        {
            Console.WriteLine("Constrained.Method called");
        }
    }

    [Kept(By = Tool.Trimmer)] // If LdobjType is never boxed or unboxed, it can be removed by NativeAot.
    class LdobjTarget;

    [Kept(By = Tool.Trimmer)]
    struct LdobjType
    {
        [Kept(By = Tool.Trimmer)]
        public int Value;
    }

    [Kept]
    class ArrayElementTarget;

    [Kept]
    class ArrayElementProxy;

    [Kept]
    struct ArrayElement;

    [Kept(By = Tool.Trimmer)] // If ConstructedNoTypeCheckNoBoxStruct is never boxed or unboxed, it can be removed by NativeAot.
    class ConstructedNoTypeCheckOrBoxTarget;

    [Kept]
    struct ConstructedNoTypeCheckNoBoxStruct
    {
        [Kept]
        public ConstructedNoTypeCheckNoBoxStruct(int i)
        {
            Value = i;
        }

        [Kept]
        [KeptBackingField]
        public int Value { [Kept] get; }
    }

    class TypeCheckOnlyProxy;

    [Kept]
    struct AllocatedNoBoxStructType
    {
        [Kept]
        public AllocatedNoBoxStructType(int value)
        {
            Value = value;
        }

        [Kept]
        [KeptBackingField]
        public int Value { [Kept] get; }
    }

    [Kept]
    class AllocatedNoBoxProxy;
}

// Polyfill for the type map types until we use an LKG runtime that has them with an updated LinkAttributes XML.
namespace System.Runtime.InteropServices
{
    [Kept(By = Tool.Trimmer)]
    [KeptBaseType(typeof(Attribute), By = Tool.Trimmer)]
    [KeptAttributeAttribute(typeof(AttributeUsageAttribute), By = Tool.Trimmer)]
    [KeptAttributeAttribute(typeof(RemoveAttributeInstancesAttribute), By = Tool.Trimmer)]
    [RemoveAttributeInstances]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class TypeMapAttribute<TTypeMapGroup> : Attribute
    {
        [Kept(By = Tool.Trimmer)]
        public TypeMapAttribute(string value, Type target) { }

        [Kept(By = Tool.Trimmer)]
        [KeptAttributeAttribute(typeof(RequiresUnreferencedCodeAttribute), By = Tool.Trimmer)]
        [RequiresUnreferencedCode("Interop types may be removed by trimming")]
        public TypeMapAttribute(string value, Type target, Type trimTarget) { }
    }

    [Kept(By = Tool.Trimmer)]
    [KeptBaseType(typeof(Attribute), By = Tool.Trimmer)]
    [KeptAttributeAttribute(typeof(AttributeUsageAttribute), By = Tool.Trimmer)]
    [KeptAttributeAttribute(typeof(RemoveAttributeInstancesAttribute), By = Tool.Trimmer)]
    [RemoveAttributeInstances]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class TypeMapAssociationAttribute<TTypeMapGroup> : Attribute
    {
        [Kept(By = Tool.Trimmer)]
        public TypeMapAssociationAttribute(Type source, Type proxy) { }
    }

    [Kept(By = Tool.Trimmer)]
    public static class TypeMapping
    {
        [Kept(By = Tool.Trimmer)]
        [KeptAttributeAttribute(typeof(RequiresUnreferencedCodeAttribute), By = Tool.Trimmer)]
        [RequiresUnreferencedCode("Interop types may be removed by trimming")]
        public static IReadOnlyDictionary<string, Type> GetOrCreateExternalTypeMapping<TTypeMapGroup>()
        {
            throw new NotImplementedException($"External type map for {typeof(TTypeMapGroup).Name}");
        }

        [Kept(By = Tool.Trimmer)]
        [KeptAttributeAttribute(typeof(RequiresUnreferencedCodeAttribute), By = Tool.Trimmer)]
        [RequiresUnreferencedCode("Interop types may be removed by trimming")]
        public static IReadOnlyDictionary<Type, Type> GetOrCreateProxyTypeMapping<TTypeMapGroup>()
        {
            throw new NotImplementedException($"Proxy type map for {typeof(TTypeMapGroup).Name}");
        }
    }
}
