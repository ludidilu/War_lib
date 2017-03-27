public enum SkillState
{ 
    ATTACH_TO_HERO,
    DRAG_WITH_HERO,
    ISOLATE,
}

public interface ISkillSDS
{
    int GetTime();
    SkillState GetSkillState();
    double GetSpeed();
    double GetRadius();
    double GetRange();
    bool GetReachTarget();
    bool GetIsObstacle();
}
