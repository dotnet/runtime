// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// A <see cref="ComWrappers"/>-based type that uses customizable strategy objects to implement COM object wrappers and managed object wrappers exposed to COM.
    /// </summary>
    [CLSCompliant(false)]
    public class StrategyBasedComWrappers : ComWrappers
    {
        internal static StrategyBasedComWrappers DefaultMarshallingInstance { get; } = new();

        /// <summary>
        /// The default strategy to discover interface details about COM interfaces.
        /// </summary>
        /// <remarks>
        /// This strategy can discover interfaces and classes that use source-generated COM interop that use the <see cref="GeneratedComInterfaceAttribute"/> and <see cref="GeneratedComClassAttribute"/> attributes.
        /// This strategy looks for an <see cref="IUnknownDerivedAttribute{T, TImpl}"/> or <see cref="ComExposedClassAttribute{T}"/> attribute on the type of the provided object to discover COM type information.
        /// </remarks>
        public static IIUnknownInterfaceDetailsStrategy DefaultIUnknownInterfaceDetailsStrategy { get; } = Marshalling.DefaultIUnknownInterfaceDetailsStrategy.Instance;

        /// <summary>
        /// The default strategy to use for calling <c>IUnknown</c> methods.
        /// </summary>
        /// <remarks>
        /// This strategy assumes that all provided COM objects are free threaded and that calls to <c>IUnknown</c> methods can be made from any thread.
        /// </remarks>
        public static IIUnknownStrategy DefaultIUnknownStrategy { get; } = FreeThreadedStrategy.Instance;

        /// <summary>
        /// The default strategy to use for caching COM objects.
        /// </summary>
        /// <returns>The default strategy caches the interface pointers per interface no matter what thread they were initially retrieved on.</returns>
        protected static IIUnknownCacheStrategy CreateDefaultCacheStrategy() => new DefaultCaching();

        /// <summary>
        /// Get or create the interface details strategy for a new COM object wrapper.
        /// </summary>
        /// <returns>The interface details strategy to use for the new COM object.</returns>
        protected virtual IIUnknownInterfaceDetailsStrategy GetOrCreateInterfaceDetailsStrategy()
        {
            if (OperatingSystem.IsWindows() && RuntimeFeature.IsDynamicCodeSupported && ComObject.BuiltInComSupported && ComObject.ComImportInteropEnabled)
            {
                return GetInteropStrategy();
            }
            return DefaultIUnknownInterfaceDetailsStrategy;

            // This logic is split into a separate method, otherwise the trimmer will think that these suppressions are unnecessary on various platforms and error on them.
            // The easiest way to handle this is to put the case that needs annotations into a separate method.
            [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "The usage is guarded, but the analyzer and the trimmer don't understand it.")]
            [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "The opt-in feature is documented to not work in trimming scenarios.")]
            static IIUnknownInterfaceDetailsStrategy GetInteropStrategy()
            {
                return ComImportInteropInterfaceDetailsStrategy.Instance;
            }
        }

        /// <summary>
        /// Get or create the IUnknown strategy for a new COM object wrapper.
        /// </summary>
        /// <returns>The IUnknown strategy to use for the new COM object.</returns>
        protected virtual IIUnknownStrategy GetOrCreateIUnknownStrategy() => DefaultIUnknownStrategy;

        /// <summary>
        /// Create the caching strategy for a new COM object wrapper.
        /// </summary>
        /// <returns>The caching strategy to use for the new COM object.</returns>
        protected virtual IIUnknownCacheStrategy CreateCacheStrategy() => CreateDefaultCacheStrategy();

        /// <inheritdoc cref="ComWrappers.ComputeVtables" />
        protected sealed override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
        {
            if (GetOrCreateInterfaceDetailsStrategy().GetComExposedTypeDetails(obj.GetType().TypeHandle) is { } details)
            {
                return details.GetComInterfaceEntries(out count);
            }
            count = 0;
            return null;
        }

        /// <inheritdoc cref="ComWrappers.CreateObject" />
        protected sealed override unsafe object CreateObject(nint externalComObject, CreateObjectFlags flags)
        {
            if (flags.HasFlag(CreateObjectFlags.TrackerObject)
                || flags.HasFlag(CreateObjectFlags.Aggregation))
            {
                throw new NotSupportedException();
            }

            var rcw = new ComObject(GetOrCreateInterfaceDetailsStrategy(), GetOrCreateIUnknownStrategy(), CreateCacheStrategy(), (void*)externalComObject)
            {
                UniqueInstance = flags.HasFlag(CreateObjectFlags.UniqueInstance)
            };

            return rcw;
        }

        /// <inheritdoc cref="ComWrappers.ReleaseObjects" />
        protected sealed override void ReleaseObjects(IEnumerable objects)
        {
            throw new NotImplementedException();
        }
    }
}
