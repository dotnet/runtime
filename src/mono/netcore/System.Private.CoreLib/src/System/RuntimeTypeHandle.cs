//
// Authors:
//   Miguel de Icaza (miguel@ximian.com)
//   Andreas Nahr (ClassDevelopment@A-SoftTech.com)
//
// (C) Ximian, Inc.  http://www.ximian.com
//
//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Runtime.Serialization;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Threading;

namespace System
{
	[Serializable]
	public struct RuntimeTypeHandle : ISerializable
	{
		readonly IntPtr value;

		internal RuntimeTypeHandle (IntPtr val)
		{
			value = val;
		}

		internal RuntimeTypeHandle (RuntimeType type)
			: this (type._impl.value)
		{
		}

		RuntimeTypeHandle (SerializationInfo info, StreamingContext context)
		{
			throw new PlatformNotSupportedException ();
		}

		public IntPtr Value {
			get {
				return value;
			}
		}

		public void GetObjectData (SerializationInfo info, StreamingContext context)
		{
			throw new PlatformNotSupportedException ();
		}

		public override bool Equals (object? obj)
		{
			if (obj == null || GetType () != obj.GetType ())
				return false;

			return value == ((RuntimeTypeHandle)obj).Value;
		}

		public bool Equals (RuntimeTypeHandle handle)
		{
			return value == handle.Value;
		}

		public override int GetHashCode ()
		{
			return value.GetHashCode ();
		}

		public static bool operator == (RuntimeTypeHandle left, Object right)
		{
			return (right != null) && (right is RuntimeTypeHandle) && left.Equals ((RuntimeTypeHandle)right);
		}

		public static bool operator != (RuntimeTypeHandle left, Object right)
		{
			return (right == null) || !(right is RuntimeTypeHandle) || !left.Equals ((RuntimeTypeHandle)right);
		}

		public static bool operator == (Object left, RuntimeTypeHandle right)
		{
			return (left != null) && (left is RuntimeTypeHandle) && ((RuntimeTypeHandle)left).Equals (right);
		}

