using EasyEmpire.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PicToGif
{
	internal class PicFile// : IImageFormat
	{
		private static Dictionary<string, PicFile> _cache = new Dictionary<string, PicFile>();
		private byte[] _bytes;
		private byte[,] _colourTable;
		//	private readonly Color[] _palette16;  //Locator.Common.GetPalette16;
		private Color[] _palette256 = new Color[256];
		private byte[,] _picture16;
		private byte[,] _picture256;

		public bool HasPalette16 { get; internal set; }
		public bool HasPalette256 { get; internal set; }
		public bool HasPicture16 { get; internal set; }
		public bool HasPicture256 { get; internal set; }

		//public Color[] GetPalette16
		//{
		//	get
		//	{
		//		return _palette16;
		//	}
		//}

		public Color[] GetPalette256
		{
			get
			{
				return _palette256;
			}
		}

		public byte[,] GetPicture16
		{
			get
			{
				return _picture16;
			}
		}

		public byte[,] GetPicture256
		{
			get
			{
				return _picture256;
			}
		}

		public int[,] GetIntRGBAArray()
		{
			int xSize = _picture256.GetUpperBound(0) + 1;
			int ySize = _picture256.GetUpperBound(1) + 1;
			var result = new int[xSize, ySize];

			for (int yy = _picture256.GetUpperBound(1); yy >= 0; yy--)
			{
				for (int xx = 0; xx <= _picture256.GetUpperBound(0); xx++)
				{
					result[xx, yy] = _palette256[_picture256[xx, yy]].GetInegerRGBA();
				}
			}

			return result;

		}


		public int[] GetIntRGBA()
		{
			int size = _picture256.GetUpperBound(0) * _picture256.GetUpperBound(1);
			int[] output = new int[size];
			int i = 0;
			for (int yy = _picture256.GetUpperBound(1); yy >= 0; yy--)
			{
				for (int xx = 0; xx <= _picture256.GetUpperBound(0); xx++)
				{
					output[i++] = _palette256[_picture256[xx, yy]].GetInegerRGBA();
				}
			}
			return output;
		}

		public int[,] GetIntRGBA2D()
		{
			throw new NotImplementedException();
			//   new int[]
			//int size = _picture256.GetUpperBound(0) * _picture256.GetUpperBound(1);
			//int[] output = new int[size];
			//int i = 0;
			//for (int yy = _picture256.GetUpperBound(1); yy >= 0; yy--)
			//{
			//    for (int xx = 0; xx <= _picture256.GetUpperBound(0); xx++)
			//    {
			//        output[i++] = _palette256[_picture256[xx, yy]].GetInegerRGBA();
			//    }
			//}
			//return output;
		}


		private byte[,] ReadColourTable(ref int index)
		{
			byte[,] colourTable = new byte[256, 2];
			uint length = BitConverter.ToUInt16(_bytes, index); index += 2;
			byte firstIndex = _bytes[index++];
			byte lastIndex = _bytes[index++];

			for (int i = 0; i < 256; i++)
			{
				if (i < firstIndex || i > lastIndex)
				{
					for (int j = 0; j < 2; j++)
					{
						colourTable[i, j] = 0;
					}
					continue;
				}

				colourTable[i, 0] = (byte)((_bytes[index] & 0xF0) >> 4);
				colourTable[i, 1] = (byte)(_bytes[index] & 0x0F);
				index++;
			}

			colourTable[0, 0] = 0;
			colourTable[0, 1] = 0;

			return colourTable;
		}

		private void ReadColourPalette(ref int index)
		{
			uint length = BitConverter.ToUInt16(_bytes, index); index += 2;
			byte firstIndex = _bytes[index++];
			byte lastIndex = _bytes[index++];
			for (int i = 0; i < 256; i++)
			{
				if (i < firstIndex || i > lastIndex)
				{
					_palette256[i] = Color.Transparent;
					continue;
				}
				byte red = _bytes[index++], green = _bytes[index++], blue = _bytes[index++];
				_palette256[i] = new Color(255, red * 4, green * 4, blue * 4);
			}

			_palette256[0] = Color.Transparent;
		}

		private byte[] DecodePicture(ref int index, uint length)
		{
			byte bits = _bytes[index++];
			byte[] img = new byte[length - 5];
			Array.Copy(_bytes, index, img, 0, (int)(length - 5));
			index += (int)(length - 5);
			return RLE.Decode(LZW.Decode(img));
		}

		private void ReadPictureX0(ref int index)
		{
			uint length = BitConverter.ToUInt16(_bytes, index); index += 2;
			uint width = BitConverter.ToUInt16(_bytes, index); index += 2;
			uint height = BitConverter.ToUInt16(_bytes, index); index += 2;

			_picture256 = new byte[width, height];

			byte[] image = DecodePicture(ref index, length);
			int c = 0;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					if (image.Length <= c)
					{
						_picture256[x, y] = 0;
						continue;
					}
					_picture256[x, y] = image[c++];
				}
			}
		}

		private void ReadPictureX1(ref int index)
		{
			uint length = BitConverter.ToUInt16(_bytes, index); index += 2;
			uint width = BitConverter.ToUInt16(_bytes, index); index += 2;
			uint height = BitConverter.ToUInt16(_bytes, index); index += 2;

			_picture16 = new byte[width, height];

			byte[] image = DecodePicture(ref index, length);
			int c = 0;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					_picture16[x++, y] = (byte)(image[c] & 0x0F);
					_picture16[x, y] = (byte)((image[c++] & 0xF0) >> 4);
				}
			}
		}

		private void ConvertPictureX0(byte[,] colourTable)
		{
			if (colourTable == null) return;

			int width = _picture256.GetLength(0);
			int height = _picture256.GetLength(1);

			_picture16 = new byte[width, height];

			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					byte col256 = _picture256[x, y];
					_picture16[x, y] = colourTable[col256, (y + x) % 2];
				}
			}
		}

		//private IEnumerable<byte> GetColourPaletteBytes()
		//{
		//	foreach (byte b in BitConverter.GetBytes((ushort)770)) yield return b;
		//	yield return (byte)0x00;
		//	yield return (byte)0xFF;

		//	foreach (Color color in _palette256)
		//	{
		//		yield return (byte)(color.R / 4);
		//		yield return (byte)(color.G / 4);
		//		yield return (byte)(color.B / 4);
		//	}
		//}

		private IEnumerable<byte> GetPictureData(byte[,] input)
		{
			for (int yy = 0; yy < _picture256.GetLength(1); yy++)
			{
				for (int xx = 0; xx < input.GetLength(0); xx++)
				{
					yield return input[xx, yy];
				}
			}
		}

		//public byte[] GetBytes()
		//{
		//	using (var ms = new MemoryStream())
		//	using (var br = new BinaryWriter(ms))
		//	{
		//		if (HasPalette16)
		//		{
		//			br.Write((ushort)0x3045);
		//			throw new NotImplementedException();
		//		}
		//		if (HasPalette256)
		//		{
		//			br.Write((ushort)0x304D);
		//			br.Write(GetColourPaletteBytes().ToArray());
		//		}
		//		if (HasPicture256)
		//		{
		//			br.Write((ushort)0x3058);

		//			byte[] encoded = RLE.Encode(GetPictureData(_picture256).ToArray());
		//			encoded = LZW.Encode(encoded);

		//			br.Write((ushort)(encoded.Length + 5));
		//			br.Write((ushort)_picture256.GetLength(0));
		//			br.Write((ushort)_picture256.GetLength(1));
		//			br.Write((byte)11);
		//			br.Write(encoded);
		//		}
		//		if (HasPalette16)
		//		{
		//			br.Write((ushort)0x3158);
		//			throw new NotImplementedException();
		//		}
		//		return ms.ToArray();
		//	}
		//}

		//public PicFile()
		//{
		//	// _palette16 = Locator.Common.GetPalette16;
		//}

		//      public PicFile(Picture picture):this()
		//{
		//	_palette256 = picture.Palette;
		//	_picture16 = picture.Bitmap;
		//	_picture256 = picture.Bitmap;

		//	HasPalette16 = false;
		//	HasPicture16 = false;
		//	HasPalette256 = true;
		//	HasPicture256 = true;
		//}

		void FillFromStream(Stream stream)
		{
			_bytes = new byte[stream.Length];
			stream.Read(_bytes, 0, _bytes.Length);
			int index = 0;
			while (index < (_bytes.Length - 1))
			{
				uint magicCode = BitConverter.ToUInt16(_bytes, index);
				index += 2;
				switch (magicCode)
				{
					case 0x3045:
						_colourTable = ReadColourTable(ref index);
						HasPalette16 = true;
						break;
					case 0x304D:
						ReadColourPalette(ref index);
						HasPalette256 = true;
						break;
					case 0x3058:
						ReadPictureX0(ref index);
						ConvertPictureX0(_colourTable);
						HasPicture256 = true;
						break;
					case 0x3158:
						ReadPictureX1(ref index);
						HasPicture16 = true;
						break;
				}
			}
		}

		public PicFile(Stream stream) //: this()
		{
			FillFromStream(stream);
		}

		public PicFile(string filename) // : this()
		{

			//if (!filename.ToLower().EndsWith(".map"))
			//{
			//	foreach (string fileEntry in Directory.GetFiles(Locator.Settings.DataDirectory))
			//	{
			//		if (Path.GetFileName(fileEntry).ToLower() != $"{filename.ToLower()}.pic")
			//		{
			//			continue;
			//		}
			//		filename = fileEntry;
			//	}
			//}

			if (!File.Exists(filename))
			{
				Debug.WriteLine($"File not found: {filename.ToUpper()}.PIC");
				HasPalette16 = true;
				HasPalette256 = true;
				//_palette256 = Locator.Common.GetPalette256;
				_picture16 = new byte[320, 200];
				_picture256 = new byte[320, 200];
				for (int yy = 0; yy < 200; yy++)
				{
					for (int xx = 0; xx < 320; xx++)
					{
						_picture16[xx, yy] = 1;
						_picture256[xx, yy] = 1;
					}
				}

				return;
			}

			using (var fileStream = new FileStream(filename, FileMode.Open))
			{
				FillFromStream(fileStream);
			}

			Debug.WriteLine($"Loaded {filename}");
		}
	}
}