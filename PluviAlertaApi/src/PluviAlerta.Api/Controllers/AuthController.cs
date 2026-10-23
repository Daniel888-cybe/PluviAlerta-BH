using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PluviAlerta.Api.Auth;

namespace PluviAlerta.Api.Controllers;

public record LoginRequest([Required] string Usuario, [Required] string Senha);
public record TokenResposta(string Token, DateTime ExpiraEm);

[ApiController]
[Route("api/auth")]
public class AuthController(IConfiguration config, ConfiguracaoJwt jwt) : ControllerBase
{
    public const string PoliticaLimite = "login";

    [HttpPost("token")]
    [EnableRateLimiting(PoliticaLimite)]
    [EndpointSummary("Login do administrador (gera o JWT da simulação)")]
    [EndpointDescription("Usuário e senha vêm das variáveis ADMIN_USUARIO e ADMIN_SENHA. O token vale 2 h.")]
    [ProducesResponseType<TokenResposta>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public IActionResult GerarToken(LoginRequest request)
    {
        // "&" (e não "&&") para sempre comparar os dois: o tempo de resposta não revela qual errou.
        var ok = IguaisEmTempoConstante(request.Usuario, config["Admin:Usuario"]!)
               & IguaisEmTempoConstante(request.Senha, config["Admin:Senha"]!);
        if (!ok)
            return Unauthorized();

        var expiraEm = DateTime.UtcNow + ConfiguracaoJwt.Validade;
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Emissor,
            Audience = jwt.Emissor,
            Expires = expiraEm,
            Subject = new ClaimsIdentity([
                new Claim(JwtRegisteredClaimNames.Sub, request.Usuario),
                new Claim(ClaimTypes.Role, "admin"),
            ]),
            SigningCredentials = new SigningCredentials(jwt.Chave, SecurityAlgorithms.HmacSha256),
        });

        return Ok(new TokenResposta(token, expiraEm));
    }

    private static bool IguaisEmTempoConstante(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
