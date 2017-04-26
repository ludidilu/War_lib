using RVO;


public class Hero : Unit
{
    internal bool castSkill { private set; get; }

    internal Vector2 skillPos { private set; get; }

    internal void CastSkill(Vector2 _pos)
    {
        castSkill = true;

        skillPos = _pos;
    }
}
