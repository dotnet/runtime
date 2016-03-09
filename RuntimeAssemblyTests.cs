using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Microsoft.Extensions.DependencyModel.Tests
{
    public class RuntimeAssemblyTests
    {
        [Fact]
        public void UsesFileNameAsAssemblyNameInCreate()
        {
            var assembly = RuntimeAssembly.Create("path/to/System.Collections.dll");
            assembly.Name.Name.Should().Be("System.Collections");
        }

        [Fact]
        public void TrimsDotNiFromDllNames()
        {
            var assembly = RuntimeAssembly.Create("path/to/System.Collections.ni.dll");
            assembly.Name.Name.Should().Be("System.Collections");
        }
    }
}
