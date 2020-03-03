// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace System
{
	[Serializable]
	[System.Runtime.CompilerServices.TypeForwardedFrom ("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]	
	public abstract class ValueType
	{
		// This is also used by RuntimeHelpers
		internal static bool DefaultEquals (object o1, object o2)
		{
			RuntimeType o1_type = (RuntimeType) o1.GetType ();
			RuntimeType o2_type = (RuntimeType) o2.GetType ();

			if (o1_type != o2_type)
				return false;

			object[] fields;
			bool res = InternalEquals (o1, o2, out fields);
			if (fields == null)
				return res;

			for (int i = 0; i < fields.Length; i += 2) {
				object meVal = fields [i];
				object youVal = fields [i + 1];
				if (meVal == null) {
					if (youVal == null)
						continue;

					return false;
				}

				if (!meVal.Equals (youVal))
					return false;
			}

			return true;
		}

		public override bool Equals (object? obj)
		{
			if (obj == null)
				return false;

			return DefaultEquals (this, obj);
		}

		public override int GetHashCode ()
		{
			int result = InternalGetHashCode (this, out var fields);

			if (fields != null)
				for (int i = 0; i < fields.Length; ++i)
					if (fields [i] != null)
						result ^= fields [i].GetHashCode ();
				
			return result;
		}

		public override string ToString ()
		{
			return GetType ().ToString ();
		}


		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static int InternalGetHashCode (object o, out object[]? fields);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static bool InternalEquals (object o1, object o2, out object[] fields);
	}
}
