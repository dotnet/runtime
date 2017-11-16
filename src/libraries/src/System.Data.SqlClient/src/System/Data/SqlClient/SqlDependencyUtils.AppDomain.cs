// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Data.SqlClient
{
    // these members were moved to a separate file in order
    // to be able to skip them on platforms where AppDomain members are not supported 
    // for example, some mobile profiles on mono
    partial class SqlDependencyPerAppDomainDispatcher
    {
        private void SubscribeToAppDomainUnload()
        {
            // If rude abort - we'll leak.  This is acceptable for now.  
            AppDomain.CurrentDomain.DomainUnload += new EventHandler(UnloadEventHandler);
        }

        private void UnloadEventHandler(object sender, EventArgs e)
        {
            // Make non-blocking call to ProcessDispatcher to ThreadPool.QueueUserWorkItem to complete 
            // stopping of all start calls in this AppDomain.  For containers shared among various AppDomains,
            // this will just be a ref-count subtract.  For non-shared containers, we will close the container
            // and clean-up.
            var dispatcher = SqlDependency.ProcessDispatcher;
            dispatcher?.QueueAppDomainUnloading(SqlDependency.AppDomainKey);
        }
    }
}
