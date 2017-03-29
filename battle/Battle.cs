using System;
using System.Collections.Generic;
using RVO;
using System.IO;

public class CommandData
{
    public bool isMine;

    public CommandData(bool _isMine)
    {
        isMine = _isMine;
    }
}

public class UnitCommandData : CommandData
{
    public int id;

    public UnitCommandData(bool _isMine, int _id) : base(_isMine)
    {
        id = _id;
    }
}

public class HeroCommandData : CommandData
{
    public int id;
    public Vector2 pos;

    public HeroCommandData(bool _isMine, int _id, Vector2 _pos) : base(_isMine)
    {
        id = _id;
        pos = _pos;
    }
}

public class SkillCommandData : CommandData
{
    public int id;
    public Vector2 pos;

    public SkillCommandData(bool _isMine, int _id, Vector2 _pos) : base(_isMine)
    {
        id = _id;
        pos = _pos;
    }
}

enum CommandType
{
    UNIT,
    HERO,
    SKILL
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
    ACTION_OK,
}

public class Battle
{
    internal static Func<int, IUnitSDS> getUnitCallBack;

    internal static Func<int, ISkillSDS> getSkillCallBack;

    internal static IGameConfig gameConfig;

    private Action<bool, MemoryStream> serverSendDataCallBack;

    public Dictionary<int, Unit> unitDic = new Dictionary<int, Unit>();//这个字典里存的是所有场上的单位和英雄 下标是uid

    public LinkedList<Unit> unitList = new LinkedList<Unit>();//这个链表存的是所有场上的单位和英雄  为了unit执行动作时 客户端服务器顺序一致 所以才用了这个链表

    public Dictionary<int, Unit> mHeroPool = new Dictionary<int, Unit>();//这个字典存的是所有场上的英雄  下标是英雄id  为了方便检测英雄能否放置和能否使用技能

    public Dictionary<int, Unit> oHeroPool = new Dictionary<int, Unit>();//这个字典存的是所有场上的英雄  下标是英雄id  为了方便检测英雄能否放置和能否使用技能

    public Dictionary<int, int> mUnitPool = new Dictionary<int, int>();//这个字典存的是所有按波次上场的单位  下标是单位id

    public Dictionary<int, int> oUnitPool = new Dictionary<int, int>();//这个字典存的是所有按波次上场的单位  下标是单位id

    public Dictionary<int, Dictionary<int, CommandData>> commandPool = new Dictionary<int, Dictionary<int, CommandData>>();//这个字典存的是所有非单位命令  第一个下标是roundNum 第二个下标是commandID

    public Dictionary<int, HeroCommandData> mHeroCommandPool = new Dictionary<int, HeroCommandData>();//这个字典存的是所有放置英雄的命令  下标是英雄id  为了方便检测英雄能否放置  英雄一旦放上了场就会从这个字典中删除

    public Dictionary<int, HeroCommandData> oHeroCommandPool = new Dictionary<int, HeroCommandData>();//这个字典存的是所有放置英雄的命令  下标是英雄id  为了方便检测英雄能否放置  英雄一旦放上了场就会从这个字典中删除

    public Dictionary<int, Dictionary<int, UnitCommandData>> mUnitCommandPool = new Dictionary<int, Dictionary<int, UnitCommandData>>();//这个字典存的是所有单位命令  第一个下标是roundNum 第二个下标是commandID

    public Dictionary<int, Dictionary<int, UnitCommandData>> oUnitCommandPool = new Dictionary<int, Dictionary<int, UnitCommandData>>();//这个字典存的是所有单位命令  第一个下标是roundNum 第二个下标是commandID

    public Dictionary<int, SkillCommandData> mSkillCommandPool = new Dictionary<int, SkillCommandData>();

    public Dictionary<int, SkillCommandData> oSkillCommandPool = new Dictionary<int, SkillCommandData>();

    public LinkedList<Skill> skillList = new LinkedList<Skill>();

    private int uid;

    private int commandID;

    private Simulator simulator;

    private int roundNum;

    private Action overCallBack;

    private Random random;

    public int mMoney { private set; get; }

    public int oMoney { private set; get; }

    private int mShowMoney;//只有服务器有这个数据

    private int oShowMoney;//只有服务器有这个数据

    private bool isClient;

    //client data
    public bool clientIsMine;
    private Action<MemoryStream> clientSendDataCallBack;
    private int serverRoundNum;
    private Dictionary<int, double> resultDic = new Dictionary<int, double>();
    private Action updateCallBack;
    private Action<bool> sendCommandCallBack;
    //----

