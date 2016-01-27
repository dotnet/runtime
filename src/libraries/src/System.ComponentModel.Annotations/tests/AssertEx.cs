// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using Xunit;

namespace System.ComponentModel.DataAnnotations
{
    // Helpers to ease port from XUnit to CoreFXTestLibrary
    internal static class AssertEx
    {
        public static void DoesNotThrow(Action action)
        {
            action();
        }

        public static void Empty(IEnumerable collection)
        {
            Assert.False(collection.GetEnumerator().MoveNext());
        }

        public static void IsType<T>(object obj)
        {
            Assert.True(obj.GetType() == typeof(T));
        }
    }
}
