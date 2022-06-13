// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// Base class for all RoParameter's returned by MethodBase.GetParameters() that have an entry in the Param table.
    /// </summary>
    internal abstract class RoFatMethodParameter : RoMethodParameter
    {
        protected RoFatMethodParameter(IRoMethodBase roMethodBase, int position, Type parameterType)
            : base(roMethodBase, position, parameterType)
        {
            Debug.Assert(roMethodBase != null);
            Debug.Assert(parameterType != null);
        }

        public sealed override string? Name => _lazyName ??= ComputeName();
        protected abstract string? ComputeName();
        private volatile string? _lazyName;

        public sealed override ParameterAttributes Attributes => (_lazyParameterAttributes == ParameterAttributesSentinel) ? (_lazyParameterAttributes = ComputeAttributes()) : _lazyParameterAttributes;
        protected abstract ParameterAttributes ComputeAttributes();
        private const ParameterAttributes ParameterAttributesSentinel = (ParameterAttributes)(-1);
        private volatile ParameterAttributes _lazyParameterAttributes = ParameterAttributesSentinel;

        public sealed override IEnumerable<CustomAttributeData> CustomAttributes
        {
            get
            {
                foreach (CustomAttributeData cad in GetTrueCustomAttributes())
                    yield return cad;

                ParameterAttributes attributes = Attributes;
                if (0 != (attributes & ParameterAttributes.In))
                {
                    ConstructorInfo? ci = Loader.TryGetInCtor();
                    if (ci != null)
                        yield return new RoPseudoCustomAttributeData(ci);
                }

                if (0 != (attributes & ParameterAttributes.Out))
                {
                    ConstructorInfo? ci = Loader.TryGetOutCtor();
                    if (ci != null)
                        yield return new RoPseudoCustomAttributeData(ci);
                }

                if (0 != (attributes & ParameterAttributes.Optional))
                {
                    ConstructorInfo? ci = Loader.TryGetOptionalCtor();
                    if (ci != null)
                        yield return new RoPseudoCustomAttributeData(ci);
                }

                if (0 != (attributes & ParameterAttributes.HasFieldMarshal))
                {
                    CustomAttributeData? cad = CustomAttributeHelpers.TryComputeMarshalAsCustomAttributeData(ComputeMarshalAsAttribute, Loader);
                    if (cad != null)
                        yield return cad;
                }
            }
        }

        protected abstract MarshalAsAttribute ComputeMarshalAsAttribute();

        public abstract override bool HasDefaultValue { get; }
        public abstract override object? RawDefaultValue { get; }

        protected abstract IEnumerable<CustomAttributeData> GetTrueCustomAttributes();

        private MetadataLoadContext Loader => GetRoMethodBase().Loader;
    }
}