    public static void Init(IGameConfig _gameConfig, Func<int, IUnitSDS> _getUnitCallBack, Func<int, ISkillSDS> _getSkillCallBack)
    {
        gameConfig = _gameConfig;

        getUnitCallBack = _getUnitCallBack;

        getSkillCallBack = _getSkillCallBack;
    }

    public void ServerInit(Action<bool, MemoryStream> _serverSendDataCallBack, Action _overCallBack)
    {
        isClient = false;

        random = new Random();

        serverSendDataCallBack = _serverSendDataCallBack;

        overCallBack = _overCallBack;

        InitSimulator();
    }

    public void ClientInit(Action<MemoryStream> _clientSendDataCallBack, Action _updateCallBack, Action _overCallBack, Action<bool> _sendCommandCallBack)
    {
        isClient = true;

        clientSendDataCallBack = _clientSendDataCallBack;

        updateCallBack = _updateCallBack;

        overCallBack = _overCallBack;

        sendCommandCallBack = _sendCommandCallBack;

        InitSimulator();
    }

    public void ServerStart()
    {
        uid = commandID = roundNum = 1;

        mShowMoney = oShowMoney = mMoney = oMoney = gameConfig.GetDefaultMoney();
    }

    private void InitSimulator()
    {
        Rect mapBounds = new Rect(-0.5 * gameConfig.GetMapWidth(), -0.5 * gameConfig.GetMapHeight(), gameConfig.GetMapWidth(), gameConfig.GetMapHeight());

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

        mUnitPool.Clear();

        oUnitPool.Clear();

        commandPool.Clear();

        mHeroCommandPool.Clear();

        oHeroCommandPool.Clear();

        mSkillCommandPool.Clear();

        oSkillCommandPool.Clear();

        mHeroPool.Clear();

        oHeroPool.Clear();

        mUnitCommandPool.Clear();

        oUnitCommandPool.Clear();

        simulator.ClearAgents();

        overCallBack();
    }

