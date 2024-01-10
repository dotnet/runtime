// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using Internal.NativeFormat;
using Internal.Runtime.Augments;
using Internal.TypeSystem;

namespace Internal.Runtime.TypeLoader
{
    /// <summary>
    /// Reads interfaces for native layout types
    /// </summary>
    internal class NativeLayoutInterfacesAlgorithm : RuntimeInterfacesAlgorithm
    {
        public override DefType[] ComputeRuntimeInterfaces(TypeDesc type)
        {
            TypeBuilderState state = type.GetOrCreateTypeBuilderState();
            int totalInterfaces = RuntimeAugments.GetInterfaceCount(state.TemplateType.RuntimeTypeHandle);

            TypeLoaderLogger.WriteLine("Building runtime interfaces for type " + type.ToString() + " (total interfaces = " + totalInterfaces.LowLevelToString() + ") ...");

            DefType[] interfaces = new DefType[totalInterfaces];
            int numInterfaces = 0;

            //
            // Copy over all interfaces from base class
            //
            if (type.BaseType != null)
            {
                foreach (var baseInterface in type.BaseType.RuntimeInterfaces)
                {
                    // There should be no duplicates
                    Debug.Assert(!InterfaceInSet(interfaces, numInterfaces, baseInterface));
                    interfaces[numInterfaces++] = baseInterface;
                    TypeLoaderLogger.WriteLine("    -> Added basetype interface " + baseInterface.ToString() + " on type " + type.ToString());
                }
            }

            NativeParser typeInfoParser = state.GetParserForNativeLayoutInfo();
            NativeParser interfaceParser = typeInfoParser.GetParserForBagElementKind(BagElementKind.ImplementedInterfaces);
            TypeDesc[] implementedInterfaces;
            if (!interfaceParser.IsNull)
                implementedInterfaces = state.NativeLayoutInfo.LoadContext.GetTypeSequence(ref interfaceParser);
            else
                implementedInterfaces = TypeDesc.EmptyTypes;

            // Note that the order in which the interfaces are added to the list is same as the order in which the MDIL binder adds them.
            // It is required for correctness

            foreach (TypeDesc interfaceType in implementedInterfaces)
            {
                DefType interfaceTypeAsDefType = (DefType)interfaceType;

                // Skip duplicates
                if (InterfaceInSet(interfaces, numInterfaces, interfaceTypeAsDefType))
                    continue;
                interfaces[numInterfaces++] = interfaceTypeAsDefType;

                TypeLoaderLogger.WriteLine("    -> Added interface " + interfaceTypeAsDefType.ToString() + " on type " + type.ToString());

                foreach (var inheritedInterface in interfaceTypeAsDefType.RuntimeInterfaces)
                {
                    // Skip duplicates
                    if (InterfaceInSet(interfaces, numInterfaces, inheritedInterface))
                        continue;
                    interfaces[numInterfaces++] = inheritedInterface;
                    TypeLoaderLogger.WriteLine("    -> Added inherited interface " + inheritedInterface.ToString() + " on type " + type.ToString());
                }
            }

            // TODO: Handle the screwy cases of generic interface folding
            Debug.Assert(numInterfaces == totalInterfaces, "Unexpected number of interfaces");

            return interfaces;
        }

        /// <summary>
        /// Checks if the interface exists in the list of interfaces
        /// </summary>
        private static bool InterfaceInSet(DefType[] interfaces, int numInterfaces, DefType interfaceType)
        {
            for (int i = 0; i < numInterfaces; i++)
            {
                if (interfaces[i].Equals(interfaceType))
                    return true;
            }

            return false;
        }
    }
}
