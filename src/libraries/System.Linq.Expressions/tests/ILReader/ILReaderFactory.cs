// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Code adapted from https://blogs.msdn.microsoft.com/haibo_luo/2010/04/19/ilvisualizer-2010-solution

using System.Reflection.Emit;
using System.Reflection;

namespace System.Linq.Expressions.Tests
{
    public class ILReaderFactory
    {
        public static ILReader Create(object obj)
        {
            Type type = obj.GetType();

            if (type == s_dynamicMethodType || type == s_rtDynamicMethodType)
            {
                DynamicMethod dm;
                if (type == s_rtDynamicMethodType)
                {
                    //
                    // if the target is RTDynamicMethod, get the value of
                    // RTDynamicMethod.m_owner instead
                    //
                    dm = (DynamicMethod)s_fiOwner.GetValue(obj);
                }
                else
                {
                    dm = obj as DynamicMethod;
                }

                return new ILReader(new DynamicMethodILProvider(dm), new DynamicScopeTokenResolver(dm));
            }

            throw new NotSupportedException($"Reading IL from type '{type}' is currently not supported.");
        }

        private static readonly Type s_dynamicMethodType = Type.GetType("System.Reflection.Emit.DynamicMethod", throwOnError: true);
        private static readonly Type s_runtimeMethodInfoType = Type.GetType("System.Reflection.RuntimeMethodInfo", throwOnError: true);
        private static readonly Type s_runtimeConstructorInfoType = Type.GetType("System.Reflection.RuntimeConstructorInfo", throwOnError: true);

        private static readonly Type s_rtDynamicMethodType = Type.GetType("System.Reflection.Emit.DynamicMethod+RTDynamicMethod", throwOnError: true);
        private static readonly FieldInfo s_fiOwner = s_rtDynamicMethodType.GetFieldAssert("m_owner");
    }
}
