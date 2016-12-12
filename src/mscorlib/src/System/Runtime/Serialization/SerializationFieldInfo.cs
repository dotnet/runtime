// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: Provides a methods of representing imaginary fields
** which are unique to serialization.  In this case, what we're
** representing is the private members of parent classes.  We
** aggregate the RuntimeFieldInfo associated with this member 
** and return a managled form of the name.  The name that we
** return is .parentname.fieldname
**
**
============================================================*/

namespace System.Runtime.Serialization {

    using System;
    using System.Reflection;
    using System.Globalization;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.Threading;

    internal sealed class SerializationFieldInfo : FieldInfo {

        internal const String FakeNameSeparatorString = "+";

        private RuntimeFieldInfo m_field;
        private String           m_serializationName;

        public override Module Module { get { return m_field.Module; } }
        public override int MetadataToken { get { return m_field.MetadataToken; } } 

        internal SerializationFieldInfo(RuntimeFieldInfo field, String namePrefix) {
            Debug.Assert(field!=null,      "[SerializationFieldInfo.ctor]field!=null");
            Debug.Assert(namePrefix!=null, "[SerializationFieldInfo.ctor]namePrefix!=null");
            
            m_field = field;
            m_serializationName = String.Concat(namePrefix, FakeNameSeparatorString, m_field.Name);
        }

        //
        // MemberInfo methods
        //
        public override String Name {
            get {
                return m_serializationName;
            }
        }

        public override Type DeclaringType {
            get {
                return m_field.DeclaringType;
            }
        }

        public override Type ReflectedType {
            get {
                return m_field.ReflectedType;
            }
        }

        public override Object[] GetCustomAttributes(bool inherit) {
            return m_field.GetCustomAttributes(inherit);
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit) {
            return m_field.GetCustomAttributes(attributeType, inherit);
        }

        public override bool IsDefined(Type attributeType, bool inherit) {
            return m_field.IsDefined(attributeType, inherit);
        }

        //
        // FieldInfo methods
        //
        public override Type FieldType {
            get {
                return m_field.FieldType;
            }
        }
        
        public override Object GetValue(Object obj) {
            return m_field.GetValue(obj);
        }

        internal Object InternalGetValue(Object obj) {
            RtFieldInfo field = m_field as RtFieldInfo;
            if (field != null)
            {
                field.CheckConsistency(obj);
                return field.UnsafeGetValue(obj);
            }
            else
                return m_field.GetValue(obj);
        }

        public override void SetValue(Object obj, Object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture) {
            m_field.SetValue(obj, value, invokeAttr, binder, culture);
        }

        internal void InternalSetValue(Object obj, Object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture) {
            RtFieldInfo field = m_field as RtFieldInfo;
            if (field != null)
            {
                field.CheckConsistency(obj);
                field.UnsafeSetValue(obj, value, invokeAttr, binder, culture);
            }
            else
                m_field.SetValue(obj, value, invokeAttr, binder, culture);
        }

        internal RuntimeFieldInfo FieldInfo {
            get {
                return m_field;
            }
        }

        public override RuntimeFieldHandle FieldHandle {
            get {
                return m_field.FieldHandle;
            }
        }

        public override FieldAttributes Attributes {
            get {
                return m_field.Attributes;
            }
        }
    }
}
