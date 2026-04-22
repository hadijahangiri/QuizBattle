using System.Net.Http.Json;
using QuizBattle.Application.DTOs;

namespace QuizBattle.AdminPanel.Services;

public interface IApiClient
{
    // Users
    Task<List<UserDto>> GetAllUsersAsync();
    Task<UserDto?> GetUserByIdAsync(int id);
    
    // Categories
    Task<List<CategoryDto>> GetAllCategoriesAsync();
    Task<CategoryDto?> GetCategoryByIdAsync(int id);
    Task<CategoryDto?> CreateCategoryAsync(CreateCategoryDto dto);
    Task<CategoryDto?> UpdateCategoryAsync(int id, CreateCategoryDto dto);
    Task<bool> DeleteCategoryAsync(int id);
    
    // Questions
    Task<List<QuestionWithCorrectAnswerDto>> GetAllQuestionsAsync(int page = 1, int pageSize = 50);
    Task<QuestionWithCorrectAnswerDto?> GetQuestionByIdAsync(int id);
    Task<QuestionWithCorrectAnswerDto?> CreateQuestionAsync(CreateQuestionDto dto);
    Task<QuestionWithCorrectAnswerDto?> UpdateQuestionAsync(int id, CreateQuestionDto dto);
    Task<bool> DeleteQuestionAsync(int id);
    
    // Store
    Task<List<StoreItemDto>> GetAllStoreItemsAsync();
    Task<StoreItemDto?> GetStoreItemByIdAsync(int id);
    Task<StoreItemDto?> CreateStoreItemAsync(CreateStoreItemDto dto);
    Task<StoreItemDto?> UpdateStoreItemAsync(int id, CreateStoreItemDto dto);
    Task<bool> DeleteStoreItemAsync(int id);
    Task<bool> ToggleStoreItemActiveAsync(int id);
}

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    #region Users

    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        try
        {
            var users = await _httpClient.GetFromJsonAsync<List<UserDto>>("users");
            return users ?? new List<UserDto>();
        }
        catch (Exception)
        {
            return new List<UserDto>();
        }
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<UserDto>($"users/{id}");
        }
        catch
        {
            return null;
        }
    }

    #endregion

    #region Categories

    public async Task<List<CategoryDto>> GetAllCategoriesAsync()
    {
        try
        {
            var categories = await _httpClient.GetFromJsonAsync<List<CategoryDto>>("categories");
            return categories ?? new List<CategoryDto>();
        }
        catch (Exception)
        {
            return new List<CategoryDto>();
        }
    }

    public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<CategoryDto>($"categories/{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<CategoryDto?> CreateCategoryAsync(CreateCategoryDto dto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("categories", dto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CategoryDto>();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<CategoryDto?> UpdateCategoryAsync(int id, CreateCategoryDto dto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"categories/{id}", dto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<CategoryDto>();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"categories/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Questions

    public async Task<List<QuestionWithCorrectAnswerDto>> GetAllQuestionsAsync(int page = 1, int pageSize = 50)
    {
        try
        {
            var questions = await _httpClient.GetFromJsonAsync<List<QuestionWithCorrectAnswerDto>>($"questions?page={page}&pageSize={pageSize}");
            return questions ?? new List<QuestionWithCorrectAnswerDto>();
        }
        catch (Exception)
        {
            return new List<QuestionWithCorrectAnswerDto>();
        }
    }

    public async Task<QuestionWithCorrectAnswerDto?> GetQuestionByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<QuestionWithCorrectAnswerDto>($"questions/{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<QuestionWithCorrectAnswerDto?> CreateQuestionAsync(CreateQuestionDto dto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("questions", dto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<QuestionWithCorrectAnswerDto>();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<QuestionWithCorrectAnswerDto?> UpdateQuestionAsync(int id, CreateQuestionDto dto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"questions/{id}", dto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<QuestionWithCorrectAnswerDto>();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteQuestionAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"questions/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Store

    public async Task<List<StoreItemDto>> GetAllStoreItemsAsync()
    {
        try
        {
            var items = await _httpClient.GetFromJsonAsync<List<StoreItemDto>>("store");
            return items ?? new List<StoreItemDto>();
        }
        catch (Exception)
        {
            return new List<StoreItemDto>();
        }
    }

    public async Task<StoreItemDto?> GetStoreItemByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<StoreItemDto>($"store/{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<StoreItemDto?> CreateStoreItemAsync(CreateStoreItemDto dto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("store", dto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<StoreItemDto>();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<StoreItemDto?> UpdateStoreItemAsync(int id, CreateStoreItemDto dto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"store/{id}", dto);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<StoreItemDto>();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteStoreItemAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"store/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ToggleStoreItemActiveAsync(int id)
    {
        try
        {
            var response = await _httpClient.PostAsync($"store/{id}/toggle-active", null);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    #endregion
}
