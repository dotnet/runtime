// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Globalization;

namespace System.ServiceModel.Channels
{
    internal class UriGenerator
    {
        private long _id;
        private readonly string _prefix;

        public UriGenerator() : this("uuid")
        {
        }

        public UriGenerator(string scheme) : this(scheme, ";")
        {
        }

        public UriGenerator(string scheme, string delimiter)
        {
            if (scheme == null)
            {
                throw new ArgumentNullException(nameof(scheme));
            }
            if (scheme.Length == 0)
            {
                throw new ArgumentException(SR.UriGeneratorSchemeMustNotBeEmpty, nameof(scheme));
            }

            _prefix = string.Concat(scheme, ":", Guid.NewGuid().ToString(), delimiter, "id=");
        }

        public string Next()
        {
            long nextId = Interlocked.Increment(ref _id);
            return _prefix + nextId.ToString(CultureInfo.InvariantCulture);
        }
    }
}
