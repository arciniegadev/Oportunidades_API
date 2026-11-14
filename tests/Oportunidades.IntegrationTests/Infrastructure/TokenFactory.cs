using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Oportunidades.IntegrationTests.Infrastructure;

internal static class TokenFactory
{
    public static string Create(string role, string signingKey, string issuer, string audience)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, role.ToLowerInvariant()),
            new Claim("role", role)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now.AddMinutes(-1),
            expires: now.AddMinutes(60),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
