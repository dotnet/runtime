// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace SerializationTestTypes
{
    public enum ComparisionType
    {
        DCS,
        POCO
    }

    #region ComparisionType passed as parameter

    /// <summary>
    /// A type which does not have DataContract,MessageContract,Serializable,does not implements ISerializable or does not implement IXmlSerializable is considered a POCO type
    /// While comparing POCO types, Non-public, readonly fields, ReadOnly/ WriteOnly properties and Non-public properties are ignored
    /// </summary>
    public static class ComparisonHelper
    {
        private const string LogMessage = "Comparing Type = {0} & Value = {1} with Type {2} & Value = {3}";

        public static void CompareRecursively(object originalData, object deserializedData, bool approxComparisonForFloatingPointAnd64BitValues = false)
        {
            ComparisionType cmpType = ComparisionType.DCS;
            SerializationMechanism att = ComparisonHelper.GetSerializationMechanism(originalData);
            if (att.Equals(SerializationMechanism.POCO))
            {
                cmpType = ComparisionType.POCO;
            }
            ComparisonHelper.CompareData(originalData, deserializedData, att, cmpType);
        }

        private static SerializationMechanism GetSerializationMechanism(object data)
        {
            if (data == null) return SerializationMechanism.POCO;
            SerializationMechanism att = SerializationMechanism.POCO;

            bool hasDataContractAttribute = data.GetType().GetCustomAttributes(typeof(DataContractAttribute), false).Length > 0;
            bool hasSerializableAttribute = data.GetType().IsSerializable;
            bool hasISerializable = typeof(ISerializable).IsAssignableFrom(data.GetType());
            bool hasIXmlSerializable = typeof(IXmlSerializable).IsAssignableFrom(data.GetType());

            if (
                (!hasDataContractAttribute) &&
                (!hasISerializable) &&
                (!hasIXmlSerializable) &&
                (!hasSerializableAttribute)
                )
            {
                att = SerializationMechanism.POCO;
            }
            else
            {
                if (hasDataContractAttribute || hasIXmlSerializable)
                {
                    att = SerializationMechanism.DataContractAttribute;
                }
                //CollectionDataContract is handled as part of DataContract containerTypeAttribute
                if (data.GetType().GetCustomAttributes(typeof(CollectionDataContractAttribute), false).Length > 0)
                {
                    hasDataContractAttribute = true;
                }

                //ISerializable interface is handled as part of Serializable containerTypeAttribute
                //since if a type implements ISerializable it must be marked with [Serializable] attribute
                if (data.GetType().GetInterface("ISerializable") != null)
                {
                    hasSerializableAttribute = true;
                }

                if (hasDataContractAttribute && hasISerializable)
                {
                    att = SerializationMechanism.DataContractAttribute;
                }
                else if (hasDataContractAttribute == true && hasISerializable == false)
                {
                    att = SerializationMechanism.DataContractAttribute;
                }
                else if (hasDataContractAttribute == false && (hasISerializable == true || hasSerializableAttribute == true))
                {
                    att = SerializationMechanism.SerializableAttribute;
                }
            }

            return att;
        }

        /// <summary>
        /// Throws an exception if mismatch is found
        /// </summary>
        /// <param name="originalData"></param>
        /// <param name="deserializedData"></param>
        private static void CompareData(object originalData, object deserializedData, SerializationMechanism containerTypeAttribute, ComparisionType cmpType)
        {
            if (originalData == null) // both are null, comparison succeeded
            {
                return;
            }

            if (originalData.GetType().Name.Equals(typeof(System.Runtime.Serialization.ExtensionDataObject).Name))
            {
                return;
            }

            //Fail if only one of the objects is null
            if ((null == originalData) != (null == deserializedData))
            {
                String message = String.Format("Comparision failed: Original data is {0}, deserialized data is {1}",
                    originalData == null ? "null" : "not null", deserializedData == null ? "null" : "not null");

                if (originalData != null)
                {
                    message += String.Format("Contents of Original data are {0}", originalData.ToString());
                }
                if (deserializedData != null)
                {
                    message += String.Format("Contents of Deserialized data are {0}", deserializedData.ToString());
                }
                throw new Exception(message);
            }

            if (originalData is IObjectReference)
            {
                //All IObjectReference types implement Equals method which compares the object returned by GetRealObject method
                bool result = originalData.Equals(deserializedData);
                if (!result)
                {
                    throw new Exception("Comparision failed for type " + originalData.GetType().Name);
                }
                return;
            }

            //Return false if the type of the object is not same
            Type originalDataType = originalData.GetType();
            Type deserializedDataType = deserializedData.GetType();

            if (!originalDataType.Equals(deserializedDataType))
            {
                throw new Exception(String.Format("Comparision failed : Original type {0} not same as deserialized type {1}", originalDataType.ToString(), deserializedDataType.ToString()));
            }

            object[] dataContractAttributes = originalDataType.GetCustomAttributes(typeof(DataContractAttribute), false);
            if (dataContractAttributes != null && dataContractAttributes.Length > 0)
            {
                DataContractAttribute dataContractAttribute = (DataContractAttribute)dataContractAttributes[0];
                if (dataContractAttribute.IsReference)
                {
                    return;
                }
            }

            MethodInfo equalsMethod = originalDataType.GetMethod("Equals", new Type[] { typeof(object) });

            #region "new object()"
            if (originalDataType == typeof(object))
            {
                return; // deserializedDataType == object as well; objects should be the same
            }
            #endregion

            #region String type
            else if (originalDataType.Equals(typeof(System.String)))
            {
                if (!originalData.Equals(deserializedData))
                {
                    throw new Exception(String.Format("Comparision failed: Original string data {0} is not same as deserialized string data {1}", originalData, deserializedData));
                }
            }
            #endregion

            #region XML types
            else if (originalDataType.Equals(typeof(XmlElement)) ||
                originalDataType.Equals(typeof(XmlNode)))
            {
                string originalDataXml = ((XmlNode)originalData).InnerXml;
                string deserializedDataXml = ((XmlNode)deserializedData).InnerXml;
                Trace.WriteLine(String.Format(LogMessage, originalDataType, originalDataXml, deserializedDataType, deserializedDataXml));
                if (!originalDataXml.Equals(deserializedDataXml))
                {
                    throw new Exception(String.Format("Comparision failed: Original XML data ({0}) is not the same as the deserialized XML data ({1})",
                        originalDataXml, deserializedDataXml));
                }
            }
            #endregion

            #region Special types
            else if (originalDataType == typeof(DBNull))
            {
                // only 1 possible value, DBNull.Value
                if ((((DBNull)originalData) == DBNull.Value) != (((DBNull)deserializedData) == DBNull.Value))
                {
                    throw new Exception(String.Format("Different instances of DBNull: original={0}, deserialized={1}", originalData, deserializedData));
                }
            }
            else if (originalDataType.Equals(typeof(DateTime)))
            {
                if (!(((DateTime)originalData).ToUniversalTime().Equals(((DateTime)deserializedData).ToUniversalTime())))
                {
                    throw new Exception(String.Format("Comparision failed: Original Datetime ticks {0} is not same as deserialized Datetime ticks {1}", ((DateTime)originalData).Ticks.ToString(), ((DateTime)deserializedData).Ticks.ToString()));
                }
            }
            else if (
                (originalDataType.Equals(typeof(TimeSpan)))
                || (originalDataType.Equals(typeof(Uri)))
                || (originalDataType.Equals(typeof(XmlQualifiedName)))
                || (originalDataType.Equals(typeof(Guid)))
                || (originalDataType.Equals(typeof(Decimal)))
                || (originalDataType.Equals(typeof(DateTimeOffset)))
             )
            {
                if (!originalData.Equals(deserializedData))
                {
                    throw new Exception(String.Format("Comparision failed : Original type data {0} is not same as deserialized type data {1}", originalData.ToString(), deserializedData.ToString()));
                }
            }

            #endregion

            #region Value Types

            else if (originalDataType.IsValueType)
            {
                //Value types can be Primitive types, Structs, Enums, Bool, User Defined Structs

                #region Primitive Types
                //Numeric types, bool 
                if (originalDataType.IsPrimitive)
                {
                    bool different = !originalData.Equals(deserializedData);
                    if (different)
                    {
                        throw new Exception(String.Format("Comparision failed: Original primitive data {0} is not same as deserialized primitive data {1}", originalData.ToString(), deserializedData.ToString()));
                    }
                }
                #endregion

                #region Enum type
                else if (originalDataType.IsEnum)
                {
                    SerializationMechanism enumAttribute = GetSerializationMechanism(originalData);

                    //Verify member is marked with EnumMember attribute and compare the value with the Value property of the enum
                    if (enumAttribute.Equals(SerializationMechanism.DataContractAttribute))
                    {
                        if (ComparisonHelper.IsMemberMarkedWithEnumMember(originalData, cmpType))
                        {
                            //Verify this will work for all scenarios
                            if (!originalData.ToString().Equals(deserializedData.ToString()))
                            {
                                throw new Exception(String.Format("Comparision failed: Original enum data {0} is not same as deserialized enum data {1}", originalData.ToString(), deserializedData.ToString()));
                            }
                        }
                    }
                }
                #endregion

                //If not a Primitive and Enum, it has to be a struct
                #region User defined structs
                else
                {
                    #region Compare Fields
                    ComparisonHelper.CompareFields(originalData, deserializedData, containerTypeAttribute, cmpType);
                    #endregion

                    #region Compare properties
                    ComparisonHelper.CompareProperties(originalData, deserializedData, containerTypeAttribute, cmpType);
                    #endregion

                }
                #endregion
            }
            #endregion

            #region Types which know how to compare themselves
            else if (equalsMethod.DeclaringType == originalData.GetType())
            {
                // the type knows how to compare itself, we'll use it
                if (!originalData.Equals(deserializedData))
                {
                    throw new Exception(String.Format("Comparision failed: Original type data {0} is not same as deserialized type data {1}", originalData.ToString(), deserializedData.ToString()));
                }
            }
            #endregion

            #region IDictionary and IDictionary<T>
            //Compares generic as well as non-generic dictionary types
            //Hashtables
            else if (originalData is IDictionary)
            {
                if (deserializedData is IDictionary)
                {
                    IDictionaryEnumerator originalDataEnum = ((IDictionary)originalData).GetEnumerator();
                    IDictionaryEnumerator deserializedDataEnum = ((IDictionary)deserializedData).GetEnumerator();
                    while (originalDataEnum.MoveNext())
                    {
                        deserializedDataEnum.MoveNext();
                        DictionaryEntry originalEntry = originalDataEnum.Entry;
                        DictionaryEntry deserializedEntry = deserializedDataEnum.Entry;
                        //Compare the keys and then the values
                        CompareData(originalEntry.Key, deserializedEntry.Key, containerTypeAttribute, cmpType);
                        CompareData(originalEntry.Value, deserializedEntry.Value, containerTypeAttribute, cmpType);
                    }
                }
                else
                {
                    throw new Exception(String.Format("Comparision failed: Original IDictionary type {0} and deserialized IDictionary type {1} are not of same", originalDataType.GetType().ToString(), deserializedDataType.GetType().ToString()));
                }
            }
            #endregion

            #region IEnumerable,IList,ICollection,IEnumerable<t>,IList<T>,ICollection<T>
            //Array,Lists,Queues,Stacks etc
            else if (originalData is IEnumerable)
            {
                IEnumerator originalDataEnumerator = ((IEnumerable)originalData).GetEnumerator();
                IEnumerator deserializedDataEnumerator = ((IEnumerable)deserializedData).GetEnumerator();
                if (null != originalDataEnumerator && null != deserializedDataEnumerator)
                {
                    while (originalDataEnumerator.MoveNext())
                    {
                        deserializedDataEnumerator.MoveNext();
                        CompareData(originalDataEnumerator.Current, deserializedDataEnumerator.Current, containerTypeAttribute, cmpType);
                    }
                }
                else
                {
                    throw new Exception(String.Format("Comparision failed: Original type {0} and deserialized type {1} are not IEnumerable", originalDataType.GetType().ToString(), deserializedDataType.GetType().ToString()));
                }
            }

            #endregion

            #region Class
            else if (originalDataType.IsClass)
            {
                #region Compare Fields
                ComparisonHelper.CompareFields(originalData, deserializedData, containerTypeAttribute, cmpType);
                #endregion

                #region Compare properties
                ComparisonHelper.CompareProperties(originalData, deserializedData, containerTypeAttribute, cmpType);
                #endregion
            }
            #endregion
        }

        private static bool IsMemberMarkedWithEnumMember(object data, ComparisionType cmpType)
        {
            bool isEnumMember = false;
            //Non-public members are not serialized for POCO types
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
            if (cmpType.Equals(ComparisionType.DCS))
            {
                flag = flag | BindingFlags.NonPublic;
            }

            FieldInfo info = data.GetType().GetField(data.ToString(), flag);
            if (null != info)
            {
                isEnumMember = info.GetCustomAttributes(typeof(EnumMemberAttribute), false).Length > 0;
            }
            return isEnumMember;
        }


        /// <summary>
        /// Iterates through the properties and invokes compare method
        /// </summary>
        /// <param name="originalData"></param>
        /// <param name="deserializedData"></param>
        /// <param name="containerTypeAttribute"></param>
        private static void CompareProperties(object originalData, object deserializedData, SerializationMechanism containerTypeAttribute, ComparisionType cmpType)
        {
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
            //Include private fields for DCS types
            if (cmpType.Equals(ComparisionType.DCS))
            {
                flag = flag | BindingFlags.NonPublic;
            }

            foreach (System.Reflection.PropertyInfo property in originalData.GetType().GetProperties(flag))
            {
                object newData = property.GetValue(originalData, null);
                SerializationMechanism fieldAttribute = ComparisonHelper.GetSerializationMechanism(newData);
                if (cmpType.Equals(ComparisionType.DCS))
                {
                    if (containerTypeAttribute.Equals(SerializationMechanism.DataContractAttribute))
                    {
                        if (
                            (property.GetCustomAttributes(typeof(DataMemberAttribute), false).Length > 0)
                            ||
                            (property.GetCustomAttributes(typeof(EnumMemberAttribute), false).Length > 0)
                            )
                        {
                            //Pass attribute of the complex type for furthur evaluation
                            if (IsComplexType(newData))
                            {
                                CompareData(newData, property.GetValue(deserializedData, null), fieldAttribute, cmpType);
                            }
                            else //Is a simple type
                            {
                                CompareData(newData, property.GetValue(deserializedData, null), containerTypeAttribute, cmpType);
                            }
                        }
                    }
                    else if (containerTypeAttribute.Equals(SerializationMechanism.SerializableAttribute))
                    {
                        if (property.GetCustomAttributes(typeof(NonSerializedAttribute), false).Length == 0)
                        {
                            //Pass attribute of the complex type for furthur evaluation
                            if (IsComplexType(newData))
                            {
                                CompareData(newData, property.GetValue(deserializedData, null), fieldAttribute, cmpType);
                            }
                            else //Is a simple type, so pass Parents attribute
                            {
                                CompareData(newData, property.GetValue(deserializedData, null), containerTypeAttribute, cmpType);
                            }
                        }
                    }
                }
                else if (cmpType.Equals(ComparisionType.POCO))
                {
                    //Ignore member with [IgnoreDataMember] attribute on a POCO type
                    if (property.GetCustomAttributes(typeof(IgnoreDataMemberAttribute), false).Length == 0)
                    {
                        //On POCO types, Properties which have both getter and setter will be serialized otherwise ignored
                        if (property.CanRead && property.CanWrite)
                        {
                            //Pass attribute of the complex type for furthur evaluation
                            if (IsComplexType(newData))
                            {
                                CompareData(newData, property.GetValue(deserializedData, null), fieldAttribute, cmpType);
                            }
                            else //Is a simple type, so pass Parents attribute
                            {
                                CompareData(newData, property.GetValue(deserializedData, null), containerTypeAttribute, cmpType);
                            }
                        }
                        else if (property.CanRead && !property.CanWrite) //Get-Only collection
                        {
                            //Pass attribute of the complex type for furthur evaluation
                            if (IsComplexType(newData))
                            {
                                CompareData(newData, property.GetValue(deserializedData, null), fieldAttribute, cmpType);
                            }
                            else //Is a simple type, so pass Parents attribute
                            {
                                CompareData(newData, property.GetValue(deserializedData, null), containerTypeAttribute, cmpType);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool IsComplexType(object data)
        {
            bool complexType = false;
            if (data == null) return false;
            if (
                ((data.GetType().IsValueType)
                &&
                (!data.GetType().IsPrimitive)
                &&
                (!data.GetType().IsEnum))
                ||
                ((data.GetType().IsClass)
                &&
                (!(data.GetType().Equals(typeof(System.String)))
                ))
                )
            {
                complexType = true;
            }
            return complexType;
        }

        private static void CompareFields(object originalData, object deserializedData, SerializationMechanism containerTypeAttribute, ComparisionType cmpType)
        {
            //Compare fields
            //Non-public members are not serialized for POCO types
            BindingFlags flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
            if (cmpType.Equals(ComparisionType.DCS))
            {
                flag = flag | BindingFlags.NonPublic;
            }

            foreach (System.Reflection.FieldInfo field in originalData.GetType().GetFields(flag))
            {
                object newData = field.GetValue(originalData);
                SerializationMechanism fieldAttribute = GetSerializationMechanism(newData);
                if (cmpType.Equals(ComparisionType.DCS))
                {
                    if (containerTypeAttribute.Equals(SerializationMechanism.DataContractAttribute))
                    {
                        if (
                            (field.GetCustomAttributes(typeof(DataMemberAttribute), false).Length > 0)
                            ||
                            (field.GetCustomAttributes(typeof(EnumMemberAttribute), false).Length > 0)
                            )
                        {
                            //Pass attribute of the complex type for furthur evaluation
                            if (ComparisonHelper.IsComplexType(newData))
                            {
                                ComparisonHelper.CompareData(field.GetValue(originalData), field.GetValue(deserializedData), fieldAttribute, cmpType);
                            }
                            else //Is a simple type
                            {
                                ComparisonHelper.CompareData(field.GetValue(originalData), field.GetValue(deserializedData), containerTypeAttribute, cmpType);
                            }
                        }
                    }
                    else if (containerTypeAttribute.Equals(SerializationMechanism.SerializableAttribute))
                    {
                        //Do not compare [NonSerialized] members
                        if (!field.IsNotSerialized)
                        {
                            if (ComparisonHelper.IsComplexType(newData))
                            {
                                ComparisonHelper.CompareData(field.GetValue(originalData), field.GetValue(deserializedData), fieldAttribute, cmpType);
                            }
                            else //Is a simple type
                            {
                                ComparisonHelper.CompareData(field.GetValue(originalData), field.GetValue(deserializedData), containerTypeAttribute, cmpType);
                            }
                        }
                    }
                }
                else if (cmpType.Equals(ComparisionType.POCO))
                {
                    //ReadOnly fields should be ignored for POCO type
                    //Ignore member with [IgnoreDataMember] attribute on a POCO type
                    if ((!field.IsInitOnly) && (field.GetCustomAttributes(typeof(IgnoreDataMemberAttribute), false).Length == 0))
                    {
                        if (ComparisonHelper.IsComplexType(newData))
                        {
                            ComparisonHelper.CompareData(field.GetValue(originalData), field.GetValue(deserializedData), fieldAttribute, cmpType);
                        }
                        else //Is a simple type
                        {
                            ComparisonHelper.CompareData(field.GetValue(originalData), field.GetValue(deserializedData), containerTypeAttribute, cmpType);
                        }
                    }
                }
            }
        }

        public static bool CompareDoubleApproximately(double d1, double d2)
        {
            if ((d1 < 0) != (d2 < 0)) return false;
            d1 = Math.Abs(d1);
            d2 = Math.Abs(d2);
            double max = Math.Max(d1, d2);
            double min = Math.Min(d1, d2);
            if (min == 0) return (max == 0);
            double difference = max - min;
            double ratio = difference / min;
            return (ratio < 0.0000001);
        }
    }

    public enum SerializationMechanism
    {
        POCO,
        DataContractAttribute,
        SerializableAttribute,
    }
    #endregion
}