		public static bool operator != (Object left, RuntimeTypeHandle right)
		{
			return (left == null) || !(left is RuntimeTypeHandle) || !((RuntimeTypeHandle)left).Equals (right);
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern TypeAttributes GetAttributes (RuntimeType type);

		[CLSCompliant (false)]
		public ModuleHandle GetModuleHandle ()
		{
			// Although MS' runtime is crashing here, we prefer throwing an exception.
			// The check is needed because Type.GetTypeFromHandle returns null
			// for zero handles.
			if (value == IntPtr.Zero)
				throw new InvalidOperationException ("Object fields may not be properly initialized");

			return Type.GetTypeFromHandle (this).Module.ModuleHandle;
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern int GetMetadataToken (RuntimeType type);

		internal static int GetToken (RuntimeType type)
		{
			return GetMetadataToken (type);
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static Type GetGenericTypeDefinition_impl (RuntimeType type);

		internal static Type GetGenericTypeDefinition (RuntimeType type)
		{
			return GetGenericTypeDefinition_impl (type);
		}

		internal static bool IsPrimitive (RuntimeType type)
		{
			CorElementType corElemType = GetCorElementType (type);
			return (corElemType >= CorElementType.ELEMENT_TYPE_BOOLEAN && corElemType <= CorElementType.ELEMENT_TYPE_R8) ||
				corElemType == CorElementType.ELEMENT_TYPE_I ||
				corElemType == CorElementType.ELEMENT_TYPE_U;
		}

		internal static bool IsByRef (RuntimeType type)
		{
			CorElementType corElemType = GetCorElementType (type);
			return corElemType == CorElementType.ELEMENT_TYPE_BYREF;
		}

		internal static bool IsPointer (RuntimeType type)
		{
			CorElementType corElemType = GetCorElementType (type);
			return corElemType == CorElementType.ELEMENT_TYPE_PTR;
		}

		internal static bool IsArray (RuntimeType type)
		{
			CorElementType corElemType = GetCorElementType (type);
			return corElemType == CorElementType.ELEMENT_TYPE_ARRAY || corElemType == CorElementType.ELEMENT_TYPE_SZARRAY;
		}

		internal static bool IsSzArray (RuntimeType type)
		{
			CorElementType corElemType = GetCorElementType (type);
			return corElemType == CorElementType.ELEMENT_TYPE_SZARRAY;
		}

		internal static bool HasElementType (RuntimeType type)
		{
			CorElementType corElemType = GetCorElementType(type);

			return ((corElemType == CorElementType.ELEMENT_TYPE_ARRAY || corElemType == CorElementType.ELEMENT_TYPE_SZARRAY) // IsArray
				   || (corElemType == CorElementType.ELEMENT_TYPE_PTR)											// IsPointer
				   || (corElemType == CorElementType.ELEMENT_TYPE_BYREF));										// IsByRef
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern CorElementType GetCorElementType (RuntimeType type);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal extern static bool HasInstantiation (RuntimeType type);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal extern static bool IsComObject (RuntimeType type);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal extern static bool IsInstanceOfType (RuntimeType type, Object o);		

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal extern static bool HasReferences (RuntimeType type);

		internal static bool IsComObject (RuntimeType type, bool isGenericCOM)
		{
			return isGenericCOM ? false : IsComObject (type);
		}

		internal static bool IsContextful (RuntimeType type)
		{
			return false;
		}

		internal static bool IsEquivalentTo (RuntimeType rtType1, RuntimeType rtType2)
		{
			// refence check is done earlier and we don't recognize anything else
			return false;
		}		

		internal static bool IsInterface (RuntimeType type)
		{
			return (type.Attributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface;
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal extern static int GetArrayRank(RuntimeType type);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal extern static RuntimeAssembly GetAssembly (RuntimeType type);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal extern static RuntimeType GetElementType (RuntimeType type);

 		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal extern static RuntimeModule GetModule (RuntimeType type);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal extern static bool IsGenericVariable (RuntimeType type);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal extern static RuntimeType GetBaseType (RuntimeType type);

		internal static bool CanCastTo (RuntimeType type, RuntimeType target)
		{
			return type_is_assignable_from (target, type);
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern bool type_is_assignable_from (Type a, Type b);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal extern static bool IsGenericTypeDefinition (RuntimeType type);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal extern static IntPtr GetGenericParameterInfo (RuntimeType type);

		internal static bool IsSubclassOf (RuntimeType childType, RuntimeType baseType)
		{
			return is_subclass_of (childType._impl.Value, baseType._impl.Value);
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal extern static bool is_subclass_of (IntPtr childType, IntPtr baseType);

		[PreserveDependency (".ctor()", "System.Runtime.CompilerServices.IsByRefLikeAttribute")]
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal extern static bool IsByRefLike (RuntimeType type);

		internal static bool IsTypeDefinition (RuntimeType type)
		{
			// That's how it has been done on CoreFX but we have no GetCorElementType method implementation
			// see https://github.com/dotnet/coreclr/pull/11355

			// CorElementType corElemType = GetCorElementType (type);
			// if (!((corElemType >= CorElementType.Void && corElemType < CorElementType.Ptr) ||
			// 		corElemType == CorElementType.ValueType ||
			// 		corElemType == CorElementType.Class ||
			// 		corElemType == CorElementType.TypedByRef ||
			// 		corElemType == CorElementType.I ||
			// 		corElemType == CorElementType.U ||
			// 		corElemType == CorElementType.Object))
			// 	return false;
			// if (HasInstantiation (type) && !IsGenericTypeDefinition (type))
			// 	return false;
			// return true;

			// It's like a workaround mentioned in https://github.com/dotnet/corefx/issues/17345
			return !type.HasElementType && !type.IsConstructedGenericType && !type.IsGenericParameter;
		}		

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern RuntimeType internal_from_name (string name, ref StackCrawlMark stackMark, Assembly callerAssembly, bool throwOnError, bool ignoreCase, bool reflectionOnly);

		internal static RuntimeType GetTypeByName (string typeName, bool throwOnError, bool ignoreCase, bool reflectionOnly, ref StackCrawlMark stackMark,
												  bool loadTypeFromPartialName)
		{
			if (typeName == null)
				throw new ArgumentNullException ("typeName");

			if (typeName == String.Empty)
				if (throwOnError)
					throw new TypeLoadException ("A null or zero length string does not represent a valid Type.");
				else
					return null;

			if (reflectionOnly) {
				int idx = typeName.IndexOf (',');
				if (idx < 0 || idx == 0 || idx == typeName.Length - 1)
					throw new ArgumentException ("Assembly qualifed type name is required", "typeName");
				string an = typeName.Substring (idx + 1);
				Assembly a;
				try {
					a = Assembly.ReflectionOnlyLoad (an);
				} catch {
					if (throwOnError)
						throw;
					return null;
				}
				return (RuntimeType)a.GetType (typeName.Substring (0, idx), throwOnError, ignoreCase);
			}

			var t = internal_from_name (typeName, ref stackMark, null, throwOnError, ignoreCase, false);
			if (throwOnError && t == null)
				throw new TypeLoadException ("Error loading '" + typeName + "'");
			return t;
		}

		internal static IntPtr[] CopyRuntimeTypeHandles (RuntimeTypeHandle[] inHandles, out int length)
		{
			if (inHandles == null || inHandles.Length == 0) {
				length = 0;
				return null;
			}

			IntPtr[] outHandles = new IntPtr [inHandles.Length];
			for (int i = 0; i < inHandles.Length; i++)
				outHandles [i] = inHandles [i].Value;
			length = outHandles.Length;
			return outHandles;
		}
	}
}
