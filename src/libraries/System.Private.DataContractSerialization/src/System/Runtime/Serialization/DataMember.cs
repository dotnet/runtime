// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//-----------------------------------------------------------------------------
//-----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Xml;
using System.Security;


namespace System.Runtime.Serialization
{
    internal class DataMember
    {
        [SecurityCritical]
        /// <SecurityNote>
        /// Critical - holds instance of CriticalHelper which keeps state that is cached statically for serialization. 
        ///            Static fields are marked SecurityCritical or readonly to prevent
        ///            data from being modified or leaked to other components in appdomain.
        /// </SecurityNote>
        private CriticalHelper _helper;

        /// <SecurityNote>
        /// Critical - initializes SecurityCritical field 'helper'
        /// Safe - doesn't leak anything
        /// </SecurityNote>
        [SecuritySafeCritical]
        internal DataMember()
        {
            _helper = new CriticalHelper();
        }

        /// <SecurityNote>
        /// Critical - initializes SecurityCritical field 'helper'
        /// Safe - doesn't leak anything
        /// </SecurityNote>
        [SecuritySafeCritical]
        internal DataMember(MemberInfo memberInfo)
        {
            _helper = new CriticalHelper(memberInfo);
        }

        /// <SecurityNote>
        /// Critical - initializes SecurityCritical field 'helper'
        /// Safe - doesn't leak anything
        /// </SecurityNote>
        [SecuritySafeCritical]
        internal DataMember(DataContract memberTypeContract, string name, bool isNullable, bool isRequired, bool emitDefaultValue, int order)
        {
            _helper = new CriticalHelper(memberTypeContract, name, isNullable, isRequired, emitDefaultValue, order);
        }

        internal MemberInfo MemberInfo
        {
            [SecuritySafeCritical]
            get
            { return _helper.MemberInfo; }
        }

        internal string Name
        {
            [SecuritySafeCritical]
            get
            { return _helper.Name; }
            [SecurityCritical]
            set
            { _helper.Name = value; }
        }

        internal int Order
        {
            [SecuritySafeCritical]
            get
            { return _helper.Order; }
            [SecurityCritical]
            set
            { _helper.Order = value; }
        }

        internal bool IsRequired
        {
            [SecuritySafeCritical]
            get
            { return _helper.IsRequired; }
            [SecurityCritical]
            set
            { _helper.IsRequired = value; }
        }

        internal bool EmitDefaultValue
        {
            [SecuritySafeCritical]
            get
            { return _helper.EmitDefaultValue; }
            [SecurityCritical]
            set
            { _helper.EmitDefaultValue = value; }
        }

        internal bool IsNullable
        {
            [SecuritySafeCritical]
            get
            { return _helper.IsNullable; }
            [SecurityCritical]
            set
            { _helper.IsNullable = value; }
        }

        internal bool IsGetOnlyCollection
        {
            [SecuritySafeCritical]
            get
            { return _helper.IsGetOnlyCollection; }
            [SecurityCritical]
            set
            { _helper.IsGetOnlyCollection = value; }
        }

        internal Type MemberType
        {
            [SecuritySafeCritical]
            get
            { return _helper.MemberType; }
        }

        internal DataContract MemberTypeContract
        {
            [SecuritySafeCritical]
            get
            { return _helper.MemberTypeContract; }
        }

        internal bool HasConflictingNameAndType
        {
            [SecuritySafeCritical]
            get
            { return _helper.HasConflictingNameAndType; }
            [SecurityCritical]
            set
            { _helper.HasConflictingNameAndType = value; }
        }

        internal DataMember ConflictingMember
        {
            [SecuritySafeCritical]
            get
            { return _helper.ConflictingMember; }
            [SecurityCritical]
            set
            { _helper.ConflictingMember = value; }
        }
        [SecurityCritical]

        /// <SecurityNote>
        /// Critical
        /// </SecurityNote>
        private class CriticalHelper
        {
            private DataContract _memberTypeContract;
            private string _name;
            private int _order;
            private bool _isRequired;
            private bool _emitDefaultValue;
            private bool _isNullable;
            private bool _isGetOnlyCollection = false;
            private MemberInfo _memberInfo;
            private bool _hasConflictingNameAndType;
            private DataMember _conflictingMember;

            internal CriticalHelper()
            {
                _emitDefaultValue = Globals.DefaultEmitDefaultValue;
            }

            internal CriticalHelper(MemberInfo memberInfo)
            {
                _emitDefaultValue = Globals.DefaultEmitDefaultValue;
                _memberInfo = memberInfo;
            }

