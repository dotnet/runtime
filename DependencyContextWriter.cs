// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Extensions.DependencyModel
{
    public class DependencyContextWriter
    {
        public void Write(DependencyContext context, Stream stream)
        {
            using (var writer = new StreamWriter(stream))
            {
                using (var jsonWriter = new JsonTextWriter(writer) { Formatting = Formatting.Indented })
                {
                    Write(context).WriteTo(jsonWriter);
                }
            }
        }

        private JObject Write(DependencyContext context)
        {
            return new JObject(
                new JProperty(DependencyContextStrings.RuntimeTargetPropertyName, WriteRuntimeTargetInfo(context)),
                new JProperty(DependencyContextStrings.CompilationOptionsPropertName, WriteCompilationOptions(context.CompilationOptions)),
                new JProperty(DependencyContextStrings.TargetsPropertyName, WriteTargets(context)),
                new JProperty(DependencyContextStrings.LibrariesPropertyName, WriteLibraries(context)),
                new JProperty(DependencyContextStrings.RuntimesPropertyName, WriteRuntimeGraph(context))
                );
        }

        private JObject WriteRuntimeTargetInfo(DependencyContext context)
        {
            var target = context.IsPortable?
                context.Target :
                context.Target + DependencyContextStrings.VersionSeperator + context.Runtime;

            return new JObject(
                    new JProperty(DependencyContextStrings.RuntimeTargetNamePropertyName, target),
                    new JProperty(DependencyContextStrings.PortablePropertyName, context.IsPortable)
                );
        }

        private JObject WriteRuntimeGraph(DependencyContext context)
        {
            return new JObject(
                    new JProperty(context.Target,
                        new JObject(
                            context.RuntimeGraph.Select(g => new JProperty(g.Key, new JArray(g.Value)))
                            )
                    )
                );
        }

        private JObject WriteCompilationOptions(CompilationOptions compilationOptions)
        {
            var o = new JObject();
            if (compilationOptions.Defines != null)
            {
                o[DependencyContextStrings.DefinesPropertyName] = new JArray(compilationOptions.Defines);
            }
            if (compilationOptions.LanguageVersion != null)
            {
                o[DependencyContextStrings.LanguageVersionPropertyName] = compilationOptions.LanguageVersion;
            }
            if (compilationOptions.Platform != null)
            {
                o[DependencyContextStrings.PlatformPropertyName] = compilationOptions.Platform;
            }
            if (compilationOptions.AllowUnsafe != null)
            {
                o[DependencyContextStrings.AllowUnsafePropertyName] = compilationOptions.AllowUnsafe;
            }
            if (compilationOptions.WarningsAsErrors != null)
            {
                o[DependencyContextStrings.WarningsAsErrorsPropertyName] = compilationOptions.WarningsAsErrors;
            }
            if (compilationOptions.Optimize != null)
            {
                o[DependencyContextStrings.OptimizePropertyName] = compilationOptions.Optimize;
            }
            if (compilationOptions.KeyFile != null)
            {
                o[DependencyContextStrings.KeyFilePropertyName] = compilationOptions.KeyFile;
            }
            if (compilationOptions.DelaySign != null)
            {
                o[DependencyContextStrings.DelaySignPropertyName] = compilationOptions.DelaySign;
            }
            if (compilationOptions.PublicSign != null)
            {
                o[DependencyContextStrings.PublicSignPropertyName] = compilationOptions.PublicSign;
            }
            if (compilationOptions.DebugType != null)
            {
                o[DependencyContextStrings.DebugTypePropertyName] = compilationOptions.DebugType;
            }
            if (compilationOptions.EmitEntryPoint != null)
            {
                o[DependencyContextStrings.EmitEntryPointPropertyName] = compilationOptions.EmitEntryPoint;
            }
            return o;
        }

        private JObject WriteTargets(DependencyContext context)
        {
            if (context.IsPortable)
            {
                return new JObject(
                    new JProperty(context.Target, WriteTarget(context.CompileLibraries)),
                    new JProperty(context.Target + DependencyContextStrings.VersionSeperator + context.Runtime,
                        WriteTarget(context.RuntimeLibraries))
                    );
            }
            else
            {
                return new JObject(
                    new JProperty(context.Target, WriteTarget(context.CompileLibraries)),
                    new JProperty(context.Target + DependencyContextStrings.VersionSeperator + context.Runtime,
                        WriteTarget(context.RuntimeLibraries))
                    );
            }
        }

        private JObject WriteTarget(IReadOnlyList<Library> libraries)
        {
            return new JObject(
                libraries.Select(library =>
                    new JProperty(library.PackageName + DependencyContextStrings.VersionSeperator + library.Version, WriteTargetLibrary(library))));
        }

        private JObject WriteTargetLibrary(Library library)
        {
            string propertyName;
            string[] assemblies;

            var runtimeLibrary = library as RuntimeLibrary;
            if (runtimeLibrary != null)
            {
                propertyName = DependencyContextStrings.RuntimeAssembliesKey;
                assemblies = runtimeLibrary.Assemblies.Select(assembly => assembly.Path).ToArray();
            }
            else
            {
                var compilationLibrary = library as CompilationLibrary;
                if (compilationLibrary != null)
                {
                    propertyName = DependencyContextStrings.CompileTimeAssembliesKey;
                    assemblies = compilationLibrary.Assemblies.ToArray();
                }
                else
                {
                    throw new NotSupportedException();
                }
            }


            return new JObject(
                new JProperty(DependencyContextStrings.DependenciesPropertyName, WriteDependencies(library.Dependencies)),
                new JProperty(propertyName,
                    WriteAssemblies(assemblies))
                );
        }
        private JObject WritePortableTargetLibrary(RuntimeLibrary compilationLibrary, CompilationLibrary runtimeLibrary)
        {
            var libraryObject = new JObject();

            string propertyName;
            string[] assemblies;

            if (runtimeLibrary != null)
            {
                propertyName = DependencyContextStrings.RuntimeAssembliesKey;
                assemblies = runtimeLibrary.Assemblies.Select(assembly => assembly.Path).ToArray();
            }

            RuntimeAssembly[] compilationAssemblies;
            if (compilationLibrary != null)
            {
                propertyName = DependencyContextStrings.CompileTimeAssembliesKey;
                compilationAssemblies = compilationLibrary.Assemblies.ToArray();
            }
            else
            {
                throw new NotSupportedException();
            }
            }


            return new JObject(
                new JProperty(DependencyContextStrings.DependenciesPropertyName, WriteDependencies(library.Dependencies)),
                new JProperty(propertyName,
                    WriteAssemblies(assemblies))
                );
        }

        private JObject WriteAssemblies(IReadOnlyList<string> assemblies)
        {
            return new JObject(assemblies.Select(assembly => new JProperty(assembly, new JObject())));
        }

        private JObject WriteDependencies(IReadOnlyList<Dependency> dependencies)
        {
            return new JObject(
                dependencies.Select(dependency => new JProperty(dependency.Name, dependency.Version))
                );
        }

        private JObject WriteLibraries(DependencyContext context)
        {
            var allLibraries =
                context.RuntimeLibraries.Cast<Library>().Concat(context.CompileLibraries)
                    .GroupBy(library => library.PackageName + DependencyContextStrings.VersionSeperator + library.Version);

            return new JObject(allLibraries.Select(libraries=> new JProperty(libraries.Key, WriteLibrary(libraries.First()))));
        }

        private JObject WriteLibrary(Library library)
        {
            return new JObject(
                new JProperty(DependencyContextStrings.TypePropertyName, library.LibraryType),
                new JProperty(DependencyContextStrings.ServiceablePropertyName, library.Serviceable),
                new JProperty(DependencyContextStrings.Sha512PropertyName, library.Hash)
                );
        }
    }
}
