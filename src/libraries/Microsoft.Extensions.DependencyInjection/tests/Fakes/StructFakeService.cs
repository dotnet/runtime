using System;
using Microsoft.Extensions.DependencyInjection.Specification.Fakes;

namespace Microsoft.Extensions.DependencyInjection.Fakes
{
    public struct StructFakeService : IFakeService
    {
        public StructFakeService(IServiceProvider serviceProvider)
        {
        }
    }
}