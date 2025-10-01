// Helpers/JwtHelper.cs
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

public static class JwtHelper
{
    public static string GenerateToken(User user, string key)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("UserId", user.Id.ToString()),
            new Claim("IsSeller", user.IsSeller.ToString())
        };
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.Now.AddHours(2),
            signingCredentials: credentials);


        if (key.Length < 32)
        {
            throw new ArgumentException("The key must be at least 32 characters long.");
        }
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
