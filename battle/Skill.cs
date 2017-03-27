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

    public bool isMine { get; protected set; }

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

    public int startRoundNum { private set; get; }

    
}
