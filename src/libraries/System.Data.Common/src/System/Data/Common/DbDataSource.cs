// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data.Common
{
    public abstract class DbDataSource : IDisposable, IAsyncDisposable
    {
        public abstract string ConnectionString { get; }

        protected abstract DbConnection CreateDbConnection();

        protected virtual DbConnection OpenDbConnection()
        {
            var connection = CreateDbConnection();

            try
            {
                connection.Open();
                return connection;
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        protected virtual async ValueTask<DbConnection> OpenDbConnectionAsync(CancellationToken cancellationToken = default)
        {
            var connection = CreateDbConnection();

            try
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                return connection;
            }
            catch
            {
                await connection.DisposeAsync().ConfigureAwait(false);
                throw;
            }
        }

        protected virtual DbCommand CreateDbCommand(string? commandText = null)
        {
            var command = CreateDbConnection().CreateCommand();
            command.CommandText = commandText;

            return new DbCommandWrapper(command);
        }

        protected virtual DbBatch CreateDbBatch()
            => new DbBatchWrapper(CreateDbConnection().CreateBatch());

        public DbConnection CreateConnection()
            => CreateDbConnection();

        public DbConnection OpenConnection()
            => OpenDbConnection();

        public ValueTask<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
            => OpenDbConnectionAsync(cancellationToken);

        public DbCommand CreateCommand(string? commandText = null)
            => CreateDbCommand(commandText);

        public DbBatch CreateBatch()
            => CreateDbBatch();

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);

            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        protected virtual ValueTask DisposeAsyncCore()
            => default;

        private sealed class DbCommandWrapper : DbCommand
        {
            private readonly DbCommand _wrappedCommand;
            private readonly DbConnection _connection;

            internal DbCommandWrapper(DbCommand wrappedCommand)
            {
                Debug.Assert(wrappedCommand.Connection is not null);

                _wrappedCommand = wrappedCommand;
                _connection = wrappedCommand.Connection;
            }

            public override int ExecuteNonQuery()
            {
                _connection.Open();

                try
                {
                    return _wrappedCommand.ExecuteNonQuery();
                }
                finally
                {
                    try
                    {
                        _connection.Close();
                    }
                    catch (Exception e)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(e);

                        // Swallow to allow the original exception to bubble up.
                        // Also, refrain from bubbling up the close exception even if there's no original exception,
                        // since it's not relevant to the user - execution did complete successfully, and the connection
                        // close is just an internal detail that shouldn't cause user code to fail.
                    }
                }
            }

            public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
            {
                await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    return await _wrappedCommand.ExecuteNonQueryAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                finally
                {
                    try
                    {
                        await _connection.CloseAsync().ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(e);

                        // Swallow to allow the original exception to bubble up
                        // Also, refrain from bubbling up the close exception even if there's no original exception,
                        // since it's not relevant to the user - execution did complete successfully, and the connection
                        // close is just an internal detail that shouldn't cause user code to fail.
                    }
                }
            }

            public override object? ExecuteScalar()
            {
                _connection.Open();

                try
                {
                    return _wrappedCommand.ExecuteScalar();
                }
                finally
                {
                    try
                    {
                        _connection.Close();
                    }
                    catch (Exception e)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(e);

                        // Swallow to allow the original exception to bubble up
                        // Also, refrain from bubbling up the close exception even if there's no original exception,
                        // since it's not relevant to the user - execution did complete successfully, and the connection
                        // close is just an internal detail that shouldn't cause user code to fail.
                    }
                }
            }

            public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
            {
                await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    return await _wrappedCommand.ExecuteScalarAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                finally
                {
                    try
                    {
                        await _connection.CloseAsync().ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(e);

                        // Swallow to allow the original exception to bubble up
                        // Also, refrain from bubbling up the close exception even if there's no original exception,
                        // since it's not relevant to the user - execution did complete successfully, and the connection
                        // close is just an internal detail that shouldn't cause user code to fail.
                    }
                }
            }

            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
            {
                _connection.Open();

                try
                {
                    return _wrappedCommand.ExecuteReader(behavior | CommandBehavior.CloseConnection);
                }
                catch
                {
                    try
                    {
                        _connection.Close();
                    }
                    catch (Exception e)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(e);

                        // Swallow to allow the original exception to bubble up
                    }

                    throw;
                }
            }

            protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(
                CommandBehavior behavior,
                CancellationToken cancellationToken)
            {
                await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    return await _wrappedCommand.ExecuteReaderAsync(
                            behavior | CommandBehavior.CloseConnection,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                catch
                {
                    try
                    {
                        await _connection.CloseAsync().ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(e);

                        // Swallow to allow the original exception to bubble up
                    }

                    throw;
                }
            }

            protected override DbParameter CreateDbParameter()
                => _wrappedCommand.CreateParameter();

            public override void Cancel()
                => _wrappedCommand.Cancel();

            [AllowNull]
            public override string CommandText
            {
                get => _wrappedCommand.CommandText;
                set => _wrappedCommand.CommandText = value;
            }

            public override int CommandTimeout
            {
                get => _wrappedCommand.CommandTimeout;
                set => _wrappedCommand.CommandTimeout = value;
            }

            public override CommandType CommandType
            {
                get => _wrappedCommand.CommandType;
                set => _wrappedCommand.CommandType = value;
            }

            protected override DbParameterCollection DbParameterCollection
                => _wrappedCommand.Parameters;

            public override bool DesignTimeVisible
            {
                get => _wrappedCommand.DesignTimeVisible;
                set => _wrappedCommand.DesignTimeVisible = value;
            }

            public override UpdateRowSource UpdatedRowSource
            {
                get => _wrappedCommand.UpdatedRowSource;
                set => _wrappedCommand.UpdatedRowSource = value;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    var connection = _wrappedCommand.Connection;

                    _wrappedCommand.Dispose();
                    connection!.Dispose();
                }
            }

            public override async ValueTask DisposeAsync()
            {
                var connection = _wrappedCommand.Connection;

                await _wrappedCommand.DisposeAsync().ConfigureAwait(false);
                await connection!.DisposeAsync().ConfigureAwait(false);
            }

            // In most case, preparation doesn't make sense on a connectionless command since prepared statements are
            // usually bound to specific physical connections.
            // When prepared statements are global (not bound to a specific connection), providers would need to
            // provide their own connection-less implementation anyway (i.e. interacting with the originating
            // DbDataSource), so they'd have to override this in any case.
            public override void Prepare()
                => throw ExceptionBuilder.NotSupportedOnDataSourceCommand();

            public override Task PrepareAsync(CancellationToken cancellationToken = default)
                => Task.FromException(ExceptionBuilder.NotSupportedOnDataSourceCommand());

            // The below are incompatible with commands executed directly against DbDataSource, since no DbConnection
            // is involved at the user API level and the DbCommandWrapper owns the DbConnection.
            protected override DbConnection? DbConnection
            {
                get => throw ExceptionBuilder.NotSupportedOnDataSourceCommand();
                set => throw ExceptionBuilder.NotSupportedOnDataSourceCommand();
            }

            protected override DbTransaction? DbTransaction
            {
                get => throw ExceptionBuilder.NotSupportedOnDataSourceCommand();
                set => throw ExceptionBuilder.NotSupportedOnDataSourceCommand();
            }
        }

        private sealed class DbBatchWrapper : DbBatch
        {
            private readonly DbBatch _wrappedBatch;
            private readonly DbConnection _connection;

            internal DbBatchWrapper(DbBatch wrappedBatch)
            {
                Debug.Assert(wrappedBatch.Connection is not null);

                _wrappedBatch = wrappedBatch;
                _connection = wrappedBatch.Connection;
            }

            public override int ExecuteNonQuery()
            {
                _connection.Open();

                try
                {
                    return _wrappedBatch.ExecuteNonQuery();
                }
                finally
                {
                    try
                    {
                        _connection.Close();
                    }
                    catch (Exception e)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(e);

                        // Swallow to allow the original exception to bubble up
                        // Also, refrain from bubbling up the close exception even if there's no original exception,
                        // since it's not relevant to the user - execution did complete successfully, and the connection
                        // close is just an internal detail that shouldn't cause user code to fail.
                    }
                }
            }

            public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
            {
                await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    return await _wrappedBatch.ExecuteNonQueryAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                finally
                {
                    try
                    {
                        await _connection.CloseAsync().ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(e);

                        // Swallow to allow the original exception to bubble up
                        // Also, refrain from bubbling up the close exception even if there's no original exception,
                        // since it's not relevant to the user - execution did complete successfully, and the connection
                        // close is just an internal detail that shouldn't cause user code to fail.
                    }
                }
            }

            public override object? ExecuteScalar()
            {
                _connection.Open();

                try
                {
                    return _wrappedBatch.ExecuteScalar();
                }
                finally
                {
                    try
                    {
                        _connection.Close();
                    }
                    catch (Exception e)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(e);

                        // Swallow to allow the original exception to bubble up
                        // Also, refrain from bubbling up the close exception even if there's no original exception,
                        // since it's not relevant to the user - execution did complete successfully, and the connection
                        // close is just an internal detail that shouldn't cause user code to fail.
                    }
                }
            }

            public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
            {
                await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    return await _wrappedBatch.ExecuteScalarAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                finally
                {
                    try
                    {
                        await _connection.CloseAsync().ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(e);

                        // Swallow to allow the original exception to bubble up
                        // Also, refrain from bubbling up the close exception even if there's no original exception,
                        // since it's not relevant to the user - execution did complete successfully, and the connection
                        // close is just an internal detail that shouldn't cause user code to fail.
                    }
                }
            }

            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
            {
                _connection.Open();

                try
                {
                    return _wrappedBatch.ExecuteReader(behavior | CommandBehavior.CloseConnection);
                }
                catch
                {
                    try
                    {
                        _connection.Close();
                    }
                    catch (Exception e)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(e);

                        // Swallow to allow the original exception to bubble up
                    }

                    throw;
                }
            }

            protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(
                CommandBehavior behavior,
                CancellationToken cancellationToken)
            {
                await _connection.OpenAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    return await _wrappedBatch.ExecuteReaderAsync(
                            behavior | CommandBehavior.CloseConnection,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                catch
                {
                    try
                    {
                        await _connection.CloseAsync().ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        ExceptionBuilder.TraceExceptionWithoutRethrow(e);

                        // Swallow to allow the original exception to bubble up
                    }

                    throw;
                }
            }

            protected override DbBatchCommand CreateDbBatchCommand() => throw new NotImplementedException();

            public override void Cancel()
                => _wrappedBatch.Cancel();

            protected override DbBatchCommandCollection DbBatchCommands => _wrappedBatch.BatchCommands;

            public override int Timeout
            {
                get => _wrappedBatch.Timeout;
                set => _wrappedBatch.Timeout = value;
            }

            public override void Dispose()
            {
                var connection = _wrappedBatch.Connection;

                _wrappedBatch.Dispose();
                connection!.Dispose();
            }

            public override async ValueTask DisposeAsync()
            {
                var connection = _wrappedBatch.Connection;

                await _wrappedBatch.DisposeAsync().ConfigureAwait(false);
                await connection!.DisposeAsync().ConfigureAwait(false);
            }

            // In most case, preparation doesn't make sense on a connectionless command since prepared statements are
            // usually bound to specific physical connections.
            // When prepared statements are global (not bound to a specific connection), providers would need to
            // provide their own connection-less implementation anyway (i.e. interacting with the originating
            // DbDataSource), so they'd have to override this in any case.
            public override void Prepare()
                => throw ExceptionBuilder.NotSupportedOnDataSourceCommand();

            public override Task PrepareAsync(CancellationToken cancellationToken = default)
                => Task.FromException(ExceptionBuilder.NotSupportedOnDataSourceCommand());

            // The below are incompatible with batches executed directly against DbDataSource, since no DbConnection
            // is involved at the user API level and the DbBatchWrapper owns the DbConnection.
            protected override DbConnection? DbConnection
            {
                get => throw ExceptionBuilder.NotSupportedOnDataSourceBatch();
                set => throw ExceptionBuilder.NotSupportedOnDataSourceBatch();
            }

            protected override DbTransaction? DbTransaction
            {
                get => throw ExceptionBuilder.NotSupportedOnDataSourceBatch();
                set => throw ExceptionBuilder.NotSupportedOnDataSourceBatch();
            }
        }
    }
}
