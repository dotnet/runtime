// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// Base class for all ConstructorInfo objects created by a MetadataLoadContext.
    /// </summary>
    internal abstract partial class RoConstructor : LeveledConstructorInfo, IRoMethodBase
    {
        protected RoConstructor() { }

        public abstract override bool Equals(object? obj);
        public abstract override int GetHashCode();

        public sealed override Type DeclaringType => GetRoDeclaringType();
        internal abstract RoType GetRoDeclaringType();

        public sealed override Type ReflectedType => DeclaringType;

        public sealed override string Name => _lazyName ??= ComputeName();
        protected abstract string ComputeName();
        private volatile string? _lazyName;

        public sealed override Module Module => GetRoModule();
        internal abstract RoModule GetRoModule();

        public abstract override int MetadataToken { get; }
        public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other) => this.HasSameMetadataDefinitionAsCore(other);

        public abstract override IEnumerable<CustomAttributeData> CustomAttributes { get; }
        public sealed override IList<CustomAttributeData> GetCustomAttributesData() => CustomAttributes.ToReadOnlyCollection();

        public sealed override object[] GetCustomAttributes(bool inherit) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);
        public sealed override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);
        public sealed override bool IsDefined(Type attributeType, bool inherit) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);

        public sealed override bool IsConstructedGenericMethod => false;
        public sealed override bool IsGenericMethodDefinition => false;
        public sealed override bool IsGenericMethod => false;

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
        public abstract override MethodBody? GetMethodBody();

        public sealed override bool ContainsGenericParameters => GetRoDeclaringType().ContainsGenericParameters;

        public sealed override ParameterInfo[] GetParameters() => GetParametersNoCopy().CloneArray<ParameterInfo>();
        internal RoParameter[] GetParametersNoCopy() => MethodSig.Parameters;

        private MethodSig<RoParameter> MethodSig => _lazyMethodSig ??= ComputeMethodSig();
        protected abstract MethodSig<RoParameter> ComputeMethodSig();
        private volatile MethodSig<RoParameter>? _lazyMethodSig;

        private MethodSig<RoType> CustomModifiers => _lazyCustomModifiers ??= ComputeCustomModifiers();
        protected abstract MethodSig<RoType> ComputeCustomModifiers();
        private volatile MethodSig<RoType>? _lazyCustomModifiers;

        public sealed override string ToString() => Loader.GetDisposedString() ?? this.ToString(ComputeMethodSigStrings());
        protected abstract MethodSig<string> ComputeMethodSigStrings();

        // No trust environment to apply these to.
        public sealed override bool IsSecurityCritical => throw new InvalidOperationException(SR.InvalidOperation_IsSecurity);
        public sealed override bool IsSecuritySafeCritical => throw new InvalidOperationException(SR.InvalidOperation_IsSecurity);
        public sealed override bool IsSecurityTransparent => throw new InvalidOperationException(SR.InvalidOperation_IsSecurity);

        // Not valid in a ReflectionOnly context
        public sealed override object Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture) => throw new InvalidOperationException(SR.Arg_ReflectionOnlyInvoke);
        public sealed override object Invoke(BindingFlags invokeAttr, Binder? binder, object?[]? parameters, CultureInfo? culture) => throw new InvalidOperationException(SR.Arg_ReflectionOnlyInvoke);
        public sealed override RuntimeMethodHandle MethodHandle => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);

        MethodBase IRoMethodBase.MethodBase => this;
        public MetadataLoadContext Loader => GetRoModule().Loader;
        public abstract TypeContext TypeContext { get; }
        Type[] IRoMethodBase.GetCustomModifiers(int position, bool isRequired) => CustomModifiers[position].ExtractCustomModifiers(isRequired);
        string IRoMethodBase.GetMethodSigString(int position) => ComputeMethodSigStrings()[position];
    }
}
