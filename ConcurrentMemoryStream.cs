
// ====================================================================================
//    Author: Al-Khafaji, Ali Kifah
//    Date:   12.12.2024
//    Description: A thread safe MemoryStream class
// =====================================================================================

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public class ConcurrentMemoryStream : IDisposable
{
    #region private members
    private MemoryStream memoryStream;
    private long readPosition = 0;
    private long writePosition = 0;
    private readonly SemaphoreSlim locker = new SemaphoreSlim(1, 1);
    private readonly SemaphoreSlim asyncLocker = new SemaphoreSlim(1, 1);
    private enum IOOperation
    {
        Write,
        Read,
    }
    #endregion

    #region public members
    public int Length => (int)memoryStream.Length;

    public byte[] ToArray => memoryStream.ToArray();

    public long Position { get { return memoryStream.Position; } set { memoryStream.Position = value; } }
    #endregion

    #region const/dest/disp
    public ConcurrentMemoryStream()
    {
        memoryStream = new MemoryStream();
    }
    public ConcurrentMemoryStream(int length)
    {
        memoryStream = new MemoryStream(length);
    }
    public ConcurrentMemoryStream(byte[] buffer)
    {
        memoryStream = new MemoryStream(buffer);
    }
    public ConcurrentMemoryStream(byte[] buffer, int offset, int count)
    {
        memoryStream = new MemoryStream(buffer, offset, count);
    }
    public ConcurrentMemoryStream(byte[] buffer, bool isWritable)
    {
        memoryStream = new MemoryStream(buffer, isWritable);
    }

    ~ConcurrentMemoryStream()
    {
        _dispose();
    }
    public void Dispose()
    {
        _dispose();
        GC.SuppressFinalize(this);
    }
    private void _dispose()
    {
        locker.Dispose();
        asyncLocker.Dispose();
        memoryStream.Dispose();
    }
    #endregion

    #region public methods
    public void Write(byte[] buffer, int offset, int count) => io(buffer, offset, count, IOOperation.Write);
    public int Read(byte[] buffer, int offset, int count) => io(buffer, offset, count, IOOperation.Read);
    public Task WriteAsync(byte[] buffer, int offset, int count) => ioAsync(buffer, offset, count, IOOperation.Write);
    public Task<int> ReadAsync(byte[] buffer, int offset, int count) => ioAsync(buffer, offset, count, IOOperation.Read);
    #endregion

    #region private methods
    private int io(byte[] buffer, int offset, int count, IOOperation iOOperation)
    {
        try
        {
            locker.Wait();
            if (iOOperation == IOOperation.Read)
            {
                memoryStream.Seek(readPosition, SeekOrigin.Begin);
                int bytesRead = memoryStream.Read(buffer, offset, count);
                readPosition += bytesRead;
                return bytesRead;
            }
            else
            {
                memoryStream.Seek(writePosition, SeekOrigin.Begin);
                memoryStream.Write(buffer, offset, count);
                writePosition = memoryStream.Position;
                return 0;
            }
        }
        catch (Exception e)
        {
            throw e;
        }
        finally
        {
            try
            {
                locker.Release();
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
    private async Task<int> ioAsync(byte[] buffer, int offset, int count, IOOperation iOOperation)
    {
        try
        {
            await asyncLocker.WaitAsync();
            if (iOOperation == IOOperation.Read)
            {
                memoryStream.Seek(readPosition, SeekOrigin.Begin);
                int bytesRead = await memoryStream.ReadAsync(buffer, offset, count);
                readPosition += bytesRead;
                return bytesRead;
            }
            else
            {
                memoryStream.Seek(writePosition, SeekOrigin.Begin);
                await memoryStream.WriteAsync(buffer, offset, count);
                writePosition = memoryStream.Position;
                return 0;
            }
        }
        catch (Exception e)
        {
            throw e;
        }
        finally
        {
            try
            {
                asyncLocker.Release();
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
    #endregion

}// end class


