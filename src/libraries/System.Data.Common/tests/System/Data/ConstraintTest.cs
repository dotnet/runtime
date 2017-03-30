// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

// (C) 2002 Franklin Wise
// (C) 2003 Martin Willemoes Hansen
// 

//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
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
    public class ConstraintTest
    {
        private DataTable _table;
        private Constraint _constraint1;
        private Constraint _constraint2;

        public ConstraintTest()
        {
            //Setup DataTable
            _table = new DataTable("TestTable");

            _table.Columns.Add("Col1", typeof(int));
            _table.Columns.Add("Col2", typeof(int));

            //Use UniqueConstraint to test Constraint Base Class
            _constraint1 = new UniqueConstraint(_table.Columns[0], false);
            _constraint2 = new UniqueConstraint(_table.Columns[1], false);

            // not sure why this is needed since a new _table was just created
            // for us, but this Clear() keeps the tests from throwing
            // an exception when the Add() is called.
            _table.Constraints.Clear();
        }

        [Fact]
        public void SetConstraintNameNullOrEmptyExceptions()
        {
            bool exceptionCaught = false;
            string name = null;

            _table.Constraints.Add(_constraint1);

            for (int i = 0; i <= 1; i++)
            {
                exceptionCaught = false;
                if (0 == i)
                    name = null;
                if (1 == i)
                    name = string.Empty;

                try
                {
                    //Next line should throw ArgumentException
                    //Because ConstraintName can't be set to null
                    //or empty while the constraint is part of the
                    //collection
                    _constraint1.ConstraintName = name;
                }
                catch (ArgumentException)
                {
                    exceptionCaught = true;
                }
                catch
                {
                    Assert.False(true);
                }

                Assert.True(exceptionCaught);
            }
        }

        [Fact]
        public void SetConstraintNameDuplicateException()
        {
            _constraint1.ConstraintName = "Dog";
            _constraint2.ConstraintName = "Cat";

            _table.Constraints.Add(_constraint1);
            _table.Constraints.Add(_constraint2);

            Assert.Throws<DuplicateNameException>(() => _constraint2.ConstraintName = "Dog");
        }

        [Fact]
        public void ToStringTest()
        {
            _constraint1.ConstraintName = "Test";
            Assert.Equal(_constraint1.ConstraintName, _constraint1.ToString());

            _constraint1.ConstraintName = null;
            Assert.NotNull(_constraint1.ToString());
        }

        [Fact]
        public void GetExtendedProperties()
        {
            PropertyCollection col = _constraint1.ExtendedProperties as
                PropertyCollection;

            Assert.NotNull(col);
        }
    }
}
