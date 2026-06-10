using System.Net.Http.Headers;

namespace KanbanDmz.Web.Services;

public class AuthorizedHandler : DelegatingHandler
{
    private readonly UserTokenProvider _tokenProvider;

    public AuthorizedHandler(UserTokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_tokenProvider.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenProvider.AccessToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
