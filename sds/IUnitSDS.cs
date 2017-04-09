public enum UnitAttackType
{
    SINGLE,
    SELF_AREA,
    TARGET_AREA,
    CONE_AREA,
}

public enum UnitType
{
    GROUND_UNIT,
    AIR_UNIT,
    BUILDING
}

public interface IUnitSDS
{
    UnitType GetUnitType();
    double GetMoveSpeed();
    double GetRadius();
    int GetWeight();
    double GetQueuePos();
    int GetHp();
    double GetVisionRange();
    double GetAttackRange();
    int GetAttackDamage();
    double GetAttackStep();
    UnitType[] GetTargetType();
    UnitAttackType GetAttackType();
    double GetAttackTypeData();
    int GetPrize();
    bool GetIsHero();
    int GetSkill();
    int GetSpawnSkill();
}
