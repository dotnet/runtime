// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Code adapted from https://blogs.msdn.microsoft.com/haibo_luo/2010/04/19/ilvisualizer-2010-solution

using System;
using System.Reflection;

namespace System.Linq.Expressions.Tests
{
    public interface ITypeFactory
    {
        Type FromHandle(IntPtr handle);
        Type MakeArrayType(Type type);
        Type MakeArrayType(Type type, int rank);
        Type MakeByRefType(Type type);
        Type MakePointerType(Type type);
        Type MakeGenericType(Type definition, Type[] arguments);
    }

    internal class DefaultTypeFactory : ITypeFactory
    {
        public static readonly ITypeFactory Instance = new DefaultTypeFactory();

        protected DefaultTypeFactory() { }

#if GETTYPEFROMHANDLEUNSAFE
        private static readonly MethodInfo s_GetTypeFromHandleUnsafe = typeof(Type).GetMethodAssert("GetTypeFromHandleUnsafe");

        public virtual Type FromHandle(IntPtr handle) => (Type)s_GetTypeFromHandleUnsafe.Invoke(null, new object[] { handle });
        public Type MakeGenericType(Type definition, Type[] arguments) => definition.MakeGenericType(arguments);
#else
        public virtual Type FromHandle(IntPtr handle) => typeof(Unknown);
        public Type MakeGenericType(Type definition, Type[] arguments)
        {
            if (definition == UnknownTypeFactory.Unknown)
            {
                definition = UnknownTypeFactory.GetGenericUnknown(arguments.Length);

                if (definition == null)
                {
                    return UnknownTypeFactory.Unknown;
                }
            }

            return definition.MakeGenericType(arguments);
        }
#endif
        public Type MakeArrayType(Type type) => type.MakeArrayType();
        public Type MakeArrayType(Type type, int rank) => type.MakeArrayType(rank);
        public Type MakeByRefType(Type type) => type.MakeByRefType();
        public Type MakePointerType(Type type) => type.MakePointerType();
    }
}

#if !GETTYPEFROMHANDLEUNSAFE
static class UnknownTypeFactory
{
    public static readonly Type Unknown = typeof(Unknown);
    public static Type GetGenericUnknown(int arity) => Unknown.GetTypeInfo().Assembly.GetType(Unknown.FullName + "`" + arity, throwOnError: false);
}

// NB: Putting these in the global namespace in order to make their type name as short as possible when printed
class Unknown { }
class Unknown<T1> { }
class Unknown<T1, T2> { }
class Unknown<T1, T2, T3> { }
class Unknown<T1, T2, T3, T4> { }
class Unknown<T1, T2, T3, T4, T5> { }
class Unknown<T1, T2, T3, T4, T5, T6> { }
class Unknown<T1, T2, T3, T4, T5, T6, T7> { }
class Unknown<T1, T2, T3, T4, T5, T6, T7, T8> { }
class Unknown<T1, T2, T3, T4, T5, T6, T7, T8, T9> { }
class Unknown<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> { }
class Unknown<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> { }
class Unknown<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> { }
class Unknown<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> { }
class Unknown<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> { }
class Unknown<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> { }
class Unknown<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> { }
#endif