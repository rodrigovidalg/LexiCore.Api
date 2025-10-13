namespace Lexico.Application.Contracts
{
    public interface ICurrentUser
    {
        string? UserId { get; }
        string? Email { get; }
        bool IsAuthenticated { get; }
    }
}
