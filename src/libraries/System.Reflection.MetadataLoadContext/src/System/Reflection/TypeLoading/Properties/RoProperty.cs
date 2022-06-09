// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// Base class for all PropertyInfo objects created by a MetadataLoadContext.
    /// </summary>
    internal abstract partial class RoProperty : LeveledPropertyInfo
    {
        private readonly RoInstantiationProviderType _declaringType;
        private readonly Type _reflectedType;

        protected RoProperty(RoInstantiationProviderType declaringType, Type reflectedType)
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
        public abstract override IEnumerable<CustomAttributeData> CustomAttributes { get; }

        public sealed override PropertyAttributes Attributes => (_lazyPropertyAttributes == PropertyAttributesSentinel) ? (_lazyPropertyAttributes = ComputeAttributes()) : _lazyPropertyAttributes;
        protected abstract PropertyAttributes ComputeAttributes();
        private const PropertyAttributes PropertyAttributesSentinel = (PropertyAttributes)(-1);
        private volatile PropertyAttributes _lazyPropertyAttributes = PropertyAttributesSentinel;

        public sealed override Type PropertyType => _lazyPropertyType ??= ComputePropertyType();
        protected abstract Type ComputePropertyType();
        private volatile Type? _lazyPropertyType;

        public sealed override MethodInfo? GetGetMethod(bool nonPublic) => GetRoGetMethod()?.FilterAccessor(nonPublic);
        public sealed override MethodInfo? GetSetMethod(bool nonPublic) => GetRoSetMethod()?.FilterAccessor(nonPublic);

        private RoMethod? GetRoGetMethod() => object.ReferenceEquals(_lazyGetter, Sentinels.RoMethod) ? (_lazyGetter = ComputeGetterMethod()?.FilterInheritedAccessor()) : _lazyGetter;
        private RoMethod? GetRoSetMethod() => object.ReferenceEquals(_lazySetter, Sentinels.RoMethod) ? (_lazySetter = ComputeSetterMethod()?.FilterInheritedAccessor()) : _lazySetter;

        protected abstract RoMethod? ComputeGetterMethod();
        protected abstract RoMethod? ComputeSetterMethod();

        private volatile RoMethod? _lazyGetter = Sentinels.RoMethod;
        private volatile RoMethod? _lazySetter = Sentinels.RoMethod;

        public sealed override bool CanRead => GetMethod != null;
        public sealed override bool CanWrite => SetMethod != null;

        public sealed override MethodInfo[] GetAccessors(bool nonPublic)
        {
            MethodInfo? getter = GetGetMethod(nonPublic);
            MethodInfo? setter = GetSetMethod(nonPublic);

            int count = 0;
            if (getter != null)
                count++;
            if (setter != null)
                count++;

            MethodInfo[] accessors = new MethodInfo[count];
            int index = 0;
            if (getter != null)
                accessors[index++] = getter;
            if (setter != null)
                accessors[index++] = setter;

            return accessors;
        }

        public sealed override ParameterInfo[] GetIndexParameters() => (_lazyIndexedParameters ??= ComputeIndexParameters()).CloneArray<ParameterInfo>();
        private RoPropertyIndexParameter[] ComputeIndexParameters()
        {
            bool useGetter = CanRead;
            RoMethod? accessor = (useGetter ? GetRoGetMethod() : GetRoSetMethod());
            if (accessor == null)
                throw new BadImageFormatException(); // Property has neither a getter or setter.
            RoParameter[] methodParameters = accessor.GetParametersNoCopy();
            int count = methodParameters.Length;
            if (!useGetter)
                count--;  // If we're taking the parameters off the setter, subtract one for the "value" parameter.
            if (count == 0)
                return Array.Empty<RoPropertyIndexParameter>();

            RoPropertyIndexParameter[] indexParameters = new RoPropertyIndexParameter[count];
            for (int i = 0; i < count; i++)
            {
                indexParameters[i] = new RoPropertyIndexParameter(this, methodParameters[i]);
            }
            return indexParameters;
        }
        private volatile RoPropertyIndexParameter[]? _lazyIndexedParameters;

        public sealed override object? GetRawConstantValue()
        {
            if ((Attributes & PropertyAttributes.HasDefault) == 0)
                throw new InvalidOperationException(SR.Arg_EnumLitValueNotFound);

            return ComputeRawConstantValue();
        }

        protected abstract object? ComputeRawConstantValue();

        public abstract override Type[] GetOptionalCustomModifiers();
        public abstract override Type[] GetRequiredCustomModifiers();

        // Operations that are not allowed for Reflection-only.
        public sealed override object[] GetCustomAttributes(bool inherit) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);
        public sealed override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);
        public sealed override bool IsDefined(Type attributeType, bool inherit) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);
        public sealed override object GetConstantValue() => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);
        public sealed override object GetValue(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);
        public sealed override void SetValue(object? obj, object? value, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture) => throw new InvalidOperationException(SR.Arg_InvalidOperation_Reflection);

        internal TypeContext TypeContext => _declaringType.Instantiation.ToTypeContext();
    }
}
