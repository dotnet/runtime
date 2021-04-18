// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal sealed class ConstantCallSite : ServiceCallSite
    {
        private readonly Type _serviceType;
        internal object DefaultValue { get; }

        public ConstantCallSite(Type serviceType, object defaultValue): base(ResultCache.None)
        {
            _serviceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
            if (defaultValue != null && !serviceType.IsInstanceOfType(defaultValue))
            {
                throw new ArgumentException(SR.Format(SR.ConstantCantBeConvertedToServiceType, defaultValue.GetType(), serviceType));
            }

            DefaultValue = defaultValue;
        }

        public override Type ServiceType => DefaultValue?.GetType() ?? _serviceType;
        public override Type ImplementationType => DefaultValue?.GetType() ?? _serviceType;
        public override CallSiteKind Kind { get; } = CallSiteKind.Constant;
    }
}
