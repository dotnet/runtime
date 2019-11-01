// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Globalization;
using System.Diagnostics;

namespace System.Runtime.Serialization
{
    internal sealed class SerializationFieldInfo : FieldInfo
    {
        private readonly FieldInfo m_field;
        private readonly string m_serializationName;

        internal SerializationFieldInfo(FieldInfo field, string namePrefix)
        {
            Debug.Assert(field != null);
            Debug.Assert(namePrefix != null);

            m_field = field;
            m_serializationName = namePrefix + "+" + m_field.Name;
        }

        internal FieldInfo FieldInfo => m_field;

        public override string Name => m_serializationName;

        // "Name" returns our custom serialization name.
        // All other overrides simply delegate to m_field

        public override Module Module { get { return m_field.Module; } }

        public override int MetadataToken { get { return m_field.MetadataToken; } }

        public override Type DeclaringType => m_field.DeclaringType;

        public override Type ReflectedType => m_field.ReflectedType;

        public override object[] GetCustomAttributes(bool inherit) => m_field.GetCustomAttributes(inherit);

        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => m_field.GetCustomAttributes(attributeType, inherit);

        public override bool IsDefined(Type attributeType, bool inherit) => m_field.IsDefined(attributeType, inherit);

        public override Type FieldType => m_field.FieldType;

        public override object GetValue(object obj) => m_field.GetValue(obj);

        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture) => m_field.SetValue(obj, value, invokeAttr, binder, culture);

        public override RuntimeFieldHandle FieldHandle => m_field.FieldHandle;

        public override FieldAttributes Attributes => m_field.Attributes;
    }
}
