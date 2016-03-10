using System.IO;

namespace Microsoft.Extensions.DependencyModel
{
    public interface IDependencyContextReader
    {
        DependencyContext Read(Stream stream);
    }
}