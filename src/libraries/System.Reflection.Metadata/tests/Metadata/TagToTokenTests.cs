// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using Xunit;

using BitArithmetic = System.Reflection.Internal.BitArithmetic;

namespace System.Reflection.Metadata.Tests
{
    public class TagToTokenTests
    {
        private sealed class XxxTag
        {
            public Func<int> GetNumberOfBits;
            public Func<uint[]> GetTagToTokenTypeArray;
            public Func<TableMask> GetTablesReferenced;
            public Func<uint, EntityHandle> ConvertToHandle;
            public Func<EntityHandle, uint?> ConvertToTag;
            public Func<uint> GetTagMask;
            public Func<string, uint?> GetTagValue;
            public string Name;
        }

        private IEnumerable<XxxTag> GetTags()
        {
            Type[] types = new[] {
                typeof(CustomAttributeTypeTag),
                typeof(HasConstantTag),
                typeof(HasCustomAttributeTag),
                typeof(HasDeclSecurityTag),
                typeof(HasFieldMarshalTag),
                typeof(HasSemanticsTag),
                typeof(ImplementationTag),
                typeof(MemberForwardedTag),
                typeof(MemberRefParentTag),
                typeof(MethodDefOrRefTag),
                typeof(ResolutionScopeTag),
                typeof(TypeDefOrRefTag),
                typeof(TypeOrMethodDefTag)
            };

            return from type in types
                   let typeInfo = type.GetTypeInfo()
                   select new XxxTag
                   {
                       GetTagToTokenTypeArray = () =>
                       {
                           var array = typeInfo.GetDeclaredField("TagToTokenTypeArray");
                           var vector = typeInfo.GetDeclaredField("TagToTokenTypeByteVector");

                           Assert.True((array == null) ^ (vector == null), typeInfo.Name + " does not have exactly one of TagToTokenTypeArray or TagToTokenTypeByteVector");

                           if (array != null)
                           {
                               return (uint[])array.GetValue(null);
                           }

                           Assert.Contains(vector.FieldType, new[] { typeof(uint), typeof(ulong) });
                           if (vector.FieldType == typeof(uint))
                           {
                               uint value = (uint)vector.GetValue(null);
                               uint[] ret = new uint[4];
                               for (uint i = 0; i < 4; i++)
                               {
                                   ret[i] = ((uint)(byte)value) << TokenTypeIds.RowIdBitCount;
                                   value >>= 8;
                               }
                               return ret;
                           }
                           else
                           {
                               ulong value = (ulong)vector.GetValue(null);
                               uint[] ret = new uint[8];
                               for (uint i = 0; i < 8; i++)
                               {
                                   ret[i] = ((uint)(byte)value) << TokenTypeIds.RowIdBitCount;
                                   value >>= 8;
                               }
                               return ret;
                           }
                       },

                       GetNumberOfBits = () => (int)typeInfo.GetDeclaredField("NumberOfBits").GetValue(null),
                       ConvertToHandle = (Func<uint, EntityHandle>)typeInfo.GetDeclaredMethod("ConvertToHandle").CreateDelegate(typeof(Func<uint, EntityHandle>)),
                       ConvertToTag = handle => { var m = typeInfo.GetDeclaredMethod("ConvertToTag"); return m == null ? null : (uint?)m.Invoke(null, new object[] { handle }); },
                       GetTablesReferenced = () => (TableMask)typeInfo.GetDeclaredField("TablesReferenced").GetValue(null),
                       GetTagMask = () => (uint)typeInfo.GetDeclaredField("TagMask").GetValue(null),
                       GetTagValue = name => { var f = typeInfo.GetDeclaredField(name); return f == null ? null : (uint?)f.GetValue(null); },
                       Name = typeInfo.Name
                   };
        }

        [Fact]
        public void ValidateTagToTokenConversion()
        {
            foreach (var tag in GetTags())
            {
                Assert.True((1 << tag.GetNumberOfBits()) <= tag.GetTagToTokenTypeArray().Length,
                    tag.Name + " has mismatch between NumberOfBits and TagToTokenTypeArray.Length");

                Assert.True(tag.GetTagMask() == (1 << tag.GetNumberOfBits()) - 1,
                    tag.Name + " has mismatch between NumberOfBits and TagMask");

                TableMask tablesNotUsed = tag.GetTablesReferenced();

                Assert.True(tablesNotUsed != 0,
                    tag.Name + " does not have anything in TablesReferenced.");

                int badTagCount = 0;
                Random random = new Random(42);

                for (uint i = 0, n = (uint)(1 << tag.GetNumberOfBits()); i < n; i++)
                {
                    uint rowId = (uint)random.Next(((int)TokenTypeIds.RIDMask + 1));
                    uint codedIndex = i | (rowId << tag.GetNumberOfBits());

                    EntityHandle handle;

                    try
                    {
                        handle = tag.ConvertToHandle(codedIndex);
                    }
                    catch (BadImageFormatException)
                    {
                        badTagCount++;
                        continue;
                    }

                    Assert.True(handle.RowId == rowId,
                        tag.Name + " did not return correct row id.");

                    uint badRowId = (uint)random.Next((int)TokenTypeIds.RIDMask + 1, int.MaxValue);
                    Assert.Throws<BadImageFormatException>(() => tag.ConvertToHandle(i | ~tag.GetTagMask()));
                    Assert.Throws<BadImageFormatException>(() => tag.ConvertToHandle(i | ((TokenTypeIds.RIDMask + 1) << tag.GetNumberOfBits())));
                    Assert.Throws<BadImageFormatException>(() => tag.ConvertToHandle(i | (badRowId << tag.GetNumberOfBits())));

                    Assert.True((uint)(handle.Kind) << 24 == tag.GetTagToTokenTypeArray()[i],
                        tag.Name + " did not return handle type matching its TagToTokenTypeArray or TagToTokenTypeByteVector");

                    TableMask handleTableMask = (TableMask)(1UL << (int)handle.Kind);

                    Assert.True(tag.GetTagValue(handleTableMask.ToString()) == i,
                        tag.Name + " does not have a constant for '" + handleTableMask.ToString() + "' with matching value: " + i);

                    Assert.True((tag.GetTablesReferenced() & handleTableMask) == handleTableMask,
                        tag.Name + " does not declare that it references '" + handleTableMask.ToString() + "' table.");

                    TableMask tablesNotUsedPreviously = tablesNotUsed;
                    tablesNotUsed &= ~handleTableMask;

                    Assert.True(handleTableMask == 0 || tablesNotUsedPreviously != tablesNotUsed,
                        tag.Name + " did not use any table for tag value: " + i);

                    uint? roundTripped = tag.ConvertToTag(handle);
                    Assert.True(roundTripped == null || roundTripped == codedIndex,
                        tag.Name + " did not round trip coded index -> handle -> coded index");
                }

                Assert.True(badTagCount == (1 << tag.GetNumberOfBits()) - BitArithmetic.CountBits((ulong)tag.GetTablesReferenced()),
                    tag.Name + " did not find the correct number of bad tags.");

                Assert.True(tablesNotUsed == 0,
                    tag.Name + " did not use all of TablesReferenced when passed all possible tag bits up to NumberOfBits.");
            }
        }
    }
}
