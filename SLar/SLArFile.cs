using SerializeLib;
using SerializeLib.Interfaces;

namespace SLar;

/// <summary>
/// A reference to a file in a SerializeLib archive.
/// </summary>
public struct SLArFile : ISerializableClass<SLArFile>, IDisposable
{
    public string? Name; // Indicates the path of the resource
    
    /// <summary>
    /// A stream which can be used to read/write the slar file. It's recommended to use a filestream, but this is not a requirement.
    /// Any stream with an implemented Length will work.
    /// When deserializing, this stream will be a PartialStream, which will redirect the stream it's reading from.
    /// </summary>
    private Stream? _stream;

    /// <summary>
    /// A PartialStream for reading this SLArFile's content. Moves parent stream when used.
    /// </summary>
    public PartialStream Stream
    {
        get
        {
            _stream?.Seek(0, SeekOrigin.Begin);
            return (_stream as PartialStream)!;
        }
    }

    public SLArFile()
    {
        Name = null;
        _stream = null;
    }

    public SLArFile(string name, Stream stream)
    {
        Name = name;
        _stream = stream;
    }
    
    private BigIntegerSerialization BigIntSerialization => new BigIntegerSerialization(); // Quick access to serialize and deserialize BigInteger without override.
    
    public void Serialize(Stream s)
    {
        Serializer.SerializeValue(Name, s);
        BigIntSerialization.Serialize(_stream!.Length, s);

        _stream.Seek(0, SeekOrigin.Begin);
        _stream.CopyTo(s);
    }

    public SLArFile Deserialize(Stream s)
    {
        Name = Serializer.DeserializeValue<string>(s)!;
        var length = (long)BigIntSerialization.Deserialize(s);
        var pos = s.Position;

        // Set Stream to the partialstream associated with this chunk of the file.
        // This is done so the actual data doesn't need to be loaded into memory, just a reference to it.
        _stream = new PartialStream(s, pos, length);
        
        var end = length + pos;
        s.Seek(end, SeekOrigin.Begin);
        
        return this;
    }

    public void Dispose()
    {
        _stream?.Dispose();
    }
}