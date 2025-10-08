namespace CompleteAuthenticationAPI.Cookies
{
    public interface ICookieService
    {
        void SetCookieAccessToken(string token);

        void SetCookieRefreshToken(string token);
    }
}
