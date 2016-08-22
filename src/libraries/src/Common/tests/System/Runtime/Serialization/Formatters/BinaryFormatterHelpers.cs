// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;

namespace System.Runtime.Serialization.Formatters.Tests
{
    internal static class BinaryFormatterHelpers
    {
        internal static T Clone<T>(T obj)
        {
            var f = new BinaryFormatter();
            using (var s = new MemoryStream())
            {
                f.Serialize(s, obj);
                s.Position = 0;
                return (T)f.Deserialize(s);
            }
        }

        internal static void AssertRoundtrips(Exception expected, params Func<Exception, object>[] additionalGetters)
        {
            for (int i = 0; i < 2; i++)
            {
                if (i > 0) // first time without stack trace, second time with
                {
                    try { throw expected; }
                    catch { }
                }

                // Serialize/deserialize the exception
                Exception actual = Clone(expected);

                // Verify core state
                Assert.Equal(expected.StackTrace, actual.StackTrace);
                Assert.Equal(expected.Data, actual.Data);
                Assert.Equal(expected.Message, actual.Message);
                Assert.Equal(expected.Source, actual.Source);
                Assert.Equal(expected.ToString(), actual.ToString());
                Assert.Equal(expected.HResult, actual.HResult);

                // Verify optional additional state
                foreach (Func<Exception, object> getter in additionalGetters)
                {
                    Assert.Equal(getter(expected), getter(actual));
                }
            }
        }
    }
}
