using GovAmbiental.API.Common;
using GovAmbiental.API.Data;
using GovAmbiental.API.Models;
using GovAmbiental.API.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace GovAmbiental.API.Services;

public class AuditoriaService : IAuditoriaService
{
    private readonly AppDbContext _context;

    public AuditoriaService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResultViewModel<AuditoriaResumoResponse>> ListarAsync(
        AuditoriaFiltro filtro, PaginationParameters paginacao, CancellationToken ct)
    {
        var query = _context.Auditorias.AsNoTracking().AsQueryable();

        if (filtro.Status.HasValue)
            query = query.Where(a => a.Status == filtro.Status.Value);

        if (!string.IsNullOrWhiteSpace(filtro.UnidadeOperacional))
            query = query.Where(a => a.UnidadeOperacional == filtro.UnidadeOperacional);

        if (filtro.DataInicio.HasValue)
            query = query.Where(a => a.DataInicio >= filtro.DataInicio.Value);

        if (filtro.DataFim.HasValue)
            query = query.Where(a => a.DataInicio <= filtro.DataFim.Value);

        var total = await query.CountAsync(ct);

        var itens = await query
            .OrderByDescending(a => a.DataInicio)
            .Skip(paginacao.Skip)
            .Take(paginacao.PageSize)
            .Select(a => new AuditoriaResumoResponse
            {
                Id = a.Id,
                Titulo = a.Titulo,
                UnidadeOperacional = a.UnidadeOperacional,
                Responsavel = a.Responsavel,
                DataInicio = a.DataInicio,
                DataConclusao = a.DataConclusao,
                Status = a.Status.ToString(),
                ScoreConformidade = a.ScoreConformidade,
                Resultado = a.Resultado.ToString(),
                TotalNaoConformidades = a.NaoConformidades.Count
            })
            .ToListAsync(ct);

        return new PagedResultViewModel<AuditoriaResumoResponse>(itens, total, paginacao.PageNumber, paginacao.PageSize);
    }

    public async Task<AuditoriaResumoResponse> CriarAsync(CriarAuditoriaRequest request, CancellationToken ct)
    {
        var requisitos = await _context.Requisitos
            .AsNoTracking()
            .Where(r => request.NormaIds.Contains(r.NormaAmbientalId))
            .Select(r => r.Id)
            .ToListAsync(ct);

        if (requisitos.Count == 0)
            throw new BusinessRuleException("As normas selecionadas não possuem requisitos ou não existem.");

        var auditoria = new Auditoria
        {
            Titulo = request.Titulo,
            UnidadeOperacional = request.UnidadeOperacional,
            Responsavel = request.Responsavel,
            DataInicio = request.DataInicio,
            Status = StatusAuditoria.Planejada,
            Resultado = ResultadoConformidade.NaoAvaliado,
            // Um item por requisito das normas selecionadas (a serem avaliados depois).
            Itens = requisitos.Select(rid => new ItemAuditoria
            {
                RequisitoNormaId = rid,
                Atendido = false
            }).ToList()
        };

        _context.Auditorias.Add(auditoria);
        await _context.SaveChangesAsync(ct);

        return new AuditoriaResumoResponse
        {
            Id = auditoria.Id,
            Titulo = auditoria.Titulo,
            UnidadeOperacional = auditoria.UnidadeOperacional,
            Responsavel = auditoria.Responsavel,
            DataInicio = auditoria.DataInicio,
            DataConclusao = auditoria.DataConclusao,
            Status = auditoria.Status.ToString(),
            ScoreConformidade = auditoria.ScoreConformidade,
            Resultado = auditoria.Resultado.ToString(),
            TotalNaoConformidades = 0
        };
    }

