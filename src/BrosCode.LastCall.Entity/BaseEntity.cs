namespace BrosCode.LastCall.Entity;

public abstract class BaseEntity
{
    public Guid Id { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; }

    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public uint? RowVersion { get; set; }
}
