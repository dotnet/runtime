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
//

using Xunit;


namespace System.Data.Tests
{
    public class DuplicateNameExceptionTest
    {
        [Fact]
        public void Generate()
        {
            var ds = new DataSet();
            ds.Tables.Add(new DataTable("Table"));
            ds.Tables.Add(new DataTable("Table1"));
            ds.Tables[0].Columns.Add(new DataColumn("Column"));
            ds.Tables[0].Columns.Add(new DataColumn("Column1"));
            ds.Tables[0].Columns.Add(new DataColumn("Column2"));
            ds.Tables[1].Columns.Add(new DataColumn("Column"));

            ds.Relations.Add(new DataRelation("Relation", ds.Tables[0].Columns[0], ds.Tables[1].Columns[0]));
            ds.Tables[0].Constraints.Add(new UniqueConstraint("Constraint", ds.Tables[0].Columns[1]));

            // DuplicateNameException - tables 
            Assert.Throws<DuplicateNameException>(() =>
            {
                ds.Tables.Add(new DataTable("Table"));
            });

            // DuplicateNameException - Column 
            Assert.Throws<DuplicateNameException>(() =>
            {
                ds.Tables[0].Columns.Add(new DataColumn("Column"));
            });

            // DuplicateNameException - Constraints 
            Assert.Throws<DuplicateNameException>(() =>
            {
                ds.Tables[0].Constraints.Add(new UniqueConstraint("Constraint", ds.Tables[0].Columns[2]));
            });

            // DuplicateNameException - Relations 
            Assert.Throws<DuplicateNameException>(() =>
            {
                ds.Relations.Add(new DataRelation("Relation", ds.Tables[0].Columns[1], ds.Tables[1].Columns[0]));
            });
        }
    }
}
