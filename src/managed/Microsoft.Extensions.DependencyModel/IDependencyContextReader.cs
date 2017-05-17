using System;
using System.IO;

namespace Microsoft.Extensions.DependencyModel
{
    public interface IDependencyContextReader: IDisposable
    {
        DependencyContext Read(Stream stream);
    }
}