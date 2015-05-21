// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class UTF7EncodingEquals
    {
        // PosTest1: test whether two UTF7  objects constructed not using parameter are equal
        [Fact]
        public void PosTest1()
        {
            UTF7Encoding utf71 = new UTF7Encoding();
            UTF7Encoding utf72 = new UTF7Encoding();
            Assert.True(utf71.Equals(utf72));
        }

        // PosTest2: test whether two same UTF7 objects are equal
        [Fact]
        public void PosTest2()
        {
            UTF7Encoding utf71 = new UTF7Encoding();
            UTF7Encoding utf72 = new UTF7Encoding();
            utf71 = utf72;
            Assert.True(utf71.Equals(utf72));
        }

        // PosTest3: compare one UTF7 object to a null object are equal
        [Fact]
        public void PosTest3()
        {
            UTF7Encoding utf71 = new UTF7Encoding();
            Assert.False(utf71.Equals(null));
        }

        // PosTest4: compare two UTF7 objects constructed with true parameter.
        [Fact]
        public void PosTest4()
        {
            UTF7Encoding utf71 = new UTF7Encoding(true);
            UTF7Encoding utf72 = new UTF7Encoding(true);
            Assert.True(utf71.Equals(utf72));
        }

        // PosTest5: test two objects constructed by new UTF7Encoding(false).
        [Fact]
        public void PosTest5()
        {
            UTF7Encoding utf71 = new UTF7Encoding(false);
            UTF7Encoding utf72 = new UTF7Encoding(false);
            Assert.True(utf71.Equals(utf72));
        }

        // PosTest6: test UTF7Encoding(true) vs UTF7Encoding(false).
        [Fact]
        public void PosTest6()
        {
            UTF7Encoding utf71 = new UTF7Encoding(true);
            UTF7Encoding utf72 = new UTF7Encoding(false);
            Assert.False(utf71.Equals(utf72));
        }

        // PosTest7: test UTF7Encoding(true) vs UTF7Encoding().
        [Fact]
        public void PosTest7()
        {
            UTF7Encoding utf71 = new UTF7Encoding(true);
            UTF7Encoding utf72 = new UTF7Encoding();
            Assert.False(utf71.Equals(utf72));
        }

        // PosTest8: test UTF7Encoding(false) vs UTF7Encoding().
        [Fact]
        public void PosTest8()
        {
            UTF7Encoding utf71 = new UTF7Encoding(false);
            UTF7Encoding utf72 = new UTF7Encoding();
            Assert.True(utf71.Equals(utf72));
        }
    }
}
