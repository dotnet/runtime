// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    internal static class ComInterfaceGeneratorHelpers
    {
        private static readonly IMarshallingGeneratorFactory s_managedToUnmanagedDisabledMarshallingGeneratorFactory = CreateGeneratorFactory(EnvironmentFlags.DisableRuntimeMarshalling, MarshalDirection.ManagedToUnmanaged);
        private static readonly IMarshallingGeneratorFactory s_unmanagedToManagedDisabledMarshallingGeneratorFactory = CreateGeneratorFactory(EnvironmentFlags.DisableRuntimeMarshalling, MarshalDirection.UnmanagedToManaged);
        private static readonly IMarshallingGeneratorFactory s_managedToUnmanagedEnabledMarshallingGeneratorFactory = CreateGeneratorFactory(EnvironmentFlags.None, MarshalDirection.ManagedToUnmanaged);
        private static readonly IMarshallingGeneratorFactory s_unmanagedToManagedEnabledMarshallingGeneratorFactory = CreateGeneratorFactory(EnvironmentFlags.None, MarshalDirection.UnmanagedToManaged);

        private static IMarshallingGeneratorFactory CreateGeneratorFactory(EnvironmentFlags env, MarshalDirection direction)
        {
            IMarshallingGeneratorFactory generatorFactory;

            // If we're in a "supported" scenario, then emit a diagnostic as our final fallback.
            generatorFactory = new UnsupportedMarshallingFactory();

            generatorFactory = new NoMarshallingInfoErrorMarshallingFactory(generatorFactory, TypeNames.GeneratedComInterfaceAttribute_ShortName);

            // Since the char type can go into the P/Invoke signature here, we can only use it when
            // runtime marshalling is disabled.
            generatorFactory = new CharMarshallingGeneratorFactory(generatorFactory, useBlittableMarshallerForUtf16: env.HasFlag(EnvironmentFlags.DisableRuntimeMarshalling), TypeNames.GeneratedComInterfaceAttribute_ShortName);

            InteropGenerationOptions interopGenerationOptions = new(UseMarshalType: true);
            generatorFactory = new MarshalAsMarshallingGeneratorFactory(interopGenerationOptions, generatorFactory);

            generatorFactory = new StructAsHResultMarshallerFactory(generatorFactory);

            IMarshallingGeneratorFactory elementFactory = new AttributedMarshallingModelGeneratorFactory(
                // Since the char type in an array will not be part of the P/Invoke signature, we can
                // use the regular blittable marshaller in all cases.
                new CharMarshallingGeneratorFactory(generatorFactory, useBlittableMarshallerForUtf16: true, TypeNames.GeneratedComInterfaceAttribute_ShortName),
                new AttributedMarshallingModelOptions(env.HasFlag(EnvironmentFlags.DisableRuntimeMarshalling), MarshalMode.ElementIn, MarshalMode.ElementRef, MarshalMode.ElementOut));
            // We don't need to include the later generator factories for collection elements
            // as the later generator factories only apply to parameters.
            generatorFactory = new AttributedMarshallingModelGeneratorFactory(
                generatorFactory,
                elementFactory,
                new AttributedMarshallingModelOptions(
                    env.HasFlag(EnvironmentFlags.DisableRuntimeMarshalling),
                    direction == MarshalDirection.ManagedToUnmanaged
                        ? MarshalMode.ManagedToUnmanagedIn
                        : MarshalMode.UnmanagedToManagedOut,
                    direction == MarshalDirection.ManagedToUnmanaged
                        ? MarshalMode.ManagedToUnmanagedRef
                        : MarshalMode.UnmanagedToManagedRef,
                    direction == MarshalDirection.ManagedToUnmanaged
                        ? MarshalMode.ManagedToUnmanagedOut
                        : MarshalMode.UnmanagedToManagedIn));

            generatorFactory = new ManagedHResultExceptionMarshallerFactory(generatorFactory, direction);

            generatorFactory = new ComInterfaceDispatchMarshallerFactory(generatorFactory);

            generatorFactory = new ByValueContentsMarshalKindValidator(generatorFactory);
            generatorFactory = new BreakingChangeDetector(generatorFactory);

            return generatorFactory;
        }

        public static IMarshallingGeneratorFactory GetGeneratorFactory(EnvironmentFlags env, MarshalDirection direction)
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
