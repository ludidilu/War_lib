public enum SkillType
{
    ATTACK_TO_UNIT,
    CONTROL_UNIT,
    ISOLATE_WITH_OBSTACLE,
    ISOLATE_WITHOUT_OBSTACLE
}

public interface ISkillSDS
{
    SkillType GetSkillType();
    int GetTime();
    double GetMoveSpeed();
    double GetRange();
    double GetRadius();
}
