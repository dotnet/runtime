// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Threading;
using System.Diagnostics;

namespace System.ComponentModel
{
    public static class AsyncOperationManager
    {
        public static AsyncOperation CreateOperation(object userSuppliedState)
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

                return SynchronizationContext.Current;
            }

            // a thread should set this to null  when it is done, else the context will never be disposed/GC'd
            set
            {
                SynchronizationContext.SetSynchronizationContext(value);
            }
        }
    }
}


