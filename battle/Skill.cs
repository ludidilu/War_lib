using RVO;
using System.IO;
using System.Collections.Generic;

public class Skill
{
    public ISkillSDS sds { private set; get; }

    public Vector2 startPos { private set; get; }

    public Vector2 targetPos { private set; get; }

    protected Battle battle;

    protected Simulator simulator;

    public int uid { get; protected set; }

    public int id { get; protected set; }

    public Vector2 pos
    {
        private set
        {
            simulator.setAgentPosition(uid, value);
        }

        get
        {
            Vector2 nowPos;

            if (sds.GetSkillType() == SkillType.ISOLATE && sds.GetObstacleRadius() == 0)
            {
                Vector2 pref = targetPos - startPos;

                if (sds.GetMoveSpeed() > 0)
                {
                    nowPos = startPos + sds.GetMoveSpeed() * simulator.getTimeStep() * (battle.roundNum - startRoundNum) * pref.normalized;
                }
                else
                {
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
            else if (sds.GetSkillType() == SkillType.ATTACH_TO_UNIT || sds.GetSkillType() == SkillType.CONTROL_UNIT)
            {
                nowPos = unit.pos;
            }
            else
            {
                nowPos = simulator.getAgentPosition(uid);
            }

            return nowPos;
        }
    }

    protected Vector2 velocity
    {
        set
        {
            simulator.setAgentVelocity(uid, value);
        }

        get
        {
            return simulator.getAgentVelocity(uid);
        }
    }

    protected Vector2 prefVelocity
    {
        set
        {
            simulator.setAgentPrefVelocity(uid, value);
        }

        get
        {
            return simulator.getAgentPrefVelocity(uid);
        }
    }

    public int startRoundNum { private set; get; }

    public Unit unit { private set; get; }

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

        unit.CastSkill();

        if (sds.GetObstacleRadius() > 0)
        {
            if (sds.GetMoveSpeed() > 0)
            {
                simulator.addAgent(uid, startPos);

                prefVelocity = targetPos - startPos;
            }
            else
            {
                Vector2 nowPos;

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

                simulator.addAgent(uid, nowPos);
            }

            InitSds();
        }
    }

    internal void Init(Battle _battle, Simulator _simulator, BinaryReader _br)
    {
        battle = _battle;

        simulator = _simulator;

        uid = _br.ReadInt32();

        id = _br.ReadInt32();

        sds = Battle.getSkillCallBack(id);

        startRoundNum = _br.ReadInt32();

        int unitUid = _br.ReadInt32();

        unit = battle.unitDic[unitUid];

        if (sds.GetObstacleRadius() > 0)
        {
            double x = _br.ReadDouble();

            double y = _br.ReadDouble();

            simulator.addAgent(uid, new Vector2(x, y));

            x = _br.ReadDouble();

            y = _br.ReadDouble();

            velocity = new Vector2(x, y);

            x = _br.ReadDouble();

            y = _br.ReadDouble();

            prefVelocity = new Vector2(x, y);

            InitSds();
        }
        else if (sds.GetSkillType() == SkillType.ISOLATE)
        {
            double x = _br.ReadDouble();

            double y = _br.ReadDouble();

            startPos = new Vector2(x, y);

            x = _br.ReadDouble();

            y = _br.ReadDouble();

            targetPos = new Vector2(x, y);
        }
        else if (sds.GetSkillType() == SkillType.CONTROL_UNIT)
        {
            double x = _br.ReadDouble();

            double y = _br.ReadDouble();

            targetPos = new Vector2(x, y);
        }
    }

    internal void Update()
    {
        if (sds.GetSkillType() == SkillType.CONTROL_UNIT)
        {
            if (sds.GetMoveSpeed() > 0)
            {
                unit.SetControledBySkill(targetPos - unit.pos, sds.GetMoveSpeed());
            }
            else
            {
                Vector2 pref = targetPos - unit.pos;

                double dis = pref.magnitude;

                if (dis > sds.GetRange())
                {
                    pref = pref.normalized * sds.GetRange();
                }

                unit.SetControledBySkill(pref, double.MaxValue);
            }
        }

        TakeEffect();
    }

    internal bool CheckEnd()
    {
        if (battle.roundNum - startRoundNum == sds.GetTime())
        {
            if (sds.GetSkillType() == SkillType.CONTROL_UNIT)
            {
                if (battle.unitDic.ContainsKey(unit.uid))
                {
                    unit.SetUncontroledBySkill();
                }
            }
            else if (sds.GetObstacleRadius() > 0)
            {
                simulator.delAgent(uid);
            }

            return true;
        }

        if (sds.GetSkillType() == SkillType.CONTROL_UNIT)
        {
            if (!battle.unitDic.ContainsKey(unit.uid))
            {
                return true;
            }
        }
        else if (sds.GetSkillType() == SkillType.ATTACH_TO_UNIT)
        {
            if (!battle.unitDic.ContainsKey(unit.uid))
            {
                return true;
            }
        }

        return false;
    }

    private void InitSds()
    {
        simulator.setAgentIsMine(uid, unit.isMine);
        simulator.setAgentRadius(uid, sds.GetObstacleRadius());

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

    private void TakeEffect()
    {
        List<int> tmpList = simulator.computePointNeightbors(pos, sds.GetEffectRadius());

        for (int i = 0; i < tmpList.Count; i++)
        {
            Unit target = battle.unitDic[tmpList[i]];

            if (CheckTarget(target))
            {
                Effect(target);
            }
        }
    }

    private bool CheckTarget(Unit _target)
    {
        if (sds.GetEffectTarget() == SkillEffectTarget.MY_UNITS && _target.isMine != unit.isMine)
        {
            return false;
        }

        if (sds.GetEffectTarget() == SkillEffectTarget.OPP_UNITS && _target.isMine == unit.isMine)
        {
            return false;
        }

        if (sds.GetEffectTargetType() == UnitTargetType.GROUND_UNIT && _target.sds.GetIsAirUnit())
        {
            return false;
        }

        if (sds.GetEffectTargetType() == UnitTargetType.AIR_UNIT && !_target.sds.GetIsAirUnit())
        {
            return false;
        }

        return true;
    }

    private void Effect(Unit _target)
    {
        switch (sds.GetEffect())
        {
            case SkillEffect.DAMAGE:

                _target.BeDamage(sds.GetEffectData()[0]);

                break;
        }
    }

    internal void WriteData(BinaryWriter _bw)
    {
        _bw.Write(uid);

        _bw.Write(id);

        _bw.Write(startRoundNum);

        _bw.Write(unit.uid);

        if (sds.GetObstacleRadius() > 0)
        {
            Vector2 tmpPos = pos;

            _bw.Write(tmpPos.x);

            _bw.Write(tmpPos.y);

            _bw.Write(velocity.x);

            _bw.Write(velocity.y);

            _bw.Write(prefVelocity.x);

            _bw.Write(prefVelocity.y);
        }
        else if (sds.GetSkillType() == SkillType.ISOLATE)
        {
            _bw.Write(startPos.x);

            _bw.Write(startPos.y);

            _bw.Write(targetPos.x);

            _bw.Write(targetPos.y);
        }
        else if (sds.GetSkillType() == SkillType.CONTROL_UNIT)
        {
            _bw.Write(targetPos.x);

            _bw.Write(targetPos.y);
        }
    }
}
