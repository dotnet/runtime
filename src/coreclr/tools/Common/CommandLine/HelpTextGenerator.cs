// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;

namespace Internal.CommandLine
{
    internal static class HelpTextGenerator
    {
        public static string Generate(ArgumentSyntax argumentSyntax, int maxWidth)
        {
            var forCommandList = argumentSyntax.ActiveCommand == null &&
                                 argumentSyntax.Commands.Any();

            var page = forCommandList
                ? GetCommandListHelp(argumentSyntax)
                : GetCommandHelp(argumentSyntax, argumentSyntax.ActiveCommand);

            var sb = new StringBuilder();
            sb.WriteHelpPage(page, maxWidth);
            return sb.ToString();
        }

        private struct HelpPage
        {
            public string ApplicationName;
            public IEnumerable<string> SyntaxElements;
            public IReadOnlyList<HelpRow> Rows;
            public IReadOnlyList<string> ExtraParagraphs;
        }

        private struct HelpRow
        {
            public string Header;
            public string Text;
        }

        private static void WriteHelpPage(this StringBuilder sb, HelpPage page, int maxWidth)
        {
            sb.WriteUsage(page.ApplicationName, page.SyntaxElements, maxWidth);

            if (!page.Rows.Any())
                return;

            sb.AppendLine();

            sb.WriteRows(page.Rows, maxWidth);

            sb.AppendLine();

            if (page.ExtraParagraphs != null)
            {
                foreach (string text in page.ExtraParagraphs)
                {
                    var words = SplitWords(text);
                    sb.WriteWordWrapped(words, 0, maxWidth);
                }
            }
        }

        private static void WriteUsage(this StringBuilder sb, string applicationName, IEnumerable<string> syntaxElements, int maxWidth)
        {
            var usageHeader = string.Format(Strings.HelpUsageOfApplicationFmt, applicationName);
            sb.Append(usageHeader);

            if (syntaxElements.Any())
                sb.Append(@" ");

            var syntaxIndent = usageHeader.Length + 1;
            var syntaxMaxWidth = maxWidth - syntaxIndent;

            sb.WriteWordWrapped(syntaxElements, syntaxIndent, syntaxMaxWidth);
        }

        private static void WriteRows(this StringBuilder sb, IReadOnlyList<HelpRow> rows, int maxWidth)
        {
            const int indent = 4;
            var maxColumnWidth = rows.Select(r => r.Header.Length).Max();
            var helpStartColumn = maxColumnWidth + 2 * indent;

            var maxHelpWidth = maxWidth - helpStartColumn;
            if (maxHelpWidth < 0)
                maxHelpWidth = maxWidth;

            foreach (var row in rows)
            {
                var headerStart = sb.Length;

                sb.Append(' ', indent);
                sb.Append(row.Header);

                var headerLength = sb.Length - headerStart;
                var requiredSpaces = helpStartColumn - headerLength;

                sb.Append(' ', requiredSpaces);

                var words = SplitWords(row.Text);
                sb.WriteWordWrapped(words, helpStartColumn, maxHelpWidth);
            }
        }

        private static void WriteWordWrapped(this StringBuilder sb, IEnumerable<string> words, int indent, int maxidth)
        {
            var helpLines = WordWrapLines(words, maxidth);
            var isFirstHelpLine = true;

            foreach (var helpLine in helpLines)
            {
                if (isFirstHelpLine)
                    isFirstHelpLine = false;
                else
                    sb.Append(' ', indent);

                sb.AppendLine(helpLine);
            }

            if (isFirstHelpLine)
                sb.AppendLine();
        }

        private static HelpPage GetCommandListHelp(ArgumentSyntax argumentSyntax)
        {
            return new HelpPage
            {
                ApplicationName = argumentSyntax.ApplicationName,
                SyntaxElements = GetGlobalSyntax(),
                Rows = GetCommandRows(argumentSyntax).ToArray(),
                ExtraParagraphs = argumentSyntax.ExtraHelpParagraphs
            };
        }

