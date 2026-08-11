// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Microsoft.Extensions.Configuration
{
    // Resolves configuration references. A value that is exactly "$ref(<key>)" reads as the value of another key. Only
    // values carry references, so nothing else about configuration changes: enumeration, change tokens and writes are
    // untouched, and a section is never redirected - a reference names a single key, and one whose target holds no value
    // of its own reads as absent.
    internal sealed class ReferenceEngine : ConfigurationEngine
    {
        private const string SwitchName = "Microsoft.Extensions.Configuration.DisableConfigurationReferences";
        private const int MaxNesting = 32;
        private const int MaxChain = 64;
        private const int StackKeyLength = 128;

        [FeatureSwitchDefinition(SwitchName)]
        internal static bool Disabled { get; } = AppContext.TryGetSwitch(SwitchName, out bool disabled) && disabled;

        internal ReferenceEngine(ConfigurationEngine next) : base(next) { }

        internal override ConfigurationValue? Get(IList<IConfigurationProvider> providers, string key)
        {
            Trail trail = default;
            ConfigurationValue? read = Read(providers, key, nesting: 0, hops: 0, ref trail);

            if (!trail.Recording)
            {
                return read;
            }

            throw new InvalidOperationException(trail.Stopped switch
            {
                Trail.Stop.Cycle => SR.Format(SR.Error_ReferenceCycle, key, trail.Path()),
                Trail.Stop.Nesting => SR.Format(SR.Error_ReferenceNestingLimit, key, MaxNesting, trail.Path()),
                _ => SR.Format(SR.Error_ReferenceChainLimit, key, MaxChain, trail.Path()),
            });
        }

        private ConfigurationValue? Read(IList<IConfigurationProvider> providers, string key, int nesting, int hops, ref Trail trail)
        {
            if (Next.Get(providers, key) is not { } declaration)
            {
                return null;
            }

            string? raw = declaration.Value;

            if (!ReferenceSyntax.TryGetBody(raw, out int bodyStart, out int bodyLength) || providers[declaration.ProviderIndex] is ChainedConfigurationProvider)
            {
                return declaration;
            }

            ConfigurationValue? followed = null;

            if (hops >= MaxChain)
            {
                trail.Open(Trail.Stop.Chain);
            }
            else if (BuildKey(providers, raw.AsSpan(bodyStart, bodyLength), key, nesting, hops, ref trail) is { } target
                && Read(providers, target, nesting, hops + 1, ref trail) is { } next)
            {
                followed = declaration.WithValue(next.Value);
            }

            trail.Record(key);

            return followed;
        }

        private string? BuildKey(IList<IConfigurationProvider> providers, ReadOnlySpan<char> body, string baseKey, int nesting, int hops, ref Trail trail)
        {
            body = body.Trim();
            if (body.IsEmpty)
            {
                ThrowMalformedException(baseKey);
            }

            ValueStringBuilder text = new ValueStringBuilder(stackalloc char[StackKeyLength]);
            try
            {
                int write = 0;
                if (ReferenceSyntax.IsRelative(body))
                {
                    text.Append(baseKey);
                    write = text.Length;
                }

                int start = write;
                int read = write;
                text.Append(body);

                bool unresolved = false;
                int puts = 0;

                while (read < text.Length)
                {
                    char c = text[read];

                    if (c == ReferenceSyntax.BraceOpen)
                    {
                        int close = ReferenceSyntax.FindMatchingBrace(text.AsSpan(), read, baseKey);
                        if (nesting >= MaxNesting || ++puts > MaxNesting)
                        {
                            trail.Open(Trail.Stop.Nesting);
                            return null;
                        }

                        string? inner = BuildKey(providers, text.AsSpan(read + 1, close - read - 1), baseKey, nesting, hops, ref trail);
                        if (inner is null)
                        {
                            return null;
                        }

                        string? value = Read(providers, inner, nesting + 1, hops, ref trail)?.Value;
                        if (value is null)
                        {
                            return null;
                        }

                        Replace(ref text, read, close - read + 1, value);
                    }
                    else if (c == ReferenceSyntax.BraceClose)
                    {
                        throw ThrowMalformedException(baseKey);
                    }
                    else if (ReferenceSyntax.IsQuote(c))
                    {
                        TakeQuoted(ref text, baseKey, ref read, ref write);
                    }
                    else if (c == ReferenceSyntax.SelfMarker && ReferenceSyntax.MoveLength(text.AsSpan(), read, start) is int dots && dots > 0)
                    {
                        TakeMove(ref text, dots, ref read, ref write, ref unresolved);
                    }
                    else
                    {
                        text[write++] = c;
                        read++;
                    }
                }

                text.Length = write;
                return unresolved ? null : text.ToString();
            }
            finally
            {
                text.Dispose();
            }
        }

        private static void TakeQuoted(ref ValueStringBuilder text, string baseKey, ref int read, ref int write)
        {
            int pastEnd = ReferenceSyntax.SkipQuoted(text.AsSpan(), read);
            if (pastEnd < 0)
            {
                ThrowMalformedException(baseKey);
            }

            write = ReferenceSyntax.WriteQuoted(ref text, read, pastEnd, write);
            read = pastEnd;
        }

        private static void TakeMove(ref ValueStringBuilder text, int dots, ref int read, ref int write, ref bool unresolved)
        {
            if (dots == 1)
            {
                write = EndSegment(ref text, write);
            }
            else
            {
                int parent = MoveToParent(ref text, write);
                if (parent < 0)
                {
                    unresolved = true;
                    parent = 0;
                }

                write = parent;
            }

            read += dots;
            if (write == 0 && read < text.Length)
            {
                read++;
            }
        }

        // Puts <paramref name="value"/> where the text spanning [start, start + length) was, leaving everything before
        // it untouched so the key built so far survives.
        private static void Replace(ref ValueStringBuilder text, int start, int length, string value)
        {
            int tail = text.Length - (start + length);
            text.RawChars.Slice(start + length, tail).CopyTo(text.RawChars.Slice(start, tail));
            text.Length -= length;
            text.Insert(start, value);
        }

        [DoesNotReturn]
        private static InvalidOperationException ThrowMalformedException(string key)
        {
            throw new InvalidOperationException(SR.Format(SR.Error_MalformedReference, key));
        }

        // What a read that went too far leaves behind. Nothing is written down until one of the bounds is reached, so
        // a read that resolves carries a null reference and never looks at it again.
        private struct Trail
        {
            private List<string>? _keys;
            private Stop _stopped;

            internal readonly bool Recording => _keys is not null;

            // Starts recording, naming the bound that noticed.
            internal void Open(Stop stopped)
            {
                _keys = new List<string>();
                _stopped = stopped;
            }

            // Writes a key down on the way out, and stops once the path leads back to one already written: that closes
            // the loop, and the trail holds it entire. What lies shallower is how the read reached the loop rather
            // than part of it, so there is nothing there worth keeping.
            internal void Record(string key)
            {
                if (_keys is null || _stopped is Stop.Cycle)
                {
                    return;
                }

                // Configuration keys match without regard to case.
                foreach (string seen in _keys)
                {
                    if (string.Equals(seen, key, StringComparison.OrdinalIgnoreCase))
                    {
                        _stopped = Stop.Cycle;
                        break;
                    }
                }

                _keys.Add(key);
            }

            // Why the read stopped, which decides what there is to say about the path.
            internal readonly Stop Stopped => _stopped;

            // The keys the read passed through, in the order it read them. Where a loop closed that is the loop and
            // nothing else, since the keys that led to it were never written down; where none did it is the whole path,
            // from the key that was asked for to wherever the read ran out.
            internal readonly string Path()
            {
                // Written down on the way out, so it reads deepest first and has to be turned round.
                StringBuilder path = new StringBuilder(_keys![_keys.Count - 1]);
                for (int i = _keys.Count - 2; i >= 0; i--)
                {
                    path.Append(" -> ").Append(_keys[i]);
                }

                return path.ToString();
            }

            // Why a read stopped, and so what there is to say about it. A loop closing overrides whichever bound noticed
            // first, because the bound is only how the read found out.
            public enum Stop
            {
                Chain,
                Nesting,
                Cycle,
            }
        }

        // Drops a trailing separator, so the key built so far ends at a segment boundary rather than part-way through
        // joining on the next one. Returns where the key now ends.
        private static int EndSegment(ref ValueStringBuilder text, int write)
        {
            if (write > 0 && text[write - 1] == ReferenceSyntax.KeyDelimiter)
            {
                write--;
            }

            return write;
        }

        // Drops the last segment of the key built so far: "A:B:C" and "A:B:C:" both become "A:B", "A" becomes the root,
        // and the root has no parent. Returns where the key now ends, or -1 when there was no parent to move to.
        private static int MoveToParent(ref ValueStringBuilder text, int write)
        {
            write = EndSegment(ref text, write);
            if (write == 0)
            {
                return -1;
            }

            while (write > 0 && text[write - 1] != ReferenceSyntax.KeyDelimiter)
            {
                write--;
            }

            if (write > 0)
            {
                // The separator belongs to the segment being dropped.
                write--;
            }

            return write;
        }
    }
}
