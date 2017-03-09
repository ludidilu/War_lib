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
        set
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
        set
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
        set
        {
            simulator.setAgentPrefVelocity(uid, value);
        }

        get
        {
            return simulator.getAgentPrefVelocity(uid);
        }
    }

    internal void Init(Battle _battle, Simulator _simulator, bool _isMine, int _uid, int _id, IUnitSDS _sds, Vector2 _pos)
    {
        battle = _battle;
        simulator = _simulator;
        isMine = _isMine;
        uid = _uid;
        id = _id;
        sds = _sds;

        simulator.addAgent(uid, _pos);
        simulator.setAgentIsMine(uid, isMine);
        simulator.setAgentMaxSpeed(uid, sds.GetMoveSpeed());
        simulator.setAgentRadius(uid, sds.GetRadius());
        simulator.setAgentWeight(uid, sds.GetWeight());
    }

    internal void Init(Battle _battle,Simulator _simulator,BinaryReader _br)
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
    }

    public void SetTargetPos(Vector2 _targetPos)
    {
        prefVelocity = _targetPos - pos;
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
    }
}
