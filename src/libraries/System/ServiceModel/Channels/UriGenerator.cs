// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ServiceModel.Channels
{
    using System.Threading;
    using System.Globalization;
    using Microsoft.ServiceModel;
    using Microsoft.ServiceModel.Syndication.Resources;

    class UriGenerator
    {
        long id;
        string prefix;

        public UriGenerator()
            : this("uuid")
        {
        }

        public UriGenerator(string scheme)
            : this(scheme, ";")
        {
        }

        public UriGenerator(string scheme, string delimiter)
        {
            if (scheme == null)
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException("scheme"));

            if (scheme.Length == 0)
                throw new ArgumentException(String.Format(SR.UriGeneratorSchemeMustNotBeEmpty, "scheme"));

            prefix = string.Concat(scheme, ":", Guid.NewGuid().ToString(), delimiter, "id=");
        }

        public string Next()
        {
            long nextId = Interlocked.Increment(ref id);
            return prefix + nextId.ToString(CultureInfo.InvariantCulture);
        }
    }
}
