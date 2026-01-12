using Microsoft.AspNetCore.Components;
using MicroFrontend.Abstractions;

namespace Shell.App.Services;

/// <summary>
/// Manager for dynamic routes registered from loaded modules
/// </summary>
public class DynamicRouteManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, RouteDefinition> _routes = new();
    private ModuleFederationManager? _moduleFederation;

    public event Action? OnRoutesChanged;

    public DynamicRouteManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Initializes the manager with the ModuleFederationManager
    /// Called after construction to avoid circular dependencies
    /// </summary>
    public void Initialize()
    {
        // Get ModuleFederationManager after construction
        _moduleFederation = _serviceProvider.GetRequiredService<ModuleFederationManager>();
        // Subscribe to module load events
        _moduleFederation.OnModuleLoaded += OnModuleLoaded;

        Console.WriteLine("✅ DynamicRouteManager initialized");
    }

    private void OnModuleLoaded(ModuleLoadedEvent evt)
    {
        Console.WriteLine($"📍 Registering routes from module: {evt.ModuleName}");

        if (_moduleFederation == null)
        {
            Console.Error.WriteLine("⚠️ DynamicRouteManager is not initialized");
            return;
        }

        var module = _moduleFederation.GetModule(evt.ModuleName);
        if (module != null)
        {
            RegisterModuleRoutes(module);
        }
    }

    /// <summary>
    /// Registers the routes of a module
    /// </summary>
    public void RegisterModuleRoutes(IMicroFrontendModule module)
    {
        var routes = module.GetRoutes();
        var count = 0;

        foreach (var route in routes)
        {
            var key = $"{module.ModuleName}:{route.Path}";

            if (!_routes.ContainsKey(key))
            {
                _routes[key] = route;
                count++;
                Console.WriteLine($"  ✓ Route registered: {route.Path} → {route.ComponentType.Name}");
            }
        }

        Console.WriteLine($"✅ {count} route(s) registered for {module.ModuleName}");
        OnRoutesChanged?.Invoke();
    }

    /// <summary>
    /// Gets all registered routes
    /// </summary>
    public IReadOnlyDictionary<string, RouteDefinition> GetAllRoutes()
    {
        return _routes;
    }

    /// <summary>
    /// Gets the routes that should be shown in the menu
    /// </summary>
    public IEnumerable<RouteDefinition> GetMenuRoutes()
    {
        return _routes.Values.Where(r => r.ShowInMenu);
    }

    /// <summary>
    /// Finds a route by path
    /// </summary>
    public RouteDefinition? FindRoute(string path)
    {
        return _routes.Values.FirstOrDefault(r =>
            MatchRoute(r.Path, path));
    }

    private bool MatchRoute(string routePattern, string actualPath)
    {
        // Simple route matching implementation
        // In production, use a more robust router

        if (routePattern == actualPath)
            return true;

        // Coincidencia con wildcards
        if (routePattern.Contains("{*"))
        {
            var basePath = routePattern.Split("{*")[0].TrimEnd('/');
            return actualPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase);
        }

        // Coincidencia con parámetros
        if (routePattern.Contains("{"))
        {
            var patternParts = routePattern.Split('/');
            var pathParts = actualPath.Split('/');

            if (patternParts.Length != pathParts.Length)
                return false;

            for (int i = 0; i < patternParts.Length; i++)
            {
                if (patternParts[i].StartsWith("{") && patternParts[i].EndsWith("}"))
                    continue; // It's a parameter, matches any value

                if (!patternParts[i].Equals(pathParts[i], StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Navigates to a module route
    /// </summary>
    public void NavigateTo(string path, bool forceLoad = false)
    {
        var navigation = _serviceProvider.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(path, forceLoad);
    }

    /// <summary>
    /// Releases resources
    /// </summary>
    public void Dispose()
    {
        if (_moduleFederation != null)
        {
            _moduleFederation.OnModuleLoaded -= OnModuleLoaded;
        }
    }
}