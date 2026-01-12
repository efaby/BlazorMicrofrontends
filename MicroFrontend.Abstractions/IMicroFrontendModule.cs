using System;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace MicroFrontend.Abstractions;

public interface IMicroFrontendModule
{
    string ModuleName { get; }
    string Version { get; }

    string Description { get; }
    string Icon { get; }

    string BasePath { get; }

    void ConfigureServices(IServiceCollection services);
    IEnumerable<RouteDefinition> GetRoutes();

    Type RootComponent { get; }
}


public class RouteDefinition
{
    public required string Path { get; init; }
    public required Type ComponentType { get; init; }
    public string? DisplayName { get; init; }
    public bool ShowInMenu { get; init; } = true;
    public Dictionary<string, object>? Parameters { get; init; }
}


public class ModuleMetadata
{
    public required string Name { get; init; }
    public required string Version { get; init; }
    public required string AssemblyName { get; init; }
    public required string RemoteUrl { get; init; }
    public List<string> Dependencies { get; init; } = new();
    public bool IsLoaded { get; set; }
    public DateTime? LoadedAt { get; set; }
}

public class ModuleLoadResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public IMicroFrontendModule? Module { get; init; }
    public TimeSpan LoadTime { get; init; }

    public static ModuleLoadResult SuccessResult(IMicroFrontendModule module, TimeSpan loadTime)
    {
        return new ModuleLoadResult
        {
            Success = true,
            Module = module,
            LoadTime = loadTime
        };
    }

    public static ModuleLoadResult FailureResult(string errorMessage)
    {
        return new ModuleLoadResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}

public abstract class MicroFrontendModuleBase : ComponentBase, IMicroFrontendModule
{
    public abstract string ModuleName { get; }
    public abstract string Version { get; }
    public abstract string Description { get; }
    public abstract string Icon { get; }
    public abstract string BasePath { get; }
    public abstract Type RootComponent { get; }

    public virtual void ConfigureServices(IServiceCollection services)
    {
        // Implementación por defecto - puede ser sobrescrita
    }

    public abstract IEnumerable<RouteDefinition> GetRoutes();
}


public record ModuleLoadedEvent(string ModuleName, DateTime LoadedAt);
public record ModuleUnloadedEvent(string ModuleName, DateTime UnloadedAt);
public record ModuleErrorEvent(string ModuleName, string Error, DateTime OccurredAt);
