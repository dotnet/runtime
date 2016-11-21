// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Threading
{
    public class HostExecutionContext : IDisposable
    {
        public HostExecutionContext()
        {
        }

        public HostExecutionContext(object state)
        {
            State = state;
        }

        protected internal object State
        {
            get;
            set;
        }

        public virtual HostExecutionContext CreateCopy()
        {
            return new HostExecutionContext(State);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public virtual void Dispose(bool disposing)
        {
        }
    }
}
