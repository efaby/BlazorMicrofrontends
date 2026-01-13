using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Shared.Contracts.Authentication;
using Shared.Contracts.Communication;
using Shared.Contracts.Http;

namespace Shared.Contracts.Services;

/// <summary>
/// Puente de servicios compartidos entre Shell y módulos
/// Permite que los módulos accedan a servicios del Shell
/// </summary>
public class SharedServiceBridge
{
    private static SharedServiceBridge? _instance;
    private static readonly object _lock = new();

    public IEventAggregator EventAggregator { get; private set; } = null!;

    // Registro de servicios compartidos adicionales
    private readonly Dictionary<Type, object> _sharedServices = new();

    private SharedServiceBridge() { }

    /// <summary>
    /// Singleton global para compartir servicios entre Shell y módulos
    /// </summary>
    public static SharedServiceBridge Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new SharedServiceBridge();
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Inicializa el puente con los servicios del Shell
    /// </summary>
    public void Initialize(IEventAggregator eventAggregator)
    {
        EventAggregator = eventAggregator;
        Console.WriteLine("✅ SharedServiceBridge initialized");
    }

    /// <summary>
    /// Registra un servicio compartido adicional
    /// </summary>
    public void RegisterSharedService<T>(T service) where T : class
    {
        if (service == null) throw new ArgumentNullException(nameof(service));

        var serviceType = typeof(T);
        _sharedServices[serviceType] = service;

        Console.WriteLine($"📦 Shared service registered: {serviceType.Name}");
    }

    /// <summary>
    /// Obtiene un servicio compartido
    /// </summary>
    public T? GetSharedService<T>() where T : class
    {
        var serviceType = typeof(T);

        if (_sharedServices.TryGetValue(serviceType, out var service))
        {
            return service as T;
        }

        return null;
    }

    /// <summary>
    /// Verifica si un servicio está registrado
    /// </summary>
    public bool HasService<T>() where T : class
    {
        return _sharedServices.ContainsKey(typeof(T));
    }

    /// <summary>
    /// Limpia todos los servicios registrados
    /// </summary>
    public void Clear()
    {
        _sharedServices.Clear();
        Console.WriteLine("🧹 SharedServiceBridge cleared");
    }
}

/// <summary>
/// Extensiones para facilitar el uso del SharedServiceBridge
/// </summary>
public static class SharedServiceBridgeExtensions
{
    /// <summary>
    /// Configura los servicios compartidos en el Shell
    /// </summary>
    public static IServiceCollection AddSharedServiceBridge(
        this IServiceCollection services)
    {
        // EventAggregator como Singleton - compartido entre módulos
        services.AddSingleton<IEventAggregator, EventAggregator>();

        // SharedAuthenticationStateProvider como Scoped
        services.AddScoped<SharedAuthenticationStateProvider>();

        // Registrar también como AuthenticationStateProvider
        services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<SharedAuthenticationStateProvider>());

        // AuthenticatedHttpClient con factory para obtener AuthStateProvider
        services.AddScoped<AuthenticatedHttpClient>(sp =>
        {
            var httpClient = sp.GetRequiredService<HttpClient>();

            // Factory que obtiene el AuthStateProvider cuando se necesita
            Func<Task<SharedAuthenticationStateProvider>> authProviderFactory = async () =>
            {
                return sp.GetRequiredService<SharedAuthenticationStateProvider>();
            };

            return new AuthenticatedHttpClient(httpClient, authProviderFactory);
        });

        // Agregar servicios de autorización
        services.AddAuthorizationCore();

        Console.WriteLine("✅ Shared services registered in DI container");

        return services;
    }

    /// <summary>
    /// Inicializa el puente después de construir el host (en Shell)
    /// </summary>
    public static async Task InitializeSharedServiceBridgeAsync(
        this WebAssemblyHost host)
    {
        var eventAggregator = host.Services.GetRequiredService<IEventAggregator>();

        SharedServiceBridge.Instance.Initialize(eventAggregator);

        Console.WriteLine("🌉 SharedServiceBridge initialized in Shell");

        await Task.CompletedTask;
    }

    /// <summary>
    /// Configura los servicios compartidos en un módulo
    /// </summary>
    public static IServiceCollection UseSharedServices(
        this IServiceCollection services)
    {
        // EventAggregator compartido (Singleton) - obtener del Bridge
        services.AddSingleton<IEventAggregator>(sp =>
        {
            var bridge = SharedServiceBridge.Instance;
            if (bridge.EventAggregator == null)
            {
                throw new InvalidOperationException(
                    "SharedServiceBridge not initialized. Make sure the Shell is running and initialized first.");
            }
            return bridge.EventAggregator;
        });

        // SharedAuthenticationStateProvider como Scoped
        services.AddScoped<SharedAuthenticationStateProvider>();

        // Registrar también como AuthenticationStateProvider
        services.AddScoped<AuthenticationStateProvider>(sp =>
            sp.GetRequiredService<SharedAuthenticationStateProvider>());

        // AuthenticatedHttpClient con factory
        services.AddScoped<AuthenticatedHttpClient>(sp =>
        {
            var httpClient = sp.GetRequiredService<HttpClient>();

            Func<Task<SharedAuthenticationStateProvider>> authProviderFactory = async () =>
            {
                return sp.GetRequiredService<SharedAuthenticationStateProvider>();
            };

            return new AuthenticatedHttpClient(httpClient, authProviderFactory);
        });

        // Agregar servicios de autorización
        services.AddAuthorizationCore();

        Console.WriteLine("✅ Module configured to use shared services");

        return services;
    }
}

/// <summary>
/// Evento para comunicación de datos entre módulos
/// </summary>
public class CrossModuleDataEvent
{
    public string SourceModule { get; }
    public string TargetModule { get; }
    public string EventType { get; }
    public object? Data { get; }
    public DateTime Timestamp { get; }

    public CrossModuleDataEvent(
        string sourceModule,
        string targetModule,
        string eventType,
        object? data = null)
    {
        SourceModule = sourceModule;
        TargetModule = targetModule;
        EventType = eventType;
        Data = data;
        Timestamp = DateTime.UtcNow;
    }
}