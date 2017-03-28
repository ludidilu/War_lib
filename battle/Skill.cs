using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RVO;


public class Skill
{
    public ISkillSDS sds { private set; get; }

    public Vector2 startPos { private set; get; }

    public Vector2 endPos { private set; get; }

    protected Battle battle;

    protected Simulator simulator;

    public int uid { get; protected set; }

    public int heroUid { get; protected set; }

    public int id { get; protected set; }

    public Vector2 pos
    {
        protected set
        {
            simulator.setAgentPosition(uid, value);
        }

        get
        {
            return simulator.getAgentPosition(uid);
        }
    }

    public Vector2 velocity
    {
        protected set
        {
            simulator.setAgentVelocity(uid, value);
        }

        get
        {
            return simulator.getAgentVelocity(uid);
        }
    }

    public Vector2 prefVelocity
    {
        protected set
        {
            simulator.setAgentPrefVelocity(uid, value);
        }

        get
        {
            return simulator.getAgentPrefVelocity(uid);
        }
    }

    public Vector2 targetPos { private set; get; }

    public int startRoundNum { private set; get; }

    private Unit unit;

    internal void Init(Battle _battle, Simulator _simulator, int _roundNum, int _uid, int _id, ISkillSDS _sds, Unit _unit, Vector2 _targetPos)
    {
        battle = _battle;

        simulator = _simulator;

        startRoundNum = _roundNum;

        uid = _uid;

        id = _id;

        sds = _sds;

        unit = _unit;

        startPos = unit.pos;

        targetPos = _targetPos;

        if (sds.GetSkillType() == SkillType.ISOLATE_WITH_OBSTACLE)
        {
            simulator.addAgent(uid, startPos);
            InitSds();
            prefVelocity = targetPos - startPos;
        }
    }

    internal bool Update(int _roundNum)
    {
        if (_roundNum - startRoundNum > sds.GetTime())
        {
            return true;
        }

        if (sds.GetSkillType() == SkillType.CONTROL_UNIT)
        {
            if (!battle.unitDic.ContainsKey(unit.uid))
            {
                return true;
            }

            unit.SetControledBySkill(targetPos - unit.pos);
        }
        else if (sds.GetSkillType() == SkillType.ATTACK_TO_UNIT)
        {
            if (!battle.unitDic.ContainsKey(unit.uid))
            {
                return true;
            }
        }

        TakeEffect(_roundNum);

        return false;
    }

    private void InitSds()
    {
        simulator.setAgentIsMine(uid, unit.isMine);
        simulator.setAgentRadius(uid, sds.GetRadius());

        if (sds.GetMoveSpeed() > 0)
        {
            simulator.setAgentType(uid, AgentType.SkillUnit);
            simulator.setAgentMaxSpeed(uid, sds.GetMoveSpeed());
        }
        else
        {
            simulator.setAgentType(uid, AgentType.SkillObstacle);
        }
    }

    private void TakeEffect(int _roundNum)
    {
        Vector2 nowPos;

        if (sds.GetSkillType() == SkillType.ISOLATE_WITHOUT_OBSTACLE)
        {
            if (sds.GetMoveSpeed() > 0)
            {
                nowPos = startPos + sds.GetMoveSpeed() * simulator.getTimeStep() * (_roundNum - startRoundNum) * (targetPos - startPos); 
            }
            else
            {
                Vector2 pref = targetPos - startPos;

                double dis = pref.magnitude;

                if (dis > sds.GetRange())
                {
                    nowPos = startPos + pref.normalized * sds.GetRange();
                }
                else
                {
                    nowPos = targetPos;
                }
            }
        }
        else if(sds.GetSkillType() == SkillType.ATTACK_TO_UNIT || sds.GetSkillType() == SkillType.CONTROL_UNIT)
        {
            nowPos = unit.pos;
        }
        else
        {
            nowPos = pos;
        }
    }
}
