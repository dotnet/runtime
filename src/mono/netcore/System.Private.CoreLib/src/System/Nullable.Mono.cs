// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    public partial struct Nullable<T>
    {
        //
        // These are called by the JIT
        //

        //
        // JIT implementation of box valuetype System.Nullable`1<T>
        //
        private static object? Box(T? o)
        {
            if (!o.hasValue)
                return null;

            return o.value;
        }

        private static T? Unbox(object o)
        {
            if (o == null)
                return null;
            return (T)o;
        }

        private static T? UnboxExact(object o)
        {
            if (o == null)
                return null;
            if (o.GetType() != typeof(T))
                throw new InvalidCastException();

            return (T)o;
        }
    }
}
