// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using Xunit;

namespace System.UnitTesting
{
    public static class ExportsAssert
    {
        public static void AreEqual<T>(IEnumerable<Export> actual, params T[] expected)
        {
            Assert.Equal((IEnumerable)expected, (IEnumerable)actual.Select(export => (T)export.Value).ToArray());
        }

        public static void AreEqual<T>(IEnumerable<Lazy<T>> actual, params T[] expected)
        {
            Assert.Equal((IEnumerable<T>)expected, (IEnumerable<T>)actual.Select(export => export.Value));
        }

        public static void AreEqual<T, TMetadataView>(IEnumerable<Lazy<T, TMetadataView>> actual, params T[] expected)
        {
            Assert.Equal((IEnumerable<T>)expected, (IEnumerable<T>)actual.Select(export => export.Value));
        }
    }
}
