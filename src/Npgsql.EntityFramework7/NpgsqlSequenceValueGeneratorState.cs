// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.Data.Entity.Utilities;
using Microsoft.Data.Entity.ValueGeneration;

namespace Npgsql.EntityFramework7
{
    public class NpgsqlSequenceValueGeneratorState : HiLoValueGeneratorState
    {
        public NpgsqlSequenceValueGeneratorState([NotNull] string sequenceName, int blockSize, int poolSize)
            : base(blockSize, poolSize)
        {
            Check.NotEmpty(sequenceName, nameof(sequenceName));

            SequenceName = sequenceName;
        }

        public virtual string SequenceName { get; }
    }
}
