using System;
using System.Collections.Generic;
using RVO;
using System.IO;

class PoolUnit
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

class CommandData
{

}

enum C2SCommand
{
    REFRESH,
    ACTION,
}

enum S2CCommand
{
    REFRESH,
    UPDATE,
}

public class Battle
{
    private static Func<int, IUnitSDS> getUnitCallBack;

    private static IGameConfig gameConfig;

    private Action<MemoryStream> sendDataCallBack;

    public Dictionary<int, Unit> unitDic = new Dictionary<int, Unit>();

    private Dictionary<int, PoolUnit> mPoolDic = new Dictionary<int, PoolUnit>();

    private Dictionary<int, PoolUnit> oPoolDic = new Dictionary<int, PoolUnit>();

    private Dictionary<int, Dictionary<int, CommandData>> commandPool = new Dictionary<int, Dictionary<int, CommandData>>();

    private int uid;

    private Simulator simulator;

    private int roundNum;

    //client data
    private int serverRoundNum;
    private Dictionary<int, double> resultDic = new Dictionary<int, double>();
    private Action updateCallBack;
    //----

    public static void Init(IGameConfig _gameConfig, Func<int, IUnitSDS> _getUnitCallBack)
    {
        gameConfig = _gameConfig;

        getUnitCallBack = _getUnitCallBack;
    }

    public void ServerStart(Action<MemoryStream> _sendDataCallBack)
    {
        sendDataCallBack = _sendDataCallBack;

        roundNum = 1;
    }

    public void ClientStart(Action<MemoryStream> _sendDataCallBack, Action _updateCallBack)
    {
        sendDataCallBack = _sendDataCallBack;

        updateCallBack = _updateCallBack;
    }

    private void InitSimulator()
    {
        Rect mapBounds = new Rect(gameConfig.GetMapX(), gameConfig.GetMapY(), gameConfig.GetMapWidth(), gameConfig.GetMapHeight());

        simulator = new Simulator();

        simulator.setAgentDefaults(100.0, 10, 0.01, 0.01, 20, 0.0, Vector2.zero);

        simulator.setTimeStep(gameConfig.GetTimeStep());

        simulator.setMapBounds(mapBounds);

        simulator.setMaxRadius(gameConfig.GetMaxRadius());

        simulator.setMapBoundFix(gameConfig.GetMapBoundFix());
    }

    private void AddUnitToPool(bool _isMine, int _id)
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

    private void Spawn()
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

    private Unit AddUnitToBattle(bool _isMine, int _id, Vector2 _pos)
    {
        Unit unit = new Unit();

        unit.Init(this, simulator, _isMine, GetUid(), _id, getUnitCallBack(_id), _pos);

        unitDic[unit.uid] = unit;

        return unit;
    }

    public void Update()
    {
        if (updateCallBack == null)
        {
            //server send commandData to client 
        }

        //do command
        if (commandPool.ContainsKey(roundNum))
        {
            Dictionary<int, CommandData> tmpDic = commandPool[roundNum];

            Dictionary<int, CommandData>.ValueCollection.Enumerator enumerator = tmpDic.Values.GetEnumerator();

            while (enumerator.MoveNext())
            {
                DoCommand(enumerator.Current);
            }

            commandPool.Remove(roundNum);
        }
        //----

        //do round action
        if (roundNum % gameConfig.GetSpawnStep() == 0)
        {
            Spawn();
        }

        for (int i = 0; i < gameConfig.GetMoveTimes() - 1; i++)
        {
            simulator.BuildAgentTree();

            simulator.doStep();
        }

        simulator.BuildAgentTree();

        double result = simulator.doStepFinal();
        //----

        if (updateCallBack != null)
        {
            if (resultDic.ContainsKey(roundNum))
            {
                double serverResult = resultDic[roundNum];

                if (!result.ToString("F4").Equals(serverResult.ToString("F4")))
                {
                    throw new Exception("我就日了  myRound:" + roundNum + "  serverRound:" + serverRoundNum + "    myResult:" + result + "  serverResult:" + serverResult);
                }
                else
                {
                    resultDic.Remove(roundNum);
                }
            }
            else
            {
                resultDic.Add(roundNum, result);
            }

            updateCallBack();
        }
        else
        {
            //server send result to client 
        }

        roundNum++;
    }