        private static HelpPage GetCommandHelp(ArgumentSyntax argumentSyntax, ArgumentCommand command)
        {
            return new HelpPage
            {
                ApplicationName = argumentSyntax.ApplicationName,
                SyntaxElements = GetCommandSyntax(argumentSyntax, command),
                Rows = GetArgumentRows(argumentSyntax, command).ToArray(),
                ExtraParagraphs = argumentSyntax.ExtraHelpParagraphs
            };
        }

        private static IEnumerable<string> GetGlobalSyntax()
        {
            yield return @"<command>";
            yield return @"[<args>]";
        }

        private static IEnumerable<string> GetCommandSyntax(ArgumentSyntax argumentSyntax, ArgumentCommand command)
        {
            if (command != null)
                yield return command.Name;

            foreach (var option in argumentSyntax.GetOptions(command).Where(o => !o.IsHidden))
                yield return GetOptionSyntax(option);

            if (argumentSyntax.GetParameters(command).All(p => p.IsHidden))
                yield break;

            if (argumentSyntax.GetOptions(command).Any(o => !o.IsHidden))
                yield return @"[--]";

            foreach (var parameter in argumentSyntax.GetParameters(command).Where(o => !o.IsHidden))
                yield return GetParameterSyntax(parameter);
        }

        private static string GetOptionSyntax(Argument option)
        {
            var sb = new StringBuilder();

            sb.Append(@"[");
            sb.Append(option.GetDisplayName());

            if (!option.IsFlag)
                sb.Append(option.IsRequired ? @" <arg>" : @" [arg]");

            if (option.IsList)
                sb.Append(@"...");

            sb.Append(@"]");

            return sb.ToString();
        }

        private static string GetParameterSyntax(Argument parameter)
        {
            var sb = new StringBuilder();

            sb.Append(parameter.GetDisplayName());
            if (parameter.IsList)
                sb.Append(@"...");

            return sb.ToString();
        }

        private static IEnumerable<HelpRow> GetCommandRows(ArgumentSyntax argumentSyntax)
        {
            return argumentSyntax.Commands
                              .Where(c => !c.IsHidden)
                              .Select(c => new HelpRow { Header = c.Name, Text = c.Help });
        }

        private static IEnumerable<HelpRow> GetArgumentRows(ArgumentSyntax argumentSyntax, ArgumentCommand command)
        {
            return argumentSyntax.GetArguments(command)
                              .Where(a => !a.IsHidden)
                              .Select(a => new HelpRow { Header = GetArgumentRowHeader(a), Text = a.Help });
        }

        private static string GetArgumentRowHeader(Argument argument)
        {
            var sb = new StringBuilder();

            foreach (var displayName in argument.GetDisplayNames())
            {
                if (sb.Length > 0)
                    sb.Append(@", ");

                sb.Append(displayName);
            }

            if (argument.IsOption && !argument.IsFlag)
                sb.Append(argument.IsRequired ? @" <arg>" : @" [arg]");

            if (argument.IsList)
                sb.Append(@"...");

            return sb.ToString();
        }

        private static IEnumerable<string> WordWrapLines(IEnumerable<string> tokens, int maxWidth)
        {
            var sb = new StringBuilder();

            foreach (var token in tokens)
            {
                var newLength = sb.Length == 0
                    ? token.Length
                    : sb.Length + 1 + token.Length;

                if (newLength > maxWidth)
                {
                    if (sb.Length == 0)
                    {
                        yield return token;
                        continue;
                    }

                    yield return sb.ToString();
                    sb.Clear();
                }

                if (sb.Length > 0)
                    sb.Append(@" ");

                sb.Append(token);
            }

            if (sb.Length > 0)
                yield return sb.ToString();
        }

        private static IEnumerable<string> SplitWords(string text)
        {
            return string.IsNullOrEmpty(text)
                ? Enumerable.Empty<string>()
                : text.Split(' ');
        }
    }
}
