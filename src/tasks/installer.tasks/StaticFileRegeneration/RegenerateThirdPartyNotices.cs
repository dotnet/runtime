// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Build.Tasks
{
    public class RegenerateThirdPartyNotices : BuildTask
    {
        private const string GitHubRawContentBaseUrl = "https://raw.githubusercontent.com/";

        private static readonly char[] NewlineChars = { '\n', '\r' };

        /// <summary>
        /// The Third Party Notices file (TPN file) to regenerate.
        /// </summary>
        [Required]
        public string TpnFile { get; set; }

        /// <summary>
        /// Potential names for the file in various repositories. Each one is tried for each repo.
        /// </summary>
        [Required]
        public string[] PotentialTpnPaths { get; set; }

        /// <summary>
        /// %(Identity): The "{organization}/{name}" of a repo to gather TPN info from.
        /// %(Branch): The branch to pull from.
        /// </summary>
        [Required]
        public ITaskItem[] TpnRepos { get; set; }

        public override bool Execute()
        {
            using (var client = new HttpClient())
            {
                ExecuteAsync(client).Wait();
            }

            return !Log.HasLoggedErrors;
        }

        public async Task ExecuteAsync(HttpClient client)
        {
            var results = await Task.WhenAll(TpnRepos
                .SelectMany(item =>
                {
                    string repo = item.ItemSpec;
                    string branch = item.GetMetadata("Branch")
                        ?? throw new ArgumentException($"{item.ItemSpec} specifies no Branch.");

                    return PotentialTpnPaths.Select(path => new
                    {
                        Repo = repo,
                        Branch = branch,
                        PotentialPath = path,
                        Url = $"{GitHubRawContentBaseUrl}{repo}/{branch}/{path}"
                    });
                })
                .Select(async c =>
                {
                    TpnDocument content = null;

                    Log.LogMessage(
                        MessageImportance.High,
                        $"Getting {c.Url}");

                    HttpResponseMessage response = await client.GetAsync(c.Url);

                    if (response.StatusCode != HttpStatusCode.NotFound)
                    {
                        response.EnsureSuccessStatusCode();

                        string tpnContent = await response.Content.ReadAsStringAsync();

                        try
                        {
                            content = TpnDocument.Parse(tpnContent.Split(NewlineChars));
                        }
                        catch
                        {
                            Log.LogError($"Failed to parse response from {c.Url}");
                            throw;
                        }

                        Log.LogMessage($"Got content from URL: {c.Url}");
                    }
                    else
                    {
                        Log.LogMessage($"Checked for content, but does not exist: {c.Url}");
                    }

                    return new
                    {
                        c.Repo,
                        c.Branch,
                        c.PotentialPath,
                        c.Url,
                        Content = content
                    };
                }));

            foreach (var r in results.Where(r => r.Content != null).OrderBy(r => r.Repo))
            {
                Log.LogMessage(
                    MessageImportance.High,
                    $"Found TPN: {r.Repo} [{r.Branch}] {r.PotentialPath}");
            }

            // Ensure we found one (and only one) TPN file for each repo.
            foreach (var miscount in results
                .GroupBy(r => r.Repo)
                .Where(g => g.Count(r => r.Content != null) != 1))
            {
                Log.LogError($"Unable to find exactly one TPN for {miscount.Key}");
            }

            if (Log.HasLoggedErrors)
            {
                return;
            }

            TpnDocument existingTpn = TpnDocument.Parse(File.ReadAllLines(TpnFile));

            Log.LogMessage(
                MessageImportance.High,
                $"Existing TPN file preamble: {existingTpn.Preamble.Substring(0, 10)}...");

            foreach (var s in existingTpn.Sections.OrderBy(s => s.Header.SingleLineName))
            {
                Log.LogMessage(
                    MessageImportance.High,
                    $"{s.Header.StartLine + 1}:{s.Header.StartLine + s.Header.LineLength} {s.Header.Format} '{s.Header.SingleLineName}'");
            }

            TpnDocument[] otherTpns = results
                .Select(r => r.Content)
                .Where(r => r != null)
                .ToArray();

            TpnSection[] newSections = otherTpns
                .SelectMany(o => o.Sections)
                .Except(existingTpn.Sections, new TpnSection.ByHeaderNameComparer())
                .OrderBy(s => s.Header.Name)
                .ToArray();

            foreach (TpnSection existing in results
                .SelectMany(r => (r.Content?.Sections.Except(newSections)).NullAsEmpty())
                .Where(s => !newSections.Contains(s))
                .OrderBy(s => s.Header.Name))
            {
                Log.LogMessage(
                    MessageImportance.High,
                    $"Found already-imported section: '{existing.Header.SingleLineName}'");
            }

            foreach (var s in newSections)
            {
                Log.LogMessage(
                    MessageImportance.High,
                    $"New section to import: '{s.Header.SingleLineName}' of " +
                    string.Join(
                        ", ",
                        results
                            .Where(r => r.Content?.Sections.Contains(s) == true)
                            .Select(r => r.Url)) +
                    $" line {s.Header.StartLine}");
            }

            Log.LogMessage(MessageImportance.High, $"Importing {newSections.Length} sections...");

            var newTpn = new TpnDocument
            {
                Preamble = existingTpn.Preamble,
                Sections = existingTpn.Sections.Concat(newSections)
            };

            File.WriteAllText(TpnFile, newTpn.ToString());

            Log.LogMessage(MessageImportance.High, $"Wrote new TPN contents to {TpnFile}.");
        }
    }
}
