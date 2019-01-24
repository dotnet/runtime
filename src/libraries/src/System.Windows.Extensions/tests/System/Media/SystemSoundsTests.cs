// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Media.Test
{
    public class SystemSoundsTests
    {
        [Fact]
        public void Asterisk_Get_ReturnsExpected()
        {
            SystemSound sound = SystemSounds.Asterisk;
            Assert.NotNull(sound);
            Assert.Same(sound, SystemSounds.Asterisk);
        }

        [Fact]
        public void Beep_Get_ReturnsExpected()
        {
            SystemSound sound = SystemSounds.Beep;
            Assert.NotNull(sound);
            Assert.Same(sound, SystemSounds.Beep);
        }

        [Fact]
        public void Exclamation_Get_ReturnsExpected()
        {
            SystemSound sound = SystemSounds.Exclamation;
            Assert.NotNull(sound);
            Assert.Same(sound, SystemSounds.Exclamation);
        }

        [Fact]
        public void Hand_Get_ReturnsExpected()
        {
            SystemSound sound = SystemSounds.Hand;
            Assert.NotNull(sound);
            Assert.Same(sound, SystemSounds.Hand);
        }

        [Fact]
        public void Question_Get_ReturnsExpected()
        {
            SystemSound sound = SystemSounds.Question;
            Assert.NotNull(sound);
            Assert.Same(sound, SystemSounds.Question);
        }
    }
}
