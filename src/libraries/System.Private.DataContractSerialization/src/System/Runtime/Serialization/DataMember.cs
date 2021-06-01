// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Xml;
using System.Security;
using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.Serialization
{
    internal sealed class DataMember
    {
        private readonly CriticalHelper _helper;

        internal DataMember(MemberInfo memberInfo)
        {
            _helper = new CriticalHelper(memberInfo);
        }

        internal MemberInfo MemberInfo
        {
            get
            { return _helper.MemberInfo; }
        }

        public string Name
        {
            get
            { return _helper.Name; }

            set
            { _helper.Name = value; }
        }

        public int Order
        {
            get
            { return _helper.Order; }

            set
            { _helper.Order = value; }
        }

        public bool IsRequired
        {
            get
            { return _helper.IsRequired; }

            set
            { _helper.IsRequired = value; }
        }

        public bool EmitDefaultValue
        {
            get
            { return _helper.EmitDefaultValue; }

            set
            { _helper.EmitDefaultValue = value; }
        }

        public bool IsNullable
        {
            get
            { return _helper.IsNullable; }

            set
            { _helper.IsNullable = value; }
        }

        public bool IsGetOnlyCollection
        {
            get
            { return _helper.IsGetOnlyCollection; }

            set
            { _helper.IsGetOnlyCollection = value; }
        }

        internal Type MemberType
        {
            get
            { return _helper.MemberType; }
        }

        internal DataContract MemberTypeContract
        {
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            { return _helper.MemberTypeContract; }
        }

        internal PrimitiveDataContract? MemberPrimitiveContract
        {
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get
            {
                return _helper.MemberPrimitiveContract;
            }
        }

        public bool HasConflictingNameAndType
        {
            get
            { return _helper.HasConflictingNameAndType; }

            set
            { _helper.HasConflictingNameAndType = value; }
        }

        internal DataMember? ConflictingMember
        {
            get
            { return _helper.ConflictingMember; }

            set
            { _helper.ConflictingMember = value; }
        }

        private FastInvokerBuilder.Getter? _getter;
        internal FastInvokerBuilder.Getter Getter
        {
            get
            {
                if (_getter == null)
                {
                    _getter = FastInvokerBuilder.CreateGetter(MemberInfo);
                }

                return _getter;
            }
        }

        private FastInvokerBuilder.Setter? _setter;
        internal FastInvokerBuilder.Setter Setter
        {
            get
            {
                if (_setter == null)
                {
                    _setter = FastInvokerBuilder.CreateSetter(MemberInfo);
                }

                return _setter;
            }
        }

        private sealed class CriticalHelper
        {
            private DataContract? _memberTypeContract;
            private string _name = null!; // Name is always initialized right after construction
            private int _order;
            private bool _isRequired;
            private bool _emitDefaultValue;
            private bool _isNullable;
            private bool _isGetOnlyCollection;
            private readonly MemberInfo _memberInfo;
            private bool _hasConflictingNameAndType;
            private DataMember? _conflictingMember;

            internal CriticalHelper(MemberInfo memberInfo)
            {
                _emitDefaultValue = Globals.DefaultEmitDefaultValue;
                _memberInfo = memberInfo;
                _memberPrimitiveContract = PrimitiveDataContract.NullContract;
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

            private Type? _memberType;

            internal Type MemberType
            {
                get
                {
                    if (_memberType == null)
                    {
                        FieldInfo? field = MemberInfo as FieldInfo;
                        if (field != null)
                            _memberType = field.FieldType;
                        else
                            _memberType = ((PropertyInfo)MemberInfo).PropertyType;
                    }

                    return _memberType;
                }
            }

            internal DataContract MemberTypeContract
            {
                [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
                get
                {
                    if (_memberTypeContract == null)
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

            internal DataMember? ConflictingMember
            {
                get { return _conflictingMember; }
                set { _conflictingMember = value; }
            }

            private PrimitiveDataContract? _memberPrimitiveContract;

            internal PrimitiveDataContract? MemberPrimitiveContract
            {
                [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
                get
                {
                    if (_memberPrimitiveContract == PrimitiveDataContract.NullContract)
                    {
                        _memberPrimitiveContract = PrimitiveDataContract.GetPrimitiveDataContract(MemberType);
                    }

                    return _memberPrimitiveContract;
                }
            }
        }

        /// <SecurityNote>
        /// Review - checks member visibility to calculate if access to it requires MemberAccessPermission for serialization.
        ///          since this information is used to determine whether to give the generated code access
        ///          permissions to private members, any changes to the logic should be reviewed.
        /// </SecurityNote>
        internal bool RequiresMemberAccessForGet()
        {
            MemberInfo memberInfo = MemberInfo;
            FieldInfo? field = memberInfo as FieldInfo;
            if (field != null)
            {
                return DataContract.FieldRequiresMemberAccess(field);
            }
            else
            {
                PropertyInfo property = (PropertyInfo)memberInfo;
                MethodInfo? getMethod = property.GetMethod;
                if (getMethod != null)
                {
                    return DataContract.MethodRequiresMemberAccess(getMethod) || !DataContract.IsTypeVisible(property.PropertyType);
                }
            }
            return false;
        }

        /// <SecurityNote>
        /// Review - checks member visibility to calculate if access to it requires MemberAccessPermission for deserialization.
        ///          since this information is used to determine whether to give the generated code access
        ///          permissions to private members, any changes to the logic should be reviewed.
        /// </SecurityNote>
        internal bool RequiresMemberAccessForSet()
        {
            MemberInfo memberInfo = MemberInfo;
            FieldInfo? field = memberInfo as FieldInfo;
            if (field != null)
            {
                return DataContract.FieldRequiresMemberAccess(field);
            }
            else
            {
                PropertyInfo property = (PropertyInfo)memberInfo;
                MethodInfo? setMethod = property.SetMethod;
                if (setMethod != null)
                {
                    return DataContract.MethodRequiresMemberAccess(setMethod) || !DataContract.IsTypeVisible(property.PropertyType);
                }
            }
            return false;
        }
    }
}
