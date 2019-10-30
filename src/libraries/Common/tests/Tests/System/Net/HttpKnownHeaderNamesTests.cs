// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;

using Xunit;

namespace Tests.System.Net
{
    public class HttpKnownHeaderNamesTests
    {
        [Theory]
        [MemberData(nameof(UnknownHeaderNames))]
        public void TryGetHeaderName_CharArray_UnknownStrings_NotFound(string shouldNotBeFound)
        {
            char[] array = shouldNotBeFound.ToCharArray();

            string name;
            Assert.False(HttpKnownHeaderNames.TryGetHeaderName(array, 0, array.Length, out name));
            Assert.Null(name);
        }

        [Theory]
        [MemberData(nameof(UnknownHeaderNames))]
        public unsafe void TryGetHeaderName_IntPtrBuffer_UnknownStrings_NotFound(string shouldNotBeFound)
        {
            byte[] buffer = shouldNotBeFound.Select(c => checked((byte)c)).ToArray();

            fixed (byte* pBuffer = buffer)
            {
                string name;
                Assert.False(HttpKnownHeaderNames.TryGetHeaderName(new IntPtr(pBuffer), buffer.Length, out name));
                Assert.Null(name);
            }
        }

        public static readonly object[][] UnknownHeaderNames =
        {
            new object[] { string.Empty },
            new object[] { "this should not be found" },
        };

        [Theory]
        [MemberData(nameof(HttpKnownHeaderNamesPublicStringConstants))]
        public void TryGetHeaderName_CharArray_AllHttpKnownHeaderNamesPublicStringConstants_Found(string constant)
        {
            char[] array = constant.ToCharArray();

            string name1;
            Assert.True(HttpKnownHeaderNames.TryGetHeaderName(array, 0, array.Length, out name1));
            Assert.NotNull(name1);
            Assert.Equal(constant, name1);

            string name2;
            Assert.True(HttpKnownHeaderNames.TryGetHeaderName(array, 0, array.Length, out name2));
            Assert.NotNull(name2);
            Assert.Equal(constant, name2);

            Assert.Same(name1, name2);
        }

        [Theory]
        [MemberData(nameof(HttpKnownHeaderNamesPublicStringConstants))]
        public unsafe void TryGetHeaderName_IntPtrBuffer_AllHttpKnownHeaderNamesPublicStringConstants_Found(string constant)
        {
            byte[] buffer = constant.Select(c => checked((byte)c)).ToArray();

            fixed (byte* pBuffer = buffer)
            {
                Assert.True(pBuffer != null);

                string name1;
                Assert.True(HttpKnownHeaderNames.TryGetHeaderName(new IntPtr(pBuffer), buffer.Length, out name1));
                Assert.NotNull(name1);
                Assert.Equal(constant, name1);

                string name2;
                Assert.True(HttpKnownHeaderNames.TryGetHeaderName(new IntPtr(pBuffer), buffer.Length, out name2));
                Assert.NotNull(name2);
                Assert.Equal(constant, name2);

                Assert.Same(name1, name2);
            }
        }

        public static IEnumerable<object[]> HttpKnownHeaderNamesPublicStringConstants
        {
            get
            {
                string[] constants = typeof(HttpKnownHeaderNames)
                    .GetTypeInfo()
                    .DeclaredFields
                    .Where(f => f.IsLiteral && f.IsStatic && f.IsPublic && f.FieldType == typeof(string))
                    .Select(f => (string)f.GetValue(null))
                    .ToArray();

                Assert.NotEmpty(constants);
                Assert.DoesNotContain(constants, c => string.IsNullOrEmpty(c));

                return constants.Select(c => new object[] { c });
            }
        }
    }
}
