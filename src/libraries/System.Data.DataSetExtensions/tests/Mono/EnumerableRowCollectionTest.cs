// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// EnumerableRowCollectionTest.cs
//
// Author:
//   Atsushi Enomoto  <atsushi@ximian.com>
//
// Copyright (C) 2008 Novell, Inc. http://www.novell.com
//

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

using System;
using System.Collections.Generic;
using System.Data;
using Xunit;

namespace MonoTests.System.Data
{
    public class EnumerableRowCollectionTest
    {
        private string _testDataSet = "Mono/testdataset1.xml";
        
        [Fact]
        public void QueryWhere()
        {
            var ds = new DataSet();
            ds.ReadXml(_testDataSet);
            var table = ds.Tables[0];
            /* schema generated as ...
            var table = ds.Tables.Add ("ScoreList");
            table.Columns.Add ("ID", typeof (int));
            table.Columns.Add ("RegisteredDate", typeof (DateTime));
            table.Columns.Add ("Name", typeof (string));
            table.Columns.Add ("Score", typeof (int));
            ds.WriteXml ("Test/System.Data/testdataset1.xsd", XmlWriteMode.WriteSchema);
            */
            var q = from line in table.AsEnumerable()
                    where line.Field<int>("Score") > 80
                    select line;
            bool iterated = false;
            foreach (var line in q)
            {
                if (iterated)
                    Assert.True(false, "should match only one raw");
                Assert.Equal(100, line["Score"]);
                iterated = true;
            }
        }

        [Fact]
        public void QueryWhereSelect ()
        {
            var ds = new DataSet ();
            ds.ReadXml (_testDataSet);
            var table = ds.Tables [0];
            var q = from line in table.AsEnumerable ()
                where line.Field<int> ("Score") > 80
                select new {
                    StudentID = line.Field<int> ("ID"),
                    StudentName = line.Field<string> ("Name"),
                    StudentScore = line.Field<int> ("Score") };
            bool iterated = false;
            foreach (var ql in q) {
                if (iterated)
                    Assert.True(false, "should match only one raw");
                Assert.Equal(100, ql.StudentScore);
                iterated = true;
            }
        }

        [Fact]
        public void QueryWhereSelectOrderBy ()
        {
            var ds = new DataSet ();
            ds.ReadXml (_testDataSet);
            var table = ds.Tables [0];
            var q = from line in table.AsEnumerable ()
                where line.Field<int> ("Score") >= 80
                orderby line.Field<int> ("ID")
                select new {
                        StudentID = line.Field<int> ("ID"),
                        StudentName = line.Field<string> ("Name"),
                        StudentScore = line.Field<int> ("Score") };
            int prevID = -1;
            foreach (var ql in q) {
                switch (prevID) {
                    case -1:
                        Assert.Equal(1, ql.StudentID);
                        break;
                    case 1:
                        Assert.Equal(4, ql.StudentID);
                        break;
                    default:
                        Assert.True(false, "should match only one raw");
                        break;
                }
	            prevID = ql.StudentID;
            }
        }

        [Fact]
        public void QueryWhereSelectOrderByDescending ()
        {
            var ds = new DataSet ();
            ds.ReadXml (_testDataSet);
            var table = ds.Tables [0];
            var q = from line in table.AsEnumerable ()
                where line.Field<int> ("Score") >= 80
                orderby line.Field<int> ("ID") descending
                select new {
                    StudentID = line.Field<int> ("ID"),
                    StudentName = line.Field<string> ("Name"),
                    StudentScore = line.Field<int> ("Score") };
            int prevID = -1;
            foreach (var ql in q) {
                switch (prevID) {
                    case -1:
                        Assert.Equal(4, ql.StudentID);
                        break;
                    case 4:
                        Assert.Equal(1, ql.StudentID);
                        break;
                    default:
                        Assert.True(false, "should match only one raw");
                        break;
                }
                prevID = ql.StudentID;
            }
        }

        [Fact]
        public void ThenBy ()
        {
            var ds = new DataSet ();
            ds.ReadXml (_testDataSet);
            var table = ds.Tables [0];
            var q = from line in table.AsEnumerable ()
                where line.Field<int> ("Score") >= 80
                orderby line.Field<bool> ("Gender"), line.Field<int> ("ID")
                select new {
                    StudentID = line.Field<int> ("ID"),
                    StudentName = line.Field<string> ("Name"),
                    StudentScore = line.Field<int> ("Score") };
            int prevID = -1;
            foreach (var ql in q) {
            switch (prevID) {
                case -1:
                    Assert.Equal(1, ql.StudentID);
                    break;
                case 1:
                    Assert.Equal(4, ql.StudentID);
                    break;
                default:
                    Assert.True(false, "should match only one raw");
                    break;
            }
            prevID = ql.StudentID;
            }
        }

        [Fact]
        public void ThenByDescending ()
        {
            var ds = new DataSet ();
            ds.ReadXml (_testDataSet);
            var table = ds.Tables [0];
            var q = from line in table.AsEnumerable ()
                where line.Field<int> ("Score") >= 80
                orderby line.Field<bool> ("Gender"), line.Field<int> ("ID") descending
                select new {
                    StudentID = line.Field<int> ("ID"),
                    StudentName = line.Field<string> ("Name"),
                    StudentScore = line.Field<int> ("Score") };
            int prevID = -1;
            foreach (var ql in q) {
                switch (prevID) {
                case -1:
                    Assert.Equal(4, ql.StudentID);
                    break;
                case 4:
                    Assert.Equal(1, ql.StudentID);
                    break;
                default:
                    Assert.True(false, "should match only one raw");
                    break;
                }
                prevID = ql.StudentID;
            }
        }
    }
}