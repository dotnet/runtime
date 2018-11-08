// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

namespace System.Reflection.Tests
{
    //
    // Do not put Type objects in XUnit Theory data lists. Especially Reflection-only Type objects. XUnit sees "Type", builds up a serialized
    // string for its output which involves everything including grabbing custom attributes using the invoking apis. 
    //
    public sealed class TypeWrapper
    {
        public TypeWrapper(Type type)
        {
            Type = type;
        }

        public Type Type { get; }

        public sealed override string ToString() => Type == null ? "<null>" : Type.ToString();
    }

    internal static class TypeWrapperExtensions
    {
        public static object[] Wrap(this object[] oa)
        {
            object[] newArray = new object[oa.Length];
            for (int i = 0; i < oa.Length; i++)
            {
                object o = oa[i];
                if (o is Type t)
                    o = new TypeWrapper(t);
                newArray[i] = o;
            }
            return newArray;
        }

        public static IEnumerable<object[]> Wrap(this IEnumerable<object[]> tds) => tds.Select(td => td.Wrap());
    }
}
