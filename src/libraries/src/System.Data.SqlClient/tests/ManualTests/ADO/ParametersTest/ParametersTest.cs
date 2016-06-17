// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Data.SqlTypes;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public static class ParametersTest
    {
        private static string s_connString = DataTestUtility.TcpConnStr;

        [CheckConnStrSetupFact]
        public static void CodeCoverageSqlClient()
        {
            SqlParameterCollection opc = new SqlCommand().Parameters;

            Assert.True(opc.Count == 0, string.Format("FAILED: Expected count: {0}. Actual count: {1}.", 0, opc.Count));
            Assert.False(((IList)opc).IsReadOnly, "FAILED: Expected collection to NOT be read only.");
            Assert.False(((IList)opc).IsFixedSize, "FAILED: Expected collection to NOT be fixed size.");
            Assert.False(((IList)opc).IsSynchronized, "FAILED: Expected collection to NOT be synchronized.");
            DataTestUtility.AssertEqualsWithDescription("Object", ((IList)opc).SyncRoot.GetType().Name, "FAILED: Incorrect SyncRoot Name");

            {
                string failValue;
                DataTestUtility.AssertThrowsWrapper<IndexOutOfRangeException>(() => failValue = opc[0].ParameterName, "Invalid index 0 for this SqlParameterCollection with Count=0.");

                DataTestUtility.AssertThrowsWrapper<IndexOutOfRangeException>(() => failValue = opc["@p1"].ParameterName, "An SqlParameter with ParameterName '@p1' is not contained by this SqlParameterCollection.");
            }

            DataTestUtility.AssertThrowsWrapper<ArgumentNullException>(() => opc.Add(null), "The SqlParameterCollection only accepts non-null SqlParameter type objects.");

            opc.Add((object)new SqlParameter());
            IEnumerator enm = opc.GetEnumerator();
            Assert.True(enm.MoveNext(), "FAILED: Expected MoveNext to be true");
            DataTestUtility.AssertEqualsWithDescription("Parameter1", ((SqlParameter)enm.Current).ParameterName, "FAILED: Incorrect ParameterName");

            opc.Add(new SqlParameter());
            DataTestUtility.AssertEqualsWithDescription("Parameter2", opc[1].ParameterName, "FAILED: Incorrect ParameterName");

            opc.Add(new SqlParameter(null, null));
            opc.Add(new SqlParameter(null, SqlDbType.Int));
            DataTestUtility.AssertEqualsWithDescription("Parameter4", opc["Parameter4"].ParameterName, "FAILED: Incorrect ParameterName");

            opc.Add(new SqlParameter("Parameter5", SqlDbType.NVarChar, 20));
            opc.Add(new SqlParameter(null, SqlDbType.NVarChar, 20, "a"));
            opc.RemoveAt(opc[3].ParameterName);
            DataTestUtility.AssertEqualsWithDescription(-1, opc.IndexOf(null), "FAILED: Incorrect index for null value");

            SqlParameter p = opc[0];

            DataTestUtility.AssertThrowsWrapper<ArgumentException>(() => opc.Add((object)p), "The SqlParameter is already contained by another SqlParameterCollection.");

            DataTestUtility.AssertThrowsWrapper<ArgumentException>(() => new SqlCommand().Parameters.Add(p), "The SqlParameter is already contained by another SqlParameterCollection.");

            DataTestUtility.AssertThrowsWrapper<ArgumentNullException>(() => opc.Remove(null), "The SqlParameterCollection only accepts non-null SqlParameter type objects.");

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

            DataTestUtility.AssertEqualsWithDescription(-1, opc.IndexOf(p.ParameterName), "FAILED: Incorrect index for parameter name");

            opc[0] = p;
            DataTestUtility.AssertEqualsWithDescription(0, opc.IndexOf(p.ParameterName), "FAILED: Incorrect index for parameter name");

            Assert.True(opc.Contains(p.ParameterName), "FAILED: Expected collection to contain provided parameter.");
            Assert.True(opc.Contains(opc[0]), "FAILED: Expected collection to contain provided parameter.");

            opc[0] = p;
            opc[p.ParameterName] = new SqlParameter(p.ParameterName, null);
            opc[p.ParameterName] = new SqlParameter();
            opc.RemoveAt(0);

            new SqlCommand().Parameters.Clear();
            new SqlCommand().Parameters.CopyTo(new object[0], 0);
            Assert.False(new SqlCommand().Parameters.GetEnumerator().MoveNext(), "FAILED: Expected MoveNext to be false");

            DataTestUtility.AssertThrowsWrapper<InvalidCastException>(() => new SqlCommand().Parameters.Add(0), "The SqlParameterCollection only accepts non-null SqlParameter type objects, not Int32 objects.");

            DataTestUtility.AssertThrowsWrapper<InvalidCastException>(() => new SqlCommand().Parameters.Insert(0, 0), "The SqlParameterCollection only accepts non-null SqlParameter type objects, not Int32 objects.");

            DataTestUtility.AssertThrowsWrapper<InvalidCastException>(() => new SqlCommand().Parameters.Remove(0), "The SqlParameterCollection only accepts non-null SqlParameter type objects, not Int32 objects.");

            DataTestUtility.AssertThrowsWrapper<ArgumentException>(() => new SqlCommand().Parameters.Remove(new SqlParameter()), "Attempted to remove an SqlParameter that is not contained by this SqlParameterCollection.");
        }

        [CheckConnStrSetupFact]
        public static void Test_WithEnumValue_ShouldInferToUnderlyingType()
        {
            using (var conn = new SqlConnection(s_connString))
            {
                conn.Open();
                var cmd = new SqlCommand("select @input", conn);
                cmd.Parameters.AddWithValue("@input", MyEnum.B);
                object value = cmd.ExecuteScalar();
                Assert.Equal((MyEnum)value, MyEnum.B);
            }
        }

        [CheckConnStrSetupFact]
        public static void Test_WithOutputEnumParameter_ShouldReturnEnum()
        {
            using (var conn = new SqlConnection(s_connString))
            {
                conn.Open();
                var cmd = new SqlCommand("set @output = @input", conn);
                cmd.Parameters.AddWithValue("@input", MyEnum.B);

                var outputParam = cmd.CreateParameter();
                outputParam.ParameterName = "@output";
                outputParam.DbType = DbType.Int32;
                outputParam.Direction = ParameterDirection.Output;
                cmd.Parameters.Add(outputParam);

                cmd.ExecuteNonQuery();

                Assert.Equal((MyEnum)outputParam.Value, MyEnum.B);
            }
        }

        [CheckConnStrSetupFact]
        public static void Test_WithDecimalValue_ShouldReturnDecimal()
        {
            using (var conn = new SqlConnection(s_connString))
            {
                conn.Open();
                var cmd = new SqlCommand("select @foo", conn);
                cmd.Parameters.AddWithValue("@foo", new SqlDecimal(0.5));
                var result = (decimal)cmd.ExecuteScalar();
                Assert.Equal(result, (decimal)0.5);
            }
        }

        [CheckConnStrSetupFact]
        public static void Test_WithGuidValue_ShouldReturnGuid()
        {
            using (var conn = new SqlConnection(s_connString))
            {
                conn.Open();
                var expectedGuid = Guid.NewGuid();
                var cmd = new SqlCommand("select @input", conn);
                cmd.Parameters.AddWithValue("@input", expectedGuid);
                var result = cmd.ExecuteScalar();
                Assert.Equal(expectedGuid, (Guid)result);
            }
        }

        private enum MyEnum
        {
            A = 1,
            B = 2
        }

    }
}
