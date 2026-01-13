namespace BrosCode.LastCall.Contracts;

public abstract class BaseDto
{
    public Guid Id { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public uint? RowVersion { get; set; }
}
