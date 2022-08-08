// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Reflection.Metadata;
using RuntimeTypeCache = System.RuntimeType.RuntimeTypeCache;

namespace System.Reflection
{
    internal sealed unsafe class MdFieldInfo : RuntimeFieldInfo
    {
        #region Private Data Members
        private int m_tkField;
        private string? m_name;
        private RuntimeType? m_fieldType;
        private FieldAttributes m_fieldAttributes;
        #endregion

        #region Constructor
        internal MdFieldInfo(
        int tkField, FieldAttributes fieldAttributes, RuntimeTypeHandle declaringTypeHandle, RuntimeTypeCache reflectedTypeCache, BindingFlags bindingFlags)
            : base(reflectedTypeCache, declaringTypeHandle.GetRuntimeType(), bindingFlags)
        {
            m_tkField = tkField;
            m_name = null;
            m_fieldAttributes = fieldAttributes;
        }
        #endregion

        #region Internal Members
        internal override bool CacheEquals(object? o)
        {
            return
                o is MdFieldInfo m &&
                m.m_tkField == m_tkField &&
                ReferenceEquals(m_declaringType, m.m_declaringType);
        }
        #endregion

        #region MemberInfo Overrides
        public override string Name => m_name ??= GetRuntimeModule().MetadataImport.GetName(m_tkField).ToString();

        public override int MetadataToken => m_tkField;
        internal override RuntimeModule GetRuntimeModule() { return m_declaringType.GetRuntimeModule(); }

        public override bool Equals(object? obj) =>
            ReferenceEquals(this, obj) ||
            (MetadataUpdater.IsSupported && CacheEquals(obj));

        public override int GetHashCode() =>
            HashCode.Combine(m_tkField.GetHashCode(), m_declaringType.GetUnderlyingNativeHandle().GetHashCode());
        #endregion

        #region FieldInfo Overrides
        public override RuntimeFieldHandle FieldHandle => throw new NotSupportedException();
        public override FieldAttributes Attributes => m_fieldAttributes;

        public override bool IsSecurityCritical => DeclaringType!.IsSecurityCritical;
        public override bool IsSecuritySafeCritical => DeclaringType!.IsSecuritySafeCritical;
        public override bool IsSecurityTransparent => DeclaringType!.IsSecurityTransparent;

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public override object? GetValueDirect(TypedReference obj)
        {
            return GetValue(null);
        }

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public override void SetValueDirect(TypedReference obj, object value)
        {
            throw new FieldAccessException(SR.Acc_ReadOnly);
        }

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public override object? GetValue(object? obj)
        {
            return GetValue(false);
        }

        public override object? GetRawConstantValue() { return GetValue(true); }

        private object? GetValue(bool raw)
        {
            // Cannot cache these because they could be user defined non-agile enumerations

            object? value = MdConstant.GetValue(GetRuntimeModule().MetadataImport, m_tkField, FieldType.TypeHandle, raw);

            if (value == DBNull.Value)
                throw new NotSupportedException(SR.Arg_EnumLitValueNotFound);

            return value;
        }

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public override void SetValue(object? obj, object? value, BindingFlags invokeAttr, Binder? binder, CultureInfo? culture)
        {
            throw new FieldAccessException(SR.Acc_ReadOnly);
        }

        public override Type FieldType
        {
            get
            {
                if (m_fieldType == null)
                {
                    ConstArray fieldMarshal = GetRuntimeModule().MetadataImport.GetSigOfFieldDef(m_tkField);

                    m_fieldType = new Signature(fieldMarshal.Signature.ToPointer(),
                        (int)fieldMarshal.Length, m_declaringType).FieldType;
                }

                return m_fieldType;
            }
        }

        public override Type[] GetRequiredCustomModifiers()
        {
            return Type.EmptyTypes;
        }

        public override Type[] GetOptionalCustomModifiers()
        {
            return Type.EmptyTypes;
        }

        #endregion
    }
}
