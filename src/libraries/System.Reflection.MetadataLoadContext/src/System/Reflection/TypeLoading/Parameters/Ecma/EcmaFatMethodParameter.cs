// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace System.Reflection.TypeLoading.Ecma
{
    /// <summary>
    /// Base class for all RoParameter's returned by MethodBase.GetParameters() that have an entry in the Param table
    /// and get their metadata from a PEReader.
    /// </summary>
    internal sealed class EcmaFatMethodParameter : RoFatMethodParameter
    {
        private readonly EcmaModule _module;
        private readonly ParameterHandle _handle;

        internal EcmaFatMethodParameter(IRoMethodBase roMethodBase, int position, Type parameterType, ParameterHandle handle) 
            : base(roMethodBase, position, parameterType)
        {
            Debug.Assert(roMethodBase != null);
            Debug.Assert(parameterType != null);
            Debug.Assert(!handle.IsNil);

            _handle = handle;
            Debug.Assert(roMethodBase.MethodBase.Module is EcmaModule);
            _module = (EcmaModule)(roMethodBase.MethodBase.Module);
            _neverAccessThisExceptThroughParameterProperty = handle.GetParameter(Reader);
        }

        public sealed override int MetadataToken => _handle.GetToken();

        protected sealed override string ComputeName() => Parameter.Name.GetStringOrNull(Reader);
        protected sealed override ParameterAttributes ComputeAttributes() => Parameter.Attributes;

        protected sealed override IEnumerable<CustomAttributeData> GetTrueCustomAttributes() => Parameter.GetCustomAttributes().ToTrueCustomAttributes(GetEcmaModule());

        public sealed override bool HasDefaultValue => TryGetRawDefaultValue(out object _);

        public sealed override object RawDefaultValue
        {
            get
            {
                if (TryGetRawDefaultValue(out object rawDefaultValue))
                    return rawDefaultValue;
                return IsOptional ? (object)Missing.Value : (object)DBNull.Value;
            }
        }

        private bool TryGetRawDefaultValue(out object rawDefaultValue)
        {
            rawDefaultValue = null;
            MetadataReader reader = Reader;
            ConstantHandle ch = Parameter.GetDefaultValue();
            if (!ch.IsNil)
            {
                rawDefaultValue = ch.ToRawObject(reader);
                return true;
            }

            return Parameter.GetCustomAttributes().TryFindRawDefaultValueFromCustomAttributes(GetEcmaModule(), out rawDefaultValue);
        }

        protected sealed override MarshalAsAttribute ComputeMarshalAsAttribute() => Parameter.GetMarshallingDescriptor().ToMarshalAsAttribute(GetEcmaModule());

        private EcmaModule GetEcmaModule() => _module;
        private MetadataReader Reader => GetEcmaModule().Reader;
        private MetadataLoadContext Loader => GetEcmaModule().Loader;

        private ref readonly Parameter Parameter { get { Loader.DisposeCheck(); return ref _neverAccessThisExceptThroughParameterProperty; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]  // Block from debugger watch windows so they don't AV the debugged process.
        private readonly Parameter _neverAccessThisExceptThroughParameterProperty;
    }
}
