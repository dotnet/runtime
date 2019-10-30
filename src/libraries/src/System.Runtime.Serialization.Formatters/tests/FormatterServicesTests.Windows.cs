// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Runtime.Serialization.Formatters.Tests
{
    public partial class FormatterServicesTests
    {
        [Fact]
        public void GetUninitializedObject_COMObject_ThrowsNotSupportedException()
        {
            Type comObjectType = typeof(COMObject);
            Assert.True(comObjectType.IsCOMObject);

            Assert.Throws<NotSupportedException>(() => FormatterServices.GetUninitializedObject(typeof(COMObject)));
            Assert.Throws<NotSupportedException>(() => FormatterServices.GetSafeUninitializedObject(typeof(COMObject)));
        }

        [ComImport]
        [Guid("00000000-0000-0000-0000-000000000000")]
        public class COMObject { }
    }
}
