﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    /// <summary>
    /// Target framework identifier
    /// </summary>
    public enum TargetFramework
    {
        Unknown,
        Framework,
        Core,
        Standard,
        Net
    }

    /// <summary>
    /// An enumeration describing how a <see cref="TypePositionInfo"/> should be represented in its corresponding native signature element (parameter, field, or return value).
    /// </summary>
    public enum SignatureBehavior
    {
        /// <summary>
        /// The native type should match the managed type, including rehydrating marshalling attributes and by-ref syntax (pure forwarding).
        /// </summary>
        ManagedTypeAndAttributes,

        /// <summary>
        /// The native signature should be the type returned by <see cref="IMarshallingGenerator.AsNativeType(TypePositionInfo)"/> passed by value.
        /// </summary>
        NativeType,

        /// <summary>
        /// The native signature should be a pointer to the type returned by <see cref="IMarshallingGenerator.AsNativeType(TypePositionInfo)"/> passed by value.
        /// </summary>
        PointerToNativeType
    }

    /// <summary>
    /// An enumeration describing how a <see cref="TypePositionInfo"/> should be represented in its corresponding native signature element (parameter, field, or return value).
    /// </summary>
    public enum ValueBoundaryBehavior
    {
        /// <summary>
        /// The managed value should be passed as-is, including any managed by-ref syntax used in the managed declaration.
        /// </summary>
        ManagedIdentifier,

        /// <summary>
        /// The native identifier provided by <see cref="StubCodeContext.GetIdentifiers(TypePositionInfo)"/> should be passed by value.
        /// </summary>
        NativeIdentifier,

        /// <summary>
        /// The address of the native identifier provided by <see cref="StubCodeContext.GetIdentifiers(TypePositionInfo)"/> should be passed by value.
        /// </summary>
        AddressOfNativeIdentifier,

        /// <summary>
        /// The native identifier provided by <see cref="StubCodeContext.GetIdentifiers(TypePositionInfo)"/> should be cast to the native type.
        /// </summary>
        CastNativeIdentifier
    }

    /// <summary>
    /// An enumeration describing if the provided <see cref="ByValueContentsMarshalKind" /> is supported and changes behavior from the default behavior.
    /// </summary>
    public enum ByValueMarshalKindSupport
    {
        /// <summary>
        /// The provided <see cref="ByValueContentsMarshalKind" /> is supported and changes behavior from the default behavior.
        /// </summary>
        Supported,
        /// <summary>
        /// The provided <see cref="ByValueContentsMarshalKind" /> is not supported.
        /// </summary>
        NotSupported,
        /// <summary>
        /// The provided <see cref="ByValueContentsMarshalKind" /> is supported but does not change behavior from the default in this scenario.
        /// </summary>
        Unnecessary,
        /// <summary>
        /// The provided <see cref="ByValueContentsMarshalKind" /> is supported but does not follow best practices.
        /// </summary>
        NotRecommended,
    }

    /// <summary>
    /// Interface for generation of marshalling code for P/Invoke stubs
    /// </summary>
    public interface IMarshallingGenerator
    {
        /// <summary>
        /// Determine if the generator is supported for the supplied version of the framework.
        /// </summary>
        /// <param name="target">The framework to target.</param>
        /// <param name="version">The version of the framework.</param>
        /// <returns>True if the marshaller is supported, otherwise false.</returns>
        bool IsSupported(TargetFramework target, Version version);

        /// <summary>
        /// Get the native type syntax for <paramref name="info"/>
        /// </summary>
        /// <param name="info">Object to marshal</param>
        /// <returns>Managed type info for the native type representing <paramref name="info"/></returns>
        ManagedTypeInfo AsNativeType(TypePositionInfo info);

        /// <summary>
        /// Get shape that represents the provided <paramref name="info"/> in the native signature
        /// </summary>
        /// <param name="info">Object to marshal</param>
        /// <returns>Parameter syntax for <paramref name="info"/></returns>
        SignatureBehavior GetNativeSignatureBehavior(TypePositionInfo info);

        /// <summary>
        /// Get shape of how the value represented by <paramref name="info"/> should be passed at the managed/native boundary in the provided <paramref name="context"/>
        /// </summary>
        /// <param name="info">Object to marshal</param>
        /// <param name="context">Code generation context</param>
        /// <returns>Argument syntax for <paramref name="info"/></returns>
        ValueBoundaryBehavior GetValueBoundaryBehavior(TypePositionInfo info, StubCodeContext context);

        /// <summary>
        /// Generate code for marshalling
        /// </summary>
        /// <param name="info">Object to marshal</param>
        /// <param name="context">Code generation context</param>
        /// <returns>List of statements to be added to the P/Invoke stub</returns>
        /// <remarks>
        /// The generator should return the appropriate statements based on the
        /// <see cref="StubCodeContext.CurrentStage" /> of <paramref name="context"/>.
        /// For <see cref="StubCodeContext.Stage.Pin"/>, any statements not of type
        /// <see cref="FixedStatementSyntax"/> will be ignored.
        /// </remarks>
        IEnumerable<StatementSyntax> Generate(TypePositionInfo info, StubCodeContext context);

        /// <summary>
        /// Returns whether or not this marshaller uses an identifier for the native value in addition
        /// to an identifier for the managed value.
        /// </summary>
        /// <param name="info">Object to marshal</param>
        /// <param name="context">Code generation context</param>
        /// <returns>If the marshaller uses an identifier for the native value, true; otherwise, false.</returns>
        /// <remarks>
        /// <see cref="StubCodeContext.CurrentStage" /> of <paramref name="context"/> may not be valid.
        /// </remarks>
        bool UsesNativeIdentifier(TypePositionInfo info, StubCodeContext context);

        /// <summary>
        /// Returns if the given ByValueContentsMarshalKind is supported in the current marshalling context.
        /// A supported marshal kind has a different behavior than the default behavior.
        /// </summary>
        /// <param name="marshalKind">The marshal kind.</param>
        /// <param name="info">The TypePositionInfo of the parameter.</param>
        /// <param name="context">The marshalling context.</param>
        /// <param name="diagnostic">
        /// The diagnostic to report if the return value is not <see cref="ByValueMarshalKindSupport.Supported"/>.
        /// It should be non-null if the value is not <see cref="ByValueMarshalKindSupport.Supported"/>
        /// </param>
        /// <returns>If the provided <paramref name="marshalKind"/> is supported and if it is required to specify the requested behavior.</returns>
        ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, TypePositionInfo info, StubCodeContext context, out GeneratorDiagnostic? diagnostic);
    }
}
