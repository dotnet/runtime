// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection
{
    /// <summary>
    /// The base class for binding algorithms used by <see cref="System.Reflection.MetadataLoadContext"/>.
    /// </summary>
    public abstract class MetadataAssemblyResolver
    {
        /// <summary>
        /// The binding algorithm. This method is called when an Assembly is to be returned from a given AssemblyName.
        /// This occurs when MetadataLoadContext.LoadAssemblyByName() is called or when a Type from one assembly has a
        /// dependency on another assembly.
        ///
        /// It should use MetadataLoadContext.LoadFromStream(), LoadFromAssemblyPath()
        /// or LoadFromByteArray() to load the requested assembly and return it.
        /// </summary>
        ///<remarks>
        /// To indicate the failure to find an assembly, the handler should return null rather than throwing an exception. Returning null commits
        /// the failure so that future attempts to load that name will fail without re-invoking the handler.
        ///
        /// If the handler throws an exception, the exception will be passed through to the application that invoked the operation that triggered
        /// the binding. The MetadataLoadContext will not catch it and no binding will occur.
        ///
        /// The handler will generally not be called more than once for the same name, unless two threads race to load the same assembly.
        /// Even in that case, one result will win and be atomically bound to the name.
        /// 
        /// The MetadataLoadContext intentionally performs no ref-def matching on the returned assembly as what constitutes a ref-def match is a policy.
        /// It is also the kind of arbitrary restriction that MetadataLoadContext strives to avoid.
        ///
        /// The MetadataLoadContext cannot consume assemblies from other MetadataLoadContexts or other type providers (such as the underlying runtime's own Reflection system.)
        /// If a handler returns such an assembly, the MetadataLoadContext throws a FileLoadException.
        /// </remarks>
        public abstract Assembly Resolve(MetadataLoadContext context, AssemblyName assemblyName);
    }
}
