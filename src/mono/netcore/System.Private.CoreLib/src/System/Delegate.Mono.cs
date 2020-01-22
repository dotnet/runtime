//
// Authors:
//   Miguel de Icaza (miguel@ximian.com)
//   Daniel Stodden (stodden@in.tum.de)
//   Dietmar Maurer (dietmar@ximian.com)
//   Marek Safar (marek.safar@gmail.com)
//
// (C) Ximian, Inc.  http://www.ximian.com
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
// Copyright 2014 Xamarin, Inc (http://www.xamarin.com)
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

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System
{
	/* Contains the rarely used fields of Delegate */
	sealed class DelegateData
	{
		public Type? target_type;
		public string? method_name;
		public bool curried_first_arg;
	}

	[StructLayout (LayoutKind.Sequential)]
	partial class Delegate
	{
		#region Sync with object-internals.h
		IntPtr method_ptr;
		IntPtr invoke_impl;
		object? _target;
		IntPtr method;
		IntPtr delegate_trampoline;
		IntPtr extra_arg;
		IntPtr method_code;
		IntPtr interp_method;
		IntPtr interp_invoke_impl;
		MethodInfo? method_info;

		// Keep a ref of the MethodInfo passed to CreateDelegate.
		// Used to keep DynamicMethods alive.
		MethodInfo? original_method_info;

		DelegateData data;

		bool method_is_virtual;
		#endregion

		protected Delegate (object target, string method)
		{
			if (target is null)
				throw new ArgumentNullException (nameof (target));

			if (method is null)
				throw new ArgumentNullException (nameof (method));

			this._target = target;
			this.data = new DelegateData () {
				method_name = method
			};
		}

		protected Delegate (Type target, string method)
		{
			if (target is null)
				throw new ArgumentNullException (nameof (target));

			if (target.ContainsGenericParameters)
				throw new ArgumentException (SR.Arg_UnboundGenParam, nameof (target));

			if (method is null)
				throw new ArgumentNullException (nameof (method));

			if (!target.IsRuntimeImplemented ())
				throw new ArgumentException (SR.Argument_MustBeRuntimeType, nameof (target));

			this.data = new DelegateData () {
				method_name = method,
				target_type = target
			};
		}

		public object? Target => GetTarget ();

		internal virtual object? GetTarget () => _target;

		public static Delegate CreateDelegate (Type type, object? firstArgument, MethodInfo method, bool throwOnBindFailure)
		{
			return CreateDelegate (type, firstArgument, method, throwOnBindFailure, true)!;
		}

		public static Delegate? CreateDelegate (Type type, MethodInfo method, bool throwOnBindFailure)
		{
			return CreateDelegate (type, null, method, throwOnBindFailure, false);
		}

		static Delegate? CreateDelegate (Type type, object? firstArgument, MethodInfo method, bool throwOnBindFailure, bool allowClosed)
		{
			if (type is null)
				throw new ArgumentNullException (nameof (type));
			if (method is null)
				throw new ArgumentNullException (nameof (method));

			if (!(type is RuntimeType rtType))
				throw new ArgumentException (SR.Argument_MustBeRuntimeType, nameof (type));
			if (!(method is RuntimeMethodInfo || method is System.Reflection.Emit.DynamicMethod))
				throw new ArgumentException (SR.Argument_MustBeRuntimeMethodInfo, nameof (method));

			if (!rtType.IsDelegate ())
				throw new ArgumentException (SR.Arg_MustBeDelegate, nameof (type));

			if (!IsMatchingCandidate (type, firstArgument, method, allowClosed, out DelegateData? delegate_data)) {
				if (throwOnBindFailure)
					throw new ArgumentException (SR.Arg_DlgtTargMeth);

				return null;
			}

			Delegate? d = CreateDelegate_internal (type, firstArgument, method, throwOnBindFailure);
			if (d != null) {
				d.original_method_info = method;
				d.data = delegate_data!;
			}

			return d;
		}

		public static Delegate? CreateDelegate (Type type, object target, string method, bool ignoreCase, bool throwOnBindFailure)
		{
			if (type is null)
				throw new ArgumentNullException (nameof (type));
			if (target is null)
				throw new ArgumentNullException (nameof (target));
			if (method is null)
				throw new ArgumentNullException (nameof (method));

			if (!(type is RuntimeType rtType))
				throw new ArgumentException (SR.Argument_MustBeRuntimeType, nameof (type));
			if (!rtType.IsDelegate ())
				throw new ArgumentException (SR.Arg_MustBeDelegate, nameof (type));

			MethodInfo? info = GetCandidateMethod (type, target.GetType (), method, BindingFlags.Instance, ignoreCase);
			if (info is null) {
				if (throwOnBindFailure)
					throw new ArgumentException (SR.Arg_DlgtTargMeth);

				return null;
			}

			return CreateDelegate_internal (type, null, info, throwOnBindFailure);
		}

		public static Delegate? CreateDelegate (Type type, Type target, string method, bool ignoreCase, bool throwOnBindFailure)
		{
			if (type is null)
				throw new ArgumentNullException (nameof (type));
			if (target is null)
				throw new ArgumentNullException (nameof (target));
			if (target.ContainsGenericParameters)
				throw new ArgumentException (SR.Arg_UnboundGenParam, nameof (target));
			if (method is null)
				throw new ArgumentNullException (nameof (method));

			if (!(type is RuntimeType rtType))
				throw new ArgumentException (SR.Argument_MustBeRuntimeType, nameof (type));

			if (!target.IsRuntimeImplemented ())
				throw new ArgumentException (SR.Argument_MustBeRuntimeType, nameof (target));
			if (!rtType.IsDelegate ())
				throw new ArgumentException (SR.Arg_MustBeDelegate, nameof (type));

			MethodInfo? info = GetCandidateMethod (type, target, method, BindingFlags.Static, ignoreCase);
			if (info is null) {
				if (throwOnBindFailure)
					throw new ArgumentException (SR.Arg_DlgtTargMeth);

				return null;
			}

			return CreateDelegate_internal (type, null, info, throwOnBindFailure);
		}

		static MethodInfo? GetCandidateMethod (Type type, Type target, string method, BindingFlags bflags, bool ignoreCase)
		{
			MethodInfo? invoke = type.GetMethod ("Invoke");
			if (invoke is null)
				return null;

			ParameterInfo [] delargs = invoke.GetParametersInternal ();
			Type[] delargtypes = new Type [delargs.Length];

			for (int i = 0; i < delargs.Length; i++)
				delargtypes [i] = delargs [i].ParameterType;

			/*
			 * since we need to walk the inheritance chain anyway to
			 * find private methods, adjust the bindingflags to ignore
			 * inherited methods
			 */
			BindingFlags flags = BindingFlags.ExactBinding |
				BindingFlags.Public | BindingFlags.NonPublic |
				BindingFlags.DeclaredOnly | bflags;

			if (ignoreCase)
				flags |= BindingFlags.IgnoreCase;

			for (Type? targetType = target; targetType != null; targetType = targetType.BaseType) {
				MethodInfo? mi = targetType.GetMethod (method, flags, null, delargtypes, Array.Empty<ParameterModifier>());

				if (mi != null && IsReturnTypeMatch (invoke.ReturnType!, mi.ReturnType!)) {
					return mi;
				}
			}

			return null;
		}

		static bool IsMatchingCandidate (Type type, object? target, MethodInfo method, bool allowClosed, out DelegateData? delegateData)
		{
			MethodInfo? invoke = type.GetMethod ("Invoke");

			if (invoke == null || !IsReturnTypeMatch (invoke.ReturnType!, method.ReturnType!)) {
				delegateData = null;
				return false;
			}

			ParameterInfo[] delargs = invoke.GetParametersInternal ();
			ParameterInfo[] args = method.GetParametersInternal ();

			bool argLengthMatch;

			if (target != null) {
				// delegate closed over target
				if (!method.IsStatic)
					// target is passed as this
					argLengthMatch = (args.Length == delargs.Length);
				else
					// target is passed as the first argument to the static method
					argLengthMatch = (args.Length == delargs.Length + 1);
			} else {
				if (!method.IsStatic) {
					//
					// Net 2.0 feature. The first argument of the delegate is passed
					// as the 'this' argument to the method.
					//
					argLengthMatch = (args.Length + 1 == delargs.Length);

					if (!argLengthMatch)
						// closed over a null reference
						argLengthMatch = (args.Length == delargs.Length);
				} else {
					argLengthMatch = (args.Length == delargs.Length);

					if (!argLengthMatch)
						// closed over a null reference
						argLengthMatch = args.Length == delargs.Length + 1;
				}
			}

			if (!argLengthMatch) {
				delegateData = null;
				return false;
			}

			bool argsMatch;
			delegateData = new DelegateData ();

			if (target != null) {
				if (!method.IsStatic) {
					argsMatch = IsArgumentTypeMatchWithThis (target.GetType (), method.DeclaringType!, true);
					for (int i = 0; i < args.Length; i++)
						argsMatch &= IsArgumentTypeMatch (delargs [i].ParameterType, args [i].ParameterType);
				} else {
					argsMatch = IsArgumentTypeMatch (target.GetType (), args [0].ParameterType);
					for (int i = 1; i < args.Length; i++)
						argsMatch &= IsArgumentTypeMatch (delargs [i - 1].ParameterType, args [i].ParameterType);

					delegateData.curried_first_arg = true;
				}
			} else {
				if (!method.IsStatic) {
					if (args.Length + 1 == delargs.Length) {
						// The first argument should match this
						argsMatch = IsArgumentTypeMatchWithThis (delargs [0].ParameterType, method.DeclaringType!, false);
						for (int i = 0; i < args.Length; i++)
							argsMatch &= IsArgumentTypeMatch (delargs [i + 1].ParameterType, args [i].ParameterType);
					} else {
						// closed over a null reference
						argsMatch = allowClosed;
						for (int i = 0; i < args.Length; i++)
							argsMatch &= IsArgumentTypeMatch (delargs [i].ParameterType, args [i].ParameterType);
					}
				} else {
					if (delargs.Length + 1 == args.Length) {
						// closed over a null reference
						argsMatch = !(args [0].ParameterType.IsValueType || args [0].ParameterType.IsByRef) && allowClosed;
						for (int i = 0; i < delargs.Length; i++)
							argsMatch &= IsArgumentTypeMatch (delargs [i].ParameterType, args [i + 1].ParameterType);

						delegateData.curried_first_arg = true;
					} else {
						argsMatch = true;
						for (int i = 0; i < args.Length; i++)
							argsMatch &= IsArgumentTypeMatch (delargs [i].ParameterType, args [i].ParameterType);
					}
				}
			}

			return argsMatch;
		}

		static bool IsReturnTypeMatch (Type delReturnType, Type returnType)
		{
			bool returnMatch = returnType == delReturnType;

			if (!returnMatch) {
				// Delegate covariance
				if (!returnType.IsValueType && delReturnType.IsAssignableFrom (returnType))
					returnMatch = true;
				else
				{
					bool isDelArgEnum = delReturnType.IsEnum;
					bool isArgEnum = returnType.IsEnum;
					if (isArgEnum && isDelArgEnum)
						returnMatch = Enum.GetUnderlyingType (delReturnType) == Enum.GetUnderlyingType (returnType);
					else if (isDelArgEnum && Enum.GetUnderlyingType (delReturnType) == returnType)
						returnMatch = true;
					else if (isArgEnum && Enum.GetUnderlyingType (returnType) == delReturnType)
						returnMatch = true;
				}
			}

			return returnMatch;
		}

		static bool IsArgumentTypeMatch (Type delArgType, Type argType)
		{
			bool match = delArgType == argType;

			// Delegate contravariance
			if (!match) {
				if (!argType.IsValueType && argType.IsAssignableFrom (delArgType))
					match = true;
			}
			// enum basetypes
			if (!match) {
				if (delArgType.IsEnum && Enum.GetUnderlyingType (delArgType) == argType)
					match = true;
				else if (argType.IsEnum && Enum.GetUnderlyingType (argType) == delArgType)
					match = true;
			}

			return match;
		}

		static bool IsArgumentTypeMatchWithThis (Type delArgType, Type argType, bool boxedThis)
		{
			bool match;
			if (argType.IsValueType)
				match = delArgType.IsByRef && delArgType.GetElementType () == argType ||
						(boxedThis && delArgType == argType);
			else
				match = delArgType == argType || argType.IsAssignableFrom (delArgType);

			return match;
		}

		protected virtual object? DynamicInvokeImpl (object?[]? args)
		{
			if (Method is null) {
#nullable disable
				// FIXME: This code cannot handle null argument values
				Type[] mtypes = new Type [args.Length];
				for (int i = 0; i < args.Length; ++i) {
					mtypes [i] = args [i].GetType ();
				}
				method_info = _target.GetType ().GetMethod (data.method_name, mtypes);
#nullable restore
			}

			var target = _target;

			if (data is null)
				data = CreateDelegateData ();

			// replace all Type.Missing with default values defined on parameters of the delegate if any
			MethodInfo? invoke = GetType ().GetMethod ("Invoke");
			if (invoke != null && args != null) {
				ParameterInfo[] delegateParameters = invoke.GetParameters ();
				for (int i = 0; i < args.Length; i++) {
					if (args [i] == Type.Missing) {
						ParameterInfo dlgParam = delegateParameters [i];
						if (dlgParam.HasDefaultValue) {
							args [i] = dlgParam.DefaultValue;
						}
					}
				}
			}

			if (Method.IsStatic) {
				//
				// The delegate is bound to _target
				//
				if (data.curried_first_arg) {
					if (args is null) {
						args = new object?[] { target };
					} else {
						Array.Resize (ref args, args.Length + 1);
						Array.Copy (args, 0, args, 1, args.Length - 1);
						args [0] = target;
					}

					target = null;
				}
			} else {
				if (_target is null && args?.Length > 0) {
					target = args [0];
					Array.Copy (args, 1, args, 0, args.Length - 1);
					Array.Resize (ref args, args.Length - 1);
				}
			}

			return Method.Invoke (target, args);
		}

		public override bool Equals (object? obj)
		{
			if (!(obj is Delegate d) || !InternalEqualTypes (this, obj))
				return false;

			// Do not compare method_ptr, since it can point to a trampoline
			if (d._target == _target && d.Method == Method) {
				if (d.data != null || data != null) {
					/* Uncommon case */
					if (d.data != null && data != null)
						return (d.data.target_type == data.target_type && d.data.method_name == data.method_name);
					else {
						if (d.data != null)
							return d.data.target_type is null;
						if (data != null)
							return data.target_type is null;
						return false;
					}
				}
				return true;
			}

			return false;
		}

		public override int GetHashCode ()
		{
			MethodInfo? m = Method;

			return (m != null ? m.GetHashCode () : GetType ().GetHashCode ()) ^ RuntimeHelpers.GetHashCode (_target);
		}

		protected virtual MethodInfo GetMethodImpl ()
		{
			if (method_info != null)
				return method_info;

			if (method != IntPtr.Zero) {
				if (!method_is_virtual)
					method_info = (MethodInfo) RuntimeMethodInfo.GetMethodFromHandleNoGenericCheck (new RuntimeMethodHandle (method));
				else
					method_info = GetVirtualMethod_internal ();
			}

			return method_info;
		}

		DelegateData CreateDelegateData ()
		{
			DelegateData delegate_data = new DelegateData ();
			if (method_info.IsStatic) {
				if (_target != null) {
					delegate_data.curried_first_arg = true;
				} else {
					MethodInfo? invoke = GetType ().GetMethod ("Invoke");
					if (invoke != null && invoke.GetParametersCount () + 1 == method_info.GetParametersCount ())
						delegate_data.curried_first_arg = true;
				}
			}

			return delegate_data;
		}

		static bool InternalEqualTypes (object source, object value)
		{
			return source.GetType () == value.GetType ();
		}

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		private protected extern static MulticastDelegate AllocDelegateLike_internal (Delegate d);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern Delegate? CreateDelegate_internal (Type type, object? target, MethodInfo info, bool throwOnBindFailure);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern MethodInfo GetVirtualMethod_internal ();
	}
}
