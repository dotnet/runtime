// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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