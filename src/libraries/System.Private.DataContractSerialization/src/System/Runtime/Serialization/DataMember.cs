// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.Runtime.Serialization
{
    public sealed class DataMember
    {
        private readonly CriticalHelper _helper;

        internal DataMember(MemberInfo memberInfo)
        {
            _helper = new CriticalHelper(memberInfo);
        }

        internal DataMember(DataContract memberTypeContract, string name, bool isNullable, bool isRequired, bool emitDefaultValue, long order)
        {
            _helper = new CriticalHelper(memberTypeContract, name, isNullable, isRequired, emitDefaultValue, order);
        }

        internal MemberInfo MemberInfo => _helper.MemberInfo;

        public string Name
        {
            get => _helper.Name;
            internal set => _helper.Name = value;
        }

        public long Order
        {
            get => _helper.Order;
            internal set => _helper.Order = value;
        }

        public bool IsRequired
        {
            get => _helper.IsRequired;
            internal set => _helper.IsRequired = value;
        }

        public bool EmitDefaultValue
        {
            get => _helper.EmitDefaultValue;
            internal set => _helper.EmitDefaultValue = value;
        }

        public bool IsNullable
        {
            get => _helper.IsNullable;
            internal set => _helper.IsNullable = value;
        }

        internal bool IsGetOnlyCollection
        {
            get => _helper.IsGetOnlyCollection;
            set => _helper.IsGetOnlyCollection = value;
        }

        internal Type MemberType => _helper.MemberType;

        public DataContract MemberTypeContract
        {
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get => _helper.MemberTypeContract;
        }

        internal PrimitiveDataContract? MemberPrimitiveContract
        {
            [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
            get => _helper.MemberPrimitiveContract;
        }

        internal bool HasConflictingNameAndType
        {
            get => _helper.HasConflictingNameAndType;
            set => _helper.HasConflictingNameAndType = value;
        }

        internal DataMember? ConflictingMember
        {
            get => _helper.ConflictingMember;
            set => _helper.ConflictingMember = value;
        }

        private FastInvokerBuilder.Getter? _getter;
        internal FastInvokerBuilder.Getter Getter => _getter ??= FastInvokerBuilder.CreateGetter(MemberInfo);

        private FastInvokerBuilder.Setter? _setter;
        internal FastInvokerBuilder.Setter Setter => _setter ??= FastInvokerBuilder.CreateSetter(MemberInfo);

        private sealed class CriticalHelper
        {
            private DataContract? _memberTypeContract;
            private string _name = null!; // Name is always initialized right after construction
            private long _order;
            private bool _isRequired;
            private bool _emitDefaultValue;
            private bool _isNullable;
            private bool _isGetOnlyCollection;
            private readonly MemberInfo _memberInfo;
            private Type? _memberType;
            private bool _hasConflictingNameAndType;
            private DataMember? _conflictingMember;

            internal CriticalHelper(MemberInfo memberInfo)
            {
                _emitDefaultValue = Globals.DefaultEmitDefaultValue;
                _memberInfo = memberInfo;
                _memberPrimitiveContract = PrimitiveDataContract.NullContract;
            }

            internal CriticalHelper(DataContract memberTypeContract, string name, bool isNullable, bool isRequired, bool emitDefaultValue, long order)
            {
                _memberTypeContract = memberTypeContract;
                _name = name;
                _isNullable = isNullable;
                _isRequired = isRequired;
                _emitDefaultValue = emitDefaultValue;
                _order = order;
                _memberInfo = memberTypeContract.UnderlyingType;
            }

            internal MemberInfo MemberInfo => _memberInfo;

            internal string Name
            {
                get => _name;
                set => _name = value;
            }

            internal long Order
            {
                get => _order;
                set => _order = value;
            }

            internal bool IsRequired
            {
                get => _isRequired;
                set => _isRequired = value;
            }

            internal bool EmitDefaultValue
            {
                get => _emitDefaultValue;
                set => _emitDefaultValue = value;
            }

            internal bool IsNullable
            {
                get => _isNullable;
                set => _isNullable = value;
            }

            internal bool IsGetOnlyCollection
            {
                get => _isGetOnlyCollection;
                set => _isGetOnlyCollection = value;
            }

            internal Type MemberType
            {
                get
                {
                    if (_memberType == null)
                    {
                        if (MemberInfo is FieldInfo field)
                            _memberType = field.FieldType;
                        else if (MemberInfo is PropertyInfo prop)
                            _memberType = prop.PropertyType;
                        else
                            _memberType = (Type)MemberInfo!;
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
                        if (IsGetOnlyCollection)
                        {
                            _memberTypeContract = DataContract.GetGetOnlyCollectionDataContract(DataContract.GetId(MemberType.TypeHandle), MemberType.TypeHandle, MemberType);
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
                get => _hasConflictingNameAndType;
                set => _hasConflictingNameAndType = value;
            }

            internal DataMember? ConflictingMember
            {
                get => _conflictingMember;
                set => _conflictingMember = value;
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

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal DataMember BindGenericParameters(DataContract[] paramContracts, IDictionary<DataContract, DataContract> boundContracts)
        {
            DataContract memberTypeContract = MemberTypeContract.BindGenericParameters(paramContracts, boundContracts);
            DataMember boundDataMember = new DataMember(memberTypeContract,
                Name,
                !memberTypeContract.IsValueType,
                IsRequired,
                EmitDefaultValue,
                Order);
            return boundDataMember;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        internal bool Equals(object? other, HashSet<DataContractPairKey> checkedContracts)
        {
            if (this == other)
                return true;

            if (other is DataMember dataMember)
            {
                // Note: comparison does not use Order hint since it influences element order but does not specify exact order
                bool thisIsNullable = (MemberTypeContract == null) ? false : !MemberTypeContract.IsValueType;
                bool dataMemberIsNullable = (dataMember.MemberTypeContract == null) ? false : !dataMember.MemberTypeContract.IsValueType;
                return (Name == dataMember.Name
                        && (IsNullable || thisIsNullable) == (dataMember.IsNullable || dataMemberIsNullable)
                        && IsRequired == dataMember.IsRequired
                        && EmitDefaultValue == dataMember.EmitDefaultValue
                        && MemberTypeContract!.Equals(dataMember.MemberTypeContract, checkedContracts));
            }
            return false;
        }
    }
}
