// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Relational;
using Microsoft.Data.Entity.Relational.Migrations.Operations;
using Npgsql.EntityFramework7.Migrations;
using Microsoft.Data.Entity.Utilities;

namespace Npgsql.EntityFramework7
{
    public class NpgsqlDataStoreCreator : RelationalDataStoreCreator, INpgsqlDataStoreCreator
    {
        private readonly INpgsqlEFConnection _connection;
        private readonly INpgsqlModelDiffer _modelDiffer;
        private readonly INpgsqlMigrationSqlGenerator _sqlGenerator;
        private readonly SqlStatementExecutor _statementExecutor;

        public NpgsqlDataStoreCreator(
            [NotNull] INpgsqlEFConnection connection,
            [NotNull] INpgsqlModelDiffer modelDiffer,
            [NotNull] INpgsqlMigrationSqlGenerator sqlGenerator,
            [NotNull] SqlStatementExecutor statementExecutor)
        {
            Check.NotNull(connection, nameof(connection));
            Check.NotNull(modelDiffer, nameof(modelDiffer));
            Check.NotNull(sqlGenerator, nameof(sqlGenerator));
            Check.NotNull(statementExecutor, nameof(statementExecutor));

            _connection = connection;
            _modelDiffer = modelDiffer;
            _sqlGenerator = sqlGenerator;
            _statementExecutor = statementExecutor;
        }

        public override void Create()
        {
            using (var masterConnection = _connection.CreateMasterConnection())
            {
                _statementExecutor.ExecuteNonQuery(masterConnection, null, CreateCreateOperations());
                ClearPool();
            }

            Exists(retryOnNotExists: true);
        }

        public override async Task CreateAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            using (var masterConnection = _connection.CreateMasterConnection())
            {
                await _statementExecutor
                    .ExecuteNonQueryAsync(masterConnection, null, CreateCreateOperations(), cancellationToken)
                    .WithCurrentCulture();
                ClearPool();
            }

            await ExistsAsync(retryOnNotExists: true, cancellationToken: cancellationToken).WithCurrentCulture();
        }

        public override void CreateTables(IModel model)
        {
            Check.NotNull(model, nameof(model));

            _statementExecutor.ExecuteNonQuery(_connection, _connection.DbTransaction, CreateSchemaCommands(model));
        }

        public override async Task CreateTablesAsync(IModel model, CancellationToken cancellationToken = default(CancellationToken))
        {
            Check.NotNull(model, nameof(model));

            await _statementExecutor
                .ExecuteNonQueryAsync(_connection, _connection.DbTransaction, CreateSchemaCommands(model), cancellationToken)
                .WithCurrentCulture();
        }

        public override bool HasTables()
            => (int)_statementExecutor.ExecuteScalar(_connection, _connection.DbTransaction, CreateHasTablesCommand()) != 0;

        public override async Task<bool> HasTablesAsync(CancellationToken cancellationToken = default(CancellationToken))
            => (int)(await _statementExecutor
                .ExecuteScalarAsync(_connection, _connection.DbTransaction, CreateHasTablesCommand(), cancellationToken)
                .WithCurrentCulture()) != 0;

        private IEnumerable<SqlBatch> CreateSchemaCommands(IModel model)
            => _sqlGenerator.Generate(_modelDiffer.GetDifferences(null, model), model);

        private string CreateHasTablesCommand()
            => @"
                 SELECT CASE WHEN COUNT(*) = 0 THEN 0 ELSE 1 END
                 FROM information_schema.tables
                 WHERE table_type = 'BASE TABLE' AND table_schema NOT IN ('pg_catalog', 'information_schema')
               ";

        private IEnumerable<SqlBatch> CreateCreateOperations()
            => _sqlGenerator.Generate(new[] { new CreateDatabaseOperation { Name = _connection.DbConnection.Database } });

        public override bool Exists()
            => Exists(retryOnNotExists: false);

