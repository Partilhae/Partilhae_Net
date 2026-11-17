using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;

namespace Partilhae_Net.Middlewares;

public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;

    private readonly ConcurrentDictionary<string, byte> _emExecucao = new();

    private record RespostaCache(
        string Hash,
        int StatusCode,
        string? ContentType,
        string? Location,
        byte[] Body);

    public IdempotencyMiddleware(
        RequestDelegate next,
        IMemoryCache cache)
    {
        _next = next;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var usuario = context.User.FindFirst("idUsuario")?.Value;

        if (!HttpMethods.IsPost(context.Request.Method) ||
            context.User.Identity?.IsAuthenticated != true ||
            string.IsNullOrWhiteSpace(usuario) ||
            !context.Request.Headers.TryGetValue(
                "Idempotency-Key", out var header))
        {
            await _next(context);
            return;
        }

        var chave = header.ToString().Trim();

        if (header.Count != 1 ||
            string.IsNullOrWhiteSpace(chave) ||
            chave.Length > 100)
        {
            context.Response.StatusCode = 400;

            await context.Response.WriteAsJsonAsync(new
            {
                mensagem = "Idempotency-Key inválida."
            });

            return;
        }

        var chaveCache =
            $"idempotencia:{usuario}:{context.Request.Path}:{context.Request.QueryString}:{chave}";

        context.Request.EnableBuffering();

        var hashBytes = await SHA256.HashDataAsync(
            context.Request.Body,
            context.RequestAborted);

        var hash = Convert.ToHexString(hashBytes);

        context.Request.Body.Position = 0;

        if (!_emExecucao.TryAdd(chaveCache, 0))
        {
            context.Response.StatusCode = 409;

            await context.Response.WriteAsJsonAsync(new
            {
                mensagem = "Esta operação já está em processamento."
            });

            return;
        }

        try
        {
            if (_cache.TryGetValue<RespostaCache>(
                    chaveCache, out var resposta) &&
                resposta is not null)
            {
                if (resposta.Hash != hash)
                {
                    context.Response.StatusCode = 409;

                    await context.Response.WriteAsJsonAsync(new
                    {
                        mensagem =
                            "Esta chave já foi utilizada com outros dados."
                    });

                    return;
                }

                context.Response.StatusCode = resposta.StatusCode;
                context.Response.ContentType = resposta.ContentType;

                if (resposta.Location is not null)
                {
                    context.Response.Headers.Location = resposta.Location;
                }

                context.Response.Headers["Idempotency-Replayed"] = "true";

                await context.Response.Body.WriteAsync(
                    resposta.Body,
                    context.RequestAborted);

                return;
            }

            var bodyOriginal = context.Response.Body;

            using var buffer = new MemoryStream();

            context.Response.Body = buffer;

            try
            {
                await _next(context);

                var body = buffer.ToArray();

                if (context.Response.StatusCode >= 200 &&
                    context.Response.StatusCode < 300)
                {
                    _cache.Set(
                        chaveCache,
                        new RespostaCache(
                            hash,
                            context.Response.StatusCode,
                            context.Response.ContentType,
                            context.Response.Headers.Location.ToString() is
                                { Length: > 0 } location ? location : null,
                            body),
                        TimeSpan.FromMinutes(30));
                }

                buffer.Position = 0;

                await buffer.CopyToAsync(
                    bodyOriginal,
                    context.RequestAborted);
            }
            finally
            {
                context.Response.Body = bodyOriginal;
            }
        }
        finally
        {
            _emExecucao.TryRemove(chaveCache, out _);
        }
    }
}