﻿using System;
using System.IO;
using System.Text;

namespace UnityDataTools.FileSystem
{
    // This class can be used to read typed data from a UnityFile. Is uses a buffer for better performance.
    public class UnityFileReader : IDisposable
    {
        UnityFile   m_File;
        byte[]      m_Buffer;
        long        m_BufferStartInFile;
        long        m_BufferEndInFile;

        public long Length { get; }

        public UnityFileReader(string path, int bufferSize)
        {
            m_Buffer = new byte[bufferSize];
            m_BufferStartInFile = 0;
            m_BufferEndInFile = 0;

            m_File = UnityFileSystem.OpenFile(path);
            Length = m_File.GetSize();
        }

        int GetBufferOffset(long fileOffset, int count)
        {
            // Should we update the buffer?
            if (fileOffset < m_BufferStartInFile || fileOffset + count > m_BufferEndInFile)
            {
                if (count > m_Buffer.Length)
                    throw new IOException("Requested size is larger than cache size");

                m_BufferStartInFile = m_File.Seek(fileOffset);

                if (m_BufferStartInFile != fileOffset)
                    throw new IOException("Invalid file offset");

                m_BufferEndInFile = m_File.Read(m_Buffer.Length, m_Buffer);
                m_BufferEndInFile += m_BufferStartInFile;
            }

            return (int)(fileOffset - m_BufferStartInFile);
        }

        public void ReadArray(long fileOffset, int size, Array dest)
        {
            var offset = GetBufferOffset(fileOffset, size);
            Buffer.BlockCopy(m_Buffer, offset, dest, 0, size);
        }
        
        public string ReadString(long fileOffset, int size)
        {
            var offset = GetBufferOffset(fileOffset, size);
            return Encoding.Default.GetString(m_Buffer, offset, size);
        }

        public float ReadFloat(long fileOffset)
        {
            var offset = GetBufferOffset(fileOffset, 4);
            return BitConverter.ToSingle(m_Buffer, offset);
        }

        public double ReadDouble(long fileOffset)
        {
            var offset = GetBufferOffset(fileOffset, 8);
            return BitConverter.ToDouble(m_Buffer, offset);
        }

        public long ReadInt64(long fileOffset)
        {
            var offset = GetBufferOffset(fileOffset, 8);
            return BitConverter.ToInt64(m_Buffer, offset);
        }

        public ulong ReadUInt64(long fileOffset)
        {
            var offset = GetBufferOffset(fileOffset, 8);
            return BitConverter.ToUInt64(m_Buffer, offset);
        }

        public int ReadInt32(long fileOffset)
        {
            var offset = GetBufferOffset(fileOffset, 4);
            return BitConverter.ToInt32(m_Buffer, offset);
        }

        public uint ReadUInt32(long fileOffset)
        {
            var offset = GetBufferOffset(fileOffset, 4);
            return BitConverter.ToUInt32(m_Buffer, offset);
        }

        public short ReadInt16(long fileOffset)
        {
            var offset = GetBufferOffset(fileOffset, 2);
            return BitConverter.ToInt16(m_Buffer, offset);
        }

        public ushort ReadUInt16(long fileOffset)
        {
            var offset = GetBufferOffset(fileOffset, 2);
            return BitConverter.ToUInt16(m_Buffer, offset);
        }

        public sbyte ReadInt8(long fileOffset)
        {
            var offset = GetBufferOffset(fileOffset, 1);
            return (sbyte)m_Buffer[offset];
        }

        public byte ReadUInt8(long fileOffset)
        {
            var offset = GetBufferOffset(fileOffset, 1);
            return m_Buffer[offset];
        }

        public void Dispose()
        {
            m_File.Dispose();
        }
    }
}
