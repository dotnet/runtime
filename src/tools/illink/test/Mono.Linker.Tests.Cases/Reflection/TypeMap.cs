// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Reflection;

[assembly: TypeMap<UsedTypeMap> ("TrimTargetIsTarget", typeof (TargetAndTrimTarget), typeof (TargetAndTrimTarget))]
[assembly: TypeMap<UsedTypeMap> ("TrimTargetIsUnrelated", typeof (TargetType), typeof (TrimTarget))]
[assembly: TypeMap<UsedTypeMap> ("TrimTargetIsAllocatedNoTypeCheckClass", typeof (TargetType2), typeof (AllocatedNoTypeCheckClass))]
[assembly: TypeMap<UsedTypeMap> ("TrimTargetIsAllocatedNoTypeCheckStruct", typeof (TargetType3), typeof (AllocatedNoTypeCheckStruct))]
[assembly: TypeMap<UsedTypeMap> ("TrimTargetIsUnreferenced", typeof (UnreferencedTargetType), typeof (UnreferencedTrimTarget))]
[assembly: TypeMapAssociation<UsedTypeMap> (typeof (SourceClass), typeof (ProxyType))]
[assembly: TypeMapAssociation<UsedTypeMap> (typeof (TypeCheckOnlyClass), typeof (ProxyType2))]
[assembly: TypeMapAssociation<UsedTypeMap> (typeof (AllocatedNoBoxStructType), typeof (ProxyType3))]
[assembly: TypeMapAssociation<UsedTypeMap> (typeof (I), typeof (IImpl))]
[assembly: TypeMapAssociation<UsedTypeMap> (typeof (IInterfaceWithDynamicImpl), typeof (IDynamicImpl))]

[assembly: TypeMap<UnusedTypeMap> ("UnusedName", typeof (UnusedTargetType), typeof (TrimTarget))]
[assembly: TypeMapAssociation<UsedTypeMap> (typeof (UnusedSourceClass), typeof (UnusedProxyType))]

namespace Mono.Linker.Tests.Cases.Reflection
{
	[Kept]
	[IgnoreTestCase ("Trimmer support is currently not implemented", IgnoredBy = Tool.Trimmer)]
	class TypeMap
	{
		[Kept]
		[ExpectedWarning ("IL2057", "Unrecognized value passed to the parameter 'typeName' of method 'System.Type.GetType(String)'")]
		public static void Main (string[] args)
		{
			object t = Activator.CreateInstance (Type.GetType (args[1]));
			if (t is TargetAndTrimTarget) {
				Console.WriteLine ("Type deriving from TargetAndTrimTarget instantiated.");
			} else if (t is TrimTarget) {
				Console.WriteLine ("Type deriving from TrimTarget instantiated.");
			} else if (t is IInterfaceWithDynamicImpl d) {
				d.Method ();
			} else if (t is TypeCheckOnlyClass typeCheckOnlyClass) {
				Console.WriteLine ("Type deriving from TypeCheckOnlyClass instantiated.");
			}

			Console.WriteLine ("Hash code of SourceClass instance: " + new SourceClass ().GetHashCode ());
			Console.WriteLine ("Hash code of UsedClass instance: " + new UsedClass ().GetHashCode ());
			Console.WriteLine ("Hash code of AllocatedNoTypeCheckClass instance: " + new AllocatedNoTypeCheckClass ().GetHashCode ());
			Console.WriteLine ("Hash code of AllocatedNoTypeCheckStruct instance: " + new AllocatedNoTypeCheckStruct ().GetHashCode ());

			Console.WriteLine (TypeMapping.GetOrCreateExternalTypeMapping<UsedTypeMap> ());
			Console.WriteLine (GetExternalTypeMap<UnusedTypeMap> ());

			var proxyMap = TypeMapping.GetOrCreateProxyTypeMapping<UsedTypeMap> ();

			AllocatedNoBoxStructType allocatedNoBoxStructType = new AllocatedNoBoxStructType (Random.Shared.Next ());
			Console.WriteLine ("AllocatedNoBoxStructType value: " + allocatedNoBoxStructType.Value);
			Console.WriteLine (proxyMap[typeof (AllocatedNoBoxStructType)]);
		}

