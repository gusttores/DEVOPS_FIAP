namespace GovAmbiental.API.Common;

/// <summary>Recurso não encontrado (mapeada para HTTP 404 pelo middleware).</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

/// <summary>Violação de regra de negócio (mapeada para HTTP 422 pelo middleware).</summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
}
