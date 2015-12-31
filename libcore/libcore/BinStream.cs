using System.IO;

namespace libcore
{
	public class BinStream
	{
		public BinStream(byte[] bytes) : this(bytes, bytes.Length)
		{
			
		}

		public BinStream(byte[] bytes, int length) : this(bytes, 0, length)
		{

		}

		public BinStream(byte[] bytes, int offset, int length)
		{
			m_offset = offset;
			m_bytes = bytes;
			m_length = length;
			m_memoryStream = new MemoryStream(bytes, offset, length);
			m_reader = new BinaryReader(m_memoryStream);
		}

		public int currentPosition()
		{
			return (int)m_reader.BaseStream.Position;
		}

		public void close()
		{
			m_bytes = null;
			m_length = 0;
			m_reader.Close();
			m_memoryStream.Close();
		}

		public byte readByte()
		{
			return m_reader.ReadByte();
		}

		public sbyte readSByte()
		{
			return m_reader.ReadSByte();
		}

		public short readShort()
		{
			return m_reader.ReadInt16();
		}

		public ushort readUShort()
		{
			return m_reader.ReadUInt16();
		}

		public int readInt()
		{
			return m_reader.ReadInt32();
		}

		public uint readUInt()
		{
			return m_reader.ReadUInt32();
		}

		public float readFloat()
		{
			return m_reader.ReadSingle();
		}

		public int readBytes(byte[] bytes, int offset, int count)
		{
			return m_reader.Read(bytes, offset, count);
		}

		public string readString(int length)
		{
			if (length <= 0) return "";
			string result = StringUtils.createStringFromBytes(m_bytes, m_offset + (int)m_reader.BaseStream.Position, length);
			m_reader.BaseStream.Seek(length, SeekOrigin.Current);
			return result;
		}

		/// <summary>
		/// Read string to the end of the stream
		/// </summary>
		/// <returns></returns>
		public string readString()
		{
			return readString(m_length - (int)m_reader.BaseStream.Position);
		}

		public void skip(int byteNumber)
		{
			m_reader.BaseStream.Seek(byteNumber, SeekOrigin.Current);
		}

		public int remainBytes()
		{
			return m_length - (int)m_reader.BaseStream.Position;
		}

		//============================================================================================================

		private MemoryStream m_memoryStream;
		private BinaryReader m_reader;
		private byte[] m_bytes;
		private int m_offset;
		private int m_length;
	}
}
