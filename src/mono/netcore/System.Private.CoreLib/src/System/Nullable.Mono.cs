// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
	partial struct Nullable<T>
	{
		//
		// These are called by the JIT
		//

		//
		// JIT implementation of box valuetype System.Nullable`1<T>
		//
		static object? Box (T? o)
		{
			if (!o.hasValue)
				return null;

			return o.value;
		}
		
		static T? Unbox (object o)
		{
			if (o == null)
				return null;
			return (T) o;
		}

		static T? UnboxExact (object o)
		{
			if (o == null)
				return null;
			if (o.GetType() != typeof (T))
				throw new InvalidCastException();

			return (T) o;
		}
	}
}
