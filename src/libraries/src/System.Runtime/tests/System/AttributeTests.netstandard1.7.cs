// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Tests
{
    public static partial class AttributeTests
    {
        [Fact]
        public static void ValidateDefaults()
        {
            StringValueAttribute sav =  new StringValueAttribute("test");
            Assert.Equal(false, sav.IsDefaultAttribute());
            Assert.Equal(sav.GetType(), sav.TypeId);
            Assert.Equal(true, sav.Match(sav));
        }
    }
}
