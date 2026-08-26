// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace ILAssembler;

internal static class NativeCommandLine
{
    private static readonly Dictionary<string, string> s_options = new(StringComparer.OrdinalIgnoreCase)
    {
        ["32B"] = "--32bitpreferred",
        ["ALI"] = "--alignment",
        ["ANA"] = "--aname",
        ["APP"] = "--appcontainer",
        ["BAS"] = "--base",
        ["CLO"] = "--clock",
        ["DEB"] = "--debug",
        ["DET"] = "--deterministic",
        ["DLL"] = "--dll",
        ["ERR"] = "--error",
        ["EXE"] = "--exe",
        ["FLA"] = "--flags",
        ["FOL"] = "--fold",
        ["HIG"] = "--highentropyva",
        ["INC"] = "--include",
        ["KEY"] = "--key",
        ["MDV"] = "--mdv",
        ["NOA"] = "--noautoinherit",
        ["NOC"] = "--nocorstub",
        ["NOL"] = "--nologo",
        ["OPT"] = "--optimize",
        ["OUT"] = "--output",
        ["PDB"] = "--pdb",
        ["PE6"] = "--pe64",
        ["QUI"] = "--quiet",
        ["STA"] = "--stack",
        ["STR"] = "--stripreloc",
        ["SSV"] = "--ssver",
        ["SUB"] = "--subsystem",
        ["X64"] = "--x64",
    };

    private static readonly HashSet<string> s_optionsWithValues = new(StringComparer.Ordinal)
    {
        "--alignment",
        "--aname",
        "--base",
        "--flags",
        "--include",
        "--key",
        "--mdv",
        "--output",
        "--ssver",
        "--stack",
        "--subsystem",
    };

    private static readonly HashSet<string> s_modernOptionsWithSeparateValues = new(StringComparer.Ordinal)
    {
        "--alignment",
        "--aname",
        "--base",
        "--debug-mode",
        "--flags",
        "--include",
        "--key",
        "--mdv",
        "--output",
        "--ssver",
        "--stack",
        "--subsystem",
        "-I",
        "-k",
        "-o",
    };

    internal static string[] Normalize(string[] args) =>
        Normalize(args, allowSlashOptions: OperatingSystem.IsWindows());

    internal static string[] Normalize(string[] args, bool allowSlashOptions)
    {
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

            if (s_modernOptionsWithSeparateValues.Contains(arg))
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
                if (!IsValidModernShortOption(arg[0], optionName, value))
                {
                    throw new ArgumentException($"Invalid native option '{arg}'.");
                }

                result.Add(arg);
                continue;
            }

            string? normalizedOption;
            if (optionName.StartsWith("ARM64", StringComparison.OrdinalIgnoreCase))
            {
                normalizedOption = "--arm64";
            }
            else if (optionName.Equals("ARM", StringComparison.OrdinalIgnoreCase))
            {
                if (value is not null)
                {
                    throw new ArgumentException($"Invalid native option '{arg}'.");
                }

                normalizedOption = "--arm";
            }
            else if (optionName.StartsWith("ARM", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Invalid native option '{arg}'.");
            }
            else
            {
                string prefix = optionName[..3];
                if (prefix.Equals("RES", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(
                        $"Unsupported native option '{arg}'. The managed ilasm implementation does not support -RESOURCES.");
                }
                if (prefix.Equals("MSV", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(
                        $"Unsupported native option '{arg}'. The managed ilasm implementation does not support -MSV.");
                }
                if (prefix.Equals("LIS", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                s_options.TryGetValue(prefix, out normalizedOption);
            }

            if (normalizedOption is null)
            {
                if (IsValidModernShortOption(arg[0], optionName, value))
                {
                    result.Add(arg);
                    continue;
                }

                throw new ArgumentException($"Invalid native option '{arg}'.");
            }

            if (normalizedOption == "--debug" && value is not null)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException($"Invalid native option '{arg}'.");
                }

                result.Add("--debug");
                result.Add("--debug-mode");
                result.Add(NormalizeDebugMode(value));
                continue;
            }

            result.Add(normalizedOption);
            if (s_optionsWithValues.Contains(normalizedOption))
            {
                if (value is null)
                {
                    throw new ArgumentException($"Invalid native option '{arg}'.");
                }

                string normalizedValue = normalizedOption == "--aname" ? value : value.TrimStart();
                if (normalizedValue.Length == 0 && normalizedOption != "--aname")
                {
                    throw new ArgumentException($"Invalid native option '{arg}'.");
                }

                result.Add(normalizedValue);
            }
        }

        return result.ToArray();
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

    private static bool IsValidModernShortOption(char prefix, string optionName, string? value)
    {
        if (prefix != '-' || optionName.Length != 1)
        {
            return false;
        }

        return optionName switch
        {
            "I" or "k" or "o" => true,
            "O" or "g" or "q" => value is null || bool.TryParse(value, out _),
            "h" => value is null,
            _ => false,
        };
    }

    private static string NormalizeDebugMode(string value)
    {
        string trimmedValue = value.TrimStart();
        if (trimmedValue.StartsWith("IMP", StringComparison.OrdinalIgnoreCase))
        {
            return nameof(DebugMode.Impl);
        }

        if (trimmedValue.StartsWith("OPT", StringComparison.OrdinalIgnoreCase))
        {
            return nameof(DebugMode.Opt);
        }

        return trimmedValue;
    }
}
