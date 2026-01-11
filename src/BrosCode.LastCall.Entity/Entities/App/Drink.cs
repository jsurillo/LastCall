using BrosCode.LastCall.Entity;

namespace BrosCode.LastCall.Entity.Entities.App;

public sealed class Drink : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public decimal BasePrice { get; set; }
}
