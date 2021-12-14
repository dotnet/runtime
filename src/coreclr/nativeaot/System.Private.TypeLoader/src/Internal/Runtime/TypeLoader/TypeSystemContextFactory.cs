// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

using Internal.TypeSystem;

namespace Internal.Runtime.TypeLoader
{
    public static class TypeSystemContextFactory
    {
        // Cache the most recent instance of TypeSystemContext in a weak handle, and reuse it if possible
        // This allows us to avoid recreating the type resolution context again and again, but still allows it to go away once the types are no longer being built
        private static GCHandle s_cachedContext = GCHandle.Alloc(null, GCHandleType.Weak);

        private static Lock s_lock = new Lock();

        public static TypeSystemContext Create()
        {
            using (LockHolder.Hold(s_lock))
            {
                TypeSystemContext context = (TypeSystemContext)s_cachedContext.Target;
                if (context != null)
                {
                    s_cachedContext.Target = null;
                    return context;
                }
            }
            return new TypeLoaderTypeSystemContext(new TargetDetails(
#if TARGET_ARM
            TargetArchitecture.ARM,
#elif TARGET_ARM64
            TargetArchitecture.ARM64,
#elif TARGET_X86
            TargetArchitecture.X86,
#elif TARGET_AMD64
            TargetArchitecture.X64,
#elif TARGET_WASM
            TargetArchitecture.Wasm32,
#else
#error Unknown architecture
#endif
            TargetOS.Windows,
            TargetAbi.Unknown));
        }

        public static void Recycle(TypeSystemContext context)
        {
            // Only cache a reasonably small context that is still in Gen0
            if (context.LoadFactor > 200 || GC.GetGeneration(context) > 0)
                return;

            // Flush the type system context from all types being recycled
            context.FlushTypeBuilderStates();

            // No lock needed here - the reference assignment is atomic
            s_cachedContext.Target = context;
        }
    }
}
