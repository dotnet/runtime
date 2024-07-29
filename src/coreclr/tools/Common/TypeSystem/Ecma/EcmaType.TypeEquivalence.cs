// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Metadata;
using System.Threading;

namespace Internal.TypeSystem.Ecma
{
    public partial class EcmaType
    {
        private  TypeIdentifierData _data;

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

        private TypeIdentifierData ComputeTypeIdentifierData()
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

                _data = new TypeIdentifierData((string)typeIdentifierAttribute.Value.FixedArguments[0].Value, (string)typeIdentifierAttribute.Value.FixedArguments[1].Value);
                return _data;
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

                    if (!TypeHasCharacteristicsRequiredToBeTypeEquivalent)
                        return null;

                    _data = ComputeTypeIdentifierFromGuids();
                }

                return null;
            }
        }

        public override TypeIdentifierData TypeIdentifierData
        {
            get
            {
                if (_data != null)
                {
                    if (object.ReferenceEquals(_data, TypeIdentifierData.Empty))
                        return null;
                    return _data;
                }
                var data = ComputeTypeIdentifierData() ?? TypeIdentifierData.Empty;

                Interlocked.CompareExchange(ref _data, data, null);

                // Recurse to read from _data and return a consistent result
                return this.TypeIdentifierData;
            }
        }

        public override bool IsWindowsRuntime
        {
            get
            {
                return _typeDefinition.Attributes.HasFlag(TypeAttributes.WindowsRuntime);
            }
        }

        public override bool IsComImport
        {
            get
            {
                return _typeDefinition.Attributes.HasFlag(TypeAttributes.Import);
            }
        }

        public override bool IsComEventInterface
        {
            get
            {
                return HasCustomAttribute("System.Runtime.InteropServices", "ComEventInterfaceAttribute");
            }
        }
    }
}
