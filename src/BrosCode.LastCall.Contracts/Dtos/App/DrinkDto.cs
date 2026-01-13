namespace BrosCode.LastCall.Contracts.Dtos.App;

public sealed class DrinkDto : BaseDto
{
    public string Name { get; set; } = string.Empty;

    public decimal BasePrice { get; set; }
}