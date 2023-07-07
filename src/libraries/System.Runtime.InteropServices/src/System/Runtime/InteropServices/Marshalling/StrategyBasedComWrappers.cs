// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Reflection;

namespace System.Runtime.InteropServices.Marshalling
{
    [CLSCompliant(false)]
    public class StrategyBasedComWrappers : ComWrappers
    {
        internal static StrategyBasedComWrappers DefaultMarshallingInstance { get; } = new();

        public static IIUnknownInterfaceDetailsStrategy DefaultIUnknownInterfaceDetailsStrategy { get; } = Marshalling.DefaultIUnknownInterfaceDetailsStrategy.Instance;

        public static IIUnknownStrategy DefaultIUnknownStrategy { get; } = FreeThreadedStrategy.Instance;

        protected static IIUnknownCacheStrategy CreateDefaultCacheStrategy() => new DefaultCaching();

        protected virtual IIUnknownInterfaceDetailsStrategy GetOrCreateInterfaceDetailsStrategy() => DefaultIUnknownInterfaceDetailsStrategy;

        protected virtual IIUnknownStrategy GetOrCreateIUnknownStrategy() => DefaultIUnknownStrategy;

        protected virtual IIUnknownCacheStrategy CreateCacheStrategy() => CreateDefaultCacheStrategy();

        protected sealed override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
        {
            if (obj.GetType().GetCustomAttribute(typeof(ComExposedClassAttribute<>)) is IComExposedDetails details)
            {
                return details.GetComInterfaceEntries(out count);
            }
            count = 0;
            return null;
        }

        protected sealed override unsafe object CreateObject(nint externalComObject, CreateObjectFlags flags)
        {
            if (flags.HasFlag(CreateObjectFlags.TrackerObject)
                || flags.HasFlag(CreateObjectFlags.Aggregation))
            {
                throw new NotSupportedException();
            }

            var rcw = new ComObject(GetOrCreateInterfaceDetailsStrategy(), GetOrCreateIUnknownStrategy(), CreateCacheStrategy(), (void*)externalComObject)
            {
                UniqueInstance = flags.HasFlag(CreateObjectFlags.UniqueInstance)
            };

            return rcw;
        }

        protected sealed override void ReleaseObjects(IEnumerable objects)
        {
            throw new NotImplementedException();
        }
    }
}
