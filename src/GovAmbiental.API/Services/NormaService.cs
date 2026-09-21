using GovAmbiental.API.Common;
using GovAmbiental.API.Data;
using GovAmbiental.API.Models;
using GovAmbiental.API.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace GovAmbiental.API.Services;

public class NormaService : INormaService
{
    private readonly AppDbContext _context;

    public NormaService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResultViewModel<NormaResponse>> ListarAsync(
        NormaFiltro filtro, PaginationParameters paginacao, CancellationToken ct)
    {
        // AsNoTracking + filtros traduzidos para SQL; só a página solicitada é materializada.
        var query = _context.Normas.AsNoTracking().AsQueryable();

        if (filtro.Categoria.HasValue)
            query = query.Where(n => n.Categoria == filtro.Categoria.Value);

        if (!string.IsNullOrWhiteSpace(filtro.OrgaoEmissor))
            query = query.Where(n => n.OrgaoEmissor == filtro.OrgaoEmissor);

        if (filtro.Ativa.HasValue)
            query = query.Where(n => n.Ativa == filtro.Ativa.Value);

        var total = await query.CountAsync(ct);

        // Projeção direta para o ViewModel (evita carregar entidades e relacionamentos completos).
        var itens = await query
            .OrderBy(n => n.Codigo)
            .Skip(paginacao.Skip)
            .Take(paginacao.PageSize)
            .Select(n => new NormaResponse
            {
                Id = n.Id,
                Codigo = n.Codigo,
                Titulo = n.Titulo,
                Descricao = n.Descricao,
                Categoria = n.Categoria.ToString(),
                OrgaoEmissor = n.OrgaoEmissor,
                Ativa = n.Ativa,
                DataVigencia = n.DataVigencia,
                QuantidadeRequisitos = n.Requisitos.Count
            })
            .ToListAsync(ct);

        return new PagedResultViewModel<NormaResponse>(itens, total, paginacao.PageNumber, paginacao.PageSize);
    }

    public async Task<NormaResponse> CriarAsync(CriarNormaRequest request, CancellationToken ct)
    {
        var codigoExiste = await _context.Normas.AnyAsync(n => n.Codigo == request.Codigo, ct);
        if (codigoExiste)
            throw new BusinessRuleException($"Já existe uma norma com o código '{request.Codigo}'.");

        var norma = new NormaAmbiental
        {
            Codigo = request.Codigo,
            Titulo = request.Titulo,
            Descricao = request.Descricao,
            Categoria = request.Categoria,
            OrgaoEmissor = request.OrgaoEmissor,
            DataVigencia = request.DataVigencia,
            Ativa = true,
            Requisitos = request.Requisitos.Select(r => new RequisitoNorma
            {
                Descricao = r.Descricao,
                Peso = r.Peso,
                Criticidade = r.Criticidade
            }).ToList()
        };

        _context.Normas.Add(norma);
        await _context.SaveChangesAsync(ct);

        return new NormaResponse
        {
            Id = norma.Id,
            Codigo = norma.Codigo,
            Titulo = norma.Titulo,
            Descricao = norma.Descricao,
            Categoria = norma.Categoria.ToString(),
            OrgaoEmissor = norma.OrgaoEmissor,
            Ativa = norma.Ativa,
            DataVigencia = norma.DataVigencia,
            QuantidadeRequisitos = norma.Requisitos.Count,
            Requisitos = norma.Requisitos.Select(r => new RequisitoResponse
            {
                Id = r.Id,
                Descricao = r.Descricao,
                Peso = r.Peso,
                Criticidade = r.Criticidade.ToString()
            }).ToList()
        };
    }
}
