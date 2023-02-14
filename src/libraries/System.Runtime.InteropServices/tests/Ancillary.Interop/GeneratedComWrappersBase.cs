// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This type is for the COM source generator and implements part of the COM-specific interactions.
// This API need to be exposed to implement the COM source generator in one form or another.

using System.Collections;

namespace System.Runtime.InteropServices.Marshalling
{
    public abstract class GeneratedComWrappersBase : ComWrappers
    {
        protected virtual IIUnknownInterfaceDetailsStrategy CreateInterfaceDetailsStrategy() => DefaultIUnknownInterfaceDetailsStrategy.Instance;

        protected virtual IIUnknownStrategy CreateIUnknownStrategy() => FreeThreadedStrategy.Instance;

        protected virtual IIUnknownCacheStrategy CreateCacheStrategy() => new DefaultCaching();

        protected override sealed unsafe object CreateObject(nint externalComObject, CreateObjectFlags flags)
        {
            if (flags.HasFlag(CreateObjectFlags.TrackerObject)
                || flags.HasFlag(CreateObjectFlags.Aggregation))
            {
                throw new NotSupportedException();
            }

            var rcw = new ComObject(CreateInterfaceDetailsStrategy(), CreateIUnknownStrategy(), CreateCacheStrategy(), (void*)externalComObject);
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

        public ComObject GetOrCreateUniqueObjectForComInstance(nint comInstance, CreateObjectFlags flags)
        {
            if (flags.HasFlag(CreateObjectFlags.Unwrap))
            {
                throw new ArgumentException("Cannot create a unique object if unwrapping a ComWrappers-based COM object is requested.", nameof(flags));
            }
            return (ComObject)GetOrCreateObjectForComInstance(comInstance, flags | CreateObjectFlags.UniqueInstance);
        }
    }
}
