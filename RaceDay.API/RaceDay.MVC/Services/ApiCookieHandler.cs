namespace RaceDay.MVC.Services
{
    // forwarding the saved api session cookie on every outgoing request to raceday.api
    public class ApiCookieHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApiCookieHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var apiCookie = _httpContextAccessor.HttpContext?.Session.GetString("ApiSessionCookie");
            if (!string.IsNullOrEmpty(apiCookie))
            {
                request.Headers.TryAddWithoutValidation("Cookie", apiCookie);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}