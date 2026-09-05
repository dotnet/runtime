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
        private const string SwitchName = "Microsoft.Extensions.Configuration.DisableConfigurationTransformations";
        // The three ways one read can grow, each bounded on its own: steps from one reference to the next, levels of
        // sub-reference, and values put into any single key expression.
        private const int MaxChain = 64;
        private const int MaxNesting = 32;
        private const int MaxExpansion = 32;
        private const int StackKeyLength = 128;

        [FeatureSwitchDefinition(SwitchName)]
        internal static bool Disabled { get; } = AppContext.TryGetSwitch(SwitchName, out bool disabled) && disabled;

        internal ReferenceEngine(ConfigurationEngine next) : base(next) { }

        internal override bool Get(IList<IConfigurationProvider> providers, string key, out string? value, out int providerIndex)
        {
            Trail trail = default;
            bool read = Read(providers, key, nesting: 0, hops: 0, ref trail, out value, out providerIndex);

            if (!trail.Recording)
            {
                return read;
            }

            throw new InvalidOperationException(trail.Stopped switch
            {
                Trail.Stop.Cycle => SR.Format(SR.Error_ReferenceCycle, key, trail.Path()),
                Trail.Stop.Nesting => SR.Format(SR.Error_ReferenceNestingLimit, key, MaxNesting, trail.Path()),
                Trail.Stop.Expansion => SR.Format(SR.Error_ReferenceExpansionLimit, key, MaxExpansion, trail.Path()),
                _ => SR.Format(SR.Error_ReferenceChainLimit, key, MaxChain, trail.Path()),
            });
        }

        private bool Read(IList<IConfigurationProvider> providers, string key, int nesting, int hops, ref Trail trail, out string? value, out int providerIndex)
        {
            if (!Next.Get(providers, key, out value, out providerIndex))
            {
                return false;
            }

            if (value is not string raw)
            {
                return true;
            }

            // A chained configuration has already read its own values, so what it serves is text.
            if (!ReferenceSyntax.IsReference(raw, out int bodyStart, out int bodyLength)
             || providers[providerIndex] is ChainedConfigurationProvider)
            {
                return true;
            }

            bool followed = false;

            if (hops >= MaxChain)
            {
                trail.Open(Trail.Stop.Chain);
            }
            else if (BuildKey(providers, raw.AsSpan(bodyStart, bodyLength), key, nesting, hops, ref trail) is { } target)
            {
                // The key was declared here whatever it turned out to name, so the target's provider is not the answer.
                followed = Read(providers, target, nesting, hops + 1, ref trail, out value, out _);
            }

            trail.Record(key);

            if (!followed)
            {
                value = null;
                providerIndex = -1;
            }

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
                text.Append(body);

                int read = 0;
                int write = 0;

                bool unresolved = false;
                int expansions = 0;

                while (read < text.Length)
                {
                    char c = text[read];

                    if (c == ReferenceSyntax.BraceOpen)
                    {
                        int close = ReferenceSyntax.FindMatchingBrace(text.AsSpan(), read, baseKey);
                        if (nesting >= MaxNesting)
                        {
                            trail.Open(Trail.Stop.Nesting);
                            return null;
                        }

                        if (++expansions > MaxExpansion)
                        {
                            trail.Open(Trail.Stop.Expansion);
                            return null;
                        }

                        string? inner = BuildKey(providers, text.AsSpan(read + 1, close - read - 1), baseKey, nesting, hops, ref trail);
                        if (inner is null)
                        {
                            return null;
                        }

                        // Neither an absent key nor one held as null has any text to put in.
                        if (!Read(providers, inner, nesting + 1, hops, ref trail, out string? value, out _) || value is null)
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
                    else if (c == ReferenceSyntax.SelfMarker && ReferenceSyntax.MoveLength(text.AsSpan(), read, write) is int dots && dots > 0)
                    {
                        // A move that opens the expression has nothing written in front of it, so it is not joined onto
                        // anything and the key it moves from is the whole of the key the reference was found at.
                        bool joined = read > 0;
                        if (!joined)
                        {
                            Anchor(ref text, baseKey, ref read, ref write);
                        }

                        TakeMove(ref text, dots, joined, ref read, ref write, ref unresolved);
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

        // Puts the key the reference was found at in front of the expression, so a move that opens it has somewhere to
        // move from. Nothing has been consumed or emitted when a move opens one, which is as true of a move a
        // substitution brought in as of one that was written there.
        private static void Anchor(ref ValueStringBuilder text, string baseKey, ref int read, ref int write)
        {
            text.Insert(0, baseKey);
            read = baseKey.Length;
            write = baseKey.Length;
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

        private static void TakeMove(ref ValueStringBuilder text, int dots, bool joined, ref int read, ref int write, ref bool unresolved)
        {
            if (joined)
            {
                // Drop the separator that joined this move onto what was written before it, so a move of no distance
                // leaves the key exactly as it was and one of a level starts from a segment boundary.
                write = EndSegment(ref text, write);
            }

            if (dots == 2)
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
                Expansion,
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

        // Drops the last segment of the key built so far: "A:B:C" becomes "A:B", "A" becomes the root, and the root has
        // no parent. Returns where the key now ends, or -1 when there was no parent to move to.
        private static int MoveToParent(ref ValueStringBuilder text, int write)
        {
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
