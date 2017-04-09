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
    int GetCd();
    SkillType GetSkillType();
    int GetTime();
    double GetMoveSpeed();
    double GetRange();
    double GetObstacleRadius();
    UnitType[] GetEffectTargetType();
    SkillEffectTarget GetEffectTarget();
    double GetEffectRadius();
    SkillEffect GetEffect();
    int[] GetEffectData();
}
