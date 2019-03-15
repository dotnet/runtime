// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using Xunit;

namespace System.Reflection.Tests
{
    public class StrongNameKeyPairTests : FileCleanupTestBase
    {
        public static IEnumerable<object[]> Ctor_ByteArray_TestData()
        {
            yield return new object[] { new byte[] { 7, 2, 0, 0, 0, 36, 0, 0, 82, 83, 65, 50, 0, 4, 0, 0, 1, 0, 1, 0, 171, 157, 248, 5, 104, 59, 240, 8, 24, 167, 113, 157, 119, 211, 91, 102, 217, 109, 111, 113, 99, 55, 124, 119, 90, 1, 114, 103, 221, 223, 66, 248, 122, 130, 237, 95, 97, 174, 90, 116, 24, 53, 75, 217, 196, 170, 198, 196, 147, 194, 154, 172, 143, 83, 255, 238, 84, 162, 155, 253, 219, 180, 172, 10, 206, 240, 74, 167, 54, 131, 226, 11, 23, 254, 104, 61, 231, 57, 156, 80, 118, 153, 104, 208, 106, 181, 242, 84, 178, 128, 13, 223, 98, 252, 228, 26, 176, 61, 114, 79, 166, 165, 172, 203, 91, 220, 33, 131, 250, 178, 137, 212, 59, 109, 223, 195, 132, 24, 67, 253, 88, 186, 113, 102, 229, 237, 208, 168, 5, 232, 20, 9, 220, 14, 22, 65, 4, 89, 164, 97, 7, 242, 146, 235, 181, 141, 106, 16, 183, 91, 95, 208, 86, 186, 53, 169, 111, 179, 42, 98, 42, 110, 132, 235, 154, 164, 85, 179, 87, 51, 15, 43, 202, 189, 148, 224, 204, 229, 61, 23, 52, 199, 131, 72, 27, 97, 28, 207, 249, 135, 44, 219, 239, 205, 136, 79, 205, 158, 195, 251, 127, 181, 83, 202, 165, 68, 33, 36, 226, 227, 27, 199, 84, 150, 200, 15, 104, 222, 174, 182, 159, 17, 249, 23, 27, 93, 50, 242, 63, 244, 132, 102, 183, 186, 214, 166, 121, 62, 197, 54, 118, 230, 181, 10, 21, 66, 174, 110, 113, 62, 100, 190, 58, 83, 46, 197, 41, 7, 161, 146, 178, 153, 173, 10, 187, 86, 171, 10, 62, 98, 100, 252, 18, 36, 160, 122, 241, 10, 3, 199, 83, 89, 163, 213, 100, 48, 198, 203, 36, 108, 175, 47, 111, 18, 171, 169, 238, 152, 247, 153, 88, 105, 35, 156, 83, 249, 43, 16, 23, 148, 245, 237, 26, 214, 250, 137, 10, 47, 124, 162, 21, 66, 110, 45, 200, 18, 60, 195, 221, 155, 179, 39, 160, 196, 172, 253, 81, 247, 186, 115, 129, 146, 148, 245, 238, 101, 91, 196, 133, 252, 246, 119, 170, 155, 191, 36, 144, 62, 9, 65, 119, 212, 194, 212, 159, 169, 192, 136, 17, 91, 63, 238, 203, 140, 104, 167, 156, 39, 186, 189, 125, 202, 102, 88, 64, 171, 173, 38, 41, 125, 164, 91, 174, 4, 104, 148, 6, 205, 237, 115, 115, 254, 85, 62, 203, 233, 213, 83, 44, 132, 156, 239, 205, 149, 138, 250, 11, 183, 173, 190, 225, 204, 145, 98, 194, 121, 232, 155, 194, 170, 219, 147, 216, 173, 250, 236, 44, 122, 126, 252, 126, 75, 115, 254, 103, 106, 51, 102, 141, 150, 115, 47, 177, 46, 189, 114, 242, 250, 114, 82, 23, 102, 113, 119, 197, 115, 67, 111, 59, 71, 39, 160, 166, 135, 136, 17, 247, 3, 227, 205, 27, 42, 86, 173, 40, 65, 3, 127, 63, 112, 204, 19, 247, 243, 74, 104, 120, 194, 51, 238, 235, 6, 46, 62, 129, 214, 201, 170, 65, 254, 78, 201, 76, 78, 219, 235, 170, 196, 112, 87, 64, 130, 178, 109, 114, 221, 222, 92, 138, 201, 236, 20, 167, 240, 23, 108, 106, 63, 171, 74, 227, 158, 62, 3, 224, 197, 110, 218, 215, 210, 63, 90, 154, 29, 188, 183, 89, 18, 143, 170, 69, 53, 80, 44, 214, 169, 9, 68, 174, 238, 102, 199, 178, 251, 17, 15 } };
            yield return new object[] { new byte[0] };
        }

        [Theory]
        [MemberData(nameof(Ctor_ByteArray_TestData))]
        public void Ctor_ByteArray(byte[] keyPairArray)
        {
            // Can't validate anything as this is a nop in .NET Core.
            // Just make sure it doesn't throw.
            var keyPair = new StrongNameKeyPair(keyPairArray);
        }

        [Fact]
        public void Ctor_NullKeyPairArray_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("keyPairArray", () => new StrongNameKeyPair((byte[])null));
        }

        [Theory]
        [MemberData(nameof(Ctor_ByteArray_TestData))]
        public void Ctor_FileStream(byte[] keyPairArray)
        {
            string tempPath = GetTestFilePath();
            File.WriteAllBytes(tempPath, keyPairArray);
            using (FileStream fileStream = File.OpenRead(tempPath))
            {
                // Can't validate anything as this is a nop in .NET Core.
                // Just make sure it doesn't throw.
                var keyPair = new StrongNameKeyPair(fileStream);
            }
        }

        [Fact]
        public void Ctor_NullKeyPairFile_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("keyPairFile", () => new StrongNameKeyPair((FileStream)null));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("keyPairContainer")]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Constructor is not supported in .NET Core")]
        public void Ctor_String_ThrowsPlatformNotSupportedException(string keyPairContainer)
        {
            Assert.Throws<PlatformNotSupportedException>(() => new StrongNameKeyPair(keyPairContainer));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Constructor is not supported in .NET Core")]
        public void Ctor_SerializationInfo_StreamingContext_ThrowsPlatformNotSupportedException()
        {
            Assert.Throws<PlatformNotSupportedException>(() => new SubStrongNameKeyPair(null, new StreamingContext()));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "PublicKey is not supported in .NET Core")]
        public void PublicKey_Get_ThrowsPlatformNotSupportedException()
        {
            var keyPair = new StrongNameKeyPair(new byte[0]);
            Assert.Throws<PlatformNotSupportedException>(() => keyPair.PublicKey);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "OnDeserialization is not supported in .NET Core")]
        public void GetObjectData_Invoke_ThrowsPlatformNotSupportedException()
        {
            ISerializable keyPair = new StrongNameKeyPair(new byte[0]);
            Assert.Throws<PlatformNotSupportedException>(() => keyPair.GetObjectData(null, new StreamingContext()));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "OnDeserialization is not supported in .NET Core")]
        public void OnDeserialization_Invoke_ThrowsPlatformNotSupportedException()
        {
            IDeserializationCallback keyPair = new StrongNameKeyPair(new byte[0]);
            Assert.Throws<PlatformNotSupportedException>(() => keyPair.OnDeserialization(null));
        }

        private class SubStrongNameKeyPair : StrongNameKeyPair
        {
            public SubStrongNameKeyPair(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        }
    }
}
