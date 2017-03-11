﻿using System;
using System.Collections.Generic;
using RVO;
using System.IO;

class CommandData
{
    public bool isMine;
    public int id;

    public CommandData(bool _isMine, int _id)
    {
        isMine = _isMine;
        id = _id;
    }
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
    ACTION_OK
}

public class Battle
{
    internal static Func<int, IUnitSDS> getUnitCallBack;

    internal static IGameConfig gameConfig;

    private Action<bool, MemoryStream> serverSendDataCallBack;

    public Dictionary<int, Unit> unitDic = new Dictionary<int, Unit>();

    public LinkedList<Unit> unitList = new LinkedList<Unit>();

    private Dictionary<int, int> mPoolDic = new Dictionary<int, int>();

    private Dictionary<int, int> oPoolDic = new Dictionary<int, int>();

    private Dictionary<int, Dictionary<int, CommandData>> commandPool = new Dictionary<int, Dictionary<int, CommandData>>();

    private int uid;

    private int commandID;

    private Simulator simulator;

    private int roundNum;

    private Action overCallBack;

    //client data
    public bool clientIsMine;
    private Action<MemoryStream> clientSendDataCallBack;
    private int serverRoundNum;
    private Dictionary<int, double> resultDic = new Dictionary<int, double>();
    private Action updateCallBack;
    //----

    public static void Init(IGameConfig _gameConfig, Func<int, IUnitSDS> _getUnitCallBack)
    {
        gameConfig = _gameConfig;

        getUnitCallBack = _getUnitCallBack;
    }

    public void ServerInit(Action<bool, MemoryStream> _serverSendDataCallBack, Action _overCallBack)
    {
        serverSendDataCallBack = _serverSendDataCallBack;

        overCallBack = _overCallBack;

        InitSimulator();
    }

    public void ClientInit(Action<MemoryStream> _clientSendDataCallBack, Action _updateCallBack, Action _overCallBack)
    {
        clientSendDataCallBack = _clientSendDataCallBack;

        updateCallBack = _updateCallBack;

        overCallBack = _overCallBack;

        InitSimulator();
    }

