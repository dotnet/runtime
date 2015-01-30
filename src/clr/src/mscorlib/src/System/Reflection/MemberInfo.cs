// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
#pragma warning disable 618
    [PermissionSetAttribute(SecurityAction.InheritanceDemand, Name = "FullTrust")]
#pragma warning restore 618
    [System.Runtime.InteropServices.ComVisible(true)]
#if CONTRACTS_FULL
    [ContractClass(typeof(MemberInfoContracts))]
#endif
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

#if !FEATURE_CORECLR
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
#endif // !FEATURE_CORECLR

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

#if !FEATURE_CORECLR
        // this method is required so Object.GetType is not made final virtual by the compiler
        Type _MemberInfo.GetType()
        { 
            return base.GetType();
        }

        void _MemberInfo.GetTypeInfoCount(out uint pcTInfo)
        {
            throw new NotImplementedException();
        }

        void _MemberInfo.GetTypeInfo(uint iTInfo, uint lcid, IntPtr ppTInfo)
        {
            throw new NotImplementedException();
        }

        void _MemberInfo.GetIDsOfNames([In] ref Guid riid, IntPtr rgszNames, uint cNames, uint lcid, IntPtr rgDispId)
        {
            throw new NotImplementedException();
        }

        void _MemberInfo.Invoke(uint dispIdMember, [In] ref Guid riid, uint lcid, short wFlags, IntPtr pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr)
        {
            throw new NotImplementedException();
        }
#endif
    }

#if CONTRACTS_FULL
    [ContractClassFor(typeof(MemberInfo))]
    internal abstract class MemberInfoContracts : MemberInfo
    {
        public override String Name {
            get {
                Contract.Ensures(Contract.Result<String>() != null);
                return default(String);
            }
        }
    }
#endif // CONTRACTS_FULL

}
