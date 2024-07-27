// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
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
        /// The native signature should be the type returned by <see cref="IUnboundMarshallingGenerator.AsNativeType(TypePositionInfo)"/> passed by value.
        /// </summary>
        NativeType,

        /// <summary>
        /// The native signature should be a pointer to the type returned by <see cref="IUnboundMarshallingGenerator.AsNativeType(TypePositionInfo)"/> passed by value.
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
    public interface IBoundMarshallingGenerator
    {
        /// <summary>
        /// The managed type and position information this generator is bound to.
        /// </summary>
        TypePositionInfo TypeInfo { get; }

        /// <summary>
        /// Get the native type for the bound element of the generator.
        /// </summary>
        ManagedTypeInfo NativeType { get; }

        /// <summary>
        /// Get the shape that represents the bound element in the native signature
        /// </summary>
        SignatureBehavior NativeSignatureBehavior { get; }

        /// <summary>
        /// Get the shape of how the value represented by this generator should be passed at the managed/native boundary in the provided <paramref name="context"/>
        /// </summary>
        /// <param name="context">Code generation context</param>
        /// <returns>How to represent the unmanaged value at the managed/unmanaged boudary</returns>
        ValueBoundaryBehavior GetValueBoundaryBehavior(StubCodeContext context);

        /// <summary>
        /// Generate code for marshalling
        /// </summary>
        /// <param name="context">Code generation context</param>
        /// <returns>List of statements to be added to the P/Invoke stub</returns>
        /// <remarks>
        /// The generator should return the appropriate statements based on the
        /// <see cref="StubCodeContext.CurrentStage" /> of <paramref name="context"/>.
        /// For <see cref="StubCodeContext.Stage.Pin"/>, any statements not of type
        /// <see cref="FixedStatementSyntax"/> will be ignored.
        /// </remarks>
        IEnumerable<StatementSyntax> Generate(StubCodeContext context);


        /// <summary>
        /// Returns whether or not this marshaller uses an identifier for the native value in addition
        /// to an identifier for the managed value.
        /// </summary>
        /// <param name="context">Code generation context</param>
        /// <returns>If the marshaller uses an identifier for the native value, true; otherwise, false.</returns>
        /// <remarks>
        /// <see cref="StubCodeContext.CurrentStage" /> of <paramref name="context"/> may not be valid.
        /// </remarks>
        bool UsesNativeIdentifier(StubCodeContext context);

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
        ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, StubCodeContext context, out GeneratorDiagnostic? diagnostic);

        IBoundMarshallingGenerator Rebind(TypePositionInfo info);
    }

    public sealed class BoundMarshallingGenerator(TypePositionInfo info, IUnboundMarshallingGenerator unbound) : IBoundMarshallingGenerator
    {
        internal bool IsForwarder => unbound is Forwarder;

        internal bool IsBlittable => unbound is BlittableMarshaller;

        public TypePositionInfo TypeInfo => info;

        public ManagedTypeInfo NativeType => unbound.AsNativeType(TypeInfo);

        public SignatureBehavior NativeSignatureBehavior => unbound.GetNativeSignatureBehavior(TypeInfo);

        public IEnumerable<StatementSyntax> Generate(StubCodeContext context) => unbound.Generate(TypeInfo, context);

        public ValueBoundaryBehavior GetValueBoundaryBehavior(StubCodeContext context) => unbound.GetValueBoundaryBehavior(TypeInfo, context);

        public ByValueMarshalKindSupport SupportsByValueMarshalKind(ByValueContentsMarshalKind marshalKind, StubCodeContext context, out GeneratorDiagnostic? diagnostic)
            => unbound.SupportsByValueMarshalKind(marshalKind, TypeInfo, context, out diagnostic);

        public bool UsesNativeIdentifier(StubCodeContext context) => unbound.UsesNativeIdentifier(TypeInfo, context);

        public IBoundMarshallingGenerator Rebind(TypePositionInfo info) => new BoundMarshallingGenerator(info, unbound);
    }

    public static class UnboundMarshallingGeneratorExtensions
    {
        /// <summary>
        /// Bind this marshalling generator to a specific element info.
        /// </summary>
        /// <param name="unbound">The unbound generator</param>
        /// <param name="info">The element info</param>
        /// <returns>A generator wrapper that is bound to this info.</returns>
        public static IBoundMarshallingGenerator Bind(this IUnboundMarshallingGenerator unbound, TypePositionInfo info) => new BoundMarshallingGenerator(info, unbound);
    }

    /// <summary>
    /// Interface for generation of marshalling code for P/Invoke stubs
    /// </summary>
    public interface IUnboundMarshallingGenerator
    {
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
