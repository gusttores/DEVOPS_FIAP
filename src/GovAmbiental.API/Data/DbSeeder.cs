using GovAmbiental.API.Models;
using Microsoft.EntityFrameworkCore;

namespace GovAmbiental.API.Data;

/// <summary>
/// Popula o banco com normas/requisitos de referência e uma auditoria de exemplo,
/// para que as listagens já retornem dados (inclusive nos testes de status 200).
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Normas.AnyAsync())
            return;

        var normas = new List<NormaAmbiental>
        {
            new()
            {
                Codigo = "CONAMA 430/2011",
                Titulo = "Condições e padrões de lançamento de efluentes",
                Descricao = "Dispõe sobre condições e padrões de lançamento de efluentes em corpos d'água.",
                Categoria = CategoriaAmbiental.Agua,
                OrgaoEmissor = "CONAMA",
                DataVigencia = new DateTime(2011, 5, 13),
                Requisitos = new List<RequisitoNorma>
                {
                    new() { Descricao = "pH do efluente entre 5 e 9", Peso = 3, Criticidade = Criticidade.Alta },
                    new() { Descricao = "Monitoramento mensal de DBO", Peso = 2, Criticidade = Criticidade.Media },
                    new() { Descricao = "Ausência de materiais flutuantes", Peso = 1, Criticidade = Criticidade.Baixa }
                }
            },
            new()
            {
                Codigo = "CONAMA 003/1990",
                Titulo = "Padrões de qualidade do ar",
                Descricao = "Estabelece padrões nacionais de qualidade do ar.",
                Categoria = CategoriaAmbiental.Ar,
                OrgaoEmissor = "CONAMA",
                DataVigencia = new DateTime(1990, 6, 28),
                Requisitos = new List<RequisitoNorma>
                {
                    new() { Descricao = "Material particulado dentro do limite legal", Peso = 4, Criticidade = Criticidade.Critica },
                    new() { Descricao = "Inventário anual de emissões atmosféricas", Peso = 2, Criticidade = Criticidade.Media }
                }
            },
            new()
            {
                Codigo = "ISO 14001:2015 - 8.1",
                Titulo = "Controle operacional de resíduos",
                Descricao = "Requisito de controle operacional do sistema de gestão ambiental.",
                Categoria = CategoriaAmbiental.Residuos,
                OrgaoEmissor = "ISO",
                DataVigencia = new DateTime(2015, 9, 15),
                Requisitos = new List<RequisitoNorma>
                {
                    new() { Descricao = "Segregação de resíduos perigosos", Peso = 4, Criticidade = Criticidade.Critica },
                    new() { Descricao = "Manifesto de transporte de resíduos atualizado", Peso = 3, Criticidade = Criticidade.Alta },
                    new() { Descricao = "Destinação para aterro licenciado", Peso = 3, Criticidade = Criticidade.Alta }
                }
            }
        };

        await context.Normas.AddRangeAsync(normas);
        await context.SaveChangesAsync();

        // Auditoria de exemplo já avaliada (parcialmente conforme), com 1 não-conformidade.
        var requisitosAgua = normas[0].Requisitos.ToList();
        var auditoria = new Auditoria
        {
            Titulo = "Auditoria ETE - Planta Industrial Sul",
            UnidadeOperacional = "Planta Industrial Sul",
            Responsavel = "Equipe SGA",
            DataInicio = DateTime.UtcNow.AddDays(-10),
            DataConclusao = DateTime.UtcNow.AddDays(-8),
            Status = StatusAuditoria.Concluida,
            ScoreConformidade = 66.67m,
            Resultado = ResultadoConformidade.ParcialmenteConforme,
            Itens = new List<ItemAuditoria>
            {
                new() { RequisitoNormaId = requisitosAgua[0].Id, Atendido = false, Observacao = "pH medido em 4,2", Evidencia = "laudo-ph-2026.pdf" },
                new() { RequisitoNormaId = requisitosAgua[1].Id, Atendido = true, Evidencia = "relatorio-dbo.pdf" },
                new() { RequisitoNormaId = requisitosAgua[2].Id, Atendido = true }
            },
            NaoConformidades = new List<NaoConformidade>
            {
                new()
                {
                    RequisitoNormaId = requisitosAgua[0].Id,
                    Descricao = "Requisito não atendido: pH do efluente entre 5 e 9",
                    Criticidade = Criticidade.Alta,
                    AcaoCorretiva = new AcaoCorretiva
                    {
                        Descricao = "Plano de ação para: pH do efluente entre 5 e 9",
                        Responsavel = "Equipe SGA",
                        PrazoLimite = DateTime.UtcNow.AddDays(15),
                        Status = StatusAcaoCorretiva.Pendente
                    }
                }
            }
        };

        await context.Auditorias.AddAsync(auditoria);
        await context.SaveChangesAsync();
    }
}
