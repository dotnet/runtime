// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System.Reflection.Emit
{
    internal sealed class MethodBuilderInstantiation : MethodInfo
    {
        #region Static Members
        internal static MethodInfo MakeGenericMethod(MethodInfo method, Type[] inst)
        {
            if (!method.IsGenericMethodDefinition)
                throw new InvalidOperationException();

            return new MethodBuilderInstantiation(method, inst);
        }

        #endregion

        #region Private Data Members
        internal MethodInfo m_method;
        private Type[] m_inst;
        #endregion

        #region Constructor
        internal MethodBuilderInstantiation(MethodInfo method, Type[] inst)
        {
            m_method = method;
            m_inst = inst;
        }
        #endregion

        internal override Type[] GetParameterTypes()
        {
            return m_method.GetParameterTypes();
        }

        #region MemberBase
        public override MemberTypes MemberType { get { return m_method.MemberType; } }
        public override string Name { get { return m_method.Name; } }
        public override Type? DeclaringType { get { return m_method.DeclaringType; } }
        public override Type? ReflectedType { get { return m_method.ReflectedType; } }
        public override object[] GetCustomAttributes(bool inherit) { return m_method.GetCustomAttributes(inherit); }
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) { return m_method.GetCustomAttributes(attributeType, inherit); }
        public override bool IsDefined(Type attributeType, bool inherit) { return m_method.IsDefined(attributeType, inherit); }
        public override Module Module { get { return m_method.Module; } }
        #endregion

        #region MethodBase Members
        public override ParameterInfo[] GetParameters() { throw new NotSupportedException(); }
        public override MethodImplAttributes GetMethodImplementationFlags() { return m_method.GetMethodImplementationFlags(); }
        public override RuntimeMethodHandle MethodHandle { get { throw new NotSupportedException(SR.NotSupported_DynamicModule); } }
        public override MethodAttributes Attributes { get { return m_method.Attributes; } }
        public override object Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            throw new NotSupportedException();
        }
        public override CallingConventions CallingConvention { get { return m_method.CallingConvention; } }
        public override Type[] GetGenericArguments() { return m_inst; }
        public override MethodInfo GetGenericMethodDefinition() { return m_method; }
        public override bool IsGenericMethodDefinition { get { return false; } }
        public override bool ContainsGenericParameters
        {
            get
            {
                for (int i = 0; i < m_inst.Length; i++)
                {
                    if (m_inst[i].ContainsGenericParameters)
                        return true;
                }

                if (DeclaringType != null && DeclaringType.ContainsGenericParameters)
                    return true;

                return false;
            }
        }

        public override MethodInfo MakeGenericMethod(params Type[] arguments)
        {
            throw new InvalidOperationException(SR.Format(SR.Arg_NotGenericMethodDefinition, this));
        }

        public override bool IsGenericMethod { get { return true; } }

        #endregion

        #region Public Abstract\Virtual Members
        public override Type? ReturnType
        {
            get
            {
                return m_method.ReturnType;
            }
        }

        public override ParameterInfo? ReturnParameter { get { throw new NotSupportedException(); } }
        public override ICustomAttributeProvider? ReturnTypeCustomAttributes { get { throw new NotSupportedException(); } }
        public override MethodInfo GetBaseDefinition() { throw new NotSupportedException(); }
        #endregion
    }
}





























