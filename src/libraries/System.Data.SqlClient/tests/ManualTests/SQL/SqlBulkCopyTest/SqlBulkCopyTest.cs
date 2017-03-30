// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using Xunit;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class SqlBulkCopyTest
    {
        private string srcConstr = null;
        private string dstConstr = null;

        public SqlBulkCopyTest()
        {
            srcConstr = DataTestUtility.TcpConnStr;
            dstConstr = (new SqlConnectionStringBuilder(srcConstr) { InitialCatalog = "tempdb" }).ConnectionString;
        }

        public string AddGuid(string stringin)
        {
            stringin += "_" + Guid.NewGuid().ToString().Replace('-', '_');
            return stringin;
        }

        [CheckConnStrSetupFact]
        public void CopyAllFromReaderTest()
        {
            CopyAllFromReader.Test(srcConstr, dstConstr, AddGuid("SqlBulkCopyTest_CopyAllFromReader"));
        }

        [CheckConnStrSetupFact]
        public void CopyAllFromReader1Test()
        {
            CopyAllFromReader1.Test(srcConstr, dstConstr, AddGuid("SqlBulkCopyTest_CopyAllFromReader1"));
        }

        [CheckConnStrSetupFact]
        public void CopyMultipleReadersTest()
        {
            CopyMultipleReaders.Test(srcConstr, dstConstr, AddGuid("SqlBulkCopyTest_CopyMultipleReaders"));
        }

        [CheckConnStrSetupFact]
        public void CopySomeFromReaderTest()
        {
            CopySomeFromReader.Test(srcConstr, dstConstr, AddGuid("SqlBulkCopyTest_CopySomeFromReader"));
        }

        [CheckConnStrSetupFact]
        public void CopySomeFromDataTableTest()
        {
            CopySomeFromDataTable.Test(srcConstr, dstConstr, AddGuid("SqlBulkCopyTest_CopySomeFromDataTable"));
        }

        [CheckConnStrSetupFact]
        public void CopySomeFromRowArrayTest()
        {
            CopySomeFromRowArray.Test(srcConstr, dstConstr, AddGuid("SqlBulkCopyTest_CopySomeFromRowArray"));
        }

        [CheckConnStrSetupFact]
        public void CopyWithEventTest()
        {
            CopyWithEvent.Test(srcConstr, dstConstr, AddGuid("SqlBulkCopyTest_CopyWithEvent"));
        }

        [CheckConnStrSetupFact]
        public void CopyWithEvent1Test()
        {
            CopyWithEvent1.Test(srcConstr, dstConstr, AddGuid("SqlBulkCopyTest_CopyWithEvent1"));
        }

        [CheckConnStrSetupFact]
        public void InvalidAccessFromEventTest()
        {
            InvalidAccessFromEvent.Test(srcConstr, dstConstr, AddGuid("SqlBulkCopyTest_InvalidAccessFromEvent"));
        }

        [CheckConnStrSetupFact]
        public void Bug84548Test()
        {
            Bug84548.Test(srcConstr, dstConstr, AddGuid("SqlBulkCopyTest_Bug84548"));
        }

        [CheckConnStrSetupFact]
        public void MissingTargetTableTest()
        {
            MissingTargetTable.Test(srcConstr, dstConstr, AddGuid("SqlBulkCopyTest_MissingTargetTable"));
        }

        [CheckConnStrSetupFact]
        public void MissingTargetColumnTest()
        {
            MissingTargetColumn.Test(srcConstr, dstConstr, AddGuid("SqlBulkCopyTest_MissingTargetColumn"));
        }

        [CheckConnStrSetupFact]
        public void Bug85007Test()
        {
            Bug85007.Test(srcConstr, dstConstr, AddGuid("SqlBulkCopyTest_Bug85007"));
        }

        [CheckConnStrSetupFact]
        public void CheckConstraintsTest()
        {
            CheckConstraints.Test(dstConstr, AddGuid("SqlBulkCopyTest_Extensionsrc"), AddGuid("SqlBulkCopyTest_Extensiondst"));
        }

        [CheckConnStrSetupFact]
        public void TransactionTest()
        {
            Transaction.Test(srcConstr, dstConstr, AddGuid("SqlBulkCopyTest_Transaction0"));
        }

        [CheckConnStrSetupFact]
        public void Transaction1Test()
        {
            Transaction1.Test(srcConstr, dstConstr, AddGuid("SqlBulkCopyTest_Transaction1"));
        }

        [CheckConnStrSetupFact]
        public void Transaction2Test()
        {
            Transaction2.Test(srcConstr, dstConstr, AddGuid("SqlBulkCopyTest_Transaction2"));
        }

        [CheckConnStrSetupFact]
        public void Transaction3Test()
        {
            Transaction3.Test(srcConstr, dstConstr, AddGuid("SqlBulkCopyTest_Transaction3"));
        }

        [CheckConnStrSetupFact]
        public void Transaction4Test()
        {
            Transaction4.Test(srcConstr, dstConstr, AddGuid("SqlBulkCopyTest_Transaction4"));
        }

        [CheckConnStrSetupFact]
        public void CopyVariantsTest()
        {
            CopyVariants.Test(dstConstr, AddGuid("SqlBulkCopyTest_Variants"));
        }

        [CheckConnStrSetupFact]
        public void Bug98182Test()
        {
            Bug98182.Test(dstConstr, AddGuid("SqlBulkCopyTest_Bug98182 "));
        }

        [CheckConnStrSetupFact]
        public void FireTriggerTest()
        {
            FireTrigger.Test(srcConstr, dstConstr, AddGuid("SqlBulkCopyTest_FireTrigger"));
        }

        [CheckConnStrSetupFact]
        public void ErrorOnRowsMarkedAsDeletedTest()
        {
            ErrorOnRowsMarkedAsDeleted.Test(dstConstr, AddGuid("SqlBulkCopyTest_ErrorOnRowsMarkedAsDeleted"));
        }

        [CheckConnStrSetupFact]
        public void SpecialCharacterNamesTest()
        {
            SpecialCharacterNames.Test(srcConstr, dstConstr, AddGuid("SqlBulkCopyTest_SpecialCharacterNames"));
        }

        [CheckConnStrSetupFact]
        public void Bug903514Test()
        {
            Bug903514.Test(dstConstr, AddGuid("SqlBulkCopyTest_Bug903514"));
        }

        [CheckConnStrSetupFact]
        public void ColumnCollationTest()
        {
            ColumnCollation.Test(dstConstr, AddGuid("SqlBulkCopyTest_ColumnCollation"));
        }

        [CheckConnStrSetupFact]
        public void CopyAllFromReaderAsyncTest()
        {
            CopyAllFromReaderAsync.Test(srcConstr, dstConstr, AddGuid("SqlBulkCopyTest_AsyncTest1")); //Async + Reader
        }

        [CheckConnStrSetupFact]
        public void CopySomeFromRowArrayAsyncTest()
        {
            CopySomeFromRowArrayAsync.Test(srcConstr, dstConstr, AddGuid("SqlBulkCopyTest_AsyncTest2")); //Async + Some Rows
        }

        [CheckConnStrSetupFact]
        public void CopySomeFromDataTableAsyncTest()
        {
            CopySomeFromDataTableAsync.Test(srcConstr, dstConstr, AddGuid("SqlBulkCopyTest_AsyncTest3")); //Async + Some Table
        }

        [CheckConnStrSetupFact]
        public void CopyWithEventAsyncTest()
        {
            CopyWithEventAsync.Test(srcConstr, dstConstr, AddGuid("SqlBulkCopyTest_AsyncTest4")); //Async + Rows + Notification
        }

        [CheckConnStrSetupFact]
        public void CopyAllFromReaderCancelAsyncTest()
        {
            CopyAllFromReaderCancelAsync.Test(srcConstr, dstConstr, AddGuid("SqlBulkCopyTest_AsyncTest5")); //Async + Reader + cancellation token
        }

        [CheckConnStrSetupFact]
        public void CopyAllFromReaderConnectionClosedAsyncTest()
        {
            CopyAllFromReaderConnectionClosedAsync.Test(srcConstr, dstConstr, AddGuid("SqlBulkCopyTest_AsyncTest6")); //Async + Reader + Connection closed
        }

        [CheckConnStrSetupFact]
        public void CopyAllFromReaderConnectionClosedOnEventAsyncTest()
        {
            CopyAllFromReaderConnectionClosedOnEventAsync.Test(srcConstr, dstConstr, AddGuid("SqlBulkCopyTest_AsyncTest7")); //Async + Reader + Connection closed during the event
        }

        [CheckConnStrSetupFact]
        public void TransactionTestAsyncTest()
        {
            TransactionTestAsync.Test(srcConstr, dstConstr, AddGuid("SqlBulkCopyTest_TransactionTestAsync")); //Async + Transaction rollback
        }
    }
}