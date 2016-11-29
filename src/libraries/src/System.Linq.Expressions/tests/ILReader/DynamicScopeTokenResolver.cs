// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Code adapted from https://blogs.msdn.microsoft.com/haibo_luo/2010/04/19/ilvisualizer-2010-solution

using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;

namespace System.Linq.Expressions.Tests
{
    internal sealed class DynamicScopeTokenResolver : ITokenResolver
    {
        private static readonly PropertyInfo s_indexer;
        private static readonly FieldInfo s_scopeFi;

        private static readonly Type s_genMethodInfoType;
        private static readonly FieldInfo s_genmethFi1, s_genmethFi2;

        private static readonly Type s_varArgMethodType;
        private static readonly FieldInfo s_varargFi1, s_varargFi2;

        private static readonly Type s_genFieldInfoType;
        private static readonly FieldInfo s_genfieldFi1, s_genfieldFi2;

        static DynamicScopeTokenResolver()
        {
            Type dynamicScope = Type.GetType("System.Reflection.Emit.DynamicScope", throwOnError: true);
            Type dynamicILGenerator = Type.GetType("System.Reflection.Emit.DynamicILGenerator", throwOnError: true);

            s_indexer = dynamicScope.GetPropertyAssert("Item");
            s_scopeFi = dynamicILGenerator.GetFieldAssert("m_scope");

            s_varArgMethodType = Type.GetType("System.Reflection.Emit.VarArgMethod", throwOnError: true);
            s_varargFi1 = s_varArgMethodType.GetFieldAssert("m_method");
            s_varargFi2 = s_varArgMethodType.GetFieldAssert("m_signature");

            s_genMethodInfoType = Type.GetType("System.Reflection.Emit.GenericMethodInfo", throwOnError: true);
            s_genmethFi1 = s_genMethodInfoType.GetFieldAssert("m_methodHandle");
            s_genmethFi2 = s_genMethodInfoType.GetFieldAssert("m_context");

            s_genFieldInfoType = Type.GetType("System.Reflection.Emit.GenericFieldInfo", throwOnError: false);
            if (s_genFieldInfoType != null)
            {
                s_genfieldFi1 = s_genFieldInfoType.GetFieldAssert("m_fieldHandle");
                s_genfieldFi2 = s_genFieldInfoType.GetFieldAssert("m_context");
            }
            else
            {
                s_genfieldFi1 = s_genfieldFi2 = null;
            }
        }

        private readonly object m_scope = null;

        internal object this[int token] => s_indexer.GetValue(m_scope, new object[] { token });

        public DynamicScopeTokenResolver(DynamicMethod dm)
        {
            m_scope = s_scopeFi.GetValue(dm.GetILGenerator());
        }

        public string AsString(int token) => this[token] as string;

        public FieldInfo AsField(int token)
        {
            object item = this[token];

            if (item is RuntimeFieldHandle)
                return FieldInfo.GetFieldFromHandle((RuntimeFieldHandle)item);

            if (item.GetType() == s_genFieldInfoType)
            {
                return FieldInfo.GetFieldFromHandle(
                        (RuntimeFieldHandle)s_genfieldFi1.GetValue(item),
                        (RuntimeTypeHandle)s_genfieldFi2.GetValue(item));
            }

            Debug.Fail(string.Format("unexpected type: {0}", item.GetType()));
            return null;
        }

        public Type AsType(int token) => Type.GetTypeFromHandle((RuntimeTypeHandle)this[token]);

        public MethodBase AsMethod(int token)
        {
            object item = this[token];

            if (item is DynamicMethod)
                return item as DynamicMethod;

            if (item is RuntimeMethodHandle)
                return MethodBase.GetMethodFromHandle((RuntimeMethodHandle)item);

            if (item.GetType() == s_genMethodInfoType)
                return MethodBase.GetMethodFromHandle(
                    (RuntimeMethodHandle)s_genmethFi1.GetValue(item),
                    (RuntimeTypeHandle)s_genmethFi2.GetValue(item));

            if (item.GetType() == s_varArgMethodType)
                return (MethodInfo)s_varargFi1.GetValue(item);

            Debug.Fail(string.Format("unexpected type: {0}", item.GetType()));
            return null;
        }

        public MemberInfo AsMember(int token)
        {
            if ((token & 0x02000000) == 0x02000000)
                return AsType(token).GetTypeInfo();
            if ((token & 0x06000000) == 0x06000000)
                return AsMethod(token);
            if ((token & 0x04000000) == 0x04000000)
                return AsField(token);

            Debug.Fail(string.Format("unexpected token type: {0:x8}", token));
            return null;
        }

        public byte[] AsSignature(int token) => this[token] as byte[];
    }
}
