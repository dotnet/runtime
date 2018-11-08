// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// Base class for all CustomAttributeData objects created by a MetadataLoadContext.
    /// </summary>
    internal abstract partial class RoCustomAttributeData : LeveledCustomAttributeData
    {
        protected RoCustomAttributeData() { }

        public sealed override Type AttributeType => _lazyAttributeType ?? (_lazyAttributeType = ComputeAttributeType());
        protected abstract Type ComputeAttributeType();
        private volatile Type _lazyAttributeType;

        public sealed override ConstructorInfo Constructor => _lazyConstructorInfo ?? (_lazyConstructorInfo = ComputeConstructor());
        protected abstract ConstructorInfo ComputeConstructor();
        private volatile ConstructorInfo _lazyConstructorInfo;

        public abstract override IList<CustomAttributeTypedArgument> ConstructorArguments { get; }
        public abstract override IList<CustomAttributeNamedArgument> NamedArguments { get;}
        public sealed override string ToString() => GetType().ToString();  // Does not match NETFX output - however, doing so can prematurely
                                                                           // trigger resolve handlers. Too impactful for ToString().
    }
}