		[Kept]
		[ExpectedWarning ("IL2124", "Type 'T' must not contain signature variables to be used as a type map group.")]
		private static IReadOnlyDictionary<string, Type> GetExternalTypeMap<T> ()
		{
			return TypeMapping.GetOrCreateExternalTypeMapping<T> ();
		}
	}

	[Kept (By = Tool.Trimmer)]
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
	[KeptMember (".ctor()")]
	class SourceClass;

	[Kept]
	class ProxyType;

	class UnusedTypeMap;
	class UnusedTargetType;
	class UnusedSourceClass;
	class UnusedProxyType;

	interface I;

	interface IImpl : I;

	[Kept]
	[KeptMember (".ctor()")]
	class UsedClass : IImpl;

	[Kept]
	interface IInterfaceWithDynamicImpl
	{
		[Kept (By = Tool.Trimmer)]
		void Method ();
	}

	[Kept]
	[KeptInterface (typeof (IInterfaceWithDynamicImpl))]
	[KeptAttributeAttribute (typeof (DynamicInterfaceCastableImplementationAttribute), By = Tool.Trimmer)]
	[DynamicInterfaceCastableImplementation]
	interface IDynamicImpl : IInterfaceWithDynamicImpl
	{
		[Kept]
		void IInterfaceWithDynamicImpl.Method ()
		{
		}
	}

	[Kept]
	[KeptMember(".ctor()")]
	class AllocatedNoTypeCheckClass;

	[Kept]
	struct AllocatedNoTypeCheckStruct;

	[Kept]
	class TargetType2;

	[Kept]
	class TargetType3;

	[Kept]
	class TypeCheckOnlyClass;

	class ProxyType2;

	[Kept]
	struct AllocatedNoBoxStructType
	{
		[Kept]
		public AllocatedNoBoxStructType (int value)
		{
			Value = value;
		}

		[Kept]
		public int Value { [Kept] get; }
	}

	[Kept]
	class ProxyType3;
}

// Polyfill for the type map types until we use an LKG runtime that has it.
namespace System.Runtime.InteropServices
{
	[Kept (By = Tool.Trimmer)]
	[KeptBaseType (typeof (Attribute), By = Tool.Trimmer)]
	[KeptAttributeAttribute (typeof (AttributeUsageAttribute), By = Tool.Trimmer)]
	[AttributeUsage (AttributeTargets.Assembly, AllowMultiple = true)]
	public sealed class TypeMapAttribute<TTypeMapGroup> : Attribute
	{
		[Kept (By = Tool.Trimmer)]
		public TypeMapAttribute (string value, Type target) { }

		[Kept (By = Tool.Trimmer)]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute), By = Tool.Trimmer)]
		[RequiresUnreferencedCode ("Interop types may be removed by trimming")]
		public TypeMapAttribute (string value, Type target, Type trimTarget) { }
	}

	[Kept (By = Tool.Trimmer)]
	[KeptBaseType (typeof (Attribute), By = Tool.Trimmer)]
	[KeptAttributeAttribute (typeof (AttributeUsageAttribute), By = Tool.Trimmer)]
	[AttributeUsage (AttributeTargets.Assembly, AllowMultiple = true)]
	public sealed class TypeMapAssociationAttribute<TTypeMapGroup> : Attribute
	{
		[Kept (By = Tool.Trimmer)]
		public TypeMapAssociationAttribute (Type source, Type proxy) { }
	}

	[Kept(By = Tool.Trimmer)]
	public static class TypeMapping
	{
		[Kept(By = Tool.Trimmer)]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute), By = Tool.Trimmer)]
		[RequiresUnreferencedCode ("Interop types may be removed by trimming")]
		public static IReadOnlyDictionary<string, Type> GetOrCreateExternalTypeMapping<TTypeMapGroup> ()
		{
			throw new NotImplementedException ();
		}

		[Kept(By = Tool.Trimmer)]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute), By = Tool.Trimmer)]
		[RequiresUnreferencedCode ("Interop types may be removed by trimming")]
		public static IReadOnlyDictionary<Type, Type> GetOrCreateProxyTypeMapping<TTypeMapGroup> ()
		{
			throw new NotImplementedException ();
		}
	}
}

