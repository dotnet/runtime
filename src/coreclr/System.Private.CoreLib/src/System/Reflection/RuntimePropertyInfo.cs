// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using RuntimeTypeCache = System.RuntimeType.RuntimeTypeCache;

namespace System.Reflection
{
    internal sealed unsafe class RuntimePropertyInfo : PropertyInfo
    {
        #region Private Data Members
        private int m_token;
        private string? m_name;
        private void* m_utf8name;
        private PropertyAttributes m_flags;
        private RuntimeTypeCache m_reflectedTypeCache;
        private RuntimeMethodInfo? m_getterMethod;
        private RuntimeMethodInfo? m_setterMethod;
        private MethodInfo[]? m_otherMethod;
        private RuntimeType m_declaringType;
        private BindingFlags m_bindingFlags;
        private Signature? m_signature;
        private ParameterInfo[]? m_parameters;
        #endregion

        #region Constructor
        internal RuntimePropertyInfo(
            int tkProperty, RuntimeType declaredType, RuntimeTypeCache reflectedTypeCache, out bool isPrivate)
        {
            Debug.Assert(declaredType != null);
            Debug.Assert(reflectedTypeCache != null);
            Debug.Assert(!reflectedTypeCache.IsGlobal);

            MetadataImport scope = declaredType.GetRuntimeModule().MetadataImport;

            m_token = tkProperty;
            m_reflectedTypeCache = reflectedTypeCache;
            m_declaringType = declaredType;

            scope.GetPropertyProps(tkProperty, out m_utf8name, out m_flags, out _);

            Associates.AssignAssociates(scope, tkProperty, declaredType, reflectedTypeCache.GetRuntimeType(),
                out _, out _, out _,
                out m_getterMethod, out m_setterMethod, out m_otherMethod,
                out isPrivate, out m_bindingFlags);
        }
        #endregion

        #region Internal Members
        internal override bool CacheEquals(object? o)
        {
            return
                o is RuntimePropertyInfo m &&
                m.m_token == m_token &&
                RuntimeTypeHandle.GetModule(m_declaringType).Equals(
                    RuntimeTypeHandle.GetModule(m.m_declaringType));
        }

        internal Signature Signature
        {
            get
            {
                if (m_signature == null)
                {

                    GetRuntimeModule().MetadataImport.GetPropertyProps(
                        m_token, out _, out _, out ConstArray sig);

                    m_signature = new Signature(sig.Signature.ToPointer(), sig.Length, m_declaringType);
                }

                return m_signature;
            }
        }
        internal bool EqualsSig(RuntimePropertyInfo target)
        {
            // @Asymmetry - Legacy policy is to remove duplicate properties, including hidden properties.
            //             The comparison is done by name and by sig. The EqualsSig comparison is expensive
            //             but forutnetly it is only called when an inherited property is hidden by name or
            //             when an interfaces declare properies with the same signature.
            //             Note that we intentionally don't resolve generic arguments so that we don't treat
            //             signatures that only match in certain instantiations as duplicates. This has the
            //             down side of treating overriding and overriden properties as different properties
            //             in some cases. But PopulateProperties in rttype.cs should have taken care of that
            //             by comparing VTable slots.
            //
            //             Class C1(Of T, Y)
            //                 Property Prop1(ByVal t1 As T) As Integer
            //                     Get
            //                         ... ...
            //                     End Get
            //                 End Property
            //                 Property Prop1(ByVal y1 As Y) As Integer
            //                     Get
            //                         ... ...
            //                     End Get
            //                 End Property
            //             End Class
            //

            Debug.Assert(Name.Equals(target.Name));
            Debug.Assert(this != target);
            Debug.Assert(this.ReflectedType == target.ReflectedType);

            return Signature.CompareSig(this.Signature, target.Signature);
        }
        internal BindingFlags BindingFlags => m_bindingFlags;
        #endregion

        #region Object Overrides
        public override string ToString()
        {
            var sbName = new ValueStringBuilder(MethodBase.MethodNameBufferSize);

            sbName.Append(PropertyType.FormatTypeName());
            sbName.Append(' ');
            sbName.Append(Name);

            RuntimeType[] arguments = Signature.Arguments;
            if (arguments.Length > 0)
            {
                sbName.Append(" [");
                MethodBase.AppendParameters(ref sbName, arguments, Signature.CallingConvention);
                sbName.Append(']');
            }

            return sbName.ToString();
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
            return CustomAttributeData.GetCustomAttributesInternal(this);
        }
        #endregion

        #region MemberInfo Overrides
        public override MemberTypes MemberType => MemberTypes.Property;
        public override string Name => m_name ??= new MdUtf8String(m_utf8name).ToString();
        public override Type? DeclaringType => m_declaringType;

        public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other) => HasSameMetadataDefinitionAsCore<RuntimePropertyInfo>(other);

        public override Type? ReflectedType => ReflectedTypeInternal;

        private RuntimeType ReflectedTypeInternal => m_reflectedTypeCache.GetRuntimeType();

        public override int MetadataToken => m_token;

        public override Module Module => GetRuntimeModule();
        internal RuntimeModule GetRuntimeModule() { return m_declaringType.GetRuntimeModule(); }
        public override bool IsCollectible => m_declaringType.IsCollectible;
        #endregion

