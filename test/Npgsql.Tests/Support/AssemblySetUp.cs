using Npgsql;
using Npgsql.Tests;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using DotNet.Testcontainers.Images;
using Testcontainers.PostgreSql;

[SetUpFixture]
public class AssemblySetUp
{
    PostgreSqlContainer? _postgreSqlContainer;

    protected virtual DockerImage ContainerImage => new(Environment.GetEnvironmentVariable("NPGSQL_TEST_DOCKER_IMAGE") ?? "postgres:18");

    protected virtual string? ContainerName => typeof(AssemblySetUp).Assembly.GetName().Name;

    [OneTimeSetUp]
    public async Task Setup()
    {
        try
        {
            CheckConnection();
        }
        catch when (Environment.GetEnvironmentVariable("NPGSQL_TEST_DB") == null)
        {
            // Connection to the default connection string failed, use Docker to run PostgreSQL
            _postgreSqlContainer = new PostgreSqlBuilder(ContainerImage).WithName(ContainerName).Build();
            await _postgreSqlContainer.StartAsync();
            if (ContainerImage.Repository == "postgres")
            {
                await EnableSslAsync(_postgreSqlContainer);
            }

            TestUtil.ConnectionString = _postgreSqlContainer.GetConnectionString();
            CheckConnection();
        }
    }

    static async Task EnableSslAsync(PostgreSqlContainer container)
    {
        const string serverCrt = "/var/lib/postgresql/server.crt";
        const string serverKey = "/var/lib/postgresql/server.key";
        const string configureSsl = $"""
                                     ALTER SYSTEM SET ssl = 'on';
                                     ALTER SYSTEM SET ssl_cert_file = '{serverCrt}';
                                     ALTER SYSTEM SET ssl_key_file = '{serverKey}';
                                     """;

        await container.ExecAsync(["openssl", "req", "-new", "-x509", "-days", "365", "-nodes", "-text", "-out", serverCrt, "-keyout", serverKey, "-subj", "/CN=localhost"]);
        await container.ExecAsync(["chown", "postgres:postgres", serverCrt, serverKey]);
        await container.ExecAsync(["chmod", "600", serverKey]);
        await container.ExecAsync(["chmod", "644", serverCrt]);
        await container.ExecScriptAsync(configureSsl);

        await container.StopAsync();
        await container.StartAsync();
    }

    static void CheckConnection()
    {
        var connString = TestUtil.ConnectionString;
        using var conn = new NpgsqlConnection(connString);
        try
        {
            conn.Open();
        }
        catch (PostgresException e)
        {
            if (e.SqlState == PostgresErrorCodes.InvalidPassword && connString == TestUtil.DefaultConnectionString)
                throw new Exception("Please create a user npgsql_tests as follows: CREATE USER npgsql_tests PASSWORD 'npgsql_tests' SUPERUSER");

            if (e.SqlState == PostgresErrorCodes.InvalidCatalogName)
            {
                var builder = new NpgsqlConnectionStringBuilder(connString)
                {
                    Pooling = false,
                    Database = "postgres"
                };

                using var adminConn = new NpgsqlConnection(builder.ConnectionString);
                adminConn.Open();
                adminConn.ExecuteNonQuery("CREATE DATABASE " + conn.Database);
                adminConn.Close();
                Thread.Sleep(1000);

                conn.Open();
                return;
            }

            throw;
        }
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        if (_postgreSqlContainer != null)
        {
            await _postgreSqlContainer.DisposeAsync();
        }
    }
}
