// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace System
{
	[CLSCompliantAttribute (false)]
	public ref struct TypedReference
	{
		#region sync with object-internals.h
		RuntimeTypeHandle type;
		IntPtr Value;
		IntPtr Type;
		#endregion

		[CLSCompliant (false)]
		public static TypedReference MakeTypedReference (object target, FieldInfo[] flds)
		{
			if (target == null)
				throw new ArgumentNullException (nameof (target));
			if (flds == null)
				throw new ArgumentNullException (nameof (flds));
			if (flds.Length == 0)
				throw new ArgumentException (SR.Arg_ArrayZeroError, nameof (flds));

			var fields = new IntPtr [flds.Length];
			var targetType = (RuntimeType)target.GetType ();
			for (int i = 0; i < flds.Length; i++) {
				var field = flds [i] as RuntimeFieldInfo;
				if (field == null)
					throw new ArgumentException (SR.Argument_MustBeRuntimeFieldInfo);
				if (field.IsStatic)
					throw new ArgumentException (SR.Argument_TypedReferenceInvalidField);

				if (targetType != field.GetDeclaringTypeInternal () && !targetType.IsSubclassOf (field.GetDeclaringTypeInternal ()))
					throw new MissingMemberException (SR.MissingMemberTypeRef);

				var fieldType = (RuntimeType)field.FieldType;
				if (fieldType.IsPrimitive)
					throw new ArgumentException (SR.Arg_TypeRefPrimitve);
				if (i < (flds.Length - 1) && !fieldType.IsValueType)
					throw new MissingMemberException (SR.MissingMemberNestErr);

				fields[i] = field.FieldHandle.Value;
				targetType = fieldType;
			}

			var result = new TypedReference ();

			unsafe {
				InternalMakeTypedReference (&result, target, fields, targetType);
			}
			return result;
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		unsafe static extern void InternalMakeTypedReference (void* result, Object target, IntPtr[] flds, RuntimeType lastFieldType);

		public override int GetHashCode ()
		{
			if (Type == IntPtr.Zero)
				return 0;
			else
				return __reftype (this).GetHashCode ();
		}

		public override bool Equals (object? o)
		{
			throw new NotSupportedException (SR.NotSupported_NYI);
		}

		[CLSCompliant (false)]
		public unsafe static object ToObject (TypedReference value)
		{
			return InternalToObject (&value);
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		unsafe extern static object InternalToObject (void * value);

		internal bool IsNull {
			get {
				return Value == IntPtr.Zero && Type == IntPtr.Zero;
			}
		}

		[CLSCompliant (false)]
		public static Type GetTargetType (TypedReference value)
		{
			return __reftype (value);
		}

		[CLSCompliant (false)]
		public static RuntimeTypeHandle TargetTypeToken (TypedReference value)
		{
			return __reftype (value).TypeHandle;
		}

		[CLSCompliant (false)]
		public unsafe static void SetTypedReference (TypedReference target, Object value)
		{
			throw new NotSupportedException ();
		}
	}
}
