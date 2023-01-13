using System;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Serialization
{
	[Reference ("System.Xml.ReaderWriter.dll")]
	[Reference ("System.Xml.XmlSerializer.dll")]
	[SetupCompileArgument ("/unsafe")]
	[SetupLinkerArgument ("--enable-serialization-discovery")]
	public class SerializationTypeRecursion
	{
		public static void Main ()
		{
			// Reference the root type to ensure it is scanned for attributes.
			Type t = typeof (RootTypeRecursive);

			// Construct a serializer to activate the logic
			var ser = new XmlSerializer (typeof (SerializerArgumentType));
		}
	}

	[Kept]
	public class SerializerArgumentType
	{
		// removed
		int f1;
	}

	[Kept]
	[KeptMember (".ctor()")]
	[KeptAttributeAttribute (typeof (XmlRootAttribute))]
	[XmlRoot]
	public class RootTypeRecursive
	{
		// removed
		class UnusedNestedType
		{
		}

		[Kept]
		public FieldType f1;

		// removed
		PrivateFieldType f2;

		[Kept]
		public FieldValueType f3;

		[Kept]
		[KeptBackingField]
		public GetPropertyType p1 { [Kept] get; }

		[Kept]
		[KeptBackingField]
		public GetSetPropertyType p2 { [Kept] get; [Kept] set; }

		// removed
		PrivatePropertyType p3 { get; set; }

		[Kept]
		[KeptBackingField]
		public PublicGetPrivateSetPropertyType p4 { [Kept] get; [Kept] private set; }

		[Kept]
		[KeptBackingField]
		public PrivateGetPublicSetPropertyType p5 { [Kept] private get; [Kept] set; }

		[Kept]
		public RecursiveType f4;

		[Kept]
		public DerivedType f5;

		[Kept]
		public InterfaceImplementingType f6;

		[Kept]
		public NonDefaultCtorType f7;

		[Kept]
		public PrivateCtorType f8;

		[Kept]
		public CctorType f9;

		[Kept]
		public BeforeFieldInitCctorType f10;

		[Kept]
		public MethodType f11;

		[Kept]
		public GenericMembersType f12;

		[Kept]
		public StaticMembersType f13;

		[Kept]
		public MethodsType f14;
	}

	[Kept]
	[KeptMember (".ctor()")]
	public class FieldType
	{
	}

	// removed
	public class PrivateFieldType
	{
	}

	[Kept]
	[KeptMember (".ctor()")]
	public struct FieldValueType
	{
	}

	[Kept]
	[KeptMember (".ctor()")]
	public class GetPropertyType
	{
	}

	[Kept]
	[KeptMember (".ctor()")]
	public class GetSetPropertyType
	{
	}

	// removed
	class PrivatePropertyType
	{
	}

	[Kept]
	[KeptMember (".ctor()")]
	public class PublicGetPrivateSetPropertyType
	{
	}

	[Kept]
	[KeptMember (".ctor()")]
	public class PrivateGetPublicSetPropertyType
	{
	}

	[Kept]
	[KeptMember (".ctor()")]
	public class RecursiveType
	{

		[Kept]
		[KeptMember (".ctor()")]
		public class RecursiveFieldType
		{
			[Kept]
			public int f1;

			// removed
			int f2;
		}

		[Kept]
		public RecursiveFieldType f1;

		// removed
		int f2;
	}

	[Kept]
	[KeptMember (".ctor()")]
	public class BaseType
	{
		[Kept]
		public int f1;
	}

	[Kept]
	[KeptMember (".ctor()")]
	[KeptBaseType (typeof (BaseType))]
	public class DerivedType : BaseType
	{
		[Kept]
		public int f1;
	}

	// removed
	interface InterfaceType
	{
		// removed
		public int P1 { get; }
	}

	[Kept]
	[KeptMember (".ctor()")]
	// removed interface implementation
	public class InterfaceImplementingType : InterfaceType
	{
		[Kept]
		[KeptBackingField]
		public int P1 { [Kept] get; }

		[Kept]
		public int f1;
	}

	[Kept]
	public class NonDefaultCtorType
	{
		// removed
		public NonDefaultCtorType (int i)
		{
		}
	}

	[Kept]
	public class PrivateCtorType
	{
		// removed
		PrivateCtorType ()
		{
		}
	}

	[Kept]
	[KeptMember (".ctor()")]
	public class CctorType
	{
		// Explicit cctors are kept for every marked type,
		// regardless of whether serializers require it.
		[Kept]
		static CctorType ()
		{
		}
	}

	[Kept]
	[KeptMember (".ctor()")]
	// removed cctor
	public class BeforeFieldInitCctorType
	{
		// removed
		public static int i = 1;
	}

	[Kept]
	[KeptMember (".ctor()")]
	public class MethodType
	{
		// removed
		class StaticMethodParameterType
		{
			public int f1;
		}
		// removed
		static void StaticMethod (StaticMethodParameterType p1) { }
		// removed
		class InstanceMethodParameterType
		{
			public int f1;
		}
		// removed
		void InstanceMethod1 (InstanceMethodParameterType p1) { }
		// removed
		class ReturnType
		{
			public int f1;
		}
		// removed
		ReturnType InstanceMethod2 () => null;
	}

	[Kept]
	[KeptMember (".ctor()")]
	public class GenericMembersType
	{
		[Kept]
		[KeptMember (".ctor()")]
		public class GenericFieldType<T> { }

		[Kept]
		[KeptMember (".ctor()")]
		public class GenericFieldParameterType { }

		[Kept]
		public GenericFieldType<GenericFieldParameterType> f1;

		[Kept]
		[KeptMember (".ctor()")]
		public class GenericPropertyType<T> { }

		[Kept]
		[KeptMember (".ctor()")]
		public class GenericPropertyParameterType { }

		[Kept]
		[KeptBackingField]
		public GenericPropertyType<GenericPropertyParameterType> p1 { [Kept] get; }

		[Kept]
		[KeptMember (".ctor()")]
		public class GenericTypeWithMembers<T, U>
		{
			[Kept]
			public T f1;
			[Kept]
			[KeptBackingField]
			public U p1 { [Kept] get; }
		}

		[Kept]
		[KeptMember (".ctor()")]
		public class GenericParameter1 { }

		[Kept]
		[KeptMember (".ctor()")]
		public class GenericParameter2 { }

		[Kept]
		public GenericTypeWithMembers<GenericParameter1, GenericParameter2> f2;

		[Kept]
		[KeptMember (".ctor()")]
		public class GenericBaseType<T, U, V>
		{
			[Kept]
			public T f1;

			[Kept]
			[KeptBackingField]
			public U p1 { [Kept] get; }

			[Kept]
			public V f2;
		}

		[Kept]
		[KeptMember (".ctor()")]
		public class GenericParameter3 { }
		[Kept]
		[KeptMember (".ctor()")]
		public class GenericParameter4 { }

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (GenericBaseType<,,>), typeof (GenericParameter3), typeof (GenericParameter4), "T")]
		public class DerivedFromGenericType<T> : GenericBaseType<GenericParameter3, GenericParameter4, T>
		{
			[Kept]
			public T f1;
		}

		[Kept]
		[KeptMember (".ctor()")]
		public class GenericParameter5 { }

		[Kept]
		public DerivedFromGenericType<GenericParameter5> f3;

		[Kept]
		[KeptMember (".ctor()")]
		public class ArrayItemType
		{
			[Kept]
			public int f1;
		}

		[Kept]
		public ArrayItemType[] f4;

		[Kept]
		public struct PointerType
		{
			[Kept]
			public int f1;
		}

		[Kept]
		public unsafe PointerType* f5;

		[Kept]
		[StructLayout (LayoutKind.Auto)]
		public struct FunctionPointerParameterType
		{
			// removed
			public int f1;
		}

		[Kept]
		[StructLayout (LayoutKind.Auto)]
		// Use auto layout to prevent automatic marking of field types,
		// to demonstrate an edge case where function pointers aren't
		// kept recursively.
		public struct FunctionPointerReturnType
		{
			// removed
			public int f2;
		}

		[Kept]
		public unsafe delegate*<FunctionPointerParameterType, FunctionPointerReturnType> f6;
	}

	[Kept]
	[KeptMember (".ctor()")]
	public class StaticMembersType
	{
		// removed
		public class StaticFieldType
		{
			public int f1;
		}
		// removed
		public static StaticFieldType sf1;
		// removed
		public class StaticPropertyType
		{
			public int f1;
		}
		// removed
		public static StaticPropertyType sp1 { get; }
	}

	[Kept]
	[KeptMember (".ctor()")]
	public class MethodsType
	{
		// removed
		public class ParameterType
		{
			public int f1;
		}
		// removed
		public void MethodWithParameter (ParameterType p1) { }
		// removed
		public class ReturnType
		{
			public int f1;
		}
		// removed
		public ReturnType MethodWithReturnType () => null;

		// removed
		public class StaticParameterType
		{
			public int f1;
		}

		// removed
		public static void StaticMethodWithParameter (StaticParameterType p1) { }
		// removed
		public class StaticReturnType
		{
			public int f1;
		}
		// removed
		public static StaticReturnType StaticMethodWithReturnType () => null;
	}
}
