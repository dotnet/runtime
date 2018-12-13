using System;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection.Specification
{
    public abstract class SkippableDependencyInjectionSpecificationTests: DependencyInjectionSpecificationTests
    {
        public abstract string[] SkippedTests { get; }


        protected sealed override IServiceProvider CreateServiceProvider(IServiceCollection serviceCollection)
        {
            foreach (var stackFrame in new StackTrace(1).GetFrames().Take(2))
            {
                if (SkippedTests.Contains(stackFrame.GetMethod().Name))
                {
                    // We skip tests by returning MEDI service provider that we know passes the test
                    return serviceCollection.BuildServiceProvider();
                }
            }

            return CreateServiceProviderImpl(serviceCollection);
        }

        protected abstract IServiceProvider CreateServiceProviderImpl(IServiceCollection serviceCollection);
    }
}
