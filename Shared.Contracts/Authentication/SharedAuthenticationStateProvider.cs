using System;
using System.Security.Claims;
using Microsoft.JSInterop;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Shared.Contracts.Communication;


namespace Shared.Contracts.Authentication;

public class SharedAuthenticationStateProvider : AuthenticationStateProvider
{
    private const string AUTH_TOKEN_KEY = "mf_auth_token";
    private const string USER_INFO_KEY = "mf_user_info";

    private readonly IEventAggregator _eventAggregator;
    private readonly IJSRuntime _jsRuntime;
    private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

    public event Action<string?>? OnAuthenticationStateChanged;

    public SharedAuthenticationStateProvider(
        IEventAggregator eventAggregator,
        IJSRuntime jsRuntime)
    {
        _eventAggregator = eventAggregator;
        _jsRuntime = jsRuntime;

        // Suscribirse a eventos de autenticación de otros módulos
        _eventAggregator.Subscribe<AuthenticationChangedEvent>(OnAuthenticationChanged);
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Intentar obtener el token del localStorage
        var token = await GetTokenFromStorageAsync();

        if (string.IsNullOrEmpty(token))
        {
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            return new AuthenticationState(_currentUser);
        }

        try
        {
            // Obtener información del usuario del localStorage
            var userInfo = await GetUserInfoFromStorageAsync();

            if (userInfo != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userInfo.Username),
                    new Claim(ClaimTypes.Email, userInfo.Email),
                    new Claim("user_id", userInfo.UserId),
                    new Claim("token", token)
                };

                // Agregar roles
                foreach (var role in userInfo.Roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var identity = new ClaimsIdentity(claims, "jwt");
                _currentUser = new ClaimsPrincipal(identity);

                return new AuthenticationState(_currentUser);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error loading authentication state: {ex.Message}");
        }

        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        return new AuthenticationState(_currentUser);
    }

    /// <summary>
    /// Marca al usuario como autenticado y guarda el token
    /// </summary>
    public async Task MarkUserAsAuthenticatedAsync(string token, UserInfo userInfo)
    {
        // Guardar en localStorage via JSInterop
        await SaveToStorageAsync(AUTH_TOKEN_KEY, token);
        await SaveToStorageAsync(USER_INFO_KEY, JsonSerializer.Serialize(userInfo));

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, userInfo.Username),
            new Claim(ClaimTypes.Email, userInfo.Email),
            new Claim("user_id", userInfo.UserId),
            new Claim("token", token)
        };

        foreach (var role in userInfo.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, "jwt");
        _currentUser = new ClaimsPrincipal(identity);

        // Notificar cambio de autenticación a todos los módulos
        var authEvent = new AuthenticationChangedEvent(token, userInfo, true);
        await _eventAggregator.PublishAsync(authEvent);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
        OnAuthenticationStateChanged?.Invoke(token);
    }

    /// <summary>
    /// Marca al usuario como no autenticado y limpia el token
    /// </summary>
    public async Task MarkUserAsLoggedOutAsync()
    {
        await RemoveFromStorageAsync(AUTH_TOKEN_KEY);
        await RemoveFromStorageAsync(USER_INFO_KEY);

        _currentUser = new ClaimsPrincipal(new ClaimsIdentity());

        // Notificar cierre de sesión a todos los módulos
        var authEvent = new AuthenticationChangedEvent(null, null, false);
        await _eventAggregator.PublishAsync(authEvent);

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
        OnAuthenticationStateChanged?.Invoke(null);
    }

    /// <summary>
    /// Obtiene el token actual
    /// </summary>
    public async Task<string?> GetTokenAsync()
    {
        return await GetTokenFromStorageAsync();
    }

    /// <summary>
    /// Obtiene la información del usuario actual
    /// </summary>
    public async Task<UserInfo?> GetUserInfoAsync()
    {
        return await GetUserInfoFromStorageAsync();
    }

    /// <summary>
    /// Verifica si el usuario está autenticado
    /// </summary>
    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetTokenAsync();
        return !string.IsNullOrEmpty(token);
    }

    // Manejador de eventos de autenticación de otros módulos
    private async void OnAuthenticationChanged(AuthenticationChangedEvent evt)
    {
        Console.WriteLine($"🔐 Authentication state changed from another module: Authenticated={evt.IsAuthenticated}");

        if (evt.IsAuthenticated && !string.IsNullOrEmpty(evt.Token) && evt.UserInfo != null)
        {
            // Actualizar estado local sin publicar evento (evitar loop infinito)
            await SaveToStorageAsync(AUTH_TOKEN_KEY, evt.Token);
            await SaveToStorageAsync(USER_INFO_KEY, JsonSerializer.Serialize(evt.UserInfo));

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, evt.UserInfo.Username),
                new Claim(ClaimTypes.Email, evt.UserInfo.Email),
                new Claim("user_id", evt.UserInfo.UserId),
                new Claim("token", evt.Token)
            };

            foreach (var role in evt.UserInfo.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, "jwt");
            _currentUser = new ClaimsPrincipal(identity);
        }
        else
        {
            // Usuario desautenticado
            await RemoveFromStorageAsync(AUTH_TOKEN_KEY);
            await RemoveFromStorageAsync(USER_INFO_KEY);
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
        }

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
    }

    #region localStorage via JSInterop

    private async Task<string?> GetTokenFromStorageAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", AUTH_TOKEN_KEY);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error reading token from localStorage: {ex.Message}");
            return null;
        }
    }

    private async Task<UserInfo?> GetUserInfoFromStorageAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", USER_INFO_KEY);

            if (!string.IsNullOrEmpty(json))
            {
                return JsonSerializer.Deserialize<UserInfo>(json);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error reading user info from localStorage: {ex.Message}");
        }

        return null;
    }

    private async Task SaveToStorageAsync(string key, string value)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, value);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error saving to localStorage: {ex.Message}");
        }
    }

    private async Task RemoveFromStorageAsync(string key)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error removing from localStorage: {ex.Message}");
        }
    }

    #endregion
}



public class UserInfo
{
    public required string UserId { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public List<string> Roles { get; set; } = new();
    public Dictionary<string, string> AdditionalClaims { get; set; } = new();
}


public class AuthenticationChangedEvent
{
    public string? Token { get; }
    public UserInfo? UserInfo { get; }
    public bool IsAuthenticated { get; }
    public DateTime Timestamp { get; }

    public AuthenticationChangedEvent(string? token, UserInfo? userInfo, bool isAuthenticated)
    {
        Token = token;
        UserInfo = userInfo;
        IsAuthenticated = isAuthenticated;
        Timestamp = DateTime.UtcNow;
    }
}
