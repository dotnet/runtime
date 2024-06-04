// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
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
        internal readonly MethodInfo _method;
        private readonly Type[] _inst;
        #endregion

        #region Constructor
        internal MethodBuilderInstantiation(MethodInfo method, Type[] inst)
        {
            _method = method;
            _inst = inst;
        }
        #endregion

#if SYSTEM_PRIVATE_CORELIB
        internal override Type[] GetParameterTypes()
        {
            return _method.GetParameterTypes();
        }
#endif

        #region MemberBase
        public override MemberTypes MemberType => _method.MemberType;
        public override string Name => _method.Name;
        public override Type? DeclaringType => _method.DeclaringType;
        public override Type? ReflectedType => _method.ReflectedType;
        public override object[] GetCustomAttributes(bool inherit) { return _method.GetCustomAttributes(inherit); }
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) { return _method.GetCustomAttributes(attributeType, inherit); }
        public override bool IsDefined(Type attributeType, bool inherit) { return _method.IsDefined(attributeType, inherit); }
        public override Module Module => _method.Module;
        #endregion

        #region MethodBase Members
        public override ParameterInfo[] GetParameters() => _method.GetParameters();
        public override MethodImplAttributes GetMethodImplementationFlags() { return _method.GetMethodImplementationFlags(); }
        public override RuntimeMethodHandle MethodHandle => throw new NotSupportedException(SR.NotSupported_DynamicModule);
        public override MethodAttributes Attributes => _method.Attributes;
        public override object Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            throw new NotSupportedException();
        }
        public override CallingConventions CallingConvention => _method.CallingConvention;
        public override Type[] GetGenericArguments() { return _inst; }
        public override MethodInfo GetGenericMethodDefinition() { return _method; }
        public override bool IsGenericMethodDefinition => false;
        public override bool ContainsGenericParameters
        {
            get
            {
                for (int i = 0; i < _inst.Length; i++)
                {
                    if (_inst[i].ContainsGenericParameters)
                    {
                        return true;
                    }
                }

                if (DeclaringType != null && DeclaringType.ContainsGenericParameters)
                {
                    return true;
                }

                return false;
            }
        }

        [RequiresDynamicCode("The native code for this instantiation might not be available at runtime.")]
        [RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public override MethodInfo MakeGenericMethod(params Type[] arguments)
        {
            throw new InvalidOperationException(SR.Format(SR.Arg_NotGenericMethodDefinition, this));
        }

        public override bool IsGenericMethod => true;

        #endregion

        #region Public Abstract\Virtual Members
        public override Type ReturnType => _method.ReturnType;

        public override ParameterInfo ReturnParameter => throw new NotSupportedException();
        public override ICustomAttributeProvider ReturnTypeCustomAttributes => throw new NotSupportedException();
        public override MethodInfo GetBaseDefinition() { throw new NotSupportedException(); }
        #endregion
    }
}
