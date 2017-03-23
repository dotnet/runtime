// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;
using RuntimeTypeCache = System.RuntimeType.RuntimeTypeCache;

namespace System.Reflection
{
    [Serializable]
    internal abstract class RuntimeFieldInfo : FieldInfo, ISerializable
    {
        #region Private Data Members
        private BindingFlags m_bindingFlags;
        protected RuntimeTypeCache m_reflectedTypeCache;
        protected RuntimeType m_declaringType;
        #endregion

        #region Constructor
        protected RuntimeFieldInfo()
        {
            // Used for dummy head node during population
        }
        protected RuntimeFieldInfo(RuntimeTypeCache reflectedTypeCache, RuntimeType declaringType, BindingFlags bindingFlags)
        {
            m_bindingFlags = bindingFlags;
            m_declaringType = declaringType;
            m_reflectedTypeCache = reflectedTypeCache;
        }
        #endregion

        #region NonPublic Members
        internal BindingFlags BindingFlags { get { return m_bindingFlags; } }
        private RuntimeType ReflectedTypeInternal
        {
            get
            {
                return m_reflectedTypeCache.GetRuntimeType();
            }
        }

        internal RuntimeType GetDeclaringTypeInternal()
        {
            return m_declaringType;
        }

        internal RuntimeType GetRuntimeType() { return m_declaringType; }
        internal abstract RuntimeModule GetRuntimeModule();
        #endregion

        #region MemberInfo Overrides
        public override MemberTypes MemberType { get { return MemberTypes.Field; } }
        public override Type ReflectedType
        {
            get
            {
                return m_reflectedTypeCache.IsGlobal ? null : ReflectedTypeInternal;
            }
        }

        public override Type DeclaringType
        {
            get
            {
                return m_reflectedTypeCache.IsGlobal ? null : m_declaringType;
            }
        }

        public override Module Module { get { return GetRuntimeModule(); } }
        #endregion

        #region Object Overrides
        public unsafe override String ToString()
        {
            return FieldType.FormatTypeName() + " " + Name;
        }
        #endregion

        #region ICustomAttributeProvider
        public override Object[] GetCustomAttributes(bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, typeof(object) as RuntimeType);
        }

        public override Object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));
            Contract.EndContractBlock();

            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            return CustomAttribute.GetCustomAttributes(this, attributeRuntimeType);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));
            Contract.EndContractBlock();

            RuntimeType attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            return CustomAttribute.IsDefined(this, attributeRuntimeType);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return CustomAttributeData.GetCustomAttributesInternal(this);
        }
        #endregion

        #region FieldInfo Overrides
        // All implemented on derived classes
        #endregion

        #region ISerializable Implementation
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            Contract.EndContractBlock();

            MemberInfoSerializationHolder.GetSerializationInfo(info, this);
        }
        #endregion
    }
}
