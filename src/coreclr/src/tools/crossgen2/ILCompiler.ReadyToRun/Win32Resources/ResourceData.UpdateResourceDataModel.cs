// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;

namespace ILCompiler.Win32Resources
{
    public unsafe partial class ResourceData
    {
        private void AddResource(object type, object name, ushort language, byte[] data)
        {
            ResType resType = null;
            // Allocate new object in case it is needed.
            ResType newResType;
            int newIndex;

            IList typeList;
            bool updateExisting;
            if (type is ushort)
            {
                ResType_Ordinal newOrdinalType = new ResType_Ordinal((ushort)type);
                newResType = newOrdinalType;
                typeList = _resTypeHeadID;

                newIndex = GetIndexOfFirstItemMatchingInListOrInsertionPoint(typeList, (ushort left, ushort right) => (int)left - (int)right, (ushort)type, out updateExisting);
            }
            else
            {
                ResType_Name newStringType = new ResType_Name((string)type);
                newResType = newStringType;
                typeList = _resTypeHeadName;

                newIndex = GetIndexOfFirstItemMatchingInListOrInsertionPoint(typeList, string.CompareOrdinal, (string)type, out updateExisting);
            }

            if (updateExisting)
            {
                resType = (ResType)typeList[newIndex];
            }
            else
            {
                // This is a new type
                if (newIndex == -1)
                    typeList.Add(newResType);
                else
                    typeList.Insert(newIndex, newResType);

                resType = newResType;
            }

            Type resNameType;
            IList nameList;
            int nameIndex;

            if (name is ushort)
            {
                nameList = resType.NameHeadID;
                resNameType = typeof(ResName_Ordinal);
                nameIndex = GetIndexOfFirstItemMatchingInListOrInsertionPoint(nameList, (ushort left, ushort right) => (int)left - (int)right, (ushort)name, out updateExisting);
            }
            else
            {
                nameList = resType.NameHeadName;
                resNameType = typeof(ResName_Name);
                nameIndex = GetIndexOfFirstItemMatchingInListOrInsertionPoint(nameList, string.CompareOrdinal, (string)name, out updateExisting);
            }

            if (updateExisting)
            {
                // We have at least 1 language with the same type/name. Insert/delete from language list
                ResName resName = (ResName)nameList[nameIndex];
                int newNumberOfLanguages = (int)resName.NumberOfLanguages + (data != null ? 1 : -1);

                int newIndexForNewOrUpdatedNameWithMatchingLanguage = GetIndexOfFirstItemMatchingInListOrInsertionPoint(nameList, nameIndex,
                    resName.NumberOfLanguages, (object o) => ((ResName)o).LanguageId, (ushort left, ushort right) => (int)left - (int)right, language, out bool exactLanguageExists);

                if (exactLanguageExists)
                {
                    if (data == null)
                    {
                        // delete item
                        nameList.RemoveAt(newIndexForNewOrUpdatedNameWithMatchingLanguage);

                        if (newNumberOfLanguages > 0)
                        {
                            // if another name is still present, update the number of languages counter
                            resName = (ResName)nameList[nameIndex];
                            resName.NumberOfLanguages = (ushort)newNumberOfLanguages;
                        }

                        if ((resType.NameHeadID.Count == 0) && (resType.NameHeadName.Count == 0))
                        {
                            /* type list completely empty? */
                            typeList.Remove(resType);
                        }
                    }
                    else
                    {
                        // Resource file has two copies of same resource... ignore second copy
                        return;
                    }
                }
                else
                {
                    // Insert a new name at the new spot
                    AddNewName(nameList, resNameType, newIndexForNewOrUpdatedNameWithMatchingLanguage, name, language, data);
                    // Update the NumberOfLanguages for the language list
                    resName = (ResName)nameList[nameIndex];
                    resName.NumberOfLanguages = (ushort)newNumberOfLanguages;
                }
            }
            else
            {
                // This is a new name in a new language list
                if (data == null)
                {
                    // Can't delete new name
                    throw new ArgumentException();
                }

                AddNewName(nameList, resNameType, nameIndex, name, language, data);
            }
        }

        private static int GetIndexOfFirstItemMatchingInListOrInsertionPoint<T>(IList list, Func<T, T, int> compareFunction, T comparand, out bool exists)
        {
            return GetIndexOfFirstItemMatchingInListOrInsertionPoint(list, 0, list.Count, (object o) => ((IUnderlyingName<T>)o).Name, compareFunction, comparand, out exists);
        }

        private static int GetIndexOfFirstItemMatchingInListOrInsertionPoint<T>(IList list, int start, int count, Func<object, T> getComparandFromListElem, Func<T, T, int> compareFunction, T comparand, out bool exists)
        {
            int i = start;
            for (; i < (start + count); i++)
            {
                int iCompare = compareFunction(comparand, getComparandFromListElem(list[i]));
                if (iCompare == 0)
                {
                    exists = true;
                    return i;
                }
                else if (iCompare < 0)
                {
                    exists = false;
                    return i;
                }
            }

            exists = false;
            if ((start + count) < list.Count)
            {
                return start + count;
            }
            return -1;
        }

        private void AddNewName(IList list, Type resNameType, int insertPoint, object name, ushort language, byte[] data)
        {
            ResName newResName = (ResName)Activator.CreateInstance(resNameType, name);
            newResName.LanguageId = language;
            newResName.NumberOfLanguages = 1;
            newResName.DataEntry = data;

            if (insertPoint == -1)
                list.Add(newResName);
            else
                list.Insert(insertPoint, newResName);
        }

        private byte[] FindResourceInternal(object name, object type, ushort language)
        {
            ResType resType = null;

            if (type is ushort)
            {
                foreach (ResType_Ordinal candidate in _resTypeHeadID)
                {
                    if (candidate.Type.Ordinal == (ushort)type)
                    {
                        resType = candidate;
                        break;
                    }
                }
            }
            if (type is string)
            {
                foreach (ResType_Name candidate in _resTypeHeadName)
                {
                    if (candidate.Type == (string)type)
                    {
                        resType = candidate;
                        break;
                    }
                }
            }

            if (resType == null)
                return null;

            if (name is ushort)
            {
                foreach (ResName_Ordinal candidate in resType.NameHeadID)
                {
                    if (candidate.Name.Ordinal != (ushort)type)
                        continue;

                    if (candidate.LanguageId != language)
                        continue;

                    return (byte[])candidate.DataEntry.Clone();
                }
            }
            if (name is string)
            {
                foreach (ResName_Name candidate in resType.NameHeadName)
                {
                    if (candidate.Name != (string)name)
                        continue;

                    if (candidate.LanguageId != language)
                        continue;

                    return (byte[])candidate.DataEntry.Clone();
                }
            }

            return null;
        }
    }
}
