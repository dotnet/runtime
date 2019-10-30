// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// Class for all RoMethod objects created by a MetadataLoadContext that has a MethodDef token associated with it
    /// and for which IsConstructedGenericMethod returns false.
    /// </summary>
    internal sealed partial class RoDefinitionMethod<TMethodDecoder>
    {
        private CustomAttributeData ComputeDllImportCustomAttributeDataIfAny()
        {
            if ((Attributes & MethodAttributes.PinvokeImpl) == 0)
                return null;

            // Make sure all the necessary framework types exist in this MetadataLoadContext's core assembly. If one doesn't, skip.
            CoreTypes ct = Loader.GetAllFoundCoreTypes();
            if (ct[CoreType.String] == null ||
                ct[CoreType.Boolean] == null ||
                ct[CoreType.DllImportAttribute] == null ||
                ct[CoreType.CharSet] == null ||
                ct[CoreType.CallingConvention] == null)
                return null;
            ConstructorInfo ctor = Loader.TryGetDllImportCtor();
            if (ctor == null)
                return null;

            Func<CustomAttributeArguments> argumentsPromise =
                () =>
                {
                    // The expensive work goes in here.

                    Type attributeType = ctor.DeclaringType;
                    DllImportAttribute dia = _decoder.ComputeDllImportAttribute();

                    CustomAttributeTypedArgument[] cats = { new CustomAttributeTypedArgument(ct[CoreType.String], dia.Value) };
                    CustomAttributeNamedArgument[] cans =
                    {
                        attributeType.ToCustomAttributeNamedArgument(nameof(DllImportAttribute.EntryPoint), ct[CoreType.String], dia.EntryPoint),
                        attributeType.ToCustomAttributeNamedArgument(nameof(DllImportAttribute.CharSet), ct[CoreType.CharSet], (int)dia.CharSet),
                        attributeType.ToCustomAttributeNamedArgument(nameof(DllImportAttribute.CallingConvention), ct[CoreType.CallingConvention], (int)dia.CallingConvention),
                        attributeType.ToCustomAttributeNamedArgument(nameof(DllImportAttribute.ExactSpelling), ct[CoreType.Boolean], dia.ExactSpelling),
                        attributeType.ToCustomAttributeNamedArgument(nameof(DllImportAttribute.PreserveSig), ct[CoreType.Boolean], dia.PreserveSig),
                        attributeType.ToCustomAttributeNamedArgument(nameof(DllImportAttribute.SetLastError), ct[CoreType.Boolean], dia.SetLastError),
                        attributeType.ToCustomAttributeNamedArgument(nameof(DllImportAttribute.BestFitMapping), ct[CoreType.Boolean], dia.BestFitMapping),
                        attributeType.ToCustomAttributeNamedArgument(nameof(DllImportAttribute.ThrowOnUnmappableChar), ct[CoreType.Boolean], dia.ThrowOnUnmappableChar),
                    };

                    return new CustomAttributeArguments(cats, cans);
                };

            return new RoPseudoCustomAttributeData(ctor, argumentsPromise);
        }
    }
}
