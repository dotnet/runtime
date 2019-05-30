// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Xunit;

namespace System.Runtime.InteropServices.Tests
{
    public class SetWin32ContextInIDispatchAttributeTests
    {
        private const string TypeName = "System.Runtime.InteropServices.SetWin32ContextInIDispatchAttribute";

        [Fact]
        public void Ctor_Default_ExistsInSrc()
        {
            Type type = typeof(HandleCollector).Assembly.GetType(TypeName);
            Assert.NotNull(type);

            ConstructorInfo constructor = type.GetConstructor(new Type[0]);
            object attribute = constructor.Invoke(new object[0]);
            Assert.NotNull(attribute);
        }
    }
}
