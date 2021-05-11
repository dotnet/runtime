// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Xunit;

namespace System.ComponentModel.Tests
{
    [SimpleUpdateTest]
    [Collection("NoParallelTests")] // Clears the cache which disrupts concurrent tests
    public class ReflectionCachesUpdateHandlerTests
    {
        [Fact]
        public void ReflectionCachesUpdateHandler_CachesCleared()
        {
            AttributeCollection ac1 = TypeDescriptor.GetAttributes(typeof(ReflectionCachesUpdateHandlerTests));
            AttributeCollection ac2 = TypeDescriptor.GetAttributes(typeof(ReflectionCachesUpdateHandlerTests));
            Assert.Equal(ac1.Count, ac2.Count);
            Assert.Equal(2, ac1.Count);
            Assert.Same(ac1[0], ac2[0]);

            MethodInfo clearCache = typeof(TypeDescriptionProvider).Assembly.GetType("System.ComponentModel.ReflectionCachesUpdateHandler", throwOnError: true).GetMethod("ClearCache");
            Assert.NotNull(clearCache);
            clearCache.Invoke(null, new object[] { null });

            AttributeCollection ac3 = TypeDescriptor.GetAttributes(typeof(ReflectionCachesUpdateHandlerTests));
            Assert.NotSame(ac1[0], ac3[0]);
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    internal sealed class SimpleUpdateTestAttribute : Attribute { }
}
