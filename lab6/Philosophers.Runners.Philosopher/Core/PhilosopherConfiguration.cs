namespace Interface;

sealed public class PhilosopherConfiguration
{
    public int EatingTimeMax { get; set; }
    public int EatingTimeMin { get; set; }
    public int TakeForkTimeMax { get;  set; }
    public int TakeForkTimeMin { get;  set; }
    public int ThinkingTimeMax { get; set; }
    public int ThinkingTimeMin { get; set; }
    public required string Name { get; set; }
}
