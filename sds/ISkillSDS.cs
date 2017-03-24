public enum SkillMoveType
{
    ATTACH,
    STAY,
    MOVE,
}

public interface ISkillSDS
{
    int GetTime();
    SkillMoveType GetMoveType();
    double GetRadius();
    double GetRange();
}
