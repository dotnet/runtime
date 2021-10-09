// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Copyright (C) 2008 Daniel Morgan
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

#region Using directives

using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Text.RegularExpressions;

using Xunit;

#endregion

namespace System.Data.Tests.Common
{
    public class DbConnectionStringBuilderTest
    {
        private DbConnectionStringBuilder _builder = null;
        private const string SERVER = "SERVER";
        private const string SERVER_VALUE = "localhost";

        public DbConnectionStringBuilderTest()
        {
            _builder = new DbConnectionStringBuilder();
        }

        [Fact]
        public void Add()
        {
            _builder.Add("driverid", "420");
            _builder.Add("driverid", "560");
            _builder.Add("DriverID", "840");
            Assert.Equal("840", _builder["driverId"]);
            Assert.True(_builder.ContainsKey("driverId"));
            _builder.Add("Driver", "OdbcDriver");
            Assert.Equal("OdbcDriver", _builder["Driver"]);
            Assert.True(_builder.ContainsKey("Driver"));
            _builder.Add("Driver", "{OdbcDriver");
            Assert.Equal("{OdbcDriver", _builder["Driver"]);
            Assert.True(_builder.ContainsKey("Driver"));
            _builder.Add("Dsn", "MyDsn");
            Assert.Equal("MyDsn", _builder["Dsn"]);
            Assert.True(_builder.ContainsKey("Dsn"));
            _builder.Add("dsN", "MyDsn2");
            Assert.Equal("MyDsn2", _builder["Dsn"]);
            Assert.True(_builder.ContainsKey("Dsn"));
        }

        [Fact]
        public void Add_Keyword_Invalid()
        {
            string[] invalid_keywords = new string[] {
                string.Empty,
                " ",
                " abc",
                "abc ",
                "\r",
                "ab\rc",
                ";abc",
                "a\0b"
                };

            for (int i = 0; i < invalid_keywords.Length; i++)
            {
                string keyword = invalid_keywords[i];

                ArgumentException ex = Assert.Throws<ArgumentException>(() => _builder.Add(keyword, "abc"));
                // Invalid keyword, contain one or more of 'no characters',
                // 'control characters', 'leading or trailing whitespace'
                // or 'leading semicolons'
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.Contains(keyword, ex.Message);
                Assert.Equal(keyword, ex.ParamName);
            }
        }

