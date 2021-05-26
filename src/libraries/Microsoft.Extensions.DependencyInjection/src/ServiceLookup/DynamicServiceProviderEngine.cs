// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace Microsoft.Extensions.DependencyInjection.ServiceLookup
{
    internal sealed class DynamicServiceProviderEngine : CompiledServiceProviderEngine
    {
        private readonly ServiceProvider _serviceProvider;

        public DynamicServiceProviderEngine(ServiceProvider serviceProvider): base(serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public override Func<ServiceProviderEngineScope, object> RealizeService(ServiceCallSite callSite)
        {
            int callCount = 0;
            return scope =>
            {
                // We want to directly use the callsite value if it's set and the scope is the root scope.
                // We've already called into the RuntimeResolver and pre-computed any singletons or root scope
                // Avoid the compilation for singletons (or promoted singletons)
                if (scope.IsRootScope && callSite.Value != null)
                {
                    return callSite.Value;
                }

                // Resolve the result before we increment the call count, this ensures that singletons
                // won't cause any side effects during the compilation of the resolve function.
                var result = CallSiteRuntimeResolver.Instance.Resolve(callSite, scope);

                if (Interlocked.Increment(ref callCount) == 2)
                {
                    // This second check is to avoid the race where we end up kicking off a background thread
                    // if multiple calls to GetService race and resolve the values for singletons before the initial check above.
                    if (scope.IsRootScope && callSite.Value != null)
                    {
                        return callSite.Value;
                    }

                    // Don't capture the ExecutionContext when forking to build the compiled version of the
                    // resolve function
                    _ = ThreadPool.UnsafeQueueUserWorkItem(_ =>
                    {
                        try
                        {
                            _serviceProvider.ReplaceServiceAccessor(callSite, base.RealizeService(callSite));
                        }
                        catch (Exception ex)
                        {
                            DependencyInjectionEventSource.Log.ServiceRealizationFailed(ex);
                        }
                    },
                    null);
                }

                return result;
            };
        }
    }
}
