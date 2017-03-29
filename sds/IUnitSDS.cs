public enum UnitAttackType
{
    SINGLE,
    SELF_AREA,
    TARGET_AREA,
    CONE_AREA,
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
    bool GetCanAttackAirUnit();
    bool GetCanAttackGroundUnit();
    UnitAttackType GetAttackType();
    double GetAttackTypeData();
    int GetPrize();
    bool GetIsHero();
    int GetSkill();
}
