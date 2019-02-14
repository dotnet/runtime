// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Media.Test
{
    [OuterLoop]
    public class SystemSoundTests
    {
        public static IEnumerable<object[]> Play_TestData()
        {
            yield return new object[] { SystemSounds.Asterisk };
            yield return new object[] { SystemSounds.Beep };
            yield return new object[] { SystemSounds.Exclamation };
            yield return new object[] { SystemSounds.Hand };
            yield return new object[] { SystemSounds.Question };
        }

        [Theory]
        [MemberData(nameof(Play_TestData))]
        public void Question_Play_Success(SystemSound sound)
        {
            sound.Play();
        }
    }
}
