namespace BrosCode.LastCall.Entity.Identity;

/// <summary>
/// Provides the current user identity for auditing persistence operations.
/// Lives in the Entity layer and is supplied by the hosting layer via DI.
/// </summary>
public interface ICurrentUserAccessor
{
    /// <summary>
    /// Gets the current user identifier or name for audit fields.
    /// </summary>
    string? UserNameOrId { get; }
}
