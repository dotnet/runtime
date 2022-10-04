// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.Interop.Analyzers
{
    internal static class FixAllContextExtensions
    {
        public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsInScopeAsync(this FixAllContext context)
        {
            switch (context.Scope)
            {
                case FixAllScope.Document:
                    return await context.GetDocumentDiagnosticsAsync(context.Document).ConfigureAwait(false);
                case FixAllScope.Project:
                    return await context.GetAllDiagnosticsAsync(context.Project).ConfigureAwait(false);
                case FixAllScope.Solution:
                    Solution solution = context.Solution;
                    ProjectDependencyGraph dependencyGraph = solution.GetProjectDependencyGraph();

                    // Walk through each project in topological order, determining and applying the diagnostics for each
                    // project.  We do this in topological order so that the compilations for successive projects are readily
                    // available as we just computed them for dependent projects.  If we were to do it out of order, we might
                    // start with a project that has a ton of dependencies, and we'd spend an inordinate amount of time just
                    // building the compilations for it before we could proceed.
                    //
                    // By processing one project at a time, we can also let go of a project once done with it, allowing us to
                    // reclaim lots of the memory so we don't overload the system while processing a large solution.
                    //
                    // Note: we have to filter down to projects of the same language as the FixAllContext points at a
                    // CodeFixProvider, and we can't call into providers of different languages with diagnostics from a
                    // different language.
                    IEnumerable<Project?> sortedProjects = dependencyGraph.GetTopologicallySortedProjects(context.CancellationToken)
                                                        .Select(solution.GetProject)
                                                        .Where(p => p.Language == context.Project.Language);
                    return (await Task.WhenAll(sortedProjects.Select(context.GetAllDiagnosticsAsync)).ConfigureAwait(false)).SelectMany(diag => diag).ToImmutableArray();
                default:
                    return ImmutableArray<Diagnostic>.Empty;
            }
        }

        public static async Task<ImmutableArray<Project>> GetProjectsWithDiagnosticsAsync(this FixAllContext context)
        {
            switch (context.Scope)
            {
                case FixAllScope.ContainingMember:
                case FixAllScope.ContainingType:
                case FixAllScope.Document:
                case FixAllScope.Project:
                    return ImmutableArray.Create(context.Project);
                case FixAllScope.Solution:
                    ImmutableArray<Project>.Builder projectsWithDiagnostics = ImmutableArray.CreateBuilder<Project>();
                    foreach (var project in context.Solution.Projects)
                    {
                        ImmutableArray<Diagnostic> diagnostics = await context.GetAllDiagnosticsAsync(project).ConfigureAwait(false);
                        if (diagnostics.Length != 0)
                        {
                            projectsWithDiagnostics.Add(project);
                        }
                    }
                    return projectsWithDiagnostics.ToImmutable();
                default:
                    return ImmutableArray<Project>.Empty;
            }
        }
    }
}
