// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using RuntimeTypeCache = System.RuntimeType.RuntimeTypeCache;

namespace System.Reflection
{
    internal unsafe sealed class RtFieldInfo : RuntimeFieldInfo, IRuntimeFieldInfo
    {
        #region Private Data Members
        // aggressive caching
        private IntPtr m_fieldHandle;
        private FieldAttributes m_fieldAttributes;
        // lazy caching
        private string? m_name;
        private RuntimeType? m_fieldType;
        private INVOCATION_FLAGS m_invocationFlags;

        internal INVOCATION_FLAGS InvocationFlags
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return (m_invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_INITIALIZED) != 0 ?
                    m_invocationFlags : InitializeInvocationFlags();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private INVOCATION_FLAGS InitializeInvocationFlags()
        {
            Type? declaringType = DeclaringType;

            INVOCATION_FLAGS invocationFlags = 0;

            // first take care of all the NO_INVOKE cases
            if (declaringType != null && declaringType.ContainsGenericParameters)
            {
                invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE;
            }

            // If the invocationFlags are still 0, then
            // this should be an usable field, determine the other flags
            if (invocationFlags == 0)
            {
                if ((m_fieldAttributes & FieldAttributes.InitOnly) != (FieldAttributes)0)
                    invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_SPECIAL_FIELD;

                if ((m_fieldAttributes & FieldAttributes.HasFieldRVA) != (FieldAttributes)0)
                    invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_SPECIAL_FIELD;

                // find out if the field type is one of the following: Primitive, Enum or Pointer
                Type fieldType = FieldType;
                if (fieldType.IsPointer || fieldType.IsEnum || fieldType.IsPrimitive)
                    invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_FIELD_SPECIAL_CAST;
            }

            // must be last to avoid threading problems
            return (m_invocationFlags = invocationFlags | INVOCATION_FLAGS.INVOCATION_FLAGS_INITIALIZED);
        }
        #endregion

        #region Constructor
        internal RtFieldInfo(
            RuntimeFieldHandleInternal handle, RuntimeType declaringType, RuntimeTypeCache reflectedTypeCache, BindingFlags bindingFlags)
            : base(reflectedTypeCache, declaringType, bindingFlags)
        {
            m_fieldHandle = handle.Value;
            m_fieldAttributes = RuntimeFieldHandle.GetAttributes(handle);
        }
        #endregion

        #region Private Members
        RuntimeFieldHandleInternal IRuntimeFieldInfo.Value
        {
            get
            {
                return new RuntimeFieldHandleInternal(m_fieldHandle);
            }
        }

        #endregion

        #region Internal Members
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CheckConsistency(object? target)
        {
            // only test instance fields
            if ((m_fieldAttributes & FieldAttributes.Static) != FieldAttributes.Static)
            {
                if (!m_declaringType.IsInstanceOfType(target))
                {
                    if (target == null)
                    {
                        throw new TargetException(SR.RFLCT_Targ_StatFldReqTarg);
                    }
                    else
                    {
                        throw new ArgumentException(
                            SR.Format(SR.Arg_FieldDeclTarget,
                                Name, m_declaringType, target.GetType()));
                    }
                }
            }
        }

        internal override bool CacheEquals(object? o)
        {
            RtFieldInfo? m = o as RtFieldInfo;

            if (m is null)
                return false;

            return m.m_fieldHandle == m_fieldHandle;
        }

        #endregion

        #region MemberInfo Overrides
        public override string Name
        {
            get
            {
                if (m_name == null)
                    m_name = RuntimeFieldHandle.GetName(this);

                return m_name;
            }
        }

        internal string FullName
        {
            get
            {
                return DeclaringType!.FullName + "." + Name;
            }
        }

        public override int MetadataToken
        {
            get { return RuntimeFieldHandle.GetToken(this); }
        }

        internal override RuntimeModule GetRuntimeModule()
        {
            return RuntimeTypeHandle.GetModule(RuntimeFieldHandle.GetApproxDeclaringType(this));
        }

        #endregion

