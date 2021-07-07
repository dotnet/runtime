// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace System.Reflection.Context.Delegation
{
    internal class DelegatingPropertyInfo : PropertyInfo
    {
        public DelegatingPropertyInfo(PropertyInfo property)
        {
            Debug.Assert(null != property);

            UnderlyingProperty = property;
        }

        public override PropertyAttributes Attributes
        {
            get { return UnderlyingProperty.Attributes; }
        }

        public override bool CanRead
        {
            get { return UnderlyingProperty.CanRead; }
        }

        public override bool CanWrite
        {
            get { return UnderlyingProperty.CanWrite; }
        }

        public override Type? DeclaringType
        {
            get { return UnderlyingProperty.DeclaringType; }
        }

        public override int MetadataToken
        {
            get { return UnderlyingProperty.MetadataToken; }
        }

        public override Module Module
        {
            get { return UnderlyingProperty.Module; }
        }

        public override string Name
        {
            get { return UnderlyingProperty.Name; }
        }

        public override Type PropertyType
        {
            get { return UnderlyingProperty.PropertyType; }
        }

        public override Type? ReflectedType
        {
            get { return UnderlyingProperty.ReflectedType; }
        }

        public PropertyInfo UnderlyingProperty { get; }

        public override MethodInfo[] GetAccessors(bool nonPublic)
        {
            return UnderlyingProperty.GetAccessors(nonPublic);
        }

        public override MethodInfo? GetGetMethod(bool nonPublic)
        {
            return UnderlyingProperty.GetGetMethod(nonPublic);
        }

        public override ParameterInfo[] GetIndexParameters()
        {
            return UnderlyingProperty.GetIndexParameters();
        }

        public override MethodInfo? GetSetMethod(bool nonPublic)
        {
            return UnderlyingProperty.GetSetMethod(nonPublic);
        }

        public override object? GetValue(object? obj, object?[]? index)
        {
            return UnderlyingProperty.GetValue(obj, index);
        }

        public override object? GetValue(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture)
        {
            return UnderlyingProperty.GetValue(obj, invokeAttr, binder, index, culture);
        }

        public override void SetValue(object? obj, object? value, object?[]? index)
        {
            UnderlyingProperty.SetValue(obj, value, index);
        }

        public override void SetValue(object? obj, object? value, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture)
        {
            UnderlyingProperty.SetValue(obj, value, invokeAttr, binder, index, culture);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return UnderlyingProperty.GetCustomAttributes(attributeType, inherit);
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return UnderlyingProperty.GetCustomAttributes(inherit);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return UnderlyingProperty.GetCustomAttributesData();
        }

        public override object? GetConstantValue()
        {
            return UnderlyingProperty.GetConstantValue();
        }

        public override object? GetRawConstantValue()
        {
            return UnderlyingProperty.GetRawConstantValue();
        }

        public override Type[] GetOptionalCustomModifiers()
        {
            return UnderlyingProperty.GetOptionalCustomModifiers();
        }

        public override Type[] GetRequiredCustomModifiers()
        {
            return UnderlyingProperty.GetRequiredCustomModifiers();
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return UnderlyingProperty.IsDefined(attributeType, inherit);
        }

        public override string? ToString()
        {
            return UnderlyingProperty.ToString();
        }
    }
}
