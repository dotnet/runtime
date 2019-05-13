// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Text
{
    public abstract class EncodingProvider
    {
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

        internal static void AddProvider(EncodingProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            lock (s_InternalSyncObject)
            {
                if (s_providers == null)
                {
                    s_providers = new EncodingProvider[1] { provider };
                    return;
                }

                if (Array.IndexOf(s_providers, provider) >= 0)
                {
                    return;
                }

                EncodingProvider[] providers = new EncodingProvider[s_providers.Length + 1];
                Array.Copy(s_providers, 0, providers, 0, s_providers.Length);
                providers[providers.Length - 1] = provider;
                s_providers = providers;
            }
        }

        internal static Encoding? GetEncodingFromProvider(int codepage)
        {
            if (s_providers == null)
                return null;

            EncodingProvider[] providers = s_providers;
            foreach (EncodingProvider provider in providers)
            {
                Encoding? enc = provider.GetEncoding(codepage);
                if (enc != null)
                    return enc;
            }

            return null;
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

        private static object s_InternalSyncObject = new object();
        private static volatile EncodingProvider[]? s_providers;
    }
}
