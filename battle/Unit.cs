using RVO;
using System.IO;

public class Unit
{
    private Battle battle;

    private Simulator simulator;

    public bool isMine { get; private set; }

    public int uid { get; private set; }

    public int id { get; private set; }

    public IUnitSDS sds { get; private set; }

    public Vector2 pos
    {
        private set
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
        private set
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
        private set
        {
            simulator.setAgentPrefVelocity(uid, value);
        }

        get
        {
            return simulator.getAgentPrefVelocity(uid);
        }
    }

    public int nowHp { private set; get; }

    public double attackStep { private set; get; }

    public int targetUid { private set; get; }

    internal void Init(Battle _battle, Simulator _simulator, bool _isMine, int _uid, int _id, IUnitSDS _sds, Vector2 _pos)
    {
        battle = _battle;
        simulator = _simulator;
        isMine = _isMine;
        uid = _uid;
        id = _id;
        sds = _sds;
        nowHp = sds.GetHp();
        attackStep = 0;
        targetUid = -1;

        simulator.addAgent(uid, _pos);
        simulator.setAgentIsMine(uid, isMine);
        simulator.setAgentMaxSpeed(uid, sds.GetMoveSpeed());
        simulator.setAgentRadius(uid, sds.GetRadius());
        simulator.setAgentWeight(uid, sds.GetWeight());
    }

    internal void Init(Battle _battle, Simulator _simulator, BinaryReader _br)
    {
        battle = _battle;
        simulator = _simulator;

        uid = _br.ReadInt32();

        isMine = _br.ReadBoolean();

        id = _br.ReadInt32();

        sds = Battle.getUnitCallBack(id);

        double x = _br.ReadDouble();

        double y = _br.ReadDouble();

        simulator.addAgent(uid, new Vector2(x, y));
        simulator.setAgentIsMine(uid, isMine);
        simulator.setAgentMaxSpeed(uid, sds.GetMoveSpeed());
        simulator.setAgentRadius(uid, sds.GetRadius());
        simulator.setAgentWeight(uid, sds.GetWeight());

        x = _br.ReadDouble();

        y = _br.ReadDouble();

        velocity = new Vector2(x, y);

        x = _br.ReadDouble();

        y = _br.ReadDouble();

        prefVelocity = new Vector2(x, y);

        nowHp = _br.ReadInt32();

        attackStep = _br.ReadDouble();

        targetUid = _br.ReadInt32();
    }

    internal void WriteData(BinaryWriter _bw)
    {
        _bw.Write(uid);

        _bw.Write(isMine);

        _bw.Write(id);

        _bw.Write(pos.x);

        _bw.Write(pos.y);

        _bw.Write(velocity.x);

        _bw.Write(velocity.y);

        _bw.Write(prefVelocity.x);

        _bw.Write(prefVelocity.y);

        _bw.Write(nowHp);

        _bw.Write(attackStep);

        _bw.Write(targetUid);
    }

    internal void BeDamage(int _damage)
    {
        nowHp -= _damage;
    }

    internal void Update()
    {
        if (attackStep > 0)
        {
            attackStep -= Battle.gameConfig.GetTimeStep();

            if (attackStep < 0)
            {
                attackStep = 0;
            }
        }

        if (targetUid != -1)
        {
            if (battle.unitDic.ContainsKey(targetUid))
            {
                Unit targetUnit = battle.unitDic[targetUid];

                if (targetUnit.nowHp > 0)
                {
                    if (Vector2.Distance(targetUnit.pos, pos) - targetUnit.sds.GetRadius() < sds.GetAttackRange())
                    {
                        if (attackStep == 0)
                        {
                            attackStep = sds.GetAttackStep();

                            targetUnit.BeDamage(sds.GetAttackDamage());
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

        if(resultUid != -1)
        {
            Unit targetUnit = battle.unitDic[resultUid];

            if (distance < sds.GetAttackRange())
            {
                targetUid = resultUid;

                if (attackStep == 0)
                {
                    attackStep = sds.GetAttackStep();

                    targetUnit.BeDamage(sds.GetAttackDamage());
                }

                prefVelocity = Vector2.zero;
            }
            else
            {
                prefVelocity = targetUnit.pos - pos;
            }

            return;
        }

        if (isMine)
        {
            prefVelocity = new Vector2(1000, 0);
        }
        else
        {
            prefVelocity = new Vector2(-1000, 0);
        }
    }

    private bool CheckTarget(int _uid)
    {
        Unit unit = battle.unitDic[_uid];

        return unit.isMine != isMine;
    }

    internal void Die()
    {
        simulator.delAgent(uid);
    }
}