            internal CriticalHelper(DataContract memberTypeContract, string name, bool isNullable, bool isRequired, bool emitDefaultValue, int order)
            {
                this.MemberTypeContract = memberTypeContract;
                this.Name = name;
                this.IsNullable = isNullable;
                this.IsRequired = isRequired;
                this.EmitDefaultValue = emitDefaultValue;
                this.Order = order;
            }

            internal MemberInfo MemberInfo
            {
                get { return _memberInfo; }
            }

            internal string Name
            {
                get { return _name; }
                set { _name = value; }
            }

            internal int Order
            {
                get { return _order; }
                set { _order = value; }
            }

            internal bool IsRequired
            {
                get { return _isRequired; }
                set { _isRequired = value; }
            }

            internal bool EmitDefaultValue
            {
                get { return _emitDefaultValue; }
                set { _emitDefaultValue = value; }
            }

            internal bool IsNullable
            {
                get { return _isNullable; }
                set { _isNullable = value; }
            }

            internal bool IsGetOnlyCollection
            {
                get { return _isGetOnlyCollection; }
                set { _isGetOnlyCollection = value; }
            }

            internal Type MemberType
            {
                get
                {
                    FieldInfo field = MemberInfo as FieldInfo;
                    if (field != null)
                        return field.FieldType;
                    return ((PropertyInfo)MemberInfo).PropertyType;
                }
            }

            internal DataContract MemberTypeContract
            {
                get
                {
                    if (_memberTypeContract == null)
                    {
                        if (MemberInfo != null)
                        {
                            if (this.IsGetOnlyCollection)
                            {
                                _memberTypeContract = DataContract.GetGetOnlyCollectionDataContract(DataContract.GetId(MemberType.TypeHandle), MemberType.TypeHandle, MemberType, SerializationMode.SharedContract);
                            }
                            else
                            {
                                _memberTypeContract = DataContract.GetDataContract(MemberType);
                            }
                        }
                    }
                    return _memberTypeContract;
                }
                set
                {
                    _memberTypeContract = value;
                }
            }

            internal bool HasConflictingNameAndType
            {
                get { return _hasConflictingNameAndType; }
                set { _hasConflictingNameAndType = value; }
            }

            internal DataMember ConflictingMember
            {
                get { return _conflictingMember; }
                set { _conflictingMember = value; }
            }
        }

        /// <SecurityNote>
        /// Review - checks member visibility to calculate if access to it requires MemberAccessPermission for serialization.
        ///          since this information is used to determine whether to give the generated code access
        ///          permissions to private members, any changes to the logic should be reviewed.
        /// </SecurityNote>
        internal bool RequiresMemberAccessForGet(string[] serializationAssemblyPatterns)
        {
            MemberInfo memberInfo = MemberInfo;
            FieldInfo field = memberInfo as FieldInfo;
            if (field != null)
            {
                return DataContract.FieldRequiresMemberAccess(field, serializationAssemblyPatterns);
            }
            else
            {
                PropertyInfo property = (PropertyInfo)memberInfo;
                MethodInfo getMethod = property.GetMethod;
                if (getMethod != null)
                {
                    return DataContract.MethodRequiresMemberAccess(getMethod, serializationAssemblyPatterns) || !DataContract.IsTypeVisible(property.PropertyType, serializationAssemblyPatterns);
                }
            }
            return false;
        }

        /// <SecurityNote>
        /// Review - checks member visibility to calculate if access to it requires MemberAccessPermission for deserialization.
        ///          since this information is used to determine whether to give the generated code access
        ///          permissions to private members, any changes to the logic should be reviewed.
        /// </SecurityNote>
        internal bool RequiresMemberAccessForSet(string[] serializationAssemblyPatterns)
        {
            MemberInfo memberInfo = MemberInfo;
            FieldInfo field = memberInfo as FieldInfo;
            if (field != null)
            {
                return DataContract.FieldRequiresMemberAccess(field, serializationAssemblyPatterns);
            }
            else
            {
                PropertyInfo property = (PropertyInfo)memberInfo;
                MethodInfo setMethod = property.SetMethod;
                if (setMethod != null)
                {
                    return DataContract.MethodRequiresMemberAccess(setMethod, serializationAssemblyPatterns) || !DataContract.IsTypeVisible(property.PropertyType, serializationAssemblyPatterns);
                }
            }
            return false;
        }
    }
}
