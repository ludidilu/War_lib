﻿using RVO;
using System.IO;
using System.Collections.Generic;
using System;

public class Unit
{
    protected Battle battle;

    protected Simulator simulator;

    public bool isMine { get; protected set; }

    public int uid { get; protected set; }

    public int id { get; protected set; }

    public IUnitSDS sds { get; protected set; }

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

    public int nowHp { protected set; get; }

    public double attackStep { protected set; get; }

    public int targetUid { protected set; get; }

    public int skillCd { protected set; get; }

    private bool isBase;

    private bool controledBySkill = false;

    internal void Init(Battle _battle, Simulator _simulator, bool _isMine, int _uid, int _id, bool _isBase, IUnitSDS _sds, Vector2 _pos)
    {
        battle = _battle;
        simulator = _simulator;
        isMine = _isMine;
        uid = _uid;
        id = _id;
        isBase = _isBase;
        sds = _sds;
        nowHp = sds.GetHp();
        attackStep = 0;
        targetUid = -1;

        if (sds.GetSkill() != 0)
        {
            CastSkill();
        }

        simulator.addAgent(uid, _pos);
        InitSds();
    }

    internal void Init(Battle _battle, Simulator _simulator, BinaryReader _br)
    {
        battle = _battle;
        simulator = _simulator;

        uid = _br.ReadInt32();

        isMine = _br.ReadBoolean();

        id = _br.ReadInt32();

        isBase = _br.ReadBoolean();

        sds = Battle.getUnitCallBack(id);

        double x = _br.ReadDouble();

        double y = _br.ReadDouble();

        simulator.addAgent(uid, new Vector2(x, y));

        InitSds();

        x = _br.ReadDouble();

        y = _br.ReadDouble();

        velocity = new Vector2(x, y);

        x = _br.ReadDouble();

        y = _br.ReadDouble();

        prefVelocity = new Vector2(x, y);

        nowHp = _br.ReadInt32();

        attackStep = _br.ReadDouble();

        targetUid = _br.ReadInt32();

        if (sds.GetSkill() != 0)
        {
            skillCd = _br.ReadInt32();
        }
    }

    protected void InitSds()
    {
        simulator.setAgentIsMine(uid, isMine);
        simulator.setAgentMaxSpeed(uid, sds.GetMoveSpeed());
        simulator.setAgentRadius(uid, sds.GetRadius());
        simulator.setAgentWeight(uid, sds.GetWeight());

        if (sds.GetUnitType() == UnitType.AIR_UNIT)
        {
            simulator.setAgentType(uid, AgentType.AirUnit);
        }
        else if (sds.GetUnitType() == UnitType.GROUND_UNIT)
        {
            simulator.setAgentType(uid, AgentType.GroundUnit);
        }
        else
        {
            simulator.setAgentType(uid, AgentType.Building);
        }
    }

    internal void WriteData(BinaryWriter _bw)
    {
        _bw.Write(uid);

        _bw.Write(isMine);

        _bw.Write(id);

        _bw.Write(isBase);

        _bw.Write(pos.x);

        _bw.Write(pos.y);

        _bw.Write(velocity.x);

        _bw.Write(velocity.y);

        _bw.Write(prefVelocity.x);

        _bw.Write(prefVelocity.y);

        _bw.Write(nowHp);

        _bw.Write(attackStep);

        _bw.Write(targetUid);

        if (sds.GetSkill() != 0)
        {
            _bw.Write(skillCd);
        }
    }

    internal void BeDamage(int _damage)
    {
        nowHp -= _damage;
    }

    internal void SetControledBySkill(Vector2 _prefVelocity, double _maxSpeed)
    {
        targetUid = -1;

        prefVelocity = _prefVelocity;

        simulator.setAgentMaxSpeed(uid, _maxSpeed);

        controledBySkill = true;
    }

    internal void SetUncontroledBySkill()
    {
        simulator.setAgentMaxSpeed(uid, sds.GetMoveSpeed());

        controledBySkill = false;
    }

    internal void CastSkill()
    {
        ISkillSDS skillSDS = Battle.getSkillCallBack(sds.GetSkill());

        skillCd = skillSDS.GetCd();
    }

