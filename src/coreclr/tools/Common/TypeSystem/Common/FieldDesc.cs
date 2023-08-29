// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    public abstract partial class FieldDesc : TypeSystemEntity
    {
#pragma warning disable CA1825 // avoid Array.Empty<T>() instantiation for TypeLoader
        public static readonly FieldDesc[] EmptyFields = new FieldDesc[0];
#pragma warning restore CA1825

        public override int GetHashCode()
        {
            // Inherited types are expected to override
            return RuntimeHelpers.GetHashCode(this);
        }

        public override bool Equals(object o)
        {
            // Its only valid to compare two FieldDescs in the same context
            Debug.Assert(o is not FieldDesc || ReferenceEquals(((FieldDesc)o).Context, this.Context));
            return ReferenceEquals(this, o);
        }

        public virtual string Name
        {
            get
            {
                return null;
            }
        }

        public abstract DefType OwningType
        {
            get;
        }

        public abstract TypeDesc FieldType
        {
            get;
        }

        // Get the embedded signature data used to hold custom modifiers and such within a field signature
        public abstract EmbeddedSignatureData[] GetEmbeddedSignatureData();

        public abstract bool HasEmbeddedSignatureData { get; }

        public abstract bool IsStatic
        {
            get;
        }

        public abstract bool IsInitOnly
        {
            get;
        }

        public abstract bool IsThreadStatic
        {
            get;
        }

        public abstract bool HasRva
        {
            get;
        }

        public abstract bool IsLiteral
        {
            get;
        }

        public abstract bool HasCustomAttribute(string attributeNamespace, string attributeName);

        public virtual FieldDesc GetTypicalFieldDefinition()
        {
            return this;
        }

        public bool IsTypicalFieldDefinition
        {
            get
            {
                return GetTypicalFieldDefinition() == this;
            }
        }

        public virtual FieldDesc InstantiateSignature(Instantiation typeInstantiation, Instantiation methodInstantiation)
        {
            FieldDesc field = this;

            DefType owningType = field.OwningType;
            TypeDesc instantiatedOwningType = owningType.InstantiateSignature(typeInstantiation, methodInstantiation);
            if (owningType != instantiatedOwningType)
                field = instantiatedOwningType.Context.GetFieldForInstantiatedType(field.GetTypicalFieldDefinition(), (InstantiatedType)instantiatedOwningType);

            return field;
        }
    }
}
