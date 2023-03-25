// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Tests
{
    public interface IShakeTrait<TShake> where TShake : IDisposable, new()
    {
        static abstract bool IsSupported { get; }
        public TShake Create() => new TShake();
        static abstract void AppendData(TShake shake, byte[] data);
        static abstract void AppendData(TShake shake, ReadOnlySpan<byte> data);
        static abstract byte[] GetHashAndReset(TShake shake, int outputLength);
        static abstract byte[] GetCurrentHash(TShake shake, int outputLength);
    }

    public class Shake128Tests : ShakeTestDriver<Shake128Tests.Traits, Shake128>
    {
        public class Traits : IShakeTrait<Shake128>
        {
            public static bool IsSupported => Shake128.IsSupported;

            public static void AppendData(Shake128 shake, byte[] data) => shake.AppendData(data);
            public static void AppendData(Shake128 shake, ReadOnlySpan<byte> data) => shake.AppendData(data);
            public static byte[] GetHashAndReset(Shake128 shake, int outputLength) => shake.GetHashAndReset(outputLength);
            public static byte[] GetCurrentHash(Shake128 shake, int outputLength) => shake.GetCurrentHash(outputLength);
        }
    }
}
