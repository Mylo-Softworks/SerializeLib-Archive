using System.Numerics;
using SerializeLib;
using SerializeLib.Interfaces;

namespace SLar;

/// <summary>
/// Serialization override for BigInteger, can be registered.
/// </summary>
public class BigIntegerSerialization : ISerializableOverride<BigInteger>
{
    public void Serialize(BigInteger target, Stream s)
    {
        var bytes = target.ToByteArray();
        Serializer.SerializeValue((byte)bytes.Length, s);
        s.Write(bytes, 0, bytes.Length);
    }

    public BigInteger Deserialize(Stream s)
    {
        var size = Serializer.DeserializeValue<byte>(s);
        var bytes = new byte[size];
        s.Read(bytes, 0, bytes.Length);
        return new BigInteger(bytes);
    }
}