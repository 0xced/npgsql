// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Data.Entity;
using Microsoft.Data.Entity.Storage;

namespace Npgsql.EntityFramework7
{
    public class NpgsqlDataStoreSource : DataStoreSource<NpgsqlDataStore, INpgsqlDataStoreServices, NpgsqlOptionsExtension>
    {
        public override void AutoConfigure(DbContextOptionsBuilder optionsBuilder)
        {
        }
    }
}