    public void ServerStart()
    {
        uid = commandID = roundNum = 1;
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

    private void Over()
    {
        unitDic.Clear();

        unitList.Clear();

        mPoolDic.Clear();

        oPoolDic.Clear();

        commandPool.Clear();

        simulator.ClearAgents();

        overCallBack();
    }

    private void AddUnitToPool(bool _isMine, int _id)
    {
        IUnitSDS sds = getUnitCallBack(_id);

        Dictionary<int, int> pool = _isMine ? mPoolDic : oPoolDic;

        if (pool.ContainsKey(_id))
        {
            pool[_id]++;
        }
        else
        {
            pool.Add(_id, 1);
        }
    }

    private void Spawn()
    {
        Dictionary<int, int>.Enumerator enumerator = mPoolDic.GetEnumerator();

        while (enumerator.MoveNext())
        {
            int id = enumerator.Current.Key;

            int num = enumerator.Current.Value;

            IUnitSDS sds = getUnitCallBack(id);

            for (int i = 0; i < num; i++)
            {
                double posX = -70 + sds.GetQueuePos();

                double posY = (i % 2 == 0 ? 1 : -1) * i * 0.1;

                Vector2 pos = new Vector2(posX, posY);

                AddUnitToBattle(true, id, pos);
            }
        }

        enumerator = oPoolDic.GetEnumerator();

        while (enumerator.MoveNext())
        {
            int id = enumerator.Current.Key;

            int num = enumerator.Current.Value;

            IUnitSDS sds = getUnitCallBack(id);

            for (int i = 0; i < num; i++)
            {
                double posX = 70 - sds.GetQueuePos();

                double posY = (i % 2 == 0 ? 1 : -1) * i * 0.1;

                Vector2 pos = new Vector2(posX, posY);

                AddUnitToBattle(false, id, pos);
            }
        }
    }

    private Unit AddUnitToBattle(bool _isMine, int _id, Vector2 _pos)
    {
        Unit unit = new Unit();

        unit.Init(this, simulator, _isMine, GetUid(), _id, getUnitCallBack(_id), _pos);

        unitDic.Add(unit.uid, unit);

        unitList.AddLast(unit);

        return unit;
    }

    public void Update()
    {
        MemoryStream ms = null;

        BinaryWriter bw = null;

        if (updateCallBack == null)
        {
            ms = new MemoryStream();

            bw = new BinaryWriter(ms);

            ServerUpdate(bw);
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

        {
            simulator.BuildAgentTree();

            LinkedList<Unit>.Enumerator enumerator = unitList.GetEnumerator();

            while (enumerator.MoveNext())
            {
                Unit unit = enumerator.Current;

                unit.Update();
            }

            simulator.BuildAgentTree();

            LinkedListNode<Unit> node = unitList.First;

            while (node != null)
            {
                LinkedListNode<Unit> tmpNode = node;

                node = node.Next;

                Unit unit = tmpNode.Value;

                if (unit.nowHp < 1)
                {
                    unit.Die();

                    unitList.Remove(tmpNode);

                    unitDic.Remove(unit.uid);
                }
            }
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

                if (Math.Round(result, 2) != Math.Round(serverResult, 2))
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
            bw.Write(result);

            serverSendDataCallBack(true, ms);

            serverSendDataCallBack(false, ms);

            ms.Dispose();

            bw.Close();
        }

        roundNum++;
    }

    private void ServerUpdate(BinaryWriter _bw)
    {
        _bw.Write((int)S2CCommand.UPDATE);

        _bw.Write(roundNum);

        int tmpRoundNum = roundNum + gameConfig.GetCommandDelay();

        if (commandPool.ContainsKey(tmpRoundNum))
        {
            Dictionary<int, CommandData> tmpDic = commandPool[tmpRoundNum];

            _bw.Write(tmpDic.Count);

            Dictionary<int, CommandData>.Enumerator enumerator = tmpDic.GetEnumerator();

            while (enumerator.MoveNext())
            {
                _bw.Write(enumerator.Current.Key);

                CommandData data = enumerator.Current.Value;

                _bw.Write(data.isMine);

                _bw.Write(data.id);
            }
        }
        else
        {
            _bw.Write(0);
        }
    }

    private void ClientUpdate(BinaryReader _br)
    {
        int tmpServerRoundNum = _br.ReadInt32();

        int num = _br.ReadInt32();

        Dictionary<int, CommandData> commandData = null;

        if (num > 0)
        {
            commandData = new Dictionary<int, CommandData>();

            for (int i = 0; i < num; i++)
            {
                int tmpCommandID = _br.ReadInt32();

                bool isMine = _br.ReadBoolean();

                int id = _br.ReadInt32();

                commandData.Add(tmpCommandID, new CommandData(isMine, id));
            }
        }

        double serverResult = _br.ReadDouble();

        if (serverRoundNum != tmpServerRoundNum)
        {
            if (tmpServerRoundNum == serverRoundNum + 1)
            {
                serverRoundNum = tmpServerRoundNum;
            }
            else
            {
                Log.Write("我上次收到服务器的同步包回合数是:" + serverRoundNum + "  这次收到服务器的同步包回合数是:" + tmpServerRoundNum);

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
                if (commandData != null)
                {
                    Log.Write("myRound:" + roundNum + "  serverRound:" + serverRoundNum + "  我日   延迟已经" + roundDiff + "回合了  而且还有玩家操作  没救了  让服务器重新刷数据吧" + "  roundDiff:" + roundDiff);

                    ClientRequestRefresh();

                    return;
                }
                else
                {
                    Log.Write("myRound:" + roundNum + "  serverRound:" + serverRoundNum + "  我日   延迟已经" + roundDiff + "回合了  还好没有动作" + "  roundDiff:" + roundDiff);
                }
            }
        }

        if (commandData != null)
        {
            Dictionary<int, CommandData>.Enumerator enumerator = commandData.GetEnumerator();

            while (enumerator.MoveNext())
            {
                ReceiveCommand(serverRoundNum + gameConfig.GetCommandDelay(), enumerator.Current.Key, enumerator.Current.Value);
            }
        }

        if (resultDic.ContainsKey(serverRoundNum))
        {
            double myResult = resultDic[serverRoundNum];

            if (Math.Round(myResult, 2) != Math.Round(serverResult, 2))
            {
                throw new Exception("myRound:" + roundNum + "  serverRound:" + serverRoundNum + "  抓住你了!!!   myResult:" + myResult + "  serverResult:" + serverResult + "  roundDiff:" + roundDiff);
            }
            else
            {
                resultDic.Remove(serverRoundNum);
            }
        }
        else
        {
            resultDic.Add(serverRoundNum, serverResult);
        }
    }

    private void ReceiveCommand(int _roundNum, int _commandID, CommandData _commandData)
    {
        Dictionary<int, CommandData> tmpDic;

        if (commandPool.ContainsKey(_roundNum))
        {
            tmpDic = commandPool[_roundNum];
        }
        else
        {
            tmpDic = new Dictionary<int, CommandData>();

            commandPool[_roundNum] = tmpDic;
        }

        if (!tmpDic.ContainsKey(_commandID))
        {
            tmpDic.Add(_commandID, _commandData);
        }
    }

    private void DoCommand(CommandData _commandData)
    {
        AddUnitToPool(_commandData.isMine, _commandData.id);
    }

    private int GetUid()
    {
        uid++;
        return uid;
    }

    private int GetCommandID()
    {
        commandID++;
        return commandID;
    }

    public void ServerGetBytes(bool _isMine, byte[] _bytes)
    {
        using (MemoryStream ms = new MemoryStream(_bytes))
        {
            using (BinaryReader br = new BinaryReader(ms))
            {
                C2SCommand command = (C2SCommand)br.ReadInt32();

                switch (command)
                {
                    case C2SCommand.REFRESH:

                        ServerRefresh(_isMine);

                        break;

                    case C2SCommand.ACTION:

                        ServerReceiveCommand(_isMine, br);

                        break;
                }
            }
        }
    }

    private void ServerRefresh(bool _isMine)
    {
        Log.Write("ServerRefresh:" + roundNum);

        using (MemoryStream ms = new MemoryStream())
        {
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write((int)S2CCommand.REFRESH);

                bw.Write(_isMine);

                bw.Write(roundNum);

                bw.Write(uid);

                bw.Write(unitList.Count);

                LinkedList<Unit>.Enumerator enumerator = unitList.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    Unit unit = enumerator.Current;

                    unit.WriteData(bw);
                }

                bw.Write(mPoolDic.Count);

                Dictionary<int, int>.Enumerator enumerator2 = mPoolDic.GetEnumerator();

                while (enumerator2.MoveNext())
                {
                    bw.Write(enumerator2.Current.Key);

                    bw.Write(enumerator2.Current.Value);
                }

                bw.Write(oPoolDic.Count);

                enumerator2 = oPoolDic.GetEnumerator();

                while (enumerator2.MoveNext())
                {
                    bw.Write(enumerator2.Current.Key);

                    bw.Write(enumerator2.Current.Value);
                }

                bw.Write(commandPool.Count);

                Dictionary<int, Dictionary<int, CommandData>>.Enumerator enumerator3 = commandPool.GetEnumerator();

                while (enumerator3.MoveNext())
                {
                    bw.Write(enumerator3.Current.Key);

                    Dictionary<int, CommandData> tmpDic = enumerator3.Current.Value;

                    bw.Write(tmpDic.Count);

                    Dictionary<int, CommandData>.Enumerator enumerator4 = tmpDic.GetEnumerator();

                    while (enumerator4.MoveNext())
                    {
                        bw.Write(enumerator4.Current.Key);

                        CommandData data = enumerator4.Current.Value;

                        bw.Write(data.isMine);

                        bw.Write(data.id);
                    }
                }

                serverSendDataCallBack(_isMine, ms);
            }
        }
    }

