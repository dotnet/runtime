// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.CompilerServices
{
	partial class RuntimeHelpers
	{
		public static void InitializeArray (Array array, RuntimeFieldHandle fldHandle)
		{
			if (array == null || fldHandle.Value == IntPtr.Zero)
				throw new ArgumentNullException ();

			InitializeArray (array, fldHandle.Value);
		}

		public static extern int OffsetToStringData {
			[MethodImpl (MethodImplOptions.InternalCall)]
			get;
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern int InternalGetHashCode (object o);

		public static int GetHashCode (object o)
		{
			return InternalGetHashCode (o);
		}

		public static new bool Equals (object? o1, object? o2)
		{
			if (o1 == o2)
				return true;

			if (o1 == null || o2 == null)
				return false;

			if (o1 is ValueType)
				return ValueType.DefaultEquals (o1, o2);

			return false;
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public static extern object GetObjectValue (object obj);

		public static void RunClassConstructor (RuntimeTypeHandle type)
		{
			if (type.Value == IntPtr.Zero)
				throw new ArgumentException ("Handle is not initialized.", "type");

			RunClassConstructor (type.Value);
		}

		public static void EnsureSufficientExecutionStack ()
		{
			if (SufficientExecutionStack ())
				return;

			throw new InsufficientExecutionStackException ();
		}

		public static bool TryEnsureSufficientExecutionStack ()
		{
			return SufficientExecutionStack ();
		}

		public static void ExecuteCodeWithGuaranteedCleanup (TryCode code, CleanupCode backoutCode, Object userData)
		{
		}

		public static void PrepareDelegate (Delegate d)
		{
		}

		public static void PrepareMethod (RuntimeMethodHandle method)
		{
			if (method.IsNullHandle ())
				throw new ArgumentException (SR.Argument_InvalidHandle);
			unsafe {
				PrepareMethod (method.Value, null, 0);
			}
		}

		public static void PrepareMethod (RuntimeMethodHandle method, RuntimeTypeHandle[] instantiation)
		{
			if (method.IsNullHandle ())
				throw new ArgumentException (SR.Argument_InvalidHandle);
			unsafe {
				IntPtr[] instantiations = RuntimeTypeHandle.CopyRuntimeTypeHandles (instantiation, out int length);
				fixed (IntPtr* pinst = instantiations) {
					PrepareMethod (method.Value, pinst, length);
					GC.KeepAlive (instantiation);
				}
			}
		}

		public static void RunModuleConstructor (ModuleHandle module)
		{
			if (module == ModuleHandle.EmptyHandle)
				throw new ArgumentException ("Handle is not initialized.", "module");

			RunModuleConstructor (module.Value);
		}

		[Intrinsic]
		public static bool IsReferenceOrContainsReferences<T> ()
		{
			return !typeof (T).IsValueType || RuntimeTypeHandle.HasReferences ((typeof (T) as RuntimeType));
		}

		[Intrinsic]
		internal static bool IsBitwiseEquatable<T> ()
		{
			throw new NotImplementedException ();
		}

		[Intrinsic]
		internal static unsafe bool ObjectHasComponentSize (object obj)
		{
			throw new NotImplementedException ();
		}

		static object GetUninitializedObjectInternal (Type type)
		{
			return GetUninitializedObjectInternal (new RuntimeTypeHandle ((RuntimeType)type).Value);
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern unsafe void PrepareMethod (IntPtr method, IntPtr* instantiations, int ninst);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern object GetUninitializedObjectInternal (IntPtr type);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern void InitializeArray (Array array, IntPtr fldHandle);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		static extern void RunClassConstructor (IntPtr type);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern void RunModuleConstructor (IntPtr module);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern bool SufficientExecutionStack ();
	}
}