    internal virtual void Update(bool _isClient, ref Dictionary<int, LinkedList<int>> _clientAttackData)
    {
        if (attackStep > 0)
        {
            attackStep -= Battle.gameConfig.GetTimeStep();

            if (attackStep < 0)
            {
                attackStep = 0;
            }
        }

        if (skillCd > 0)
        {
            skillCd--;
        }

        if (controledBySkill)
        {
            return;
        }

        if (targetUid != -1)
        {
            if (battle.unitDic.ContainsKey(targetUid))
            {
                Unit targetUnit = battle.unitDic[targetUid];

                if (targetUnit.IsAlive())
                {
                    if (Vector2.Distance(targetUnit.pos, pos) - targetUnit.sds.GetRadius() < sds.GetAttackRange())
                    {
                        if (attackStep == 0)
                        {
                            DamageTarget(targetUnit, _isClient, ref _clientAttackData);
                        }

                        prefVelocity = Vector2.zero;

                        return;
                    }
                    else
                    {
                        targetUid = -1;
                    }
                }
                else
                {
                    targetUid = -1;
                }
            }
            else
            {
                targetUid = -1;
            }
        }

        int resultUid = -1;

        double distance = sds.GetVisionRange();

        simulator.getNearestAgent(uid, ref resultUid, 0, ref distance, CheckTarget);

        if (resultUid != -1)
        {
            Unit targetUnit = battle.unitDic[resultUid];

            if (distance < sds.GetAttackRange())
            {
                targetUid = resultUid;

                if (attackStep == 0)
                {
                    DamageTarget(targetUnit, _isClient, ref _clientAttackData);
                }

                prefVelocity = Vector2.zero;
            }
            else if (sds.GetUnitType() != UnitType.BUILDING)
            {
                prefVelocity = targetUnit.pos - pos;
            }
        }
        else if (sds.GetUnitType() != UnitType.BUILDING)
        {
            if (isMine)
            {
                prefVelocity = new Vector2(10000, 0);
            }
            else
            {
                prefVelocity = new Vector2(-10000, 0);
            }
        }
    }

    internal bool IsAlive()
    {
        return nowHp > 0;
    }

    protected void DamageTarget(Unit _targetUnit, bool _isClient, ref Dictionary<int, LinkedList<int>> _clientAttackData)
    {
        LinkedList<int> result = null;

        if (_isClient)
        {
            if (_clientAttackData == null)
            {
                _clientAttackData = new Dictionary<int, LinkedList<int>>();

            }

            result = new LinkedList<int>();

            _clientAttackData.Add(uid, result);
        }

        attackStep = sds.GetAttackStep();

        switch (sds.GetAttackType())
        {
            case UnitAttackType.SINGLE:

                _targetUnit.BeDamage(sds.GetAttackDamage());

                if (_isClient)
                {
                    result.AddLast(_targetUnit.uid);
                }

                break;

            case UnitAttackType.SELF_AREA:

                List<int> tmpTargetUids = simulator.computePointNeightbors(pos, sds.GetAttackRange());

                for (int i = 0; i < tmpTargetUids.Count; i++)
                {
                    int tmpUid = tmpTargetUids[i];

                    if (CheckTarget(tmpUid))
                    {
                        Unit tmpUnit = battle.unitDic[tmpUid];

                        tmpUnit.BeDamage(sds.GetAttackDamage());

                        if (_isClient)
                        {
                            result.AddLast(tmpUnit.uid);
                        }
                    }
                }

                break;

            case UnitAttackType.TARGET_AREA:

                tmpTargetUids = simulator.computePointNeightbors(_targetUnit.pos, sds.GetAttackTypeData());

                for (int i = 0; i < tmpTargetUids.Count; i++)
                {
                    int tmpUid = tmpTargetUids[i];

                    if (CheckTarget(tmpUid))
                    {
                        Unit tmpUnit = battle.unitDic[tmpUid];

                        tmpUnit.BeDamage(sds.GetAttackDamage());

                        if (_isClient)
                        {
                            result.AddLast(tmpUnit.uid);
                        }
                    }
                }

                break;

            case UnitAttackType.CONE_AREA:

                _targetUnit.BeDamage(sds.GetAttackDamage());

                Vector2 v = _targetUnit.pos - pos;

                tmpTargetUids = simulator.computePointNeightbors(pos, sds.GetAttackRange());

                for (int i = 0; i < tmpTargetUids.Count; i++)
                {
                    int tmpUid = tmpTargetUids[i];

                    if (tmpUid != _targetUnit.uid)
                    {
                        if (CheckTarget(tmpUid))
                        {
                            Unit tmpUnit = battle.unitDic[tmpUid];

                            double angle = Vector2.Angle(v, tmpUnit.pos - pos);

                            double radiusAngle = Math.Asin(tmpUnit.sds.GetRadius() / Vector2.Distance(pos, tmpUnit.pos));

                            if (angle - radiusAngle < sds.GetAttackTypeData())
                            {
                                tmpUnit.BeDamage(sds.GetAttackDamage());

                                if (_isClient)
                                {
                                    result.AddLast(tmpUnit.uid);
                                }
                            }
                        }
                    }
                }

                break;
        }
    }

    protected bool CheckTarget(int _uid)
    {
        Unit unit = battle.unitDic[_uid];

        if (!unit.IsAlive() || unit.isMine == isMine)
        {
            return false;
        }

        if (Array.IndexOf(sds.GetTargetType(), unit.sds.GetUnitType()) == -1)
        {
            return false;
        }

        return true;
    }

    internal bool Die()
    {
        simulator.delAgent(uid);

        return isBase;
    }

    internal void CheckHpOverflow()
    {
        if (nowHp > sds.GetHp())
        {
            nowHp = sds.GetHp();
        }
    }
}
