// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.CommandLine;

namespace ILDisassembler;

internal static class NativeCommandLine
{
    private static readonly Dictionary<string, string> s_options = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ALL"] = "--all",
        ["BYT"] = "--bytes",
        ["CAV"] = "--caverbal",
        ["CLA"] = "--classlist",
        ["FOR"] = "--forward",
        ["HEA"] = "--headers",
        ["HTM"] = "--html",
        ["ITE"] = "--item",
        ["LIN"] = "--linenum",
        ["MET"] = "--metadata",
        ["NOC"] = "--noca",
        ["NOI"] = "--noil",
        ["NOL"] = "--nologo",
        ["NOT"] = "--raweh",
        ["OUT"] = "--output",
        ["PUB"] = "--pubonly",
        ["QUI"] = "--quiet",
        ["QUO"] = "--quoteallnames",
        ["R2R"] = "--r2rnativemetadata",
        ["RAW"] = "--raweh",
        ["RTF"] = "--rtf",
        ["SOU"] = "--source",
        ["STA"] = "--stats",
        ["TOK"] = "--tokens",
        ["TYP"] = "--typelist",
        ["UNI"] = "--unicode",
        ["UTF"] = "--utf8",
        ["VIS"] = "--visibility",
    };

    // ADV only unlocked STATS, CLASSLIST and METADATA in release builds; PRE (PRETTY) is obsolete.
    private static readonly HashSet<string> s_ignoredOptions = new(StringComparer.OrdinalIgnoreCase)
    {
        "ADV",
        "PRE",
    };

    private static readonly HashSet<string> s_unsupportedOptions = new(StringComparer.OrdinalIgnoreCase)
    {
        "DEC",
        "OBJ",
        "PRO",
        "REF",
        "SUM",
    };

    internal static string[] Normalize(string[] args) =>
        Normalize(args, new IldasmRootCommand(), allowSlashOptions: OperatingSystem.IsWindows());

    internal static string[] Normalize(string[] args, bool allowSlashOptions)
        => Normalize(args, new IldasmRootCommand(), allowSlashOptions);

    internal static string[] Normalize(string[] args, IldasmRootCommand command)
        => Normalize(args, command, allowSlashOptions: OperatingSystem.IsWindows());

    private static string[] Normalize(string[] args, IldasmRootCommand command, bool allowSlashOptions)
    {
        HashSet<string> optionsWithValues = new(StringComparer.Ordinal);
        Dictionary<string, Option> modernShortOptions = new(StringComparer.Ordinal);
        foreach (Option option in command.Options)
        {
            AddModernOption(option.Name, option);
            foreach (string alias in option.Aliases)
            {
                AddModernOption(alias, option);
            }
        }

        List<string> result = new(args.Length);
        bool preserveRemainingArguments = false;
        bool preserveNextArgument = false;

        foreach (string arg in args)
        {
            if (preserveRemainingArguments || preserveNextArgument)
            {
                result.Add(arg);
                preserveNextArgument = false;
                continue;
            }

            if (arg == "--")
            {
                result.Add(arg);
                preserveRemainingArguments = true;
                continue;
            }

            if (optionsWithValues.Contains(arg))
            {
                result.Add(arg);
                preserveNextArgument = true;
                continue;
            }

            if (arg is "-?" || (allowSlashOptions && arg == "/?"))
            {
                result.Add("--help");
                continue;
            }

            if (!TrySplitNativeOption(arg, allowSlashOptions, out string optionName, out string? value))
            {
                result.Add(arg);
                continue;
            }

            if (optionName.Length < 3)
            {
                if (!IsValidModernShortOption(arg[0], optionName, value, modernShortOptions))
                {
                    throw new ArgumentException($"Invalid native option '{arg}'.");
                }

                result.Add(arg);
                continue;
            }

            string prefix = optionName[..3];
            if (s_unsupportedOptions.Contains(prefix))
            {
                throw new ArgumentException(
                    $"Unsupported native option '{arg}'. The managed ildasm implementation does not support -{prefix.ToUpperInvariant()}.");
            }

            if (s_ignoredOptions.Contains(prefix))
            {
                continue;
            }

            if (!s_options.TryGetValue(prefix, out string? normalizedOption))
            {
                if (IsValidModernShortOption(arg[0], optionName, value, modernShortOptions))
                {
                    result.Add(arg);
                    continue;
                }

                throw new ArgumentException($"Invalid native option '{arg}'.");
            }

            if (normalizedOption == "--metadata")
            {
                result.Add("--metadata");
                if (value is not null)
                {
                    string filter = value.TrimStart();
                    if (filter.Length == 0)
                    {
                        throw new ArgumentException($"Invalid native option '{arg}'.");
                    }

                    result.Add("--metadata-filter");
                    result.Add(filter);
                }

                continue;
            }

            if (!optionsWithValues.Contains(normalizedOption))
            {
                result.Add(normalizedOption);
                continue;
            }

            string normalizedValue = value?.TrimStart() ?? string.Empty;
            if (normalizedValue.Length == 0)
            {
                throw new ArgumentException($"Invalid native option '{arg}'.");
            }

            // The native tool writes to the console for -OUT=CON.
            if (normalizedOption == "--output" && normalizedValue.Equals("CON", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            result.Add(normalizedOption);
            result.Add(normalizedValue);
        }

        return result.ToArray();

        void AddModernOption(string name, Option option)
        {
            if (!name.StartsWith('-', StringComparison.Ordinal))
            {
                return;
            }

            if (option.Arity.MinimumNumberOfValues > 0)
            {
                optionsWithValues.Add(name);
            }

            AddModernShortOption(name, option);
        }

        void AddModernShortOption(string name, Option option)
        {
            if (name is ['-', _])
            {
                modernShortOptions.Add(name, option);
            }
        }
    }

    private static bool TrySplitNativeOption(
        string arg,
        bool allowSlash,
        out string optionName,
        out string? value)
    {
        optionName = string.Empty;
        value = null;

        if (arg.Length < 2 ||
            arg.StartsWith("--", StringComparison.Ordinal) ||
            (arg[0] != '-' && (!allowSlash || arg[0] != '/')))
        {
            return false;
        }

        ReadOnlySpan<char> option = arg.AsSpan(1);
        int separatorIndex = option.IndexOfAny('=', ':');
        if (separatorIndex >= 0)
        {
            optionName = option[..separatorIndex].ToString();
            value = option[(separatorIndex + 1)..].ToString();
        }
        else
        {
            optionName = option.ToString();
        }

        return true;
    }

    private static bool IsValidModernShortOption(
        char prefix,
        string optionName,
        string? value,
        Dictionary<string, Option> modernShortOptions)
    {
        if (prefix != '-' || optionName.Length != 1)
        {
            return false;
        }

        if (!modernShortOptions.TryGetValue($"-{optionName}", out Option? option))
        {
            return false;
        }

        if (value is null)
        {
            return true;
        }

        return option.ValueType == typeof(bool)
            ? bool.TryParse(value, out _)
            : option.Arity.MaximumNumberOfValues > 0;
    }
}
