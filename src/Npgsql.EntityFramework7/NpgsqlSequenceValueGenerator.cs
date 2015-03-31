// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using JetBrains.Annotations;
using Microsoft.Data.Entity.Relational;
using Microsoft.Data.Entity.Utilities;
using Microsoft.Data.Entity.ValueGeneration;

namespace Npgsql.EntityFramework7
{
    public class NpgsqlSequenceValueGenerator<TValue> : HiLoValueGenerator<TValue>
    {
        private readonly SqlStatementExecutor _executor;
        private readonly INpgsqlConnection _connection;
        private readonly string _sequenceName;

        public NpgsqlSequenceValueGenerator(
            [NotNull] SqlStatementExecutor executor,
            [NotNull] NpgsqlSequenceValueGeneratorState generatorState,
            [NotNull] INpgsqlConnection connection)
            : base(generatorState)
        {
            Check.NotNull(executor, nameof(executor));
            Check.NotNull(generatorState, nameof(generatorState));
            Check.NotNull(connection, nameof(connection));

            _sequenceName = generatorState.SequenceName;
            _executor = executor;
            _connection = connection;
        }

        protected override long GetNewLowValue()
        {
            // TODO: Parameterize query and/or delimit identifier without using NpgsqlMigrationOperationSqlGenerator
            var sql = string.Format(CultureInfo.InvariantCulture, "SELECT NEXT VALUE FOR {0}", _sequenceName);
            var nextValue = _executor.ExecuteScalar(_connection, _connection.DbTransaction, sql);

            return (long)Convert.ChangeType(nextValue, typeof(long), CultureInfo.InvariantCulture);
        }

        public override bool GeneratesTemporaryValues => false;
    }
}
