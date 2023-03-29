using Microsoft.Extensions.ObjectPool;

namespace HtmlKit
{
	internal sealed class CharBufferPoolPolicy : IPooledObjectPolicy<CharBuffer2>
	{
		private const int MaxSize = 4 * 1024 * 1024;

		public CharBuffer2 Create ()
		{
			return new CharBuffer2 (MaxSize);
		}

		public bool Return (CharBuffer2 obj)
		{
			obj.Reset ();
			return obj.Capacity == MaxSize;
		}
	}
}
