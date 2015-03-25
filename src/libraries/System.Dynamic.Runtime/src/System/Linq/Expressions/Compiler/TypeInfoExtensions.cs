// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic;
using System.Dynamic.Utils;
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
#if FEATURE_CORECLR        
            paramTypes[0] = typeof(CallSite);
#else
            paramTypes[0] = typeof(object);
#endif 
            paramTypes[paramTypes.Length - 1] = retType;
            for (int i = 0; i < args.Count; i++)
            {
                paramTypes[i + 1] = args[i].Type;
            }

            return info.DelegateType = DelegateHelpers.MakeNewDelegate(paramTypes);
        }
    }
}
