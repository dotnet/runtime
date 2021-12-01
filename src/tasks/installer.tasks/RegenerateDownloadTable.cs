// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.DotNet.Build.Tasks
{
    public class RegenerateDownloadTable : BuildTask
    {
        private const string TableComment = "generated table";
        private const string LinksComment = "links to include in table";

        /// <summary>
        /// A file that contains a Markdown table and a list of links. This task reads the
        /// "links to include in table" section to find available links, then updates the
        /// "generated table" section to include a Markdown table. Cells in the table are generated
        /// by looking for links that apply to the current combination of platform and branch.
        ///
        /// The sections are marked by one-line html comments:
        ///
        /// <!-- BEGIN <section name> -->
        /// ...
        /// <!-- END <section name> -->
        /// </summary>
        [Required]
        public string ReadmeFile { get; set; }

        /// <summary>
        /// %(Identity): Name of this branch, as appears in the column header.
        /// %(Abbr): Abbreviation of this branch, used to match up with link names.
        /// </summary>
        [Required]
        public ITaskItem[] Branches { get; set; }

        /// <summary>
        /// %(Identity): Name of this platform, as appears in bold as the first column of the row.
        /// %(Parenthetical): An extra non-bold string to add after the platform name.
        /// %(Abbr): Abbreviation of this platform, used to match up with link names.
        /// </summary>
        [Required]
        public ITaskItem[] Platforms { get; set; }


        private string Begin(string marker) => $"<!-- BEGIN {marker} -->";
        private string End(string marker) => $"<!-- END {marker} -->";


        public override bool Execute()
        {
            string[] readmeLines = File.ReadAllLines(ReadmeFile);

            if (readmeLines.Contains(Begin(LinksComment)) &&
                readmeLines.Contains(End(LinksComment)))
            {
                // In the links section, extract the name of each reference-style Markdown link.
                // For example, grabs 'win-x86-badge-2.1.X' from
                // [win-x86-badge-2.1.X]: https://example.org/foo
                string[] links = readmeLines
                    .SkipWhile(line => line != Begin(LinksComment))
                    .Skip(1)
                    .TakeWhile(line => line != End(LinksComment))
                    .Where(line => line.StartsWith("[") && line.Contains("]:"))
                    .Select(line => line.Substring(
                        1,
                        line.IndexOf("]:", StringComparison.Ordinal) - 1))
                    .ToArray();

                string[] rows = Platforms.Select(p => CreateRow(p, links)).ToArray();

                // Final table to write to the file, with a newline before and after.
                string[] table = new[]
                {
                    "",
                    $"| Platform |{string.Concat(Branches.Select(p => $" {p.ItemSpec} |"))}",
                    $"| --- | {string.Concat(Enumerable.Repeat(" :---: |", Branches.Length))}"
                }.Concat(rows).Concat(new[]
                {
                    ""
                }).ToArray();

                if (readmeLines.Contains(Begin(TableComment)) &&
                    readmeLines.Contains(End(TableComment)))
                {
                    string[] beforeTable = readmeLines
                        .TakeWhile(line => line != Begin(TableComment))
                        .Concat(new[] { Begin(TableComment) })
                        .ToArray();

                    string[] afterTable = readmeLines
                        .Skip(beforeTable.Length)
                        .SkipWhile(line => line != End(TableComment))
                        .ToArray();

                    File.WriteAllLines(
                        ReadmeFile,
                        beforeTable.Concat(table).Concat(afterTable));
                }
                else
                {
                    Log.LogError($"Readme '{ReadmeFile}' has no '{TableComment}' section.");
                }
            }
            else
            {
                Log.LogError($"Readme '{ReadmeFile}' has no '{LinksComment}' section.");
            }

            return !Log.HasLoggedErrors;
        }

        private string CreateRow(ITaskItem platform, string[] links)
        {
            string parenthetical = platform.GetMetadata("Parenthetical");

            string cells = string.Concat(
                Branches.Select(branch => $" {CreateCell(platform, branch, links)} |"));

            return $"| **{platform.ItemSpec}**{parenthetical} |{cells}";
        }

        private string CreateCell(ITaskItem platform, ITaskItem branch, string[] links)
        {
            string branchAbbr = branch.GetMetadata("Abbr");
            if (string.IsNullOrEmpty(branchAbbr))
            {
                Log.LogError($"Branch '{branch.ItemSpec}' has no Abbr metadata.");
            }

            string platformAbbr = platform.GetMetadata("Abbr");
            if (string.IsNullOrEmpty(platformAbbr))
            {
                Log.LogError($"Platform '{platform.ItemSpec}' has no Abbr metadata.");
            }

            var sb = new StringBuilder();

            string Link(string type) => $"{platformAbbr}-{type}-{branchAbbr}";

            void AddLink(string name, string type)
            {
                string link = Link(type);
                string checksum = Link($"{type}-checksum");

                if (links.Contains(link))
                {
                    sb.Append("<br>");
                    sb.Append($"[{name}][{link}]");
                    if (links.Contains(checksum))
                    {
                        sb.Append($" ([Checksum][{checksum}])");
                    }
                }
            }

            string badge = Link("badge");
            string version = Link("version");

            // if (links.Contains(badge) && links.Contains(version))
            // {
            //     sb.Append($"[![][{badge}]][{version}]");
            // }

            // Look for various types of links. The first parameter is the name of the link as it
            // appears in the table cell. The second parameter is how this type of link is
            // abbreviated in the link section. A generic checksum link is added for any of these
            // that also have a '<type>-checksum' link.

            AddLink("Installer", "installer");

            AddLink("Runtime-Deps", "runtime-deps");
            AddLink("Host", "host");
            AddLink("App Hosts", "apphost-pack");
            AddLink("Host FX Resolver", "hostfxr");
            AddLink("Targeting Pack", "targeting-pack");
            AddLink("Shared Framework", "sharedfx");

            AddLink("zip", "zip");
            AddLink("tar.gz", "targz");

            // AddLink("NetHost (zip)", "nethost-zip");
            // AddLink("NetHost (tar.gz)", "nethost-targz");

            // AddLink("Symbols (zip)", "symbols-zip");
            // AddLink("Symbols (tar.gz)", "symbols-targz");

            if (sb.Length == 0)
            {
                sb.Append("N/A");
            }

            return sb.ToString();
        }
    }
}
