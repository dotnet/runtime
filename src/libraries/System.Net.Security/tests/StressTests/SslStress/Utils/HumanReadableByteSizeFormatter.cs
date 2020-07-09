// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace SslStress.Utils
{
    public static class HumanReadableByteSizeFormatter
    {
        private static readonly string[] s_suffixes = { "B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB" };

        public static string Format(long byteCount)
        {
            // adapted from https://stackoverflow.com/a/4975942
            if (byteCount == 0)
            {
                return $"0{s_suffixes[0]}";
            }

            int position = (int)Math.Floor(Math.Log(Math.Abs(byteCount), 1024));
            double renderedValue = byteCount / Math.Pow(1024, position);
            return $"{renderedValue:0.#}{s_suffixes[position]}";
        }
    }
}
