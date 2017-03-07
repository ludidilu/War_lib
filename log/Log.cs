using System;

public class Log
{
    private static Action<string> logCallBack;

    public static void Init(Action<string> _logCallBack)
    {
        logCallBack = _logCallBack;
    }

    public static void Write(string _str)
    {
        if (logCallBack != null)
        {
            logCallBack(_str);
        }
    }
}

