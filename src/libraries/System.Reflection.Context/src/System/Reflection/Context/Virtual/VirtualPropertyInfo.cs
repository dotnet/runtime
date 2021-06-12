// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Context.Custom;

namespace System.Reflection.Context.Virtual
{
    // Represents a func-based 'PropertyInfo'
    internal sealed partial class VirtualPropertyInfo : VirtualPropertyBase
    {
        private readonly PropertyGetter _getter;
        private readonly PropertySetter _setter;
        private readonly IEnumerable<Attribute> _attributes;

        public VirtualPropertyInfo(
            string name,
            Type propertyType,
            Func<object, object> getter,
            Action<object, object> setter,
            IEnumerable<Attribute> propertyAttributes,
            IEnumerable<Attribute> getterAttributes,
            IEnumerable<Attribute> setterAttributes,
            CustomReflectionContext context)
            : base(propertyType, name, context)
        {
            if (getter == null && setter == null)
                throw new ArgumentException(SR.ArgumentNull_GetterOrSetterMustBeSpecified);

            CustomType rcType = propertyType as CustomType;
            if (rcType == null || rcType.ReflectionContext != context)
                throw new ArgumentException(SR.Argument_PropertyTypeFromDifferentContext);

            if (getter != null)
                _getter = new PropertyGetter(this, getter, getterAttributes);

            if (setter != null)
                _setter = new PropertySetter(this, setter, setterAttributes);

            _attributes = propertyAttributes ?? CollectionServices.Empty<Attribute>();
        }

        public override MethodInfo GetGetMethod(bool nonPublic)
        {
            // Current we don't support adding nonpulbic getters
            Debug.Assert(_getter == null || _getter.IsPublic);
            return _getter;
        }

        public override MethodInfo GetSetMethod(bool nonPublic)
        {
            // Current we don't support adding nonpulbic setters
            Debug.Assert(_setter == null || _setter.IsPublic);
            return _setter;
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return CollectionServices.IEnumerableToArray(AttributeUtils.FilterCustomAttributes(_attributes, attributeType), attributeType);
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return CollectionServices.IEnumerableToArray(_attributes, typeof(Attribute));
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return CollectionServices.Empty<CustomAttributeData>();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return GetCustomAttributes(attributeType, inherit).Length > 0;
        }

        public override string ToString()
        {
            // Mimic the behavior of RuntimePRopertyInfo.ToString()
            return PropertyType.ToString() + " " + Name;
        }
    }
}