        private bool Exists(bool retryOnNotExists)
        {
            var retryCount = 0;
            while (true)
            {
                try
                {
                    _connection.Open();
                    _connection.Close();
                    return true;
                }
                catch (SqlException e)
                {
                    if (!retryOnNotExists
                        && IsDoesNotExist(e))
                    {
                        return false;
                    }

                    if (!RetryOnExistsFailure(e, ref retryCount))
                    {
                        throw;
                    }
                }
            }
        }

        public override Task<bool> ExistsAsync(CancellationToken cancellationToken = default(CancellationToken))
            => ExistsAsync(retryOnNotExists: false, cancellationToken: cancellationToken);

        private async Task<bool> ExistsAsync(bool retryOnNotExists, CancellationToken cancellationToken)
        {
            var retryCount = 0;
            while (true)
            {
                try
                {
                    await _connection.OpenAsync(cancellationToken).WithCurrentCulture();
                    _connection.Close();
                    return true;
                }
                catch (SqlException e)
                {
                    if (!retryOnNotExists
                        && IsDoesNotExist(e))
                    {
                        return false;
                    }

                    if (!RetryOnExistsFailure(e, ref retryCount))
                    {
                        throw;
                    }
                }
            }
        }

        // Login failed is thrown when database does not exist (See Issue #776)
        private static bool IsDoesNotExist(SqlException exception) => exception.Number == 4060;

        // See Issue #985
        private bool RetryOnExistsFailure(SqlException exception, ref int retryCount)
        {
            // This is to handle the case where Open throws (Number 233):
            //   System.Data.SqlClient.SqlException: A connection was successfully established with the
            //   server, but then an error occurred during the login process. (provider: Named Pipes
            //   Provider, error: 0 - No process is on the other end of the pipe.)
            // It appears that this happens when the database has just been created but has not yet finished
            // opening or is auto-closing when using the AUTO_CLOSE option. The workaround is to flush the pool
            // for the connection and then retry the Open call.
            // Also handling (Number -2):
            //   System.Data.SqlClient.SqlException: Connection Timeout Expired.  The timeout period elapsed while
            //   attempting to consume the pre-login handshake acknowledgement.  This could be because the pre-login
            //   handshake failed or the server was unable to respond back in time.
            // And (Number 4060):
            //   System.Data.SqlClient.SqlException: Cannot open database "X" requested by the login. The
            //   login failed.
            if ((exception.Number == 233 || exception.Number == -2 || exception.Number == 4060)
                && ++retryCount < 30)
            {
                ClearPool();
                Thread.Sleep(100);
                return true;
            }
            return false;
        }

        public override void Delete()
        {
            ClearAllPools();

            using (var masterConnection = _connection.CreateMasterConnection())
            {
                _statementExecutor.ExecuteNonQuery(masterConnection, null, CreateDropCommands());
            }
        }

        public override async Task DeleteAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            ClearAllPools();

            using (var masterConnection = _connection.CreateMasterConnection())
            {
                await _statementExecutor
                    .ExecuteNonQueryAsync(masterConnection, null, CreateDropCommands(), cancellationToken)
                    .WithCurrentCulture();
            }
        }

        private IEnumerable<SqlBatch> CreateDropCommands()
        {
            var operations = new MigrationOperation[]
                {
                    // TODO Check DbConnection.Database always gives us what we want
                    // Issue #775
                    new DropDatabaseOperation { Name = _connection.DbConnection.Database }
                };

            var masterCommands = _sqlGenerator.Generate(operations);
            return masterCommands;
        }

        // Clear connection pools in case there are active connections that are pooled
        private static void ClearAllPools() => SqlConnection.ClearAllPools();

        // Clear connection pool for the database connection since after the 'create database' call, a previously
        // invalid connection may now be valid.
        private void ClearPool() => SqlConnection.ClearPool((SqlConnection)_connection.DbConnection);
    }
}
