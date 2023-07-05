// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;

using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;
using System.Reflection.Runtime.CustomAttributes;

using Internal.Reflection.Core;
using Internal.Reflection.Core.Execution;

namespace System.Reflection.Runtime.ParameterInfos
{
    // This class is used for the "Get/Set" methods on array types.
    internal sealed partial class RuntimeSyntheticParameterInfo : RuntimeParameterInfo
    {
        private RuntimeSyntheticParameterInfo(MemberInfo memberInfo, int position, RuntimeTypeInfo parameterType)
            : base(memberInfo, position)
        {
            _parameterType = parameterType;
        }

        public sealed override ParameterAttributes Attributes
        {
            get
            {
                return ParameterAttributes.None;
            }
        }

        public sealed override IEnumerable<CustomAttributeData> CustomAttributes
        {
            get
            {
                return Array.Empty<CustomAttributeData>();
            }
        }

        public sealed override object DefaultValue
        {
            get
            {
                return null; // Legacy: This is what the desktop returns.
            }
        }

        public sealed override object RawDefaultValue
        {
            get
            {
                return null; // Legacy: This is what the desktop returns.
            }
        }

        public sealed override bool HasDefaultValue
        {
            get
            {
                // Compat: returning "true" makes no sense but this is how it's always been.
                return true;
            }
        }

        public sealed override Type[] GetOptionalCustomModifiers() => Array.Empty<Type>();

        public sealed override Type[] GetRequiredCustomModifiers() => Array.Empty<Type>();

        public sealed override string Name
        {
            get
            {
                return null; // Legacy: This is what the dekstop returns.
            }
        }

        public sealed override Type ParameterType
        {
            get
            {
                return _parameterType;
            }
        }

        public sealed override int MetadataToken
        {
            get
            {
                return 0x08000000; // nil ParamDef token
            }
        }

        private readonly RuntimeTypeInfo _parameterType;
    }
}
