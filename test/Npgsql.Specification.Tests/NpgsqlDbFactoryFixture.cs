using System;
using System.Data.Common;
using System.Threading.Tasks;
using AdoNet.Specification.Tests;
using Testcontainers.PostgreSql;
using Testcontainers.Xunit;
using Xunit.Abstractions;

namespace Npgsql.Specification.Tests;

public abstract class NpgsqlDbFactoryFixture(IMessageSink messageSink)
    : DbContainerFixture<PostgreSqlBuilder, PostgreSqlContainer>(messageSink), IDbFactoryFixture
{
    public DbProviderFactory Factory => NpgsqlFactory.Instance;

    public override DbProviderFactory DbProviderFactory => Factory;

    protected override PostgreSqlBuilder Configure() => new PostgreSqlBuilder("postgres:18").WithName(ContainerName);

    protected abstract string ContainerName { get; }

    protected override async Task InitializeAsync()
    {
        if (Environment.GetEnvironmentVariable("NPGSQL_TEST_DB") == null)
        {
            await base.InitializeAsync();
        }
    }

    public class Command(IMessageSink messageSink) : NpgsqlDbFactoryFixture(messageSink)
    {
        protected override string ContainerName => "Npgsql.Specification.Tests.NpgsqlCommandTests";
    }

    public class Connection(IMessageSink messageSink) : NpgsqlDbFactoryFixture(messageSink)
    {
        protected override string ContainerName => "Npgsql.Specification.Tests.NpgsqlConnectionTests";
    }

    public class DataReader(IMessageSink messageSink) : NpgsqlDbFactoryFixture(messageSink)
    {
        protected override string ContainerName => "Npgsql.Specification.Tests.NpgsqlDataReaderTests";
    }
}
