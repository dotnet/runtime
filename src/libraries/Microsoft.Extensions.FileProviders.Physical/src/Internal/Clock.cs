// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Extensions.FileProviders.Physical
{
    internal class Clock : IClock
    {
        public static readonly Clock Instance = new Clock();

        private Clock()
        {
        }

        public DateTime UtcNow => DateTime.UtcNow;
    }
}