    public async Task<AvaliacaoResultadoResponse> AvaliarAsync(
        int auditoriaId, AvaliacaoAuditoriaRequest request, CancellationToken ct)
    {
        var auditoria = await _context.Auditorias
            .Include(a => a.Itens).ThenInclude(i => i.Requisito)
            .Include(a => a.NaoConformidades).ThenInclude(nc => nc.AcaoCorretiva)
            .FirstOrDefaultAsync(a => a.Id == auditoriaId, ct);

        if (auditoria is null)
            throw new NotFoundException($"Auditoria {auditoriaId} não encontrada.");

        if (auditoria.Status == StatusAuditoria.Concluida)
            throw new BusinessRuleException("Esta auditoria já foi avaliada e concluída.");

        // Toda avaliação enviada deve corresponder a um requisito previsto na auditoria.
        var itensPorRequisito = auditoria.Itens.ToDictionary(i => i.RequisitoNormaId);
        var requisitosEnviados = request.Itens.Select(i => i.RequisitoNormaId).ToHashSet();

        var invalidos = requisitosEnviados.Except(itensPorRequisito.Keys).ToList();
        if (invalidos.Count > 0)
            throw new BusinessRuleException($"Requisitos não pertencem à auditoria: {string.Join(", ", invalidos)}.");

        var naoAvaliados = itensPorRequisito.Keys.Except(requisitosEnviados).ToList();
        if (naoAvaliados.Count > 0)
            throw new BusinessRuleException("Todos os requisitos da auditoria devem ser avaliados.");

        // Aplica as avaliações nos itens.
        foreach (var avaliacao in request.Itens)
        {
            var item = itensPorRequisito[avaliacao.RequisitoNormaId];
            item.Atendido = avaliacao.Atendido;
            item.Observacao = avaliacao.Observacao;
            item.Evidencia = avaliacao.Evidencia;
        }

        // Cálculo do score ponderado pelo peso dos requisitos.
        var pesoTotal = auditoria.Itens.Sum(i => i.Requisito!.Peso);
        var pesoAtendido = auditoria.Itens.Where(i => i.Atendido).Sum(i => i.Requisito!.Peso);
        var score = pesoTotal == 0 ? 0m : Math.Round((decimal)pesoAtendido / pesoTotal * 100m, 2);

        auditoria.ScoreConformidade = score;
        auditoria.Resultado = ClassificarResultado(score);
        auditoria.Status = StatusAuditoria.Concluida;
        auditoria.DataConclusao = DateTime.UtcNow;

        // Geração automática de não-conformidades + planos de ação para requisitos não atendidos.
        var naoConformidades = new List<NaoConformidade>();
        foreach (var item in auditoria.Itens.Where(i => !i.Atendido))
        {
            var requisito = item.Requisito!;
            var nc = new NaoConformidade
            {
                AuditoriaId = auditoria.Id,
                RequisitoNormaId = requisito.Id,
                Descricao = $"Requisito não atendido: {requisito.Descricao}",
                Criticidade = requisito.Criticidade,
                IdentificadaEm = DateTime.UtcNow,
                AcaoCorretiva = new AcaoCorretiva
                {
                    Descricao = $"Plano de ação para: {requisito.Descricao}",
                    Responsavel = auditoria.Responsavel,
                    PrazoLimite = DateTime.UtcNow.AddDays(PrazoPorCriticidade(requisito.Criticidade)),
                    Status = StatusAcaoCorretiva.Pendente
                }
            };
            naoConformidades.Add(nc);
            auditoria.NaoConformidades.Add(nc);
        }

        await _context.SaveChangesAsync(ct);

        return new AvaliacaoResultadoResponse
        {
            AuditoriaId = auditoria.Id,
            ScoreConformidade = score,
            Resultado = auditoria.Resultado.ToString(),
            RequisitosAvaliados = auditoria.Itens.Count,
            RequisitosAtendidos = auditoria.Itens.Count(i => i.Atendido),
            NaoConformidadesGeradas = naoConformidades.Select(nc => new NaoConformidadeResponse
            {
                Id = nc.Id,
                AuditoriaId = nc.AuditoriaId,
                AuditoriaTitulo = auditoria.Titulo,
                Descricao = nc.Descricao,
                Criticidade = nc.Criticidade.ToString(),
                IdentificadaEm = nc.IdentificadaEm,
                AcaoResponsavel = nc.AcaoCorretiva!.Responsavel,
                AcaoPrazoLimite = nc.AcaoCorretiva.PrazoLimite,
                AcaoStatus = nc.AcaoCorretiva.Status.ToString()
            }).ToList()
        };
    }

    public async Task<RelatorioAuditoriaResponse> ObterRelatorioAsync(int auditoriaId, CancellationToken ct)
    {
        var auditoria = await _context.Auditorias
            .AsNoTracking()
            .Include(a => a.Itens).ThenInclude(i => i.Requisito!).ThenInclude(r => r.Norma)
            .Include(a => a.NaoConformidades).ThenInclude(nc => nc.AcaoCorretiva)
            .FirstOrDefaultAsync(a => a.Id == auditoriaId, ct);

        if (auditoria is null)
            throw new NotFoundException($"Auditoria {auditoriaId} não encontrada.");

        var scorePorCategoria = auditoria.Itens
            .GroupBy(i => i.Requisito!.Norma!.Categoria)
            .Select(g => new CategoriaScoreResponse
            {
                Categoria = g.Key.ToString(),
                TotalRequisitos = g.Count(),
                Atendidos = g.Count(i => i.Atendido),
                PercentualConformidade = g.Any()
                    ? Math.Round((decimal)g.Count(i => i.Atendido) / g.Count() * 100m, 2)
                    : 0m
            })
            .OrderBy(c => c.Categoria)
            .ToList();

        return new RelatorioAuditoriaResponse
        {
            AuditoriaId = auditoria.Id,
            Titulo = auditoria.Titulo,
            UnidadeOperacional = auditoria.UnidadeOperacional,
            Status = auditoria.Status.ToString(),
            ScoreConformidade = auditoria.ScoreConformidade,
            Resultado = auditoria.Resultado.ToString(),
            TotalRequisitos = auditoria.Itens.Count,
            RequisitosAtendidos = auditoria.Itens.Count(i => i.Atendido),
            ScorePorCategoria = scorePorCategoria,
            NaoConformidades = auditoria.NaoConformidades.Select(nc => new NaoConformidadeResponse
            {
                Id = nc.Id,
                AuditoriaId = nc.AuditoriaId,
                AuditoriaTitulo = auditoria.Titulo,
                Descricao = nc.Descricao,
                Criticidade = nc.Criticidade.ToString(),
                IdentificadaEm = nc.IdentificadaEm,
                AcaoResponsavel = nc.AcaoCorretiva?.Responsavel,
                AcaoPrazoLimite = nc.AcaoCorretiva?.PrazoLimite,
                AcaoStatus = nc.AcaoCorretiva?.Status.ToString()
            }).ToList()
        };
    }

    private static ResultadoConformidade ClassificarResultado(decimal score) => score switch
    {
        >= 90m => ResultadoConformidade.Conforme,
        >= 60m => ResultadoConformidade.ParcialmenteConforme,
        _ => ResultadoConformidade.NaoConforme
    };

    /// <summary>Prazo (em dias) sugerido para a ação corretiva conforme a criticidade.</summary>
    private static int PrazoPorCriticidade(Criticidade criticidade) => criticidade switch
    {
        Criticidade.Critica => 7,
        Criticidade.Alta => 15,
        Criticidade.Media => 30,
        _ => 60
    };
}
