namespace SLar;

/// <summary>
/// Stream which wraps around another stream, uses seeking with offsets to allow it to read a specified part of the parent stream.
/// </summary>
public class PartialStream : Stream
{
    private readonly Stream _parent;
    private readonly long _startOffset;
    private readonly long _size;
    
    public PartialStream(Stream parent, long startOffset, long size)
    {
        _parent = parent;
        _startOffset = startOffset;
        _size = size;
    }

    /// <summary>
    /// Read all the bytes that this stream has access to into a byte array.
    /// </summary>
    /// <returns>The byte array that was read into.</returns>
    public byte[] ReadToByteArray()
    {
        Seek(0, SeekOrigin.Begin);
        byte[] buffer = new byte[_size];
        Read(buffer, 0, buffer.Length);
        return buffer;
    }
    
    public override void Flush()
    {
        _parent.Flush();
    }
    
    public override int Read(byte[] buffer, int offset, int count)
    {
        var remaining = Length - Position;
        var returnVal = 0;
        if (offset + count > remaining) returnVal = -1;
        
        var ableCount = (int)Math.Min(remaining, count);
        
        var newReturnVal = _parent.Read(buffer, offset, ableCount);
        if (returnVal == -1)
            return -1;
        
        return newReturnVal;
    }

    public override void CopyTo(Stream destination, int bufferSize)
    {
        Seek(0, SeekOrigin.Begin);
        var buffer = new byte[bufferSize];
        var readCount = (int)Math.Floor(Length / (double)bufferSize); // +1
        var lastReadCount = (int)Length % bufferSize;

        for (var i = 0; i < readCount; i++)
        {
            CopyWrite(destination, buffer, bufferSize);
        }
        CopyWrite(destination, buffer, lastReadCount);
    }

    private void CopyWrite(Stream destination, byte[] buffer, int bufferSize)
    {
        Read(buffer, 0, bufferSize);
        destination.Write(buffer, 0, bufferSize);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return origin switch
        {
            SeekOrigin.Begin => _parent.Seek(_startOffset + offset, SeekOrigin.Begin),
            SeekOrigin.Current => _parent.Seek(offset, SeekOrigin.Current),
            SeekOrigin.End => _parent.Seek(_startOffset + _size - offset, SeekOrigin.Begin), // Since it has a different end
            _ => throw new NotSupportedException()
        };
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException("PartialStream cannot be resized.");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException("Writing is not supported");
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _size;

    public override long Position
    {
        get => _parent.Position - _startOffset;
        set => _parent.Position = _startOffset + value;
    }

    protected override void Dispose(bool disposing)
    {
        _parent.Dispose();
    }
}