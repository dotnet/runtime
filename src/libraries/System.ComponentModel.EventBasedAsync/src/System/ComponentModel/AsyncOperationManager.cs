// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Threading;

namespace System.ComponentModel
{
    public static class AsyncOperationManager
    {
        public static AsyncOperation CreateOperation(object? userSuppliedState)
        {
            return AsyncOperation.CreateOperation(userSuppliedState, SynchronizationContext);
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static SynchronizationContext SynchronizationContext
        {
            get
            {
                if (SynchronizationContext.Current == null)
                {
                    SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
                }

                return SynchronizationContext.Current!;
            }

            // a thread should set this to null  when it is done, else the context will never be disposed/GC'd
            set
            {
                SynchronizationContext.SetSynchronizationContext(value);
            }
        }
    }
}
