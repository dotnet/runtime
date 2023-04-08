// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.Marshalling
{
    public abstract class NonGeneratedStrategyBasedComWrappers : ComWrappers
    {
        public static IIUnknownInterfaceDetailsStrategy DefaultIUnknownInterfaceDetailsStrategy { get; } = Marshalling.DefaultIUnknownInterfaceDetailsStrategy.Instance;

        public static IIUnknownStrategy DefaultIUnknownStrategy { get; } = FreeThreadedStrategy.Instance;

        protected static IIUnknownCacheStrategy CreateDefaultCacheStrategy() => new DefaultCaching();

        protected virtual IIUnknownInterfaceDetailsStrategy GetOrCreateInterfaceDetailsStrategy() => DefaultIUnknownInterfaceDetailsStrategy;

        protected virtual IIUnknownStrategy GetOrCreateIUnknownStrategy() => DefaultIUnknownStrategy;

        protected virtual IIUnknownCacheStrategy CreateCacheStrategy() => CreateDefaultCacheStrategy();


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
