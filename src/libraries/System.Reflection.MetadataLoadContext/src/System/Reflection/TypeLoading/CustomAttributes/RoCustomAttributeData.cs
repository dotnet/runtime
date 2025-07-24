// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// Base class for all CustomAttributeData objects created by a MetadataLoadContext.
    /// </summary>
    internal abstract partial class RoCustomAttributeData : LeveledCustomAttributeData
    {
        protected RoCustomAttributeData() { }

        public sealed override Type AttributeType => field ??= ComputeAttributeType()!;
        protected abstract Type? ComputeAttributeType();

        public sealed override ConstructorInfo Constructor => field ??= ComputeConstructor();
        protected abstract ConstructorInfo ComputeConstructor();

        public abstract override IList<CustomAttributeTypedArgument> ConstructorArguments { get; }
        public abstract override IList<CustomAttributeNamedArgument> NamedArguments { get; }
        public sealed override string ToString() => GetType().ToString();  // Does not match .NET Framework output - however, doing so can prematurely
                                                                           // trigger resolve handlers. Too impactful for ToString().
    }
}
