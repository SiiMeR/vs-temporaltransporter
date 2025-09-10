namespace TemporalTransporter;

public class TemporalTransporterConfig
{
    public int InterceptorRadius { get; set; } = 25;
    public int SendCooldownSeconds { get; set; } = 10;
    public int OnlyUsableAboveYLevel { get; set; } = 50;

    public int ChargesPerGear { get; set; } = 1;
}