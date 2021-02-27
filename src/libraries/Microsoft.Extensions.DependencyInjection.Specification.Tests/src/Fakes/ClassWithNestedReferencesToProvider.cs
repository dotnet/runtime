// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.DependencyInjection.Specification.Fakes
{
    public class ClassWithNestedReferencesToProvider : IDisposable
    {
        private IServiceProvider _serviceProvider;
        private ClassWithNestedReferencesToProvider _nested;

        public ClassWithNestedReferencesToProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _nested = new ClassWithNestedReferencesToProvider(_serviceProvider, 0);
        }

        private ClassWithNestedReferencesToProvider(IServiceProvider serviceProvider, int level)
        {
            _serviceProvider = serviceProvider;
            if (level > 1)
            {
                _nested = new ClassWithNestedReferencesToProvider(_serviceProvider, level + 1);
            }
        }

        public void Dispose()
        {
            _nested?.Dispose();
            (_serviceProvider as IDisposable)?.Dispose();
        }
    }
}
