// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;

using ILLink.Shared.DataFlow;

using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    /// <summary>
    /// Helpers for the analysis that determines whether delegates stored into fields can ever be observed.
    /// The IL importer of the scanner recognizes the following patterns. In all of them, the consumer is a store
    /// into a tracked field (`stfld` or `stsfld`), or a call to a trivial setter of a tracked field.
    ///
    /// Delegate creation, in the form the C# compiler uses to cache lambdas and method groups. F is a field of a compiler
    /// generated class, and the receiver is `ldnull` for a static target or the `&lt;&gt;9` singleton of a lambda container
    /// class, so that dropping the creation can't change behavior:
    ///   ldsfld F; dup; brtrue L; pop; (ldsfld C::&lt;&gt;9 | ldnull); ldftn M; newobj D::.ctor; dup; stsfld F; L: consumer
    ///
    /// The creation itself is recognized from the receiver on, without looking at the instructions before it.
    /// The created delegate can only end up in F and in the destination of the consumer, so it is observable only if the
    /// program may observe one of those. A read of F that is the first instruction of the sequence forwards the question to
    /// the destination of the consumer. This requires the null case to be the creation that refills F: anything else could
    /// make whether F has a delegate observable. A read of F that is not understood makes F observable on its own.
    ///
    /// Copy from one tracked field to another:
    ///   (ldfld G | ldsfld G | call trivial getter of G); consumer
    ///
    /// Clearing a tracked instance field (not a read of it):
    ///   ldflda F; ldnull; call Volatile.Write&lt;T&gt;(ref T, T)
    ///
    /// Any other use of a tracked field marks it as read.
    /// </summary>
    internal sealed class DelegateFlowAnalysis
    {
        public enum AccessorKind : byte
        {
            None,

            // Instance method with the exact body `ldarg.0; ldfld F; ret` that returns a delegate.
            Getter,

            // Instance method with the exact body `ldarg.0; ldarg.1; stfld F; ret` that takes a delegate.
            Setter,
        }

        private readonly ILProvider _ilProvider;
        private readonly ConcurrentDictionary<MethodDesc, (AccessorKind Kind, FieldDesc Field)> _accessors = new();

        public DelegateFlowAnalysis(ILProvider ilProvider)
        {
            _ilProvider = ilProvider;
        }

        /// <summary>
        /// Gets whether the given field is one whose contents the analysis tracks. These are fields of delegate type
        /// on reference types (or static fields) that can't be reinterpreted by overlapping fields.
        /// </summary>
        public static bool IsTrackedField(FieldDesc typicalField)
        {
            Debug.Assert(typicalField.IsTypicalFieldDefinition);

            if (typicalField.IsLiteral || typicalField.HasRva || typicalField.IsThreadStatic)
                return false;

            try
            {
                if (!typicalField.FieldType.IsDelegate)
                    return false;
            }
            catch (TypeSystemException)
            {
                // Reflection can make fields of types that don't load visible to us.
                return false;
            }

            if (!typicalField.IsStatic)
            {
                if (typicalField.OwningType is not MetadataType owningType
                    || owningType.IsValueType
                    || owningType.IsExplicitLayout)
                {
                    return false;
                }
            }

            return true;
        }

        public (AccessorKind Kind, FieldDesc Field) GetAccessor(MethodDesc method)
        {
            MethodDesc typicalMethod = method.GetTypicalMethodDefinition();

            // Only methods from metadata can be accessors. Compiler generated methods can have signatures that are expensive or impossible to compute.
            if (typicalMethod is not EcmaMethod)
                return default;

            // Cheap filter before looking at IL: this gets called for many methods.
            bool maybeGetter;
            bool maybeSetter;
            try
            {
                MethodSignature signature = typicalMethod.Signature;
                if (signature.IsStatic
                    || signature.GenericParameterCount != 0
                    || typicalMethod.IsVirtual
                    || typicalMethod.OwningType.IsValueType
                    || typicalMethod.OwningType.IsInterface)
                {
                    return default;
                }

                maybeGetter = signature.Length == 0 && signature.ReturnType.IsDelegate;
                maybeSetter = signature.Length == 1 && signature[0].IsDelegate && signature.ReturnType.IsVoid;
            }
            catch (TypeSystemException)
            {
                return default;
            }

            if (!maybeGetter && !maybeSetter)
                return default;

            if (_accessors.TryGetValue(typicalMethod, out var result))
                return result;

            result = ComputeAccessor(typicalMethod, maybeGetter);
            _accessors.TryAdd(typicalMethod, result);
            return result;
        }

        private (AccessorKind Kind, FieldDesc Field) ComputeAccessor(MethodDesc typicalMethod, bool isGetter)
        {
            MethodIL methodIL;
            try
            {
                methodIL = _ilProvider.GetMethodIL(typicalMethod);
            }
            catch (TypeSystemException)
            {
                return default;
            }

            if (methodIL == null)
                return default;

            byte[] body = methodIL.GetILBytes();

            int fieldToken;
            if (isGetter)
            {
                // ldarg.0; ldfld <token>; ret
                if (body.Length != 7
                    || body[0] != (byte)ILOpcode.ldarg_0
                    || body[1] != (byte)ILOpcode.ldfld
                    || body[6] != (byte)ILOpcode.ret)
                {
                    return default;
                }

                fieldToken = BinaryPrimitives.ReadInt32LittleEndian(body.AsSpan(2));
            }
            else
            {
                // ldarg.0; ldarg.1; stfld <token>; ret
                if (body.Length != 8
                    || body[0] != (byte)ILOpcode.ldarg_0
                    || body[1] != (byte)ILOpcode.ldarg_1
                    || body[2] != (byte)ILOpcode.stfld
                    || body[7] != (byte)ILOpcode.ret)
                {
                    return default;
                }

                fieldToken = BinaryPrimitives.ReadInt32LittleEndian(body.AsSpan(3));
            }

            if (methodIL.GetObject(fieldToken, NotFoundBehavior.ReturnNull) is not FieldDesc field)
                return default;

            field = field.GetTypicalFieldDefinition();
            if (field.IsStatic || !IsTrackedField(field))
                return default;

            return (isGetter ? AccessorKind.Getter : AccessorKind.Setter, field);
        }

        public static FieldDesc GetTrackedField(MethodIL methodIL, int token)
        {
            if (methodIL.GetObject(token, NotFoundBehavior.ReturnNull) is not FieldDesc field)
                return null;

            field = field.GetTypicalFieldDefinition();
            return IsTrackedField(field) ? field : null;
        }

        /// <summary>
        /// Gets whether the given tracked field is the cache the C# compiler uses for the delegates it creates for
        /// lambdas and method groups.
        /// </summary>
        public static bool IsDelegateCacheField(FieldDesc field)
        {
            // A delegate creation that type preinitialization can interpret must not be dropped: the scanner doesn't see the code
            // preinitialization runs, so it can't tell whether the delegate is observed, and preinitialization would otherwise
            // embed a delegate to a method that was not scanned. Preinitialization gives up when it has to store into the static
            // of another type, which is the case for the cache of every creation we recognize. A cache in a class that could run
            // user code in its static constructor is not recognized.
            return field.OwningType is MetadataType owner
                && (CompilerGeneratedNames.IsLambdaDisplayClass(owner.Name.AsSpan())
                    || (IsDelegateCacheContainer(owner.Name.AsSpan()) && !owner.HasStaticConstructor));
        }

        // The C# compiler caches method group delegates in a container class named `<>O` (`<Method>O__ordinal_id` for
        // methods that need a container of their own, such as generic ones).
        private static bool IsDelegateCacheContainer(ReadOnlySpan<byte> name)
        {
            int end = name.IndexOf(">O"u8);

            return name.StartsWith("<"u8)
                && end > 0
                && (end + 2 == name.Length || name.Slice(end + 2).StartsWith("__"u8));
        }

        /// <summary>
        /// Gets whether a delegate creation that targets the given method can be a site (see <see cref="IsInfallibleReceiver"/>).
        /// A delegate creation that is not a site only needs to be tracked for targets where it can share a key with one.
        /// </summary>
        public static bool IsPossibleSiteTarget(MethodDesc target)
        {
            return target.Signature.IsStatic
                || (target.OwningType is MetadataType owner && CompilerGeneratedNames.IsLambdaDisplayClass(owner.Name.AsSpan()));
        }

        /// <summary>
        /// Gets whether the instruction that loads the receiver of a delegate creation can be left in place when the creation
        /// is replaced with null. Without the creation, nothing binds the delegate to its receiver, so the creation must not be able
        /// to throw or have an observable effect:
        /// - `ldnull` with a static target (an open static delegate), or
        /// - the singleton instance of the container class the C# compiler uses for lambdas that capture nothing
        ///   (a static readonly field of the container's own type, assigned by its static constructor and never null),
        ///   with a target in that class.
        /// </summary>
        public static bool IsInfallibleReceiver(MethodIL methodIL, ILOpcode receiver, int receiverToken, MethodDesc target)
        {
            switch (receiver)
            {
                case ILOpcode.ldnull:
                    return target.Signature.IsStatic;

                case ILOpcode.ldsfld:
                    return !target.Signature.IsStatic
                        && methodIL.GetObject(receiverToken, NotFoundBehavior.ReturnNull) is FieldDesc field
                        && field.IsStatic
                        && field.IsInitOnly
                        && field.OwningType is MetadataType owningType
                        && owningType.HasStaticConstructor
                        && CompilerGeneratedNames.IsLambdaDisplayClass(owningType.Name.AsSpan())
                        && field.FieldType.GetTypeDefinition() == owningType.GetTypeDefinition()
                        && field.GetTypicalFieldDefinition().OwningType == target.GetTypicalMethodDefinition().OwningType;

                default:
                    return false;
            }
        }
    }
}
