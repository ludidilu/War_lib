public enum UnitAttackType
{
    SINGLE,
    SELF_AREA,
    TARGET_AREA,
    CONE_AREA,
}

public enum UnitTargetType
{
    GROUND_UNIT,
    AIR_UNIT,
    BOTH
}

public interface IUnitSDS
{
    double GetMoveSpeed();
    double GetRadius();
    int GetWeight();
    double GetQueuePos();
    int GetHp();
    double GetVisionRange();
    double GetAttackRange();
    int GetAttackDamage();
    double GetAttackStep();
    bool GetIsAirUnit();
    UnitTargetType GetTargetType();
    UnitAttackType GetAttackType();
    double GetAttackTypeData();
    int GetPrize();
    bool GetIsHero();
    int GetSkill();
}
