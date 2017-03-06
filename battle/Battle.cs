using System;
using System.Collections.Generic;
using RVO;

internal class PoolUnit
{
    internal double queuePos;
    internal int id;
    internal int num;

    internal PoolUnit(double _queuePos, int _id, int _num)
    {
        queuePos = _queuePos;
        id = _id;
        num = _num;
    }
}

public class Battle
{
    internal Func<int, IUnitSDS> getUnitCallBack;

    public Dictionary<int, Unit> unitDic = new Dictionary<int, Unit>();

    private Dictionary<int, PoolUnit> mPoolDic = new Dictionary<int, PoolUnit>();

    private Dictionary<int, PoolUnit> oPoolDic = new Dictionary<int, PoolUnit>();

    private Rect mapBounds;

    private int uid;

    private Simulator simulator;

    public void Init(double _timeStep, Rect _mapBounds, double _maxRadius, double _mapBoundFix, Func<int, IUnitSDS> _getUnitCallBack)
    {
        getUnitCallBack = _getUnitCallBack;

        mapBounds = _mapBounds;

        simulator = new Simulator();

        simulator.setAgentDefaults(100.0, 10, 0.01, 0.01, 20, 0.0, Vector2.zero);

        simulator.setTimeStep(_timeStep);

        simulator.setMapBounds(mapBounds);

        simulator.setMaxRadius(_maxRadius);

        simulator.setMapBoundFix(_mapBoundFix);
    }

    public void AddUnitToPool(bool _isMine, int _id)
    {
        IUnitSDS sds = getUnitCallBack(_id);

        Dictionary<int, PoolUnit> pool = _isMine ? mPoolDic : oPoolDic;

        if (pool.ContainsKey(_id))
        {
            PoolUnit unit = pool[_id];

            unit.num++;
        }
        else
        {
            PoolUnit unit = new PoolUnit(sds.GetQueuePos(), _id, 1);

            pool.Add(_id, unit);
        }
    }

    public void Spawn()
    {
        Dictionary<int, PoolUnit>.ValueCollection.Enumerator enumerator = mPoolDic.Values.GetEnumerator();

        while (enumerator.MoveNext())
        {
            PoolUnit unit = enumerator.Current;

            for (int i = 0; i < unit.num; i++)
            {
                double posX = -70 + unit.queuePos;

                double posY = (i % 2 == 0 ? 1 : -1) * i * 0.1;

                Vector2 pos = new Vector2(posX, posY);

                AddUnitToBattle(true, unit.id, pos);
            }
        }

        enumerator = oPoolDic.Values.GetEnumerator();

        while (enumerator.MoveNext())
        {
            PoolUnit unit = enumerator.Current;

            for (int i = 0; i < unit.num; i++)
            {
                double posX = 70 - unit.queuePos;

                double posY = (i % 2 == 0 ? 1 : -1) * i * 0.1;

                Vector2 pos = new Vector2(posX, posY);

                AddUnitToBattle(false, unit.id, pos);
            }
        }
    }

    public Unit AddUnitToBattle(bool _isMine, int _id, Vector2 _pos)
    {
        Unit unit = new Unit();

        unit.Init(this, simulator, _isMine, GetUid(), _id, getUnitCallBack(_id), _pos);

        unitDic[unit.uid] = unit;

        return unit;
    }

    public void Update()
    {
        simulator.BuildAgentTree();

        simulator.doStepFinal();
    }

    private int GetUid()
    {
        uid++;
        return uid;
    }
}
