// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Data.Common
{
    public class RowUpdatedEventArgsTest
    {
        [Fact]
        public void Ctor_InvalidStatementType_ThrowsArgumentOutOfRangeException()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>(nameof(StatementType), () => new RowUpdatedEventArgs(null, null, (StatementType)100, null));
        }

        [Fact]
        public void Ctor_AssignsCorrectPropertyValues()
        {
            var argsEmpty = new RowUpdatedEventArgs(null, null, 0, null);
            Assert.Null(argsEmpty.Row);
            Assert.Null(argsEmpty.Command);
            Assert.Null(argsEmpty.TableMapping);
            Assert.Equal((StatementType)0, argsEmpty.StatementType);
            Assert.Equal(0, argsEmpty.RowCount);
            Assert.Equal(0, argsEmpty.RecordsAffected);

            var table = new DataTable();
            var args = new RowUpdatedEventArgs(table.NewRow(), null, StatementType.Update, new DataTableMapping());
            Assert.NotNull(args.Row);
            Assert.Same(table, args.Row.Table);
            Assert.Equal(1, args.RowCount);
            Assert.Null(args.Command);
            Assert.Equal(StatementType.Update, args.StatementType);
            Assert.NotNull(args.TableMapping);
        }

        [Fact]
        public void Status_SetInvalidUpdateStatus_ThrowsArgumentOutOfRangeException()
        {
            var args = new RowUpdatedEventArgs(null, null, 0, null);
            AssertExtensions.Throws<ArgumentOutOfRangeException>(nameof(UpdateStatus), () => args.Status = (UpdateStatus)100);
        }

        [Fact]
        public void CopyToRows()
        {
            var table = new DataTable();
            var args = new RowUpdatedEventArgs(table.NewRow(), null, StatementType.Update, null);

            Assert.Throws<ArgumentNullException>(() => args.CopyToRows(null));

            var newRows = new DataRow[2];

            args.CopyToRows(newRows, 1);
            Assert.Null(newRows[0]);
            Assert.NotNull(newRows[1]);
            Assert.Same(table, newRows[1].Table);

            Assert.Throws<IndexOutOfRangeException>(() => args.CopyToRows(newRows, 2));
        }
    }
}