        #region PropertyInfo Overrides

        #region Non Dynamic

        public override Type[] GetRequiredCustomModifiers()
        {
            return Signature.GetCustomModifiers(0, true);
        }

        public override Type[] GetOptionalCustomModifiers()
        {
            return Signature.GetCustomModifiers(0, false);
        }

        internal object GetConstantValue(bool raw)
        {
            object? defaultValue = MdConstant.GetValue(GetRuntimeModule().MetadataImport, m_token, PropertyType.GetTypeHandleInternal(), raw);

            if (defaultValue == DBNull.Value)
                // Arg_EnumLitValueNotFound -> "Literal value was not found."
                throw new InvalidOperationException(SR.Arg_EnumLitValueNotFound);

            return defaultValue!;
        }

        public override object? GetConstantValue() { return GetConstantValue(false); }

        public override object? GetRawConstantValue() { return GetConstantValue(true); }

        public override MethodInfo[] GetAccessors(bool nonPublic)
        {
            List<MethodInfo> accessorList = new List<MethodInfo>();

            if (Associates.IncludeAccessor(m_getterMethod, nonPublic))
                accessorList.Add(m_getterMethod!);

            if (Associates.IncludeAccessor(m_setterMethod, nonPublic))
                accessorList.Add(m_setterMethod!);

            if (m_otherMethod is not null)
            {
                for (int i = 0; i < m_otherMethod.Length; i++)
                {
                    if (Associates.IncludeAccessor(m_otherMethod[i], nonPublic))
                        accessorList.Add(m_otherMethod[i]);
                }
            }
            return accessorList.ToArray();
        }

        public override Type PropertyType => Signature.ReturnType;

        public override RuntimeMethodInfo? GetGetMethod(bool nonPublic)
        {
            if (!Associates.IncludeAccessor(m_getterMethod, nonPublic))
                return null;

            return m_getterMethod;
        }

        public override RuntimeMethodInfo? GetSetMethod(bool nonPublic)
        {
            if (!Associates.IncludeAccessor(m_setterMethod, nonPublic))
                return null;

            return m_setterMethod;
        }

        public override ParameterInfo[] GetIndexParameters()
        {
            ParameterInfo[] indexParams = GetIndexParametersNoCopy();

            int numParams = indexParams.Length;

            if (numParams == 0)
                return indexParams;

            ParameterInfo[] ret = new ParameterInfo[numParams];

            Array.Copy(indexParams, ret, numParams);

            return ret;
        }

        internal ParameterInfo[] GetIndexParametersNoCopy()
        {
            // @History - Logic ported from RTM

            // No need to lock because we don't guarantee the uniqueness of ParameterInfo objects
            if (m_parameters == null)
            {
                int numParams = 0;
                ParameterInfo[]? methParams = null;

                // First try to get the Get method.
                RuntimeMethodInfo? m = GetGetMethod(true);
                if (m != null)
                {
                    // There is a Get method so use it.
                    methParams = m.GetParametersNoCopy();
                    numParams = methParams.Length;
                }
                else
                {
                    // If there is no Get method then use the Set method.
                    m = GetSetMethod(true);

                    if (m != null)
                    {
                        methParams = m.GetParametersNoCopy();
                        numParams = methParams.Length - 1;
                    }
                }

                // Now copy over the parameter info's and change their
                // owning member info to the current property info.

                ParameterInfo[] propParams = numParams != 0 ?
                    new ParameterInfo[numParams] :
                    Array.Empty<ParameterInfo>();

                for (int i = 0; i < propParams.Length; i++)
                    propParams[i] = new RuntimeParameterInfo((RuntimeParameterInfo)methParams![i], this);

                m_parameters = propParams;
            }

            return m_parameters;
        }

        public override PropertyAttributes Attributes => m_flags;

        public override bool CanRead => m_getterMethod != null;

        public override bool CanWrite => m_setterMethod != null;
        #endregion

        #region Dynamic
        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public override object? GetValue(object? obj, object?[]? index)
        {
            return GetValue(obj, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
                null, index, null);
        }

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public override object? GetValue(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture)
        {
            RuntimeMethodInfo? m = GetGetMethod(true);
            if (m == null)
                throw new ArgumentException(System.SR.Arg_GetMethNotFnd);
            return m.Invoke(obj, invokeAttr, binder, index, null);
        }

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public override void SetValue(object? obj, object? value, object?[]? index)
        {
            SetValue(obj,
                    value,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
                    null,
                    index,
                    null);
        }

        [DebuggerStepThroughAttribute]
        [Diagnostics.DebuggerHidden]
        public override void SetValue(object? obj, object? value, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture)
        {
            RuntimeMethodInfo? m = GetSetMethod(true);

            if (m == null)
                throw new ArgumentException(System.SR.Arg_SetMethNotFnd);

            if (index is null)
            {
                m.InvokeOneParameter(obj, invokeAttr, binder, value, culture);
            }
            else
            {
                var args = new object?[index.Length + 1];

                for (int i = 0; i < index.Length; i++)
                    args[i] = index[i];

                args[index.Length] = value;

                m.Invoke(obj, invokeAttr, binder, args, culture);
            }
        }
        #endregion

        #endregion
    }
}
