// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace Internal.TypeSystem
{
    public sealed partial class InstantiatedType : MetadataType
    {
        public override MetadataType MetadataBaseType
        {
            get
            {
                if (_baseType == this)
                    return InitializeBaseType();
                return _baseType;
            }
        }

        // Properties that are passed through from the type definition
        public override ClassLayoutMetadata GetClassLayout()
        {
            return _typeDef.GetClassLayout();
        }

        public override bool IsExplicitLayout
        {
            get
            {
                return _typeDef.IsExplicitLayout;
            }
        }

        public override bool IsSequentialLayout
        {
            get
            {
                return _typeDef.IsSequentialLayout;
            }
        }

        public override bool IsBeforeFieldInit
        {
            get
            {
                return _typeDef.IsBeforeFieldInit;
            }
        }

        public override bool IsModuleType
        {
            get
            {
                // The global module type cannot be generic.
                return false;
            }
        }

        public override bool IsSealed
        {
            get
            {
                return _typeDef.IsSealed;
            }
        }

        public override bool IsAbstract
        {
            get
            {
                return _typeDef.IsAbstract;
            }
        }

        public override ModuleDesc Module
        {
            get
            {
                return _typeDef.Module;
            }
        }

        public override bool HasCustomAttribute(string attributeNamespace, string attributeName)
        {
            return _typeDef.HasCustomAttribute(attributeNamespace, attributeName);
        }

        public override int GetInlineArrayLength()
        {
            return _typeDef.GetInlineArrayLength();
        }

        public override MetadataType GetNestedType(string name)
        {
            // Return the result from the typical type definition.
            return _typeDef.GetNestedType(name);
        }

        public override IEnumerable<MetadataType> GetNestedTypes()
        {
            // Return the result from the typical type definition.
            return _typeDef.GetNestedTypes();
        }
    }
}
