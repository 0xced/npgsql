using System;
using DotNet.Testcontainers.Images;

namespace Npgsql.PluginTests;

public class AssemblySetUp : global::AssemblySetUp
{
    protected override DockerImage ContainerImage
    {
        get
        {
            // Form native arm64 support, use imresamu/postgis, see https://github.com/postgis/docker-postgis/issues/216#issuecomment-2936824962
            // For example, imresamu/postgis:18-3.6.1-alpine3.23
            var imageName = Environment.GetEnvironmentVariable("NPGSQL_TEST_DOCKER_IMAGE_POSTGIS");
            if (imageName != null)
            {
                return new DockerImage(imageName);
            }

            // Explicit linux/amd64 platform since arm64 is not yet supported, see https://github.com/postgis/docker-postgis/issues/216
            return new DockerImage("postgis/postgis:18-3.6", new Platform("linux/amd64"));
        }
    }

    protected override string? ContainerName => typeof(AssemblySetUp).Assembly.GetName().Name;
}
