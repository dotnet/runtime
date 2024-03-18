// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler.DependencyAnalysis;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    public static class MethodExtensions
    {
        public static string GetRuntimeImportName(this EcmaMethod This)
        {
            var decoded = This.GetDecodedCustomAttribute("System.Runtime", "RuntimeImportAttribute");
            if (decoded == null)
                return null;

            var decodedValue = decoded.Value;

            if (decodedValue.FixedArguments.Length != 0)
                return (string)decodedValue.FixedArguments[decodedValue.FixedArguments.Length - 1].Value;

            return null;
        }

        public static string GetRuntimeImportDllName(this EcmaMethod This)
        {
            var decoded = This.GetDecodedCustomAttribute("System.Runtime", "RuntimeImportAttribute");
            if (decoded == null)
                return null;

            var decodedValue = decoded.Value;

            if (decodedValue.FixedArguments.Length == 2)
                return (string)decodedValue.FixedArguments[0].Value;

            return null;
        }

        public static string GetRuntimeExportName(this EcmaMethod This)
        {
            var decoded = This.GetDecodedCustomAttribute("System.Runtime", "RuntimeExportAttribute");
            if (decoded == null)
                return null;

            var decodedValue = decoded.Value;

            if (decodedValue.FixedArguments.Length != 0)
                return (string)decodedValue.FixedArguments[0].Value;

            foreach (var argument in decodedValue.NamedArguments)
            {
                if (argument.Name == "EntryPoint")
                    return (string)argument.Value;
            }

            return null;
        }

        public static string GetUnmanagedCallersOnlyExportName(this EcmaMethod This)
        {
            var decoded = This.GetDecodedCustomAttribute("System.Runtime.InteropServices", "UnmanagedCallersOnlyAttribute");
            if (decoded == null)
                return null;

            var decodedValue = decoded.Value;

            foreach (var argument in decodedValue.NamedArguments)
            {
                if (argument.Name == "EntryPoint")
                    return (string)argument.Value;
            }

            return null;
        }

#if !READYTORUN
        /// <summary>
        /// Determine whether a method can go into the sealed vtable of a type. Such method must be a sealed virtual
        /// method that is not overriding any method on a base type.
        /// Given that such methods can never be overridden in any derived type, we can
        /// save space in the vtable of a type, and all of its derived types by not emitting these methods in their vtables,
        /// and storing them in a separate table on the side. This is especially beneficial for all array types,
        /// since all of their collection interface methods are sealed and implemented on the System.Array and
        /// System.Array&lt;T&gt; base types, and therefore we can minimize the vtable sizes of all derived array types.
        /// </summary>
        public static bool CanMethodBeInSealedVTable(this MethodDesc method, NodeFactory factory)
        {
            Debug.Assert(!method.OwningType.ContainsSignatureVariables(treatGenericParameterLikeSignatureVariable: true));

            TypeDesc owningType = method.OwningType;

            // Interface types don't have physical slots so we never optimize to sealed slots
            if (owningType.IsInterface)
                return false;

            // Implementations of static virtual methods go into the sealed vtable.
            if (method.Signature.IsStatic)
                return true;

            // If the owning type is already considered sealed, there's little benefit in placing the slots
            // in the sealed vtable: the sealed vtable has these properties:
            //
            // 1. We don't need to repeat them in derived classes.
            // 2. The slots use 4-byte relative pointers, so they can be smaller.
            // 3. The sealed vtable is shared among canonically-equivalent types.
            //
            // Benefit 1 doesn't apply to sealed types by definition. Benefit 2 doesn't manifest itself
            // when data dehydration is enabled (which is the default) since pointers are compressed either way.
            // Benefit 3 is still real, so we condition this opt out on type not having a canonical form.
            if (factory.DevirtualizationManager.IsEffectivelySealed(owningType)
                && !owningType.ConvertToCanonForm(CanonicalFormKind.Specific).IsCanonicalSubtype(CanonicalFormKind.Any))
            {
                return false;
            }

            // Newslot final methods go into the sealed vtable.
            if (method.IsNewSlot && factory.DevirtualizationManager.IsEffectivelySealed(method))
                return true;

            return false;
        }
#endif

        public static bool NotCallableWithoutOwningEEType(this MethodDesc method)
        {
            TypeDesc owningType = method.OwningType;
            return !method.Signature.IsStatic && /* Static methods don't have this */
                !owningType.IsValueType && /* Value type instance methods take a ref to data */
                !owningType.IsArrayTypeWithoutGenericInterfaces() && /* Type loader can make these at runtime */
                (owningType is not MetadataType mdType || !mdType.IsModuleType) && /* Compiler parks some instance methods on the <Module> type */
                !method.IsSharedByGenericInstantiations; /* Current impl limitation; can be lifted */
        }
    }
}
