// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Reflection;

namespace System.Runtime.InteropServices.Marshalling
{
    public class StrategyBasedComWrappers : ComWrappers
    {
        public static IIUnknownInterfaceDetailsStrategy DefaultIUnknownInterfaceDetailsStrategy { get; } = Marshalling.DefaultIUnknownInterfaceDetailsStrategy.Instance;

        public static IIUnknownStrategy DefaultIUnknownStrategy { get; } = FreeThreadedStrategy.Instance;

        protected static IIUnknownCacheStrategy CreateDefaultCacheStrategy() => new DefaultCaching();

        protected virtual IIUnknownInterfaceDetailsStrategy GetOrCreateInterfaceDetailsStrategy() => DefaultIUnknownInterfaceDetailsStrategy;

        protected virtual IIUnknownStrategy GetOrCreateIUnknownStrategy() => DefaultIUnknownStrategy;

        protected virtual IIUnknownCacheStrategy CreateCacheStrategy() => CreateDefaultCacheStrategy();

        protected override sealed unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
        {
            if (obj.GetType().GetCustomAttribute(typeof(ComExposedClassAttribute<>)) is IComExposedDetails details)
            {
                return details.GetComInterfaceEntries(out count);
            }
            count = 0;
            return null;
        }

        protected override sealed unsafe object CreateObject(nint externalComObject, CreateObjectFlags flags)
        {
            if (flags.HasFlag(CreateObjectFlags.TrackerObject)
                || flags.HasFlag(CreateObjectFlags.Aggregation))
            {
                throw new NotSupportedException();
            }

            var rcw = new ComObject(GetOrCreateInterfaceDetailsStrategy(), GetOrCreateIUnknownStrategy(), CreateCacheStrategy(), (void*)externalComObject);
            if (flags.HasFlag(CreateObjectFlags.UniqueInstance))
            {
                // Set value on MyComObject to enable the FinalRelease option.
                // This could also be achieved through an internal factory
                // function on ComObject type.
            }
            return rcw;
        }

        protected override sealed void ReleaseObjects(IEnumerable objects)
        {
            throw new NotImplementedException();
        }
    }
}
