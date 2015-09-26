// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text;
using System.IO;

namespace System
{
    /// <summary>Static helper class for performance tests</summary>
    public static class PerfUtils
    {
        private static Random rand = new Random(1234132);

        /// <summary>
        /// Helper method to create a string containing a number of random
        /// characters equal to the specified length
        /// </summary>
        public static string CreateString(int length)
        {
            // Random is not thread safe and will cause intermittent thread deadlock if used without being locked
            lock (rand)
            {
                byte[] bytes = new byte[length];
                rand.NextBytes(bytes);
                return System.Convert.ToBase64String(bytes);
            }
        }

        /// <summary>Gets a test file full path that is associated with the call site.</summary>
        /// <param name="index">An optional index value to use as a suffix on the file name.  Typically a loop index.</param>
        /// <param name="memberName">The member name of the function calling this method.</param>
        /// <param name="lineNumber">The line number of the function calling this method.</param>
        public static string GetTestFilePath(int? index = null, [CallerMemberName] string memberName = null, [CallerLineNumber] int lineNumber = 0)
        {
            return Path.Combine(Path.GetTempPath(), string.Format(
                index.HasValue ? "{0}_{1}_{2}_{3}" : "{0}_{1}_{2}",
                memberName ?? "TestBase", lineNumber, Path.GetRandomFileName(), 
                index.GetValueOrDefault()));
        }
    }
}