        [Fact]
        public void Add_Keyword_Null()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => _builder.Add(null, "abc"));
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
            Assert.Equal("keyword", ex.ParamName);
        }

        [Fact]
        public void ConnectionString()
        {
            DbConnectionStringBuilder sb;

            sb = new DbConnectionStringBuilder();
            sb.ConnectionString = "A=B";
            Assert.True(sb.ContainsKey("A"));
            Assert.Equal("a=B", sb.ConnectionString);
            Assert.Equal(1, sb.Count);
            Assert.Equal(1, sb.Keys.Count);

            sb.ConnectionString = null;
            Assert.False(sb.ContainsKey("A"));
            Assert.Equal(string.Empty, sb.ConnectionString);
            Assert.Equal(0, sb.Count);
            Assert.Equal(0, sb.Keys.Count);

            sb = new DbConnectionStringBuilder();
            sb.ConnectionString = "A=B";
            sb.ConnectionString = string.Empty;
            Assert.False(sb.ContainsKey("A"));
            Assert.Equal(string.Empty, sb.ConnectionString);
            Assert.Equal(0, sb.Count);
            Assert.Equal(0, sb.Keys.Count);

            sb = new DbConnectionStringBuilder();
            sb.ConnectionString = "A=B";
            sb.ConnectionString = "\r ";
            Assert.False(sb.ContainsKey("A"));
            Assert.Equal(string.Empty, sb.ConnectionString);
            Assert.Equal(0, sb.Count);
            Assert.Equal(0, sb.Keys.Count);
        }

        [Fact]
        public void ConnectionString_Value_Empty()
        {
            DbConnectionStringBuilder[] sbs = new DbConnectionStringBuilder[] {
                new DbConnectionStringBuilder (),
                new DbConnectionStringBuilder (false),
                new DbConnectionStringBuilder (true)
                };

            foreach (DbConnectionStringBuilder sb in sbs)
            {
                sb.ConnectionString = "A=";
                Assert.False(sb.ContainsKey("A"));
                Assert.Equal(string.Empty, sb.ConnectionString);
                Assert.Equal(0, sb.Count);
            }
        }

        [Fact]
        public void Clear()
        {
            DbConnectionStringBuilder[] sbs = new DbConnectionStringBuilder[] {
                new DbConnectionStringBuilder (),
                new DbConnectionStringBuilder (false),
                new DbConnectionStringBuilder (true)
                };

            foreach (DbConnectionStringBuilder sb in sbs)
            {
                sb["Dbq"] = "C:\\Data.xls";
                sb["Driver"] = "790";
                sb.Add("Port", "56");
                sb.Clear();
                Assert.Equal(string.Empty, sb.ConnectionString);
                Assert.False(sb.ContainsKey("Dbq"));
                Assert.False(sb.ContainsKey("Driver"));
                Assert.False(sb.ContainsKey("Port"));
                Assert.Equal(0, sb.Count);
                Assert.Equal(0, sb.Keys.Count);
                Assert.Equal(0, sb.Values.Count);
            }
        }

        [Fact]
        public void AddDuplicateTest()
        {
            _builder.Add(SERVER, SERVER_VALUE);
            _builder.Add(SERVER, SERVER_VALUE);
            // should allow duplicate addition. rather, it should re-assign
            Assert.Equal(SERVER + "=" + SERVER_VALUE, _builder.ConnectionString);
        }

        [Fact]
        public void Indexer()
        {
            _builder["abc Def"] = "xa 34";
            Assert.Equal("xa 34", _builder["abc def"]);
            Assert.Equal("abc Def=\"xa 34\"", _builder.ConnectionString);
            _builder["na;"] = "abc;";
            Assert.Equal("abc;", _builder["na;"]);
            Assert.Equal("abc Def=\"xa 34\";na;=\"abc;\"", _builder.ConnectionString);
            _builder["Na;"] = "de\rfg";
            Assert.Equal("de\rfg", _builder["na;"]);
            Assert.Equal("abc Def=\"xa 34\";na;=\"de\rfg\"", _builder.ConnectionString);
            _builder["val"] = ";xyz";
            Assert.Equal(";xyz", _builder["val"]);
            Assert.Equal("abc Def=\"xa 34\";na;=\"de\rfg\";val=\";xyz\"", _builder.ConnectionString);
            _builder["name"] = string.Empty;
            Assert.Equal(string.Empty, _builder["name"]);
            Assert.Equal("abc Def=\"xa 34\";na;=\"de\rfg\";val=\";xyz\";name=", _builder.ConnectionString);
            _builder["name"] = " ";
            Assert.Equal(" ", _builder["name"]);
            Assert.Equal("abc Def=\"xa 34\";na;=\"de\rfg\";val=\";xyz\";name=\" \"", _builder.ConnectionString);

            _builder = new DbConnectionStringBuilder(false);
            _builder["abc Def"] = "xa 34";
            Assert.Equal("xa 34", _builder["abc def"]);
            Assert.Equal("abc Def=\"xa 34\"", _builder.ConnectionString);
            _builder["na;"] = "abc;";
            Assert.Equal("abc;", _builder["na;"]);
            Assert.Equal("abc Def=\"xa 34\";na;=\"abc;\"", _builder.ConnectionString);
            _builder["Na;"] = "de\rfg";
            Assert.Equal("de\rfg", _builder["na;"]);
            Assert.Equal("abc Def=\"xa 34\";na;=\"de\rfg\"", _builder.ConnectionString);
            _builder["val"] = ";xyz";
            Assert.Equal(";xyz", _builder["val"]);
            Assert.Equal("abc Def=\"xa 34\";na;=\"de\rfg\";val=\";xyz\"", _builder.ConnectionString);
            _builder["name"] = string.Empty;
            Assert.Equal(string.Empty, _builder["name"]);
            Assert.Equal("abc Def=\"xa 34\";na;=\"de\rfg\";val=\";xyz\";name=", _builder.ConnectionString);
            _builder["name"] = " ";
            Assert.Equal(" ", _builder["name"]);
            Assert.Equal("abc Def=\"xa 34\";na;=\"de\rfg\";val=\";xyz\";name=\" \"", _builder.ConnectionString);

            _builder = new DbConnectionStringBuilder(true);
            _builder["abc Def"] = "xa 34";
            Assert.Equal("xa 34", _builder["abc def"]);
            Assert.Equal("abc Def=xa 34", _builder.ConnectionString);
            _builder["na;"] = "abc;";
            Assert.Equal("abc;", _builder["na;"]);
            Assert.Equal("abc Def=xa 34;na;={abc;}", _builder.ConnectionString);
            _builder["Na;"] = "de\rfg";
            Assert.Equal("de\rfg", _builder["na;"]);
            Assert.Equal("abc Def=xa 34;na;=de\rfg", _builder.ConnectionString);
            _builder["val"] = ";xyz";
            Assert.Equal(";xyz", _builder["val"]);
            Assert.Equal("abc Def=xa 34;na;=de\rfg;val={;xyz}", _builder.ConnectionString);
            _builder["name"] = string.Empty;
            Assert.Equal(string.Empty, _builder["name"]);
            Assert.Equal("abc Def=xa 34;na;=de\rfg;val={;xyz};name=", _builder.ConnectionString);
            _builder["name"] = " ";
            Assert.Equal(" ", _builder["name"]);
            Assert.Equal("abc Def=xa 34;na;=de\rfg;val={;xyz};name= ", _builder.ConnectionString);
        }

        [Fact]
        public void Indexer_Keyword_Invalid()
        {
            string[] invalid_keywords = new string[] {
                string.Empty,
                " ",
                " abc",
                "abc ",
                "\r",
                "ab\rc",
                ";abc",
                "a\0b"
                };

            for (int i = 0; i < invalid_keywords.Length; i++)
            {
                string keyword = invalid_keywords[i];
                ArgumentException ex = Assert.Throws<ArgumentException>(() => _builder[keyword] = "abc");
                // Invalid keyword, contain one or more of 'no characters',
                // 'control characters', 'leading or trailing whitespace'
                // or 'leading semicolons'
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.Contains(keyword, ex.Message);
                Assert.Equal(keyword, ex.ParamName);

                _builder[keyword] = null;
                Assert.False(_builder.ContainsKey(keyword));

                ArgumentException ex2 = Assert.Throws<ArgumentException>(() => _builder[keyword]);
                // Keyword not supported: '...'
                Assert.Null(ex2.InnerException);
                Assert.NotNull(ex2.Message);
                // \p{Pi} any kind of opening quote https://www.compart.com/en/unicode/category/Pi
                // \p{Pf} any kind of closing quote https://www.compart.com/en/unicode/category/Pf
                // \p{Po} any kind of punctuation character that is not a dash, bracket, quote or connector https://www.compart.com/en/unicode/category/Po
                Assert.Matches(@"[\p{Pi}\p{Po}]" + Regex.Escape(keyword) + @"[\p{Pf}\p{Po}]", ex2.Message);
                Assert.Null(ex2.ParamName);
            }
        }

        [Fact]
        public void Indexer_Keyword_NotSupported()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(() => _builder["abc"]);
            // Keyword not supported: 'abc'
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
            // \p{Pi} any kind of opening quote https://www.compart.com/en/unicode/category/Pi
            // \p{Pf} any kind of closing quote https://www.compart.com/en/unicode/category/Pf
            // \p{Po} any kind of punctuation character that is not a dash, bracket, quote or connector https://www.compart.com/en/unicode/category/Po
            Assert.Matches(@"[\p{Pi}\p{Po}]" + "abc" + @"[\p{Pf}\p{Po}]", ex.Message);
            Assert.Null(ex.ParamName);
        }

        [Fact]
        public void Indexer_Keyword_Null()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => _builder[null] = "abc");
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
            Assert.Equal("keyword", ex.ParamName);

            ArgumentNullException ex2 = Assert.Throws<ArgumentNullException>(() => _builder[null] = null);
            Assert.Null(ex2.InnerException);
            Assert.NotNull(ex2.Message);
            Assert.Equal("keyword", ex2.ParamName);

            ArgumentNullException ex3 = Assert.Throws<ArgumentNullException>(() => _builder[null]);
            Assert.Null(ex3.InnerException);
            Assert.NotNull(ex3.Message);
            Assert.Equal("keyword", ex3.ParamName);
        }

        [Fact]
        public void Indexer_Value_Null()
        {
            _builder["DriverID"] = null;
            Assert.Equal(string.Empty, _builder.ConnectionString);

            ArgumentException ex = Assert.Throws<ArgumentException>(() => _builder["DriverID"]);
            // Keyword not supported: 'DriverID'
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
            // \p{Pi} any kind of opening quote https://www.compart.com/en/unicode/category/Pi
            // \p{Pf} any kind of closing quote https://www.compart.com/en/unicode/category/Pf
            // \p{Po} any kind of punctuation character that is not a dash, bracket, quote or connector https://www.compart.com/en/unicode/category/Po
            Assert.Matches(@"[\p{Pi}\p{Po}]" + "DriverID" + @"[\p{Pf}\p{Po}]", ex.Message);
            Assert.Null(ex.ParamName);

            Assert.False(_builder.ContainsKey("DriverID"));
            Assert.Equal(string.Empty, _builder.ConnectionString);

            _builder["DriverID"] = "A";
            Assert.Equal("DriverID=A", _builder.ConnectionString);
            _builder["DriverID"] = null;
            Assert.False(_builder.ContainsKey("DriverID"));
            Assert.Equal(string.Empty, _builder.ConnectionString);
        }

        [Fact]
        public void Remove()
        {
            Assert.False(_builder.Remove("Dsn"));
            Assert.False(_builder.Remove("Driver"));
            _builder.Add("DriverID", "790");
            _builder["DefaultDir"] = "C:\\";
            Assert.True(_builder.Remove("DriverID"));
            Assert.False(_builder.ContainsKey("DriverID"));
            Assert.False(_builder.Remove("DriverID"));
            Assert.False(_builder.ContainsKey("DriverID"));
            Assert.True(_builder.Remove("defaulTdIr"));
            Assert.False(_builder.ContainsKey("DefaultDir"));
            Assert.False(_builder.Remove("defaulTdIr"));
            Assert.False(_builder.Remove("userid"));
            Assert.False(_builder.Remove(string.Empty));
            Assert.False(_builder.Remove("\r"));
            Assert.False(_builder.Remove("a;"));
            _builder["Dsn"] = "myDsn";
            Assert.True(_builder.Remove("Dsn"));
            Assert.False(_builder.ContainsKey("Dsn"));
            Assert.False(_builder.Remove("Dsn"));
            _builder["Driver"] = "SQL Server";
            Assert.True(_builder.Remove("Driver"));
            Assert.False(_builder.ContainsKey("Driver"));
            Assert.False(_builder.Remove("Driver"));

            _builder = new DbConnectionStringBuilder(false);
            Assert.False(_builder.Remove("Dsn"));
            Assert.False(_builder.Remove("Driver"));
            _builder.Add("DriverID", "790");
            _builder["DefaultDir"] = "C:\\";
            Assert.True(_builder.Remove("DriverID"));
            Assert.False(_builder.ContainsKey("DriverID"));
            Assert.False(_builder.Remove("DriverID"));
            Assert.False(_builder.ContainsKey("DriverID"));
            Assert.True(_builder.Remove("defaulTdIr"));
            Assert.False(_builder.ContainsKey("DefaultDir"));
            Assert.False(_builder.Remove("defaulTdIr"));
            Assert.False(_builder.Remove("userid"));
            Assert.False(_builder.Remove(string.Empty));
            Assert.False(_builder.Remove("\r"));
            Assert.False(_builder.Remove("a;"));
            _builder["Dsn"] = "myDsn";
            Assert.True(_builder.Remove("Dsn"));
            Assert.False(_builder.ContainsKey("Dsn"));
            Assert.False(_builder.Remove("Dsn"));
            _builder["Driver"] = "SQL Server";
            Assert.True(_builder.Remove("Driver"));
            Assert.False(_builder.ContainsKey("Driver"));
            Assert.False(_builder.Remove("Driver"));

            _builder = new DbConnectionStringBuilder(true);
            Assert.False(_builder.Remove("Dsn"));
            Assert.False(_builder.Remove("Driver"));
            _builder.Add("DriverID", "790");
            _builder["DefaultDir"] = "C:\\";
            Assert.True(_builder.Remove("DriverID"));
            Assert.False(_builder.ContainsKey("DriverID"));
            Assert.False(_builder.Remove("DriverID"));
            Assert.False(_builder.ContainsKey("DriverID"));
            Assert.True(_builder.Remove("defaulTdIr"));
            Assert.False(_builder.ContainsKey("DefaultDir"));
            Assert.False(_builder.Remove("defaulTdIr"));
            Assert.False(_builder.Remove("userid"));
            Assert.False(_builder.Remove(string.Empty));
            Assert.False(_builder.Remove("\r"));
            Assert.False(_builder.Remove("a;"));
            _builder["Dsn"] = "myDsn";
            Assert.True(_builder.Remove("Dsn"));
            Assert.False(_builder.ContainsKey("Dsn"));
            Assert.False(_builder.Remove("Dsn"));
            _builder["Driver"] = "SQL Server";
            Assert.True(_builder.Remove("Driver"));
            Assert.False(_builder.ContainsKey("Driver"));
            Assert.False(_builder.Remove("Driver"));
        }

        [Fact]
        public void Remove_Keyword_Null()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => _builder.Remove(null));
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
            Assert.Equal("keyword", ex.ParamName);
        }

        [Fact]
        public void ContainsKey()
        {
            _builder["SourceType"] = "DBC";
            _builder.Add("Port", "56");
            Assert.True(_builder.ContainsKey("SourceType"));
            Assert.True(_builder.ContainsKey("Port"));
            Assert.False(_builder.ContainsKey("Dsn"));
            Assert.False(_builder.ContainsKey("Driver"));
            Assert.False(_builder.ContainsKey("xyz"));
            _builder["Dsn"] = "myDsn";
            Assert.True(_builder.ContainsKey("Dsn"));
            _builder["Driver"] = "SQL Server";
            Assert.True(_builder.ContainsKey("Driver"));
            _builder["abc"] = "pqr";
            Assert.True(_builder.ContainsKey("ABC"));
            Assert.False(_builder.ContainsKey(string.Empty));

            _builder = new DbConnectionStringBuilder(false);
            _builder["SourceType"] = "DBC";
            _builder.Add("Port", "56");
            Assert.True(_builder.ContainsKey("SourceType"));
            Assert.True(_builder.ContainsKey("Port"));
            Assert.False(_builder.ContainsKey("Dsn"));
            Assert.False(_builder.ContainsKey("Driver"));
            Assert.False(_builder.ContainsKey("xyz"));
            _builder["Dsn"] = "myDsn";
            Assert.True(_builder.ContainsKey("Dsn"));
            _builder["Driver"] = "SQL Server";
            Assert.True(_builder.ContainsKey("Driver"));
            _builder["abc"] = "pqr";
            Assert.True(_builder.ContainsKey("ABC"));
            Assert.False(_builder.ContainsKey(string.Empty));

            _builder = new DbConnectionStringBuilder(true);
            _builder["SourceType"] = "DBC";
            _builder.Add("Port", "56");
            Assert.True(_builder.ContainsKey("SourceType"));
            Assert.True(_builder.ContainsKey("Port"));
            Assert.False(_builder.ContainsKey("Dsn"));
            Assert.False(_builder.ContainsKey("Driver"));
            Assert.False(_builder.ContainsKey("xyz"));
            _builder["Dsn"] = "myDsn";
            Assert.True(_builder.ContainsKey("Dsn"));
            _builder["Driver"] = "SQL Server";
            Assert.True(_builder.ContainsKey("Driver"));
            _builder["abc"] = "pqr";
            Assert.True(_builder.ContainsKey("ABC"));
            Assert.False(_builder.ContainsKey(string.Empty));
        }

        [Fact]
        public void ContainsKey_Keyword_Null()
        {
            _builder["SourceType"] = "DBC";
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => _builder.ContainsKey(null));
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
            Assert.Equal("keyword", ex.ParamName);
        }

        [Fact]
        public void EquivalentToTest()
        {
            _builder.Add(SERVER, SERVER_VALUE);
            DbConnectionStringBuilder sb2 = new DbConnectionStringBuilder();
            sb2.Add(SERVER, SERVER_VALUE);
            bool value = _builder.EquivalentTo(sb2);
            Assert.True(value);

            // negative tests
            sb2.Add(SERVER + "1", SERVER_VALUE);
            value = _builder.EquivalentTo(sb2);
            Assert.False(value);
        }

        [Fact] // AppendKeyValuePair (StringBuilder, String, String)
        public void AppendKeyValuePair1()
        {
            StringBuilder sb = new StringBuilder();
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adventure");
            Assert.Equal("Database=Adventure", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven'ture");
            Assert.Equal("Database=\"Adven'ture\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven\"ture");
            Assert.Equal("Database='Adven\"ture'", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "\"Adventure\"");
            Assert.Equal("Database='\"Adventure\"'", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "\"Adven'ture\"");
            Assert.Equal("Database=\"\"\"Adven'ture\"\"\"", sb.ToString());
            sb.Length = 0;

            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven;ture");
            Assert.Equal("Database=\"Adven;ture\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adventure;");
            Assert.Equal("Database=\"Adventure;\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", ";Adventure");
            Assert.Equal("Database=\";Adventure\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv'en;ture");
            Assert.Equal("Database=\"Adv'en;ture\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv\"en;ture");
            Assert.Equal("Database='Adv\"en;ture'", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "A'dv\"en;ture");
            Assert.Equal("Database=\"A'dv\"\"en;ture\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "\"Adven;ture\"");
            Assert.Equal("Database='\"Adven;ture\"'", sb.ToString());
            sb.Length = 0;

            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven=ture");
            Assert.Equal("Database=\"Adven=ture\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv'en=ture");
            Assert.Equal("Database=\"Adv'en=ture\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv\"en=ture");
            Assert.Equal("Database='Adv\"en=ture'", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "A'dv\"en=ture");
            Assert.Equal("Database=\"A'dv\"\"en=ture\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "\"Adven=ture\"");
            Assert.Equal("Database='\"Adven=ture\"'", sb.ToString());
            sb.Length = 0;

            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven{ture");
            Assert.Equal("Database=Adven{ture", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven}ture");
            Assert.Equal("Database=Adven}ture", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "{Adventure");
            Assert.Equal("Database={Adventure", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "}Adventure");
            Assert.Equal("Database=}Adventure", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adventure{");
            Assert.Equal("Database=Adventure{", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adventure}");
            Assert.Equal("Database=Adventure}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv'en{ture");
            Assert.Equal("Database=\"Adv'en{ture\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv'en}ture");
            Assert.Equal("Database=\"Adv'en}ture\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv\"en{ture");
            Assert.Equal("Database='Adv\"en{ture'", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv\"en}ture");
            Assert.Equal("Database='Adv\"en}ture'", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "A'dv\"en{ture");
            Assert.Equal("Database=\"A'dv\"\"en{ture\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "A'dv\"en}ture");
            Assert.Equal("Database=\"A'dv\"\"en}ture\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "\"Adven{ture\"");
            Assert.Equal("Database='\"Adven{ture\"'", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "\"Adven}ture\"");
            Assert.Equal("Database='\"Adven}ture\"'", sb.ToString());
            sb.Length = 0;

            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adventure");
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Server", "localhost");
            Assert.Equal("Database=Adventure;Server=localhost", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", string.Empty);
            Assert.Equal("Database=", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", null);
            Assert.Equal("Database=", sb.ToString());
            sb.Length = 0;

            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Datab=ase", "Adven=ture", false);
            Assert.Equal("Datab==ase=\"Adven=ture\"", sb.ToString());
        }

        [Fact] // AppendKeyValuePair (StringBuilder, String, String)
        public void AppendKeyValuePair1_Builder_Null()
        {
            ArgumentNullException ex =
                Assert.Throws<ArgumentNullException>(() => DbConnectionStringBuilder.AppendKeyValuePair(null, "Server", "localhost"));
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
            Assert.Equal("builder", ex.ParamName);
        }

        [Fact] // AppendKeyValuePair (StringBuilder, String, String)
        public void AppendKeyValuePair1_Keyword_Empty()
        {
            StringBuilder sb = new StringBuilder();
            ArgumentException ex =
                Assert.Throws<ArgumentException>(() => DbConnectionStringBuilder.AppendKeyValuePair(sb, string.Empty, "localhost"));
            // Expecting non-empty string for 'keyName' parameter
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
            Assert.Null(ex.ParamName);
        }

        [Fact] // AppendKeyValuePair (StringBuilder, String, String)
        public void AppendKeyValuePair1_Keyword_Null()
        {
            StringBuilder sb = new StringBuilder();
            ArgumentNullException ex =
                Assert.Throws<ArgumentNullException>(() => DbConnectionStringBuilder.AppendKeyValuePair(sb, null, "localhost"));
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
            Assert.Equal("keyName", ex.ParamName);
        }

        [Fact] // AppendKeyValuePair (StringBuilder, String, String, Boolean)
        public void AppendKeyValuePair2_UseOdbcRules_False()
        {
            StringBuilder sb = new StringBuilder();
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adventure Works", false);
            Assert.Equal("Database=\"Adventure Works\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adventure", false);
            Assert.Equal("Database=Adventure", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven'ture Works", false);
            Assert.Equal("Database=\"Adven'ture Works\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven'ture", false);
            Assert.Equal("Database=\"Adven'ture\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven\"ture Works", false);
            Assert.Equal("Database='Adven\"ture Works'", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven\"ture", false);
            Assert.Equal("Database='Adven\"ture'", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "\"Adventure Works\"", false);
            Assert.Equal("Database='\"Adventure Works\"'", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "\"Adventure\"", false);
            Assert.Equal("Database='\"Adventure\"'", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "\"Adven'ture Works\"", false);
            Assert.Equal("Database=\"\"\"Adven'ture Works\"\"\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "\"Adven'ture\"", false);
            Assert.Equal("Database=\"\"\"Adven'ture\"\"\"", sb.ToString());
            sb.Length = 0;

            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven;ture Works", false);
            Assert.Equal("Database=\"Adven;ture Works\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven;ture", false);
            Assert.Equal("Database=\"Adven;ture\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adventure Works;", false);
            Assert.Equal("Database=\"Adventure Works;\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adventure;", false);
            Assert.Equal("Database=\"Adventure;\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", ";Adventure Works", false);
            Assert.Equal("Database=\";Adventure Works\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", ";Adventure", false);
            Assert.Equal("Database=\";Adventure\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv'en;ture Works", false);
            Assert.Equal("Database=\"Adv'en;ture Works\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv'en;ture", false);
            Assert.Equal("Database=\"Adv'en;ture\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv\"en;ture Works", false);
            Assert.Equal("Database='Adv\"en;ture Works'", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv\"en;ture", false);
            Assert.Equal("Database='Adv\"en;ture'", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "A'dv\"en;ture Works", false);
            Assert.Equal("Database=\"A'dv\"\"en;ture Works\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "A'dv\"en;ture", false);
            Assert.Equal("Database=\"A'dv\"\"en;ture\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "\"Adven;ture Works\"", false);
            Assert.Equal("Database='\"Adven;ture Works\"'", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "\"Adven;ture\"", false);
            Assert.Equal("Database='\"Adven;ture\"'", sb.ToString());
            sb.Length = 0;

            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven=ture Works", false);
            Assert.Equal("Database=\"Adven=ture Works\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven=ture", false);
            Assert.Equal("Database=\"Adven=ture\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv'en=ture Works", false);
            Assert.Equal("Database=\"Adv'en=ture Works\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv'en=ture", false);
            Assert.Equal("Database=\"Adv'en=ture\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv\"en=ture Works", false);
            Assert.Equal("Database='Adv\"en=ture Works'", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv\"en=ture", false);
            Assert.Equal("Database='Adv\"en=ture'", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "A'dv\"en=ture Works", false);
            Assert.Equal("Database=\"A'dv\"\"en=ture Works\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "A'dv\"en=ture", false);
            Assert.Equal("Database=\"A'dv\"\"en=ture\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "\"Adven=ture Works\"", false);
            Assert.Equal("Database='\"Adven=ture Works\"'", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "\"Adven=ture\"", false);
            Assert.Equal("Database='\"Adven=ture\"'", sb.ToString());
            sb.Length = 0;

            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven{ture Works", false);
            Assert.Equal("Database=\"Adven{ture Works\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven{ture", false);
            Assert.Equal("Database=Adven{ture", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven}ture Works", false);
            Assert.Equal("Database=\"Adven}ture Works\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven}ture", false);
            Assert.Equal("Database=Adven}ture", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "{Adventure Works", false);
            Assert.Equal("Database=\"{Adventure Works\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "{Adventure", false);
            Assert.Equal("Database={Adventure", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "}Adventure Works", false);
            Assert.Equal("Database=\"}Adventure Works\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "}Adventure", false);
            Assert.Equal("Database=}Adventure", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adventure Works{", false);
            Assert.Equal("Database=\"Adventure Works{\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adventure{", false);
            Assert.Equal("Database=Adventure{", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adventure Works}", false);
            Assert.Equal("Database=\"Adventure Works}\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adventure}", false);
            Assert.Equal("Database=Adventure}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv'en{ture Works", false);
            Assert.Equal("Database=\"Adv'en{ture Works\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv'en{ture", false);
            Assert.Equal("Database=\"Adv'en{ture\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv'en}ture Works", false);
            Assert.Equal("Database=\"Adv'en}ture Works\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv'en}ture", false);
            Assert.Equal("Database=\"Adv'en}ture\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv\"en{ture Works", false);
            Assert.Equal("Database='Adv\"en{ture Works'", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv\"en{ture", false);
            Assert.Equal("Database='Adv\"en{ture'", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv\"en}ture Works", false);
            Assert.Equal("Database='Adv\"en}ture Works'", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv\"en}ture", false);
            Assert.Equal("Database='Adv\"en}ture'", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "A'dv\"en{ture Works", false);
            Assert.Equal("Database=\"A'dv\"\"en{ture Works\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "A'dv\"en{ture", false);
            Assert.Equal("Database=\"A'dv\"\"en{ture\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "A'dv\"en}ture Works", false);
            Assert.Equal("Database=\"A'dv\"\"en}ture Works\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "A'dv\"en}ture", false);
            Assert.Equal("Database=\"A'dv\"\"en}ture\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "\"Adven{ture Works\"", false);
            Assert.Equal("Database='\"Adven{ture Works\"'", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "\"Adven{ture\"", false);
            Assert.Equal("Database='\"Adven{ture\"'", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "\"Adven}ture Works\"", false);
            Assert.Equal("Database='\"Adven}ture Works\"'", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "\"Adven}ture\"", false);
            Assert.Equal("Database='\"Adven}ture\"'", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "{{{B}}}", false);
            Assert.Equal("Database={{{B}}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "{{{B}}}", false);
            Assert.Equal("Driver={{{B}}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "{A{B{C}D}E}", false);
            Assert.Equal("Database={A{B{C}D}E}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "{A{B{C}D}E}", false);
            Assert.Equal("Driver={A{B{C}D}E}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "{{{B}}", false);
            Assert.Equal("Database={{{B}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "{{{B}}", false);
            Assert.Equal("Driver={{{B}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "{{{B}", false);
            Assert.Equal("Database={{{B}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "{{{B}", false);
            Assert.Equal("Driver={{{B}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "{{B}", false);
            Assert.Equal("Database={{B}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "{{B}", false);
            Assert.Equal("Driver={{B}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "{B}}", false);
            Assert.Equal("Database={B}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "{B}}", false);
            Assert.Equal("Driver={B}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "{B}}C", false);
            Assert.Equal("Database={B}}C", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "{B}}C", false);
            Assert.Equal("Driver={B}}C", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "A{B}}", false);
            Assert.Equal("Database=A{B}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "A{B}}", false);
            Assert.Equal("Driver=A{B}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", " {B}} ", false);
            Assert.Equal("Database=\" {B}} \"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", " {B}} ", false);
            Assert.Equal("Driver=\" {B}} \"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "{{B}}", false);
            Assert.Equal("Database={{B}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "{{B}}", false);
            Assert.Equal("Driver={{B}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "}}", false);
            Assert.Equal("Database=}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "}}", false);
            Assert.Equal("Driver=}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "}", false);
            Assert.Equal("Database=}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "}", false);
            Assert.Equal("Driver=}", sb.ToString());
            sb.Length = 0;

            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adventure Works", false);
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Server", "localhost", false);
            Assert.Equal("Database=\"Adventure Works\";Server=localhost", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adventure", false);
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Server", "localhost", false);
            Assert.Equal("Database=Adventure;Server=localhost", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", string.Empty, false);
            Assert.Equal("Database=", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", null, false);
            Assert.Equal("Database=", sb.ToString());
            sb.Length = 0;

            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Datab=ase", "Adven=ture", false);
            Assert.Equal("Datab==ase=\"Adven=ture\"", sb.ToString());
        }

        [Fact] // AppendKeyValuePair (StringBuilder, String, String, Boolean)
        public void AppendKeyValuePair2_UseOdbcRules_True()
        {
            StringBuilder sb = new StringBuilder();
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adventure Works", true);
            Assert.Equal("Database=Adventure Works", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adventure Works", true);
            Assert.Equal("Driver={Adventure Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adventure", true);
            Assert.Equal("Database=Adventure", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adventure", true);
            Assert.Equal("Driver={Adventure}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven'ture Works", true);
            Assert.Equal("Database=Adven'ture Works", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adven'ture Works", true);
            Assert.Equal("Driver={Adven'ture Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven'ture", true);
            Assert.Equal("Database=Adven'ture", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adven'ture", true);
            Assert.Equal("Driver={Adven'ture}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven\"ture Works", true);
            Assert.Equal("Database=Adven\"ture Works", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adven\"ture Works", true);
            Assert.Equal("Driver={Adven\"ture Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven\"ture", true);
            Assert.Equal("Database=Adven\"ture", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adven\"ture", true);
            Assert.Equal("Driver={Adven\"ture}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "\"Adventure Works\"", true);
            Assert.Equal("Database=\"Adventure Works\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "\"Adventure Works\"", true);
            Assert.Equal("Driver={\"Adventure Works\"}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "\"Adventure\"", true);
            Assert.Equal("Database=\"Adventure\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "\"Adventure\"", true);
            Assert.Equal("Driver={\"Adventure\"}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "\"Adven'ture Works\"", true);
            Assert.Equal("Database=\"Adven'ture Works\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "\"Adven'ture Works\"", true);
            Assert.Equal("Driver={\"Adven'ture Works\"}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "\"Adven'ture\"", true);
            Assert.Equal("Database=\"Adven'ture\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "\"Adven'ture\"", true);
            Assert.Equal("Driver={\"Adven'ture\"}", sb.ToString());
            sb.Length = 0;

            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven;ture Works", true);
            Assert.Equal("Database={Adven;ture Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adven;ture Works", true);
            Assert.Equal("Driver={Adven;ture Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven;ture", true);
            Assert.Equal("Database={Adven;ture}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adven;ture", true);
            Assert.Equal("Driver={Adven;ture}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adventure Works;", true);
            Assert.Equal("Database={Adventure Works;}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adventure Works;", true);
            Assert.Equal("Driver={Adventure Works;}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adventure;", true);
            Assert.Equal("Database={Adventure;}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adventure;", true);
            Assert.Equal("Driver={Adventure;}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", ";Adventure Works", true);
            Assert.Equal("Database={;Adventure Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", ";Adventure Works", true);
            Assert.Equal("Driver={;Adventure Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", ";Adventure", true);
            Assert.Equal("Database={;Adventure}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", ";Adventure", true);
            Assert.Equal("Driver={;Adventure}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv'en;ture Works", true);
            Assert.Equal("Database={Adv'en;ture Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adv'en;ture Works", true);
            Assert.Equal("Driver={Adv'en;ture Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv'en;ture", true);
            Assert.Equal("Database={Adv'en;ture}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adv'en;ture", true);
            Assert.Equal("Driver={Adv'en;ture}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv\"en;ture Works", true);
            Assert.Equal("Database={Adv\"en;ture Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adv\"en;ture Works", true);
            Assert.Equal("Driver={Adv\"en;ture Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv\"en;ture", true);
            Assert.Equal("Database={Adv\"en;ture}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adv\"en;ture", true);
            Assert.Equal("Driver={Adv\"en;ture}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "A'dv\"en;ture Works", true);
            Assert.Equal("Database={A'dv\"en;ture Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "A'dv\"en;ture Works", true);
            Assert.Equal("Driver={A'dv\"en;ture Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "A'dv\"en;ture", true);
            Assert.Equal("Database={A'dv\"en;ture}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "A'dv\"en;ture", true);
            Assert.Equal("Driver={A'dv\"en;ture}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "\"Adven;ture Works\"", true);
            Assert.Equal("Database={\"Adven;ture Works\"}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "\"Adven;ture Works\"", true);
            Assert.Equal("Driver={\"Adven;ture Works\"}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "\"Adven;ture\"", true);
            Assert.Equal("Database={\"Adven;ture\"}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "\"Adven;ture\"", true);
            Assert.Equal("Driver={\"Adven;ture\"}", sb.ToString());
            sb.Length = 0;

            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven=ture Works", true);
            Assert.Equal("Database=Adven=ture Works", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adven=ture Works", true);
            Assert.Equal("Driver={Adven=ture Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven=ture", true);
            Assert.Equal("Database=Adven=ture", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adven=ture", true);
            Assert.Equal("Driver={Adven=ture}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv'en=ture Works", true);
            Assert.Equal("Database=Adv'en=ture Works", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adv'en=ture Works", true);
            Assert.Equal("Driver={Adv'en=ture Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv'en=ture", true);
            Assert.Equal("Database=Adv'en=ture", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adv'en=ture", true);
            Assert.Equal("Driver={Adv'en=ture}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv\"en=ture Works", true);
            Assert.Equal("Database=Adv\"en=ture Works", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adv\"en=ture Works", true);
            Assert.Equal("Driver={Adv\"en=ture Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv\"en=ture", true);
            Assert.Equal("Database=Adv\"en=ture", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adv\"en=ture", true);
            Assert.Equal("Driver={Adv\"en=ture}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "A'dv\"en=ture Works", true);
            Assert.Equal("Database=A'dv\"en=ture Works", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "A'dv\"en=ture Works", true);
            Assert.Equal("Driver={A'dv\"en=ture Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "A'dv\"en=ture", true);
            Assert.Equal("Database=A'dv\"en=ture", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "A'dv\"en=ture", true);
            Assert.Equal("Driver={A'dv\"en=ture}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "\"Adven=ture Works\"", true);
            Assert.Equal("Database=\"Adven=ture Works\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "\"Adven=ture Works\"", true);
            Assert.Equal("Driver={\"Adven=ture Works\"}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "\"Adven=ture\"", true);
            Assert.Equal("Database=\"Adven=ture\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "\"Adven=ture\"", true);
            Assert.Equal("Driver={\"Adven=ture\"}", sb.ToString());
            sb.Length = 0;

            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven{ture Works", true);
            Assert.Equal("Database=Adven{ture Works", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adven{ture Works", true);
            Assert.Equal("Driver={Adven{ture Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven{tu}re Works", true);
            Assert.Equal("Database=Adven{tu}re Works", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adven{tu}re Works", true);
            Assert.Equal("Driver={Adven{tu}}re Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven{ture", true);
            Assert.Equal("Database=Adven{ture", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adven{ture", true);
            Assert.Equal("Driver={Adven{ture}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven{tu}re", true);
            Assert.Equal("Database=Adven{tu}re", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adven{tu}re", true);
            Assert.Equal("Driver={Adven{tu}}re}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven}ture Works", true);
            Assert.Equal("Database=Adven}ture Works", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adven}ture Works", true);
            Assert.Equal("Driver={Adven}}ture Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adven}ture", true);
            Assert.Equal("Database=Adven}ture", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adven}ture", true);
            Assert.Equal("Driver={Adven}}ture}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "{Adventure Works", true);
            Assert.Equal("Database={{Adventure Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "{Adventure Works", true);
            Assert.Equal("Driver={{Adventure Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "{Adventure", true);
            Assert.Equal("Database={{Adventure}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "{Adventure", true);
            Assert.Equal("Driver={{Adventure}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "{Adventure Works}", true);
            Assert.Equal("Database={Adventure Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "{Adventure Works}", true);
            Assert.Equal("Driver={Adventure Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "{Adventu{re Works}", true);
            Assert.Equal("Database={Adventu{re Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "{Adventu{re Works}", true);
            Assert.Equal("Driver={Adventu{re Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "{Adventu}re Works}", true);
            Assert.Equal("Database={{Adventu}}re Works}}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "{Adventu}re Works}", true);
            Assert.Equal("Driver={{Adventu}}re Works}}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "{Adventure}", true);
            Assert.Equal("Database={Adventure}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "{Adventure}", true);
            Assert.Equal("Driver={Adventure}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "{Adventu{re}", true);
            Assert.Equal("Database={Adventu{re}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "{Adventu{re}", true);
            Assert.Equal("Driver={Adventu{re}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "{Adventu}re}", true);
            Assert.Equal("Database={{Adventu}}re}}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "{Adventu}re}", true);
            Assert.Equal("Driver={{Adventu}}re}}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "{Adventure }Works", true);
            Assert.Equal("Database={{Adventure }}Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "{Adventure }Works", true);
            Assert.Equal("Driver={{Adventure }}Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "{Adven}ture", true);
            Assert.Equal("Database={{Adven}}ture}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "{Adven}ture", true);
            Assert.Equal("Driver={{Adven}}ture}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "}Adventure Works", true);
            Assert.Equal("Database=}Adventure Works", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "}Adventure Works", true);
            Assert.Equal("Driver={}}Adventure Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "}Adventure", true);
            Assert.Equal("Database=}Adventure", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "}Adventure", true);
            Assert.Equal("Driver={}}Adventure}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adventure Works{", true);
            Assert.Equal("Database=Adventure Works{", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adventure Works{", true);
            Assert.Equal("Driver={Adventure Works{}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adventure{", true);
            Assert.Equal("Database=Adventure{", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adventure{", true);
            Assert.Equal("Driver={Adventure{}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adventure Works}", true);
            Assert.Equal("Database=Adventure Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adventure Works}", true);
            Assert.Equal("Driver={Adventure Works}}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adventure}", true);
            Assert.Equal("Database=Adventure}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adventure}", true);
            Assert.Equal("Driver={Adventure}}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv'en{ture Works", true);
            Assert.Equal("Database=Adv'en{ture Works", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adv'en{ture Works", true);
            Assert.Equal("Driver={Adv'en{ture Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv'en{ture", true);
            Assert.Equal("Database=Adv'en{ture", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adv'en{ture", true);
            Assert.Equal("Driver={Adv'en{ture}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv'en}ture Works", true);
            Assert.Equal("Database=Adv'en}ture Works", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adv'en}ture Works", true);
            Assert.Equal("Driver={Adv'en}}ture Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv'en}ture", true);
            Assert.Equal("Database=Adv'en}ture", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adv'en}ture", true);
            Assert.Equal("Driver={Adv'en}}ture}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv\"en{ture Works", true);
            Assert.Equal("Database=Adv\"en{ture Works", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adv\"en{ture Works", true);
            Assert.Equal("Driver={Adv\"en{ture Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv\"en{ture", true);
            Assert.Equal("Database=Adv\"en{ture", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adv\"en{ture", true);
            Assert.Equal("Driver={Adv\"en{ture}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv\"en}ture Works", true);
            Assert.Equal("Database=Adv\"en}ture Works", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adv\"en}ture Works", true);
            Assert.Equal("Driver={Adv\"en}}ture Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adv\"en}ture", true);
            Assert.Equal("Database=Adv\"en}ture", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adv\"en}ture", true);
            Assert.Equal("Driver={Adv\"en}}ture}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "A'dv\"en{ture Works", true);
            Assert.Equal("Database=A'dv\"en{ture Works", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "A'dv\"en{ture Works", true);
            Assert.Equal("Driver={A'dv\"en{ture Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "A'dv\"en{ture", true);
            Assert.Equal("Database=A'dv\"en{ture", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "A'dv\"en{ture", true);
            Assert.Equal("Driver={A'dv\"en{ture}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "A'dv\"en}ture Works", true);
            Assert.Equal("Database=A'dv\"en}ture Works", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "A'dv\"en}ture Works", true);
            Assert.Equal("Driver={A'dv\"en}}ture Works}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "A'dv\"en}ture", true);
            Assert.Equal("Database=A'dv\"en}ture", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "A'dv\"en}ture", true);
            Assert.Equal("Driver={A'dv\"en}}ture}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "\"Adven{ture Works\"", true);
            Assert.Equal("Database=\"Adven{ture Works\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "\"Adven{ture Works\"", true);
            Assert.Equal("Driver={\"Adven{ture Works\"}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "\"Adven{ture\"", true);
            Assert.Equal("Database=\"Adven{ture\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "\"Adven{ture\"", true);
            Assert.Equal("Driver={\"Adven{ture\"}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "\"Adven}ture Works\"", true);
            Assert.Equal("Database=\"Adven}ture Works\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "\"Adven}ture Works\"", true);
            Assert.Equal("Driver={\"Adven}}ture Works\"}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "\"Adven}ture\"", true);
            Assert.Equal("Database=\"Adven}ture\"", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "\"Adven}ture\"", true);
            Assert.Equal("Driver={\"Adven}}ture\"}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "{{{B}}}", true);
            Assert.Equal("Database={{{B}}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "{{{B}}}", true);
            Assert.Equal("Driver={{{B}}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "{A{B{C}D}E}", true);
            Assert.Equal("Database={{A{B{C}}D}}E}}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "{A{B{C}D}E}", true);
            Assert.Equal("Driver={{A{B{C}}D}}E}}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "{{{B}}", true);
            Assert.Equal("Database={{{{B}}}}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "{{{B}}", true);
            Assert.Equal("Driver={{{{B}}}}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "{{{B}", true);
            Assert.Equal("Database={{{B}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "{{{B}", true);
            Assert.Equal("Driver={{{B}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "{{B}", true);
            Assert.Equal("Database={{B}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "{{B}", true);
            Assert.Equal("Driver={{B}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "{B}}", true);
            Assert.Equal("Database={{B}}}}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "{B}}", true);
            Assert.Equal("Driver={{B}}}}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "{B}}C", true);
            Assert.Equal("Database={{B}}}}C}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "{B}}C", true);
            Assert.Equal("Driver={{B}}}}C}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "A{B}}", true);
            Assert.Equal("Database=A{B}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "A{B}}", true);
            Assert.Equal("Driver={A{B}}}}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", " {B}} ", true);
            Assert.Equal("Database= {B}} ", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", " {B}} ", true);
            Assert.Equal("Driver={ {B}}}} }", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "{{B}}", true);
            Assert.Equal("Database={{{B}}}}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "{{B}}", true);
            Assert.Equal("Driver={{{B}}}}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "}}", true);
            Assert.Equal("Database=}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "}}", true);
            Assert.Equal("Driver={}}}}}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "}", true);
            Assert.Equal("Database=}", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "}", true);
            Assert.Equal("Driver={}}}", sb.ToString());
            sb.Length = 0;

            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adventure Works", true);
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Server", "localhost", true);
            Assert.Equal("Database=Adventure Works;Server=localhost", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adventure Works", true);
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Server", "localhost", true);
            Assert.Equal("Driver={Adventure Works};Server=localhost", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", "Adventure", true);
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Server", "localhost", true);
            Assert.Equal("Database=Adventure;Server=localhost", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", "Adventure", true);
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Server", "localhost", true);
            Assert.Equal("Driver={Adventure};Server=localhost", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", string.Empty, true);
            Assert.Equal("Database=", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", string.Empty, true);
            Assert.Equal("Driver=", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Database", null, true);
            Assert.Equal("Database=", sb.ToString());
            sb.Length = 0;
            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Driver", null, true);
            Assert.Equal("Driver=", sb.ToString());
            sb.Length = 0;

            DbConnectionStringBuilder.AppendKeyValuePair(sb, "Datab=ase", "Adven=ture", true);
            Assert.Equal("Datab=ase=Adven=ture", sb.ToString());
        }

        [Fact] // AppendKeyValuePair (StringBuilder, String, String, Boolean)
        public void AppendKeyValuePair2_Builder_Null()
        {
            ArgumentNullException ex1 =
                Assert.Throws<ArgumentNullException>(() => DbConnectionStringBuilder.AppendKeyValuePair(null, "Server", "localhost", true));
            Assert.Null(ex1.InnerException);
            Assert.NotNull(ex1.Message);
            Assert.Equal("builder", ex1.ParamName);

            ArgumentNullException ex2 =
                Assert.Throws<ArgumentNullException>(() => DbConnectionStringBuilder.AppendKeyValuePair(null, "Server", "localhost", false));
            Assert.Null(ex2.InnerException);
            Assert.NotNull(ex2.Message);
            Assert.Equal("builder", ex2.ParamName);
        }

        [Fact] // AppendKeyValuePair (StringBuilder, String, String, Boolean)
        public void AppendKeyValuePair2_Keyword_Empty()
        {
            StringBuilder sb = new StringBuilder();

            ArgumentException ex1 =
                Assert.Throws<ArgumentException>(() => DbConnectionStringBuilder.AppendKeyValuePair(sb, string.Empty, "localhost", true));
            // Expecting non-empty string for 'keyName' parameter
            Assert.Null(ex1.InnerException);
            Assert.NotNull(ex1.Message);
            Assert.Null(ex1.ParamName);

            ArgumentException ex2 =
                Assert.Throws<ArgumentException>(() => DbConnectionStringBuilder.AppendKeyValuePair(sb, string.Empty, "localhost", false));
            // Expecting non-empty string for 'keyName' parameter
            Assert.Null(ex2.InnerException);
            Assert.NotNull(ex2.Message);
            Assert.Null(ex2.ParamName);
        }

        [Fact] // AppendKeyValuePair (StringBuilder, String, String, Boolean)
        public void AppendKeyValuePair2_Keyword_Null()
        {
            StringBuilder sb = new StringBuilder();
            ArgumentNullException ex1 =
                Assert.Throws<ArgumentNullException>(() => DbConnectionStringBuilder.AppendKeyValuePair(sb, null, "localhost", true));
            Assert.Null(ex1.InnerException);
            Assert.NotNull(ex1.Message);
            Assert.Equal("keyName", ex1.ParamName);

            ArgumentNullException ex2 =
                Assert.Throws<ArgumentNullException>(() => DbConnectionStringBuilder.AppendKeyValuePair(sb, null, "localhost", false));
            Assert.Null(ex2.InnerException);
            Assert.NotNull(ex2.Message);
            Assert.Equal("keyName", ex2.ParamName);
        }

        [Fact]
        public void ToStringTest()
        {
            _builder.Add(SERVER, SERVER_VALUE);
            string str = _builder.ToString();
            string value = _builder.ConnectionString;
            Assert.Equal(value, str);
        }

        [Fact]
        public void ItemTest()
        {
            _builder.Add(SERVER, SERVER_VALUE);
            string value = (string)_builder[SERVER];
            Assert.Equal(SERVER_VALUE, value);
        }

        [Fact]
        public void ICollectionCopyToTest()
        {
            KeyValuePair<string, object>[] dict = new KeyValuePair<string, object>[2];
            _builder.Add(SERVER, SERVER_VALUE);
            _builder.Add(SERVER + "1", SERVER_VALUE + "1");

            int i = 0;
            int j = 1;
            ((ICollection)_builder).CopyTo(dict, 0);
            Assert.Equal(SERVER, dict[i].Key);
            Assert.Equal(SERVER_VALUE, dict[i].Value);
            Assert.Equal(SERVER + "1", dict[j].Key);
            Assert.Equal(SERVER_VALUE + "1", dict[j].Value);
        }

        [Fact]
        public void NegICollectionCopyToTest()
        {
            AssertExtensions.Throws<ArgumentException>(null, () =>
            {
                KeyValuePair<string, object>[] dict = new KeyValuePair<string, object>[1];
                _builder.Add(SERVER, SERVER_VALUE);
                _builder.Add(SERVER + "1", SERVER_VALUE + "1");
                ((ICollection)_builder).CopyTo(dict, 0);
                Assert.False(true);
            });
        }

        [Fact]
        public void TryGetValueTest()
        {
            object value = null;

            _builder["DriverID"] = "790";
            _builder.Add("Server", "C:\\");
            Assert.True(_builder.TryGetValue("DriverID", out value));
            Assert.Equal("790", value);
            Assert.True(_builder.TryGetValue("SERVER", out value));
            Assert.Equal("C:\\", value);
            Assert.False(_builder.TryGetValue(string.Empty, out value));
            Assert.Null(value);
            Assert.False(_builder.TryGetValue("a;", out value));
            Assert.Null(value);
            Assert.False(_builder.TryGetValue("\r", out value));
            Assert.Null(value);
            Assert.False(_builder.TryGetValue(" ", out value));
            Assert.Null(value);
            Assert.False(_builder.TryGetValue("doesnotexist", out value));
            Assert.Null(value);
            Assert.False(_builder.TryGetValue("Driver", out value));
            Assert.Null(value);
            Assert.False(_builder.TryGetValue("Dsn", out value));
            Assert.Null(value);

            _builder = new DbConnectionStringBuilder(false);
            _builder["DriverID"] = "790";
            _builder.Add("Server", "C:\\");
            Assert.True(_builder.TryGetValue("DriverID", out value));
            Assert.Equal("790", value);
            Assert.True(_builder.TryGetValue("SERVER", out value));
            Assert.Equal("C:\\", value);
            Assert.False(_builder.TryGetValue(string.Empty, out value));
            Assert.Null(value);
            Assert.False(_builder.TryGetValue("a;", out value));
            Assert.Null(value);
            Assert.False(_builder.TryGetValue("\r", out value));
            Assert.Null(value);
            Assert.False(_builder.TryGetValue(" ", out value));
            Assert.Null(value);
            Assert.False(_builder.TryGetValue("doesnotexist", out value));
            Assert.Null(value);
            Assert.False(_builder.TryGetValue("Driver", out value));
            Assert.Null(value);
            Assert.False(_builder.TryGetValue("Dsn", out value));
            Assert.Null(value);

            _builder = new DbConnectionStringBuilder(true);
            _builder["DriverID"] = "790";
            _builder.Add("Server", "C:\\");
            Assert.True(_builder.TryGetValue("DriverID", out value));
            Assert.Equal("790", value);
            Assert.True(_builder.TryGetValue("SERVER", out value));
            Assert.Equal("C:\\", value);
            Assert.False(_builder.TryGetValue(string.Empty, out value));
            Assert.Null(value);
            Assert.False(_builder.TryGetValue("a;", out value));
            Assert.Null(value);
            Assert.False(_builder.TryGetValue("\r", out value));
            Assert.Null(value);
            Assert.False(_builder.TryGetValue(" ", out value));
            Assert.Null(value);
            Assert.False(_builder.TryGetValue("doesnotexist", out value));
            Assert.Null(value);
            Assert.False(_builder.TryGetValue("Driver", out value));
            Assert.Null(value);
            Assert.False(_builder.TryGetValue("Dsn", out value));
            Assert.Null(value);
        }

        [Fact]
        public void TryGetValue_Keyword_Null()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => _builder.TryGetValue(null, out object value));
            Assert.Null(ex.InnerException);
            Assert.NotNull(ex.Message);
            Assert.Equal("keyword", ex.ParamName);

        }

        [Fact]
        public void EmbeddedCharTest1()
        {
            // Notice how the keywords show up in the connection string
            //  in the order they were added.
            // And notice the case of the keyword when added is preserved
            //  in the connection string.

            DbConnectionStringBuilder sb = new DbConnectionStringBuilder();

            sb["Data Source"] = "testdb";
            sb["User ID"] = "someuser";
            sb["Password"] = "PLACEHOLDER";
            Assert.Equal("Data Source=testdb;User ID=someuser;Password=PLACEHOLDER",
                sb.ConnectionString);

            sb["Password"] = "PLACEHOLDER#";
            Assert.Equal("Data Source=testdb;User ID=someuser;Password=PLACEHOLDER#",
                sb.ConnectionString);

            // an embedded single-quote value will result in the value being delimieted with double quotes
            sb["Password"] = "PLACEHOLDER\'def";
            Assert.Equal("Data Source=testdb;User ID=someuser;Password=\"PLACEHOLDER\'def\"",
                sb.ConnectionString);

            // an embedded double-quote value will result in the value being delimieted with single quotes
            sb["Password"] = "abc\"def";
            Assert.Equal("Data Source=testdb;User ID=someuser;Password=\'abc\"def\'",
                sb.ConnectionString);

            // an embedded single-quote and double-quote in the value
            // will result in the value being delimited by double-quotes
            // with the embedded double quote being escaped with two double-quotes
            sb["Password"] = "abc\"d\'ef";
            Assert.Equal("Data Source=testdb;User ID=someuser;Password=\"abc\"\"d\'ef\"",
                sb.ConnectionString);

            sb = new DbConnectionStringBuilder();
            sb["PASSWORD"] = "PLACEHOLDERabcdef1";
            sb["user id"] = "someuser";
            sb["Data Source"] = "testdb";
            Assert.Equal("PASSWORD=PLACEHOLDERabcdef1;user id=someuser;Data Source=testdb",
                sb.ConnectionString);

            // case is preserved for a keyword that was added the first time
            sb = new DbConnectionStringBuilder();
            sb["PassWord"] = "PLACEHOLDERabcdef2";
            sb["uSER iD"] = "someuser";
            sb["DaTa SoUrCe"] = "testdb";
            Assert.Equal("PassWord=PLACEHOLDERabcdef2;uSER iD=someuser;DaTa SoUrCe=testdb",
                sb.ConnectionString);
            sb["passWORD"] = "PLACEHOLDERabc123";
            Assert.Equal("PassWord=PLACEHOLDERabc123;uSER iD=someuser;DaTa SoUrCe=testdb",
                sb.ConnectionString);

            // embedded equal sign in the value will cause the value to be
            // delimited with double-quotes
            sb = new DbConnectionStringBuilder();
            sb["Password"] = "PLACEHOLDER=def";
            sb["Data Source"] = "testdb";
            sb["User ID"] = "someuser";
            Assert.Equal("Password=\"PLACEHOLDER=def\";Data Source=testdb;User ID=someuser",
                sb.ConnectionString);

            // embedded semicolon in the value will cause the value to be
            // delimited with double-quotes
            sb = new DbConnectionStringBuilder();
            sb["Password"] = "PLACEHOLDER;def";
            sb["Data Source"] = "testdb";
            sb["User ID"] = "someuser";
            Assert.Equal("Password=\"PLACEHOLDER;def\";Data Source=testdb;User ID=someuser",
                sb.ConnectionString);

            // more right parentheses then left parentheses - happily takes it
            sb = new DbConnectionStringBuilder();
            sb.ConnectionString = "Data Source=(((Blah=Something))))))";
            Assert.Equal("data source=\"(((Blah=Something))))))\"",
                sb.ConnectionString);

            // more left curly braces then right curly braces - happily takes it
            sb = new DbConnectionStringBuilder();
            sb.ConnectionString = "Data Source={{{{Blah=Something}}";
            Assert.Equal("data source=\"{{{{Blah=Something}}\"",
                sb.ConnectionString);

            // spaces, empty string, null are treated like an empty string
            // and any previous settings is cleared
            sb.ConnectionString = "   ";
            Assert.Equal(string.Empty,
                sb.ConnectionString);

            sb.ConnectionString = " ";
            Assert.Equal(string.Empty,
                sb.ConnectionString);

            sb.ConnectionString = "";
            Assert.Equal(string.Empty,
                sb.ConnectionString);

            sb.ConnectionString = string.Empty;
            Assert.Equal(string.Empty,
                sb.ConnectionString);

            sb.ConnectionString = null;
            Assert.Equal(string.Empty,
                sb.ConnectionString);

            sb = new DbConnectionStringBuilder();
            Assert.Equal(string.Empty,
                sb.ConnectionString);
        }

        [Fact]
        public void EmbeddedCharTest2()
        {
            DbConnectionStringBuilder sb;

            sb = new DbConnectionStringBuilder();
            sb.ConnectionString = "Driver={SQL Server};Server=(local host);" +
                "Trusted_Connection=Yes Or No;Database=Adventure Works;";
            Assert.Equal("{SQL Server}", sb["Driver"]);
            Assert.Equal("(local host)", sb["Server"]);
            Assert.Equal("Yes Or No", sb["Trusted_Connection"]);
            Assert.Equal("driver=\"{SQL Server}\";server=\"(local host)\";" +
                "trusted_connection=\"Yes Or No\";database=\"Adventure Works\"",
                sb.ConnectionString);

            sb = new DbConnectionStringBuilder();
            sb.ConnectionString = "Driver={SQLServer};Server=(local);" +
                "Trusted_Connection=Yes;Database=AdventureWorks;";
            Assert.Equal("{SQLServer}", sb["Driver"]);
            Assert.Equal("(local)", sb["Server"]);
            Assert.Equal("Yes", sb["Trusted_Connection"]);
            Assert.Equal("driver={SQLServer};server=(local);" +
                "trusted_connection=Yes;database=AdventureWorks",
                sb.ConnectionString);

            sb = new DbConnectionStringBuilder(false);
            sb.ConnectionString = "Driver={SQL Server};Server=(local host);" +
                "Trusted_Connection=Yes Or No;Database=Adventure Works;";
            Assert.Equal("{SQL Server}", sb["Driver"]);
            Assert.Equal("(local host)", sb["Server"]);
            Assert.Equal("Yes Or No", sb["Trusted_Connection"]);
            Assert.Equal("driver=\"{SQL Server}\";server=\"(local host)\";" +
                "trusted_connection=\"Yes Or No\";database=\"Adventure Works\"",
                sb.ConnectionString);


            sb = new DbConnectionStringBuilder(false);
            sb.ConnectionString = "Driver={SQLServer};Server=(local);" +
                "Trusted_Connection=Yes;Database=AdventureWorks;";
            Assert.Equal("{SQLServer}", sb["Driver"]);
            Assert.Equal("(local)", sb["Server"]);
            Assert.Equal("Yes", sb["Trusted_Connection"]);
            Assert.Equal("driver={SQLServer};server=(local);" +
                "trusted_connection=Yes;database=AdventureWorks",
                sb.ConnectionString);

            sb = new DbConnectionStringBuilder(true);
            sb.ConnectionString = "Driver={SQL Server};Server=(local host);" +
                "Trusted_Connection=Yes Or No;Database=Adventure Works;";
            Assert.Equal("{SQL Server}", sb["Driver"]);
            Assert.Equal("(local host)", sb["Server"]);
            Assert.Equal("Yes Or No", sb["Trusted_Connection"]);
            Assert.Equal("driver={SQL Server};server=(local host);" +
                "trusted_connection=Yes Or No;database=Adventure Works",
                sb.ConnectionString);

            sb = new DbConnectionStringBuilder(true);
            sb.ConnectionString = "Driver={SQLServer};Server=(local);" +
                "Trusted_Connection=Yes;Database=AdventureWorks;";
            Assert.Equal("{SQLServer}", sb["Driver"]);
            Assert.Equal("(local)", sb["Server"]);
            Assert.Equal("Yes", sb["Trusted_Connection"]);
            Assert.Equal("driver={SQLServer};server=(local);" +
                "trusted_connection=Yes;database=AdventureWorks",
                sb.ConnectionString);
        }

        [Fact]
        public void EmbeddedCharTest3()
        {
            string dataSource = "(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=192.168.1.101)" +
                "(PORT=1521))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=TESTDB)))";
            DbConnectionStringBuilder sb;

            sb = new DbConnectionStringBuilder();
            sb.ConnectionString = "User ID=SCOTT;Password=PLACEHOLDER;Data Source=" + dataSource;
            Assert.Equal(dataSource, sb["Data Source"]);
            Assert.Equal("SCOTT", sb["User ID"]);
            Assert.Equal("PLACEHOLDER", sb["Password"]);
            Assert.Equal(
                "user id=SCOTT;password=PLACEHOLDER;data source=\"(DESCRIPTION=(ADDRESS=(PROTOCOL=" +
                "TCP)(HOST=192.168.1.101)(PORT=1521))(CONNECT_DATA=(SERVER=DEDICATED)" +
                "(SERVICE_NAME=TESTDB)))\"", sb.ConnectionString);

            sb = new DbConnectionStringBuilder(false);
            sb.ConnectionString = "User ID=SCOTT;Password=PLACEHOLDER;Data Source=" + dataSource;
            Assert.Equal(dataSource, sb["Data Source"]);
            Assert.Equal("SCOTT", sb["User ID"]);
            Assert.Equal("PLACEHOLDER", sb["Password"]);
            Assert.Equal(
                "user id=SCOTT;password=PLACEHOLDER;data source=\"(DESCRIPTION=(ADDRESS=(PROTOCOL=" +
                "TCP)(HOST=192.168.1.101)(PORT=1521))(CONNECT_DATA=(SERVER=DEDICATED)" +
                "(SERVICE_NAME=TESTDB)))\"", sb.ConnectionString);

            sb = new DbConnectionStringBuilder(true);
            sb.ConnectionString = "User ID=SCOTT;Password=PLACEHOLDER;Data Source=" + dataSource;
            Assert.Equal(dataSource, sb["Data Source"]);
            Assert.Equal("SCOTT", sb["User ID"]);
            Assert.Equal("PLACEHOLDER", sb["Password"]);
            Assert.Equal(
                "user id=SCOTT;password=PLACEHOLDER;data source=(DESCRIPTION=(ADDRESS=(PROTOCOL=" +
                "TCP)(HOST=192.168.1.101)(PORT=1521))(CONNECT_DATA=(SERVER=DEDICATED)" +
                "(SERVICE_NAME=TESTDB)))", sb.ConnectionString);
        }

        [Fact]
        public void EmbeddedCharTest4()
        {
            DbConnectionStringBuilder sb;

            sb = new DbConnectionStringBuilder();
            sb.ConnectionString = "PassWord=PLACEHOLDER;user iD=someuser;DaTa SoUrCe=testdb";
            sb["Integrated Security"] = "False";
            Assert.Equal(
                "password=PLACEHOLDER;user id=someuser;data source=testdb;Integrated Security=False",
                sb.ConnectionString);

            sb = new DbConnectionStringBuilder(false);
            sb.ConnectionString = "PassWord=PLACEHOLDER;uSER iD=someuser;DaTa SoUrCe=testdb";
            sb["Integrated Security"] = "False";
            Assert.Equal(
                "password=PLACEHOLDER;user id=someuser;data source=testdb;Integrated Security=False",
                sb.ConnectionString);

            sb = new DbConnectionStringBuilder(true);
            sb.ConnectionString = "PassWord=PLACEHOLDER;uSER iD=someuser;DaTa SoUrCe=testdb";
            sb["Integrated Security"] = "False";
            Assert.Equal(
                "password=PLACEHOLDER;user id=someuser;data source=testdb;Integrated Security=False",
                sb.ConnectionString);
        }

        [Fact]
        public void EmbeddedCharTest5()
        {
            string connectionString = "A={abcdef2};B=some{us;C=test}db;D=12\"3;E=\"45;6\";F=AB==C;G{A'}\";F={1='\"2};G==C=B==C;Z=ABC";
            DbConnectionStringBuilder sb;

            sb = new DbConnectionStringBuilder();
            sb.ConnectionString = connectionString;
            Assert.Equal("a={abcdef2};b=some{us;c=test}db;d='12\"3';e=\"45;6\";f=\"AB==C\";g{a'}\";f=\"{1='\"\"2}\";g==c=\"B==C\";z=ABC", sb.ConnectionString);
            Assert.Equal("{abcdef2}", sb["A"]);
            Assert.Equal("some{us", sb["B"]);
            Assert.Equal("test}db", sb["C"]);
            Assert.Equal("12\"3", sb["D"]);
            Assert.Equal("45;6", sb["E"]);
            Assert.Equal("AB==C", sb["F"]);
            Assert.Equal("{1='\"2}", sb["g{a'}\";f"]);
            Assert.Equal("ABC", sb["Z"]);
            Assert.Equal("B==C", sb["g=c"]);

            sb = new DbConnectionStringBuilder(false);
            sb.ConnectionString = connectionString;
            Assert.Equal("a={abcdef2};b=some{us;c=test}db;d='12\"3';e=\"45;6\";f=\"AB==C\";g{a'}\";f=\"{1='\"\"2}\";g==c=\"B==C\";z=ABC", sb.ConnectionString);
            Assert.Equal("{abcdef2}", sb["A"]);
            Assert.Equal("some{us", sb["B"]);
            Assert.Equal("test}db", sb["C"]);
            Assert.Equal("12\"3", sb["D"]);
            Assert.Equal("45;6", sb["E"]);
            Assert.Equal("AB==C", sb["F"]);
            Assert.Equal("{1='\"2}", sb["g{a'}\";f"]);
            Assert.Equal("ABC", sb["Z"]);
            Assert.Equal("B==C", sb["g=c"]);

            sb = new DbConnectionStringBuilder(true);
            sb.ConnectionString = connectionString;
            Assert.Equal("a={abcdef2};b=some{us;c=test}db;d=12\"3;e=\"45;6\";f=AB==C;g{a'}\";f={1='\"2};g==C=B==C;z=ABC", sb.ConnectionString);
            Assert.Equal("{abcdef2}", sb["A"]);
            Assert.Equal("some{us", sb["B"]);
            Assert.Equal("test}db", sb["C"]);
            Assert.Equal("12\"3", sb["D"]);
            Assert.Equal("\"45", sb["E"]);
            Assert.Equal("AB==C", sb["6\";f"]);
            Assert.Equal("{1='\"2}", sb["g{a'}\";f"]);
            Assert.Equal("ABC", sb["Z"]);
            Assert.Equal("=C=B==C", sb["g"]);
        }

        [Fact]
        public void EmbeddedCharTest6()
        {
            string[][] shared_tests = new string[][] {
                new string [] { "A=(B;", "A", "(B", "a=(B" },
                new string [] { "A={B{}", "A", "{B{}", "a={B{}" },
                new string [] { "A={B{{}", "A", "{B{{}", "a={B{{}" },
                new string [] { " A =B{C", "A", "B{C", "a=B{C" },
                new string [] { " A =B{{C}", "A", "B{{C}", "a=B{{C}" },
                new string [] { "A={{{B}}}", "A", "{{{B}}}", "a={{{B}}}" },
                new string [] { "A={B}", "A", "{B}", "a={B}" },
                new string [] { "A= {B}", "A", "{B}", "a={B}" },
                new string [] { " A =BC",  "a", "BC", "a=BC" },
                new string [] { "\rA\t=BC",  "a", "BC", "a=BC" },
                new string [] { "\rA\t=BC",  "a", "BC", "a=BC" },
                new string [] { "A;B=BC",  "a;b", "BC", "a;b=BC" },
                };

            string[][] non_odbc_tests = new string[][] {
                new string [] { "A=''", "A", "", "a=" },
                new string [] { "A='BC;D'", "A", "BC;D", "a=\"BC;D\"" },
                new string [] { "A=BC''D", "A", "BC''D", "a=\"BC''D\"" },
                new string [] { "A='\"'", "A", "\"", "a='\"'" },
                new string [] { "A=B\"\"C;", "A", "B\"\"C", "a='B\"\"C'" },
                new string [] { "A={B{", "A", "{B{", "a={B{" },
                new string [] { "A={B}C", "A", "{B}C", "a={B}C" },
                new string [] { "A=B'C", "A", "B'C", "a=\"B'C\"" },
                new string [] { "A=B''C", "A", "B''C", "a=\"B''C\"" },
                new string [] { "A=  B C ;", "A", "B C", "a=\"B C\"" },
                new string [] { "A={B { }} }", "A", "{B { }} }", "a=\"{B { }} }\"" },
                new string [] { "A={B {{ }} }", "A", "{B {{ }} }", "a=\"{B {{ }} }\"" },
                new string [] { "A= B {C ", "A", "B {C", "a=\"B {C\"" },
                new string [] { "A= B }C ", "A", "B }C", "a=\"B }C\"" },
                new string [] { "A=B }C", "A", "B }C", "a=\"B }C\"" },
                new string [] { "A=B { }C", "A", "B { }C", "a=\"B { }C\"" },
                new string [] { "A= B{C {}}", "A", "B{C {}}", "a=\"B{C {}}\"" },
                new string [] { "A= {C {};B=A", "A", "{C {}", "a=\"{C {}\";b=A" },
                new string [] { "A= {C {}  ", "A", "{C {}", "a=\"{C {}\"" },
                new string [] { "A= {C {}  ;B=A", "A", "{C {}", "a=\"{C {}\";b=A" },
                new string [] { "A= {C {}}}", "A", "{C {}}}", "a=\"{C {}}}\"" },
                new string [] { "A={B=C}", "A", "{B=C}", "a=\"{B=C}\"" },
                new string [] { "A={B==C}", "A", "{B==C}", "a=\"{B==C}\"" },
                new string [] { "A=B==C", "A", "B==C", "a=\"B==C\"" },
                new string [] { "A={=}", "A", "{=}", "a=\"{=}\"" },
                new string [] { "A={==}", "A", "{==}", "a=\"{==}\"" },
                new string [] { "A=\"B;(C)'\"", "A", "B;(C)'", "a=\"B;(C)'\"" },
                new string [] { "A=B(=)C", "A", "B(=)C", "a=\"B(=)C\"" },
                new string [] { "A=B=C", "A", "B=C", "a=\"B=C\"" },
                new string [] { "A=B(==)C", "A", "B(==)C", "a=\"B(==)C\"" },
                new string [] { "A=B  C", "A", "B  C", "a=\"B  C\"" },
                new string [] { "A= B  C ", "A", "B  C", "a=\"B  C\"" },
                new string [] { "A=  B  C  ", "A", "B  C", "a=\"B  C\"" },
                new string [] { "A='  B C '", "A", "  B C ", "a=\"  B C \"" },
                new string [] { "A=\"  B C \"", "A", "  B C ", "a=\"  B C \"" },
                new string [] { "A={  B C }", "A", "{  B C }", "a=\"{  B C }\"" },
                new string [] { "A=  B C  ;", "A", "B C", "a=\"B C\"" },
                new string [] { "A=  B\rC\r\t;", "A", "B\rC", "a=\"B\rC\"" },
                new string [] { "A=\"\"\"B;C\"\"\"", "A", "\"B;C\"", "a='\"B;C\"'" },
                new string [] { "A= \"\"\"B;C\"\"\" ", "A", "\"B;C\"", "a='\"B;C\"'" },
                new string [] { "A='''B;C'''", "A", "'B;C'", "a=\"'B;C'\"" },
                new string [] { "A= '''B;C''' ", "A", "'B;C'", "a=\"'B;C'\"" },
                new string [] { "A={{", "A", "{{", "a={{" },
                new string [] { "A={B C}", "A", "{B C}", "a=\"{B C}\"" },
                new string [] { "A={ B C }", "A", "{ B C }", "a=\"{ B C }\"" },
                new string [] { "A={B {{ } }", "A", "{B {{ } }", "a=\"{B {{ } }\"" },
                new string [] { "A='='", "A", "=", "a=\"=\"" },
                new string [] { "A='=='", "A", "==", "a=\"==\"" },
                new string [] { "A=\"=\"", "A", "=", "a=\"=\"" },
                new string [] { "A=\"==\"", "A", "==", "a=\"==\"" },
                new string [] { "A={B}}", "A", "{B}}", "a={B}}" },
                new string [] { "A=\";\"", "A", ";", "a=\";\"" },
                new string [] { "A(=)=B", "A(", ")=B", "a(=\")=B\"" },
                new string [] { "A==B=C",  "A=B", "C", "a==b=C" },
                new string [] { "A===B=C",  "A=", "B=C", "a===\"B=C\"" },
                new string [] { "(A=)=BC",  "(a", ")=BC", "(a=\")=BC\"" },
                new string [] { "A==C=B==C", "a=c", "B==C", "a==c=\"B==C\"" },
                };
            DbConnectionStringBuilder sb;

            for (int i = 0; i < non_odbc_tests.Length; i++)
            {
                string[] test = non_odbc_tests[i];
                sb = new DbConnectionStringBuilder();
                sb.ConnectionString = test[0];
                Assert.Equal(test[3], sb.ConnectionString);
                Assert.Equal(test[2], sb[test[1]]);
            }

            for (int i = 0; i < non_odbc_tests.Length; i++)
            {
                string[] test = non_odbc_tests[i];
                sb = new DbConnectionStringBuilder(false);
                sb.ConnectionString = test[0];
                Assert.Equal(test[3], sb.ConnectionString);
                Assert.Equal(test[2], sb[test[1]]);
            }

            for (int i = 0; i < shared_tests.Length; i++)
            {
                string[] test = shared_tests[i];
                sb = new DbConnectionStringBuilder();
                sb.ConnectionString = test[0];
                Assert.Equal(test[3], sb.ConnectionString);
                Assert.Equal(test[2], sb[test[1]]);
            }

            for (int i = 0; i < shared_tests.Length; i++)
            {
                string[] test = shared_tests[i];
                sb = new DbConnectionStringBuilder(false);
                sb.ConnectionString = test[0];
                Assert.Equal(test[3], sb.ConnectionString);
                Assert.Equal(test[2], sb[test[1]]);
            }

            string[][] odbc_tests = new string[][] {
                new string [] { "A=B(=)C", "A", "B(=)C", "a=B(=)C" },
                new string [] { "A=B(==)C", "A", "B(==)C", "a=B(==)C" },
                new string [] { "A=  B C  ;", "A", "B C", "a=B C" },
                new string [] { "A=  B\rC\r\t;", "A", "B\rC", "a=B\rC" },
                new string [] { "A='''", "A", "'''", "a='''" },
                new string [] { "A=''", "A", "''", "a=''" },
                new string [] { "A=''B", "A", "''B", "a=''B" },
                new string [] { "A=BC''D", "A", "BC''D", "a=BC''D" },
                new string [] { "A='\"'", "A", "'\"'", "a='\"'" },
                new string [] { "A=\"\"B", "A", "\"\"B", "a=\"\"B"},
                new string [] { "A=B\"\"C;", "A", "B\"\"C", "a=B\"\"C" },
                new string [] { "A=\"B", "A", "\"B", "a=\"B" },
                new string [] { "A=\"", "A", "\"", "a=\"" },
                new string [] { "A=B'C", "A", "B'C", "a=B'C" },
                new string [] { "A=B''C", "A", "B''C", "a=B''C" },
                new string [] { "A='A'C", "A", "'A'C", "a='A'C" },
                new string [] { "A=B  C", "A", "B  C", "a=B  C" },
                new string [] { "A= B  C ", "A", "B  C", "a=B  C" },
                new string [] { "A=  B  C  ", "A", "B  C", "a=B  C" },
                new string [] { "A='  B C '", "A", "'  B C '", "a='  B C '" },
                new string [] { "A=\"  B C \"", "A", "\"  B C \"", "a=\"  B C \"" },
                new string [] { "A={  B C }", "A", "{  B C }", "a={  B C }" },
                new string [] { "A=  B C ;", "A", "B C", "a=B C" },
                new string [] { "A=\"\"BC\"\"", "A", "\"\"BC\"\"", "a=\"\"BC\"\"" },
                new string [] { "A=\"\"B\"C\"\";", "A", "\"\"B\"C\"\"", "a=\"\"B\"C\"\"" },
                new string [] { "A= \"\"B\"C\"\" ", "A", "\"\"B\"C\"\"", "a=\"\"B\"C\"\"" },
                new string [] { "A=''BC''", "A", "''BC''", "a=''BC''" },
                new string [] { "A=''B'C'';", "A", "''B'C''", "a=''B'C''" },
                new string [] { "A= ''B'C'' ", "A", "''B'C''", "a=''B'C''" },
                new string [] { "A={B C}", "A", "{B C}", "a={B C}" },
                new string [] { "A={ B C }", "A", "{ B C }", "a={ B C }" },
                new string [] { "A={ B;C }", "A", "{ B;C }", "a={ B;C }" },
                new string [] { "A={B { }} }", "A", "{B { }} }", "a={B { }} }" },
                new string [] { "A={ B;= {;=}};= }", "A", "{ B;= {;=}};= }", "a={ B;= {;=}};= }" },
                new string [] { "A={B {{ }} }", "A", "{B {{ }} }", "a={B {{ }} }" },
                new string [] { "A={ B;= {{:= }};= }", "A", "{ B;= {{:= }};= }", "a={ B;= {{:= }};= }" },
                new string [] { "A= B {C ", "A", "B {C", "a=B {C" },
                new string [] { "A= B }C ", "A", "B }C", "a=B }C" },
                new string [] { "A=B }C", "A", "B }C", "a=B }C" },
                new string [] { "A=B { }C", "A", "B { }C", "a=B { }C" },
                new string [] { "A= {B;{}", "A", "{B;{}", "a={B;{}" },
                new string [] { "A= {B;{}}}", "A", "{B;{}}}", "a={B;{}}}" },
                new string [] { "A= B{C {}}", "A", "B{C {}}", "a=B{C {}}" },
                new string [] { "A= {C {};B=A", "A", "{C {}", "a={C {};b=A" },
                new string [] { "A= {C {}  ", "A", "{C {}", "a={C {}" },
                new string [] { "A= {C {}  ;B=A", "A", "{C {}", "a={C {};b=A" },
                new string [] { "A= {C {}}}", "A", "{C {}}}", "a={C {}}}" },
                new string [] { "A={B=C}", "A", "{B=C}", "a={B=C}" },
                new string [] { "A={B==C}", "A", "{B==C}", "a={B==C}" },
                new string [] { "A=B==C", "A", "B==C", "a=B==C" },
                new string [] { "A='='", "A", "'='", "a='='" },
                new string [] { "A='=='", "A", "'=='", "a='=='" },
                new string [] { "A=\"=\"", "A", "\"=\"", "a=\"=\"" },
                new string [] { "A=\"==\"", "A", "\"==\"", "a=\"==\"" },
                new string [] { "A={=}", "A", "{=}", "a={=}" },
                new string [] { "A={==}", "A", "{==}", "a={==}" },
                new string [] { "A=B=C", "A", "B=C", "a=B=C" },
                new string [] { "A(=)=B", "A(", ")=B", "a(=)=B" },
                new string [] { "A==B=C",  "A", "=B=C", "a==B=C" },
                new string [] { "A===B=C",  "A", "==B=C", "a===B=C" },
                new string [] { "A'='=B=C",  "A'", "'=B=C", "a'='=B=C" },
                new string [] { "A\"=\"=B=C",  "A\"", "\"=B=C", "a\"=\"=B=C" },
                new string [] { "\"A=\"=BC",  "\"a", "\"=BC", "\"a=\"=BC" },
                new string [] { "(A=)=BC",  "(a", ")=BC", "(a=)=BC" },
                new string [] { "A==C=B==C", "A", "=C=B==C", "a==C=B==C" },
                };

            for (int i = 0; i < odbc_tests.Length; i++)
            {
                string[] test = odbc_tests[i];
                sb = new DbConnectionStringBuilder(true);
                sb.ConnectionString = test[0];
                Assert.Equal(test[3], sb.ConnectionString);
                Assert.Equal(test[2], sb[test[1]]);
            }

            for (int i = 0; i < shared_tests.Length; i++)
            {
                string[] test = shared_tests[i];
                sb = new DbConnectionStringBuilder(true);
                sb.ConnectionString = test[0];
                Assert.Equal(test[3], sb.ConnectionString);
                Assert.Equal(test[2], sb[test[1]]);
            }

            // each test that is in odbc_tests and not in non_odbc_tests
            // (or vice versa) should result in an ArgumentException
            AssertValueTest(non_odbc_tests, odbc_tests, true);
            AssertValueTest(odbc_tests, non_odbc_tests, false);
        }

        [Fact]
        public void EmbeddedChar_ConnectionString_Invalid()
        {
            string[] tests = new string[] {
                " =",
                "=",
                "=;",
                "=ABC;",
                "='A'",
                "A",
                "A=(B;)",
                "A=B';'C",
                "A=B { {;} }",
                "A=B { ; }C",
                "A=BC'E;F'D",
                };

            DbConnectionStringBuilder[] cbs = new DbConnectionStringBuilder[] {
                new DbConnectionStringBuilder (),
                new DbConnectionStringBuilder (false),
                new DbConnectionStringBuilder (true)
                };

            for (int i = 0; i < tests.Length; i++)
            {
                for (int j = 0; j < cbs.Length; j++)
                {
                    DbConnectionStringBuilder cb = cbs[j];
                    try
                    {
                        cb.ConnectionString = tests[i];
                        Assert.False(true);
                    }
                    catch (ArgumentException ex)
                    {
                        // Format of the initialization string does
                        // not conform to specification starting
                        // at index 0
                        Assert.Equal(typeof(ArgumentException), ex.GetType());
                        Assert.Null(ex.InnerException);
                        Assert.NotNull(ex.Message);
                        Assert.Null(ex.ParamName);
                    }
                }
            }
        }

        private void AssertValueTest(string[][] tests1, string[][] tests2, bool useOdbc)
        {
            DbConnectionStringBuilder sb = new DbConnectionStringBuilder(useOdbc);
            for (int i = 0; i < tests1.Length; i++)
            {
                string[] test1 = tests1[i];
                bool found = false;
                for (int j = 0; j < tests2.Length; j++)
                {
                    string[] test2 = tests2[j];
                    if (test2[0] == test1[0])
                    {
                        found = true;

                        if (test2[1] != test1[1])
                            continue;
                        if (test2[2] != test1[2])
                            continue;
                        if (test2[3] != test1[3])
                            continue;

                        Assert.False(true);
                    }
                }
                if (found)
                    continue;

                ArgumentException ex = Assert.Throws<ArgumentException>(() => sb.ConnectionString = test1[0]);
                // Format of the initialization string does
                // not conform to specification starting
                // at index 0
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.Null(ex.ParamName);
            }

            // check uniqueness of tests
            for (int i = 0; i < tests1.Length; i++)
            {
                for (int j = 0; j < tests1.Length; j++)
                {
                    if (i == j)
                        continue;
                    Assert.NotEqual(tests1[i], tests1[j]);
                }
            }

            // check uniqueness of tests
            for (int i = 0; i < tests2.Length; i++)
            {
                for (int j = 0; j < tests2.Length; j++)
                {
                    if (i == j)
                        continue;
                    Assert.NotEqual(tests2[i], tests2[j]);
                }
            }
        }
    }
}