    private void ClientUpdate(int _serverRoundNum, Dictionary<int, CommandData> _commandData, double _serverResult)
    {
        if (serverRoundNum != _serverRoundNum)
        {
            if (_serverRoundNum == serverRoundNum + 1)
            {
                serverRoundNum = _serverRoundNum;
            }
            else
            {
                Log.Write("我上次收到服务器的同步包回合数是:" + serverRoundNum + "  这次收到服务器的同步包回合数是:" + _serverRoundNum);

                int[] op = new int[0];

                op[2] = 4;
            }
        }

        int roundDiff = roundNum - serverRoundNum;

        if (roundDiff < 0)
        {
            Log.Write("我日  服务器时间竟然比我快 myRound:" + roundNum + " serverRound:" + serverRoundNum + "  roundDiff:" + roundDiff);

            for (int i = 0; i < -roundDiff; i++)
            {
                Update();
            }

            Log.Write("我日  我追完了  myRound:" + roundNum + "  roundDiff:" + roundDiff);
        }
        else if (roundDiff > 0)
        {
            if (roundDiff > gameConfig.GetCommandDelay())
            {
                if (_commandData != null)
                {
                    Log.Write("myRound:" + roundNum + "  serverRound:" + serverRoundNum + "  我日   延迟已经" + roundDiff + "回合了  而且还有玩家操作  没救了  让服务器重新刷数据吧" + "  roundDiff:" + roundDiff);

                    //ask server to refresh all data

                    return;
                }
                else
                {
                    Log.Write("myRound:" + roundNum + "  serverRound:" + serverRoundNum + "  我日   延迟已经" + roundDiff + "回合了  还好没有动作" + "  roundDiff:" + roundDiff);
                }
            }
        }

        if (_commandData != null)
        {
            Dictionary<int, CommandData>.Enumerator enumerator = _commandData.GetEnumerator();

            while (enumerator.MoveNext())
            {
                ReceiveCommand(enumerator.Current.Key, enumerator.Current.Value);
            }
        }

        if (resultDic.ContainsKey(serverRoundNum))
        {
            double myResult = resultDic[serverRoundNum];

            if (!myResult.ToString("F4").Equals(_serverResult.ToString("F4")))
            {
                throw new Exception("myRound:" + roundNum + "  serverRound:" + serverRoundNum + "  抓住你了!!!   myResult:" + myResult + "  serverResult:" + _serverResult + "  roundDiff:" + roundDiff);
            }
            else
            {
                resultDic.Remove(serverRoundNum);
            }
        }
        else
        {
            resultDic.Add(serverRoundNum, _serverResult);
        }
    }

    private void ReceiveCommand(int _commandID, CommandData _commandData)
    {
        int tmpRound = roundNum + gameConfig.GetCommandDelay();

        Dictionary<int, CommandData> tmpDic;

        if (commandPool.ContainsKey(tmpRound))
        {
            tmpDic = commandPool[tmpRound];
        }
        else
        {
            tmpDic = new Dictionary<int, CommandData>();

            commandPool[tmpRound] = tmpDic;
        }

        if (!tmpDic.ContainsKey(_commandID))
        {
            tmpDic.Add(_commandID, _commandData);
        }
    }

    private void DoCommand(CommandData _commandData)
    {

    }

    private int GetUid()
    {
        uid++;
        return uid;
    }

    public void ServerGetBytes(byte[] _bytes)
    {
        using (MemoryStream ms = new MemoryStream(_bytes))
        {
            using (BinaryReader br = new BinaryReader(ms))
            {
                C2SCommand command = (C2SCommand)br.ReadInt32();

                switch (command)
                {
                    case C2SCommand.REFRESH:



                        break;

                    case C2SCommand.ACTION:


                        break;
                }
            }
        }
    }

    public void ClientGetBytes(byte[] _bytes)
    {
        using (MemoryStream ms = new MemoryStream(_bytes))
        {
            using (BinaryReader br = new BinaryReader(ms))
            {
                S2CCommand command = (S2CCommand)br.ReadInt32();

                switch (command)
                {
                    case S2CCommand.REFRESH:



                        break;

                    case S2CCommand.UPDATE:


                        break;
                }
            }
        }
    }
}
