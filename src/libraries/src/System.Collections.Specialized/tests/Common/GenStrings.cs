// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Text;

namespace GenStrings
{
    public class IntlStrings
    {
        private const string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_ąĄęĘśŚćĆńŃżŻźŹóÓłŁ";
        private Random _rand = new Random();
        public String GetRandomString(int length)
        {
            StringBuilder sb = new StringBuilder();
            while (length-- > 0)
            {
                sb.Append(GetRandomLetter());
            }
            return sb.ToString();
        }

        private char GetRandomLetter()
        {
            Assert.NotEqual(0, alphabet.Length);
            return alphabet[_rand.Next(0, alphabet.Length)];
        }
    }
}
