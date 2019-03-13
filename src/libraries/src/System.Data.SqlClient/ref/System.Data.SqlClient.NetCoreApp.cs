// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.SqlServer.Server
{
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property | System.AttributeTargets.ReturnValue, AllowMultiple=false, Inherited=false)]
    public partial class SqlFacetAttribute : System.Attribute
    {
        public SqlFacetAttribute() { }
        public bool IsFixedLength { get { throw null; } set { } }
        public bool IsNullable { get { throw null; } set { } }
        public int MaxSize { get { throw null; } set { } }
        public int Precision { get { throw null; } set { } }
        public int Scale { get { throw null; } set { } }
    }
}
namespace System.Data.SqlClient
{
    public partial class SqlDataReader : System.Data.Common.IDbColumnSchemaGenerator
    {
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Data.Common.DbColumn> GetColumnSchema() { throw null; }
    }
    public enum PoolBlockingPeriod
    {
        AlwaysBlock = 1,
        Auto = 0,
        NeverBlock = 2,
    }
    public sealed partial class SqlConnectionStringBuilder : System.Data.Common.DbConnectionStringBuilder
    {
        public System.Data.SqlClient.PoolBlockingPeriod PoolBlockingPeriod { get { throw null; } set { } }
    }
}
namespace System.Data.SqlTypes
{
    public sealed partial class SqlFileStream : System.IO.Stream
    {
        public SqlFileStream(string path, byte[] transactionContext, System.IO.FileAccess access) { }
        public SqlFileStream(string path, byte[] transactionContext, System.IO.FileAccess access, System.IO.FileOptions options, long allocationSize) { }
        public override bool CanRead { get { throw null; } }
        public override bool CanSeek { get { throw null; } }
        public override bool CanWrite { get { throw null; } }
        public override long Length { get { throw null; } }
        public string Name { get { throw null; } }
        public override long Position { get { throw null; } set { } }
        public byte[] TransactionContext { get { throw null; } }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) { throw null; }
        public override long Seek(long offset, System.IO.SeekOrigin origin) { throw null; }
        public override void SetLength(long value) { }
        public override void Write(byte[] buffer, int offset, int count) { }
    }
}
