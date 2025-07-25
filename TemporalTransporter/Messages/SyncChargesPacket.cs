using ProtoBuf;

namespace TemporalTransporter.Messages;

[ProtoContract]
public class SyncChargesPacket
{
    [ProtoMember(2)] public required int ChargeCount;

    [ProtoMember(1)] public required string CoordinateKey;
}