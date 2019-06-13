// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Security;
using System.Runtime.Serialization;

namespace System
{
	// Contains information about the type which is expensive to compute
	[StructLayout (LayoutKind.Sequential)]
	internal class MonoTypeInfo {
		// this is the displayed form: special characters
		// ,+*&*[]\ in the identifier portions of the names
		// have been escaped with a leading backslash (\)
		public string full_name;
		public RuntimeConstructorInfo default_ctor;
	}

	[StructLayout (LayoutKind.Sequential)]
	partial class RuntimeType
	{
#region keep in sync with object-internals.h
		[NonSerialized]
		MonoTypeInfo type_info;
#endregion

		internal Object GenericCache;

		internal RuntimeType (Object obj)
		{
			throw new NotImplementedException ();
		}

		internal RuntimeConstructorInfo GetDefaultConstructor ()
		{
			RuntimeConstructorInfo ctor = null;

			if (type_info == null)
				type_info = new MonoTypeInfo ();
			else
				ctor = type_info.default_ctor;

			if (ctor == null) {
				var ctors = GetConstructors (BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

				for (int i = 0; i < ctors.Length; ++i) {
					if (ctors [i].GetParametersCount () == 0) {
						type_info.default_ctor = ctor = (RuntimeConstructorInfo) ctors [i];
						break;
					}
				}
			}

			return ctor;
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern MethodInfo GetCorrespondingInflatedMethod (MethodInfo generic);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern ConstructorInfo GetCorrespondingInflatedConstructor (ConstructorInfo generic);

		internal override MethodInfo GetMethod (MethodInfo fromNoninstanciated)
		{
			if (fromNoninstanciated == null)
				throw new ArgumentNullException ("fromNoninstanciated");
			return GetCorrespondingInflatedMethod (fromNoninstanciated);
		}

		internal override ConstructorInfo GetConstructor (ConstructorInfo fromNoninstanciated)
		{
			if (fromNoninstanciated == null)
				throw new ArgumentNullException ("fromNoninstanciated");
			return GetCorrespondingInflatedConstructor (fromNoninstanciated);
		}

		internal override FieldInfo GetField (FieldInfo fromNoninstanciated)
		{
			/* create sensible flags from given FieldInfo */
			BindingFlags flags = fromNoninstanciated.IsStatic ? BindingFlags.Static : BindingFlags.Instance;
			flags |= fromNoninstanciated.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic;
			return GetField (fromNoninstanciated.Name, flags);
		}

		string GetDefaultMemberName ()
		{
			object [] att = GetCustomAttributes (typeof (DefaultMemberAttribute), true);
			return att.Length != 0 ? ((DefaultMemberAttribute) att [0]).MemberName : null;
		}

		RuntimeConstructorInfo m_serializationCtor;
		internal RuntimeConstructorInfo GetSerializationCtor()
		{
			if (m_serializationCtor == null) {
				var s_SICtorParamTypes = new Type[] { typeof(SerializationInfo), typeof(StreamingContext) };

				m_serializationCtor = GetConstructor(
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
					null,
					CallingConventions.Any,
					s_SICtorParamTypes,
					null) as RuntimeConstructorInfo;
			}

			return m_serializationCtor;
		}

		internal Object CreateInstanceSlow(bool publicOnly, bool wrapExceptions, bool skipCheckThis, bool fillCache)
		{
			//bool bNeedSecurityCheck = true;
			//bool bCanBeCached = false;
			//bool bSecurityCheckOff = false;

			if (!skipCheckThis)
				CreateInstanceCheckThis();

			//if (!fillCache)
			//	bSecurityCheckOff = true;

			return CreateInstanceMono (!publicOnly, wrapExceptions);
		}

		object CreateInstanceMono (bool nonPublic, bool wrapExceptions)
		{
			var ctor = GetDefaultConstructor ();
			if (!nonPublic && ctor != null && !ctor.IsPublic) {
				throw new MissingMethodException(SR.Format(SR.Arg_NoDefCTor, FullName));
			}

			if (ctor == null) {
				Type elementType = this.GetRootElementType();
				if (ReferenceEquals (elementType, typeof (TypedReference)) || ReferenceEquals (elementType, typeof (RuntimeArgumentHandle)))
					throw new NotSupportedException (Environment.GetResourceString ("NotSupported_ContainsStackPtr"));

				if (IsValueType)
					return CreateInstanceInternal (this);

				throw new MissingMethodException ("Default constructor not found for type " + FullName);
			}

			// TODO: .net does more checks in unmanaged land in RuntimeTypeHandle::CreateInstance
			if (IsAbstract) {
				throw new MissingMethodException ("Cannot create an abstract class '{0}'.", FullName);
			}

			return ctor.InternalInvoke (null, null, wrapExceptions);
		}

		internal Object CheckValue (Object value, Binder binder, CultureInfo culture, BindingFlags invokeAttr)
		{
			bool failed = false;
			var res = TryConvertToType (value, ref failed);
			if (!failed)
				return res;

			if ((invokeAttr & BindingFlags.ExactBinding) == BindingFlags.ExactBinding)
				throw new ArgumentException(String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Arg_ObjObjEx"), value.GetType(), this));

			if (binder != null && binder != Type.DefaultBinder)
				return binder.ChangeType (value, this, culture);

			throw new ArgumentException(String.Format(CultureInfo.CurrentUICulture, Environment.GetResourceString("Arg_ObjObjEx"), value.GetType(), this));
		}

		object TryConvertToType (object value, ref bool failed)
		{
			if (IsInstanceOfType (value)) {
				return value;
			}

			if (IsByRef) {
				var elementType = GetElementType ();
				if (value == null || elementType.IsInstanceOfType (value)) {
					return value;
				}
			}

			if (value == null)
				return value;

			if (IsEnum) {
				var type = Enum.GetUnderlyingType (this);
				if (type == value.GetType ())
					return value;
				var res = IsConvertibleToPrimitiveType (value, this);
				if (res != null)
					return res;
			} else if (IsPrimitive) {
				var res = IsConvertibleToPrimitiveType (value, this);
				if (res != null)
					return res;
			} else if (IsPointer) {
				var vtype = value.GetType ();
				if (vtype == typeof (IntPtr) || vtype == typeof (UIntPtr))
					return value;
				if (value is Pointer pointer) {
					Type pointerType = pointer.GetPointerType ();
					if (pointerType == this)
						return pointer.GetPointerValue ();
				}
			}

			failed = true;
			return null;
		}

		// Binder uses some incompatible conversion rules. For example
		// int value cannot be used with decimal parameter but in other
		// ways it's more flexible than normal convertor, for example
		// long value can be used with int based enum
		static object IsConvertibleToPrimitiveType (object value, Type targetType)
		{
			var type = value.GetType ();
			if (type.IsEnum) {
				type = Enum.GetUnderlyingType (type);
				if (type == targetType)
					return value;
			}

			var from = Type.GetTypeCode (type);
			var to = Type.GetTypeCode (targetType);

			switch (to) {
				case TypeCode.Char:
					switch (from) {
						case TypeCode.Byte:
							return (Char) (Byte) value;
						case TypeCode.UInt16:
							return value;
					}
					break;
				case TypeCode.Int16:
					switch (from) {
						case TypeCode.Byte:
							return (Int16) (Byte) value;
						case TypeCode.SByte:
							return (Int16) (SByte) value;
					}
					break;
				case TypeCode.UInt16:
					switch (from) {
						case TypeCode.Byte:
							return (UInt16) (Byte) value;
						case TypeCode.Char:
							return value;
					}
					break;
				case TypeCode.Int32:
					switch (from) {
						case TypeCode.Byte:
							return (Int32) (Byte) value;
						case TypeCode.SByte:
							return (Int32) (SByte) value;
						case TypeCode.Char:
							return (Int32) (Char) value;
						case TypeCode.Int16:
							return (Int32) (Int16) value;
						case TypeCode.UInt16:
							return (Int32) (UInt16) value;
					}
					break;
				case TypeCode.UInt32:
					switch (from) {
						case TypeCode.Byte:
							return (UInt32) (Byte) value;
						case TypeCode.Char:
							return (UInt32) (Char) value;
						case TypeCode.UInt16:
							return (UInt32) (UInt16) value;
					}
					break;
				case TypeCode.Int64:
					switch (from) {
						case TypeCode.Byte:
							return (Int64) (Byte) value;
						case TypeCode.SByte:
							return (Int64) (SByte) value;
						case TypeCode.Int16:
							return (Int64) (Int16) value;
						case TypeCode.Char:
							return (Int64) (Char) value;
						case TypeCode.UInt16:
							return (Int64) (UInt16) value;
						case TypeCode.Int32:
							return (Int64) (Int32) value;
						case TypeCode.UInt32:
							return (Int64) (UInt32) value;
					}
					break;
				case TypeCode.UInt64:
					switch (from) {
						case TypeCode.Byte:
							return (UInt64) (Byte) value;
						case TypeCode.Char:
							return (UInt64) (Char) value;
						case TypeCode.UInt16:
							return (UInt64) (UInt16) value;
						case TypeCode.UInt32:
							return (UInt64) (UInt32) value;
					}
					break;
				case TypeCode.Single:
					switch (from) {
						case TypeCode.Byte:
							return (Single) (Byte) value;
						case TypeCode.SByte:
							return (Single) (SByte) value;
						case TypeCode.Int16:
							return (Single) (Int16) value;
						case TypeCode.Char:
							return (Single) (Char) value;
						case TypeCode.UInt16:
							return (Single) (UInt16) value;
						case TypeCode.Int32:
							return (Single) (Int32) value;
						case TypeCode.UInt32:
							return (Single) (UInt32) value;
						case TypeCode.Int64:
							return (Single) (Int64) value;
						case TypeCode.UInt64:
							return (Single) (UInt64) value;
					}
					break;
				case TypeCode.Double:
					switch (from) {
						case TypeCode.Byte:
							return (Double) (Byte) value;
						case TypeCode.SByte:
							return (Double) (SByte) value;
						case TypeCode.Char:
							return (Double) (Char) value;
						case TypeCode.Int16:
							return (Double) (Int16) value;
						case TypeCode.UInt16:
							return (Double) (UInt16) value;
						case TypeCode.Int32:
							return (Double) (Int32) value;
						case TypeCode.UInt32:
							return (Double) (UInt32) value;
						case TypeCode.Int64:
							return (Double) (Int64) value;
						case TypeCode.UInt64:
							return (Double) (UInt64) value;
						case TypeCode.Single:
							return (Double) (Single) value;
					}
					break;
			}

			// Everything else is rejected
			return null;
		}

		string GetCachedName (TypeNameKind kind)
		{
			switch (kind) {
			case TypeNameKind.SerializationName:
				return ToString ();
			default:
				throw new NotImplementedException ();
			}
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern Type make_array_type (int rank);

		public override Type MakeArrayType ()
		{
			return make_array_type (0);
		}

		public override Type MakeArrayType (int rank)
		{
			if (rank < 1 || rank > 255)
				throw new IndexOutOfRangeException ();
			return make_array_type (rank);
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern Type make_byref_type ();

		public override Type MakeByRefType ()
		{
			if (IsByRef)
				throw new TypeLoadException ("Can not call MakeByRefType on a ByRef type");
			return make_byref_type ();
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		static extern Type MakePointerType (Type type);

		public override Type MakePointerType ()
		{
			if (IsByRef)
				throw new TypeLoadException ($"Could not load type '{GetType()}' from assembly '{AssemblyQualifiedName}");			
			return MakePointerType (this);
		}

		public override StructLayoutAttribute? StructLayoutAttribute {
			get {
				return GetStructLayoutAttribute ();
			}
		}

		public override bool ContainsGenericParameters {
			get {
				if (IsGenericParameter)
					return true;

				if (IsGenericType) {
					foreach (Type arg in GetGenericArguments ())
						if (arg.ContainsGenericParameters)
							return true;
				}

				if (HasElementType)
					return GetElementType ().ContainsGenericParameters;

				return false;
			}
		}

		public override Type[] GetGenericParameterConstraints ()
		{
			if (!IsGenericParameter)
				throw new InvalidOperationException(Environment.GetResourceString("Arg_NotGenericParameter"));

			var paramInfo = new Mono.RuntimeGenericParamInfoHandle (RuntimeTypeHandle.GetGenericParameterInfo (this));
			Type[] constraints = paramInfo.Constraints;

			return constraints ?? Array.Empty<Type> ();
		}

		internal static object CreateInstanceForAnotherGenericParameter (Type genericType, RuntimeType genericArgument)
		{
			var gt = (RuntimeType) MakeGenericType (genericType, new Type [] { genericArgument });
			var ctor = gt.GetDefaultConstructor ();
			return ctor.InternalInvoke (null, null, wrapExceptions: true);
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		static extern Type MakeGenericType (Type gt, Type [] types);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		internal extern IntPtr GetMethodsByName_native (IntPtr namePtr, BindingFlags bindingAttr, MemberListType listType);

		internal RuntimeMethodInfo[] GetMethodsByName (string name, BindingFlags bindingAttr, MemberListType listType, RuntimeType reflectedType)
		{
			var refh = new RuntimeTypeHandle (reflectedType);
			using (var namePtr = new Mono.SafeStringMarshal (name))
			using (var h = new Mono.SafeGPtrArrayHandle (GetMethodsByName_native (namePtr.Value, bindingAttr, listType))) {
				var n = h.Length;
				var a = new RuntimeMethodInfo [n];
				for (int i = 0; i < n; i++) {
					var mh = new RuntimeMethodHandle (h[i]);
					a[i] = (RuntimeMethodInfo) RuntimeMethodInfo.GetMethodFromHandleNoGenericCheck (mh, refh);
				}
				return a;
			}
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern IntPtr GetPropertiesByName_native (IntPtr name, BindingFlags bindingAttr, MemberListType listType);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern IntPtr GetConstructors_native (BindingFlags bindingAttr);

		RuntimeConstructorInfo[] GetConstructors_internal (BindingFlags bindingAttr, RuntimeType reflectedType)
		{
			var refh = new RuntimeTypeHandle (reflectedType);
			using (var h = new Mono.SafeGPtrArrayHandle (GetConstructors_native (bindingAttr))) {
				var n = h.Length;
				var a = new RuntimeConstructorInfo [n];
				for (int i = 0; i < n; i++) {
					var mh = new RuntimeMethodHandle (h[i]);
					a[i] = (RuntimeConstructorInfo) RuntimeMethodInfo.GetMethodFromHandleNoGenericCheck (mh, refh);
				}
				return a;
			}
		}

		RuntimePropertyInfo[] GetPropertiesByName (string name, BindingFlags bindingAttr, MemberListType listType, RuntimeType reflectedType)
		{
			var refh = new RuntimeTypeHandle (reflectedType);
			using (var namePtr = new Mono.SafeStringMarshal (name))
			using (var h = new Mono.SafeGPtrArrayHandle (GetPropertiesByName_native (namePtr.Value, bindingAttr, listType))) {
				var n = h.Length;
				var a = new RuntimePropertyInfo [n];
				for (int i = 0; i < n; i++) {
					var ph = new Mono.RuntimePropertyHandle (h[i]);
					a[i] = (RuntimePropertyInfo) RuntimePropertyInfo.GetPropertyFromHandle (ph, refh);
				}
				return a;
			}
		}

		public override InterfaceMapping GetInterfaceMap (Type ifaceType)
		{
			if (IsGenericParameter)
				throw new InvalidOperationException(Environment.GetResourceString("Arg_GenericParameter"));
		
			if ((object)ifaceType == null)
				throw new ArgumentNullException("ifaceType");

			RuntimeType ifaceRtType = ifaceType as RuntimeType;

			if (ifaceRtType == null)
				throw new ArgumentException(Environment.GetResourceString("Argument_MustBeRuntimeType"), "ifaceType");

			InterfaceMapping res;
			if (!ifaceType.IsInterface)
				throw new ArgumentException ("Argument must be an interface.", "ifaceType");
			if (IsInterface)
				throw new ArgumentException ("'this' type cannot be an interface itself");
			res.TargetType = this;
			res.InterfaceType = ifaceType;
			GetInterfaceMapData (this, ifaceType, out res.TargetMethods, out res.InterfaceMethods);
			if (res.TargetMethods == null)
				throw new ArgumentException ("Interface not found", "ifaceType");

			return res;
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		static extern void GetInterfaceMapData (Type t, Type iface, out MethodInfo[] targets, out MethodInfo[] methods);		

		public override Guid GUID {
			get {
				object[] att = GetCustomAttributes(typeof(System.Runtime.InteropServices.GuidAttribute), true);
				if (att.Length == 0)
					return Guid.Empty;
				return new Guid(((System.Runtime.InteropServices.GuidAttribute)att[0]).Value);
			}
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		internal extern void GetPacking (out int packing, out int size);

		internal static Type GetTypeFromCLSIDImpl(Guid clsid, String server, bool throwOnError)
		{
			throw new NotImplementedException ("Unmanaged activation removed");
		}

		protected override TypeCode GetTypeCodeImpl ()
		{
			return GetTypeCodeImplInternal (this);
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern static TypeCode GetTypeCodeImplInternal (Type type);		

		internal static Type GetTypeFromProgIDImpl(String progID, String server, bool throwOnError)
		{
			throw new NotImplementedException ("Unmanaged activation is not supported");
		}

		public override string ToString()
		{
			return getFullName (false, false);
		}

		bool IsGenericCOMObjectImpl ()
		{
			return false;
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern object CreateInstanceInternal (Type type);

		public extern override MethodBase? DeclaringMethod {
			[MethodImplAttribute (MethodImplOptions.InternalCall)]
			get;
		}		
		
		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		internal extern string getFullName(bool full_name, bool assembly_qualified);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern Type[] GetGenericArgumentsInternal (bool runtimeArray);

		GenericParameterAttributes GetGenericParameterAttributes () {
			return (new Mono.RuntimeGenericParamInfoHandle (RuntimeTypeHandle.GetGenericParameterInfo (this))).Attributes;
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern int GetGenericParameterPosition ();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern IntPtr GetEvents_native (IntPtr name,  MemberListType listType);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern IntPtr GetFields_native (IntPtr name, BindingFlags bindingAttr, MemberListType listType);

		RuntimeFieldInfo[] GetFields_internal (string name, BindingFlags bindingAttr, MemberListType listType, RuntimeType reflectedType)
		{
			var refh = new RuntimeTypeHandle (reflectedType);
			using (var namePtr = new Mono.SafeStringMarshal (name))
			using (var h = new Mono.SafeGPtrArrayHandle (GetFields_native (namePtr.Value, bindingAttr, listType))) {
				int n = h.Length;
				var a = new RuntimeFieldInfo[n];
				for (int i = 0; i < n; i++) {
					var fh = new RuntimeFieldHandle (h[i]);
					a[i] = (RuntimeFieldInfo) FieldInfo.GetFieldFromHandle (fh, refh);
				}
				return a;
			}
		}

		RuntimeEventInfo[] GetEvents_internal (string name, BindingFlags bindingAttr, MemberListType listType, RuntimeType reflectedType)
		{
			var refh = new RuntimeTypeHandle (reflectedType);
			using (var namePtr = new Mono.SafeStringMarshal (name))
			using (var h = new Mono.SafeGPtrArrayHandle (GetEvents_native (namePtr.Value, listType))) {
				int n = h.Length;
				var a = new RuntimeEventInfo[n];
				for (int i = 0; i < n; i++) {
					var eh = new Mono.RuntimeEventHandle (h[i]);
					a[i] = (RuntimeEventInfo) RuntimeEventInfo.GetEventFromHandle (eh, refh);
				}
				return a;
			}
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern override Type[] GetInterfaces();

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern IntPtr GetNestedTypes_native (IntPtr name, BindingFlags bindingAttr, MemberListType listType);

		RuntimeType[] GetNestedTypes_internal (string displayName, BindingFlags bindingAttr, MemberListType listType)
		{
			string internalName = null;
			if (displayName != null)
				internalName = displayName;
			using (var namePtr = new Mono.SafeStringMarshal (internalName))
			using (var h = new Mono.SafeGPtrArrayHandle (GetNestedTypes_native (namePtr.Value, bindingAttr, listType))) {
				int n = h.Length;
				var a = new RuntimeType [n];
				for (int i = 0; i < n; i++) {
					var th = new RuntimeTypeHandle (h[i]);
					a[i] = (RuntimeType) Type.GetTypeFromHandle (th);
				}
				return a;
			}
		}

		public override string? AssemblyQualifiedName {
			get {
				return getFullName (true, true);
			}
		}

		public extern override Type? DeclaringType {
			[MethodImplAttribute (MethodImplOptions.InternalCall)]
			get;
		}

		public extern override string Name {
			[MethodImplAttribute (MethodImplOptions.InternalCall)]
			get;
		}

		public extern override string? Namespace {
			[MethodImplAttribute (MethodImplOptions.InternalCall)]
			get;
		}

		public override bool IsSecurityTransparent {
			get { return false; }
		}

		public override bool IsSecurityCritical {
			get { return true; }
		}

		public override bool IsSecuritySafeCritical {
			get { return false; }
		}

		public override string? FullName {
			get {
				// https://bugzilla.xamarin.com/show_bug.cgi?id=57938
				if (IsGenericType && ContainsGenericParameters && !IsGenericTypeDefinition)
					return null;

				string fullName;
				// This doesn't need locking
				if (type_info == null)
					type_info = new MonoTypeInfo ();
				if ((fullName = type_info.full_name) == null)
					fullName = type_info.full_name = getFullName (true, false);

				return fullName;
			}
		}

		public sealed override bool HasSameMetadataDefinitionAs (MemberInfo other) => HasSameMetadataDefinitionAsCore<RuntimeType> (other);	

		public override bool IsSZArray {
			get {
				// TODO: intrinsic
				return IsArray && ReferenceEquals (this, GetElementType ().MakeArrayType ());
			}
		}

		internal override bool IsUserType {
			get {
				return false;
			}
		}

		public override bool IsSubclassOf(Type type)
		{
			if ((object)type == null)
				throw new ArgumentNullException("type");

			RuntimeType rtType = type as RuntimeType;
			if (rtType == null)
				return false;

			return RuntimeTypeHandle.IsSubclassOf (this, rtType);
		}

		public override bool IsByRefLike {
			get {
				return RuntimeTypeHandle.IsByRefLike (this);
			}
		}

		public override bool IsTypeDefinition => RuntimeTypeHandle.IsTypeDefinition (this);

        private const int DEFAULT_PACKING_SIZE = 8;

        internal StructLayoutAttribute GetStructLayoutAttribute ()
        {
            if (IsInterface || HasElementType || IsGenericParameter)
                return null;

            int pack = 0, size = 0;
            LayoutKind layoutKind = LayoutKind.Auto;
            switch (Attributes & TypeAttributes.LayoutMask)
            {
                case TypeAttributes.ExplicitLayout: layoutKind = LayoutKind.Explicit; break;
                case TypeAttributes.AutoLayout: layoutKind = LayoutKind.Auto; break;
                case TypeAttributes.SequentialLayout: layoutKind = LayoutKind.Sequential; break;
                default: break;
            }

            CharSet charSet = CharSet.None;
            switch (Attributes & TypeAttributes.StringFormatMask)
            {
                case TypeAttributes.AnsiClass: charSet = CharSet.Ansi; break;
                case TypeAttributes.AutoClass: charSet = CharSet.Auto; break;
                case TypeAttributes.UnicodeClass: charSet = CharSet.Unicode; break;
                default: break;
            }

            GetPacking (out pack, out size);

            // Metadata parameter checking should not have allowed 0 for packing size.
            // The runtime later converts a packing size of 0 to 8 so do the same here
            // because it's more useful from a user perspective. 
            if (pack == 0)
                pack = DEFAULT_PACKING_SIZE;

            return new StructLayoutAttribute (layoutKind) { Pack = pack, Size = size, CharSet = charSet };
        }
	}
}
