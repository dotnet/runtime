// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Reflection.Emit
{
    internal sealed partial class MethodOnTypeBuilderInstantiation : MethodInfo
    {
        #region Internal Static Members
        internal static MethodInfo GetMethod(MethodInfo method, TypeBuilderInstantiation type)
        {
            return new MethodOnTypeBuilderInstantiation(method, type);
        }
        #endregion

        #region Private Data Members
        internal MethodInfo _method;
        private Type _type;
        #endregion

        #region Constructor
        internal MethodOnTypeBuilderInstantiation(MethodInfo method, Type type)
        {
            _method = method;
            _type = type;
        }
        #endregion

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "Reflection.Emit is not subject to trimming")]
        private static MethodInfo ExtractBaseMethod(MethodInfo info)
        {
            if (info is MethodBuilder)
                return info;
            if (info is MethodOnTypeBuilderInstantiation mbi)
                return mbi._method;

            if (info.IsGenericMethod)
                info = info.GetGenericMethodDefinition();

            Type t = info.DeclaringType!;
            if (!t.IsGenericType || t.IsGenericTypeDefinition)
                return info;

            return (MethodInfo)t.Module.ResolveMethod(info.MetadataToken)!;
        }

        #region MemberInfo Overrides
        public override MemberTypes MemberType => _method.MemberType;
        public override string Name => _method.Name;
        public override Type? DeclaringType => _type;
        public override Type? ReflectedType => _type;
        public override object[] GetCustomAttributes(bool inherit) { return _method.GetCustomAttributes(inherit); }
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) { return _method.GetCustomAttributes(attributeType, inherit); }
        public override bool IsDefined(Type attributeType, bool inherit) { return _method.IsDefined(attributeType, inherit); }
        public override Module Module => _method.Module;
        #endregion

        #region MethodBase Members
        public override ParameterInfo[] GetParameters() { return _method.GetParameters(); }
        public override MethodImplAttributes GetMethodImplementationFlags() { return _method.GetMethodImplementationFlags(); }
        public override RuntimeMethodHandle MethodHandle => _method.MethodHandle;
        public override MethodAttributes Attributes => _method.Attributes;
        public override object Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture)
        {
            throw new NotSupportedException();
        }
        public override CallingConventions CallingConvention => _method.CallingConvention;
#if !MONO
        public override MethodInfo GetGenericMethodDefinition() { return _method; }
        public override bool IsGenericMethodDefinition => _method.IsGenericMethodDefinition;
        public override Type[] GetGenericArguments()
        {
            return _method.GetGenericArguments();
        }
        public override bool ContainsGenericParameters
        {
            get
            {
                if (_method.ContainsGenericParameters)
                    return true;
                if (!_method.IsGenericMethodDefinition)
                    throw new NotSupportedException();

                return _method.ContainsGenericParameters;
            }
        }
        [RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public override MethodInfo MakeGenericMethod(params Type[] typeArgs)
        {
            if (!IsGenericMethodDefinition)
            {
                throw new InvalidOperationException(SR.Format(SR.Arg_NotGenericMethodDefinition, this));
            }

            return MethodBuilderInstantiation.MakeGenericMethod(this, typeArgs);
        }
#endif
        public override bool IsGenericMethod => _method.IsGenericMethod;
        #endregion

        #region Public Abstract\Virtual Members
        public override Type ReturnType => _method.ReturnType;
        public override ParameterInfo ReturnParameter => throw new NotSupportedException();
        public override ICustomAttributeProvider ReturnTypeCustomAttributes => throw new NotSupportedException();
        public override MethodInfo GetBaseDefinition() { throw new NotSupportedException(); }
        #endregion

        #region Internal overrides
        internal override Type[] GetParameterTypes()
        {
            return _method.GetParameterTypes();
        }
        #endregion
    }
}
