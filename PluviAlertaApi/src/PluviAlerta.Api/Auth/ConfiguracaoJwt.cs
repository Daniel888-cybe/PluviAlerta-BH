using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace PluviAlerta.Api.Auth;

/// <summary>Chave e emissor do JWT, lidos da configuração (Jwt:Chave vem de variável de ambiente / user-secrets).</summary>
public record ConfiguracaoJwt(SymmetricSecurityKey Chave, string Emissor)
{
    public const string EmissorPadrao = "PluviAlertaBH";
    public static readonly TimeSpan Validade = TimeSpan.FromHours(2);

    public static ConfiguracaoJwt Ler(IConfiguration config)
    {
        var chave = config["Jwt:Chave"];
        // HS256 exige chave de pelo menos 256 bits (32 bytes).
        if (string.IsNullOrWhiteSpace(chave) || Encoding.UTF8.GetByteCount(chave) < 32)
            throw new InvalidOperationException(
                "Jwt:Chave não configurada ou com menos de 32 caracteres. Veja 'Variáveis de ambiente' no README.");

        if (string.IsNullOrWhiteSpace(config["Admin:Usuario"]) || string.IsNullOrWhiteSpace(config["Admin:Senha"]))
            throw new InvalidOperationException(
                "Admin:Usuario e Admin:Senha não configurados. Veja 'Variáveis de ambiente' no README.");

        return new ConfiguracaoJwt(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(chave)), EmissorPadrao);
    }
}