    private void AddUnitToPool(bool _isMine, int _id)
    {
        IUnitSDS sds = getUnitCallBack(_id);

        Dictionary<int, int> pool = _isMine ? mUnitPool : oUnitPool;

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
        Dictionary<int, int>.Enumerator enumerator = mUnitPool.GetEnumerator();

        while (enumerator.MoveNext())
        {
            int id = enumerator.Current.Key;

            int num = enumerator.Current.Value;

            IUnitSDS sds = getUnitCallBack(id);

            for (int i = 0; i < num; i++)
            {
                double posX = -gameConfig.GetSpawnX() + sds.GetQueuePos();

                double posY = -gameConfig.GetSpawnY() + (i % 2 == 0 ? 1 : -1) * i * 0.1;

                Vector2 pos = new Vector2(posX, posY);

                AddUnitToBattle(true, id, pos);
            }
        }

        enumerator = oUnitPool.GetEnumerator();

        while (enumerator.MoveNext())
        {
            int id = enumerator.Current.Key;

            int num = enumerator.Current.Value;

            IUnitSDS sds = getUnitCallBack(id);

            for (int i = 0; i < num; i++)
            {
                double posX = gameConfig.GetSpawnX() - sds.GetQueuePos();

                double posY = gameConfig.GetSpawnY() + (i % 2 == 0 ? -1 : 1) * i * 0.1;

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

        if (unit.sds.GetIsHero())
        {
            Dictionary<int, Unit> tmpDic;

            Dictionary<int, HeroCommandData> tmpDic2;

            if (_isMine)
            {
                tmpDic = mHeroPool;

                tmpDic2 = mHeroCommandPool;
            }
            else
            {
                tmpDic = oHeroPool;

                tmpDic2 = oHeroCommandPool;
            }

            tmpDic.Add(_id, unit);

            tmpDic2.Remove(_id);
        }

        return unit;
    }

    public void Update()
    {
        MemoryStream mMs = null;

        MemoryStream oMs = null;

        BinaryWriter mBw = null;

        BinaryWriter oBw = null;

        if (!isClient)
        {
            mMs = new MemoryStream();

            mBw = new BinaryWriter(mMs);

            oMs = new MemoryStream();

            oBw = new BinaryWriter(oMs);

            ServerUpdate(mBw, true);

            ServerUpdate(oBw, false);
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

        //服务器必须在波次到来前N帧将新增单位指令添加到pool中
        if (!isClient && (roundNum + gameConfig.GetCommandDelay()) % gameConfig.GetSpawnStep() == 0)
        {
            Dictionary<int, Dictionary<int, UnitCommandData>>.ValueCollection.Enumerator enumerator = mUnitCommandPool.Values.GetEnumerator();

            while (enumerator.MoveNext())
            {
                Dictionary<int, UnitCommandData> tmpDic = enumerator.Current;

                Dictionary<int, UnitCommandData>.ValueCollection.Enumerator enumerator2 = tmpDic.Values.GetEnumerator();

                while (enumerator2.MoveNext())
                {
                    DoCommand(enumerator2.Current);
                }
            }

            mUnitCommandPool.Clear();

            enumerator = oUnitCommandPool.Values.GetEnumerator();

            while (enumerator.MoveNext())
            {
                Dictionary<int, UnitCommandData> tmpDic = enumerator.Current;

                Dictionary<int, UnitCommandData>.ValueCollection.Enumerator enumerator2 = tmpDic.Values.GetEnumerator();

                while (enumerator2.MoveNext())
                {
                    DoCommand(enumerator2.Current);
                }
            }

            oUnitCommandPool.Clear();
        }
        //客户端必须在波次到来的这一帧将新增单位指令添加到pool中  同时还要将下一个波次的新增单位指令继续保存
        else if (isClient && roundNum % gameConfig.GetSpawnStep() == 0)
        {
            Dictionary<int, Dictionary<int, UnitCommandData>> newDic = null;

            Dictionary<int, Dictionary<int, UnitCommandData>>.Enumerator enumerator = mUnitCommandPool.GetEnumerator();

            while (enumerator.MoveNext())
            {
                if (enumerator.Current.Key > roundNum)
                {
                    if (newDic == null)
                    {
                        newDic = new Dictionary<int, Dictionary<int, UnitCommandData>>();
                    }

                    newDic.Add(enumerator.Current.Key, enumerator.Current.Value);
                }
                else
                {
                    Dictionary<int, UnitCommandData> tmpDic = enumerator.Current.Value;

                    Dictionary<int, UnitCommandData>.ValueCollection.Enumerator enumerator2 = tmpDic.Values.GetEnumerator();

                    while (enumerator2.MoveNext())
                    {
                        DoCommand(enumerator2.Current);
                    }
                }
            }

            if (newDic != null)
            {
                mUnitCommandPool = newDic;

                newDic = null;
            }
            else
            {
                mUnitCommandPool.Clear();
            }

            enumerator = oUnitCommandPool.GetEnumerator();

            while (enumerator.MoveNext())
            {
                if (enumerator.Current.Key > roundNum)
                {
                    if (newDic == null)
                    {
                        newDic = new Dictionary<int, Dictionary<int, UnitCommandData>>();
                    }

                    newDic.Add(enumerator.Current.Key, enumerator.Current.Value);
                }
                else
                {
                    Dictionary<int, UnitCommandData> tmpDic = enumerator.Current.Value;

                    Dictionary<int, UnitCommandData>.ValueCollection.Enumerator enumerator2 = tmpDic.Values.GetEnumerator();

                    while (enumerator2.MoveNext())
                    {
                        DoCommand(enumerator2.Current);
                    }
                }
            }

            if (newDic != null)
            {
                oUnitCommandPool = newDic;
            }
            else
            {
                oUnitCommandPool.Clear();
            }
        }
        //----

        //do round action
        if (roundNum % gameConfig.GetSpawnStep() == 0)
        {
            Spawn();
        }

        simulator.BuildAgentTree();

        UpdateSkill();

        simulator.BuildAgentTree();

        UpdateUnit();

        for (int i = 0; i < gameConfig.GetMoveTimes() - 1; i++)
        {
            simulator.BuildAgentTree();

            simulator.doStep();
        }

        simulator.BuildAgentTree();

        double result = simulator.doStepFinal();
        //----

        mMoney += gameConfig.GetMoneyPerStep();

        oMoney += gameConfig.GetMoneyPerStep();

        if (isClient)
        {
            if (resultDic.ContainsKey(roundNum))
            {
                double serverResult = resultDic[roundNum];

                if (result.ToString("F4") != serverResult.ToString("F4"))
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
            mBw.Write(result);

            oBw.Write(result);

            serverSendDataCallBack(true, mMs);

            serverSendDataCallBack(false, oMs);

            mMs.Dispose();

            mBw.Close();

            oMs.Dispose();

            oBw.Close();

            mShowMoney += gameConfig.GetMoneyPerStep();

            oShowMoney += gameConfig.GetMoneyPerStep();
        }

        roundNum++;
    }

    private void UpdateSkill()
    {
        LinkedListNode<Skill> skillNode = skillList.First;

        while (skillNode != null)
        {
            LinkedListNode<Skill> next = skillNode.Next;

            bool b = skillNode.Value.Update(roundNum);

            if (b)
            {
                skillList.Remove(skillNode);
            }

            skillNode = next;
        }
    }

    private void UpdateUnit()
    {
        LinkedList<Unit>.Enumerator enumerator = unitList.GetEnumerator();

        while (enumerator.MoveNext())
        {
            Unit unit = enumerator.Current;

            unit.Update();
        }

        LinkedListNode<Unit> node = unitList.First;

        while (node != null)
        {
            LinkedListNode<Unit> next = node.Next;

            Unit unit = node.Value;

            if (!unit.IsAlive())
            {
                unit.Die();

                unitList.Remove(node);

                unitDic.Remove(unit.uid);

                if (unit.sds.GetIsHero())
                {
                    Dictionary<int, Unit> tmpDic = unit.isMine ? mHeroPool : oHeroPool;

                    tmpDic.Remove(unit.id);
                }
            }

            node = next;
        }
    }

    private void ServerUpdate(BinaryWriter _bw, bool _isMine)
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

                if (data is HeroCommandData)
                {
                    HeroCommandData command = data as HeroCommandData;

                    _bw.Write((int)CommandType.HERO);

                    _bw.Write(command.id);

                    _bw.Write(command.pos.x);

                    _bw.Write(command.pos.y);
                }
            }
        }
        else
        {
            _bw.Write(0);
        }

        Dictionary<int, Dictionary<int, UnitCommandData>> tmpUnitCommandPool = _isMine ? mUnitCommandPool : oUnitCommandPool;

        if (tmpUnitCommandPool.ContainsKey(tmpRoundNum))
        {
            Dictionary<int, UnitCommandData> tmpDic = tmpUnitCommandPool[tmpRoundNum];

            _bw.Write(tmpDic.Count);

            Dictionary<int, UnitCommandData>.Enumerator enumerator = tmpDic.GetEnumerator();

            while (enumerator.MoveNext())
            {
                _bw.Write(enumerator.Current.Key);

                _bw.Write(enumerator.Current.Value.id);
            }
        }
        else
        {
            _bw.Write(0);
        }

        if (tmpRoundNum % gameConfig.GetSpawnStep() == 0)
        {
            tmpUnitCommandPool = _isMine ? oUnitCommandPool : mUnitCommandPool;

            _bw.Write(tmpUnitCommandPool.Count);

            Dictionary<int, Dictionary<int, UnitCommandData>>.ValueCollection.Enumerator enumerator = tmpUnitCommandPool.Values.GetEnumerator();

            while (enumerator.MoveNext())
            {
                _bw.Write(enumerator.Current.Count);

                Dictionary<int, UnitCommandData>.Enumerator enumerator2 = enumerator.Current.GetEnumerator();

                while (enumerator2.MoveNext())
                {
                    _bw.Write(enumerator2.Current.Key);

                    _bw.Write(enumerator2.Current.Value.id);
                }
            }

            if (_isMine)
            {
                oShowMoney = oMoney;
            }
            else
            {
                mShowMoney = mMoney;
            }
        }
    }

    private void ClientUpdateRead(BinaryReader _br, out int _serverRoundNum, out Dictionary<int, CommandData> _commandData, out Dictionary<int, UnitCommandData> _mUnitCommandData, out Dictionary<int, UnitCommandData> _oUnitCommandData, out double _serverResult)
    {
        _serverRoundNum = _br.ReadInt32();

        int num = _br.ReadInt32();

        _commandData = null;

        if (num > 0)
        {
            _commandData = new Dictionary<int, CommandData>();

            for (int i = 0; i < num; i++)
            {
                int tmpCommandID = _br.ReadInt32();

                bool isMine = _br.ReadBoolean();

                CommandData command;

                CommandType commandType = (CommandType)_br.ReadInt32();

                switch (commandType)
                {
                    case CommandType.HERO:

                        int id = _br.ReadInt32();

                        double x = _br.ReadDouble();

                        double y = _br.ReadDouble();

                        command = new HeroCommandData(isMine, id, new Vector2(x, y));

                        break;

                    default:

                        throw new Exception("commandtype error");
                }

                _commandData.Add(tmpCommandID, command);
            }
        }

        num = _br.ReadInt32();

        _mUnitCommandData = null;

        if (num > 0)
        {
            _mUnitCommandData = new Dictionary<int, UnitCommandData>();

            for (int i = 0; i < num; i++)
            {
                int tmpCommandID = _br.ReadInt32();

                int id = _br.ReadInt32();

                _mUnitCommandData.Add(tmpCommandID, new UnitCommandData(clientIsMine, id));
            }
        }

        _oUnitCommandData = null;

        if ((_serverRoundNum + gameConfig.GetCommandDelay()) % gameConfig.GetSpawnStep() == 0)
        {
            num = _br.ReadInt32();

            if (num > 0)
            {
                _oUnitCommandData = new Dictionary<int, UnitCommandData>();

                for (int i = 0; i < num; i++)
                {
                    int num2 = _br.ReadInt32();

                    for (int m = 0; m < num2; m++)
                    {
                        int tmpCommandID = _br.ReadInt32();

                        int id = _br.ReadInt32();

                        _oUnitCommandData.Add(tmpCommandID, new UnitCommandData(!clientIsMine, id));
                    }
                }
            }
        }

        _serverResult = _br.ReadDouble();
    }

    private void ClientUpdate(BinaryReader _br)
    {
        int tmpServerRoundNum;

        Dictionary<int, CommandData> commandData;

        Dictionary<int, UnitCommandData> mUnitCommandData;

        Dictionary<int, UnitCommandData> oUnitCommandData;

        double serverResult;

        ClientUpdateRead(_br, out tmpServerRoundNum, out commandData, out mUnitCommandData, out oUnitCommandData, out serverResult);

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
                if (commandData != null || mUnitCommandData != null || oUnitCommandData != null)
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
                bool b = ReceiveCommand(serverRoundNum + gameConfig.GetCommandDelay(), enumerator.Current.Key, enumerator.Current.Value);

                if (!b)
                {
                    throw new Exception("error1");
                }
            }
        }

