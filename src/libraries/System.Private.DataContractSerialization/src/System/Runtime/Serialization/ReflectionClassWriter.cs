// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace System.Runtime.Serialization
{
    internal abstract class ReflectionClassWriter
    {
        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public void ReflectionWriteClass(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, ClassDataContract classContract, XmlDictionaryString[]? memberNames)
        {
            InvokeOnSerializing(obj, context, classContract);
            obj = ResolveAdapterType(obj, classContract);
            if (classContract.IsISerializable)
            {
                context.WriteISerializable(xmlWriter, (ISerializable)obj);
            }
            else
            {
                if (classContract.HasExtensionData)
                {
                    context.WriteExtensionData(xmlWriter, ((IExtensibleDataObject)obj).ExtensionData, -1);
                }

                ReflectionWriteMembers(xmlWriter, obj, context, classContract, classContract, 0 /*childElementIndex*/, memberNames);
            }

            InvokeOnSerialized(obj, context, classContract);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        public void ReflectionWriteValue(XmlWriterDelegator xmlWriter, XmlObjectSerializerWriteContext context, Type type, object? value, bool writeXsiType, PrimitiveDataContract? primitiveContractForParamType)
        {
            Type memberType = type;
            object? memberValue = value;
            bool originValueIsNullableOfT = (memberType.IsGenericType && memberType.GetGenericTypeDefinition() == Globals.TypeOfNullable);
            if (memberType.IsValueType && !originValueIsNullableOfT)
            {
                Debug.Assert(memberValue != null);

                PrimitiveDataContract? primitiveContract = primitiveContractForParamType;
                if (primitiveContract != null && !writeXsiType)
                {
                    primitiveContract.WriteXmlValue(xmlWriter, memberValue, context);
                }
                else
                {
                    ReflectionInternalSerialize(xmlWriter, context, memberValue, memberValue.GetType().TypeHandle.Equals(memberType.TypeHandle), writeXsiType, memberType);
                }
            }
            else
            {
                if (originValueIsNullableOfT)
                {
                    if (memberValue == null)
                    {
                        memberType = Nullable.GetUnderlyingType(memberType)!;
                    }
                    else
                    {
                        MethodInfo getValue = memberType.GetMethod("get_Value", Type.EmptyTypes)!;
                        memberValue = getValue.Invoke(memberValue, Array.Empty<object>())!;
                        memberType = memberValue.GetType();
                    }
                }

                if (memberValue == null)
                {
                    context.WriteNull(xmlWriter, memberType, DataContract.IsTypeSerializable(memberType));
                }
                else
                {
                    PrimitiveDataContract? primitiveContract = originValueIsNullableOfT ? PrimitiveDataContract.GetPrimitiveDataContract(memberType) : primitiveContractForParamType;
                    if (primitiveContract != null && primitiveContract.UnderlyingType != Globals.TypeOfObject && !writeXsiType)
                    {
                        primitiveContract.WriteXmlValue(xmlWriter, memberValue, context);
                    }
                    else
                    {
                        if (memberValue == null &&
                            (memberType == Globals.TypeOfObject
                            || (originValueIsNullableOfT && memberType.IsValueType)))
                        {
                            context.WriteNull(xmlWriter, memberType, DataContract.IsTypeSerializable(memberType));
                        }
                        else
                        {
                            ReflectionInternalSerialize(xmlWriter, context, memberValue!, memberValue!.GetType().TypeHandle.Equals(memberType.TypeHandle), writeXsiType, memberType, originValueIsNullableOfT);
                        }
                    }
                }
            }
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected abstract int ReflectionWriteMembers(XmlWriterDelegator xmlWriter, object obj, XmlObjectSerializerWriteContext context, ClassDataContract classContract, ClassDataContract derivedMostClassContract, int childElementIndex, XmlDictionaryString[]? memberNames);

        protected object? ReflectionGetMemberValue(object obj, DataMember dataMember)
        {
            return dataMember.Getter(obj);
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        protected bool ReflectionTryWritePrimitive(XmlWriterDelegator xmlWriter, XmlObjectSerializerWriteContext context, Type type, object? value, XmlDictionaryString name, XmlDictionaryString? ns, PrimitiveDataContract? primitiveContract)
        {
            if (primitiveContract == null || primitiveContract.UnderlyingType == Globals.TypeOfObject)
                return false;

            primitiveContract.WriteXmlElement(xmlWriter, value, context, name, ns);

            return true;
        }

        private void InvokeOnSerializing(object obj, XmlObjectSerializerWriteContext context, ClassDataContract classContract)
        {
            if (classContract.BaseContract != null)
                InvokeOnSerializing(obj, context, classContract.BaseContract);
            if (classContract.OnSerializing != null)
            {
                var contextArg = context.GetStreamingContext();
                classContract.OnSerializing.Invoke(obj, new object[] { contextArg });
            }
        }

        private void InvokeOnSerialized(object obj, XmlObjectSerializerWriteContext context, ClassDataContract classContract)
        {
            if (classContract.BaseContract != null)
                InvokeOnSerialized(obj, context, classContract.BaseContract);
            if (classContract.OnSerialized != null)
            {
                var contextArg = context.GetStreamingContext();
                classContract.OnSerialized.Invoke(obj, new object[] { contextArg });
            }
        }

        private object ResolveAdapterType(object obj, ClassDataContract classContract)
        {
            Type type = obj.GetType();
            if (type == Globals.TypeOfDateTimeOffset)
            {
                obj = DateTimeOffsetAdapter.GetDateTimeOffsetAdapter((DateTimeOffset)obj);
            }
            else if (type == Globals.TypeOfMemoryStream)
            {
                obj = MemoryStreamAdapter.GetMemoryStreamAdapter((MemoryStream)obj);
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == Globals.TypeOfKeyValuePair)
            {
                obj = classContract.KeyValuePairAdapterConstructorInfo!.Invoke(new object[] { obj });
            }

            return obj;
        }

        [RequiresUnreferencedCode(DataContract.SerializerTrimmerWarning)]
        private void ReflectionInternalSerialize(XmlWriterDelegator xmlWriter, XmlObjectSerializerWriteContext context, object obj, bool isDeclaredType, bool writeXsiType, Type memberType, bool isNullableOfT = false)
        {
            if (isNullableOfT)
            {
                context.InternalSerialize(xmlWriter, obj, isDeclaredType, writeXsiType, DataContract.GetId(memberType.TypeHandle), memberType.TypeHandle);
            }
            else
            {
                context.InternalSerializeReference(xmlWriter, obj, isDeclaredType, writeXsiType, DataContract.GetId(memberType.TypeHandle), memberType.TypeHandle);
            }
        }
    }
}
