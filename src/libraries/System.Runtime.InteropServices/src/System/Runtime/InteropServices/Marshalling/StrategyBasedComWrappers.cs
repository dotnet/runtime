// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.Marshalling
{
    [CLSCompliant(false)]
    public class StrategyBasedComWrappers : ComWrappers
    {
        internal static StrategyBasedComWrappers DefaultMarshallingInstance { get; } = new();

        public static IIUnknownInterfaceDetailsStrategy DefaultIUnknownInterfaceDetailsStrategy { get; } = Marshalling.DefaultIUnknownInterfaceDetailsStrategy.Instance;

        public static IIUnknownStrategy DefaultIUnknownStrategy { get; } = FreeThreadedStrategy.Instance;

        protected static IIUnknownCacheStrategy CreateDefaultCacheStrategy() => new DefaultCaching();

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

        protected virtual IIUnknownStrategy GetOrCreateIUnknownStrategy() => DefaultIUnknownStrategy;

        protected virtual IIUnknownCacheStrategy CreateCacheStrategy() => CreateDefaultCacheStrategy();

        protected sealed override unsafe ComInterfaceEntry* ComputeVtables(object obj, CreateComInterfaceFlags flags, out int count)
        {
            if (obj.GetType().GetCustomAttribute(typeof(ComExposedClassAttribute<>)) is IComExposedDetails details)
            {
                return details.GetComInterfaceEntries(out count);
            }
            count = 0;
            return null;
        }

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

        protected sealed override void ReleaseObjects(IEnumerable objects)
        {
            throw new NotImplementedException();
        }
    }
}
