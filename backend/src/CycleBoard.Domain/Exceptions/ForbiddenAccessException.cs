namespace CycleBoard.Domain.Exceptions;

public sealed class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException(string message = "You do not have permission to perform this action.")
        : base(message) { }
}
