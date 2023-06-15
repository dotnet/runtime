// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Metadata;

namespace Internal.TypeSystem.Ecma
{
    public partial class EcmaType
    {
        private TypeIdentifierData ComputeTypeIdentifierFromGuids()
        {
            CustomAttributeValue<TypeDesc>? guidAttribute;
            if (IsInterface && _typeDefinition.Attributes.HasFlag(TypeAttributes.Import))
            {
                // ComImport interfaces get scope from their GUID
                guidAttribute = this.GetDecodedCustomAttribute("System.Runtime.InteropServices", "GuidAttribute");
            }
            else
            {
                // other equivalent types get it from the declaring assembly
                var attributeHandle = this.MetadataReader.GetCustomAttributeHandle(MetadataReader.GetAssemblyDefinition().GetCustomAttributes(), "System.Runtime.InteropServices", "GuidAttribute");
                if (attributeHandle.IsNil)
                    return null;

                guidAttribute = this.MetadataReader.GetCustomAttribute(attributeHandle).DecodeValue(new CustomAttributeTypeProvider(this.EcmaModule));
            }

            if (!guidAttribute.HasValue)
                return null;

            if (guidAttribute.Value.FixedArguments.Length < 1)
                return null;

            if (guidAttribute.Value.FixedArguments[0].Type != Context.GetWellKnownType(WellKnownType.String))
                return null;

            string scope = (string)guidAttribute.Value.FixedArguments[0].Value;
            string name = this.Name;
            if (this.Namespace != null)
                name = this.Namespace + "." + name;

            return new TypeIdentifierData(scope, name);
        }
        public override TypeIdentifierData TypeIdentifierData
        {
            get
            {
                if (!Context.SupportsTypeEquivalence)
                    return null;

                // Check for type identifier attribute
                var typeIdentifierAttribute = this.GetDecodedCustomAttribute("System.Runtime.InteropServices", "TypeIdentifierAttribute");
                if (typeIdentifierAttribute.HasValue)
                {
                    // If the type has a type identifier attribute it is always considered to be type equivalent
                    if (typeIdentifierAttribute.Value.FixedArguments.Length == 0)
                        return ComputeTypeIdentifierFromGuids();

                    if (typeIdentifierAttribute.Value.FixedArguments.Length != 2)
                        return null;

                    if (typeIdentifierAttribute.Value.FixedArguments[0].Type != Context.GetWellKnownType(WellKnownType.String))
                        return null;

                    if (typeIdentifierAttribute.Value.FixedArguments[1].Type != Context.GetWellKnownType(WellKnownType.String))
                        return null;

                    return new TypeIdentifierData((string)typeIdentifierAttribute.Value.FixedArguments[0].Value, (string)typeIdentifierAttribute.Value.FixedArguments[1].Value);
                }
                else
                {
                    // In addition to the TypeIdentifierAttribute certain other types may also be opted in to type equivalence
                    if (Context.SupportsCOMInterop)
                    {
                        // 1. Type is within assembly marked with ImportedFromTypeLibAttribute or PrimaryInteropAssemblyAttribute
                        if (this.HasCustomAttribute("System.Runtime.InteropServices", "ImportedFromTypeLibAttribute") || this.HasCustomAttribute("System.Runtime.InteropServices", "PrimaryInteropAssemblyAttribute"))
                        {
                            // This type has a TypeIdentifier attribute if it has an appropriate shape to be considered type equivalent

                        }

                        // 2. Type is a COMImport/COMEvent interface, enum, struct, or delegate
                        bool isCOMInterface = false;
                        if (this.IsInterface)
                        {
                            if (_typeDefinition.Attributes.HasFlag(TypeAttributes.Import))
                                isCOMInterface = true;
                            else if (HasCustomAttribute("System.Runtime.InteropServices", "ComEventInterfaceAttribute"))
                                isCOMInterface = true;
                        }

                        if (!isCOMInterface && !IsValueType && !IsDelegate)
                        {
                            return null;
                        }

                        // 3. Type is not generic
                        if (HasInstantiation)
                        {
                            return null;
                        }

                        // 4. Type is externally visible (i.e. public)
                        if (!this.GetEffectiveVisibility().IsExposedOutsideOfThisAssembly(false))
                        {
                            return null;
                        }

                        // 5. Type is not tdWindowsRuntime
                        if (_typeDefinition.Attributes.HasFlag(TypeAttributes.WindowsRuntime))
                        {
                            return null;
                        }

                        // 6. If type is nested, nesting type must be equivalent.
                        var containingType = ContainingType;
                        if (ContainingType != null)
                        {
                            if (containingType.TypeIdentifierData == null)
                            {
                                return null;
                            }
                        }

                        return ComputeTypeIdentifierFromGuids();
                    }

                    return null;
                }
            }
        }
    }
}
