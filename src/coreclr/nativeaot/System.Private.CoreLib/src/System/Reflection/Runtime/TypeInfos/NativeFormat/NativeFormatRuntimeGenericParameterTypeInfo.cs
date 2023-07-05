// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.MethodInfos;
using System.Reflection.Runtime.TypeInfos;
using System.Reflection.Runtime.CustomAttributes;

using Internal.Metadata.NativeFormat;

namespace System.Reflection.Runtime.TypeInfos.NativeFormat
{
    internal abstract partial class NativeFormatRuntimeGenericParameterTypeInfo : RuntimeGenericParameterTypeInfo
    {
        protected NativeFormatRuntimeGenericParameterTypeInfo(MetadataReader reader, GenericParameterHandle genericParameterHandle, GenericParameter genericParameter)
            : base(genericParameter.Number)
        {
            Reader = reader;
            GenericParameterHandle = genericParameterHandle;
            _genericParameter = genericParameter;
        }

        public sealed override IEnumerable<CustomAttributeData> CustomAttributes
        {
            get
            {
                return RuntimeCustomAttributeData.GetCustomAttributes(Reader, _genericParameter.CustomAttributes);
            }
        }

        public sealed override GenericParameterAttributes GenericParameterAttributes
        {
            get
            {
                return _genericParameter.Flags;
            }
        }

        public sealed override int MetadataToken
        {
            get
            {
                throw new InvalidOperationException(SR.NoMetadataTokenAvailable);
            }
        }

        protected sealed override int InternalGetHashCode()
        {
            return GenericParameterHandle.GetHashCode();
        }

        protected GenericParameterHandle GenericParameterHandle { get; }

        protected MetadataReader Reader { get; }

        public sealed override string Name
        {
            get
            {
                if (_genericParameter.Name.IsNull(Reader))
                    return string.Empty;
                return _genericParameter.Name.GetString(Reader);
            }
        }

        protected sealed override QTypeDefRefOrSpec[] Constraints
        {
            get
            {
                MetadataReader reader = Reader;
                LowLevelList<QTypeDefRefOrSpec> constraints = new LowLevelList<QTypeDefRefOrSpec>();
                foreach (Handle constraintHandle in _genericParameter.Constraints)
                {
                    // We're skipping custom modifiers here because Roslyn generates
                    // a modifier for the "unmanaged" constraint. This doesn't conform to the
                    // ECMA-335 spec, but we need to deal with it. The modifier is not visible
                    // to reflection.
                    constraints.Add(new QTypeDefRefOrSpec(reader, constraintHandle.SkipCustomModifiers(reader)));
                }
                return constraints.ToArray();
            }
        }

        private readonly GenericParameter _genericParameter;
    }
}
