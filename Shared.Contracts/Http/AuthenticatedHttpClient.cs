using System;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Shared.Contracts.Authentication;

namespace Shared.Contracts.Http;

public class AuthenticatedHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly Func<Task<SharedAuthenticationStateProvider>> _authProviderFactory;

    public AuthenticatedHttpClient(
        HttpClient httpClient,
        Func<Task<SharedAuthenticationStateProvider>> authProviderFactory)
    {
        _httpClient = httpClient;
        _authProviderFactory = authProviderFactory;
    }

    /// <summary>
    /// GET request con token automático
    /// </summary>
    public async Task<T?> GetAsync<T>(string requestUri)
    {
        await EnsureAuthorizationHeaderAsync();
        return await _httpClient.GetFromJsonAsync<T>(requestUri);
    }

    /// <summary>
    /// POST request con token automático
    /// </summary>
    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string requestUri, TRequest data)
    {
        await EnsureAuthorizationHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync(requestUri, data);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>();
    }

    /// <summary>
    /// PUT request con token automático
    /// </summary>
    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string requestUri, TRequest data)
    {
        await EnsureAuthorizationHeaderAsync();
        var response = await _httpClient.PutAsJsonAsync(requestUri, data);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>();
    }

    /// <summary>
    /// DELETE request con token automático
    /// </summary>
    public async Task<HttpResponseMessage> DeleteAsync(string requestUri)
    {
        await EnsureAuthorizationHeaderAsync();
        var response = await _httpClient.DeleteAsync(requestUri);
        response.EnsureSuccessStatusCode();
        return response;
    }

    /// <summary>
    /// Acceso directo al HttpClient
    /// </summary>
    public HttpClient HttpClient => _httpClient;

    /// <summary>
    /// Asegura que el header de autorización esté actualizado
    /// </summary>
    private async Task EnsureAuthorizationHeaderAsync()
    {
        try
        {
            var authProvider = await _authProviderFactory();
            var token = await authProvider.GetTokenAsync();

            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error setting authorization header: {ex.Message}");
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }
}