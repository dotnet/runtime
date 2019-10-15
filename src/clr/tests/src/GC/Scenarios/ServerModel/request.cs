// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace ServerSimulator
{
    /// <summary>
    /// This class models a typical server request
    /// </summary>
    internal class Request
    {
        private Object[] survivors;
        private GCHandle pin;

        public Request()
        {
            survivors = new Object[1 + (int)(ServerSimulator.Params.AllocationVolume * ServerSimulator.Params.SurvivalRate) / 100];
            int index = 0;
            int volume = 0;

            // allocate half of the request size.
            while (volume < (int)(ServerSimulator.Params.AllocationVolume / 2))
            {
                volume += allocateRequest(index++);
            }

            // allocate one pinned buffer
            if (ServerSimulator.Params.Pinning)
            {
                pin = GCHandle.Alloc(new byte[100], GCHandleType.Pinned);
            }

            // allocate the rest of the request
            while (volume < ServerSimulator.Params.AllocationVolume)
            {
                volume += allocateRequest(index++);
            }

        }

        // allocates the request along with garbage to simulate work on the server side
        protected int allocateRequest(int index)
        {
            int alloc_surv = ServerSimulator.Rand.Next(100, 2000 + 2 * index);
            int alloc = (int)(alloc_surv / ServerSimulator.Params.SurvivalRate) - alloc_surv;

            // create garbage
            int j = 0;
            while (j < alloc)
            {
                int s = ServerSimulator.Rand.Next(10, 200 + 2 * j);
                byte[] garbage = new byte[s];
                j += s;
            }

            survivors[index] = new byte[alloc_surv];
            return alloc_surv + alloc;
        }

        // deallocates the request
        public void Retire()
        {
            if (pin.IsAllocated)
            {
                pin.Free();
            }
        }

    }


    /// <summary>
    /// This class is a finalizable version of Request that allocates inside its finalizer
    /// </summary>

    internal sealed class FinalizableRequest : Request
    {
// disabling unused variable warning
#pragma warning disable 0414
        private byte[] finalizedData = null;
#pragma warning restore 0414

        public FinalizableRequest() : base()
        {
        }

        ~FinalizableRequest()
        {
            finalizedData = new byte[ServerSimulator.Params.AllocationVolume];
        }
    }
}
