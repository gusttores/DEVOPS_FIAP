namespace GovAmbiental.API.Models;

/// <summary>Categoria ambiental coberta por uma norma.</summary>
public enum CategoriaAmbiental
{
    Agua = 1,
    Ar = 2,
    Residuos = 3,
    Ruido = 4,
    Solo = 5,
    Biodiversidade = 6,
    RecursosHidricos = 7,
    Energia = 8
}

/// <summary>Grau de criticidade de um requisito ambiental.</summary>
public enum Criticidade
{
    Baixa = 1,
    Media = 2,
    Alta = 3,
    Critica = 4
}

/// <summary>Ciclo de vida de uma auditoria interna.</summary>
public enum StatusAuditoria
{
    Planejada = 1,
    EmAndamento = 2,
    Concluida = 3
}

/// <summary>Resultado consolidado da avaliação de conformidade.</summary>
public enum ResultadoConformidade
{
    NaoAvaliado = 0,
    Conforme = 1,
    ParcialmenteConforme = 2,
    NaoConforme = 3
}

/// <summary>Situação de um plano de ação corretiva.</summary>
public enum StatusAcaoCorretiva
{
    Pendente = 1,
    EmAndamento = 2,
    Concluida = 3
}
