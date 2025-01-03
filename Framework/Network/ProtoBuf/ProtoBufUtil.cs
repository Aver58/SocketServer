﻿#region Copyright © 2018 Aver. All rights reserved.

/*
=====================================================
 AverFrameWork v1.0
 Filename:    ProtoBufUtil.cs
 Author:      Zeng Zhiwei
 Time:        2019/12/9 14:55:28
=====================================================
*/

#endregion

using ProtoBuf;
using System;
using System.IO;

public class ProtoBufUtil {
    // 序列化  
    public static byte[] Serialize<T>(T msg) {
        if (msg == null) {
            throw new ArgumentNullException(nameof(msg), "对象不能为空");
        }

        using (var memoryStream = new MemoryStream()) {
            Serializer.Serialize(memoryStream, msg);
            return memoryStream.ToArray();
        }
    }

    // 封包，依次写入协议数据长度、协议id、协议内容
    public static byte[] PackNetMsg(NetMsgData data) {
        ushort protoId = data.ID;
        MemoryStream ms = null;
        using (ms = new MemoryStream()) {
            ms.Position = 0;
            BinaryWriter writer = new BinaryWriter(ms);
            byte[] pbdata = Serialize(data.Data);
            ushort msglen = (ushort)pbdata.Length;
            writer.Write(msglen);
            writer.Write(protoId);
            writer.Write(pbdata);
            writer.Flush();
            return ms.ToArray();
        }
    }

    // 反序列化
    static public T Deserialize<T>(byte[] message) {
        T result = default(T);
        if (message != null) {
            using (var stream = new MemoryStream(message)) {
                result = Serializer.Deserialize<T>(stream);
            }
        }

        return result;
    }

    // 解包，依次写出协议数据长度、协议id、协议数据内容
    public static NetMsgData UnpackNetMsg(byte[] msgData) {
        MemoryStream ms = null;
        using (ms = new MemoryStream(msgData)) {
            BinaryReader reader = new BinaryReader(ms);
            ushort msgLen = reader.ReadUInt16(); // 2字节
            ushort protoId = reader.ReadUInt16(); // 2字节
            string pbdata = Deserialize<string>(reader.ReadBytes(msgLen));
            // 扣除4字节
            if (msgLen <= msgData.Length - 4) {
                NetMsgData data = new NetMsgData() {
                    ID = protoId,
                    Data = pbdata
                };
                return data;
            } else {
                Console.WriteLine($"协议长度错误 {msgData.Length}");
            }
        }

        return null;
    }
}