        #region FieldInfo Overrides
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public override object? GetValue(object? obj)
        {
            INVOCATION_FLAGS invocationFlags = InvocationFlags;
            RuntimeType? declaringType = DeclaringType as RuntimeType;

            if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE) != 0)
            {
                if (declaringType != null && DeclaringType!.ContainsGenericParameters)
                    throw new InvalidOperationException(SR.Arg_UnboundGenField);

                throw new FieldAccessException();
            }

            CheckConsistency(obj);

            RuntimeType fieldType = (RuntimeType)FieldType;

            bool domainInitialized = false;
            if (declaringType == null)
            {
                return RuntimeFieldHandle.GetValue(this, obj, fieldType, null, ref domainInitialized);
            }
            else
            {
                domainInitialized = declaringType.DomainInitialized;
                object? retVal = RuntimeFieldHandle.GetValue(this, obj, fieldType, declaringType, ref domainInitialized);
                declaringType.DomainInitialized = domainInitialized;
                return retVal;
            }
        }

        public override object GetRawConstantValue() { throw new InvalidOperationException(); }

#pragma warning disable CS8609 // TODO-NULLABLE: Covariant return types (https://github.com/dotnet/roslyn/issues/23268)
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public override object? GetValueDirect(TypedReference obj)
        {
            if (obj.IsNull)
                throw new ArgumentException(SR.Arg_TypedReference_Null);

            unsafe
            {
                // Passing TypedReference by reference is easier to make correct in native code
                return RuntimeFieldHandle.GetValueDirect(this, (RuntimeType)FieldType, &obj, (RuntimeType?)DeclaringType);
            }
        }
#pragma warning restore CS8609

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public override void SetValue(object? obj, object? value, BindingFlags invokeAttr, Binder? binder, CultureInfo? culture)
        {
            INVOCATION_FLAGS invocationFlags = InvocationFlags;
            RuntimeType? declaringType = DeclaringType as RuntimeType;

            if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE) != 0)
            {
                if (declaringType != null && declaringType.ContainsGenericParameters)
                    throw new InvalidOperationException(SR.Arg_UnboundGenField);

                throw new FieldAccessException();
            }

            CheckConsistency(obj);

            RuntimeType fieldType = (RuntimeType)FieldType;
            value = fieldType.CheckValue(value, binder, culture, invokeAttr);

            bool domainInitialized = false;
            if (declaringType == null)
            {
                RuntimeFieldHandle.SetValue(this, obj, value, fieldType, m_fieldAttributes, null, ref domainInitialized);
            }
            else
            {
                domainInitialized = declaringType.DomainInitialized;
                RuntimeFieldHandle.SetValue(this, obj, value, fieldType, m_fieldAttributes, declaringType, ref domainInitialized);
                declaringType.DomainInitialized = domainInitialized;
            }
        }

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public override void SetValueDirect(TypedReference obj, object value)
        {
            if (obj.IsNull)
                throw new ArgumentException(SR.Arg_TypedReference_Null);

            unsafe
            {
                // Passing TypedReference by reference is easier to make correct in native code
                RuntimeFieldHandle.SetValueDirect(this, (RuntimeType)FieldType, &obj, value, (RuntimeType?)DeclaringType);
            }
        }

        public override RuntimeFieldHandle FieldHandle
        {
            get
            {
                return new RuntimeFieldHandle(this);
            }
        }

        internal IntPtr GetFieldHandle()
        {
            return m_fieldHandle;
        }

        public override FieldAttributes Attributes
        {
            get
            {
                return m_fieldAttributes;
            }
        }

        public override Type FieldType
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return m_fieldType ?? InitializeFieldType();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private RuntimeType InitializeFieldType()
        {
            return (m_fieldType = new Signature(this, m_declaringType).FieldType);
        }

        public override Type[] GetRequiredCustomModifiers()
        {
            return new Signature(this, m_declaringType).GetCustomModifiers(1, true);
        }

        public override Type[] GetOptionalCustomModifiers()
        {
            return new Signature(this, m_declaringType).GetCustomModifiers(1, false);
        }

        #endregion
    }
}
