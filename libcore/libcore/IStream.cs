namespace libcore
{
	public interface IStream
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="offset"></param>
		/// <param name="count"></param>
		/// <returns>
		///		number of read bytes
		///		0: disconnected
		///		-1: pending, need to retry again
		/// </returns>
		int read(byte[] buffer, int offset, int count);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="offset"></param>
		/// <param name="count"></param>
		/// <returns>
		///		number of written bytes
		///		0: disconnected
		/// </returns>
		int write(byte[] buffer, int offset, int count);

		void close();
	}
}