        if (mUnitCommandData != null)
        {
            Dictionary<int, UnitCommandData>.Enumerator enumerator = mUnitCommandData.GetEnumerator();

            while (enumerator.MoveNext())
            {
                bool b = ReceiveCommand(serverRoundNum + gameConfig.GetCommandDelay(), enumerator.Current.Key, enumerator.Current.Value);

                if (!b)
                {
                    throw new Exception("error2");
                }
            }
        }

        if (oUnitCommandData != null)
        {
            Dictionary<int, UnitCommandData>.Enumerator enumerator = oUnitCommandData.GetEnumerator();

            while (enumerator.MoveNext())
            {
                bool b = ReceiveCommand(serverRoundNum + gameConfig.GetCommandDelay(), enumerator.Current.Key, enumerator.Current.Value);

                if (!b)
                {
                    throw new Exception("error3");
                }
            }
        }

        if (resultDic.ContainsKey(serverRoundNum))
        {
            double myResult = resultDic[serverRoundNum];

            if (myResult.ToString("F4") != serverResult.ToString("F4"))
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

    private bool ReceiveCommand(int _roundNum, int _commandID, CommandData _commandData)
    {
        if (_commandData is UnitCommandData)
        {
            UnitCommandData command = _commandData as UnitCommandData;

            IUnitSDS sds = getUnitCallBack(command.id);

            if (!sds.GetIsHero())
            {
                if (command.isMine)
                {
                    if (sds.GetPrize() > mMoney)
                    {
                        return false;
                    }
                    else
                    {
                        mMoney -= sds.GetPrize();
                    }
                }
                else
                {
                    if (sds.GetPrize() > oMoney)
                    {
                        return false;
                    }
                    else
                    {
                        oMoney -= sds.GetPrize();
                    }
                }
            }
            else
            {
                return false;
            }

            Dictionary<int, Dictionary<int, UnitCommandData>> unitCommandPool = command.isMine ? mUnitCommandPool : oUnitCommandPool;

            if (unitCommandPool.ContainsKey(_roundNum))
            {
                Dictionary<int, UnitCommandData> tmpDic = unitCommandPool[_roundNum];

                if (!tmpDic.ContainsKey(_commandID))
                {
                    tmpDic.Add(_commandID, command);
                }
            }
            else
            {
                Dictionary<int, UnitCommandData> tmpDic = new Dictionary<int, UnitCommandData>();

                unitCommandPool.Add(_roundNum, tmpDic);

                tmpDic.Add(_commandID, command);
            }
        }
        else if (_commandData is HeroCommandData)
        {
            HeroCommandData command = _commandData as HeroCommandData;

            IUnitSDS sds = getUnitCallBack(command.id);

            if (sds.GetIsHero())
            {
                if (command.isMine)
                {
                    if (mHeroCommandPool.ContainsKey(command.id) || mHeroPool.ContainsKey(command.id) || sds.GetPrize() > mMoney)
                    {
                        return false;
                    }
                    else
                    {
                        mMoney -= sds.GetPrize();
                    }
                }
                else
                {
                    if (oHeroCommandPool.ContainsKey(command.id) || oHeroPool.ContainsKey(command.id) || sds.GetPrize() > oMoney)
                    {
                        return false;
                    }
                    else
                    {
                        oMoney -= sds.GetPrize();
                    }
                }
            }
            else
            {
                return false;
            }

            if (commandPool.ContainsKey(_roundNum))
            {
                Dictionary<int, CommandData> tmpDic = commandPool[_roundNum];

                if (!tmpDic.ContainsKey(_commandID))
                {
                    tmpDic.Add(_commandID, _commandData);
                }
            }
            else
            {
                Dictionary<int, CommandData> tmpDic = new Dictionary<int, CommandData>();

                commandPool.Add(_roundNum, tmpDic);

                tmpDic.Add(_commandID, _commandData);
            }

            Dictionary<int, HeroCommandData> tmpDic2 = _commandData.isMine ? mHeroCommandPool : oHeroCommandPool;

            tmpDic2.Add(command.id, command);
        }
        else if (_commandData is SkillCommandData)
        {
            SkillCommandData command = _commandData as SkillCommandData;

            Dictionary<int, SkillCommandData> skillDic = command.isMine ? mSkillCommandPool : oSkillCommandPool;

            Dictionary<int, Unit> heroDic = command.isMine ? mHeroPool : oHeroPool;

            if (heroDic.ContainsKey(command.id) && !skillDic.ContainsKey(command.id))
            {
                if (commandPool.ContainsKey(_roundNum))
                {
                    Dictionary<int, CommandData> tmpDic = commandPool[_roundNum];

                    if (!tmpDic.ContainsKey(_commandID))
                    {
                        tmpDic.Add(_commandID, _commandData);
                    }
                }
                else
                {
                    Dictionary<int, CommandData> tmpDic = new Dictionary<int, CommandData>();

                    commandPool.Add(_roundNum, tmpDic);

                    tmpDic.Add(_commandID, _commandData);
                }

                skillDic.Add(command.id, command);
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    private void DoCommand(CommandData _commandData)
    {
        if (_commandData is UnitCommandData)
        {
            UnitCommandData command = _commandData as UnitCommandData;

            AddUnitToPool(command.isMine, command.id);
        }
        else if (_commandData is HeroCommandData)
        {
            HeroCommandData command = _commandData as HeroCommandData;

            AddUnitToBattle(command.isMine, command.id, command.pos);
        }
        else if (_commandData is SkillCommandData)
        {
            SkillCommandData command = _commandData as SkillCommandData;

            AddSkillToBattle(command.isMine, command.id, command.pos);
        }
    }

    private void AddSkillToBattle(bool _isMine, int _id, Vector2 _pos)
    {
        Dictionary<int, Unit> dic = _isMine ? mHeroPool : oHeroPool;

        if (dic.ContainsKey(_id))
        {
            Unit unit = dic[_id];

            if (unit.sds.GetSkill() != 0)
            {
                Skill skill = new Skill();

                skill.Init(this, simulator, roundNum, GetUid(), _id, getSkillCallBack(_id), unit, _pos);

                skillList.AddLast(skill);
            }
        }

        if (_isMine)
        {
            mSkillCommandPool.Remove(_id);
        }
        else
        {
            oSkillCommandPool.Remove(_id);
        }
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

    public void ServerRefresh(bool _isMine)
    {
        Log.Write("ServerRefresh:" + roundNum + "   isMine:" + _isMine);

        using (MemoryStream ms = new MemoryStream())
        {
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write((int)S2CCommand.REFRESH);

                bw.Write(_isMine);

                bw.Write(roundNum);

                bw.Write(uid);

                if (_isMine)
                {
                    bw.Write(mMoney);

                    bw.Write(oShowMoney);
                }
                else
                {
                    bw.Write(mShowMoney);

                    bw.Write(oMoney);
                }

                bw.Write(unitList.Count);

                LinkedList<Unit>.Enumerator enumerator = unitList.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    Unit unit = enumerator.Current;

                    unit.WriteData(bw);
                }

                bw.Write(skillList.Count);

                LinkedList<Skill>.Enumerator enumerator7 = skillList.GetEnumerator();

                while (enumerator7.MoveNext())
                {
                    Skill skill = enumerator7.Current;

                    skill.WriteData(bw);
                }

                bw.Write(mUnitPool.Count);

                Dictionary<int, int>.Enumerator enumerator2 = mUnitPool.GetEnumerator();

                while (enumerator2.MoveNext())
                {
                    bw.Write(enumerator2.Current.Key);

                    bw.Write(enumerator2.Current.Value);
                }

                bw.Write(oUnitPool.Count);

                enumerator2 = oUnitPool.GetEnumerator();

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

                        if (data is UnitCommandData)
                        {
                            UnitCommandData command = data as UnitCommandData;

                            bw.Write((int)CommandType.UNIT);

                            bw.Write(command.id);
                        }
                        else if (data is HeroCommandData)
                        {
                            HeroCommandData command = data as HeroCommandData;

                            bw.Write((int)CommandType.HERO);

                            bw.Write(command.id);

                            bw.Write(command.pos.x);

                            bw.Write(command.pos.y);
                        }
                        else if (data is SkillCommandData)
                        {
                            SkillCommandData command = data as SkillCommandData;

                            bw.Write((int)CommandType.SKILL);

                            bw.Write(command.id);

                            bw.Write(command.pos.x);

                            bw.Write(command.pos.y);
                        }
                    }
                }

                Dictionary<int, Dictionary<int, UnitCommandData>> unitCommandData = _isMine ? mUnitCommandPool : oUnitCommandPool;

                bw.Write(unitCommandData.Count);

                Dictionary<int, Dictionary<int, UnitCommandData>>.Enumerator enumerator5 = unitCommandData.GetEnumerator();

                while (enumerator5.MoveNext())
                {
                    bw.Write(enumerator5.Current.Key);

                    Dictionary<int, UnitCommandData> tmpDic = enumerator5.Current.Value;

                    bw.Write(tmpDic.Count);

                    Dictionary<int, UnitCommandData>.Enumerator enumerator6 = tmpDic.GetEnumerator();

                    while (enumerator6.MoveNext())
                    {
                        bw.Write(enumerator6.Current.Key);

                        bw.Write(enumerator6.Current.Value.id);
                    }
                }

                serverSendDataCallBack(_isMine, ms);
            }
        }
    }

    private void ServerReceiveCommand(bool _isMine, BinaryReader _br)
    {
        CommandData data;

        CommandType commandType = (CommandType)_br.ReadInt32();

        switch (commandType)
        {
            case CommandType.UNIT:

                int id = _br.ReadInt32();

                data = new UnitCommandData(_isMine, id);

                break;

            case CommandType.HERO:

                id = _br.ReadInt32();

                double x = _br.ReadDouble() + (random.NextDouble() - 0.5) * 0.01;

                double y = _br.ReadDouble() + (random.NextDouble() - 0.5) * 0.01;

                data = new HeroCommandData(_isMine, id, new Vector2(x, y));

                break;

            case CommandType.SKILL:

                id = _br.ReadInt32();

                x = _br.ReadDouble() + (random.NextDouble() - 0.5) * 0.01;

                y = _br.ReadDouble() + (random.NextDouble() - 0.5) * 0.01;

                data = new SkillCommandData(_isMine, id, new Vector2(x, y));

                break;

            default:

                throw new Exception("commandtype error");
        }

        using (MemoryStream ms = new MemoryStream())
        {
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bool b = ReceiveCommand(roundNum + gameConfig.GetCommandDelay(), GetCommandID(), data);

                bw.Write((int)S2CCommand.ACTION_OK);

                bw.Write(b);

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

                        ClientRefresh(br);

                        break;

                    case S2CCommand.UPDATE:

                        ClientUpdate(br);

                        break;

                    case S2CCommand.ACTION_OK:

                        bool b = br.ReadBoolean();

                        sendCommandCallBack(b);

                        break;
                }
            }
        }
    }

    private void ClientRefresh(BinaryReader _br)
    {
        unitDic.Clear();

        unitList.Clear();

        mUnitPool.Clear();

        oUnitPool.Clear();

        commandPool.Clear();

        mHeroCommandPool.Clear();

        oHeroCommandPool.Clear();

        mSkillCommandPool.Clear();

        oSkillCommandPool.Clear();

        mHeroPool.Clear();

        oHeroPool.Clear();

        mUnitCommandPool.Clear();

        oUnitCommandPool.Clear();

        simulator.ClearAgents();

        clientIsMine = _br.ReadBoolean();

        serverRoundNum = roundNum = _br.ReadInt32();

        Log.Write("client refresh data " + roundNum + "  clientIsMine:" + clientIsMine);

        uid = _br.ReadInt32();

        mMoney = _br.ReadInt32();

        oMoney = _br.ReadInt32();

        int num = _br.ReadInt32();

        for (int i = 0; i < num; i++)
        {
            Unit unit = new Unit();

            unit.Init(this, simulator, _br);

            unitDic.Add(unit.uid, unit);

            unitList.AddLast(unit);

            if (unit.sds.GetIsHero())
            {
                Dictionary<int, Unit> tmpDic = unit.isMine ? mHeroPool : oHeroPool;

                tmpDic.Add(unit.id, unit);
            }
        }

        num = _br.ReadInt32();

        for (int i = 0; i < num; i++)
        {
            int id = _br.ReadInt32();

            int num2 = _br.ReadInt32();

            mUnitPool.Add(id, num2);
        }

        num = _br.ReadInt32();

        for (int i = 0; i < num; i++)
        {
            int id = _br.ReadInt32();

            int num2 = _br.ReadInt32();

            oUnitPool.Add(id, num2);
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

                CommandData commandData;

                CommandType commandType = (CommandType)_br.ReadInt32();

                switch (commandType)
                {
                    case CommandType.UNIT:

                        int id = _br.ReadInt32();

                        commandData = new UnitCommandData(isMine, id);

                        break;

                    case CommandType.HERO:

                        id = _br.ReadInt32();

                        double x = _br.ReadDouble();

                        double y = _br.ReadDouble();

                        commandData = new HeroCommandData(isMine, id, new Vector2(x, y));

                        Dictionary<int, HeroCommandData> tmpDic2 = isMine ? mHeroCommandPool : oHeroCommandPool;

                        tmpDic2.Add(id, commandData as HeroCommandData);

                        break;

                    case CommandType.SKILL:

                        id = _br.ReadInt32();

                        x = _br.ReadDouble();

                        y = _br.ReadDouble();

                        commandData = new SkillCommandData(isMine, id, new Vector2(x, y));

                        Dictionary<int, SkillCommandData> tmpDic3 = isMine ? mSkillCommandPool : oSkillCommandPool;

                        tmpDic3.Add(id, commandData as SkillCommandData);

                        break;

                    default:

                        throw new Exception("commandtype error");
                }

                tmpDic.Add(tmpCommandID, commandData);
            }
        }

        num = _br.ReadInt32();

        Dictionary<int, Dictionary<int, UnitCommandData>> unitCommandData = clientIsMine ? mUnitCommandPool : oUnitCommandPool;

        for (int i = 0; i < num; i++)
        {
            int tmpRoundNum = _br.ReadInt32();

            int num2 = _br.ReadInt32();

            Dictionary<int, UnitCommandData> tmpDic = new Dictionary<int, UnitCommandData>();

            unitCommandData.Add(tmpRoundNum, tmpDic);

            for (int m = 0; m < num2; m++)
            {
                int tmpCommandID = _br.ReadInt32();

                int id = _br.ReadInt32();

                tmpDic.Add(tmpCommandID, new UnitCommandData(clientIsMine, id));
            }
        }
    }

    private void ClientSendCommand(CommandData _data)
    {
        using (MemoryStream ms = new MemoryStream())
        {
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                bw.Write((int)C2SCommand.ACTION);

                if (_data is UnitCommandData)
                {
                    UnitCommandData command = _data as UnitCommandData;

                    bw.Write((int)CommandType.UNIT);

                    bw.Write(command.id);
                }
                else if (_data is HeroCommandData)
                {
                    HeroCommandData command = _data as HeroCommandData;

                    bw.Write((int)CommandType.HERO);

                    bw.Write(command.id);

                    bw.Write(command.pos.x);

                    bw.Write(command.pos.y);
                }
                else if (_data is SkillCommandData)
                {
                    SkillCommandData command = _data as SkillCommandData;

                    bw.Write((int)CommandType.SKILL);

                    bw.Write(command.id);

                    bw.Write(command.pos.x);

                    bw.Write(command.pos.y);
                }

                clientSendDataCallBack(ms);
            }
        }
    }

    public void ClientSendUnitCommand(int _id)
    {
        UnitCommandData data = new UnitCommandData(true, _id);

        ClientSendCommand(data);
    }

    public void ClientSendHeroCommand(int _id, double _x, double _y)
    {
        HeroCommandData data = new HeroCommandData(true, _id, new Vector2(_x, _y));

        ClientSendCommand(data);
    }

    public void ClientSendSkillCommand(int _id, double _x, double _y)
    {
        SkillCommandData data = new SkillCommandData(true, _id, new Vector2(_x, _y));

        ClientSendCommand(data);
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
