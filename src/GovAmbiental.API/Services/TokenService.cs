using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GovAmbiental.API.Common;
using GovAmbiental.API.ViewModels;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GovAmbiental.API.Services;

public class TokenService : ITokenService
{
    private readonly JwtSettings _jwt;
    private readonly AuthCredentials _credentials;

    public TokenService(IOptions<JwtSettings> jwt, IOptions<AuthCredentials> credentials)
    {
        _jwt = jwt.Value;
        _credentials = credentials.Value;
    }

    public bool ValidarCredenciais(string email, string senha)
    {
        return string.Equals(email, _credentials.Email, StringComparison.OrdinalIgnoreCase)
               && senha == _credentials.Senha;
    }

    public LoginResponse GerarToken(string email)
    {
        var expiraEm = DateTime.UtcNow.AddMinutes(_jwt.ExpiresMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, email),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(ClaimTypes.Role, _credentials.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: expiraEm,
            signingCredentials: creds);

        return new LoginResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiraEm = expiraEm
        };
    }
}
