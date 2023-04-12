// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace System.Reflection
{
    public class ParameterInfo : ICustomAttributeProvider
#pragma warning disable SYSLIB0050 // IObjectReference is obsolete
#pragma warning disable SA1001 // CommasMustBeSpacedCorrectly
        , IObjectReference
#pragma warning restore SA1001
#pragma warning restore SYSLIB0050
    {
        protected ParameterInfo() { }

        public virtual ParameterAttributes Attributes => AttrsImpl;
        public virtual MemberInfo Member => MemberImpl;
        public virtual string? Name => NameImpl;
        public virtual Type ParameterType => ClassImpl!;
        public virtual int Position => PositionImpl;

        public bool IsIn => (Attributes & ParameterAttributes.In) != 0;
        public bool IsLcid => (Attributes & ParameterAttributes.Lcid) != 0;
        public bool IsOptional => (Attributes & ParameterAttributes.Optional) != 0;
        public bool IsOut => (Attributes & ParameterAttributes.Out) != 0;
        public bool IsRetval => (Attributes & ParameterAttributes.Retval) != 0;

        public virtual object? DefaultValue => throw NotImplemented.ByDesign;
        public virtual object? RawDefaultValue => throw NotImplemented.ByDesign;
        public virtual bool HasDefaultValue => throw NotImplemented.ByDesign;

        public virtual bool IsDefined(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);
            return false;
        }

        public virtual IEnumerable<CustomAttributeData> CustomAttributes => GetCustomAttributesData();
        public virtual IList<CustomAttributeData> GetCustomAttributesData() { throw NotImplemented.ByDesign; }

        public virtual object[] GetCustomAttributes(bool inherit) => Array.Empty<object>();
        public virtual object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            ArgumentNullException.ThrowIfNull(attributeType);
            return Array.Empty<object>();
        }

        public virtual Type GetModifiedParameterType() => throw new NotSupportedException();

        public virtual Type[] GetOptionalCustomModifiers() => Type.EmptyTypes;
        public virtual Type[] GetRequiredCustomModifiers() => Type.EmptyTypes;

        public virtual int MetadataToken => MetadataToken_ParamDef;

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object GetRealObject(StreamingContext context)
        {
            // Once all the serializable fields have come in we can set up the real
            // instance based on just two of them (MemberImpl and PositionImpl).

            if (MemberImpl == null)
                throw new SerializationException(SR.Serialization_InsufficientState);

            ParameterInfo[] args;
            switch (MemberImpl.MemberType)
            {
                case MemberTypes.Constructor:
                case MemberTypes.Method:
                    if (PositionImpl == -1)
                    {
                        if (MemberImpl.MemberType == MemberTypes.Method)
                            return ((MethodInfo)MemberImpl).ReturnParameter;
                        else
                            throw new SerializationException(SR.Serialization_BadParameterInfo);
                    }
                    else
                    {
                        args = ((MethodBase)MemberImpl).GetParametersNoCopy();

                        if (args != null && PositionImpl < args.Length)
                            return args[PositionImpl];
                        else
                            throw new SerializationException(SR.Serialization_BadParameterInfo);
                    }

                case MemberTypes.Property:
                    args = ((PropertyInfo)MemberImpl).GetIndexParameters();

                    if (args != null && PositionImpl > -1 && PositionImpl < args.Length)
                        return args[PositionImpl];
                    else
                        throw new SerializationException(SR.Serialization_BadParameterInfo);

                default:
                    throw new SerializationException(SR.Serialization_NoParameterInfo);
            }
        }

        public override string ToString()
        {
            string typeName = ParameterType.FormatTypeName();
            string? name = Name;
            return name is null ? typeName : typeName + " " + name;
        }

        protected ParameterAttributes AttrsImpl;
        protected Type? ClassImpl;
        protected object? DefaultValueImpl;
        protected MemberInfo MemberImpl = null!;
        protected string? NameImpl;
        protected int PositionImpl;

        private const int MetadataToken_ParamDef = 0x08000000;
    }
}
