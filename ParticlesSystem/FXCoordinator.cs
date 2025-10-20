
namespace Raylib_cs_fx;

/// <summary>
/// An FX coordination system similar to city of heroes where FX can happen at specific times or on specific schedules
/// </summary>
public class FXCoordinator
{
    List<FXCondition> Conditions = new List<FXCondition>();
}


public enum TriggerConditionType
{
    Time,
    Cycle
}

public class FXCondition
{
    public float Time;
    public TriggerConditionType Type;
    public List<FXEvent> Events = new List<FXEvent>();
}

public class FXEvent
{
    public required string Name;
    public ParticleSystem? ParticleSystem;
    public Sound? Sound; //if a sound gets played
}
