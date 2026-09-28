using Microsoft.AspNetCore.Components;
using StockReplenishment.DTOs;
using StockReplenishment.Models;
using System.Net;
using System.Net.Http.Json;

namespace StockReplenishment.Services;

public class ReplenishmentApiClient
{
    private readonly HttpClient _httpClient;

    //public ReplenishmentApiClient(HttpClient httpClient)
    //{
    //    _httpClient = httpClient;
    //}
    public ReplenishmentApiClient(HttpClient httpClient,NavigationManager navigationManager)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(navigationManager.BaseUri);
    }

    public async Task<PagedResultDto<RequestResponseDto>?> GetRequestsAsync(
        RequestStatus? status = null,
        RequestPriority? priority = null,
        int? locationId = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>
        {
            $"page={page}",
            $"pageSize={pageSize}"
        };

        if (status.HasValue) query.Add($"status={status.Value}");
        if (priority.HasValue) query.Add($"priority={priority.Value}");
        if (locationId.HasValue) query.Add($"locationId={locationId.Value}");

        return await _httpClient.GetFromJsonAsync<PagedResultDto<RequestResponseDto>>(
            $"api/replenishment-requests?{string.Join("&", query)}", cancellationToken);
    }

    public async Task<List<Location>> GetLocationsAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<List<Location>>(
            "api/locations", cancellationToken) ?? new List<Location>();
    }

    public async Task<RequestResponseDto?> GetRequestAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/replenishment-requests/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RequestResponseDto>(cancellationToken: cancellationToken);
    }

    public async Task<RequestResponseDto> CreateAsync(CreateRequestDto request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/replenishment-requests", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RequestResponseDto>(cancellationToken: cancellationToken))!;
    }

    //public async Task<RequestResponseDto> SubmitAsync(int id, CancellationToken cancellationToken = default)
    //    => await PostActionAsync($"api/replenishment-requests/{id}/submit", null, cancellationToken);

    //public async Task<RequestResponseDto> ApproveAsync(int id, CancellationToken cancellationToken = default)
    //    => await PostActionAsync($"api/replenishment-requests/{id}/approve", null, cancellationToken);
    public async Task<RequestResponseDto> SubmitAsync(int id,CancellationToken cancellationToken = default)
    {
        return await PostActionAsync<RequestResponseDto>($"api/replenishment-requests/{id}/submit",cancellationToken);
    }

    public async Task<RequestResponseDto> ApproveAsync(int id,CancellationToken cancellationToken = default)
    {
        return await PostActionAsync<RequestResponseDto>($"api/replenishment-requests/{id}/approve",cancellationToken);
    }
    public async Task<RequestResponseDto> RejectAsync(int id, RejectRequestDto request, CancellationToken cancellationToken = default)
        => await PostActionAsync($"api/replenishment-requests/{id}/reject", request, cancellationToken);

    public async Task<RequestResponseDto> FulfillAsync(int id, FulfillRequestDto request, CancellationToken cancellationToken = default)
        => await PostActionAsync($"api/replenishment-requests/{id}/fulfill", request, cancellationToken);

    private async Task<RequestResponseDto> PostActionAsync<T>(string url, T? body, CancellationToken cancellationToken)
    {
        HttpResponseMessage response = body == null
            ? await _httpClient.PostAsync(url, null, cancellationToken)
            : await _httpClient.PostAsJsonAsync(url, body, cancellationToken);

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RequestResponseDto>(cancellationToken: cancellationToken))!;
    }
    private async Task<T> PostActionAsync<T>(string url,CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync(url,null,cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken))!;
    }
}
