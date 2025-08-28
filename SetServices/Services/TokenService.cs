using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using SignInApi.Services.IServices;

namespace TodoListJwt.SetServices.Services
{
    public class TokenService : ITokenService
    {
        public JwtSecurityToken GenerateAccessToken(IEnumerable<Claim> claims, IConfiguration _config)
        {
            string? key = _config.GetSection("JWT").GetValue<string>("SecretKey") ?? 
                throw new InvalidOperationException();

            byte[]? privateKey = Encoding.UTF8.GetBytes(key);

            SigningCredentials? signingCredentials = new SigningCredentials(new SymmetricSecurityKey(privateKey), 
                SecurityAlgorithms.HmacSha256Signature);

            SecurityTokenDescriptor? tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_config.GetSection("JWT").GetValue<double>("TokenValidityInMinutes")),
                Audience = _config.GetSection("JWT").GetValue<string>("ValidAudience"),
                Issuer = _config.GetSection("JWT").GetValue<string>("ValidIssuer"),
                SigningCredentials = signingCredentials
                    
            };

            JwtSecurityTokenHandler? tokenHandler = new JwtSecurityTokenHandler();

            JwtSecurityToken? token = tokenHandler.CreateJwtSecurityToken(tokenDescriptor);

            return token;

        }

        public string GenerateRefreshToken()
        {
            byte[]? secureRandomBytes = new byte[256];

            using RandomNumberGenerator? randomNumberGenerator = RandomNumberGenerator.Create();

            randomNumberGenerator.GetBytes(secureRandomBytes);

            string? refreshToken = Convert.ToBase64String(secureRandomBytes);

            return refreshToken;
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token, IConfiguration _config)
        {
            string? secretKey = _config["JWT:SecretKey"] ?? throw new InvalidOperationException("Invalid key");

            TokenValidationParameters? tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(secretKey)),
                ValidateLifetime = false
            };

            JwtSecurityTokenHandler? tokenHandler = new JwtSecurityTokenHandler();
            ClaimsPrincipal? principal = tokenHandler.ValidateToken(token, tokenValidationParameters,
                out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(
                SecurityAlgorithms.HmacSha256,
                StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }

    }
}