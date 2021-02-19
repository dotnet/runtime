// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Logging
{
    internal static class LoggerRuleSelector
    {
        public static void Select(LoggerFilterOptions options, Type providerType, string category, out LogLevel? minLevel, out Func<string, string, LogLevel, bool> filter)
        {
            filter = null;
            minLevel = options.MinLevel;

            // Filter rule selection:
            // 1. Select rules for current logger type, if there is none, select ones without logger type specified
            // 2. Select rules with longest matching categories
            // 3. If there nothing matched by category take all rules without category
            // 3. If there is only one rule use it's level and filter
            // 4. If there are multiple rules use last
            // 5. If there are no applicable rules use global minimal level

            string providerAlias = ProviderAliasUtilities.GetAlias(providerType);
            LoggerFilterRule current = null;
            foreach (LoggerFilterRule rule in options.Rules)
            {
                if (IsBetter(rule, current, providerType.FullName, category)
                    || (!string.IsNullOrEmpty(providerAlias) && IsBetter(rule, current, providerAlias, category)))
                {
                    current = rule;
                }
            }

            if (current != null)
            {
                filter = current.Filter;
                minLevel = current.LogLevel;
            }
        }

        private static bool IsBetter(LoggerFilterRule rule, LoggerFilterRule current, string logger, string category)
        {
            // Skip rules with inapplicable type or category
            if (rule.ProviderName != null && rule.ProviderName != logger)
            {
                return false;
            }

            string categoryName = rule.CategoryName;
            if (categoryName != null)
            {
                const char WildcardChar = '*';

                int wildcardIndex = categoryName.IndexOf(WildcardChar);
                if (wildcardIndex != -1 &&
                    categoryName.IndexOf(WildcardChar, wildcardIndex + 1) != -1)
                {
                    throw new InvalidOperationException(SR.MoreThanOneWildcard);
                }

                ReadOnlySpan<char> prefix, suffix;
                if (wildcardIndex == -1)
                {
                    prefix = categoryName.AsSpan();
                    suffix = default;
                }
                else
                {
                    prefix = categoryName.AsSpan(0, wildcardIndex);
                    suffix = categoryName.AsSpan(wildcardIndex + 1);
                }

                if (!category.AsSpan().StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                    !category.AsSpan().EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (current?.ProviderName != null)
            {
                if (rule.ProviderName == null)
                {
                    return false;
                }
            }
            else
            {
                // We want to skip category check when going from no provider to having provider
                if (rule.ProviderName != null)
                {
                    return true;
                }
            }

            if (current?.CategoryName != null)
            {
                if (rule.CategoryName == null)
                {
                    return false;
                }

                if (current.CategoryName.Length > rule.CategoryName.Length)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
