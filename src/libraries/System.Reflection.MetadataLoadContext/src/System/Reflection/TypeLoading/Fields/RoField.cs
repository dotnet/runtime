// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// Base class for all FieldInfo objects created by a MetadataLoadContext.
    /// </summary>
    internal abstract partial class RoField : LeveledFieldInfo
    {
        private readonly RoInstantiationProviderType _declaringType;
        private readonly Type _reflectedType;

        protected RoField(RoInstantiationProviderType declaringType, Type reflectedType)
        {
            Debug.Assert(declaringType != null);
            Debug.Assert(reflectedType != null);

            _declaringType = declaringType;
            _reflectedType = reflectedType;
        }

        public abstract override bool Equals(object? obj);
        public abstract override int GetHashCode();
        public abstract override string ToString();

        public sealed override Type DeclaringType => GetRoDeclaringType();
        internal RoInstantiationProviderType GetRoDeclaringType() => _declaringType;

        public sealed override Type ReflectedType => _reflectedType;

        public sealed override string Name => _lazyName ??= ComputeName();
        protected abstract string ComputeName();
        private volatile string? _lazyName;

        public sealed override Module Module => GetRoModule();
        internal abstract RoModule GetRoModule();

        public abstract override int MetadataToken { get; }
        public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other) => this.HasSameMetadataDefinitionAsCore(other);

        public sealed override IList<CustomAttributeData> GetCustomAttributesData() => CustomAttributes.ToReadOnlyCollection();
        public sealed override IEnumerable<CustomAttributeData> CustomAttributes
        {
            get
            {
                foreach (CustomAttributeData cad in GetTrueCustomAttributes())
                    yield return cad;

                if (_declaringType.IsExplicitLayout)
                {
                    ConstructorInfo? ci = Loader.TryGetFieldOffsetCtor();
                    if (ci != null)
                    {
                        int offset = GetExplicitFieldOffset();
                        Type int32Type = Loader.GetCoreType(CoreType.Int32); // Since we got the constructor, we know the Int32 exists.
                        CustomAttributeTypedArgument[] cats = { new CustomAttributeTypedArgument(int32Type, offset) };
                        yield return new RoPseudoCustomAttributeData(ci, cats);
                    }
                }

                if (0 != (Attributes & FieldAttributes.HasFieldMarshal))
                {
                    CustomAttributeData? cad = CustomAttributeHelpers.TryComputeMarshalAsCustomAttributeData(ComputeMarshalAsAttribute, Loader);
                    if (cad != null)
                        yield return cad;
                }
            }
        }

        protected abstract IEnumerable<CustomAttributeData> GetTrueCustomAttributes();
        protected abstract int GetExplicitFieldOffset();
        protected abstract MarshalAsAttribute ComputeMarshalAsAttribute();

        public sealed override FieldAttributes Attributes => (_lazyFieldAttributes == FieldAttributesSentinel) ? (_lazyFieldAttributes = ComputeAttributes()) : _lazyFieldAttributes;
        protected abstract FieldAttributes ComputeAttributes();
        private const FieldAttributes FieldAttributesSentinel = (FieldAttributes)(-1);
        private volatile FieldAttributes _lazyFieldAttributes = FieldAttributesSentinel;

        public sealed override Type FieldType => _lazyFieldType ??= ComputeFieldType();
        protected abstract Type ComputeFieldType();
        private volatile Type? _lazyFieldType;

        public sealed override object? GetRawConstantValue() => IsLiteral ? ComputeRawConstantValue() : throw new InvalidOperationException();
        protected abstract object? ComputeRawConstantValue();

        public abstract override Type[] GetOptionalCustomModifiers();
        public abstract override Type[] GetRequiredCustomModifiers();

        // No trust environment to apply these to.
        public sealed override bool IsSecurityCritical => throw new InvalidOperationException(SR.InvalidOperation_IsSecurity);
        public sealed override bool IsSecuritySafeCritical => throw new InvalidOperationException(SR.InvalidOperation_IsSecurity);
        public sealed override bool IsSecurityTransparent => throw new InvalidOperationException(SR.InvalidOperation_IsSecurity);

        // Operations that are not allowed for Reflection-only.
        public sealed override object[] GetCustomAttributes(bool inherit) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);
        public sealed override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);
        public sealed override bool IsDefined(Type attributeType, bool inherit) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);
        public sealed override RuntimeFieldHandle FieldHandle => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);
        public sealed override object GetValue(object? obj) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);
        public sealed override object GetValueDirect(TypedReference obj) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);
        public sealed override void SetValue(object? obj, object? value, BindingFlags invokeAttr, Binder? binder, CultureInfo? culture) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);
        public sealed override void SetValueDirect(TypedReference obj, object value) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);

        private MetadataLoadContext Loader => GetRoModule().Loader;
        internal TypeContext TypeContext => _declaringType.Instantiation.ToTypeContext();
    }
}
