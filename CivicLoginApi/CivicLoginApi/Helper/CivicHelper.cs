using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CivicLoginApi.Result;
using Jose;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Math;
using RestSharp;

namespace CivicLoginApi.Helper
{
    public class CivicHelper : ICivicHelper
    {
        private readonly string _appId;
        private readonly string _appSecret;
        private readonly string _privateSigningKey;

        public CivicHelper(string privateSigningKey, string appId, string appSecret)
        {
            _privateSigningKey = privateSigningKey;
            _appId = appId;
            _appSecret = appSecret;
        }

        public Result<CivicUserData> ExchangeCodeAsync(string jwt)
        {
            var body = CreateBody(jwt);
            var header = CreateHeader(body);

            var client = new RestClient("https://api.civic.com/");
            var request = new RestRequest("sip/prod/scopeRequest/authCode", Method.POST);
            request.Parameters.Add(new Parameter
            {
                ContentType = "application/json",
                Name = "application/json",
                Type = ParameterType.RequestBody,
                Value = body
            });

            request.AddHeader("Content-Length", body.Length.ToString());
            request.AddHeader("Accept", "*/*");
            request.AddHeader("Authorization", header);
            request.AddHeader("Content-Type", "application/json");

            var response = client.Execute<Response>(request);
            if (!response.IsSuccessful)
            {
                return Result<CivicUserData>.Error(response.ErrorMessage + ", this happens when a token has already been used.");
            }

            var jwtClient = new RestClient(response.Data.Data);
            var jwtRequest = new RestRequest(Method.GET);
            var jwtResult = jwtClient.Execute(jwtRequest);
            if (!jwtResult.IsSuccessful)
            {
                return Result<CivicUserData>.Error(jwtResult.ErrorMessage);
            }

            //Console.WriteLine(jwtResult.Content);

            //decode jwt payload.
            var payload = DecodePayload(jwtResult.Content);
            if (!payload.IsSuccess)
            {
                return Result<CivicUserData>.Error(payload.Message);
            }


            //decrypt
            var decrypted = Decrypt(payload.Data.data);
            if (!decrypted.IsSuccess)
            {
                return Result<CivicUserData>.Error(decrypted.Message);
            }

            //deserialize into final result
            var attributes = JsonConvert.DeserializeObject<CivicUserData>(decrypted.Data);
            return Result<CivicUserData>.Success(attributes);
        }

        internal static string CreateBody(string jwt)
        {
            return JsonConvert.SerializeObject(new { authToken = jwt, processPayload = true });
        }

        internal string CreateCivicExtension(string body)
        {
            using (var hmac = new HMACSHA256 { Key = Encoding.UTF8.GetBytes(_appSecret) })
            {
                var contentBytes = Encoding.UTF8.GetBytes(body);
                var signature = hmac.ComputeHash(contentBytes);
                return Convert.ToBase64String(signature);
            }
        }


        internal string CreateCivicToken(string uuid, double now, double until)
        {
            uuid = uuid ?? Guid.NewGuid().ToString();
            now = now < 1 ? DateTimeOffset.UtcNow.ToUnixTimeSeconds() : now;
            until = until < 1 ? DateTimeOffset.UtcNow.AddMinutes(3).ToUnixTimeSeconds() : until;

            var content = new Dictionary<string, object>
            {
                {"jti", uuid},
                {"iat", now},
                {"exp", until},
                {"iss", _appId},
                {"aud", "https://api.civic.com/sip/"},
                {"sub", _appId},
                {
                    "data", new
                    {
                        method = "POST",
                        path = "scopeRequest/authCode"
                    }
                }
            };
            var headers = new Dictionary<string, object>
            {
                {"alg", "ES256"},
                {"typ", "JWT"}
            };
            var secretKey = LoadPrivateKey(StringToByteArray(_privateSigningKey));
            var signedToken = JWT.Encode(content, secretKey, JwsAlgorithm.ES256, headers);
            var json = JWT.Decode(signedToken, secretKey);
            return signedToken;
        }


        internal string CreateHeader(string body, string uuid = null, double now = 0, double until = 0)
        {
            var token = CreateCivicToken(uuid, now, until);
            var extension = CreateCivicExtension(body);
            return $"Civic {token}.{extension}";
        }

        internal Result<string> Decrypt(string data)
        {
            try
            {
                var bytesKey = StringToByteArray(_appSecret);
                var iv = StringToByteArray(data.Substring(0, 32));
                var encrypted = data.Substring(32);
                using (var aes = new AesCryptoServiceProvider())
                {
                    var fromBase64ToBytes = Convert.FromBase64String(encrypted);
                    var decryptor = aes.CreateDecryptor(bytesKey, iv);
                    var decryptedBytes = decryptor.TransformFinalBlock(fromBase64ToBytes, 0, fromBase64ToBytes.Length);
                    var decrypted = Encoding.UTF8.GetString(decryptedBytes);
                    return Result<string>.Success(decrypted);
                }
            }
            catch (Exception e)
            {
                return Result<string>.Error(e.Message);
            }
        }

        private static ECDsa LoadPrivateKey(byte[] key)
        {
            var privKeyInt = new BigInteger(+1, key);
            var parameters = SecNamedCurves.GetByName("secp256r1");
            var ecPoint = parameters.G.Multiply(privKeyInt);
            var privKeyX = ecPoint.Normalize().XCoord.ToBigInteger().ToByteArrayUnsigned();
            var privKeyY = ecPoint.Normalize().YCoord.ToBigInteger().ToByteArrayUnsigned();

            return ECDsa.Create(new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                D = privKeyInt.ToByteArrayUnsigned(),
                Q = new ECPoint
                {
                    X = privKeyX,
                    Y = privKeyY
                }
            });
        }

        internal static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }


        internal Result<JwtResponse> DecodePayload(string jwt)
        {
            try
            {
                var json = JWT.Payload<JwtResponse>(jwt);
                return Result<JwtResponse>.Success(json);
            }
            catch (Exception e)
            {
                return Result<JwtResponse>.Error(e.Message);
            }
        }
    }
}
