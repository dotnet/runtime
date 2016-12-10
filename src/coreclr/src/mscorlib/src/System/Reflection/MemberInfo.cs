// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

namespace System.Reflection
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Runtime;
    using System.Runtime.InteropServices;
    using System.Security.Permissions;

    [Serializable]
    [ClassInterface(ClassInterfaceType.None)]
    [ComDefaultInterface(typeof(_MemberInfo))]
    [System.Runtime.InteropServices.ComVisible(true)]
    public abstract class MemberInfo : ICustomAttributeProvider, _MemberInfo
    {
        #region Constructor
        protected MemberInfo() { }
        #endregion

        #region Internal Methods
        internal virtual bool CacheEquals(object o) { throw new NotImplementedException(); } 
        #endregion

        #region Public Abstract\Virtual Members
        public abstract MemberTypes MemberType { get; }

        public abstract String Name { get; }

        public abstract Type DeclaringType { get; }

        public abstract Type ReflectedType { get; }

        public virtual IEnumerable<CustomAttributeData> CustomAttributes
        {
            get
            {
                return GetCustomAttributesData();
            }
        }
        public abstract Object[] GetCustomAttributes(bool inherit);

        public abstract Object[] GetCustomAttributes(Type attributeType, bool inherit);

        public abstract bool IsDefined(Type attributeType, bool inherit);

        public virtual IList<CustomAttributeData> GetCustomAttributesData()
        {
            throw new NotImplementedException();
        }

        public virtual int MetadataToken { get { throw new InvalidOperationException(); } }

        public virtual Module Module
        { 
            get
            {
                if (this is Type)
                    return ((Type)this).Module;

                throw new NotImplementedException(); 
            } 
        }
        
        
        
        #endregion

        public static bool operator ==(MemberInfo left, MemberInfo right)
        {
            if (ReferenceEquals(left, right))
                return true;

            if ((object)left == null || (object)right == null)
                return false;

            Type type1, type2;
            MethodBase method1, method2;
            FieldInfo field1, field2;
            EventInfo event1, event2;
            PropertyInfo property1, property2;

            if ((type1 = left as Type) != null && (type2 = right as Type) != null)
                return type1 == type2;
            else if ((method1 = left as MethodBase) != null && (method2 = right as MethodBase) != null)
                return method1 == method2;
            else if ((field1 = left as FieldInfo) != null && (field2 = right as FieldInfo) != null)
                return field1 == field2;
            else if ((event1 = left as EventInfo) != null && (event2 = right as EventInfo) != null)
                return event1 == event2;
            else if ((property1 = left as PropertyInfo) != null && (property2 = right as PropertyInfo) != null)
                return property1 == property2;

            return false;
        }

        public static bool operator !=(MemberInfo left, MemberInfo right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
