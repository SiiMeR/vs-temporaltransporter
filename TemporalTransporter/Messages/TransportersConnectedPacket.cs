using ProtoBuf;

namespace TemporalTransporter.Messages;

[ProtoContract]
public class TransportersConnectedPacket
{
    [ProtoMember(1)] public required string[] TransporterIds;
}