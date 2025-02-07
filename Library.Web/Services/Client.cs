﻿using Blazored.LocalStorage;
using Library.Common.Models;
using Library.Web.Constants;
using Library.Web.Helper;
using Library.Web.Models;
using Library.Web.Services.Interface;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace Library.Web.Services
{
    public class Client : IClient
    {
        #region field

        private const string ApplicationUrl = "https://localhost:5001/";
        public HttpClient HttpClient { get; }

        private readonly ILocalStorageService _localStorageService;

        #endregion


        #region ctor

        public Client(HttpClient httpClient, ILocalStorageService localStorageService)
        {
            HttpClient = httpClient;
            _localStorageService = localStorageService;
        }

        #endregion


        #region 封装请求

        private async Task<T> SendRequest<T>(HttpMethod method, string queryUrl, string? accessToken = null,
            string? json = null, Dictionary<string, object>? queryPairs = null)
        {
            if (queryPairs != null)
            {
                var queryParams = new StringBuilder("?");
                foreach (var (key, value) in queryPairs)
                {
                    queryParams.Append(key + "=" + value + "&");
                }

                queryParams.Remove(queryParams.Length - 1, 1);
                queryUrl += queryParams.ToString();
            }

            using var request = new HttpRequestMessage(method, queryUrl);
            if (accessToken != null)
            {
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);
            }

            if (json != null)
            {
                var content = new StringContent(json);
                request.Content = content;
                content.Headers.ContentType =
                    new MediaTypeHeaderValue("application/json");
            }

            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await HttpClient.SendAsync(request).ConfigureAwait(false);
            var headers = response.Headers.ToDictionary(h => h.Key, h => h.Value);
            foreach (var item in response.Content.Headers)
                headers[item.Key] = item.Value;

            var status = (int) response.StatusCode;
            if (status == 200)
            {
                var stringContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var objectResponse = JsonConvert.DeserializeObject<T>(stringContent);
                if (objectResponse == null)
                {
                    throw new ApiException("Response was null which was not expected.", status, stringContent, headers);
                }

                return objectResponse;
            }

            string? jsonMessage = null;
            string? responseData = null;
            if (status != 404)
            {
                responseData = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                jsonMessage = JObject.Parse(responseData).SelectToken("message")?.Value<string>();
            }

            throw new ApiException(jsonMessage ?? "服务器错误，请稍后重试！", status, responseData ?? "", headers);
        }

        private async Task<bool> SendRequest(HttpMethod method, string queryUrl, string? accessToken = null,
            string? json = null, Dictionary<string, object>? queryPairs = null)
        {
            if (queryPairs != null)
            {
                var queryParams = new StringBuilder("?");
                foreach (var (key, value) in queryPairs)
                {
                    queryParams.Append(key + "=" + value + "&");
                }

                queryParams.Remove(queryParams.Length - 1, 1);
                queryUrl += queryParams.ToString();
            }

            using var request = new HttpRequestMessage(method, queryUrl);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
            if (json != null)
            {
                var content = new StringContent(json);
                request.Content = content;
                content.Headers.ContentType =
                    new MediaTypeHeaderValue("application/json");
            }

            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await HttpClient.SendAsync(request).ConfigureAwait(false);
            var headers = response.Headers.ToDictionary(h => h.Key, h => h.Value);
            foreach (var item in response.Content.Headers)
                headers[item.Key] = item.Value;
            var status = (int) response.StatusCode;
            if (status == 200)
            {
                return true;
            }

            string? jsonMessage = null;
            string? responseData = null;
            if (status != 404)
            {
                responseData = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                jsonMessage = JObject.Parse(responseData).SelectToken("message")?.Value<string>();
            }

            throw new ApiException(jsonMessage ?? "服务器错误，请稍后重试！", status, responseData ?? "", headers);
        }

        #endregion


        #region auth

        public async Task<AuthResponse> LoginAsync(LoginUserDto body)
        {
            var apiUrl = ApplicationUrl + Apis.AuthLogin;
            return await SendRequest<AuthResponse>(HttpMethod.Post, apiUrl, json: JsonConvert.SerializeObject(body));
        }

        public async Task<bool> RegisterUserAsync(RegisterUser registerUser)
        {
            var apiUrl = ApplicationUrl + Apis.AuthRegister;
            return await SendRequest(HttpMethod.Post, apiUrl, json: JsonConvert.SerializeObject(registerUser));
        }

        #endregion

        #region Book

        public async Task<BookCreateDto> CreateBookAsync(BookCreateDto body)
        {
            var apiUrl = ApplicationUrl + Apis.CreateBook;
            var accessToken = await _localStorageService.GetAccessTokenAsync();
            return await SendRequest<BookCreateDto>(HttpMethod.Post, apiUrl, accessToken,
                JsonConvert.SerializeObject(body));
        }

        public async Task<bool> DeleteBookAsync(string id)
        {
            var apiUrl = ApplicationUrl + Apis.DeleteOrUpdateOrGetBook + id;
            var accessToken = await _localStorageService.GetAccessTokenAsync();
            return await SendRequest(HttpMethod.Delete, apiUrl, accessToken);
        }

        public async Task<bool> UpdateBookAsync(string id, BookUpdateDto bookUpdateDto)
        {
            var apiUrl = ApplicationUrl + Apis.DeleteOrUpdateOrGetBook + id;
            var accessToken = await _localStorageService.GetAccessTokenAsync();
            return await SendRequest(HttpMethod.Put, apiUrl, accessToken, JsonConvert.SerializeObject(bookUpdateDto));
        }

        public async Task<List<BookDto>> GetBooksAsync(BookQueryParameters? queryParameters)
        {
            var apiUrl = ApplicationUrl + Apis.CreateBook;
            var accessToken = await _localStorageService.GetAccessTokenAsync();
            var dict = queryParameters.ToDictionary();
            return await SendRequest<List<BookDto>>(HttpMethod.Get, apiUrl, accessToken, queryPairs: dict);
        }

        public async Task<BookDto> GetBookById(string id)
        {
            var apiUrl = ApplicationUrl + Apis.DeleteOrUpdateOrGetBook + id;
            var accessToken = await _localStorageService.GetAccessTokenAsync();
            return await SendRequest<BookDto>(HttpMethod.Get, apiUrl, accessToken);
        }

        #endregion

        #region Category

        public async Task<CategoryCreateDto> CreateCategoryAsync(CategoryCreateDto body)
        {
            var apiUrl = ApplicationUrl + Apis.CreateCategory;
            var accessToken = await _localStorageService.GetAccessTokenAsync();
            return await SendRequest<CategoryCreateDto>(HttpMethod.Post, apiUrl, accessToken,
                JsonConvert.SerializeObject(body));
        }

        public async Task<bool> DeleteCategoryAsync(string id)
        {
            var apiUrl = ApplicationUrl + Apis.DeleteOrUpdateOrGetCategory + id;
            var accessToken = await _localStorageService.GetAccessTokenAsync();
            return await SendRequest(HttpMethod.Delete, apiUrl, accessToken);
        }

        public async Task<bool> UpdateCategoryAsync(string id, CategoryCreateDto updateDto)
        {
            var apiUrl = ApplicationUrl + Apis.DeleteOrUpdateOrGetCategory + id;
            var accessToken = await _localStorageService.GetAccessTokenAsync();
            return await SendRequest(HttpMethod.Put, apiUrl, accessToken, JsonConvert.SerializeObject(updateDto));
        }

        public async Task<List<CategoryDto>> GetCategoriesAsync(CategoryQueryParameters queryParameters)
        {
            var apiUrl = ApplicationUrl + Apis.CreateCategory;
            var accessToken = await _localStorageService.GetAccessTokenAsync();
            var dict = queryParameters.ToDictionary();
            return await SendRequest<List<CategoryDto>>(HttpMethod.Get, apiUrl, accessToken, queryPairs: dict);
        }

        public async Task<CategoryDto> GetCategoryById(string id)
        {
            var apiUrl = ApplicationUrl + Apis.DeleteOrUpdateOrGetCategory + id;
            var accessToken = await _localStorageService.GetAccessTokenAsync();
            return await SendRequest<CategoryDto>(HttpMethod.Get, apiUrl, accessToken);
        }

        #endregion

        #region lendConfig

        public async Task<LendConfigDto> CreateLendConfigAsync(LendConfigCreateDto body)
        {
            var apiUrl = ApplicationUrl + Apis.CreateLendConfig;
            var accessToken = await _localStorageService.GetAccessTokenAsync();
            return await SendRequest<LendConfigDto>(HttpMethod.Post, apiUrl, accessToken,
                JsonConvert.SerializeObject(body));
        }

        public async Task<bool> DeleteLendConfigAsync(string id)
        {
            var apiUrl = ApplicationUrl + Apis.DeleteOrUpdateOrGetLendConfig + id;
            var accessToken = await _localStorageService.GetAccessTokenAsync();
            return await SendRequest(HttpMethod.Delete, apiUrl, accessToken);
        }

        public async Task<bool> UpdateLendConfigAsync(string id, LendConfigCreateDto updateDto)
        {
            var apiUrl = ApplicationUrl + Apis.DeleteOrUpdateOrGetLendConfig + id;
            var accessToken = await _localStorageService.GetAccessTokenAsync();
            return await SendRequest(HttpMethod.Put, apiUrl, accessToken, JsonConvert.SerializeObject(updateDto));
        }

        public async Task<List<LendConfigDto>> GetLendConfigsAsync()
        {
            var apiUrl = ApplicationUrl + Apis.CreateLendConfig;
            var accessToken = await _localStorageService.GetAccessTokenAsync();
            return await SendRequest<List<LendConfigDto>>(HttpMethod.Get, apiUrl, accessToken);
        }

        public async Task<LendConfigDto> GetLendConfigById(string id)
        {
            var apiUrl = ApplicationUrl + Apis.DeleteOrUpdateOrGetLendConfig + id;
            var accessToken = await _localStorageService.GetAccessTokenAsync();
            return await SendRequest<LendConfigDto>(HttpMethod.Get, apiUrl, accessToken);
        }

        #endregion

        #region lendRecord

        public async Task<LendRecordDto> CreateLendRecordAsync(LendRecordCreateDto body)
        {
            var apiUrl = ApplicationUrl + Apis.CreateLendRecord;
            var accessToken = await _localStorageService.GetAccessTokenAsync();
            return await SendRequest<LendRecordDto>(HttpMethod.Post, apiUrl, accessToken,
                JsonConvert.SerializeObject(body));
        }

        public async Task<bool> DeleteLendRecordAsync(string id)
        {
            var apiUrl = ApplicationUrl + Apis.DeleteOrUpdateOrGetLendRecord + id;
            var accessToken = await _localStorageService.GetAccessTokenAsync();
            return await SendRequest(HttpMethod.Delete, apiUrl, accessToken);
        }

        public async Task<List<LendRecordDto>> GetLendRecordsAsync(LendRecordQueryParameters queryParameters)
        {
            var apiUrl = ApplicationUrl + Apis.CreateLendRecord;
            var accessToken = await _localStorageService.GetAccessTokenAsync();
            var dict = queryParameters.ToDictionary();
            return await SendRequest<List<LendRecordDto>>(HttpMethod.Get, apiUrl, accessToken, queryPairs: dict);
        }

        public async Task<LendRecordDto> GetLendRecordById(string id)
        {
            var apiUrl = ApplicationUrl + Apis.DeleteOrUpdateOrGetLendRecord + id;
            var accessToken = await _localStorageService.GetAccessTokenAsync();
            return await SendRequest<LendRecordDto>(HttpMethod.Get, apiUrl, accessToken);
        }

        public async Task<bool> UpdateLendRecordAsync(string id)
        {
            var apiUrl = ApplicationUrl + Apis.DeleteOrUpdateOrGetLendRecord + id;
            var accessToken = await _localStorageService.GetAccessTokenAsync();
            return await SendRequest(HttpMethod.Put, apiUrl, accessToken);
        }

        public async Task<bool> RenewAsync(string id)
        {
            var apiUrl = $"{ApplicationUrl + Apis.DeleteOrUpdateOrGetLendRecord}renew/{id}";
            var accessToken = await _localStorageService.GetAccessTokenAsync();
            return await SendRequest(HttpMethod.Put, apiUrl, accessToken);
        }

        #endregion

        #region notice

        public async Task<NoticeDto> CreateNoticeAsync(NoticeCreateDto body)
        {
            var apiUrl = ApplicationUrl + Apis.CreateNotice;
            var accessToken = await _localStorageService.GetAccessTokenAsync();
            return await SendRequest<NoticeDto>(HttpMethod.Post, apiUrl, accessToken,
                JsonConvert.SerializeObject(body));
        }

        public async Task<bool> DeleteNoticeAsync(string id)
        {
            var apiUrl = ApplicationUrl + Apis.DeleteOrUpdateOrGetNotice + id;
            var accessToken = await _localStorageService.GetAccessTokenAsync();
            return await SendRequest(HttpMethod.Delete, apiUrl, accessToken);
        }

        public async Task<bool> UpdateNoticeAsync(string id, NoticeCreateDto updateDto)
        {
            var apiUrl = ApplicationUrl + Apis.DeleteOrUpdateOrGetNotice + id;
            var accessToken = await _localStorageService.GetAccessTokenAsync();
            return await SendRequest(HttpMethod.Post, apiUrl, accessToken, JsonConvert.SerializeObject(updateDto));
        }

        public async Task<List<NoticeNoContentVo>> GetNoticesAsync(QueryParameters queryParameters)
        {
            var apiUrl = ApplicationUrl + Apis.CreateNotice;
            var accessToken = await _localStorageService.GetAccessTokenAsync();
            var dict = queryParameters.ToDictionary();
            return await SendRequest<List<NoticeNoContentVo>>(HttpMethod.Get, apiUrl, accessToken, queryPairs: dict);
        }

        public async Task<NoticeDto> GetNoticeById(string id)
        {
            var apiUrl = ApplicationUrl + Apis.DeleteOrUpdateOrGetNotice + id;
            var accessToken = await _localStorageService.GetAccessTokenAsync();
            return await SendRequest<NoticeDto>(HttpMethod.Get, apiUrl, accessToken);
        }

        #endregion

        #region dashBoard

        public async Task<List<ChartDataItem>> SelectLast30DaysData()
        {
            var apiUrl = ApplicationUrl + Apis.SelectMonth;
            var accessToken = await _localStorageService.GetAccessTokenAsync();
            return await SendRequest<List<ChartDataItem>>(HttpMethod.Get, apiUrl, accessToken);
        }

        public async Task<List<ChartDataItem>> SelectLastYearData()
        {
            var apiUrl = ApplicationUrl + Apis.SelectYear;
            var accessToken = await _localStorageService.GetAccessTokenAsync();
            return await SendRequest<List<ChartDataItem>>(HttpMethod.Get, apiUrl, accessToken);
        }

        #endregion
    }
}