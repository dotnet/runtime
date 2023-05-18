// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    internal static class VtableIndexStubGeneratorHelpers
    {
        public static MarshallingGeneratorFactoryKey<(TargetFramework, Version)> CreateGeneratorFactory(StubEnvironment env, MarshalDirection direction)
        {
            IMarshallingGeneratorFactory generatorFactory;

            // If we're in a "supported" scenario, then emit a diagnostic as our final fallback.
            generatorFactory = new UnsupportedMarshallingFactory();

            generatorFactory = new NoMarshallingInfoErrorMarshallingFactory(generatorFactory);

            // The presence of System.Runtime.CompilerServices.DisableRuntimeMarshallingAttribute is tied to TFM,
            // so we use TFM in the generator factory key instead of the Compilation as the compilation changes on every keystroke.
            IAssemblySymbol coreLibraryAssembly = env.Compilation.GetSpecialType(SpecialType.System_Object).ContainingAssembly;
            ITypeSymbol? disabledRuntimeMarshallingAttributeType = coreLibraryAssembly.GetTypeByMetadataName(TypeNames.System_Runtime_CompilerServices_DisableRuntimeMarshallingAttribute);
            bool runtimeMarshallingDisabled = disabledRuntimeMarshallingAttributeType is not null
                && env.Compilation.Assembly.GetAttributes().Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, disabledRuntimeMarshallingAttributeType));

            // Since the char type can go into the P/Invoke signature here, we can only use it when
            // runtime marshalling is disabled.
            generatorFactory = new CharMarshallingGeneratorFactory(generatorFactory, useBlittableMarshallerForUtf16: runtimeMarshallingDisabled);

            InteropGenerationOptions interopGenerationOptions = new(UseMarshalType: true);
            generatorFactory = new MarshalAsMarshallingGeneratorFactory(interopGenerationOptions, generatorFactory);

            IMarshallingGeneratorFactory elementFactory = new AttributedMarshallingModelGeneratorFactory(
                // Since the char type in an array will not be part of the P/Invoke signature, we can
                // use the regular blittable marshaller in all cases.
                new CharMarshallingGeneratorFactory(generatorFactory, useBlittableMarshallerForUtf16: true),
                new AttributedMarshallingModelOptions(runtimeMarshallingDisabled, MarshalMode.ElementIn, MarshalMode.ElementRef, MarshalMode.ElementOut));
            // We don't need to include the later generator factories for collection elements
            // as the later generator factories only apply to parameters.
            generatorFactory = new AttributedMarshallingModelGeneratorFactory(
                generatorFactory,
                elementFactory,
                new AttributedMarshallingModelOptions(
                    runtimeMarshallingDisabled,
                    direction == MarshalDirection.ManagedToUnmanaged
                        ? MarshalMode.ManagedToUnmanagedIn
                        : MarshalMode.UnmanagedToManagedOut,
                    direction == MarshalDirection.ManagedToUnmanaged
                        ? MarshalMode.ManagedToUnmanagedRef
                        : MarshalMode.UnmanagedToManagedRef,
                    direction == MarshalDirection.ManagedToUnmanaged
                        ? MarshalMode.ManagedToUnmanagedOut
                        : MarshalMode.UnmanagedToManagedIn));

            generatorFactory = new ObjectUnwrapperMarshallerFactory(generatorFactory);

            generatorFactory = new ByValueContentsMarshalKindValidator(generatorFactory);

            return MarshallingGeneratorFactoryKey.Create((env.TargetFramework, env.TargetFrameworkVersion), generatorFactory);
        }
    }
}
