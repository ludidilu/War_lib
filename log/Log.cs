using System;

public class Log
{
    private static Action<string> logPrintCallBack;
    private static Action<string> logWriteCallBack;

    public static void Init(Action<string> _logPrintCallBack, Action<string> _logWriteCallBack)
    {
        logPrintCallBack = _logPrintCallBack;
        logWriteCallBack = _logWriteCallBack;
    }

    public static void Print(string _str)
    {
        if (logPrintCallBack != null)
        {
            logPrintCallBack(_str);
        }
    }

    public static void Write(string _str)
    {
        if (logWriteCallBack != null)
        {
            logWriteCallBack(_str);
        }
    }
}

