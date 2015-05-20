// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    // GetString(System.Byte[],System.Int32,System.Int32)
    public class UTF8EncodingGetString
    {
        #region Positive Test Cases
        [Fact]
        public void PosTest1()
        {
            Byte[] bytes = new Byte[] {
                             85,  84,  70,  56,  32,  69, 110,
                             99, 111, 100, 105, 110, 103,  32,
                             69, 120,  97, 109, 112, 108, 101};

            UTF8Encoding utf8 = new UTF8Encoding();
            string str = utf8.GetString(bytes, 0, bytes.Length);
        }
        #endregion

        #region Negative Test Cases
        // NegTest1: ArgumentNullException is not thrown when bytes is a null reference
        [Fact]
        public void NegTest1()
        {
            Byte[] bytes = null;

            UTF8Encoding utf8 = new UTF8Encoding();
            Assert.Throws<ArgumentNullException>(() =>
            {
                string str = utf8.GetString(bytes, 0, 2);
            });
        }

        // NegTest2: ArgumentOutOfRangeException is not thrown when index is less than zero
        [Fact]
        public void NegTest2()
        {
            Byte[] bytes = new Byte[] {
                             85,  84,  70,  56,  32,  69, 110,
                             99, 111, 100, 105, 110, 103,  32,
                             69, 120,  97, 109, 112, 108, 101};

            UTF8Encoding utf8 = new UTF8Encoding();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                string str = utf8.GetString(bytes, -1, bytes.Length);
            });
        }

        // NegTest3: ArgumentOutOfRangeException is not thrown when count is less than zero
        [Fact]
        public void NegTest3()
        {
            Byte[] bytes = new Byte[] {
                             85,  84,  70,  56,  32,  69, 110,
                             99, 111, 100, 105, 110, 103,  32,
                             69, 120,  97, 109, 112, 108, 101};

            UTF8Encoding utf8 = new UTF8Encoding();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                string str = utf8.GetString(bytes, 0, -1);
            });
        }

        // NegTest4: ArgumentOutOfRangeException is not thrown when index and count do not denote a valid range in bytes
        [Fact]
        public void NegTest4()
        {
            Byte[] bytes = new Byte[] {
                             85,  84,  70,  56,  32,  69, 110,
                             99, 111, 100, 105, 110, 103,  32,
                             69, 120,  97, 109, 112, 108, 101};

            UTF8Encoding utf8 = new UTF8Encoding();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                string str = utf8.GetString(bytes, 1, bytes.Length);
            });
        }
        #endregion
    }
}
