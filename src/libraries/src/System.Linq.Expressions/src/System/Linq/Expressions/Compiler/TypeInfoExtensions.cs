// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions.Compiler
{
    internal static class TypeInfoExtensions
    {
        public static Type MakeDelegateType(this DelegateHelpers.TypeInfo info, Type retType, params Expression[] args)
        {
            return info.MakeDelegateType(retType, (IList<Expression>)args);
        }

        public static Type MakeDelegateType(this DelegateHelpers.TypeInfo info, Type retType, IList<Expression> args)
        {
            // nope, go ahead and create it and spend the
            // cost of creating the array.
            Type[] paramTypes = new Type[args.Count + 2];
            paramTypes[0] = typeof(CallSite);
            paramTypes[paramTypes.Length - 1] = retType;
            for (int i = 0; i < args.Count; i++)
            {
                paramTypes[i + 1] = args[i].Type;
            }

            return info.DelegateType = DelegateHelpers.MakeNewDelegate(paramTypes);
        }
    }
}
