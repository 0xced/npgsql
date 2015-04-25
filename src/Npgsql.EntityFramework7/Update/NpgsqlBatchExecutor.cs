// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using JetBrains.Annotations;
using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Relational.Update;
using Microsoft.Framework.Logging;

namespace Npgsql.EntityFramework7.Update
{
    public class NpgsqlBatchExecutor : BatchExecutor, INpgsqlBatchExecutor
    {
        public NpgsqlBatchExecutor(
            [NotNull] INpgsqlTypeMapper typeMapper,
            [NotNull] DbContext context,
            [NotNull] ILoggerFactory loggerFactory)
            : base(typeMapper, context, loggerFactory)
        {
        }
    }
}
