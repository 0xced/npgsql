﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using JetBrains.Annotations;
using Microsoft.Data.Entity.Relational.Metadata;

namespace EntityFramework7.Npgsql.Metadata
{
    public interface INpgsqlPropertyExtensions : IRelationalPropertyExtensions
    {
        [CanBeNull]
        NpgsqlValueGenerationStrategy? ValueGenerationStrategy { get; }

        [CanBeNull]
        string ComputedExpression { get; }

        [CanBeNull]
        string SequenceName { get; }

        [CanBeNull]
        string SequenceSchema { get; }

        [CanBeNull]
        Sequence TryGetSequence();
    }
}
