// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.Data.Entity.ChangeTracking.Internal;
using Microsoft.Data.Entity.Metadata;
using Microsoft.Data.Entity.Metadata.Internal;
using Microsoft.Data.Entity.Relational;
using Microsoft.Data.Entity.Relational.Query;
using Microsoft.Framework.Logging;

namespace Npgsql.EntityFramework7.Query
{
    public class NpgsqlQueryContextFactory : RelationalQueryContextFactory, INpgsqlQueryContextFactory
    {
        public NpgsqlQueryContextFactory(
            [NotNull] IStateManager stateManager,
            [NotNull] IEntityKeyFactorySource entityKeyFactorySource,
            [NotNull] IClrCollectionAccessorSource collectionAccessorSource,
            [NotNull] IClrAccessorSource<IClrPropertySetter> propertySetterSource,
            [NotNull] INpgsqlEFConnection connection,
            [NotNull] ILoggerFactory loggerFactory)
            : base(
                  stateManager,
                  entityKeyFactorySource,
                  collectionAccessorSource,
                  propertySetterSource,
                  connection,
                  loggerFactory)
        {
        }
    }
}
