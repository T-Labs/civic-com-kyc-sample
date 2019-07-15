namespace CivicLoginApi.Result
{
    public class JwtResponse
    {
        public string jti { get; set; }
        public float iat { get; set; }
        public float exp { get; set; }
        public string iss { get; set; }
        public string aud { get; set; }
        public string sub { get; set; }
        public string data { get; set; }
    }
}
