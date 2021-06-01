// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Text;

namespace System.Net.Http.Json
{
    internal static class JsonHelpers
    {
        internal const DynamicallyAccessedMemberTypes SerializationMemberTypes = DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties;
        internal const DynamicallyAccessedMemberTypes DeserializationMemberTypes = DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties;

        internal static MediaTypeHeaderValue GetDefaultMediaType() => new("application/json") { CharSet = "utf-8" };

        internal static Encoding? GetEncoding(string? charset)
        {
            Encoding? encoding = null;

            if (charset != null)
            {
                try
                {
                    // Remove at most a single set of quotes.
                    if (charset.Length > 2 && charset[0] == '\"' && charset[charset.Length - 1] == '\"')
                    {
                        encoding = Encoding.GetEncoding(charset.Substring(1, charset.Length - 2));
                    }
                    else
                    {
                        encoding = Encoding.GetEncoding(charset);
                    }
                }
                catch (ArgumentException e)
                {
                    throw new InvalidOperationException(SR.CharSetInvalid, e);
                }

                Debug.Assert(encoding != null);
            }

            return encoding;
        }
    }
}
