// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.CustomAttributes;

namespace System.Reflection.Runtime.ParameterInfos
{
    //
    // This implements ParameterInfo objects owned by MethodBase objects that have associated Parameter metadata. (In practice,
    // this means all non-return parameters since most such parameters have at least a name.)
    //
    internal abstract class RuntimeFatMethodParameterInfo : RuntimeMethodParameterInfo
    {
        protected RuntimeFatMethodParameterInfo(MethodBase member, int position, QSignatureTypeHandle qualifiedParameterTypeHandle, TypeContext typeContext)
            : base(member, position, qualifiedParameterTypeHandle, typeContext)
        {
        }

        public sealed override IEnumerable<CustomAttributeData> CustomAttributes
        {
            get
            {
                foreach (CustomAttributeData cad in TrueCustomAttributes)
                    yield return cad;

                ParameterAttributes attributes = Attributes;
                if (0 != (attributes & ParameterAttributes.In))
                    yield return new RuntimePseudoCustomAttributeData(typeof(InAttribute), null);
                if (0 != (attributes & ParameterAttributes.Out))
                    yield return new RuntimePseudoCustomAttributeData(typeof(OutAttribute), null);
                if (0 != (attributes & ParameterAttributes.Optional))
                    yield return new RuntimePseudoCustomAttributeData(typeof(OptionalAttribute), null);
            }
        }

        protected abstract IEnumerable<CustomAttributeData> TrueCustomAttributes { get; }

        public sealed override bool HasDefaultValue => DefaultValueInfo.Item1;
        public sealed override object DefaultValue => DefaultValueInfo.Item2;

        public sealed override object RawDefaultValue
        {
            get
            {
                Tuple<object> rawDefaultValueInfo = _lazyRawDefaultValueInfo;
                if (rawDefaultValueInfo == null)
                {
                    object rawDefaultValue;
                    GetDefaultValueOrSentinel(raw: true, defaultValue: out rawDefaultValue);
                    rawDefaultValueInfo = _lazyRawDefaultValueInfo = Tuple.Create(rawDefaultValue);
                }
                return rawDefaultValueInfo.Item1;
            }
        }

        protected abstract bool GetDefaultValueIfAvailable(bool raw, out object defaultValue);

        private Tuple<bool, object> DefaultValueInfo
        {
            get
            {
                Tuple<bool, object> defaultValueInfo = _lazyDefaultValueInfo;
                if (defaultValueInfo == null)
                {
                    object defaultValue;
                    bool hasDefaultValue = GetDefaultValueOrSentinel(raw: false, defaultValue: out defaultValue);
                    defaultValueInfo = _lazyDefaultValueInfo = Tuple.Create(hasDefaultValue, defaultValue);
                }
                return defaultValueInfo;
            }
        }

        private bool GetDefaultValueOrSentinel(bool raw, out object defaultValue)
        {
            bool hasDefaultValue = GetDefaultValueIfAvailable(raw, out defaultValue);
            if (!hasDefaultValue)
            {
                defaultValue = IsOptional ? (object)Missing.Value : (object)DBNull.Value;
            }
            return hasDefaultValue;
        }

        private volatile Tuple<bool, object> _lazyDefaultValueInfo;
        private volatile Tuple<object> _lazyRawDefaultValueInfo;
    }
}
