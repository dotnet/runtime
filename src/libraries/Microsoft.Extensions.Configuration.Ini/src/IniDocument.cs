// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Extensions.Configuration.Ini
{
    /// <summary>
    /// A lossless, editable model of an INI file: a sequence of <c>[Section]</c>
    /// blocks whose entries are <c>Key=value</c> lines, where integer-style values
    /// are written bare (<c>VICIIModel=3</c>) and string values may be
    /// double-quoted (<c>WIC64MACAddress="08:d1:f9:0a:0c:0e"</c>).
    ///
    /// This is the write engine the built-in <see cref="IniStreamConfigurationProvider"/>
    /// lacks. Because callers frequently edit a config file they <em>share</em>
    /// with another application, the document preserves every section, key, order,
    /// and quoting it parsed: a read-modify-write round-trips losslessly and never
    /// drops resources the caller does not itself manage.
    ///
    /// (Comment lines and blank-line layout are not preserved on write; the read
    /// path ignores them, matching the built-in provider.)
    /// </summary>
    public sealed class IniDocument
    {
        private sealed class IniEntry
        {
            public required string Key { get; init; }
            public string Value { get; set; } = string.Empty;
            public bool Quoted { get; set; }
        }

        private sealed class IniSection
        {
            public required string Name { get; init; }
            public List<IniEntry> Entries { get; } = new();
        }

        private readonly List<IniSection> _sections = new();

        /// <summary>Section names in document order.</summary>
        public IReadOnlyList<string> Sections => _sections.ConvertAll(s => s.Name);

        /// <summary>Parse INI text into an editable, round-trippable document.</summary>
        public static IniDocument Parse(string text)
        {
            ArgumentNullException.ThrowIfNull(text);

            var document = new IniDocument();
            IniSection? current = null;

            foreach (var rawLine in text.Split('\n'))
            {
                var line = rawLine.TrimEnd('\r').Trim();
                if (line.Length == 0)
                    continue;

                // Skip comment lines (parity with the built-in INI read path).
                if (line[0] is ';' or '#' or '/')
                    continue;

                if (line[0] == '[' && line[^1] == ']')
                {
                    var name = line[1..^1].Trim();
                    current = document.GetOrAddSection(name);
                    continue;
                }

                var eq = line.IndexOf('=');
                if (eq <= 0 || current is null)
                    continue;

                var key = line[..eq].Trim();
                var rawValue = line[(eq + 1)..].Trim();
                var quoted = rawValue.Length >= 2 && rawValue[0] == '"' && rawValue[^1] == '"';
                var value = quoted ? rawValue[1..^1] : rawValue;

                current.Entries.Add(new IniEntry { Key = key, Value = value, Quoted = quoted });
            }

            return document;
        }

        /// <summary>Get a value (quotes stripped), or null if the section/key is absent.</summary>
        public string? Get(string section, string key)
        {
            var entry = FindSection(section)?.Entries
                .FirstOrDefault(e => string.Equals(e.Key, key, StringComparison.Ordinal));
            return entry?.Value;
        }

        /// <summary>
        /// Set a value, updating an existing entry in place or appending a new one
        /// (creating the section if needed). <paramref name="quote"/>: true forces
        /// double-quoting, false forces bare; null (the default) preserves the
        /// existing entry's quoting on update and writes bare for a new entry.
        /// Preserving on update is what lets a read-modify-write round-trip a shared
        /// file without unquoting its string values.
        /// </summary>
        public void Set(string section, string key, string value, bool? quote = null)
        {
            ArgumentNullException.ThrowIfNull(value);

            var target = GetOrAddSection(section);
            var entry = target.Entries
                .FirstOrDefault(e => string.Equals(e.Key, key, StringComparison.Ordinal));
            if (entry is null)
            {
                target.Entries.Add(new IniEntry { Key = key, Value = value, Quoted = quote ?? false });
                return;
            }

            entry.Value = value;
            if (quote.HasValue)
                entry.Quoted = quote.Value;
        }

        /// <summary>Remove a value. Returns true if it existed.</summary>
        public bool Remove(string section, string key)
        {
            var target = FindSection(section);
            return target is not null
                && target.Entries.RemoveAll(e => string.Equals(e.Key, key, StringComparison.Ordinal)) > 0;
        }

        /// <summary>Entries of a section in order, with quotes stripped.</summary>
        public IReadOnlyList<(string Key, string Value)> Entries(string section)
        {
            var target = FindSection(section);
            return target is null ? Array.Empty<(string, string)>() : target.Entries.ConvertAll(e => (e.Key, e.Value));
        }

        /// <summary>Serialize back to INI text (sections in order, blank line between each).</summary>
        public string ToIniString()
        {
            var builder = new StringBuilder();
            foreach (var section in _sections)
            {
                builder.Append('[').Append(section.Name).Append(']').Append('\n');
                foreach (var entry in section.Entries)
                {
                    builder.Append(entry.Key).Append('=');
                    if (entry.Quoted)
                        builder.Append('"').Append(entry.Value).Append('"');
                    else
                        builder.Append(entry.Value);
                    builder.Append('\n');
                }

                builder.Append('\n');
            }

            return builder.ToString();
        }

        /// <summary>Returns this document serialized as INI text.</summary>
        public override string ToString() => ToIniString();

        private IniSection? FindSection(string section) =>
            _sections.FirstOrDefault(s => string.Equals(s.Name, section, StringComparison.Ordinal));

        private IniSection GetOrAddSection(string section)
        {
            var existing = FindSection(section);
            if (existing is not null)
                return existing;

            var created = new IniSection { Name = section };
            _sections.Add(created);
            return created;
        }
    }
}
