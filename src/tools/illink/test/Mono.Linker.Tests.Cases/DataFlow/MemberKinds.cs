// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	public class MemberKinds
	{
		public static void Main ()
		{
			RequireDefaultConstructor (typeof (DefaultConstructorType));
			RequireDefaultConstructor (typeof (PrivateParameterlessConstructorType));
			RequireDefaultConstructor (typeof (DefaultConstructorBeforeFieldInitType));
			RequirePublicConstructors (typeof (PublicConstructorsType));
			RequirePublicConstructors (typeof (PublicConstructorsBeforeFieldInitType));
			RequireConstructors (typeof (ConstructorsType));
			RequireConstructors (typeof (ConstructorsBeforeFieldInitType));
			RequirePublicMethods (typeof (PublicMethodsType));
			RequireMethods (typeof (MethodsType));
			RequirePublicFields (typeof (PublicFieldsType));
			RequireFields (typeof (FieldsType));
			RequirePublicNestedTypes (typeof (PublicNestedTypesType));
			RequireNestedTypes (typeof (NestedTypesType));
			RequirePublicProperties (typeof (PublicPropertiesType));
			RequireProperties (typeof (PropertiesType));
			RequirePublicEvents (typeof (PublicEventsType));
			RequireEvents (typeof (EventsType));
		}


		[Kept]
		private static void RequireDefaultConstructor (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.DefaultConstructor)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type)
		{
		}

		[Kept]
		class DefaultConstructorBaseType
		{
			[Kept]
			public DefaultConstructorBaseType () { }

			public DefaultConstructorBaseType (int i) { }
		}

		[Kept]
		[KeptBaseType (typeof (DefaultConstructorBaseType))]
		class DefaultConstructorType : DefaultConstructorBaseType
		{
			[Kept]
			public DefaultConstructorType () { }

			public DefaultConstructorType (int i) { }

			private DefaultConstructorType (int i, int j) { }

			// Not implied by the DynamicallyAccessedMemberKinds logic, but
			// explicit cctors would be kept by the linker.
			// [Kept]
			// static DefaultConstructorType () { }

			public void Method1 () { }
			public bool Property1 { get; set; }
			public bool Field1;
		}

		[Kept]
		class DefaultConstructorBeforeFieldInitType
		{
			static int i = 10;

			[Kept]
			public DefaultConstructorBeforeFieldInitType () { }
		}

		[Kept]
		class PrivateParameterlessConstructorBaseType
		{
			protected PrivateParameterlessConstructorBaseType () { }

			PrivateParameterlessConstructorBaseType (int i) { }
		}

		[Kept]
		[KeptBaseType (typeof (PrivateParameterlessConstructorBaseType))]
		class PrivateParameterlessConstructorType : PrivateParameterlessConstructorBaseType
		{
			PrivateParameterlessConstructorType () { }

			public PrivateParameterlessConstructorType (int i) { }

			public void Method1 () { }

			public bool Property1 { get; set; }

			public bool Field1;
		}

		[Kept]
		private static void RequirePublicConstructors (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.PublicConstructors)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type)
		{
		}

		[Kept]
		class PublicConstructorsBaseType
		{
			[Kept]
			public PublicConstructorsBaseType () { }

			public PublicConstructorsBaseType (int i) { }
		}

		[Kept]
		[KeptBaseType (typeof (PublicConstructorsBaseType))]
		class PublicConstructorsType : PublicConstructorsBaseType
		{
			private PublicConstructorsType () { }

			[Kept]
			public PublicConstructorsType (int i) { }

			private PublicConstructorsType (int i, int j) { }

			// Not implied by the DynamicallyAccessedMemberKinds logic, but
			// explicit cctors would be kept by the linker.
			// [Kept]
			// static PublicConstructorsType () { }

			public void Method1 () { }
			public bool Property1 { get; set; }
			public bool Field1;
		}

		class PublicConstructorsBeforeFieldInitType
		{
			static int i = 10;

			[Kept]
			public PublicConstructorsBeforeFieldInitType () { }
		}


		[Kept]
		private static void RequireConstructors (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.Constructors)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type)
		{
		}

		[Kept]
		class ConstructorsBaseType
		{
			[Kept]
			protected ConstructorsBaseType () { }

			protected ConstructorsBaseType (int i) { }
		}

		[Kept]
		[KeptBaseType (typeof (ConstructorsBaseType))]
		class ConstructorsType : ConstructorsBaseType
		{
			[Kept]
			private ConstructorsType () { }

			[Kept]
			public ConstructorsType (int i) { }

			[Kept]
			private ConstructorsType (int i, int j) { }

			// Kept by the DynamicallyAccessedMembers logic
			[Kept]
			static ConstructorsType () { }

			public void Method1 () { }
			public bool Property1 { get; set; }
			public bool Field1;
		}

		[Kept]
		class ConstructorsBeforeFieldInitType
		{
			[Kept]
			public int i = 10;

			[Kept]
			public ConstructorsBeforeFieldInitType () { }
		}

		[Kept]
		private static void RequirePublicMethods (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.PublicMethods)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type)
		{
		}

		[Kept]
		class PublicMethodsBaseType
		{
			[Kept]
			public void PublicBaseMethod () { }
			private void PrivateBaseMethod () { }
			protected void ProtectedBaseMethod () { }
			[Kept]
			public void HideMethod () { }

			[Kept]
			[KeptBackingField]
			public bool PublicPropertyOnBase { [Kept] get; [Kept] set; }
			protected bool ProtectedPropertyOnBase { get; set; }
			private bool PrivatePropertyOnBase { get; set; }
			[Kept]
			[KeptBackingField]
			public bool HideProperty { [Kept] get; [Kept] set; }

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> PublicEventOnBase;
			protected event EventHandler<EventArgs> ProtectedEventOnBase;
			private event EventHandler<EventArgs> PrivateEventOnBase;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> HideEvent;

			[Kept]
			public static void PublicStaticBaseMethod () { }
			private static void PrivateStaticBaseMethod () { }
			protected static void ProtectedStaticBaseMethod () { }
			[Kept]
			public static void HideStaticMethod () { }

			[Kept]
			[KeptBackingField]
			static public bool PublicStaticPropertyOnBase { [Kept] get; [Kept] set; }
			static protected bool ProtectedStaticPropertyOnBase { get; set; }
			static private bool PrivateStaticPropertyOnBase { get; set; }
			[Kept]
			[KeptBackingField]
			static public bool HideStaticProperty { [Kept] get; [Kept] set; }

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public static event EventHandler<EventArgs> PublicStaticEventOnBase;
			protected static event EventHandler<EventArgs> ProtectedStaticEventOnBase;
			private static event EventHandler<EventArgs> PrivateStaticEventOnBase;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public static event EventHandler<EventArgs> HideStaticEvent;
		}

		[Kept]
		[KeptBaseType (typeof (PublicMethodsBaseType))]
		class PublicMethodsType : PublicMethodsBaseType
		{
			public PublicMethodsType () { }

			[Kept]
			public void PublicMethod1 () { }
			[Kept]
			public bool PublicMethod2 (int i) { return false; }

			internal void InternalMethod () { }
			protected void ProtectedMethod () { }
			private void PrivateMethod () { }
			[Kept]
			public void HideMethod () { }

			[Kept]
			[KeptBackingField]
			public bool PublicProperty { [Kept] get; [Kept] set; }
			protected bool ProtectedProperty { get; set; }
			private bool PrivateProperty { get; set; }
			[Kept]
			[KeptBackingField]
			public bool HideProperty { [Kept] get; [Kept] set; }

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> PublicEvent;
			protected event EventHandler<EventArgs> ProtectedEvent;
			private event EventHandler<EventArgs> PrivateEvent;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> HideEvent;

			[Kept]
			public static void PublicStaticMethod () { }
			private static void PrivateStaticMethod () { }
			protected static void ProtectedStaticMethod () { }
			[Kept]
			public static void HideStaticMethod () { }

			[Kept]
			[KeptBackingField]
			static public bool PublicStaticProperty { [Kept] get; [Kept] set; }
			static protected bool ProtectedStaticProperty { get; set; }
			static private bool PrivateStaticProperty { get; set; }
			[Kept]
			[KeptBackingField]
			static public bool HideStaticProperty { [Kept] get; [Kept] set; }

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public static event EventHandler<EventArgs> PublicStaticEvent;
			protected static event EventHandler<EventArgs> ProtectedStaticEvent;
			private static event EventHandler<EventArgs> PrivateStaticEvent;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public static event EventHandler<EventArgs> HideStaticEvent;
		}


		[Kept]
		private static void RequireMethods (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.Methods)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type)
		{
		}

		[Kept]
		class MethodsBaseType
		{
			[Kept]
			public void PublicBaseMethod () { }
			private void PrivateBaseMethod () { }
			[Kept]
			protected void ProtectedBaseMethod () { }
			[Kept]
			public void HideMethod () { }

			[Kept]
			[KeptBackingField]
			public bool PublicPropertyOnBase { [Kept] get; [Kept] set; }
			[Kept]
			[KeptBackingField]
			protected bool ProtectedPropertyOnBase { [Kept] get; [Kept] set; }
			private bool PrivatePropertyOnBase { get; set; }
			[Kept]
			[KeptBackingField]
			public bool HideProperty { [Kept] get; [Kept] set; }

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> PublicEventOnBase;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			protected event EventHandler<EventArgs> ProtectedEventOnBase;
			private event EventHandler<EventArgs> PrivateEventOnBase;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> HideEvent;

			[Kept]
			public static void PublicStaticBaseMethod () { }
			private static void PrivateStaticBaseMethod () { }
			[Kept]
			protected static void ProtectedStaticBaseMethod () { }
			[Kept]
			public static void HideStaticMethod () { }

			[Kept]
			[KeptBackingField]
			static public bool PublicStaticPropertyOnBase { [Kept] get; [Kept] set; }
			[Kept]
			[KeptBackingField]
			static protected bool ProtectedStaticPropertyOnBase { [Kept] get; [Kept] set; }
			static private bool PrivateStaticPropertyOnBase { get; set; }
			[Kept]
			[KeptBackingField]
			static public bool HideStaticProperty { [Kept] get; [Kept] set; }

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public static event EventHandler<EventArgs> PublicStaticEventOnBase;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			protected static event EventHandler<EventArgs> ProtectedStaticEventOnBase;
			private static event EventHandler<EventArgs> PrivateStaticEventOnBase;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public static event EventHandler<EventArgs> HideStaticEvent;
		}

		[Kept]
		[KeptBaseType (typeof (MethodsBaseType))]
		class MethodsType : MethodsBaseType
		{
			public MethodsType () { }

			[Kept]
			public void PublicMethod1 () { }
			[Kept]
			public bool PublicMethod2 (int i) { return false; }

			[Kept]
			internal void InternalMethod () { }
			[Kept]
			protected void ProtectedMethod () { }
			[Kept]
			private void PrivateMethod () { }
			[Kept]
			public void HideMethod () { }

			[Kept]
			[KeptBackingField]
			public bool PublicProperty { [Kept] get; [Kept] set; }
			[Kept]
			[KeptBackingField]
			protected bool ProtectedProperty { [Kept] get; [Kept] set; }
			[Kept]
			[KeptBackingField]
			private bool PrivateProperty { [Kept] get; [Kept] set; }
			[Kept]
			[KeptBackingField]
			public bool HideProperty { [Kept] get; [Kept] set; }

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> PublicEvent;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			protected event EventHandler<EventArgs> ProtectedEvent;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			private event EventHandler<EventArgs> PrivateEvent;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> HideEvent;

			[Kept]
			public static void PublicStaticMethod () { }
			[Kept]
			private static void PrivateStaticMethod () { }
			[Kept]
			protected static void ProtectedStaticMethod () { }
			[Kept]
			public static void HideStaticMethod () { }

			[Kept]
			[KeptBackingField]
			static public bool PublicStaticProperty { [Kept] get; [Kept] set; }
			[Kept]
			[KeptBackingField]
			static protected bool ProtectedStaticProperty { [Kept] get; [Kept] set; }
			[Kept]
			[KeptBackingField]
			static private bool PrivateStaticProperty { [Kept] get; [Kept] set; }
			[Kept]
			[KeptBackingField]
			static public bool HideStaticProperty { [Kept] get; [Kept] set; }

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public static event EventHandler<EventArgs> PublicStaticEvent;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			protected static event EventHandler<EventArgs> ProtectedStaticEvent;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			private static event EventHandler<EventArgs> PrivateStaticEvent;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public static event EventHandler<EventArgs> HideStaticEvent;

			public bool Field1;
		}


		[Kept]
		private static void RequirePublicFields (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.PublicFields)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type)
		{
		}

		[Kept]
		class PublicFieldsBaseType
		{
			[Kept]
			public bool PublicBaseField;
			protected bool ProtectedBaseField;
			private bool PrivateBaseField;
			[Kept]
			public bool HideField;

			// Backing fields are private, so they are not accessible from a derived type
			public bool PublicPropertyOnBase { get; set; }
			protected bool ProtectedPropertyOnBase { get; set; }
			private bool PrivatePropertyOnBase { get; set; }

			public event EventHandler<EventArgs> PublicEventOnBase;
			protected event EventHandler<EventArgs> ProtectedEventOnBase;
			private event EventHandler<EventArgs> PrivateEventOnBase;

			[Kept]
			static public bool StaticPublicBaseField;
			static protected bool StaticProtectedBaseField;
			static private bool StaticPrivateBaseField;
			[Kept]
			static public bool HideStaticField;

			static public bool PublicStaticPropertyOnBase { get; set; }
			static protected bool ProtectedStaticPropertyOnBase { get; set; }
			static private bool PrivateStaticPropertyOnBase { get; set; }
			static public bool HideStaticProperty { get; set; }

			public static event EventHandler<EventArgs> PublicStaticEventOnBase;
			protected static event EventHandler<EventArgs> ProtectedStaticEventOnBase;
			private static event EventHandler<EventArgs> PrivateStaticEventOnBase;
			public static event EventHandler<EventArgs> HideStaticEvent;
		}

		[Kept]
		[KeptBaseType (typeof (PublicFieldsBaseType))]
		class PublicFieldsType : PublicFieldsBaseType
		{
			[Kept]
			public bool PublicField;
			[Kept]
			public string PublicStringField;
			internal bool InternalField;
			protected bool ProtectedField;
			private bool PrivateField;
			[Kept]
			public bool HideField;

			// Backing fields are all private
			public bool PublicProperty { get; set; }
			protected bool ProtectedProperty { get; set; }
			private bool PrivateProperty { get; set; }
			public bool HideProperty { get; set; }

			public event EventHandler<EventArgs> PublicEvent;
			protected event EventHandler<EventArgs> ProtectedEvent;
			private event EventHandler<EventArgs> PrivateEvent;
			public event EventHandler<EventArgs> HideEvent;

			[Kept]
			static public bool StaticPublicField;
			[Kept]
			static public string StaticPublicStringField;
			static protected bool StaticProtectedField;
			static private bool StaticPrivateField;
			[Kept]
			static public bool HideStaticField;

			static public bool PublicStaticProperty { get; set; }
			static protected bool ProtectedStaticProperty { get; set; }
			static private bool PrivateStaticProperty { get; set; }
			static public bool HideStaticProperty { get; set; }

			public static event EventHandler<EventArgs> PublicStaticEvent;
			protected static event EventHandler<EventArgs> ProtectedStaticEvent;
			private static event EventHandler<EventArgs> PrivateStaticEvent;
			public static event EventHandler<EventArgs> HideStaticEvent;
		}


		[Kept]
		private static void RequireFields (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.Fields)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type)
		{
		}

		[Kept]
		class FieldsBaseType
		{
			[Kept]
			public bool PublicBaseField;
			[Kept]
			protected bool ProtectedBaseField;
			private bool PrivateBaseField;
			[Kept]
			public bool HideField;

			// Backing fields are private, so they are not accessible from a derived type
			public bool PublicPropertyOnBase { get; set; }
			protected bool ProtectedPropertyOnBase { get; set; }
			private bool PrivatePropertyOnBase { get; set; }

			public event EventHandler<EventArgs> PublicEventOnBase;
			protected event EventHandler<EventArgs> ProtectedEventOnBase;
			private event EventHandler<EventArgs> PrivateEventOnBase;

			[Kept]
			static public bool StaticPublicBaseField;
			[Kept]
			static protected bool StaticProtectedBaseField;
			static private bool StaticPrivateBaseField;
			[Kept]
			static public bool HideStaticField;

			static public bool PublicStaticPropertyOnBase { get; set; }
			static protected bool ProtectedStaticPropertyOnBase { get; set; }
			static private bool PrivateStaticPropertyOnBase { get; set; }
			static public bool HideStaticProperty { get; set; }

			public static event EventHandler<EventArgs> PublicStaticEventOnBase;
			protected static event EventHandler<EventArgs> ProtectedStaticEventOnBase;
			private static event EventHandler<EventArgs> PrivateStaticEventOnBase;
			public static event EventHandler<EventArgs> HideStaticEvent;
		}

		[Kept]
		[KeptBaseType (typeof (FieldsBaseType))]
		class FieldsType : FieldsBaseType
		{
			[Kept]
			public bool PublicField;
			[Kept]
			public string PublicStringField;
			[Kept]
			internal bool InternalField;
			[Kept]
			protected bool ProtectedField;
			[Kept]
			private bool PrivateField;
			[Kept]
			public bool HideField;

			[KeptBackingField]
			public bool PublicProperty { get; set; }
			[KeptBackingField]
			protected bool ProtectedProperty { get; set; }
			[KeptBackingField]
			private bool PrivateProperty { get; set; }
			[KeptBackingField]
			public bool HideProperty { get; set; }

			[KeptBackingField]
			public event EventHandler<EventArgs> PublicEvent;
			[KeptBackingField]
			protected event EventHandler<EventArgs> ProtectedEvent;
			[KeptBackingField]
			private event EventHandler<EventArgs> PrivateEvent;
			[KeptBackingField]
			public event EventHandler<EventArgs> HideEvent;

			[Kept]
			static public bool StaticPublicField;
			[Kept]
			static public string StaticPublicStringField;
			[Kept]
			static protected bool StaticProtectedField;
			[Kept]
			static private bool StaticPrivateField;
			[Kept]
			static public bool HideStaticField;

			[KeptBackingField]
			static public bool PublicStaticProperty { get; set; }
			[KeptBackingField]
			static protected bool ProtectedStaticProperty { get; set; }
			[KeptBackingField]
			static private bool PrivateStaticProperty { get; set; }
			[KeptBackingField]
			static public bool HideStaticProperty { get; set; }

			[KeptBackingField]
			public static event EventHandler<EventArgs> PublicStaticEvent;
			[KeptBackingField]
			protected static event EventHandler<EventArgs> ProtectedStaticEvent;
			[KeptBackingField]
			private static event EventHandler<EventArgs> PrivateStaticEvent;
			[KeptBackingField]
			public static event EventHandler<EventArgs> HideStaticEvent;
		}


		[Kept]
		private static void RequirePublicNestedTypes (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.PublicNestedTypes)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type)
		{
		}

		[Kept]
		class PublicNestedTypesBaseType
		{
			// Nested types are not propagated from base class at all
			public class PublicBaseNestedType { }
			protected class ProtectedBaseNestedType { }
			private class PrivateBaseNestedType { }
			public class HideBaseNestedType { }
		}

		[Kept]
		[KeptBaseType (typeof (PublicNestedTypesBaseType))]
		class PublicNestedTypesType : PublicNestedTypesBaseType
		{
			[Kept]
			public class PublicNestedType { }
			protected class ProtectedNestedType { }
			private class PrivateNestedType { }
			[Kept]
			public class HideNestedType { }
		}


		[Kept]
		private static void RequireNestedTypes (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.NestedTypes)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type)
		{
		}

		[Kept]
		class NestedTypesBaseType
		{
			// Nested types are not propagated from base class at all
			public class PublicBaseNestedType { }
			protected class ProtectedBaseNestedType { }
			private class PrivateBaseNestedType { }
			public class HideBaseNestedType { }
		}

		[Kept]
		[KeptBaseType (typeof (NestedTypesBaseType))]
		class NestedTypesType : NestedTypesBaseType
		{
			[Kept]
			public class PublicNestedType { }
			[Kept]
			protected class ProtectedNestedType { }
			[Kept]
			private class PrivateNestedType { }
			[Kept]
			public class HideNestedType { }
		}


		[Kept]
		private static void RequirePublicProperties (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.PublicProperties)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type)
		{
		}

		[Kept]
		class PublicPropertiesBaseType
		{
			[Kept]
			[KeptBackingField]
			public bool PublicPropertyOnBase { [Kept] get; [Kept] set; }
			[Kept]
			public bool PublicPropertyGetterOnBase { [Kept] get { return false; } [Kept] private set { } }
			[Kept]
			public bool PublicPropertySetterOnBase { [Kept] private get { return false; } [Kept] set { } }
			[Kept]
			public bool PublicPropertyOnlyGetterOnBase { [Kept] get { return false; } }
			[Kept]
			public bool PublicPropertyOnlySetterOnBase { [Kept] set { } }
			protected bool ProtectedPropertyOnBase { get; set; }
			private bool PrivatePropertyOnBase { get; set; }
			[Kept]
			[KeptBackingField]
			public bool HideProperty { [Kept] get; [Kept] set; }

			[Kept]
			[KeptBackingField]
			static public bool PublicStaticPropertyOnBase { [Kept] get; [Kept] set; }
			static protected bool ProtectedStaticPropertyOnBase { get; set; }
			static private bool PrivateStaticPropertyOnBase { get; set; }
			[Kept]
			[KeptBackingField]
			static public bool HideStaticProperty { [Kept] get; [Kept] set; }
		}

		[Kept]
		[KeptBaseType (typeof (PublicPropertiesBaseType))]
		class PublicPropertiesType : PublicPropertiesBaseType
		{
			[Kept]
			[KeptBackingField]
			public bool PublicProperty { [Kept] get; [Kept] set; }
			[Kept]
			public bool PublicPropertyGetter { [Kept] get { return false; } [Kept] private set { } }
			[Kept]
			public bool PublicPropertySetter { [Kept] private get { return false; } [Kept] set { } }
			[Kept]
			public bool PublicPropertyOnlyGetter { [Kept] get { return false; } }
			[Kept]
			public bool PublicPropertyOnlySetter { [Kept] set { } }
			protected bool ProtectedProperty { get; set; }
			private bool PrivateProperty { get; set; }
			[Kept]
			[KeptBackingField]
			public bool HideProperty { [Kept] get; [Kept] set; }

			[Kept]
			[KeptBackingField]
			static public bool PublicStaticProperty { [Kept] get; [Kept] set; }
			static protected bool ProtectedStaticProperty { get; set; }
			static private bool PrivateStaticProperty { get; set; }
			[Kept]
			[KeptBackingField]
			static public bool HideStaticProperty { [Kept] get; [Kept] set; }
		}


		[Kept]
		private static void RequireProperties (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.Properties)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type)
		{
		}

		[Kept]
		class PropertiesBaseType
		{
			[Kept]
			[KeptBackingField]
			public bool PublicPropertyOnBase { [Kept] get; [Kept] set; }
			[Kept]
			public bool PublicPropertyGetterOnBase { [Kept] get { return false; } [Kept] private set { } }
			[Kept]
			public bool PublicPropertySetterOnBase { [Kept] private get { return false; } [Kept] set { } }
			[Kept]
			public bool PublicPropertyOnlyGetterOnBase { [Kept] get { return false; } }
			[Kept]
			public bool PublicPropertyOnlySetterOnBase { [Kept] set { } }
			[Kept]
			[KeptBackingField]
			protected bool ProtectedPropertyOnBase { [Kept] get; [Kept] set; }
			private bool PrivatePropertyOnBase { get; set; }
			[Kept]
			[KeptBackingField]
			public bool HideProperty { [Kept] get; [Kept] set; }

			[Kept]
			[KeptBackingField]
			static public bool PublicStaticPropertyOnBase { [Kept] get; [Kept] set; }
			[Kept]
			[KeptBackingField]
			static protected bool ProtectedStaticPropertyOnBase { [Kept] get; [Kept] set; }
			static private bool PrivateStaticPropertyOnBase { get; set; }
			[Kept]
			[KeptBackingField]
			static public bool HideStaticProperty { [Kept] get; [Kept] set; }
		}

		[Kept]
		[KeptBaseType (typeof (PropertiesBaseType))]
		class PropertiesType : PropertiesBaseType
		{
			[Kept]
			[KeptBackingField]
			public bool PublicProperty { [Kept] get; [Kept] set; }
			[Kept]
			public bool PublicPropertyGetter { [Kept] get { return false; } [Kept] private set { } }
			[Kept]
			public bool PublicPropertySetter { [Kept] private get { return false; } [Kept] set { } }
			[Kept]
			public bool PublicPropertyOnlyGetter { [Kept] get { return false; } }
			[Kept]
			public bool PublicPropertyOnlySetter { [Kept] set { } }
			[Kept]
			[KeptBackingField]
			protected bool ProtectedProperty { [Kept] get; [Kept] set; }
			[Kept]
			[KeptBackingField]
			private bool PrivateProperty { [Kept] get; [Kept] set; }
			[Kept]
			[KeptBackingField]
			public bool HideProperty { [Kept] get; [Kept] set; }

			[Kept]
			[KeptBackingField]
			static public bool PublicStaticProperty { [Kept] get; [Kept] set; }
			[Kept]
			[KeptBackingField]
			static protected bool ProtectedStaticProperty { [Kept] get; [Kept] set; }
			[Kept]
			[KeptBackingField]
			static private bool PrivateStaticProperty { [Kept] get; [Kept] set; }
			[Kept]
			[KeptBackingField]
			static public bool HideStaticProperty { [Kept] get; [Kept] set; }
		}


		[Kept]
		private static void RequirePublicEvents (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.PublicEvents)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type)
		{
		}

		[Kept]
		class PublicEventsBaseType
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> PublicEventOnBase;
			protected event EventHandler<EventArgs> ProtectedEventOnBase;
			private event EventHandler<EventArgs> PrivateEventOnBase;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> HideEvent;

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			static public event EventHandler<EventArgs> PublicStaticEventOnBase;
			static protected event EventHandler<EventArgs> ProtectedStaticEventOnBase;
			static private event EventHandler<EventArgs> PrivateStaticEventOnBase;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			static public event EventHandler<EventArgs> HideStaticEvent;
		}

		[Kept]
		[KeptBaseType (typeof (PublicEventsBaseType))]
		class PublicEventsType : PublicEventsBaseType
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> PublicEvent;
			protected event EventHandler<EventArgs> ProtectedEvent;
			private event EventHandler<EventArgs> PrivateEvent;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> HideEvent;

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			static public event EventHandler<EventArgs> PublicStaticEvent;
			static protected event EventHandler<EventArgs> ProtectedStaticEvent;
			static private event EventHandler<EventArgs> PrivateStaticEvent;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			static public event EventHandler<EventArgs> HideStaticEvent;
		}


		[Kept]
		private static void RequireEvents (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.Events)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type)
		{
		}

		[Kept]
		class EventsBaseType
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> PublicEventOnBase;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			protected event EventHandler<EventArgs> ProtectedEventOnBase;
			private event EventHandler<EventArgs> PrivateEventOnBase;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> HideEvent;

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			static public event EventHandler<EventArgs> PublicStaticEventOnBase;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			static protected event EventHandler<EventArgs> ProtectedStaticEventOnBase;
			static private event EventHandler<EventArgs> PrivateStaticEventOnBase;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			static public event EventHandler<EventArgs> HideStaticEvent;
		}

		[Kept]
		[KeptBaseType (typeof (EventsBaseType))]
		class EventsType : EventsBaseType
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> PublicEvent;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			protected event EventHandler<EventArgs> ProtectedEvent;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			private event EventHandler<EventArgs> PrivateEvent;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler<EventArgs> HideEvent;

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			static public event EventHandler<EventArgs> PublicStaticEvent;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			static protected event EventHandler<EventArgs> ProtectedStaticEvent;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			static private event EventHandler<EventArgs> PrivateStaticEvent;
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			static public event EventHandler<EventArgs> HideStaticEvent;
		}
	}
}