    private void ServerReceiveCommand(bool _isMine, BinaryReader _br)
    {
        int id = _br.ReadInt32();

        CommandData data = new CommandData(_isMine, id);

        ReceiveCommand(roundNum + gameConfig.GetCommandDelay(), GetCommandID(), data);

        using (MemoryStream ms = new MemoryStream())
        {
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write((int)S2CCommand.ACTION_OK);

                serverSendDataCallBack(_isMine, ms);
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

                        ClientRefreshData(br);

                        break;

                    case S2CCommand.UPDATE:

                        ClientUpdate(br);

                        break;

                    case S2CCommand.ACTION_OK:



                        break;
                }
            }
        }
    }

    private void ClientRefreshData(BinaryReader _br)
    {
        unitDic.Clear();

        unitList.Clear();

        mPoolDic.Clear();

        oPoolDic.Clear();

        commandPool.Clear();

        simulator.ClearAgents();

        clientIsMine = _br.ReadBoolean();

        serverRoundNum = roundNum = _br.ReadInt32();

        Log.Write("client refresh data " + roundNum);

        uid = _br.ReadInt32();

        int num = _br.ReadInt32();

        for (int i = 0; i < num; i++)
        {
            Unit unit = new Unit();

            unit.Init(this, simulator, _br);

            unitDic.Add(unit.uid, unit);

            unitList.AddLast(unit);
        }

        num = _br.ReadInt32();

        for (int i = 0; i < num; i++)
        {
            int id = _br.ReadInt32();

            int num2 = _br.ReadInt32();

            mPoolDic.Add(id, num2);
        }

        num = _br.ReadInt32();

        for (int i = 0; i < num; i++)
        {
            int id = _br.ReadInt32();

            int num2 = _br.ReadInt32();

            oPoolDic.Add(id, num2);
        }

        num = _br.ReadInt32();

        for (int i = 0; i < num; i++)
        {
            int tmpRoundNum = _br.ReadInt32();

            int num2 = _br.ReadInt32();

            Dictionary<int, CommandData> tmpDic = new Dictionary<int, CommandData>();

            commandPool.Add(tmpRoundNum, tmpDic);

            for (int m = 0; m < num2; m++)
            {
                int tmpCommandID = _br.ReadInt32();

                bool isMine = _br.ReadBoolean();

                int id = _br.ReadInt32();

                tmpDic.Add(tmpCommandID, new CommandData(isMine, id));
            }
        }
    }

    public void ClientSendCommand(int _id)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write((int)C2SCommand.ACTION);

                bw.Write(_id);

                clientSendDataCallBack(ms);
            }
        }
    }

    public void ClientRequestRefresh()
    {
        using (MemoryStream ms = new MemoryStream())
        {
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write((int)C2SCommand.REFRESH);

                clientSendDataCallBack(ms);
            }
        }
    }
}
