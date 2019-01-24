// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;

namespace System.Media.Test
{
    public class SoundPlayerTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var player = new SoundPlayer();
            Assert.Null(player.Container);
            Assert.False(player.IsLoadCompleted);
            Assert.Equal(10000, player.LoadTimeout);
            Assert.Null(player.Site);
            Assert.Empty(player.SoundLocation);
            Assert.Null(player.Stream);
            Assert.Null(player.Tag);
        }

        public static IEnumerable<object[]> Stream_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new MemoryStream() };
        }

        [Theory]
        [MemberData(nameof(Stream_TestData))]
        public void Ctor_Stream(Stream stream)
        {
            var player = new SoundPlayer(stream);
            Assert.Null(player.Container);
            Assert.False(player.IsLoadCompleted);
            Assert.Equal(10000, player.LoadTimeout);
            Assert.Null(player.Site);
            Assert.Empty(player.SoundLocation);
            Assert.Same(stream, player.Stream);
            Assert.Null(player.Tag);
        }

        [Theory]
        [InlineData("http://google.com")]
        [InlineData("invalid")]
        [InlineData("/file")]
        public void Ctor_String(string soundLocation)
        {
            var player = new SoundPlayer(soundLocation);
            Assert.Null(player.Container);
            Assert.False(player.IsLoadCompleted);
            Assert.Equal(10000, player.LoadTimeout);
            Assert.Null(player.Site);
            Assert.Equal(soundLocation ?? "", player.SoundLocation);
            Assert.Null(player.Stream);
            Assert.Null(player.Tag);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Ctor_NullOrEmptyString_ThrowsArgumentException(string soundLocation)
        {
            AssertExtensions.Throws<ArgumentException>("path", null, () => new SoundPlayer(soundLocation));
        }

        public static IEnumerable<object[]> Play_String_TestData()
        {
            yield return new object[] { "adpcm.wav" };
            yield return new object[] { "pcm.wav" };
        }

        public static IEnumerable<object[]> Play_InvalidString_TestData()
        {
            yield return new object[] { "ccitt.wav" };
            yield return new object[] { "ima.wav" };
        }

        [Theory]
        [MemberData(nameof(Play_String_TestData))]
        [OuterLoop]
        public void Load_SourceLocation_Success(string sourceLocation)
        {
            var soundPlayer = new SoundPlayer(sourceLocation);
            soundPlayer.Load();

            // Load again.
            soundPlayer.Load();

            // Play.
            soundPlayer.Play();
        }

        [Theory]
        [MemberData(nameof(Play_String_TestData))]
        [OuterLoop]
        public void Load_Stream_Success(string sourceLocation)
        {
            using (FileStream stream = File.OpenRead(sourceLocation.Replace("file://", "")))
            {
                var soundPlayer = new SoundPlayer(stream);
                soundPlayer.Load();

                // Load again.
                soundPlayer.Load();

                // Play.
                soundPlayer.Play();
            }
        }

        [Fact]
        public void Load_NoSuchFile_ThrowsFileNotFoundException()
        {
            var soundPlayer = new SoundPlayer("noSuchFile");
            Assert.Throws<FileNotFoundException>(() => soundPlayer.Load());
        }

        [Fact]
        public void Load_NullStream_ThrowsNullReferenceException()
        {
            var player = new SoundPlayer();
            Assert.Throws<NullReferenceException>(() => player.Load());

            player = new SoundPlayer((Stream)null);
            Assert.Throws<NullReferenceException>(() => player.Load());
        }

        [Theory]
        [MemberData(nameof(Play_InvalidString_TestData))]
        public void Load_InvalidSourceLocation_Success(string sourceLocation)
        {
            var soundPlayer = new SoundPlayer(sourceLocation);
            soundPlayer.Load();
        }

        [Theory]
        [MemberData(nameof(Play_InvalidString_TestData))]
        public void Load_InvalidStream_Success(string sourceLocation)
        {
            using (FileStream stream = File.OpenRead(sourceLocation))
            {
                var soundPlayer = new SoundPlayer(stream);
                soundPlayer.Load();
            }
        }

        [Theory]
        [MemberData(nameof(Play_String_TestData))]
        [OuterLoop]
        public void Play_SourceLocation_Success(string sourceLocation)
        {
            var soundPlayer = new SoundPlayer(sourceLocation);
            soundPlayer.Play();

            // Play again.
            soundPlayer.Play();

            // Load.
            soundPlayer.Load();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Play_NoSuchFile_ThrowsFileNotFoundException()
        {
            var soundPlayer = new SoundPlayer("noSuchFile");
            Assert.Throws<FileNotFoundException>(() => soundPlayer.Play());
        }

        [Theory]
        [MemberData(nameof(Play_String_TestData))]
        [OuterLoop]
        public void Play_Stream_Success(string sourceLocation)
        {
            using (FileStream stream = File.OpenRead(sourceLocation.Replace("file://", "")))
            {
                var soundPlayer = new SoundPlayer(stream);
                soundPlayer.Play();

                // Play again.
                soundPlayer.Play();

                // Load.
                soundPlayer.Load();
            }
        }

        [Fact]
        [OuterLoop]
        public void Play_NullStream_Success()
        {
            var player = new SoundPlayer();
            player.Play();

            player = new SoundPlayer((Stream)null);
            player.Play();
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(Play_InvalidString_TestData))]
        [InlineData("http://google.com")]
        public void Play_InvalidFile_ThrowsInvalidOperationException(string sourceLocation)
        {
            var soundPlayer = new SoundPlayer(sourceLocation);
            Assert.Throws<InvalidOperationException>(() => soundPlayer.Play());
        }

        [Theory]
        [MemberData(nameof(Play_InvalidString_TestData))]
        public void Play_InvalidStream_ThrowsInvalidOperationException(string sourceLocation)
        {
            using (FileStream stream = File.OpenRead(sourceLocation.Replace("file://", "")))
            {
                var soundPlayer = new SoundPlayer(stream);
                Assert.Throws<InvalidOperationException>(() => soundPlayer.Play());
            }
        }

        [Fact]
        [OuterLoop]
        public void PlayLooping_NullStream_Success()
        {
            var player = new SoundPlayer();
            player.PlayLooping();

            player = new SoundPlayer((Stream)null);
            player.PlayLooping();
        }

        [Fact]
        [OuterLoop]
        public void PlaySync_NullStream_Success()
        {
            var player = new SoundPlayer();
            player.PlaySync();

            player = new SoundPlayer((Stream)null);
            player.PlaySync();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        public void LoadTimeout_SetValid_GetReturnsExpected(int value)
        {
            var player = new SoundPlayer { LoadTimeout = value };
            Assert.Equal(value, player.LoadTimeout);
        }

        [Fact]
        public void LoadTimeout_SetNegative_ThrowsArgumentOutOfRangeException()
        {
            var player = new SoundPlayer();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("LoadTimeout", () => player.LoadTimeout = -1);
        }

        [Theory]
        [InlineData("http://google.com")]
        [InlineData("invalid")]
        [InlineData("/file")]
        [InlineData("file:///name")]
        public void SoundLocation_SetValid_Success(string soundLocation)
        {
            var player = new SoundPlayer() { SoundLocation = soundLocation };
            Assert.Equal(soundLocation, player.SoundLocation);

            bool calledHandler = false;
            player.SoundLocationChanged += (args, sender) => calledHandler = true;

            // Set the same.
            player.SoundLocation = soundLocation;
            Assert.Equal(soundLocation, player.SoundLocation);
            Assert.False(calledHandler);

            // Set different.
            player.SoundLocation = soundLocation + "a";
            Assert.Equal(soundLocation + "a", player.SoundLocation);
            Assert.True(calledHandler);

            player = new SoundPlayer("location") { SoundLocation = soundLocation };
            Assert.Equal(soundLocation, player.SoundLocation);

            using (var stream = new MemoryStream())
            {
                player = new SoundPlayer(stream) { SoundLocation = soundLocation };
                Assert.Equal(soundLocation, player.SoundLocation);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void SoundLocation_SetNullOrEmpty_ThrowsArgumentException(string soundLocation)
        {
            var player = new SoundPlayer() { SoundLocation = soundLocation };
            Assert.Equal("", player.SoundLocation);

            player = new SoundPlayer("location");
            AssertExtensions.Throws<ArgumentException>("path", null, () => player.SoundLocation = soundLocation);

            using (var stream = new MemoryStream())
            {
                player = new SoundPlayer(stream) { SoundLocation = soundLocation };
                Assert.Equal("", player.SoundLocation);
            }
        }

        [Theory]
        [MemberData(nameof(Stream_TestData))]
        public void Stream_SetValid_Success(Stream stream)
        {
            var player = new SoundPlayer() { Stream = stream };
            Assert.Equal(stream, player.Stream);

            bool calledHandler = false;
            player.StreamChanged += (args, sender) => calledHandler = true;

            // Set the same.
            player.Stream = stream;
            Assert.Equal(stream, player.Stream);
            Assert.False(calledHandler);

            // Set different.
            using (var other = new MemoryStream())
            {
                player.Stream = other;
                Assert.Equal(other, player.Stream);
                Assert.True(calledHandler);
            }

            player = new SoundPlayer("location") { Stream = stream };
            Assert.Equal(stream, player.Stream);

            using (var other = new MemoryStream())
            {
                player = new SoundPlayer(other) { Stream = stream };
                Assert.Equal(stream, player.Stream);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("tag")]
        public void Tag_Set_GetReturnsExpected(object value)
        {
            var player = new SoundPlayer { Tag = value };
            Assert.Equal(value, player.Tag);
        }

        [Fact]
        public void LoadCompleted_AddRemove_Success()
        {
            bool calledHandler = false;
            void handler(object args, EventArgs sender) => calledHandler = true;

            var player = new SoundPlayer();
            player.LoadCompleted += handler;
            player.LoadCompleted -= handler;

            Assert.False(calledHandler);
        }

        [Fact]
        public void SoundLocationChanged_AddRemove_Success()
        {
            bool calledHandler = false;
            void handler(object args, EventArgs sender) => calledHandler = true;

            var player = new SoundPlayer();
            player.SoundLocationChanged += handler;
            player.SoundLocationChanged -= handler;

            player.SoundLocation = "location";
            Assert.False(calledHandler);
        }

        [Fact]
        public void StreamChanged_AddRemove_Success()
        {
            bool calledHandler = false;
            void handler(object args, EventArgs sender) => calledHandler = true;

            var player = new SoundPlayer();
            player.StreamChanged += handler;
            player.StreamChanged -= handler;

            using (var stream = new MemoryStream())
            {
                player.Stream = stream;
                Assert.False(calledHandler);
            }
        }
    }
}
