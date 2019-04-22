// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualBasic.Devices;
using Xunit;

namespace Microsoft.VisualBasic.MyServices.Tests
{
    public class SpecialDirectoriesProxyTests
    {
        [Fact]
        public void Properties()
        {
            SpecialDirectoriesProxy specialDirectories = new ServerComputer().FileSystem.SpecialDirectories;
            VerifySpecialDirectory(() => Microsoft.VisualBasic.FileIO.SpecialDirectories.AllUsersApplicationData, () => specialDirectories.AllUsersApplicationData);
            VerifySpecialDirectory(() => Microsoft.VisualBasic.FileIO.SpecialDirectories.CurrentUserApplicationData, () => specialDirectories.CurrentUserApplicationData);
            VerifySpecialDirectory(() => Microsoft.VisualBasic.FileIO.SpecialDirectories.Desktop, () => specialDirectories.Desktop);
            VerifySpecialDirectory(() => Microsoft.VisualBasic.FileIO.SpecialDirectories.MyDocuments, () => specialDirectories.MyDocuments);
            VerifySpecialDirectory(() => Microsoft.VisualBasic.FileIO.SpecialDirectories.MyMusic, () => specialDirectories.MyMusic);
            VerifySpecialDirectory(() => Microsoft.VisualBasic.FileIO.SpecialDirectories.MyPictures, () => specialDirectories.MyPictures);
            VerifySpecialDirectory(() => Microsoft.VisualBasic.FileIO.SpecialDirectories.Programs, () => specialDirectories.Programs);
            VerifySpecialDirectory(() => Microsoft.VisualBasic.FileIO.SpecialDirectories.ProgramFiles, () => specialDirectories.ProgramFiles);
            VerifySpecialDirectory(() => Microsoft.VisualBasic.FileIO.SpecialDirectories.Temp, () => specialDirectories.Temp);
        }

        private static void VerifySpecialDirectory(Func<string> getExpected, Func<string> getActual)
        {
            string expected;
            try
            {
                expected = getExpected();
            }
            catch (Exception)
            {
                return;
            }
            string actual = getActual();
            Assert.Equal(expected, actual);
        }
    }
}
