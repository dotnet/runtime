// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;
using System.Runtime.Serialization;
using System.Globalization;

namespace System.Runtime.Serialization.Json
{
    internal class JsonObjectDataContract : JsonDataContract
    {
        public JsonObjectDataContract(DataContract traditionalDataContract)
            : base(traditionalDataContract)
        {
        }

        public override object ReadJsonValueCore(XmlReaderDelegator jsonReader, XmlObjectSerializerReadContextComplexJson context)
        {
            object obj;
            string contentMode = jsonReader.GetAttribute(JsonGlobals.typeString);

            switch (contentMode)
            {
                case JsonGlobals.nullString:
                    jsonReader.Skip();
                    obj = null;
                    break;
                case JsonGlobals.booleanString:
                    obj = jsonReader.ReadElementContentAsBoolean();
                    break;
                case JsonGlobals.stringString:
                case null:
                    obj = jsonReader.ReadElementContentAsString();
                    break;
                case JsonGlobals.numberString:
                    obj = ParseJsonNumber(jsonReader.ReadElementContentAsString());
                    break;
                case JsonGlobals.objectString:
                    jsonReader.Skip();
                    obj = new object();
                    break;
                case JsonGlobals.arrayString:
                    // Read as object array
                    return DataContractJsonSerializerImpl.ReadJsonValue(DataContract.GetDataContract(Globals.TypeOfObjectArray), jsonReader, context);
                default:
                    throw XmlObjectSerializer.CreateSerializationException(SR.Format(SR.JsonUnexpectedAttributeValue, contentMode));
            }

            if (context != null)
            {
                context.AddNewObject(obj);
            }
            return obj;
        }

        public override void WriteJsonValueCore(XmlWriterDelegator jsonWriter, object obj, XmlObjectSerializerWriteContextComplexJson context, RuntimeTypeHandle declaredTypeHandle)
        {
            jsonWriter.WriteAttributeString(null, JsonGlobals.typeString, null, JsonGlobals.objectString);
        }

        internal static object ParseJsonNumber(string value, out TypeCode objectTypeCode)
        {
            if (value == null)
            {
                throw new XmlException(SR.Format(SR.XmlInvalidConversion, value, Globals.TypeOfInt));
            }

            if (value.IndexOfAny(JsonGlobals.FloatingPointCharacters) == -1)
            {
                int intValue;
                if (int.TryParse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out intValue))
                {
                    objectTypeCode = TypeCode.Int32;
                    return intValue;
                }

                long longValue;
                if (long.TryParse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out longValue))
                {
                    objectTypeCode = TypeCode.Int64;
                    return longValue;
                }
            }

            decimal decimalValue;
            if (decimal.TryParse(value, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out decimalValue))
            {
                objectTypeCode = TypeCode.Decimal;

                //check for decimal underflow
                if (decimalValue == decimal.Zero)
                {
                    double doubleValue = XmlConverter.ToDouble(value);
                    if (doubleValue != 0.0)
                    {
                        objectTypeCode = TypeCode.Double;
                        return doubleValue;
                    }
                }
                return decimalValue;
            }

            objectTypeCode = TypeCode.Double;
            return XmlConverter.ToDouble(value);
        }

        private static object ParseJsonNumber(string value)
        {
            TypeCode unusedTypeCode;
            return ParseJsonNumber(value, out unusedTypeCode);
        }
    }
}
