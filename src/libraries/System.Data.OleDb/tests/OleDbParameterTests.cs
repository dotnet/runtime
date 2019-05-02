// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace System.Data.OleDb.Tests
{
    public class OleDbParameterTests : OleDbTestBase
    {
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void OleDbParameterCollection_MultipleScenarios_Success()
        {
            OleDbParameterCollection opc = new OleDbCommand().Parameters;

            Assert.True(opc.Count == 0);
            Assert.False(((Collections.IList)opc).IsReadOnly);
            Assert.False(((Collections.IList)opc).IsFixedSize);
            Assert.False(((Collections.IList)opc).IsSynchronized);
            Assert.Throws<IndexOutOfRangeException>(() => opc[0].ParameterName);
            Assert.Throws<IndexOutOfRangeException>(() => opc["@p1"].ParameterName);
            Assert.Throws<ArgumentNullException>(() => opc.Add(null));

            opc.Add((object)new OleDbParameter());
            opc.Add((object)new OleDbParameter());
            Collections.IEnumerator enm = opc.GetEnumerator();
            Assert.True(enm.MoveNext());
            Assert.Equal("Parameter1", ((OleDbParameter)enm.Current).ParameterName);
            Assert.True(enm.MoveNext());
            Assert.Equal("Parameter2", ((OleDbParameter)enm.Current).ParameterName);

            opc.Add(new OleDbParameter(null, null));
            opc.Add(null, OleDbType.Integer, 0, null);
            Assert.Equal("Parameter4", opc["Parameter4"].ParameterName);

            opc.Add(new OleDbParameter("Parameter5", OleDbType.LongVarWChar, 20));
            opc.Add(new OleDbParameter(null, OleDbType.WChar, 20, "a"));

            opc.RemoveAt(opc[3].ParameterName);
            Assert.Equal(-1, opc.IndexOf(null));
            Assert.False(opc.Contains(null));
            Assert.Throws<IndexOutOfRangeException>(() => opc.RemoveAt(null));

            OleDbParameter p = opc[0];
            Assert.Throws<ArgumentException>(() => opc.Add((object)p));
            Assert.Throws<ArgumentException>(() => new OleDbCommand().Parameters.Add(p));
            Assert.Throws<ArgumentNullException>(() => opc.Remove(null));

            string pname = p.ParameterName;
            p.ParameterName = pname;
            p.ParameterName = pname.ToUpper();
            p.ParameterName = pname.ToLower();
            p.ParameterName = "@p1";
            p.ParameterName = pname;

            opc.Clear();
            opc.Add(p);

            opc.Clear();
            opc.AddWithValue("@p1", null);

            Assert.Equal(-1, opc.IndexOf(p.ParameterName));

            opc[0] = p;
            Assert.Equal(0, opc.IndexOf(p.ParameterName));

            Assert.True(opc.Contains(p.ParameterName));
            Assert.True(opc.Contains(opc[0]));

            opc[0] = p;
            opc[p.ParameterName] = new OleDbParameter(p.ParameterName, null);
            opc[p.ParameterName] = (OleDbParameter)OleDbFactory.Instance.CreateParameter();
            opc.RemoveAt(0);

            new OleDbCommand().Parameters.Clear();
            new OleDbCommand().Parameters.CopyTo(new object[0], 0);
            Assert.False(new OleDbCommand().Parameters.GetEnumerator().MoveNext());

            Assert.Throws<InvalidCastException>(() => new OleDbCommand().Parameters.Add(0));
            Assert.Throws<ArgumentNullException>(() => new OleDbCommand().Parameters.AddRange(null));
            Assert.Throws<InvalidCastException>(() => new OleDbCommand().Parameters.Insert(0, 0));
            Assert.Throws<InvalidCastException>(() => new OleDbCommand().Parameters.Remove(0));
            
            Assert.Throws<ArgumentException>(() => opc.Remove(new OleDbParameter()));
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void IsNullable_Default_False()
        {
            var oleDbParameter = new OleDbParameter();
            Assert.False(oleDbParameter.IsNullable);
            oleDbParameter.IsNullable = true;
            Assert.True(oleDbParameter.IsNullable);
        }

        [ConditionalTheory(Helpers.IsDriverAvailable)]
        [MemberData(nameof(OleDbTypes))]
        public void Ctor_SetOleDbType_Success(OleDbType type)
        {
            var oleDbParameter = new OleDbParameter(name: "ParameterName", dataType: type);
            Assert.Equal(type, oleDbParameter.OleDbType);
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void Ctor_InvalidOleDbType_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new OleDbParameter(name: "ParameterName", dataType: (OleDbType)500));
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void ResetOleDbType_ResetsToVarWChar()
        {
            var oleDbParameter = new OleDbParameter();
            Assert.Equal(OleDbType.VarWChar, oleDbParameter.OleDbType);

            oleDbParameter.OleDbType = OleDbType.Guid;
            Assert.Equal(OleDbType.Guid, oleDbParameter.OleDbType);

            oleDbParameter.ResetOleDbType();
            Assert.Equal(OleDbType.VarWChar, oleDbParameter.OleDbType);
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void ResetDbType_ResetsToVarWChar()
        {
            var oleDbParameter = new OleDbParameter();
            Assert.Equal(DbType.String, oleDbParameter.DbType);

            oleDbParameter.DbType = DbType.DateTime;
            Assert.Equal(DbType.DateTime, oleDbParameter.DbType);

            oleDbParameter.ResetDbType();
            Assert.Equal(DbType.String, oleDbParameter.DbType);
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void SourceColumn_Success()
        {
            var oleDbParameter = new OleDbParameter(default, default, default, srcColumn: null);
            Assert.Equal(string.Empty, oleDbParameter.SourceColumn);

            oleDbParameter.SourceColumn = "someSourceColumn";
            Assert.Equal("someSourceColumn", oleDbParameter.SourceColumn);
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void PrecisionAndScale_Success()
        {
            var oleDbParameter = new OleDbParameter(default, default, default, default, default, precision: default, scale: default,
                srcColumn: default, srcVersion: DataRowVersion.Default, value: default);
            Assert.Equal(default(byte), oleDbParameter.Precision);
            Assert.Equal(default(byte), oleDbParameter.Scale);

            oleDbParameter.Precision = 10;
            Assert.Equal(10, oleDbParameter.Precision);

            oleDbParameter.Scale = 4;
            Assert.Equal(4, oleDbParameter.Scale);
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void SourceColumnNullMapping_Success()
        {
            var oleDbParameter = new OleDbParameter();
            Assert.False(oleDbParameter.SourceColumnNullMapping);

            oleDbParameter.SourceColumnNullMapping = true;
            Assert.True(oleDbParameter.SourceColumnNullMapping);
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void Size_InvalidSizeValue_Throws()
        {
            Assert.Throws<ArgumentException>(() => new OleDbParameter(default, default, size: -2));
            Assert.Throws<ArgumentException>(() => new OleDbParameter(default, default, size: -2, srcColumn: default));

            var oleDbParameter = new OleDbParameter();
            Assert.Equal(0, oleDbParameter.Size);
            oleDbParameter = new OleDbParameter(default, default, size: 10);
            Assert.Equal(10, oleDbParameter.Size);
            Assert.Throws<ArgumentException>(() => oleDbParameter.Size = -2);
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void ParameterDirection_InvalidEnumValue_Throws()
        {
            var oleDbParameter = new OleDbParameter(default, default, default, (ParameterDirection)0, 
                default, default, default, default, DataRowVersion.Original, default);
            Assert.Equal(ParameterDirection.Input, oleDbParameter.Direction);

            oleDbParameter.Direction = ParameterDirection.InputOutput;
            Assert.Equal(ParameterDirection.InputOutput, oleDbParameter.Direction);

            Assert.Throws<ArgumentOutOfRangeException>(() => oleDbParameter.Direction = (ParameterDirection)0);
        }
        
        [ConditionalTheory(Helpers.IsDriverAvailable)]
        [MemberData(nameof(ParameterDirections))]
        public void ParameterDirection_Success(ParameterDirection direction)
        {
            var oleDbParameter = new OleDbParameter(default, default, default, direction, 
                default, default, default, DataRowVersion.Original, default, default);
            Assert.Equal(direction, oleDbParameter.Direction);
        }
        
        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void SourceVersion_InvalidEnumValue_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new OleDbParameter(default, default, default, default, 
                default, default, default, default, (DataRowVersion)0, default));

            var oleDbParameter = new OleDbParameter();
            oleDbParameter.SourceVersion = DataRowVersion.Proposed;
            Assert.Equal(DataRowVersion.Proposed, oleDbParameter.SourceVersion);

            Assert.Throws<ArgumentOutOfRangeException>(() => oleDbParameter.SourceVersion = (DataRowVersion)0);
        }
        
        [ConditionalTheory(Helpers.IsDriverAvailable)]
        [MemberData(nameof(DataRowVersions))]
        public void SourceVersion_Success(DataRowVersion dataRowVersion)
        {
            var oleDbParameter = new OleDbParameter(default, default, default, default, 
                default, default, default, dataRowVersion, default, default);
            Assert.Equal(dataRowVersion, oleDbParameter.SourceVersion);
        }

        [ConditionalFact(Helpers.IsDriverAvailable)]
        public void Value_Success()
        {
            const string ParameterName = "Name";
            const string ParameterValue = "Value";
            OleDbParameter oleDbParameter;

            oleDbParameter = new OleDbParameter(null, null);
            Assert.Equal(string.Empty, oleDbParameter.ToString());
            Assert.Equal(string.Empty, oleDbParameter.ParameterName);
            Assert.Null(oleDbParameter.Value);

            oleDbParameter = new OleDbParameter(ParameterName, null);
            Assert.Equal(ParameterName, oleDbParameter.ToString());
            Assert.Equal(ParameterName, oleDbParameter.ParameterName);
            Assert.Null(oleDbParameter.Value);
            
            oleDbParameter = new OleDbParameter(ParameterName, ParameterValue);
            Assert.Equal(ParameterName, oleDbParameter.ToString());
            Assert.Equal(ParameterName, oleDbParameter.ParameterName);
            Assert.Equal(ParameterValue, oleDbParameter.Value);
        }

        public static IEnumerable<object[]> ParameterDirections
        {
            get
            {
                yield return new object[] { ParameterDirection.Input };
                yield return new object[] { ParameterDirection.Output };
                yield return new object[] { ParameterDirection.InputOutput };
                yield return new object[] { ParameterDirection.ReturnValue };
            }
        }

        public static IEnumerable<object[]> DataRowVersions
        {
            get
            {
                yield return new object[] { DataRowVersion.Original };
                yield return new object[] { DataRowVersion.Current };
                yield return new object[] { DataRowVersion.Proposed };
                yield return new object[] { DataRowVersion.Default };
            }
        }

        public static IEnumerable<object[]> OleDbTypes
        {
            get
            {
                foreach (var x in Enum.GetValues(typeof(OleDbType)).Cast<OleDbType>())
                    yield return new object[] { x };
            }
        }
    }
}