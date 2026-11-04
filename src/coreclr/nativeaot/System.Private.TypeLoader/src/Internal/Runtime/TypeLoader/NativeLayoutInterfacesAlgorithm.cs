// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.NativeFormat;
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
            NativeParser typeInfoParser = state.GetParserForNativeLayoutInfo();
            NativeParser interfaceParser = typeInfoParser.GetParserForBagElementKind(BagElementKind.ImplementedInterfaces);
            uint count = interfaceParser.IsNull ? 0 : interfaceParser.GetSequenceCount();

            // The compiler emits the complete slot-ordered list. Substitution can make distinct slots equal.
            DefType[] interfaces = new DefType[count];
            for (int i = 0; i < interfaces.Length; i++)
            {
                interfaces[i] = (DefType)state.NativeLayoutInfo.LoadContext.GetType(ref interfaceParser);
            }

            return interfaces;
        }
    }
}
