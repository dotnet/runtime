// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System
{
	[Serializable]
	public struct RuntimeMethodHandle : ISerializable
	{
		readonly IntPtr value;

		internal RuntimeMethodHandle (IntPtr v)
		{
			value = v;
		}

		RuntimeMethodHandle (SerializationInfo info, StreamingContext context)
		{
			throw new PlatformNotSupportedException ();
		}

		public IntPtr Value => value;

		public void GetObjectData (SerializationInfo info, StreamingContext context)
		{
			throw new PlatformNotSupportedException ();
		}

		[MethodImpl (MethodImplOptions.InternalCall)]
		static extern IntPtr GetFunctionPointer (IntPtr m);

		public IntPtr GetFunctionPointer ()
		{
			return GetFunctionPointer (value);
		}

		public override bool Equals (object? obj)
		{
			if (obj == null || GetType () != obj.GetType ())
				return false;

			return value == ((RuntimeMethodHandle)obj).Value;
		}

		public bool Equals (RuntimeMethodHandle handle)
		{
			return value == handle.Value;
		}

		public override int GetHashCode ()
		{
			return value.GetHashCode ();
		}

		public static bool operator == (RuntimeMethodHandle left, RuntimeMethodHandle right)
		{
			return left.Equals (right);
		}

		public static bool operator != (RuntimeMethodHandle left, RuntimeMethodHandle right)
		{
			return !left.Equals (right);
		}

		internal static string ConstructInstantiation (RuntimeMethodInfo method, TypeNameFormatFlags format)
		{
			var sb = new StringBuilder ();
			var gen_params = method.GetGenericArguments ();
			sb.Append ("[");
			for (int j = 0; j < gen_params.Length; j++) {
				if (j > 0)
					sb.Append (",");
				sb.Append (gen_params [j].Name);
			}
			sb.Append ("]");
			return sb.ToString ();
		}

		internal bool IsNullHandle ()
		{
			return value == IntPtr.Zero;
		}
	}
}
