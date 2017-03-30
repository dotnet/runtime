// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

// Copyright (c) 2004 Mainsoft Co.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Xunit;

namespace System.Data.Tests
{
    public class ConstraintExceptionTest
    {
        [Fact]
        public void Generate()
        {
            DataTable dtParent = DataProvider.CreateParentDataTable();
            DataTable dtChild = DataProvider.CreateChildDataTable();

            var ds = new DataSet();
            ds.Tables.Add(dtChild);
            ds.Tables.Add(dtParent);

            //------ check UniqueConstraint ---------

            //create unique constraint
            UniqueConstraint uc;

            //Column type = int
            uc = new UniqueConstraint(dtParent.Columns[0]);
            dtParent.Constraints.Add(uc);
            Assert.Throws<ConstraintException>(() => dtParent.Rows.Add(dtParent.Rows[0].ItemArray)); //add exisiting value - will raise exception

            //Column type = DateTime
            dtParent.Constraints.Clear();
            uc = new UniqueConstraint(dtParent.Columns["ParentDateTime"]);
            dtParent.Constraints.Add(uc);
            Assert.Throws<ConstraintException>(() => dtParent.Rows.Add(dtParent.Rows[0].ItemArray)); //add exisiting value - will raise exception

            //Column type = double
            dtParent.Constraints.Clear();
            uc = new UniqueConstraint(dtParent.Columns["ParentDouble"]);
            dtParent.Constraints.Add(uc);
            Assert.Throws<ConstraintException>(() => dtParent.Rows.Add(dtParent.Rows[0].ItemArray)); //add exisiting value - will raise exception

            //Column type = string
            dtParent.Constraints.Clear();
            uc = new UniqueConstraint(dtParent.Columns["String1"]);
            dtParent.Constraints.Add(uc);
            Assert.Throws<ConstraintException>(() => dtParent.Rows.Add(dtParent.Rows[0].ItemArray)); //add exisiting value - will raise exception

            //Column type = string, ds.CaseSensitive = false;
            ds.CaseSensitive = false;
            dtParent.Constraints.Clear();
            uc = new UniqueConstraint(dtParent.Columns["String1"]);
            dtParent.Constraints.Add(uc);
            DataRow dr = dtParent.NewRow();
            dr.ItemArray = dtParent.Rows[0].ItemArray;
            dr["String1"] = dr["String1"].ToString().ToUpper();

            // UniqueConstraint Exception - Column type = String, CaseSensitive = false;
            Assert.Throws<ConstraintException>(() => dtParent.Rows.Add(dr));

            ds.CaseSensitive = true;

            //Column type = string, ds.CaseSensitive = true;
            dtParent.Constraints.Clear();
            uc = new UniqueConstraint(dtParent.Columns["String1"]);
            dtParent.Constraints.Add(uc);

            // No UniqueConstraint Exception - Column type = String, CaseSensitive = true;
            dtParent.Rows.Add(dr);

            // Column type = string, ds.CaseSensitive = false;
            // UniqueConstraint Exception - Column type = String, Enable CaseSensitive = true;
            Assert.Throws<ConstraintException>(() => ds.CaseSensitive = false);

            dtChild.Constraints.Add(new UniqueConstraint(new DataColumn[] { dtChild.Columns[0], dtChild.Columns[1] }));
            ds.EnforceConstraints = false;
            dtChild.Rows.Add(dtChild.Rows[0].ItemArray);

            // UniqueConstraint Exception - ds.EnforceConstraints 
            Assert.Throws<ConstraintException>(() => ds.EnforceConstraints = true);
        }
    }
}
