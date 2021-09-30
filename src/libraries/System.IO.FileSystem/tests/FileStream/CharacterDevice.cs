// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    [PlatformSpecific(TestPlatforms.AnyUnix)]
    public class CharacterDevice
    {
        const string CharacterDevicePath = "/dev/tty";
        [Theory]
        [InlineData(FileOptions.None)]
        [InlineData(FileOptions.Asynchronous)]
        public void CharacterDevice_Write(FileOptions fileOptions)
        {
            using FileStream fs = new(CharacterDevicePath, new FileStreamOptions { Options = fileOptions, Access = FileAccess.Write });
            fs.Write(Encoding.UTF8.GetBytes("foo"));
        }

        [Theory]
        [InlineData(FileOptions.None)]
        [InlineData(FileOptions.Asynchronous)]
        public async Task CharacterDevice_WriteAsync(FileOptions fileOptions)
        {
            using FileStream fs = new(CharacterDevicePath, new FileStreamOptions { Options = fileOptions, Access = FileAccess.Write  });
            await fs.WriteAsync(Encoding.UTF8.GetBytes("foo"));
        }
    }
}
