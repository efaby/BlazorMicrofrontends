using System.Reflection;
using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using MicroFrontend.Abstractions;

namespace Shell.App.Services;

public class ModuleFederationManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, IMicroFrontendModule> _loadedModules = new();
    private readonly Dictionary<string, ModuleMetadata> _moduleRegistry = new();
    private readonly SemaphoreSlim _loadingSemaphore = new(1, 1);

    public event Action<ModuleLoadedEvent>? OnModuleLoaded;
    public event Action<ModuleErrorEvent>? OnModuleError;

    public ModuleFederationManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;

        InitializeRegistry();
    }

    private void InitializeRegistry()
    {
        _moduleRegistry["products"] = new ModuleMetadata
        {
            Name = "products",
            Version = "1.0.0",
            AssemblyName = "Products.MicroFrontend",
            RemoteUrl = "http://localhost:5002/products"
        };

        _moduleRegistry["customers"] = new ModuleMetadata
        {
            Name = "customers",
            Version = "1.0.0",
            AssemblyName = "Customers.MicroFrontend",
            RemoteUrl = "http://localhost:5003/customers"
        };

        _moduleRegistry["orders"] = new ModuleMetadata
        {
            Name = "orders",
            Version = "1.0.0",
            AssemblyName = "Orders.MicroFrontend",
            RemoteUrl = "http://localhost:5004/orders"
        };
    }

    public async Task<ModuleLoadResult> LoadModuleAsync(string moduleName)
    {
        await _loadingSemaphore.WaitAsync();

        try
        {
            var stopwatch = Stopwatch.StartNew();

            Console.WriteLine($"🔄 Starting to load module: {moduleName}");

            if (_loadedModules.ContainsKey(moduleName))
            {
                Console.WriteLine($"✅ MModule {moduleName} is already loaded (cache)");
                return ModuleLoadResult.SuccessResult(_loadedModules[moduleName], stopwatch.Elapsed);
            }

            if (!_moduleRegistry.TryGetValue(moduleName, out var metadata))
            {
                var error = $"MModule '{moduleName}' not found in registry";
                Console.Error.WriteLine($"❌ {error}");
                OnModuleError?.Invoke(new ModuleErrorEvent(moduleName, error, DateTime.Now));
                return ModuleLoadResult.FailureResult(error);
            }

            try
            {
                // Method 1: Try to load the assembly if it is in the same domain

                var assembly = await TryLoadAssemblyAsync(metadata);

                if (assembly != null)
                {
                    var module = await ActivateModuleAsync(assembly, metadata);

                    if (module != null)
                    {
                        _loadedModules[moduleName] = module;
                        metadata.IsLoaded = true;
                        metadata.LoadedAt = DateTime.Now;

                        stopwatch.Stop();
                        Console.WriteLine($"✅ MModule {moduleName} loaded successfully in {stopwatch.ElapsedMilliseconds}ms");

                        OnModuleLoaded?.Invoke(new ModuleLoadedEvent(moduleName, DateTime.Now));

                        return ModuleLoadResult.SuccessResult(module, stopwatch.Elapsed);
                    }
                }

                // Method 2: Remote loading via iframe (fallback)
                Console.WriteLine($"⚠️ Using remote loading for {moduleName}");
                var remoteModule = CreateRemoteModule(metadata);
                _loadedModules[moduleName] = remoteModule;

                stopwatch.Stop();
                return ModuleLoadResult.SuccessResult(remoteModule, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                var error = $"Error loading module: {ex.Message}";
                Console.Error.WriteLine($"❌ {error}");
                Console.Error.WriteLine(ex.StackTrace);

                OnModuleError?.Invoke(new ModuleErrorEvent(moduleName, error, DateTime.Now));
                return ModuleLoadResult.FailureResult(error);
            }
        }
        finally
        {
            _loadingSemaphore.Release();
        }
    }

    private async Task<Assembly?> TryLoadAssemblyAsync(ModuleMetadata metadata)
    {
        try
        {
            // Try to load from already loaded assemblies
            var loadedAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == metadata.AssemblyName);

            if (loadedAssembly != null)
            {
                Console.WriteLine($"📦 Assembly {metadata.AssemblyName} found in current domain");
                return loadedAssembly;
            }

            // Try dynamic loading (requires DLLs to be available)
            Console.WriteLine($"📥 Attempting to load assembly: {metadata.AssemblyName}");

            // In production, this would require downloading the DLL from the remote server
            // For now, we only try to load it if it is available locally
            return Assembly.Load(metadata.AssemblyName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Could not load assembly locally: {ex.Message}");
            return null;
        }
    }

    private async Task<IMicroFrontendModule?> ActivateModuleAsync(Assembly assembly, ModuleMetadata metadata)
    {
        try
        {
            // Search for types that implement IMicroFrontendModule
            var moduleType = assembly.GetTypes()
                .FirstOrDefault(t => typeof(IMicroFrontendModule).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            if (moduleType == null)
            {
                Console.WriteLine($"⚠️ No implementation of IMicroFrontendModule found in {assembly.FullName}");
                return null;
            }

            Console.WriteLine($"🎯 Activating module: {moduleType.Name}");

            // Create an instance of the module
            var module = Activator.CreateInstance(moduleType) as IMicroFrontendModule;

            if (module != null)
            {
                Console.WriteLine($"✅ MModule activated: {module.ModuleName} v{module.Version}");
                return module;
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"❌ Error activating module: {ex.Message}");
            return null;
        }
    }

    private IMicroFrontendModule CreateRemoteModule(ModuleMetadata metadata)
    {
        // Create a proxy/wrapper for remote modules
        return new RemoteMicroFrontendModule(metadata);
    }

    /// <summary>
    /// Gets a loaded module
    /// </summary>
    public IMicroFrontendModule? GetModule(string moduleName)
    {
        return _loadedModules.TryGetValue(moduleName, out var module) ? module : null;
    }

    /// <summary>
    /// Gets all loaded modules
    /// </summary>
    public IReadOnlyDictionary<string, IMicroFrontendModule> GetLoadedModules()
    {
        return _loadedModules;
    }

    /// <summary>
    /// Gets all available modules in the registry
    /// </summary>
    public IReadOnlyDictionary<string, ModuleMetadata> GetAvailableModules()
    {
        return _moduleRegistry;
    }

    /// <summary>
    /// Unloads a module from memory
    /// </summary>
    public bool UnloadModule(string moduleName)
    {
        if (_loadedModules.Remove(moduleName))
        {
            if (_moduleRegistry.TryGetValue(moduleName, out var metadata))
            {
                metadata.IsLoaded = false;
                metadata.LoadedAt = null;
            }

            Console.WriteLine($"🗑️ MModule {moduleName} unloaded");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Preloads all modules
    /// </summary>
    public async Task PreloadAllModulesAsync()
    {
        Console.WriteLine("🚀 Preloading all modules...");
        var tasks = _moduleRegistry.Keys.Select(LoadModuleAsync).ToList();
        var results = await Task.WhenAll(tasks);

        var successful = results.Count(r => r.Success);
        var failed = results.Length - successful;

        Console.WriteLine($"✅ Preload complete: {successful} successful, {failed} failed");
    }
}

/// <summary>
/// Implementation of a remote module (loaded via iframe)
/// </summary>
internal class RemoteMicroFrontendModule : IMicroFrontendModule
{
    private readonly ModuleMetadata _metadata;

    public RemoteMicroFrontendModule(ModuleMetadata metadata)
    {
        _metadata = metadata;
    }

    public string ModuleName => _metadata.Name;
    public string Version => _metadata.Version;
    public string Description => $"Remote module: {_metadata.Name}";
    public string Icon => "bi-cloud-download";
    public string BasePath => $"/{_metadata.Name}";
    public Type RootComponent => typeof(RemoteModuleHost);

    public void ConfigureServices(IServiceCollection services)
    {
        // The services of the remote module are not loaded here
    }

    public IEnumerable<RouteDefinition> GetRoutes()
    {
        return new[]
        {
            new RouteDefinition
            {
                Path = $"{BasePath}/{{*path}}",
                ComponentType = typeof(RemoteModuleHost),
                DisplayName = ModuleName,
                Parameters = new Dictionary<string, object>
                {
                    ["RemoteUrl"] = _metadata.RemoteUrl
                }
            }
        };
    }
}


public class RemoteModuleHost : ComponentBase
{
    [Parameter]
    public string? RemoteUrl { get; set; }

    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "remote-module-container");

        builder.OpenElement(2, "iframe");
        builder.AddAttribute(3, "src", RemoteUrl);
        builder.AddAttribute(4, "style", "width: 100%; min-height: 500px; border: none;");
        builder.CloseElement();

        builder.CloseElement();
    }
}