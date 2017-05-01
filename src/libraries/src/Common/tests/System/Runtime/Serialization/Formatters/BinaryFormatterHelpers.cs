// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

using Xunit;

namespace System.Runtime.Serialization.Formatters.Tests
{
	public static class BinaryFormatterHelpers
	{
		internal static T Clone<T>(T obj)
		{
            // https://github.com/dotnet/corefx/issues/18942 - Binary serialization still WIP on AOT platforms.
            if (RuntimeInformation.FrameworkDescription.StartsWith(".NET Native"))
                return obj;

			var f = new BinaryFormatter();
			using (var s = new MemoryStream())
			{
				f.Serialize(s, obj);
				s.Position = 0;
				return (T)f.Deserialize(s);
			}
		}

        internal static Lazy<T> Clone<T>(Lazy<T> lazy)
        {
            // https://github.com/dotnet/corefx/issues/18942 - Binary serialization still WIP on AOT platforms.
            if (RuntimeInformation.FrameworkDescription.StartsWith(".NET Native"))
            {
                T ignore = lazy.Value;
                return lazy;
            }

            return Clone<Lazy<T>>(lazy);
        }

        public static void AssertRoundtrips<T>(T expected, params Func<T, object>[] additionalGetters)
			where T : Exception
		{
			for (int i = 0; i < 2; i++)
			{
				if (i > 0) // first time without stack trace, second time with
				{
					try { throw expected; }
					catch { }
				}

				// Serialize/deserialize the exception
				T actual = Clone(expected);

				// Verify core state
				if (!PlatformDetection.IsFullFramework) // On full framework, line number is method body start
				{
				    Assert.Equal(expected.StackTrace, actual.StackTrace);
				}
				Assert.Equal(expected.Data, actual.Data);
				Assert.Equal(expected.Message, actual.Message);
				Assert.Equal(expected.Source, actual.Source);
				Assert.Equal(expected.ToString(), actual.ToString());
				Assert.Equal(expected.HResult, actual.HResult);

				// Verify optional additional state
				foreach (Func<T, object> getter in additionalGetters)
				{
					Assert.Equal(getter(expected), getter(actual));
				}
			}
		}
	}
}
