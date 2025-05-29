// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Runtime.General;

using Internal.Metadata.NativeFormat;
using Internal.Reflection.Core;
using Internal.Runtime.TypeLoader;

namespace Internal.Reflection.Execution
{
    //=============================================================================================================================
    // The assembly resolution policy for emulation of "classic reflection."
    //
    // The policy is very simple: the only assemblies that can be "loaded" are those that are statically linked into the running
    // native process. There is no support for probing for assemblies in directories, user-supplied files, GACs, NICs or any
    // other repository.
    //=============================================================================================================================
    public sealed partial class AssemblyBinderImplementation : AssemblyBinder
    {
        private AssemblyBinderImplementation()
        {
            ArrayBuilder<KeyValuePair<RuntimeAssemblyName, QScopeDefinition>> scopes = default;
            foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules())
            {
                MetadataReader reader = module.MetadataReader;
                foreach (ScopeDefinitionHandle scopeDefinitionHandle in reader.ScopeDefinitions)
                {
                    scopes.Add(new KeyValuePair<RuntimeAssemblyName, QScopeDefinition>(
                        scopeDefinitionHandle.ToRuntimeAssemblyName(reader),
                        new QScopeDefinition(reader, scopeDefinitionHandle)
                        ));
                }
            }

            ScopeGroups = scopes.ToArray();
        }

        public static AssemblyBinderImplementation Instance { get; } = new AssemblyBinderImplementation();

        public sealed override bool Bind(RuntimeAssemblyName refName, bool cacheMissedLookups, out AssemblyBindResult result, out Exception exception)
        {
            bool foundMatch = false;
            result = default(AssemblyBindResult);
            exception = null;

            Exception preferredException = null;

            foreach (KeyValuePair<RuntimeAssemblyName, QScopeDefinition> group in ScopeGroups)
            {
                if (AssemblyNameMatches(refName, group.Key, ref preferredException))
                {
                    if (foundMatch)
                    {
                        exception = new AmbiguousMatchException(SR.Format(SR.AmbiguousMatchException_Assembly, refName.FullName));
                        return false;
                    }

                    foundMatch = true;
                    QScopeDefinition scopeDefinitionGroup = group.Value;

                    result.Reader = scopeDefinitionGroup.Reader;
                    result.ScopeDefinitionHandle = scopeDefinitionGroup.Handle;
                }
            }

            if (!foundMatch)
            {
                exception = preferredException ?? new FileNotFoundException(SR.Format(SR.FileNotFound_AssemblyNotFound, refName.FullName));
                return false;
            }

            return true;
        }

        public sealed override IList<AssemblyBindResult> GetLoadedAssemblies()
        {
            List<AssemblyBindResult> loadedAssemblies = new List<AssemblyBindResult>(ScopeGroups.Length);
            foreach (KeyValuePair<RuntimeAssemblyName, QScopeDefinition> group in ScopeGroups)
            {
                QScopeDefinition scopeDefinitionGroup = group.Value;

                AssemblyBindResult result = default(AssemblyBindResult);
                result.Reader = scopeDefinitionGroup.Reader;
                result.ScopeDefinitionHandle = scopeDefinitionGroup.Handle;
                loadedAssemblies.Add(result);
            }

            return loadedAssemblies;
        }

        //
        // Encapsulates the assembly ref->def matching policy.
        //
        private static bool AssemblyNameMatches(RuntimeAssemblyName refName, RuntimeAssemblyName defName, ref Exception preferredException)
        {
            //
            // The defName came from trusted metadata so it should be fully specified.
            //
            Debug.Assert(defName.Version != null);
            Debug.Assert(defName.CultureName != null);

            if (!(refName.Name.Equals(defName.Name, StringComparison.OrdinalIgnoreCase)))
                return false;

            if (refName.Version != null)
            {
                if (!AssemblyVersionMatches(refVersion: refName.Version, defVersion: defName.Version))
                {
                    preferredException = new FileLoadException(SR.Format(SR.FileLoadException_RefDefMismatch, refName.FullName, defName.Version, refName.Version));
                    return false;
                }
            }

            if (refName.CultureName != null)
            {
                if (!(refName.CultureName.Equals(defName.CultureName)))
                    return false;
            }

            // Strong names are ignored in .NET Core

            return true;
        }

        private static bool AssemblyVersionMatches(Version refVersion, Version defVersion)
        {
            if (defVersion.Major < refVersion.Major)
                return false;
            if (defVersion.Major > refVersion.Major)
                return true;

            if (defVersion.Minor < refVersion.Minor)
                return false;
            if (defVersion.Minor > refVersion.Minor)
                return true;

            if (refVersion.Build == -1)
                return true;
            if (defVersion.Build < refVersion.Build)
                return false;
            if (defVersion.Build > refVersion.Build)
                return true;

            if (refVersion.Revision == -1)
                return true;
            if (defVersion.Revision < refVersion.Revision)
                return false;

            return true;
        }

        private KeyValuePair<RuntimeAssemblyName, QScopeDefinition>[] ScopeGroups { get; }
    }
}
