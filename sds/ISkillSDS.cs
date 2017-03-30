public enum SkillType
{
    ATTACH_TO_UNIT,
    CONTROL_UNIT,
    ISOLATE,
}

public enum SkillEffect
{
    DAMAGE,
}

public enum SkillEffectTarget
{
    MY_UNITS,
    OPP_UNITS,
    BOTH
}

public interface ISkillSDS
{
    SkillType GetSkillType();
    int GetTime();
    double GetMoveSpeed();
    double GetRange();
    double GetObstacleRadius();
    double GetEffectRadius();
    SkillEffect GetSkillEffect();
    SkillEffectTarget GetSkillEffectTarget();
    UnitTargetType GetTargetType();
    int[] GetSkillData();
}
