// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection.Runtime.PropertyInfos;

namespace System.Reflection.Runtime.ParameterInfos
{
    //
    // This implements ParameterInfo objects returned by PropertyInfo.GetIndexParameters(). Basically, they're identical to the underling accessor method's
    // ParameterInfo's except that the Member property returns the PropertyInfo rather than a MethodBase.
    //
    internal sealed partial class RuntimePropertyIndexParameterInfo : RuntimeParameterInfo
    {
        private RuntimePropertyIndexParameterInfo(RuntimePropertyInfo member, RuntimeParameterInfo backingParameter)
            : base(member, backingParameter.Position)
        {
            _backingParameter = backingParameter;
        }

        public sealed override ParameterAttributes Attributes
        {
            get
            {
                return _backingParameter.Attributes;
            }
        }

        public sealed override IEnumerable<CustomAttributeData> CustomAttributes
        {
            get
            {
                return _backingParameter.CustomAttributes;
            }
        }

        public sealed override object DefaultValue
        {
            get
            {
                return _backingParameter.DefaultValue;
            }
        }

        public sealed override object RawDefaultValue
        {
            get
            {
                return _backingParameter.RawDefaultValue;
            }
        }

        public sealed override Type[] GetOptionalCustomModifiers()
        {
            return _backingParameter.GetOptionalCustomModifiers();
        }

        public sealed override Type[] GetRequiredCustomModifiers()
        {
            return _backingParameter.GetRequiredCustomModifiers();
        }

        public sealed override bool HasDefaultValue
        {
            get
            {
                return _backingParameter.HasDefaultValue;
            }
        }

        public sealed override string Name
        {
            get
            {
                return _backingParameter.Name;
            }
        }

        public sealed override Type ParameterType
        {
            get
            {
                return _backingParameter.ParameterType;
            }
        }

        public sealed override int MetadataToken
        {
            get
            {
                return _backingParameter.MetadataToken;
            }
        }

        private readonly RuntimeParameterInfo _backingParameter;
    }
}
