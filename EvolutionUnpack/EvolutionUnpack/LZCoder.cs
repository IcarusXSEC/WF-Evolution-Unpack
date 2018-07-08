using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EvolutionUnpack
{
	public static class LZCoder
	{
		public static void Decompress(byte[] compressedData, byte[] decompressedData)
		{
			int compPos = 0;
			int decompPos = 0;
			int compLen = compressedData.Length;
			int decompLen = decompressedData.Length;

			while (compPos < compLen)
			{
				byte codeWord = compressedData[compPos++];
				if (codeWord <= 0x1f)
				{
					// Encode literal
					if (decompPos + codeWord + 1 > decompLen) throw new IndexOutOfRangeException("Attempting to index past decompression buffer.");
					if (compPos + codeWord + 1 > compLen) throw new IndexOutOfRangeException("Attempting to index past compression buffer.");
					for (int i = codeWord; i >= 0; --i)
					{
						decompressedData[decompPos] = compressedData[compPos];
						++decompPos;
						++compPos;
					}

				}
				else
				{
					// Encode dictionary
					int copyLen = codeWord >> 5; // High 3 bits are copy length
					if (copyLen == 7) // If those three make 7, then there are more bytes to copy (maybe)
					{
						if (compPos >= compLen) throw new IndexOutOfRangeException("Attempting to index past compression buffer.");
						copyLen += compressedData[compPos++]; // Grab next byte and add 7 to it
					}
					if (compPos >= compLen) throw new IndexOutOfRangeException("Attempting to index past compression buffer.");
					int dictDist = ((codeWord & 0x1f) << 8) | compressedData[compPos]; // 13 bits code lookback offset
					++compPos;
					copyLen += 2; // Add 2 to copy length
					if (decompPos + copyLen > decompLen) throw new IndexOutOfRangeException("Attempting to index past decompression buffer.");
					int decompDistBeginPos = decompPos - 1 - dictDist;
					if (decompDistBeginPos < 0) throw new IndexOutOfRangeException("Attempting to index below decompression buffer.");
					for (int i = 0; i < copyLen; ++i, ++decompPos)
					{
						decompressedData[decompPos] = decompressedData[decompDistBeginPos + i];
					}
				}
			}

			if (decompPos != decompLen) throw new System.IO.InvalidDataException("Decoder did not decode all bytes.");
		}
	}
}
