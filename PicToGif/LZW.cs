using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EasyEmpire.IO
{
	public static class LZW
	{
		private class ByteList
		{
			private readonly List<byte> _byteList = new List<byte>();
			private sbyte _byteNumber = 0;
			private byte _byte = 0;

			public void Add(int entry, int dictionarySize)
			{
				int codeLength = CodeLength(dictionarySize - 1);
				for (int bit = 0; bit < codeLength; bit++)
				{
					int outputBit = (entry & (0x01 << bit)) >> bit;
					_byte |= (byte)(outputBit << _byteNumber++);
					if (_byteNumber < 8) continue;
					_byteList.Add(_byte);
					_byteNumber = 0;
					_byte = 0;
				}
			}

			public void Close()
			{
				_byteList.Add(_byte);
			}

			public byte[] ToArray()
			{
				return _byteList.ToArray();
			}
		}

		private static byte CodeLength(int input)
		{
			for (int i = 31; i >= 0; i--)
			{
				if (((input >> i) & 1) == 1) return (byte)(i + 1);
			}
			return 1;
		}

		private static byte[] Append(byte[] array, params byte[] bytes)
		{
			byte[] output = new byte[array.Length + bytes.Length];
			for (int i = 0; i < array.Length; i++)
				output[i] = array[i];
			for (int i = 0; i < bytes.Length; i++)
				output[i + array.Length] = bytes[i];
			return output;
		}

		private static void DecodeDictionary(bool clearEnd, out Dictionary<int, byte[]> dictionary, out List<string> valueList)
		{
			dictionary = Enumerable.Range(0, 256).ToDictionary(x => x, x => new byte[] { (byte)x });
			dictionary.Add(dictionary.Count, new byte[0]);
			if (clearEnd)
			{
				dictionary.Add(dictionary.Count, new byte[0]);
			}
			valueList= new List<string>(dictionary.Values.Select(x => string.Join(",", x)));
		}
		
		public static byte[] Decode(byte[] input, bool clearEnd = false, bool flushDictionary = true, int maxBits = 11)
		{
			using (var ms = new MemoryStream())
			using (var bw = new BinaryWriter(ms))
			{
				Dictionary<int, byte[]> dictionary;
				List<string> values;

				DecodeDictionary(clearEnd, out dictionary, out values);

				int value = 0;
				int counter = 0;
				byte[] entry = new byte[0];
				for (int i = 0; i < input.Length; i++)
				{
					int codeLength = CodeLength(dictionary.Count);
					if (codeLength > maxBits) codeLength = maxBits;
					for (int bit = 0; bit < 8; bit++)
					{
						value |= ((input[i] >> bit) & 0x01) << counter++;
						if (counter != codeLength) continue;
						
						if (!dictionary.ContainsKey(value) && (flushDictionary || dictionary.Count < ((0x01 << maxBits) - 1)))
						{
							byte[] bytes = Append(entry, entry[0]);
							dictionary.Add(dictionary.Count, bytes);
							values.Add(string.Join(",", bytes));
						}
						
						byte[] outVal = dictionary[value];
						byte[] newEntry = Append(entry, outVal[0]);
						bw.Write(outVal);

						string stringValue = string.Join(",", newEntry);
						if (!values.Contains(stringValue) && (flushDictionary || dictionary.Count < ((0x01 << maxBits) - 1)))
						{
							dictionary.Add(dictionary.Count, newEntry);
							values.Add(stringValue);
						}
						entry = outVal;
						value = 0;
						counter = 0;
						
						if (flushDictionary && CodeLength(dictionary.Count) > maxBits)
						{
							DecodeDictionary(clearEnd, out dictionary, out values);
							entry = new byte[0];
						}
					}
				}

				return ms.ToArray();
			}
		}

		private static Dictionary<string, int> EncodeDictionary(bool clearEnd)
		{
			var output = Enumerable.Range(0, 256).ToDictionary(x => x.ToString(), x => x);
			output.Add("CLR", output.Count);
			if (clearEnd)
			{
				output.Add("END", output.Count);
			}
			return output;
		}

		public static byte[] Encode(byte[] input, bool clearEnd = false, bool flushDictionary = true, int maxBits = 11)
		{
			Dictionary<string, int> dictionary = EncodeDictionary(clearEnd);
			var byteList = new ByteList();

			if (clearEnd)
			{
				byteList.Add(dictionary["CLR"], dictionary.Count);
			}
			
			byte[] entry = new byte[0];
			for (int i = 0; i < input.Length; i++)
			{
				byte[] newEntry = Append(entry, input[i]);
				if (dictionary.ContainsKey(string.Join(",", newEntry)))
				{
					entry = newEntry;
					continue;
				}
				byteList.Add(dictionary[string.Join(",", entry)], dictionary.Count);
				if (flushDictionary)
				{
					if (CodeLength(dictionary.Count) <= maxBits)
					{
						dictionary.Add(string.Join(",", newEntry), dictionary.Count);
					}
					else
					{
						dictionary = EncodeDictionary(clearEnd);
					}
				}
				else if (CodeLength(dictionary.Count + 1) <= maxBits)
				{
					dictionary.Add(string.Join(",", newEntry), dictionary.Count);
				}
				
				entry = new byte[] { input[i] };
			}

			if (entry.Length > 0)
			{
				byteList.Add(dictionary[string.Join(",", entry)], dictionary.Count);
			}

			if (clearEnd)
			{
				byteList.Add(dictionary["END"], dictionary.Count);
			}
			byteList.Close();

			return byteList.ToArray();
		}
	}
}