// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

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

using System.Data.Common;

using Xunit;

namespace System.Data.Tests.Common
{
    public class DataAdapterTest
    {
        [Fact]
        public void AcceptChangesDuringFill()
        {
            DataAdapter da = new MyAdapter();
            da.AcceptChangesDuringFill = true;
            Assert.True(da.AcceptChangesDuringFill);
            da.AcceptChangesDuringFill = false;
            Assert.False(da.AcceptChangesDuringFill);
            da.AcceptChangesDuringFill = true;
            Assert.True(da.AcceptChangesDuringFill);
        }

        [Fact]
        public void AcceptChangesDuringUpdate()
        {
            DataAdapter da = new MyAdapter();
            da.AcceptChangesDuringUpdate = true;
            Assert.True(da.AcceptChangesDuringUpdate);
            da.AcceptChangesDuringUpdate = false;
            Assert.False(da.AcceptChangesDuringUpdate);
            da.AcceptChangesDuringUpdate = true;
            Assert.True(da.AcceptChangesDuringUpdate);
        }

        [Fact]
        public void ContinueUpdateOnError()
        {
            DataAdapter da = new MyAdapter();
            da.ContinueUpdateOnError = true;
            Assert.True(da.ContinueUpdateOnError);
            da.ContinueUpdateOnError = false;
            Assert.False(da.ContinueUpdateOnError);
            da.ContinueUpdateOnError = true;
            Assert.True(da.ContinueUpdateOnError);
        }

        [Fact]
        public void Fill_Direct()
        {
            DataAdapter da = new MyAdapter();
            DataSet ds = new DataSet();
            try
            {
                da.Fill(ds);
                Assert.False(true);
            }
            catch (NotSupportedException ex)
            {
                // Specified method is not supported
                Assert.Equal(typeof(NotSupportedException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
            }
        }

        [Fact]
        public void FillLoadOption()
        {
            DataAdapter da = new MyAdapter();
            da.FillLoadOption = LoadOption.PreserveChanges;
            Assert.Equal(LoadOption.PreserveChanges, da.FillLoadOption);
            da.FillLoadOption = LoadOption.OverwriteChanges;
            Assert.Equal(LoadOption.OverwriteChanges, da.FillLoadOption);
            da.FillLoadOption = LoadOption.Upsert;
            Assert.Equal(LoadOption.Upsert, da.FillLoadOption);
        }

        [Fact]
        public void FillLoadOption_Invalid()
        {
            DataAdapter da = new MyAdapter();
            try
            {
                da.FillLoadOption = (LoadOption)666;
                Assert.False(true);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                // The LoadOption enumeration value, 666, is invalid
                Assert.Equal(typeof(ArgumentOutOfRangeException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.True(ex.Message.IndexOf("LoadOption") != -1);
                Assert.True(ex.Message.IndexOf("666") != -1);
                Assert.NotNull(ex.ParamName);
                Assert.Equal("LoadOption", ex.ParamName);
            }
        }

        [Fact]
        public void MissingMappingAction_Valid()
        {
            DataAdapter da = new MyAdapter();
            da.MissingMappingAction = MissingMappingAction.Passthrough;
            Assert.Equal(MissingMappingAction.Passthrough, da.MissingMappingAction);
            da.MissingMappingAction = MissingMappingAction.Ignore;
            Assert.Equal(MissingMappingAction.Ignore, da.MissingMappingAction);
            da.MissingMappingAction = MissingMappingAction.Error;
            Assert.Equal(MissingMappingAction.Error, da.MissingMappingAction);
        }

        [Fact]
        public void MissingMappingAction_Invalid()
        {
            DataAdapter da = new MyAdapter();
            try
            {
                da.MissingMappingAction = (MissingMappingAction)666;
                Assert.False(true);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                // The MissingMappingAction enumeration value, 666, is invalid
                Assert.Equal(typeof(ArgumentOutOfRangeException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.True(ex.Message.IndexOf("MissingMappingAction") != -1);
                Assert.True(ex.Message.IndexOf("666") != -1);
                Assert.NotNull(ex.ParamName);
                Assert.Equal("MissingMappingAction", ex.ParamName);
            }
        }

        [Fact]
        public void MissingSchemaAction_Valid()
        {
            DataAdapter da = new MyAdapter();
            da.MissingSchemaAction = MissingSchemaAction.AddWithKey;
            Assert.Equal(MissingSchemaAction.AddWithKey, da.MissingSchemaAction);
            da.MissingSchemaAction = MissingSchemaAction.Ignore;
            Assert.Equal(MissingSchemaAction.Ignore, da.MissingSchemaAction);
            da.MissingSchemaAction = MissingSchemaAction.Error;
            Assert.Equal(MissingSchemaAction.Error, da.MissingSchemaAction);
        }

        [Fact]
        public void MissingSchemaAction_Invalid()
        {
            DataAdapter da = new MyAdapter();
            try
            {
                da.MissingSchemaAction = (MissingSchemaAction)666;
                Assert.False(true);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                // The MissingSchemaAction enumeration value, 666, is invalid
                Assert.Equal(typeof(ArgumentOutOfRangeException), ex.GetType());
                Assert.Null(ex.InnerException);
                Assert.NotNull(ex.Message);
                Assert.True(ex.Message.IndexOf("MissingSchemaAction") != -1);
                Assert.True(ex.Message.IndexOf("666") != -1);
                Assert.NotNull(ex.ParamName);
                Assert.Equal("MissingSchemaAction", ex.ParamName);
            }
        }

        [Fact]
        public void ReturnProviderSpecificTypes()
        {
            DataAdapter da = new MyAdapter();
            da.ReturnProviderSpecificTypes = true;
            Assert.True(da.ReturnProviderSpecificTypes);
            da.ReturnProviderSpecificTypes = false;
            Assert.False(da.ReturnProviderSpecificTypes);
            da.ReturnProviderSpecificTypes = true;
            Assert.True(da.ReturnProviderSpecificTypes);
        }
    }

    internal class MyAdapter : DataAdapter
    {
    }
}
