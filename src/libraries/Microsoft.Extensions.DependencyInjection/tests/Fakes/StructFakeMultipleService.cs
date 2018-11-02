using Microsoft.Extensions.DependencyInjection.Specification.Fakes;

namespace Microsoft.Extensions.DependencyInjection.Fakes
{
    public struct StructFakeMultipleService : IFakeMultipleService
    {
        public StructFakeMultipleService(IFakeService service, StructService direct)
        {
        }
    }
}
