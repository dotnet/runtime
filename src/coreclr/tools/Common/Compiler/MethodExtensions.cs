// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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

        /// <summary>
        /// Determine whether a method can go into the sealed vtable of a type. Such method must be a sealed virtual
        /// method that is not overriding any method on a base type.
        /// Given that such methods can never be overridden in any derived type, we can
        /// save space in the vtable of a type, and all of its derived types by not emitting these methods in their vtables,
        /// and storing them in a separate table on the side. This is especially beneficial for all array types,
        /// since all of their collection interface methods are sealed and implemented on the System.Array and
        /// System.Array&lt;T&gt; base types, and therefore we can minimize the vtable sizes of all derived array types.
        /// </summary>
        public static bool CanMethodBeInSealedVTable(this MethodDesc method)
        {
            bool isInterfaceMethod = method.OwningType.IsInterface;

            // Methods on interfaces never go into sealed vtable
            // We would hit this code path for default implementations of interface methods (they are newslot+final).
            // Interface types don't get physical slots, but they have logical slot numbers and that logic shouldn't
            // attempt to place final+newslot methods differently.
            if (method.IsFinal && method.IsNewSlot && !isInterfaceMethod)
                return true;

            // Implementations of static virtual method also go into the sealed vtable.
            // Again, we don't let that happen for interface methods because the slot numbers are only logical,
            // not physical.
            if (method.Signature.IsStatic && !isInterfaceMethod)
                return true;

            return false;
        }

        public static bool NotCallableWithoutOwningEEType(this MethodDesc method)
        {
            TypeDesc owningType = method.OwningType;
            return !method.Signature.IsStatic && /* Static methods don't have this */
                !owningType.IsValueType && /* Value type instance methods take a ref to data */
                !owningType.IsArrayTypeWithoutGenericInterfaces() && /* Type loader can make these at runtime */
                (owningType is not MetadataType mdType || !mdType.IsModuleType) && /* Compiler parks some instance methods on the <Module> type */
                !method.IsSharedByGenericInstantiations; /* Current impl limitation; can be lifted */
        }

        public static PropertyPseudoDesc GetPropertyForAccessor(this MethodDesc accessor)
        {
            if (accessor.GetTypicalMethodDefinition() is not EcmaMethod ecmaAccessor)
                return null;

            var type = (EcmaType)ecmaAccessor.OwningType;
            var reader = type.MetadataReader;
            foreach (var propertyHandle in reader.GetTypeDefinition(type.Handle).GetProperties())
            {
                var accessors = reader.GetPropertyDefinition(propertyHandle).GetAccessors();
                if (ecmaAccessor.Handle == accessors.Getter
                    || ecmaAccessor.Handle == accessors.Setter)
                {
                    return new PropertyPseudoDesc(type, propertyHandle);
                }
            }

            return null;
        }
    }
}
