// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection.Runtime.General;

using Internal.Reflection.Core;

namespace System.Reflection.Runtime.ParameterInfos
{
    //
    // This implements ParameterInfo objects owned by MethodBase objects that have no associated Parameter metadata. (In practice,
    // this means return type "Parameters" that don't have custom attributes.
    //
    internal sealed partial class RuntimeThinMethodParameterInfo : RuntimeMethodParameterInfo
    {
        private RuntimeThinMethodParameterInfo(MethodBase member, int position, QSignatureTypeHandle qualifiedParameterTypeHandle, TypeContext typeContext)
            : base(member, position, qualifiedParameterTypeHandle, typeContext)
        {
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
                // Returning "null" matches the desktop behavior, though this is inconsistent with the DBNull/Missing values
                // returned by non-return ParameterInfo's without default values.
                return null;
            }
        }

        public sealed override object RawDefaultValue
        {
            get
            {
                // Returning "null" matches the desktop behavior, though this is inconsistent with the DBNull/Missing values
                // returned by non-return ParameterInfo's without default values.
                return null;
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

        public sealed override string Name
        {
            get
            {
                return null;
            }
        }

        public sealed override int MetadataToken
        {
            get
            {
                return 0x08000000; // nil ParamDef token
            }
        }
    }
}
