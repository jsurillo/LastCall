namespace BrosCode.LastCall.Entity;

public sealed class Drink : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public decimal BasePrice { get; set; }
}
