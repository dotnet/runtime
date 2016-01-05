using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                using (var jsonWriter = new JsonTextWriter(writer))
                {
                    Write(context).WriteTo(jsonWriter);
                }
            }
        }

        private JObject Write(DependencyContext context)
        {
            return new JObject(
                new JProperty(DependencyContextStrings.CompilationOptionsPropertName, WriteCompilationOptions(context.CompilationOptions)),
                new JProperty(DependencyContextStrings.TargetsPropertyName, WriteTargets(context)),
                new JProperty(DependencyContextStrings.LibrariesPropertyName, WriteLibraries(context))
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
            if (compilationOptions.EmitEntryPoint != null)
            {
                o[DependencyContextStrings.EmitEntryPointPropertyName] = compilationOptions.EmitEntryPoint;
            }
            return o;
        }

        private JObject WriteTargets(DependencyContext context)
        {
            return new JObject(
                new JProperty(context.Target, WriteTarget(context.CompileLibraries, false)),
                new JProperty(context.Target + DependencyContextStrings.VersionSeperator + context.Runtime,
                    WriteTarget(context.RuntimeLibraries, true))
                );
        }

        private JObject WriteTarget(IReadOnlyList<Library> libraries, bool runtime)
        {
            return new JObject(
                libraries.Select(library =>
                    new JProperty(library.PackageName + DependencyContextStrings.VersionSeperator + library.Version, WriteTargetLibrary(library, runtime))));
        }

        private JObject WriteTargetLibrary(Library library, bool runtime)
        {
            return new JObject(
                new JProperty(DependencyContextStrings.DependenciesPropertyName, WriteDependencies(library.Dependencies)),
                new JProperty(runtime ? DependencyContextStrings.RunTimeAssembliesKey : DependencyContextStrings.CompileTimeAssembliesKey,
                    WriteAssemblies(library.Assemblies))
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
                context.RuntimeLibraries.Concat(context.CompileLibraries)
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