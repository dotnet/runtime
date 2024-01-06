// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    internal static class ComInterfaceGeneratorHelpers
    {
        private static readonly IMarshallingGeneratorResolver s_managedToUnmanagedDisabledMarshallingGeneratorFactory = CreateGeneratorFactory(EnvironmentFlags.DisableRuntimeMarshalling, MarshalDirection.ManagedToUnmanaged);
        private static readonly IMarshallingGeneratorResolver s_unmanagedToManagedDisabledMarshallingGeneratorFactory = CreateGeneratorFactory(EnvironmentFlags.DisableRuntimeMarshalling, MarshalDirection.UnmanagedToManaged);
        private static readonly IMarshallingGeneratorResolver s_managedToUnmanagedEnabledMarshallingGeneratorFactory = CreateGeneratorFactory(EnvironmentFlags.None, MarshalDirection.ManagedToUnmanaged);
        private static readonly IMarshallingGeneratorResolver s_unmanagedToManagedEnabledMarshallingGeneratorFactory = CreateGeneratorFactory(EnvironmentFlags.None, MarshalDirection.UnmanagedToManaged);

        private static IMarshallingGeneratorResolver CreateGeneratorFactory(EnvironmentFlags env, MarshalDirection direction)
            => DefaultMarshallingGeneratorResolver.Create(env, direction, TypeNames.GeneratedComInterfaceAttribute_ShortName,
                [
                    new StructAsHResultMarshallerFactory(),
                    new ManagedHResultExceptionGeneratorResolver(direction),
                    new ComInterfaceDispatchMarshallingResolver(),
                ]);

        public static IMarshallingGeneratorResolver GetGeneratorFactory(EnvironmentFlags env, MarshalDirection direction)
            => (env.HasFlag(EnvironmentFlags.DisableRuntimeMarshalling), direction) switch
            {
                (true, MarshalDirection.ManagedToUnmanaged) => s_managedToUnmanagedDisabledMarshallingGeneratorFactory,
                (true, MarshalDirection.UnmanagedToManaged) => s_unmanagedToManagedDisabledMarshallingGeneratorFactory,
                (false, MarshalDirection.ManagedToUnmanaged) => s_managedToUnmanagedEnabledMarshallingGeneratorFactory,
                (false, MarshalDirection.UnmanagedToManaged) => s_unmanagedToManagedEnabledMarshallingGeneratorFactory,
                _ => throw new UnreachableException(),
            };
    }
}
