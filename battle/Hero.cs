using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    internal override void Update()
    {

    }
}
