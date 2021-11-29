// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;

namespace System.Text
{
    public abstract class EncodingProvider
    {
        private static volatile EncodingProvider[]? s_providers;

        public EncodingProvider() { }
        public abstract Encoding? GetEncoding(string name);
        public abstract Encoding? GetEncoding(int codepage);

        // GetEncoding should return either valid encoding or null. shouldn't throw any exception except on null name
        public virtual Encoding? GetEncoding(string name, EncoderFallback encoderFallback, DecoderFallback decoderFallback)
        {
            Encoding? enc = GetEncoding(name);
            if (enc != null)
            {
                enc = (Encoding)enc.Clone();
                enc.EncoderFallback = encoderFallback;
                enc.DecoderFallback = decoderFallback;
            }

            return enc;
        }

        public virtual Encoding? GetEncoding(int codepage, EncoderFallback encoderFallback, DecoderFallback decoderFallback)
        {
            Encoding? enc = GetEncoding(codepage);
            if (enc != null)
            {
                enc = (Encoding)enc.Clone();
                enc.EncoderFallback = encoderFallback;
                enc.DecoderFallback = decoderFallback;
            }

            return enc;
        }

        public virtual IEnumerable<EncodingInfo> GetEncodings() => Array.Empty<EncodingInfo>();

        internal static void AddProvider(EncodingProvider provider)
        {
            if (provider is null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            // Few providers are added in a typical app (typically just CodePagesEncodingProvider.Instance), and when they are,
            // they're generally not added concurrently.  So use an optimistic concurrency scheme rather than paying for a lock
            // object allocation on the startup path.

            if (s_providers is null &&
                Interlocked.CompareExchange(ref s_providers, new EncodingProvider[1] { provider }, null) is null)
            {
                return;
            }

            while (true)
            {
                EncodingProvider[] providers = s_providers;

                if (Array.IndexOf(providers, provider) >= 0)
                {
                    return;
                }

                var newProviders = new EncodingProvider[providers.Length + 1];
                Array.Copy(providers, newProviders, providers.Length);
                newProviders[^1] = provider;

                if (Interlocked.CompareExchange(ref s_providers, newProviders, providers) == providers)
                {
                    return;
                }
            }
        }

        internal static Encoding? GetEncodingFromProvider(int codepage)
        {
            EncodingProvider[]? providers = s_providers;
            if (providers == null)
                return null;

            foreach (EncodingProvider provider in providers)
            {
                Encoding? enc = provider.GetEncoding(codepage);
                if (enc != null)
                    return enc;
            }

            return null;
        }

        internal static Dictionary<int, EncodingInfo>? GetEncodingListFromProviders()
        {
            EncodingProvider[]? providers = s_providers;
            if (providers == null)
                return null;

            Dictionary<int, EncodingInfo> result = new Dictionary<int, EncodingInfo>();

            foreach (EncodingProvider provider in providers)
            {
                IEnumerable<EncodingInfo>? encodingInfoList = provider.GetEncodings();
                if (encodingInfoList != null)
                {
                    foreach (EncodingInfo ei in encodingInfoList)
                    {
                        result.TryAdd(ei.CodePage, ei);
                    }
                }
            }

            return result;
        }

        internal static Encoding? GetEncodingFromProvider(string encodingName)
        {
            if (s_providers == null)
                return null;

            EncodingProvider[] providers = s_providers;
            foreach (EncodingProvider provider in providers)
            {
                Encoding? enc = provider.GetEncoding(encodingName);
                if (enc != null)
                    return enc;
            }

            return null;
        }

        internal static Encoding? GetEncodingFromProvider(int codepage, EncoderFallback enc, DecoderFallback dec)
        {
            if (s_providers == null)
                return null;

            EncodingProvider[] providers = s_providers;
            foreach (EncodingProvider provider in providers)
            {
                Encoding? encoding = provider.GetEncoding(codepage, enc, dec);
                if (encoding != null)
                    return encoding;
            }

            return null;
        }

        internal static Encoding? GetEncodingFromProvider(string encodingName, EncoderFallback enc, DecoderFallback dec)
        {
            if (s_providers == null)
                return null;

            EncodingProvider[] providers = s_providers;
            foreach (EncodingProvider provider in providers)
            {
                Encoding? encoding = provider.GetEncoding(encodingName, enc, dec);
                if (encoding != null)
                    return encoding;
            }

            return null;
        }
    }
}
