using System;
using System.Collections.Generic;
using Google.Protobuf;
using Protocol;

namespace TeddyServer.Shared {
    public partial class ProtoManager {
        // 手动维护协议id
        private static readonly Dictionary<int, Func<IMessage>> protocolMap = new Dictionary<int, Func<IMessage>> {
            { 1, () => new Handshake() },
            { 2, () => new PlayerInfo() },
            { 3, () => new ReqLogin() },
            { 4, () => new ReqRegister() },
            { 5, () => new RetLogin() },
            { 6, () => new RetRegister() },
        };

        public static IMessage ParseMessage(int id, byte[] data) {
            if (protocolMap.TryGetValue(id, out var factory)) {
                IMessage message = factory();
                message.MergeFrom(data);
                return message;
            }

            throw new Exception($"Unknown Protocol ID: {id}");
        }
    }
}