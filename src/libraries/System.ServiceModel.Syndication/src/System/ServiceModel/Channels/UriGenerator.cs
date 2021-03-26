// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Globalization;

namespace System.ServiceModel.Channels
{
    internal sealed class UriGenerator
    {
        private long _id;
        private readonly string _prefix;

        public UriGenerator()
        {
            _prefix = string.Concat("uuid:", Guid.NewGuid().ToString(), ";id=");
        }

        public string Next()
        {
            long nextId = Interlocked.Increment(ref _id);
            return _prefix + nextId.ToString(CultureInfo.InvariantCulture);
        }
    }
}
