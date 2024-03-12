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
            _scopeGroups = Array.Empty<KeyValuePair<RuntimeAssemblyName, ScopeDefinitionGroup>>();

            foreach (NativeFormatModuleInfo module in ModuleList.EnumerateModules())
                RegisterModule(module);
        }

        public static AssemblyBinderImplementation Instance { get; } = new AssemblyBinderImplementation();

        partial void BindEcmaFilePath(string assemblyPath, ref AssemblyBindResult bindResult, ref Exception exception, ref bool? result);
        partial void BindEcmaBytes(ReadOnlySpan<byte> rawAssembly, ReadOnlySpan<byte> rawSymbolStore, ref AssemblyBindResult bindResult, ref Exception exception, ref bool? result);
        partial void BindEcmaAssemblyName(RuntimeAssemblyName refName, bool cacheMissedLookups, ref AssemblyBindResult result, ref Exception exception, ref Exception preferredException, ref bool resultBoolean);
        partial void InsertEcmaLoadedAssemblies(List<AssemblyBindResult> loadedAssemblies);

        public sealed override bool Bind(string assemblyPath, out AssemblyBindResult bindResult, out Exception exception)
        {
            bool? result = null;
            exception = null;
            bindResult = default(AssemblyBindResult);

            BindEcmaFilePath(assemblyPath, ref bindResult, ref exception, ref result);

            // If the Ecma assembly binder isn't linked in, simply throw PlatformNotSupportedException
            if (!result.HasValue)
                throw new PlatformNotSupportedException();
            else
                return result.Value;
        }

        public sealed override bool Bind(ReadOnlySpan<byte> rawAssembly, ReadOnlySpan<byte> rawSymbolStore, out AssemblyBindResult bindResult, out Exception exception)
        {
            bool? result = null;
            exception = null;
            bindResult = default(AssemblyBindResult);

            BindEcmaBytes(rawAssembly, rawSymbolStore, ref bindResult, ref exception, ref result);

            // If the Ecma assembly binder isn't linked in, simply throw PlatformNotSupportedException
            if (!result.HasValue)
                throw new PlatformNotSupportedException();
            else
                return result.Value;
        }

        public sealed override bool Bind(RuntimeAssemblyName refName, bool cacheMissedLookups, out AssemblyBindResult result, out Exception exception)
        {
            bool foundMatch = false;
            result = default(AssemblyBindResult);
            exception = null;

            Exception preferredException = null;

            foreach (KeyValuePair<RuntimeAssemblyName, ScopeDefinitionGroup> group in ScopeGroups)
            {
                if (AssemblyNameMatches(refName, group.Key, ref preferredException))
                {
                    if (foundMatch)
                    {
                        exception = new AmbiguousMatchException(SR.Format(SR.AmbiguousMatchException_Assembly, refName.FullName));
                        return false;
                    }

                    foundMatch = true;
                    ScopeDefinitionGroup scopeDefinitionGroup = group.Value;

                    result.Reader = scopeDefinitionGroup.CanonicalScope.Reader;
                    result.ScopeDefinitionHandle = scopeDefinitionGroup.CanonicalScope.Handle;
                    result.OverflowScopes = scopeDefinitionGroup.OverflowScopes;
                }
            }

            BindEcmaAssemblyName(refName, cacheMissedLookups, ref result, ref exception, ref preferredException, ref foundMatch);
            if (exception != null)
                return false;

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
            foreach (KeyValuePair<RuntimeAssemblyName, ScopeDefinitionGroup> group in ScopeGroups)
            {
                ScopeDefinitionGroup scopeDefinitionGroup = group.Value;

                AssemblyBindResult result = default(AssemblyBindResult);
                result.Reader = scopeDefinitionGroup.CanonicalScope.Reader;
                result.ScopeDefinitionHandle = scopeDefinitionGroup.CanonicalScope.Handle;
                result.OverflowScopes = scopeDefinitionGroup.OverflowScopes;
                loadedAssemblies.Add(result);
            }

            InsertEcmaLoadedAssemblies(loadedAssemblies);

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

        /// <summary>
        /// This callback gets called whenever a module gets registered. It adds the metadata reader
        /// for the new module to the available scopes. The lock in ExecutionEnvironmentImplementation ensures
        /// that this function may never be called concurrently so that we can assume that two threads
        /// never update the reader and scope list at the same time.
        /// </summary>
        /// <param name="nativeFormatModuleInfo">Module to register</param>
        private void RegisterModule(NativeFormatModuleInfo nativeFormatModuleInfo)
        {
            LowLevelDictionaryWithIEnumerable<RuntimeAssemblyName, ScopeDefinitionGroup> scopeGroups = new LowLevelDictionaryWithIEnumerable<RuntimeAssemblyName, ScopeDefinitionGroup>();
            foreach (KeyValuePair<RuntimeAssemblyName, ScopeDefinitionGroup> oldGroup in _scopeGroups)
            {
                scopeGroups.Add(oldGroup.Key, oldGroup.Value);
            }
            AddScopesFromReaderToGroups(scopeGroups, nativeFormatModuleInfo.MetadataReader);

            // Update reader and scope list
            KeyValuePair<RuntimeAssemblyName, ScopeDefinitionGroup>[] scopeGroupsArray = new KeyValuePair<RuntimeAssemblyName, ScopeDefinitionGroup>[scopeGroups.Count];
            int i = 0;
            foreach (KeyValuePair<RuntimeAssemblyName, ScopeDefinitionGroup> data in scopeGroups)
            {
                scopeGroupsArray[i] = data;
                i++;
            }

            _scopeGroups = scopeGroupsArray;
        }

        private KeyValuePair<RuntimeAssemblyName, ScopeDefinitionGroup>[] ScopeGroups
        {
            get
            {
                return _scopeGroups;
            }
        }

        private static void AddScopesFromReaderToGroups(LowLevelDictionaryWithIEnumerable<RuntimeAssemblyName, ScopeDefinitionGroup> groups, MetadataReader reader)
        {
            foreach (ScopeDefinitionHandle scopeDefinitionHandle in reader.ScopeDefinitions)
            {
                RuntimeAssemblyName defName = scopeDefinitionHandle.ToRuntimeAssemblyName(reader);
                ScopeDefinitionGroup scopeDefinitionGroup;
                if (groups.TryGetValue(defName, out scopeDefinitionGroup))
                {
                    scopeDefinitionGroup.AddOverflowScope(new QScopeDefinition(reader, scopeDefinitionHandle));
                }
                else
                {
                    scopeDefinitionGroup = new ScopeDefinitionGroup(new QScopeDefinition(reader, scopeDefinitionHandle));
                    groups.Add(defName, scopeDefinitionGroup);
                }
            }
        }

        private volatile KeyValuePair<RuntimeAssemblyName, ScopeDefinitionGroup>[] _scopeGroups;

        private class ScopeDefinitionGroup
        {
            public ScopeDefinitionGroup(QScopeDefinition canonicalScope)
            {
                _canonicalScope = canonicalScope;
            }

            public QScopeDefinition CanonicalScope { get { return _canonicalScope; } }

            public IEnumerable<QScopeDefinition> OverflowScopes
            {
                get
                {
                    return _overflowScopes.ToArray();
                }
            }

            public void AddOverflowScope(QScopeDefinition overflowScope)
            {
                _overflowScopes.Add(overflowScope);
            }

            private readonly QScopeDefinition _canonicalScope;
            private ArrayBuilder<QScopeDefinition> _overflowScopes;
        }
    }
}
