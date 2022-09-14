// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Sharpliner.AzureDevOps;
using Sharpliner.AzureDevOps.ConditionedExpressions;

namespace Pipelines;

public abstract class PlatformMatrixBase : JobTemplateDefinition
{
    // Fill these three in an ancestor to generate a list of classes
    protected abstract List<string> AllowedPlatforms { get; }
    protected abstract List<string> DisallowedPlatforms { get; }
    protected abstract List<Platform> Platforms { get; }

    public override string[]? Header => base.Header!.Concat(new[]
    {
        string.Empty,
        "You can add platform filters to PlatformMatrix.cs and only keep platforms you care about in your PR",
    }).ToArray();

    public override List<Parameter> Parameters => new()
    {
        StringParameter("runtimeFlavor", "coreclr", new[] { "coreclr", "mono" }),
        JobParameter("jobTemplate"),
        StringParameter("buildConfig", string.Empty),
        ObjectParameter("platforms"),

        // platformGroup is a named collection of platforms.
        StringParameter("platformGroup", string.Empty, new[]
        {
            "all",     // all platforms
            "gcstress" // platforms that support running under GCStress0x3 and GCStress0xC scenarios
        }),

        StringParameter("helixQueueGroup", "pr", new[]
        {
            "pr",
            "ci",
            "all"
        }),

        // helixQueuesTemplate is a yaml template which will be expanded in order to set up the helix queues
        // for the given platform and helixQueueGroup.
        StringParameter("helixQueuesTemplate", string.Empty),
        BooleanParameter("stagedBuild", false),

        // When set to false, suppresses reuse of OSX managed build artifacts (for pipelines without an OSX obj)
        // When set to true, passes the 'platforms' value as a job parameter also named 'platforms'.
        // Handled as an opt-in parameter to avoid excessive yaml.
        BooleanParameter("passPlatforms", false),
        StringParameter("container"),
        BooleanParameter("shouldContinueOnError", false),
        ObjectParameter("jobParameters"),
        ObjectParameter("variables"),
    };

    public override ConditionedList<JobBase> Definition
    {
        get
        {
            var list = new ConditionedList<JobBase>();

            foreach (Conditioned<JobBase> job in Platforms.Where(IsAllowed).Select(CreateTemplate))
            {
                list.Add(job);
            }

            return list;
        }
    }

    private bool IsAllowed(Platform platform)
    {
        static bool ListContains(IEnumerable<string> substrings, string? haystack)
        {
            if (haystack is null)
            {
                return false;
            }

            haystack = haystack.ToLowerInvariant();
            return substrings.Any(substring => haystack.Contains(substring.ToLowerInvariant()));
        }

        bool isAllowed = true;

        if (AllowedPlatforms.Count > 0
            && !ListContains(AllowedPlatforms, platform.Name)
            && !ListContains(AllowedPlatforms, platform.TargetRid))
        {
            isAllowed = false;
        }

        if (ListContains(DisallowedPlatforms, platform.Name)
            || ListContains(DisallowedPlatforms, platform.TargetRid))
        {
            isAllowed = false;
        }

        return isAllowed;
    }

    private Conditioned<JobBase> CreateTemplate(Platform platform)
    {
        var templateParameters = new TemplateParameters
        {
            { "jobTemplate", parameters["jobTemplate"] },
            { "helixQueuesTemplate", parameters["helixQueuesTemplate"] },
            { "variables", parameters["variables"] },
            { "osGroup", platform.OsGroup },
            { "archType", platform.Architecture },
            { "targetRid", platform.TargetRid ?? platform.OsGroup.ToLowerInvariant() + platform.OsSubGroup?.Replace("_", "-") + "-" + platform.Architecture.ToLowerInvariant() },
            { "platform", platform.PlatformId ?? platform.OsGroup + platform.OsSubGroup + "_" + platform.Architecture },
            { "shouldContinueOnError", parameters["shouldContinueOnError"] }
        };

        if (platform.OsSubGroup != null)
        {
            templateParameters["osSubgroup"] = platform.OsSubGroup;
        }

        if (platform.Container != null)
        {
            templateParameters["container"] = platform.Container is string name
                ? new TemplateParameters
                {
                    { "image", name },
                    { "registry", "mcr" },
                }
                : platform.Container;
        }

        var jobParameters = new TemplateParameters
        {
            { "runtimeFlavor", platform.RuntimeFlavor ?? parameters["runtimeFlavor"] },
            { "stagedBuild", parameters["stagedBuild"] },
            { "buildConfig", parameters["buildConfig"] },
            { "helixQueueGroup", parameters["helixQueueGroup"] },
        };

        if (platform.HostedOs != null)
        {
            jobParameters["hostedOs"] = platform.HostedOs;
        }

        if (platform.CrossBuild)
        {
            jobParameters["crossBuild"] = true;
        }

        if (platform.CrossRootFsDir != null)
        {
            jobParameters["crossrootfsDir"] = platform.CrossRootFsDir;
        }

        if (platform.AdditionalJobParams != null)
        {
            foreach (KeyValuePair<string, object> pair in platform.AdditionalJobParams)
            {
                jobParameters[pair.Key] = pair.Value;
            }
        }

        jobParameters["${{ insert }}"] = parameters["jobParameters"];

        templateParameters["jobParameters"] = jobParameters;

        return platform.RunForPlatforms != null
            ? If.Or(ContainsValue(platform.Name, parameters["platforms"]),
                    In(parameters["platformGroup"], platform.RunForPlatforms.ToArray()))
                .JobTemplate("xplat-setup.yml", templateParameters)

            : If.ContainsValue(platform.Name, parameters["platforms"])
                 .JobTemplate("xplat-setup.yml", templateParameters);
    }
}
