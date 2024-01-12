// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using RuntimeTypeCache = System.RuntimeType.RuntimeTypeCache;

namespace System.Reflection
{
    internal sealed unsafe class RtFieldInfo : RuntimeFieldInfo, IRuntimeFieldInfo
    {
        #region Private Data Members
        // aggressive caching
        private readonly IntPtr m_fieldHandle;
        private readonly FieldAttributes m_fieldAttributes;
        // lazy caching
        private string? m_name;
        private RuntimeType? m_fieldType;
        private FieldAccessor? m_fieldAccessor;

        internal FieldAccessor FieldAccessor
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                m_fieldAccessor ??= new FieldAccessor(this);
                return m_fieldAccessor;
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
        RuntimeFieldHandleInternal IRuntimeFieldInfo.Value => new RuntimeFieldHandleInternal(m_fieldHandle);
        #endregion

        #region Internal Members
        internal override bool CacheEquals(object? o)
        {
            return o is RtFieldInfo m && m.m_fieldHandle == m_fieldHandle;
        }

        #endregion

        #region MemberInfo Overrides
        public override string Name => m_name ??= RuntimeFieldHandle.GetName(this);

        internal string FullName => DeclaringType!.FullName + "." + Name;

        public override int MetadataToken => RuntimeFieldHandle.GetToken(this);

        internal override RuntimeModule GetRuntimeModule()
        {
            return RuntimeTypeHandle.GetModule(RuntimeFieldHandle.GetApproxDeclaringType(this));
        }

        public override bool Equals(object? obj) =>
            ReferenceEquals(this, obj) ||
            (MetadataUpdater.IsSupported &&
                obj is RtFieldInfo fi &&
                fi.m_fieldHandle == m_fieldHandle &&
                ReferenceEquals(fi.m_reflectedTypeCache.GetRuntimeType(), m_reflectedTypeCache.GetRuntimeType()));

        public override int GetHashCode() =>
            HashCode.Combine(m_fieldHandle.GetHashCode(), m_declaringType.GetUnderlyingNativeHandle().GetHashCode());

        #endregion

        #region FieldInfo Overrides
        [DebuggerStepThrough]
        [DebuggerHidden]
        public override object? GetValue(object? obj) => FieldAccessor.GetValue(obj);

        public override object GetRawConstantValue() { throw new InvalidOperationException(); }

        [DebuggerStepThrough]
        [DebuggerHidden]
        public override object? GetValueDirect(TypedReference obj)
        {
            if (obj.IsNull)
                throw new ArgumentException(SR.Arg_TypedReference_Null);

            // Passing TypedReference by reference is easier to make correct in native code
#pragma warning disable CS8500 // Takes a pointer to a managed type
            return RuntimeFieldHandle.GetValueDirect(this, (RuntimeType)FieldType, &obj, (RuntimeType?)DeclaringType);
#pragma warning restore CS8500
        }

        [DebuggerStepThrough]
        [DebuggerHidden]
        public override void SetValue(object? obj, object? value, BindingFlags invokeAttr, Binder? binder, CultureInfo? culture)
            => FieldAccessor.SetValue(obj, value, invokeAttr, binder, culture);

        [DebuggerStepThrough]
        [DebuggerHidden]
        public override void SetValueDirect(TypedReference obj, object value)
        {
            if (obj.IsNull)
                throw new ArgumentException(SR.Arg_TypedReference_Null);

            // Passing TypedReference by reference is easier to make correct in native code
#pragma warning disable CS8500 // Takes a pointer to a managed type
            RuntimeFieldHandle.SetValueDirect(this, (RuntimeType)FieldType, &obj, value, (RuntimeType?)DeclaringType);
#pragma warning restore CS8500
        }

        public override RuntimeFieldHandle FieldHandle => new RuntimeFieldHandle(this);

        internal IntPtr GetFieldHandle()
        {
            return m_fieldHandle;
        }

        public override FieldAttributes Attributes => m_fieldAttributes;

        public override Type FieldType
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_fieldType ?? InitializeFieldType();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private RuntimeType InitializeFieldType()
        {
            return m_fieldType = GetSignature().FieldType;
        }

        public override Type[] GetRequiredCustomModifiers()
        {
            return GetSignature().GetCustomModifiers(1, true);
        }

        public override Type[] GetOptionalCustomModifiers()
        {
            return GetSignature().GetCustomModifiers(1, false);
        }

        internal Signature GetSignature() => new Signature(this, m_declaringType);

        public override Type GetModifiedFieldType() =>
            ModifiedType.Create(FieldType, GetSignature());
        #endregion
    }
}
