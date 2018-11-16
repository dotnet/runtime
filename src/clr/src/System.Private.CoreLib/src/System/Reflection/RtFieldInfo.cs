// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
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
        private string m_name;
        private RuntimeType m_fieldType;
        private INVOCATION_FLAGS m_invocationFlags;

        internal INVOCATION_FLAGS InvocationFlags
        {
            get
            {
                if ((m_invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_INITIALIZED) == 0)
                {
                    Type declaringType = DeclaringType;

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
                    m_invocationFlags = invocationFlags | INVOCATION_FLAGS.INVOCATION_FLAGS_INITIALIZED;
                }

                return m_invocationFlags;
            }
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
        internal void CheckConsistency(object target)
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
                            string.Format(CultureInfo.CurrentUICulture, SR.Arg_FieldDeclTarget,
                                Name, m_declaringType, target.GetType()));
                    }
                }
            }
        }

        internal override bool CacheEquals(object o)
        {
            RtFieldInfo m = o as RtFieldInfo;

            if ((object)m == null)
                return false;

            return m.m_fieldHandle == m_fieldHandle;
        }

        // UnsafeSetValue doesn't perform any consistency or visibility check.
        // It is the caller's responsibility to ensure the operation is safe.
        // When the caller needs to perform consistency checks they should 
        // call CheckConsistency() before calling this method.
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        internal void UnsafeSetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
        {
            RuntimeType declaringType = DeclaringType as RuntimeType;
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

        // UnsafeGetValue doesn't perform any consistency or visibility check.
        // It is the caller's responsibility to ensure the operation is safe.
        // When the caller needs to perform consistency checks they should 
        // call CheckConsistency() before calling this method.
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        internal object UnsafeGetValue(object obj)
        {
            RuntimeType declaringType = DeclaringType as RuntimeType;

            RuntimeType fieldType = (RuntimeType)FieldType;

            bool domainInitialized = false;
            if (declaringType == null)
            {
                return RuntimeFieldHandle.GetValue(this, obj, fieldType, null, ref domainInitialized);
            }
            else
            {
                domainInitialized = declaringType.DomainInitialized;
                object retVal = RuntimeFieldHandle.GetValue(this, obj, fieldType, declaringType, ref domainInitialized);
                declaringType.DomainInitialized = domainInitialized;
                return retVal;
            }
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
                return string.Format("{0}.{1}", DeclaringType.FullName, Name);
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
        public override object GetValue(object obj)
        {
            INVOCATION_FLAGS invocationFlags = InvocationFlags;
            RuntimeType declaringType = DeclaringType as RuntimeType;

            if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE) != 0)
            {
                if (declaringType != null && DeclaringType.ContainsGenericParameters)
                    throw new InvalidOperationException(SR.Arg_UnboundGenField);

                throw new FieldAccessException();
            }

            CheckConsistency(obj);

            return UnsafeGetValue(obj);
        }

        public override object GetRawConstantValue() { throw new InvalidOperationException(); }

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public override object GetValueDirect(TypedReference obj)
        {
            if (obj.IsNull)
                throw new ArgumentException(SR.Arg_TypedReference_Null);

            unsafe
            {
                // Passing TypedReference by reference is easier to make correct in native code
                return RuntimeFieldHandle.GetValueDirect(this, (RuntimeType)FieldType, &obj, (RuntimeType)DeclaringType);
            }
        }

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
        {
            INVOCATION_FLAGS invocationFlags = InvocationFlags;
            RuntimeType declaringType = DeclaringType as RuntimeType;

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
                RuntimeFieldHandle.SetValueDirect(this, (RuntimeType)FieldType, &obj, value, (RuntimeType)DeclaringType);
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
            get
            {
                if (m_fieldType == null)
                    m_fieldType = new Signature(this, m_declaringType).FieldType;

                return m_fieldType;
            }
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
