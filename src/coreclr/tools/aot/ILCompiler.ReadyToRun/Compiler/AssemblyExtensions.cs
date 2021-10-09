// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection.Metadata;

using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    public static class AssemblyExtensions
    {
        /// <summary>
        /// Determines whether the assembly was compiled without optimizations using the DebuggableAttribute
        /// </summary>
        public static bool HasOptimizationsDisabled(this EcmaAssembly assembly)
        {
            bool result = false;
            MetadataReader reader = assembly.MetadataReader;
            var attributeHandles = assembly.AssemblyDefinition.GetCustomAttributes();
            CustomAttributeHandle attributeHandle = reader.GetCustomAttributeHandle(attributeHandles, "System.Diagnostics", "DebuggableAttribute");
            if (!attributeHandle.IsNil)
            {
                CustomAttribute attribute = reader.GetCustomAttribute(attributeHandle);
                CustomAttributeValue<TypeDesc> decoded = attribute.DecodeValue(new CustomAttributeTypeProvider(assembly));

                if (decoded.FixedArguments.Length == 1)
                {
                    // DebuggableAttribute( DebuggableAttribute.DebuggingModes modes )
                    if (!(decoded.FixedArguments[0].Value is int))
                    {
                        ThrowHelper.ThrowBadImageFormatException();
                    }
                    DebuggableAttribute.DebuggingModes modes = (DebuggableAttribute.DebuggingModes)decoded.FixedArguments[0].Value;
                    result = modes.HasFlag(DebuggableAttribute.DebuggingModes.DisableOptimizations) && modes.HasFlag(DebuggableAttribute.DebuggingModes.Default);
                }
                else if (decoded.FixedArguments.Length == 2)
                {
                    // DebuggableAttribute( bool isJITTrackingEnabled, bool isJITOptimizerDisabled )
                    if (!(decoded.FixedArguments[0].Value is bool) || !(decoded.FixedArguments[1].Value is bool))
                    {
                        ThrowHelper.ThrowBadImageFormatException();
                    }
                    result = ((bool)decoded.FixedArguments[1].Value);
                }
            }
            return result;
        }
    }
}
