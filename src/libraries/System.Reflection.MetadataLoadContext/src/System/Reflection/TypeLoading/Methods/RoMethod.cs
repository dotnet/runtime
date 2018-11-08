// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// Base class for all MethodInfo objects created by a MetadataLoadContext.
    /// </summary>
    internal abstract partial class RoMethod : LeveledMethodInfo, IRoMethodBase
    {
        private readonly Type _reflectedType;

        protected RoMethod(Type reflectedType)
        {
            Debug.Assert(reflectedType != null);
            _reflectedType = reflectedType;
        }

        public abstract override bool Equals(object obj);
        public abstract override int GetHashCode();

        public sealed override Type DeclaringType => GetRoDeclaringType();
        internal abstract RoType GetRoDeclaringType();

        public sealed override Type ReflectedType => _reflectedType;

        public sealed override string Name => _lazyName ?? (_lazyName = ComputeName());
        protected abstract string ComputeName();
        private volatile string _lazyName;

        public sealed override Module Module => GetRoModule();
        internal abstract RoModule GetRoModule();

        public abstract override int MetadataToken { get; }
        public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other) => this.HasSameMetadataDefinitionAsCore(other);

        public abstract override IEnumerable<CustomAttributeData> CustomAttributes { get; }
        public sealed override IList<CustomAttributeData> GetCustomAttributesData() => CustomAttributes.ToReadOnlyCollection();

        public sealed override object[] GetCustomAttributes(bool inherit) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);
        public sealed override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);
        public sealed override bool IsDefined(Type attributeType, bool inherit) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);

        public abstract override bool IsConstructedGenericMethod { get; }
        public abstract override bool IsGenericMethodDefinition { get; }
        public sealed override bool IsGenericMethod => IsGenericMethodDefinition || IsConstructedGenericMethod;

        public sealed override MethodAttributes Attributes => (_lazyMethodAttributes == MethodAttributesSentinel) ? (_lazyMethodAttributes = ComputeAttributes()) : _lazyMethodAttributes;
        protected abstract MethodAttributes ComputeAttributes();
        private const MethodAttributes MethodAttributesSentinel = (MethodAttributes)(-1);
        private volatile MethodAttributes _lazyMethodAttributes = MethodAttributesSentinel;

        public sealed override CallingConventions CallingConvention => (_lazyCallingConventions == CallingConventionsSentinel) ? (_lazyCallingConventions = ComputeCallingConvention()) : _lazyCallingConventions;
        protected abstract CallingConventions ComputeCallingConvention();
        private const CallingConventions CallingConventionsSentinel = (CallingConventions)(-1);
        private volatile CallingConventions _lazyCallingConventions = CallingConventionsSentinel;

        public sealed override MethodImplAttributes MethodImplementationFlags => (_lazyMethodImplAttributes == MethodImplAttributesSentinel) ? (_lazyMethodImplAttributes = ComputeMethodImplementationFlags()) : _lazyMethodImplAttributes;
        protected abstract MethodImplAttributes ComputeMethodImplementationFlags();
        private const MethodImplAttributes MethodImplAttributesSentinel = (MethodImplAttributes)(-1);
        private volatile MethodImplAttributes _lazyMethodImplAttributes = MethodImplAttributesSentinel;

        public sealed override MethodImplAttributes GetMethodImplementationFlags() => MethodImplementationFlags;
        public abstract override MethodBody GetMethodBody();

        public sealed override bool ContainsGenericParameters
        {
            get
            {
                if (GetRoDeclaringType().ContainsGenericParameters)
                    return true;

                Type[] pis = GetGenericArgumentsOrParametersNoCopy();
                for (int i = 0; i < pis.Length; i++)
                {
                    if (pis[i].ContainsGenericParameters)
                        return true;
                }

                return false;
            }
        }

        public sealed override ParameterInfo[] GetParameters() => GetParametersNoCopy().CloneArray<ParameterInfo>();
        public sealed override ParameterInfo ReturnParameter => MethodSig.Return;
        internal RoParameter[] GetParametersNoCopy() => MethodSig.Parameters;

        private MethodSig<RoParameter> MethodSig => _lazyMethodSig ?? (_lazyMethodSig = ComputeMethodSig());
        protected abstract MethodSig<RoParameter> ComputeMethodSig();
        private volatile MethodSig<RoParameter> _lazyMethodSig;

        private MethodSig<RoType> CustomModifiers => _lazyCustomModifiers ?? (_lazyCustomModifiers = ComputeCustomModifiers());
        protected abstract MethodSig<RoType> ComputeCustomModifiers();
        private volatile MethodSig<RoType> _lazyCustomModifiers;

        public sealed override ICustomAttributeProvider ReturnTypeCustomAttributes => ReturnParameter;
        public sealed override Type ReturnType => ReturnParameter.ParameterType;

        public abstract override MethodInfo GetGenericMethodDefinition();

        public sealed override Type[] GetGenericArguments() => GetGenericArgumentsOrParametersNoCopy().CloneArray<Type>();
        internal RoType[] GetGenericArgumentsOrParametersNoCopy() => _lazyGenericArgumentsOrParameters ?? (_lazyGenericArgumentsOrParameters = ComputeGenericArgumentsOrParameters());
        protected abstract RoType[] ComputeGenericArgumentsOrParameters();
        private volatile RoType[] _lazyGenericArgumentsOrParameters;

        internal abstract RoType[] GetGenericTypeParametersNoCopy();
        internal abstract RoType[] GetGenericTypeArgumentsNoCopy();

        public abstract override MethodInfo MakeGenericMethod(params Type[] typeArguments);

        public sealed override string ToString() => Loader.GetDisposedString() ?? this.ToString(ComputeMethodSigStrings());
        protected abstract MethodSig<string> ComputeMethodSigStrings();

        public sealed override MethodInfo GetBaseDefinition() => throw new NotSupportedException(SR.NotSupported_GetBaseDefinition);

        // No trust environment to apply these to.
        public sealed override bool IsSecurityCritical => throw new InvalidOperationException(SR.InvalidOperation_IsSecurity);
        public sealed override bool IsSecuritySafeCritical => throw new InvalidOperationException(SR.InvalidOperation_IsSecurity);
        public sealed override bool IsSecurityTransparent => throw new InvalidOperationException(SR.InvalidOperation_IsSecurity);

        // Not valid in a ReflectionOnly context
        public sealed override object Invoke(object obj, BindingFlags invokeAttr, Binder binder, object[] parameters, CultureInfo culture) => throw new InvalidOperationException(SR.Arg_ReflectionOnlyInvoke);
        public sealed override Delegate CreateDelegate(Type delegateType) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);
        public sealed override Delegate CreateDelegate(Type delegateType, object target) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);
        public sealed override RuntimeMethodHandle MethodHandle => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);

        MethodBase IRoMethodBase.MethodBase => this;
        public MetadataLoadContext Loader => GetRoModule().Loader;
        public abstract TypeContext TypeContext { get; }
        Type[] IRoMethodBase.GetCustomModifiers(int position, bool isRequired) => CustomModifiers[position].ExtractCustomModifiers(isRequired);
        string IRoMethodBase.GetMethodSigString(int position) => ComputeMethodSigStrings()[position];
    }
}
