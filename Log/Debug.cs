#region Copyright © 2020 Aver. All rights reserved.
/*
=====================================================
 AverFrameWork v1.0
 Filename:    Debug.cs
 Author:      Zeng Zhiwei
 Time:        2020/12/26 10:15:47
=====================================================
*/
#endregion

using System;

public class Debug
{
    public static void Log(string msg, params object[] arg)
    {
        Console.WriteLine(msg, arg);
    }
}