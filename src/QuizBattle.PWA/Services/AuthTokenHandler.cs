using System.Net;
using System.Net.Http.Headers;
using QuizBattle.Shared;

namespace QuizBattle.PWA.Services;

/// <summary>
/// پیام‌رسان HTTP که توکن احراز هویت را به درخواست‌ها اضافه می‌کند
/// و در صورت خطای 401 کاربر را به صفحه ورود هدایت می‌کند
/// </summary>
public class AuthTokenHandler : DelegatingHandler
{
    private readonly AppState _appState;

    public AuthTokenHandler(AppState appState)
    {
        _appState = appState;
        InnerHandler = new HttpClientHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // اضافه کردن توکن به header اگر موجود باشد
        if (!string.IsNullOrEmpty(_appState.AuthToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _appState.AuthToken);
        }

        var response = await base.SendAsync(request, cancellationToken);

        // اگر خطای 401 دریافت شد، کاربر را به صفحه ورود هدایت کن
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // پاک کردن توکن و اطلاعات کاربر
            _appState.AuthToken = null;
            _appState.CurrentUserId = Guid.Empty;
            _appState.NotifyUnauthorized();
        }

        return response;
    }
}
