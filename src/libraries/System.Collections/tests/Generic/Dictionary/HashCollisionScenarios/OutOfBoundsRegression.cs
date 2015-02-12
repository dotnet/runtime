// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace DictionaryTests
{
    public class InternalHashCodeTests
    {
        /// <summary>
        /// Given a byte array, copies it to the string, without messing with any encoding.  This issue was hit on a x64 machine
        /// </summary>
        private static string GetString(byte[] bytes)
        {
            var chars = new char[bytes.Length / sizeof(char)];
            Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        [Fact]
        public static void OutOfBoundsRegression()
        {
            var dictionary = new Dictionary<string, string>();

            foreach (var item in TestData.GetData())
            {
                var operation = item.Item1;
                var keyBase64 = item.Item2;

                var key = keyBase64.Length > 0 ? GetString(Convert.FromBase64String(keyBase64)) : string.Empty;

                if (operation == InputAction.Add)
                    dictionary[key] = key;
                else if (operation == InputAction.Delete)
                    dictionary.Remove(key);
            }
        }
    }
}