// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace System.Reflection.Context.Delegation
{
    internal class DelegatingFieldInfo : FieldInfo
    {
        public DelegatingFieldInfo(FieldInfo field)
        {
            Debug.Assert(null != field);

            UnderlyingField = field;
        }

        public override FieldAttributes Attributes
        {
            get { return UnderlyingField.Attributes; }
        }

        public override Type DeclaringType
        {
            get { return UnderlyingField.DeclaringType; }
        }

        public override RuntimeFieldHandle FieldHandle
        {
            get { return UnderlyingField.FieldHandle; }
        }

        public override Type FieldType
        {
            get { return UnderlyingField.FieldType; }
        }

        public override bool IsSecurityCritical
        {
            get { return UnderlyingField.IsSecurityCritical; }
        }

        public override bool IsSecuritySafeCritical
        {
            get { return UnderlyingField.IsSecuritySafeCritical; }
        }

        public override bool IsSecurityTransparent
        {
            get { return UnderlyingField.IsSecurityTransparent; }
        }

        public override int MetadataToken
        {
            get { return UnderlyingField.MetadataToken; }
        }

        public override Module Module
        {
            get { return UnderlyingField.Module; }
        }

        public override string Name
        {
            get { return UnderlyingField.Name; }
        }

        public override Type ReflectedType
        {
            get { return UnderlyingField.ReflectedType; }
        }

        public FieldInfo UnderlyingField { get; }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return UnderlyingField.GetCustomAttributes(attributeType, inherit);
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return UnderlyingField.GetCustomAttributes(inherit);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return UnderlyingField.GetCustomAttributesData();
        }

        public override Type[] GetOptionalCustomModifiers()
        {
            return UnderlyingField.GetOptionalCustomModifiers();
        }

        public override object GetRawConstantValue()
        {
            return UnderlyingField.GetRawConstantValue();
        }

        public override Type[] GetRequiredCustomModifiers()
        {
            return UnderlyingField.GetRequiredCustomModifiers();
        }

        public override object GetValue(object obj)
        {
            return UnderlyingField.GetValue(obj);
        }

        public override object GetValueDirect(TypedReference obj)
        {
            return UnderlyingField.GetValueDirect(obj);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return UnderlyingField.IsDefined(attributeType, inherit);
        }

        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
        {
            UnderlyingField.SetValue(obj, value, invokeAttr, binder, culture);
        }

        public override void SetValueDirect(TypedReference obj, object value)
        {
            UnderlyingField.SetValueDirect(obj, value);
        }

        public override string ToString()
        {
            return UnderlyingField.ToString();
        }        
    }
}
