// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using RuntimeTypeCache = System.RuntimeType.RuntimeTypeCache;

namespace System.Reflection
{
    internal unsafe sealed class RtFieldInfo : RuntimeFieldInfo, IRuntimeFieldInfo
    {
        #region FCalls
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        static private extern void PerformVisibilityCheckOnField(IntPtr field, Object target, RuntimeType declaringType, FieldAttributes attr, uint invocationFlags);
        #endregion

        #region Private Data Members
        // agressive caching
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
                    bool fIsReflectionOnlyType = (declaringType is ReflectionOnlyType);

                    INVOCATION_FLAGS invocationFlags = 0;

                    // first take care of all the NO_INVOKE cases
                    if (
                        (declaringType != null && declaringType.ContainsGenericParameters) ||
                        (declaringType == null && Module.Assembly.ReflectionOnly) ||
                        (fIsReflectionOnlyType)
                       )
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

                        // A public field is inaccesible to Transparent code if the field is Critical.
                        bool needsTransparencySecurityCheck = IsSecurityCritical && !IsSecuritySafeCritical;
                        bool needsVisibilitySecurityCheck = ((m_fieldAttributes & FieldAttributes.FieldAccessMask) != FieldAttributes.Public) ||
                                                            (declaringType != null && declaringType.NeedsReflectionSecurityCheck);
                        if (needsTransparencySecurityCheck || needsVisibilitySecurityCheck)
                            invocationFlags |= INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY;

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

        private RuntimeAssembly GetRuntimeAssembly() { return m_declaringType.GetRuntimeAssembly(); }

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
        internal void CheckConsistency(Object target)
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
                            String.Format(CultureInfo.CurrentUICulture, SR.Arg_FieldDeclTarget,
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

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        internal void InternalSetValue(Object obj, Object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture, ref StackCrawlMark stackMark)
        {
            INVOCATION_FLAGS invocationFlags = InvocationFlags;
            RuntimeType declaringType = DeclaringType as RuntimeType;

            if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE) != 0)
            {
                if (declaringType != null && declaringType.ContainsGenericParameters)
                    throw new InvalidOperationException(SR.Arg_UnboundGenField);

                if ((declaringType == null && Module.Assembly.ReflectionOnly) || declaringType is ReflectionOnlyType)
                    throw new InvalidOperationException(SR.Arg_ReflectionOnlyField);

                throw new FieldAccessException();
            }

            CheckConsistency(obj);

            RuntimeType fieldType = (RuntimeType)FieldType;
            value = fieldType.CheckValue(value, binder, culture, invokeAttr);

            #region Security Check
            if ((invocationFlags & (INVOCATION_FLAGS.INVOCATION_FLAGS_SPECIAL_FIELD | INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY)) != 0)
                PerformVisibilityCheckOnField(m_fieldHandle, obj, m_declaringType, m_fieldAttributes, (uint)m_invocationFlags);
            #endregion

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

        // UnsafeSetValue doesn't perform any consistency or visibility check.
        // It is the caller's responsibility to ensure the operation is safe.
        // When the caller needs to perform visibility checks they should call
        // InternalSetValue() instead. When the caller needs to perform 
        // consistency checks they should call CheckConsistency() before 
        // calling this method.
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        internal void UnsafeSetValue(Object obj, Object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
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

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        internal Object InternalGetValue(Object obj, ref StackCrawlMark stackMark)
        {
            INVOCATION_FLAGS invocationFlags = InvocationFlags;
            RuntimeType declaringType = DeclaringType as RuntimeType;

            if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NO_INVOKE) != 0)
            {
                if (declaringType != null && DeclaringType.ContainsGenericParameters)
                    throw new InvalidOperationException(SR.Arg_UnboundGenField);

                if ((declaringType == null && Module.Assembly.ReflectionOnly) || declaringType is ReflectionOnlyType)
                    throw new InvalidOperationException(SR.Arg_ReflectionOnlyField);

                throw new FieldAccessException();
            }

            CheckConsistency(obj);

            RuntimeType fieldType = (RuntimeType)FieldType;
            if ((invocationFlags & INVOCATION_FLAGS.INVOCATION_FLAGS_NEED_SECURITY) != 0)
                PerformVisibilityCheckOnField(m_fieldHandle, obj, m_declaringType, m_fieldAttributes, (uint)(m_invocationFlags & ~INVOCATION_FLAGS.INVOCATION_FLAGS_SPECIAL_FIELD));

            return UnsafeGetValue(obj);
        }

        // UnsafeGetValue doesn't perform any consistency or visibility check.
        // It is the caller's responsibility to ensure the operation is safe.
        // When the caller needs to perform visibility checks they should call
        // InternalGetValue() instead. When the caller needs to perform 
        // consistency checks they should call CheckConsistency() before 
        // calling this method.
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        internal Object UnsafeGetValue(Object obj)
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
        public override String Name
        {
            get
            {
                if (m_name == null)
                    m_name = RuntimeFieldHandle.GetName(this);

                return m_name;
            }
        }

        internal String FullName
        {
            get
            {
                return String.Format("{0}.{1}", DeclaringType.FullName, Name);
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
        public override Object GetValue(Object obj)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            return InternalGetValue(obj, ref stackMark);
        }

        public override object GetRawConstantValue() { throw new InvalidOperationException(); }

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public override Object GetValueDirect(TypedReference obj)
        {
            if (obj.IsNull)
                throw new ArgumentException(SR.Arg_TypedReference_Null);
            Contract.EndContractBlock();

            unsafe
            {
                // Passing TypedReference by reference is easier to make correct in native code
                return RuntimeFieldHandle.GetValueDirect(this, (RuntimeType)FieldType, &obj, (RuntimeType)DeclaringType);
            }
        }

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public override void SetValue(Object obj, Object value, BindingFlags invokeAttr, Binder binder, CultureInfo culture)
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            InternalSetValue(obj, value, invokeAttr, binder, culture, ref stackMark);
        }

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public override void SetValueDirect(TypedReference obj, Object value)
        {
            if (obj.IsNull)
                throw new ArgumentException(SR.Arg_TypedReference_Null);
            Contract.EndContractBlock();

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
                Type declaringType = DeclaringType;
                if ((declaringType == null && Module.Assembly.ReflectionOnly) || declaringType is ReflectionOnlyType)
                    throw new InvalidOperationException(SR.InvalidOperation_NotAllowedInReflectionOnly);
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
