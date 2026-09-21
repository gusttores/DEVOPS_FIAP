using GovAmbiental.API.Common;
using GovAmbiental.API.Data;
using GovAmbiental.API.Models;
using GovAmbiental.API.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace GovAmbiental.API.Services;

public class ConformidadeService : IConformidadeService
{
    private readonly AppDbContext _context;

    public ConformidadeService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IndicadoresResponse> ObterIndicadoresAsync(CancellationToken ct)
    {
        var agora = DateTime.UtcNow;

        // Cada agregação é resolvida no banco (COUNT/AVG), sem trazer entidades.
        var totalAuditorias = await _context.Auditorias.CountAsync(ct);
        var concluidas = await _context.Auditorias
            .CountAsync(a => a.Status == StatusAuditoria.Concluida, ct);

        // AVG calculado sobre double (CAST AS float no SQL) — portável entre provedores
        // relacionais; a média é resolvida no banco e convertida de volta para decimal.
        var scoreMedio = await _context.Auditorias
            .Where(a => a.Status == StatusAuditoria.Concluida)
            .Select(a => (double?)a.ScoreConformidade)
            .AverageAsync(ct) ?? 0d;

        var totalNc = await _context.NaoConformidades.CountAsync(ct);

        var acoesVencidas = await _context.AcoesCorretivas
            .CountAsync(ac => ac.Status != StatusAcaoCorretiva.Concluida && ac.PrazoLimite < agora, ct);

        var acoesPendentes = await _context.AcoesCorretivas
            .CountAsync(ac => ac.Status == StatusAcaoCorretiva.Pendente, ct);

        var normasMaisDescumpridas = await _context.NaoConformidades
            .GroupBy(nc => new { nc.Requisito!.Norma!.Id, nc.Requisito.Norma.Codigo, nc.Requisito.Norma.Titulo })
            .Select(g => new NormaDescumpridaResponse
            {
                NormaId = g.Key.Id,
                Codigo = g.Key.Codigo,
                Titulo = g.Key.Titulo,
                Ocorrencias = g.Count()
            })
            .OrderByDescending(x => x.Ocorrencias)
            .Take(5)
            .ToListAsync(ct);

        return new IndicadoresResponse
        {
            TotalAuditorias = totalAuditorias,
            AuditoriasConcluidas = concluidas,
            ScoreMedioConformidade = Math.Round((decimal)scoreMedio, 2),
            TotalNaoConformidades = totalNc,
            AcoesVencidas = acoesVencidas,
            AcoesPendentes = acoesPendentes,
            NormasMaisDescumpridas = normasMaisDescumpridas
        };
    }

    public async Task<PagedResultViewModel<NaoConformidadeResponse>> ListarNaoConformidadesAsync(
        bool apenasVencidas, PaginationParameters paginacao, CancellationToken ct)
    {
        var agora = DateTime.UtcNow;

        var query = _context.NaoConformidades.AsNoTracking().AsQueryable();

        if (apenasVencidas)
        {
            query = query.Where(nc => nc.AcaoCorretiva != null
                && nc.AcaoCorretiva.Status != StatusAcaoCorretiva.Concluida
                && nc.AcaoCorretiva.PrazoLimite < agora);
        }

        var total = await query.CountAsync(ct);

        var itens = await query
            .OrderBy(nc => nc.AcaoCorretiva!.PrazoLimite)
            .Skip(paginacao.Skip)
            .Take(paginacao.PageSize)
            .Select(nc => new NaoConformidadeResponse
            {
                Id = nc.Id,
                AuditoriaId = nc.AuditoriaId,
                AuditoriaTitulo = nc.Auditoria!.Titulo,
                Descricao = nc.Descricao,
                Criticidade = nc.Criticidade.ToString(),
                IdentificadaEm = nc.IdentificadaEm,
                AcaoResponsavel = nc.AcaoCorretiva!.Responsavel,
                AcaoPrazoLimite = nc.AcaoCorretiva.PrazoLimite,
                AcaoStatus = nc.AcaoCorretiva.Status.ToString()
            })
            .ToListAsync(ct);

        // Cálculo dos alertas (vencida / dias para o prazo) em memória, após a paginação.
        foreach (var item in itens)
        {
            if (item.AcaoPrazoLimite is null) continue;
            var dias = (int)Math.Ceiling((item.AcaoPrazoLimite.Value - agora).TotalDays);
            item.DiasParaPrazo = dias;
            item.Vencida = dias < 0 && item.AcaoStatus != nameof(StatusAcaoCorretiva.Concluida);
        }

        return new PagedResultViewModel<NaoConformidadeResponse>(itens, total, paginacao.PageNumber, paginacao.PageSize);
    }
}
