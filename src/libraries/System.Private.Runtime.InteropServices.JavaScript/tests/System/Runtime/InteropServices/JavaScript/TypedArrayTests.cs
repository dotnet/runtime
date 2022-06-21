// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Runtime.InteropServices.JavaScript.Tests
{
    public static class TypedArrayTests
    {
        private static Function _objectPrototype;

        public static IEnumerable<object[]> Object_Prototype()
        {
            _objectPrototype ??= new Function("return Object.prototype.toString;");
            yield return new object[] { _objectPrototype.Call() };
        }

        [Theory]
        [MemberData(nameof(Object_Prototype))]
        public static void Uint8ArrayFrom(Function objectPrototype)
        {
            var array = new byte[50];
            Uint8Array from = Uint8Array.From(array);
            Assert.Equal(50, from.Length);
            Assert.Equal("[object Uint8Array]", objectPrototype.Call(from));
        }

        [Theory]
        [MemberData(nameof(Object_Prototype))]
        public static void Uint8ArrayFromArrayBuffer(Function objectPrototype)
        {
            Uint8Array from = new Uint8Array(new ArrayBuffer(50));
            Assert.True(from.Length == 50);
            Assert.Equal("[object Uint8Array]", objectPrototype.Call(from));
        }
    }
}
