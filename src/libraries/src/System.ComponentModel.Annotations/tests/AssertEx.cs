// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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