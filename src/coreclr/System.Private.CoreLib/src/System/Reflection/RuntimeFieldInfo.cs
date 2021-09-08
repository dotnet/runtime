// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using RuntimeTypeCache = System.RuntimeType.RuntimeTypeCache;

namespace System.Reflection
{
    internal abstract class RuntimeFieldInfo : FieldInfo
    {
        #region Private Data Members
        private BindingFlags m_bindingFlags;
        protected RuntimeTypeCache m_reflectedTypeCache;
        protected RuntimeType m_declaringType;
        #endregion

        #region Constructor
        protected RuntimeFieldInfo(RuntimeTypeCache reflectedTypeCache, RuntimeType declaringType, BindingFlags bindingFlags)
        {
            m_bindingFlags = bindingFlags;
            m_declaringType = declaringType;
            m_reflectedTypeCache = reflectedTypeCache;
        }
        #endregion

        #region NonPublic Members
        internal BindingFlags BindingFlags => m_bindingFlags;
        private RuntimeType ReflectedTypeInternal => m_reflectedTypeCache.GetRuntimeType();

        internal RuntimeType GetDeclaringTypeInternal()
        {
            return m_declaringType;
        }

        internal RuntimeType GetRuntimeType() { return m_declaringType; }
        internal abstract RuntimeModule GetRuntimeModule();
        #endregion

        #region MemberInfo Overrides
        public override MemberTypes MemberType => MemberTypes.Field;
        public override Type? ReflectedType => m_reflectedTypeCache.IsGlobal ? null : ReflectedTypeInternal;

        public override Type? DeclaringType => m_reflectedTypeCache.IsGlobal ? null : m_declaringType;

        public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other) => HasSameMetadataDefinitionAsCore<RuntimeFieldInfo>(other);

        public override Module Module => GetRuntimeModule();
        public override bool IsCollectible => m_declaringType.IsCollectible;
        #endregion

        #region Object Overrides
        public override string ToString()
        {
            return FieldType.FormatTypeName() + " " + Name;
        }
        #endregion

        #region ICustomAttributeProvider
        public override object[] GetCustomAttributes(bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, (typeof(object) as RuntimeType)!);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));

            RuntimeType? attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            return CustomAttribute.GetCustomAttributes(this, attributeRuntimeType);
        }

        public override bool IsDefined(Type attributeType, bool inherit)
        {
            if (attributeType == null)
                throw new ArgumentNullException(nameof(attributeType));

            RuntimeType? attributeRuntimeType = attributeType.UnderlyingSystemType as RuntimeType;

            if (attributeRuntimeType == null)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(attributeType));

            return CustomAttribute.IsDefined(this, attributeRuntimeType);
        }

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return RuntimeCustomAttributeData.GetCustomAttributesInternal(this);
        }
        #endregion

        #region FieldInfo Overrides
        // All implemented on derived classes
        #endregion
    }
}
