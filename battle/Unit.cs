using RVO;

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

    public void Init(Battle _battle, Simulator _simulator, bool _isMine, int _uid, int _id, IUnitSDS _sds, Vector2 _pos)
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

    public void SetTargetPos(Vector2 _targetPos)
    {
        prefVelocity = _targetPos - pos;
    }
}
